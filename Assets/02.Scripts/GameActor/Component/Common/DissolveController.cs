
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Component
{
    public class DissolveController : MonoBehaviour
    {
        private struct RendererInfo
        {
            public Renderer renderer;
            public Material[] originalSharedMaterials; // ResetDissolve 복원용
            public MaterialSlotInfo[] slots;
        }

        private struct MaterialSlotInfo
        {
            public Material originalMaterial;
            public Texture baseMap;
            public Texture mainTex;
            public bool isParticleMaterial;
            public bool supportsLilToonDissolve;
            public Vector4 originalDissolveParams;
        }

        private struct RuntimeMaterialInfo
        {
            public Material material;
            public bool supportsLilToonDissolve;
            public Vector4 lilToonDissolvePosition;
            public float lilToonStartRange;
            public float lilToonEndRange;
        }
        
        private const string DissolveMaterialAddress = "DissolveMaterial";
        private const string LilToonCutoutShaderName = "Hidden/lilToonCutout";
        private const string LilToonCutoutOutlineShaderName = "Hidden/lilToonCutoutOutline";
        private const string LilToonDissolveKeepAliveResourcePath = "Rendering/LilToonDissolveKeepAlive";
        private const string LilDissolveKeyword = "GEOM_TYPE_BRANCH_DETAIL";
        private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
        private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        private static readonly int LilDissolveParamsID = Shader.PropertyToID("_DissolveParams");
        private static readonly int LilDissolvePosID = Shader.PropertyToID("_DissolvePos");
        private static readonly int LilDissolveColorID = Shader.PropertyToID("_DissolveColor");
        private static readonly int LilDissolveNoiseMaskID = Shader.PropertyToID("_DissolveNoiseMask");
        private static readonly int LilDissolveNoiseMaskScrollRotateID = Shader.PropertyToID("_DissolveNoiseMask_ScrollRotate");
        private static readonly int LilDissolveNoiseStrengthID = Shader.PropertyToID("_DissolveNoiseStrength");
        private static readonly int TransparentModeID = Shader.PropertyToID("_TransparentMode");
        private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
        private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int AlphaToMaskID = Shader.PropertyToID("_AlphaToMask");
        private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");
        private static readonly PropertyInfo MaterialParentProperty = typeof(Material).GetProperty("parent", BindingFlags.Instance | BindingFlags.Public);

        // 셰이더만 공유, 머티리얼 인스턴스는 액터마다 생성
        private static AsyncOperationHandle<Material> _loadHandle;
        private static Material _dissolveSourceMaterial;
        private static Shader _lilToonCutoutShader;
        private static bool _reportedMissingLilToonCutoutShader;

        [Header("lilToon Dissolve")]
        [SerializeField] private bool _useLilToonDissolve = true;
        [SerializeField] private bool _forceLilToonCutout = true;
        [SerializeField] private LilToonDissolveShaderConversionProfile _lilToonShaderConversionProfile;
        [SerializeField] private float _lilToonDissolveMode = 3f;
        [SerializeField] private float _lilToonDissolveShape = 1f;
        [SerializeField] private Vector4 _lilToonDissolvePosition = new Vector4(0f, 1f, 0f, 0f);
        [SerializeField] private bool _useRendererBoundsRange = true;
        [SerializeField] private float _lilToonStartRange = -1f;
        [SerializeField] private float _lilToonEndRange = 1f;
        [SerializeField] private float _lilToonRangePadding = 0.05f;
        [SerializeField] private float _lilToonBlur = 0.1f;
        [SerializeField] private Color _lilToonDissolveColor = Color.white;
        [SerializeField] private Texture _lilToonDissolveNoiseMask;
        [SerializeField] private float _lilToonDissolveNoiseStrength = 0.1f;
        [SerializeField] private Vector4 _lilToonDissolveNoiseScrollRotate = Vector4.zero;
        [SerializeField] private float _fallbackTransparentMode = 1f;
        [SerializeField] private float _fallbackCutoff = 0.5f;
        [SerializeField] private UnityEngine.Rendering.RenderQueue _cutoutRenderQueue = UnityEngine.Rendering.RenderQueue.AlphaTest;

        private readonly List<RendererInfo> _rendererInfos = new List<RendererInfo>();
        private readonly List<Material> _instancedMaterials = new List<Material>(); // 해제용
        private readonly List<RuntimeMaterialInfo> _runtimeMaterialInfos = new List<RuntimeMaterialInfo>();
        private readonly Dictionary<Renderer, Material[]> _preparedMaterialSets = new Dictionary<Renderer, Material[]>();
        private static readonly HashSet<int> _reportedMissingLilToonDissolveProperty = new HashSet<int>();
        
        private float _dissolveDuration = 2f;
        private bool _overrideLilToonDissolveNoise;
        private bool _isDissolvePrepared;
        
        private void Awake()
        {
            InitializeRendererData();
            EnsureDissolveMaterialLoading();
        }

        private void OnDestroy()
        {
            // 인스턴스 머티리얼 해제
            foreach (var mat in _instancedMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
        
        private void EnsureDissolveMaterialLoading()
        {
            if (!HasFallbackDissolveSlots())
                return;

            if (!_loadHandle.IsValid())
            {
                _loadHandle = Addressables.LoadAssetAsync<Material>(DissolveMaterialAddress);
                _loadHandle.Completed += OnDissolveMaterialLoaded;
            }
            else if (_loadHandle.IsDone)
            {
                OnDissolveMaterialLoaded(_loadHandle);
            }
            else
            {
                _loadHandle.Completed -= OnDissolveMaterialLoaded;
                _loadHandle.Completed += OnDissolveMaterialLoaded;
            }
        }

        private static void OnDissolveMaterialLoaded(AsyncOperationHandle<Material> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[DissolveController] DissolveMaterial 로드 실패: {DissolveMaterialAddress}");
                return;
            }

            _dissolveSourceMaterial = handle.Result;
        }
        
        public void InitializeRendererData()
        {
            _rendererInfos.Clear();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;

                var sharedMaterials = r.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0) continue;

                bool hasRenderableSlot = false;
                var slots = new MaterialSlotInfo[sharedMaterials.Length];
                for (int i = 0; i < sharedMaterials.Length; i++)
                {
                    var material = sharedMaterials[i];
                    bool supportsLilToonDissolve = IsLilToonDissolveMaterial(material);

                    slots[i] = new MaterialSlotInfo
                    {
                        originalMaterial = material,
                        baseMap = GetTextureIfExists(material, BaseMapID),
                        mainTex = GetTextureIfExists(material, MainTexID),
                        isParticleMaterial = false,
                        supportsLilToonDissolve = supportsLilToonDissolve,
                        originalDissolveParams = supportsLilToonDissolve ? material.GetVector(LilDissolveParamsID) : Vector4.zero
                    };

                    if (material != null)
                        hasRenderableSlot = true;
                }

                if (!hasRenderableSlot) continue;

                _rendererInfos.Add(new RendererInfo
                {
                    renderer = r,
                    originalSharedMaterials = sharedMaterials,
                    slots = slots
                });
            }
        }
        
        /// <summary>
        /// 내장 무기 복원 시 호출. 디졸브 머티리얼을 제거하고 원본 머티리얼과 렌더러 상태를 복원한다.
        /// </summary>
        public void ResetDissolve()
        {
            StopAllCoroutines();

            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null) continue;
                info.renderer.enabled = true;
                if (info.originalSharedMaterials != null)
                    info.renderer.sharedMaterials = info.originalSharedMaterials;
            }

            foreach (var mat in _instancedMaterials)
                if (mat != null) Destroy(mat);
            _instancedMaterials.Clear();
            _runtimeMaterialInfos.Clear();
            _preparedMaterialSets.Clear();
            _isDissolvePrepared = false;
        }

        /// <summary>
        /// 모델 교체 시 호출. 기존 인스턴스 머티리얼을 해제하고 새 Model의 렌더러로 재초기화한다.
        /// </summary>
        public void RefreshRenderers()
        {
            StopAllCoroutines();

            foreach (var mat in _instancedMaterials)
                if (mat != null) Destroy(mat);
            _instancedMaterials.Clear();
            _runtimeMaterialInfos.Clear();
            _preparedMaterialSets.Clear();
            _isDissolvePrepared = false;

            InitializeRendererData();
            EnsureDissolveMaterialLoading();
        }

        public void WarmupDissolveMaterials()
        {
            if (_rendererInfos.Count == 0)
                return;

            EnsureDissolveMaterialLoading();
            PrepareDissolveMaterials(assignToRenderers: false);
            SetDissolveAmount(0f);
        }
        
        public void StartDissolve(float duration, bool destroyOnComplete = true, System.Action onComplete = null)
        {
            if (_rendererInfos.Count == 0)
            {
                onComplete?.Invoke();
                if (destroyOnComplete) Destroy(gameObject);
                return;
            }

            _dissolveDuration = duration;

            StopAllCoroutines();
            StartCoroutine(DissolveRoutine(destroyOnComplete, onComplete));
        }

        public void SetDissolveColor(Color color)
        {
            _lilToonDissolveColor = color;

            foreach (var info in _runtimeMaterialInfos)
            {
                if (info.material == null || !info.material.HasProperty(LilDissolveColorID))
                    continue;

                info.material.SetColor(LilDissolveColorID, _lilToonDissolveColor);
            }
        }

        public void SetDissolveNoise(Texture noiseMask, float strength, Vector4 scrollRotate)
        {
            _lilToonDissolveNoiseMask = noiseMask;
            _lilToonDissolveNoiseStrength = Mathf.Max(0f, strength);
            _lilToonDissolveNoiseScrollRotate = scrollRotate;
            _overrideLilToonDissolveNoise = noiseMask != null;

            foreach (var info in _runtimeMaterialInfos)
            {
                if (info.material == null || !info.supportsLilToonDissolve)
                    continue;

                ApplyLilToonDissolveNoise(info.material);
            }
        }

        public void CompleteDissolve(bool destroyOnComplete = true, System.Action onComplete = null)
        {
            StopAllCoroutines();

            if (_rendererInfos.Count > 0 && _dissolveSourceMaterial != null)
            {
                PrepareDissolveMaterials(assignToRenderers: true);
                SetDissolveAmount(1f);
            }
            else if (_rendererInfos.Count > 0 && HasLilToonDissolveSlots())
            {
                PrepareDissolveMaterials(assignToRenderers: true);
                SetDissolveAmount(1f);
            }

            onComplete?.Invoke();
            if (destroyOnComplete) Destroy(gameObject);
        }

        private IEnumerator DissolveRoutine(bool destroyOnComplete, System.Action onComplete)
        {
            EnsureDissolveMaterialLoading();

            float waitTime = 0f;
            while (HasFallbackDissolveSlots() && _dissolveSourceMaterial == null)
            {
                waitTime += Time.unscaledDeltaTime;
                if (waitTime > 1.5f)
                {
                    if (!HasLilToonDissolveSlots())
                    {
                        Debug.LogWarning("[DissolveController] DissolveMaterial 로드 지연/실패 — 즉시 파괴 처리.");
                        onComplete?.Invoke();
                        if (destroyOnComplete) Destroy(gameObject);
                        yield break;
                    }

                    Debug.LogWarning("[DissolveController] DissolveMaterial 로드 지연/실패 — lilToon 슬롯만 디졸브 처리.");
                    break;
                }
                yield return null;
            }

            if (!_isDissolvePrepared)
                PrepareDissolveMaterials(assignToRenderers: true);
            else
                ApplyPreparedMaterials();

            SetDissolveAmount(0f);

            float elapsed = 0f;
            while (elapsed < _dissolveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetDissolveAmount(Mathf.Clamp01(elapsed / _dissolveDuration));
                yield return null;
            }

            SetDissolveAmount(1f);

            onComplete?.Invoke();
            if (destroyOnComplete) Destroy(gameObject);
        }

        private void PrepareDissolveMaterials(bool assignToRenderers)
        { 
            foreach (var mat in _instancedMaterials)
                if (mat != null) Destroy(mat);
            _instancedMaterials.Clear();
            _runtimeMaterialInfos.Clear();
            _preparedMaterialSets.Clear();
            _isDissolvePrepared = false;
            GetGlobalLilToonRange(out float globalStartRange, out float globalEndRange);
            
            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null) continue;

                var mats = new Material[info.slots.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    var slot = info.slots[i];
                    if (slot.originalMaterial == null)
                    {
                        mats[i] = null;
                        continue;
                    }

                    if (slot.isParticleMaterial)
                    {
                        mats[i] = slot.originalMaterial;
                        continue;
                    }

                    Material instance = slot.supportsLilToonDissolve
                        ? CreateLilToonDissolveInstance(slot)
                        : CreateFallbackDissolveInstance(slot);

                    if (instance == null)
                    {
                        mats[i] = slot.originalMaterial;
                        continue;
                    }

                    mats[i] = instance;
                    _instancedMaterials.Add(instance);
                    GetLilToonRange(info.renderer, globalStartRange, globalEndRange, out float startRange, out float endRange, out Vector4 dissolvePosition);
                    if (slot.supportsLilToonDissolve)
                        SetLilToonDissolve(instance, startRange, dissolvePosition);

                    _runtimeMaterialInfos.Add(new RuntimeMaterialInfo
                    {
                        material = instance,
                        supportsLilToonDissolve = slot.supportsLilToonDissolve,
                        lilToonDissolvePosition = dissolvePosition,
                        lilToonStartRange = startRange,
                        lilToonEndRange = endRange
                    });
                }

                _preparedMaterialSets[info.renderer] = mats;
                if (assignToRenderers)
                    info.renderer.materials = mats;
            }

            _isDissolvePrepared = !HasFallbackDissolveSlots() || _dissolveSourceMaterial != null;
        }

        private void ApplyPreparedMaterials()
        {
            foreach (var pair in _preparedMaterialSets)
            {
                if (pair.Key == null || pair.Value == null)
                    continue;

                pair.Key.materials = pair.Value;
            }
        }

        private void SetDissolveAmount(float amount)
        {   
            foreach (var info in _runtimeMaterialInfos)
            {
                if (info.material == null) continue;

                if (info.supportsLilToonDissolve)
                {
                    float lilToonRange = Mathf.Lerp(info.lilToonStartRange, info.lilToonEndRange, amount);
                    SetLilToonDissolve(info.material, lilToonRange, info.lilToonDissolvePosition);
                }
                else if (info.material.HasProperty(DissolveAmountID))
                {
                    info.material.SetFloat(DissolveAmountID, amount);
                }
            }
        }

        private Material CreateLilToonDissolveInstance(MaterialSlotInfo slot)
        {
            if (!_useLilToonDissolve || slot.originalMaterial == null)
                return null;

            var instance = new Material(slot.originalMaterial);
            DetachMaterialVariant(instance);
            if (_forceLilToonCutout)
                ConvertLilToonInstanceToCutout(instance, slot.originalMaterial);

            SetLilToonDissolve(instance, _lilToonStartRange);
            if (instance.HasProperty(LilDissolveColorID))
                instance.SetColor(LilDissolveColorID, _lilToonDissolveColor);
            ApplyLilToonDissolveNoise(instance);

            return instance;
        }

        private void GetGlobalLilToonRange(out float startRange, out float endRange)
        {
            startRange = _lilToonStartRange;
            endRange = _lilToonEndRange;
            if (!_useRendererBoundsRange)
                return;

            Vector3 direction = new Vector3(_lilToonDissolvePosition.x, _lilToonDissolvePosition.y, _lilToonDissolvePosition.z);
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.up;

            direction.Normalize();
            bool hasBounds = false;
            float minRange = float.PositiveInfinity;
            float maxRange = float.NegativeInfinity;
            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null || !HasLilToonDissolveSlot(info))
                    continue;

                if (!TryGetLocalBounds(info.renderer, out var bounds))
                    continue;

                EncapsulateProjectedBounds(info.renderer, bounds, direction, ref minRange, ref maxRange);
                hasBounds = true;
            }

            if (!hasBounds)
                return;

            float padding = Mathf.Max(0f, _lilToonRangePadding + _lilToonBlur);
            startRange = minRange - padding;
            endRange = maxRange + padding;

            if (Mathf.Approximately(startRange, endRange))
                endRange = startRange + 0.01f;
        }

        private void GetLilToonRange(Renderer renderer, float globalStartRange, float globalEndRange, out float startRange, out float endRange, out Vector4 dissolvePosition)
        {
            startRange = _lilToonStartRange;
            endRange = _lilToonEndRange;
            dissolvePosition = _lilToonDissolvePosition;
            if (!_useRendererBoundsRange || renderer == null)
                return;

            Vector3 rootDirection = new Vector3(_lilToonDissolvePosition.x, _lilToonDissolvePosition.y, _lilToonDissolvePosition.z);
            if (rootDirection.sqrMagnitude <= 0.0001f)
                rootDirection = Vector3.up;

            rootDirection.Normalize();
            Vector3 rendererOriginInRoot = transform.InverseTransformPoint(renderer.transform.TransformPoint(Vector3.zero));
            float offset = Vector3.Dot(rendererOriginInRoot, rootDirection);
            Vector3 coefficient = new Vector3(
                Vector3.Dot(transform.InverseTransformPoint(renderer.transform.TransformPoint(Vector3.right)), rootDirection) - offset,
                Vector3.Dot(transform.InverseTransformPoint(renderer.transform.TransformPoint(Vector3.up)), rootDirection) - offset,
                Vector3.Dot(transform.InverseTransformPoint(renderer.transform.TransformPoint(Vector3.forward)), rootDirection) - offset);

            float scale = coefficient.magnitude;
            if (scale <= 0.0001f)
                return;

            Vector3 localDirection = coefficient / scale;
            dissolvePosition = new Vector4(localDirection.x, localDirection.y, localDirection.z, _lilToonDissolvePosition.w);
            startRange = (globalStartRange - offset) / scale;
            endRange = (globalEndRange - offset) / scale;

            if (Mathf.Approximately(startRange, endRange))
                endRange = startRange + 0.01f;
        }

        private void EncapsulateProjectedBounds(Renderer renderer, Bounds bounds, Vector3 direction, ref float minRange, ref float maxRange)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 rootCorner = transform.InverseTransformPoint(renderer.transform.TransformPoint(localCorner));
                        float projected = Vector3.Dot(rootCorner, direction);
                        minRange = Mathf.Min(minRange, projected);
                        maxRange = Mathf.Max(maxRange, projected);
                    }
                }
            }
        }

        private static bool HasLilToonDissolveSlot(RendererInfo info)
        {
            if (info.slots == null)
                return false;

            foreach (var slot in info.slots)
            {
                if (slot.originalMaterial != null && !slot.isParticleMaterial && slot.supportsLilToonDissolve)
                    return true;
            }

            return false;
        }

        private static bool TryGetLocalBounds(Renderer renderer, out Bounds bounds)
        {
            bounds = default;
            if (renderer is SkinnedMeshRenderer skinnedRenderer)
            {
                bounds = skinnedRenderer.localBounds;
                return true;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                bounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            return false;
        }

        private void SetLilToonDissolve(Material material, float range)
        {
            SetLilToonDissolve(material, range, _lilToonDissolvePosition);
        }

        private void SetLilToonDissolve(Material material, float range, Vector4 dissolvePosition)
        {
            EnableLilToonDissolveKeyword(material);
            
            if (!material.HasProperty(LilDissolveParamsID))
            {
                int materialId = material.GetInstanceID();
                if (_reportedMissingLilToonDissolveProperty.Add(materialId))
                    Debug.LogWarning($"[DissolveController] lilToon 디졸브 프로퍼티가 없습니다. material={material.name}, shader={material.shader?.name}");
                return;
            }

            material.SetVector(LilDissolveParamsID, new Vector4(
                _lilToonDissolveMode,
                _lilToonDissolveShape,
                range,
                _lilToonBlur));

            if (material.HasProperty(LilDissolvePosID))
                material.SetVector(LilDissolvePosID, dissolvePosition);
        }

        private static void EnableLilToonDissolveKeyword(Material material)
        {
            if (material == null)
                return;

            material.EnableKeyword(LilDissolveKeyword);

#if UNITY_2021_2_OR_NEWER
            if (material.shader == null)
                return;

            var keyword = new UnityEngine.Rendering.LocalKeyword(material.shader, LilDissolveKeyword);
            if (keyword.isValid)
                material.SetKeyword(keyword, true);
#endif
        }

        private void ApplyLilToonDissolveNoise(Material material)
        {
            if (!_overrideLilToonDissolveNoise || material == null)
                return;

            if (material.HasProperty(LilDissolveNoiseMaskID))
                material.SetTexture(LilDissolveNoiseMaskID, _lilToonDissolveNoiseMask);

            if (material.HasProperty(LilDissolveNoiseStrengthID))
                material.SetFloat(LilDissolveNoiseStrengthID, _lilToonDissolveNoiseStrength);

            if (material.HasProperty(LilDissolveNoiseMaskScrollRotateID))
                material.SetVector(LilDissolveNoiseMaskScrollRotateID, _lilToonDissolveNoiseScrollRotate);
        }

        private Material CreateFallbackDissolveInstance(MaterialSlotInfo slot)
        {
            if (_dissolveSourceMaterial == null)
                return null;

            var instance = new Material(_dissolveSourceMaterial);
            if (slot.baseMap != null && instance.HasProperty(BaseMapID))
                instance.SetTexture(BaseMapID, slot.baseMap);

            if (slot.mainTex != null && instance.HasProperty(MainTexID))
                instance.SetTexture(MainTexID, slot.mainTex);

            if (instance.HasProperty(LilDissolveColorID))
                instance.SetColor(LilDissolveColorID, _lilToonDissolveColor);

            return instance;
        }

        private void ConvertLilToonInstanceToCutout(Material material, Material sourceMaterial)
        {
            if (material == null)
                return;

            if (TryGetProfileRule(sourceMaterial, out var rule))
            {
                if (!rule.keepSourceShader && rule.cutoutShader != null)
                    material.shader = rule.cutoutShader;

                ApplyCutoutRenderState(material, rule.transparentMode);
                return;
            }

            Shader cutoutShader = ResolveLilToonCutoutShader(sourceMaterial);
            if (cutoutShader != null)
                material.shader = cutoutShader;

            ApplyCutoutRenderState(material, _fallbackTransparentMode);
        }

        private bool TryGetProfileRule(Material material, out LilToonDissolveShaderConversionProfile.ShaderConversionRule rule)
        {
            rule = null;
            if (_lilToonShaderConversionProfile == null || material == null)
                return false;

            foreach (var current in EnumerateMaterialAndParents(material))
            {
                if (current == null || current.shader == null)
                    continue;

                if (_lilToonShaderConversionProfile.TryGetRule(current.shader, out rule))
                    return true;
            }

            return false;
        }

        private Shader ResolveLilToonCutoutShader(Material material)
        {
            foreach (var current in EnumerateMaterialAndParents(material))
            {
                Shader shader = ResolveLilToonCutoutShaderFromSource(current.shader);
                if (shader != null)
                    return shader;
            }

            return null;
        }

        private Shader ResolveLilToonCutoutShaderFromSource(Shader sourceShader)
        {
            if (sourceShader == null)
                return null;

            string sourceName = sourceShader.name;
            if (string.IsNullOrEmpty(sourceName))
                return null;

            if (sourceName == "lilToon")
                return ResolveRuntimeLilToonCutoutShader();

            if (!sourceName.Contains("lilToon"))
                return ResolveRuntimeLilToonCutoutShader();

            if (sourceName.Contains("Cutout"))
                return sourceShader;

            string cutoutName = sourceName;
            cutoutName = cutoutName.Replace("TwoPassTransparent", "Cutout");
            cutoutName = cutoutName.Replace("OnePassTransparent", "Cutout");
            cutoutName = cutoutName.Replace("Transparent", "Cutout");

            if (cutoutName != sourceName)
            {
                Shader convertedCutoutShader = Shader.Find(cutoutName);
                if (convertedCutoutShader != null)
                    return convertedCutoutShader;
            }

            if (sourceName.EndsWith("Outline"))
            {
                Shader outlineCutoutShader = Shader.Find(sourceName.Replace("Outline", "CutoutOutline"));
                if (outlineCutoutShader != null)
                    return outlineCutoutShader;

                outlineCutoutShader = Shader.Find(LilToonCutoutOutlineShaderName);
                if (outlineCutoutShader != null)
                    return outlineCutoutShader;
            }

            Shader namedCutoutShader = Shader.Find($"{sourceName}Cutout");
            if (namedCutoutShader != null)
                return namedCutoutShader;

            return ResolveRuntimeLilToonCutoutShader();
        }

        private static Shader ResolveRuntimeLilToonCutoutShader()
        {
            if (_lilToonCutoutShader != null)
                return _lilToonCutoutShader;

            _lilToonCutoutShader = Shader.Find(LilToonCutoutShaderName);
            if (_lilToonCutoutShader != null)
                return _lilToonCutoutShader;

            var keepAliveMaterial = Resources.Load<Material>(LilToonDissolveKeepAliveResourcePath);
            if (keepAliveMaterial != null)
                _lilToonCutoutShader = keepAliveMaterial.shader;

            if (_lilToonCutoutShader == null && !_reportedMissingLilToonCutoutShader)
            {
                _reportedMissingLilToonCutoutShader = true;
                Debug.LogWarning($"[DissolveController] lilToon Cutout 셰이더를 찾을 수 없습니다. shader={LilToonCutoutShaderName}, resource={LilToonDissolveKeepAliveResourcePath}");
            }

            return _lilToonCutoutShader;
        }

        private void ApplyCutoutRenderState(Material material, float transparentMode)
        {
            if (material.HasProperty(TransparentModeID))
                material.SetFloat(TransparentModeID, transparentMode);

            if (material.HasProperty(CutoffID) && material.GetFloat(CutoffID) <= 0f)
                material.SetFloat(CutoffID, _fallbackCutoff);

            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)_cutoutRenderQueue;

            if (material.HasProperty(SrcBlendID))
                material.SetInt(SrcBlendID, (int)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty(DstBlendID))
                material.SetInt(DstBlendID, (int)UnityEngine.Rendering.BlendMode.Zero);
            if (material.HasProperty(AlphaToMaskID))
                material.SetInt(AlphaToMaskID, 1);
            if (material.HasProperty(ZWriteID))
                material.SetInt(ZWriteID, 1);
        }

        private bool HasFallbackDissolveSlots()
        {
            foreach (var info in _rendererInfos)
            {
                if (info.slots == null) continue;
                foreach (var slot in info.slots)
                {
                    if (slot.originalMaterial != null && !slot.isParticleMaterial && !slot.supportsLilToonDissolve)
                        return true;
                }
            }

            return false;
        }

        private bool HasLilToonDissolveSlots()
        {
            foreach (var info in _rendererInfos)
            {
                if (info.slots == null) continue;
                foreach (var slot in info.slots)
                {
                    if (slot.originalMaterial != null && !slot.isParticleMaterial && slot.supportsLilToonDissolve)
                        return true;
                }
            }

            return false;
        }

        private bool IsLilToonDissolveMaterial(Material material)
        {
            if (!_useLilToonDissolve || material == null)
                return false;

            foreach (var current in EnumerateMaterialAndParents(material))
            {
                if (current != null && current.HasProperty(LilDissolveParamsID))
                    return true;
            }

            return false;
        }

        private Texture GetTextureIfExists(Material material, int propertyId)
        {
            foreach (var current in EnumerateMaterialAndParents(material))
            {
                if (current == null || !current.HasProperty(propertyId))
                    continue;

                var texture = current.GetTexture(propertyId);
                if (texture != null)
                    return texture;
            }

            return null;
        }

        private static IEnumerable<Material> EnumerateMaterialAndParents(Material material)
        {
            var visited = new HashSet<Material>();
            var current = material;
            while (current != null && visited.Add(current))
            {
                yield return current;
                current = GetMaterialParent(current);
            }
        }

        private static Material GetMaterialParent(Material material)
        {
            if (material == null || MaterialParentProperty == null || !MaterialParentProperty.CanRead)
                return null;

            return MaterialParentProperty.GetValue(material) as Material;
        }

        private static void DetachMaterialVariant(Material material)
        {
            if (material == null || MaterialParentProperty == null || !MaterialParentProperty.CanWrite)
                return;

            if (GetMaterialParent(material) != null)
                MaterialParentProperty.SetValue(material, null);
        }
        
    }
}
