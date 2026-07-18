using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Components
{
    /// <summary>
    /// 카메라가 액터의 lilToon 렌더러에 가까워질 때 Cutout 디더로 가시성을 낮춘다.
    /// 카메라가 액터 시각 중심에 실제로 진입했을 때만 lilToon 렌더러 드로우를 중단한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorCameraProximityDither : MonoBehaviour
    {
        private sealed class RendererInfo
        {
            public Renderer Renderer;
            public Material[] OriginalMaterials;
            public Material[] RuntimeMaterials;
            public bool OriginalForceRenderingOff;
        }

        private sealed class RuntimeMaterialInfo
        {
            public Material Material;
            public float BaseAlphaMaskScale;
            public float BaseAlphaMaskValue;
        }

        private const string LilToonCutoutShaderName = "Hidden/lilToonCutout";
        private const string LilToonCutoutOutlineShaderName = "Hidden/lilToonCutoutOutline";
        private const string KeepAliveResourcePath = "Rendering/LilToonDissolveKeepAlive";
        private const string DitherKeyword = "ETC1_EXTERNAL_ALPHA";
        private const string AlphaMaskKeyword = "_COLOROVERLAY_ON";

        private static readonly int TransparentModeID = Shader.PropertyToID("_TransparentMode");
        private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
        private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int AlphaToMaskID = Shader.PropertyToID("_AlphaToMask");
        private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");
        private static readonly int UseDitherID = Shader.PropertyToID("_UseDither");
        private static readonly int DitherTexID = Shader.PropertyToID("_DitherTex");
        private static readonly int DitherMaxValueID = Shader.PropertyToID("_DitherMaxValue");
        private static readonly int AlphaMaskModeID = Shader.PropertyToID("_AlphaMaskMode");
        private static readonly int AlphaMaskScaleID = Shader.PropertyToID("_AlphaMaskScale");
        private static readonly int AlphaMaskValueID = Shader.PropertyToID("_AlphaMaskValue");
        private static readonly PropertyInfo MaterialParentProperty =
            typeof(Material).GetProperty(
                "parent",
                BindingFlags.Instance | BindingFlags.Public);

        private static Shader _baseCutoutShader;
        private static Texture2D _ditherTexture;

        [Header("카메라 근접 디더")]
        [SerializeField, Min(0f)] private float _insideHideDistance = 0.08f;
        [SerializeField, Min(0.01f)] private float _maximumFadeDistance = 0.3f;
        [SerializeField, Min(0.02f)] private float _fadeStartDistance = 0.85f;
        [Tooltip("근접 상태의 최대 투명도. 화면 공간 디더 노출을 줄이기 위해 기본값은 65%, 상한은 85%로 제한한다.")]
        [SerializeField, Range(0f, 0.85f)] private float _maximumTransparency = 0.65f;
        [SerializeField, Min(0f)] private float _fadeSpeed = 8f;

        private readonly List<RendererInfo> _rendererInfos = new();
        private readonly List<RuntimeMaterialInfo> _runtimeMaterials = new();
        private Camera _camera;
        private float _visibility = 1f;
        private bool _isCameraInside;
        private bool _runtimePrepared;
        private Coroutine _warmupCoroutine;

        private void Awake()
        {
            RefreshRenderers();
        }

        private void OnEnable()
        {
            ResolveCamera();
            if (_rendererInfos.Count > 0 && !_runtimePrepared)
                PrepareRuntimeMaterials();
        }

        private void LateUpdate()
        {
            if (_rendererInfos.Count == 0)
                return;

            if (_camera == null || !_camera.isActiveAndEnabled)
                ResolveCamera();

            if (_camera == null)
            {
                ApplyVisibility(1f, false);
                return;
            }

            Vector3 cameraPosition = _camera.transform.position;
            if (!TryGetRendererDistance(
                    cameraPosition,
                    out float cameraDistance,
                    out Vector3 visualCenter))
            {
                ApplyVisibility(1f, false);
                return;
            }

            if (Vector3.Distance(cameraPosition, visualCenter) <= _insideHideDistance)
            {
                EnsureRuntimeMaterials();
                _visibility = 0f;
                ApplyVisibility(0f, true);
                return;
            }

            float fadeStartDistance =
                Mathf.Max(_fadeStartDistance, _maximumFadeDistance + 0.01f);
            float normalized = Mathf.InverseLerp(
                _maximumFadeDistance,
                fadeStartDistance,
                cameraDistance);
            float smoothDistance = normalized * normalized * (3f - 2f * normalized);
            // 페이드 구간 초입에서는 디더 점이 갑자기 많이 드러나지 않도록 감쇠량을
            // 한 번 더 제곱한다. 최대 근접 구간에서는 설정한 최대 투명도에 정확히 도달한다.
            float fadeAmount = 1f - smoothDistance;
            fadeAmount *= fadeAmount;
            float maximumTransparency = Mathf.Clamp(_maximumTransparency, 0f, 0.85f);
            float targetVisibility = 1f - maximumTransparency * fadeAmount;
            if (targetVisibility < 0.999f)
                EnsureRuntimeMaterials();

            _visibility = _fadeSpeed <= 0f
                ? targetVisibility
                : Mathf.MoveTowards(_visibility, targetVisibility, _fadeSpeed * Time.unscaledDeltaTime);

            ApplyVisibility(_visibility, false);
            if (targetVisibility >= 0.999f && _visibility >= 0.999f && _runtimePrepared)
                RestoreOriginalMaterials();
        }

        private bool TryGetRendererDistance(
            Vector3 cameraPosition,
            out float cameraDistance,
            out Vector3 visualCenter)
        {
            float nearestDistanceSqr = float.PositiveInfinity;
            Bounds combinedBounds = default;
            bool hasBounds = false;
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer == null || !info.Renderer.enabled ||
                    !info.Renderer.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds = info.Renderer.bounds;
                nearestDistanceSqr = Mathf.Min(
                    nearestDistanceSqr,
                    bounds.SqrDistance(cameraPosition));
                if (!hasBounds)
                {
                    combinedBounds = bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(bounds);
                }
            }

            if (!hasBounds || float.IsPositiveInfinity(nearestDistanceSqr))
            {
                cameraDistance = float.PositiveInfinity;
                visualCenter = transform.position;
                return false;
            }

            cameraDistance = Mathf.Sqrt(nearestDistanceSqr);
            visualCenter = combinedBounds.center;
            return true;
        }

        private void OnDisable()
        {
            StopWarmup();
            RestoreOriginalMaterials();
            ReleaseRuntimeMaterials();
            _runtimePrepared = false;
        }

        private void OnDestroy()
        {
            RestoreOriginalMaterials();
            ReleaseRuntimeMaterials();
        }

        /// <summary>
        /// 캐릭터 모델이나 장비 렌더러가 교체된 뒤 대상 목록을 다시 구성한다.
        /// </summary>
        public void RefreshRenderers()
        {
            StopWarmup();
            RestoreOriginalMaterials();
            ReleaseRuntimeMaterials();
            _rendererInfos.Clear();
            _visibility = 1f;
            _isCameraInside = false;
            _runtimePrepared = false;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer is ParticleSystemRenderer ||
                    renderer is UnityEngine.VFX.VFXRenderer)
                    continue;

                Material[] originals = renderer.sharedMaterials;
                if (!HasConvertibleSlot(originals))
                    continue;

                _rendererInfos.Add(new RendererInfo
                {
                    Renderer = renderer,
                    OriginalMaterials = originals,
                    OriginalForceRenderingOff = renderer.forceRenderingOff
                });
            }

            PrepareRuntimeMaterials();
        }

        private void PrepareRuntimeMaterials()
        {
            if (_runtimePrepared)
                return;

            _runtimePrepared = true;
            Texture2D ditherTexture = GetOrCreateDitherTexture();
            var preparedBySource = new Dictionary<Material, RuntimeMaterialInfo>();
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer == null ||
                    !IsSameMaterialSet(info.Renderer.sharedMaterials, info.OriginalMaterials))
                    continue;

                var runtimeSet = new Material[info.OriginalMaterials.Length];
                bool hasConvertedSlot = false;
                for (int i = 0; i < info.OriginalMaterials.Length; i++)
                {
                    Material source = info.OriginalMaterials[i];
                    runtimeSet[i] = source;
                    if (!CanConvertMaterial(source))
                        continue;

                    if (!preparedBySource.TryGetValue(source, out RuntimeMaterialInfo runtimeInfo))
                    {
                        runtimeInfo = CreateDitherMaterial(source, ditherTexture);
                        if (runtimeInfo == null)
                            continue;

                        preparedBySource.Add(source, runtimeInfo);
                        _runtimeMaterials.Add(runtimeInfo);
                    }

                    runtimeSet[i] = runtimeInfo.Material;
                    hasConvertedSlot = true;
                }

                if (!hasConvertedSlot)
                    continue;

                info.RuntimeMaterials = runtimeSet;
            }

            if (isActiveAndEnabled && _runtimeMaterials.Count > 0)
                _warmupCoroutine = StartCoroutine(WarmupMaterialsRoutine());
        }

        private void EnsureRuntimeMaterials()
        {
            PrepareRuntimeMaterials();
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer == null || info.RuntimeMaterials == null ||
                    !IsSameMaterialSet(info.Renderer.sharedMaterials, info.OriginalMaterials))
                    continue;

                info.Renderer.sharedMaterials = info.RuntimeMaterials;
            }
        }

        private void ApplyVisibility(float visibility, bool cameraInside)
        {
            visibility = Mathf.Clamp01(visibility);
            if (!Mathf.Approximately(_visibility, visibility))
                _visibility = visibility;

            foreach (RuntimeMaterialInfo runtimeInfo in _runtimeMaterials)
            {
                if (runtimeInfo?.Material == null)
                    continue;

                runtimeInfo.Material.SetFloat(
                    AlphaMaskScaleID,
                    runtimeInfo.BaseAlphaMaskScale * visibility);
                runtimeInfo.Material.SetFloat(
                    AlphaMaskValueID,
                    runtimeInfo.BaseAlphaMaskValue * visibility);
            }

            if (_isCameraInside == cameraInside)
                return;

            _isCameraInside = cameraInside;
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer != null)
                    info.Renderer.forceRenderingOff = info.OriginalForceRenderingOff || cameraInside;
            }
        }

        private void ResolveCamera()
        {
            _camera = Camera.main;
        }

        private static bool HasConvertibleSlot(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
                return false;

            foreach (Material material in materials)
            {
                if (CanConvertMaterial(material))
                    return true;
            }

            return false;
        }

        private static bool CanConvertMaterial(Material material)
        {
            foreach (Material current in EnumerateMaterialAndParents(material))
            {
                Shader shader = current.shader;
                string shaderName = shader != null ? shader.name : string.Empty;
                if (shaderName.IndexOf("lilToon", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (current.HasProperty(UseDitherID) &&
                    current.HasProperty(AlphaMaskModeID) &&
                    current.HasProperty(AlphaMaskScaleID) &&
                    current.HasProperty(AlphaMaskValueID))
                {
                    int alphaMaskMode =
                        Mathf.RoundToInt(current.GetFloat(AlphaMaskModeID));
                    return alphaMaskMode >= 0 && alphaMaskMode <= 2;
                }
            }

            return false;
        }

        private static RuntimeMaterialInfo CreateDitherMaterial(
            Material source,
            Texture ditherTexture)
        {
            Shader cutoutShader = ResolveCutoutShader(source);
            if (cutoutShader == null)
                return null;

            int alphaMaskMode = Mathf.RoundToInt(source.GetFloat(AlphaMaskModeID));
            float baseAlphaMaskScale = source.GetFloat(AlphaMaskScaleID);
            float baseAlphaMaskValue = source.GetFloat(AlphaMaskValueID);
            var material = new Material(source)
            {
                name = $"{source.name} (Camera Dither)",
                hideFlags = HideFlags.DontSave
            };
            DetachMaterialVariant(material);
            ChangeShaderClearingKeywords(material, cutoutShader);

            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetFloat(TransparentModeID, 1f);
            material.SetFloat(CutoffID, 0.5f);
            material.SetInt(SrcBlendID, (int)BlendMode.One);
            material.SetInt(DstBlendID, (int)BlendMode.Zero);
            // lilToon Dither는 최종 알파를 0/1로 양자화한다. 별도의 AlphaToCoverage를
            // 겹치지 않아 MSAA 설정에 따른 점 밀도 차이를 방지한다.
            material.SetInt(AlphaToMaskID, 0);
            material.SetInt(ZWriteID, 1);
            material.SetFloat(UseDitherID, 1f);
            material.SetTexture(DitherTexID, ditherTexture);
            material.SetFloat(DitherMaxValueID, 255f);

            // AlphaMask가 없던 재질은 흰색 Multiply 마스크로 전환한다.
            // visibility를 Scale/Value에 곱하면 lilToon Dither가 비교하는
            // 최종 fd.col.a를 모든 재질 슬롯에서 동일하게 제어할 수 있다.
            if (alphaMaskMode == 0)
            {
                material.SetFloat(AlphaMaskModeID, 2f);
                baseAlphaMaskScale = 0f;
                baseAlphaMaskValue = 1f;
            }

            material.SetFloat(AlphaMaskScaleID, baseAlphaMaskScale);
            material.SetFloat(AlphaMaskValueID, baseAlphaMaskValue);
            SetKeywordIfExists(material, DitherKeyword, true);
            SetKeywordIfExists(material, AlphaMaskKeyword, true);
            return new RuntimeMaterialInfo
            {
                Material = material,
                BaseAlphaMaskScale = baseAlphaMaskScale,
                BaseAlphaMaskValue = baseAlphaMaskValue
            };
        }

        private static Shader ResolveCutoutShader(Material sourceMaterial)
        {
            foreach (Material current in EnumerateMaterialAndParents(sourceMaterial))
            {
                Shader shader = ResolveCutoutShaderFromSource(current.shader);
                if (shader != null)
                    return shader;
            }

            return null;
        }

        private static Shader ResolveCutoutShaderFromSource(Shader sourceShader)
        {
            if (sourceShader == null)
                return null;

            string sourceName = sourceShader.name;
            if (string.Equals(sourceName, "lilToon", StringComparison.Ordinal))
                return ResolveBaseCutoutShader();

            if (sourceName.IndexOf("Cutout", StringComparison.OrdinalIgnoreCase) >= 0)
                return sourceShader;

            string cutoutName = sourceName
                .Replace("TwoPassTransparent", "Cutout")
                .Replace("OnePassTransparent", "Cutout")
                .Replace("Transparent", "Cutout");

            if (!string.Equals(cutoutName, sourceName, StringComparison.Ordinal))
            {
                Shader converted = Shader.Find(cutoutName);
                if (converted != null)
                    return converted;
            }

            if (sourceName.EndsWith("Outline", StringComparison.Ordinal))
            {
                Shader outline = Shader.Find(sourceName.Replace("Outline", "CutoutOutline"));
                if (outline != null)
                    return outline;

                outline = Shader.Find(LilToonCutoutOutlineShaderName);
                if (outline != null)
                    return outline;
            }

            return ResolveBaseCutoutShader();
        }

        private static Shader ResolveBaseCutoutShader()
        {
            if (_baseCutoutShader == null)
                _baseCutoutShader = Shader.Find(LilToonCutoutShaderName);

            if (_baseCutoutShader == null)
            {
                Material keepAlive = Resources.Load<Material>(KeepAliveResourcePath);
                if (keepAlive != null)
                    _baseCutoutShader = keepAlive.shader;
            }

            return _baseCutoutShader;
        }

        private static void SetKeywordIfExists(Material material, string keywordName, bool enabled)
        {
            foreach (LocalKeyword keyword in material.shader.keywordSpace.keywords)
            {
                if (keyword.name != keywordName)
                    continue;

                material.SetKeyword(keyword, enabled);
                return;
            }
        }

        private static void ChangeShaderClearingKeywords(
            Material material,
            Shader shader)
        {
            if (material.shader == shader)
                return;

            // LocalKeyword는 생성된 셰이더에 귀속된다. 귀속 셰이더를 바꾼 뒤 제거하면
            // Unity 6 렌더 스레드에서 IncompatibleKeywordSpace 오류가 발생할 수 있으므로
            // 원래 셰이더가 연결된 상태에서 먼저 모두 해제한다.
            LocalKeyword[] sourceKeywords = material.enabledKeywords;
            foreach (LocalKeyword keyword in sourceKeywords)
                material.SetKeyword(keyword, false);

            material.shader = shader;
            material.shaderKeywords = Array.Empty<string>();
        }

        private static Texture2D GetOrCreateDitherTexture()
        {
            if (_ditherTexture != null)
                return _ditherTexture;

            const int textureSize = 32;
            byte[] pixels = GenerateInterleavedGradientThresholds(textureSize);

            _ditherTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.R8,
                false,
                true)
            {
                name = "UPlayGround Camera Dither IGN 32x32",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };
            _ditherTexture.SetPixelData(pixels, 0);
            _ditherTexture.Apply(false, true);
            return _ditherTexture;
        }

        private static byte[] GenerateInterleavedGradientThresholds(int size)
        {
            int pixelCount = size * size;
            var noise = new float[pixelCount];
            var sortedIndices = new int[pixelCount];

            // Interleaved Gradient Noise는 화면 픽셀에서 국소 분포가 고르게 나타나는
            // 저비용 순서형 노이즈다. Bayer의 규칙적인 격자와 백색 노이즈의 점 뭉침을
            // 모두 줄이면서 카메라가 정지했을 때 패턴도 시간적으로 안정적이다.
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float gradient = 0.06711056f * x + 0.00583715f * y;
                    noise[index] = Mathf.Repeat(
                        52.9829189f * Mathf.Repeat(gradient, 1f),
                        1f);
                    sortedIndices[index] = index;
                }
            }

            Array.Sort(
                sortedIndices,
                (left, right) =>
                    noise[left].CompareTo(noise[right]));

            // lilToon은 0~255 임계값을 직접 비교하므로 순위화하여 각 투명도 단계의
            // 화면 점유율이 선형으로 변하도록 보장한다.
            var thresholds = new byte[pixelCount];
            for (int rank = 0; rank < pixelCount; rank++)
            {
                int threshold = rank * 256 / pixelCount;
                thresholds[sortedIndices[rank]] = (byte)Mathf.Min(threshold, 255);
            }

            return thresholds;
        }

        private void RestoreOriginalMaterials()
        {
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer == null)
                    continue;

                info.Renderer.forceRenderingOff = info.OriginalForceRenderingOff;
                Material[] current = info.Renderer.sharedMaterials;
                if (IsSameMaterialSet(current, info.RuntimeMaterials))
                    info.Renderer.sharedMaterials = info.OriginalMaterials;
            }
        }

        private void ReleaseRuntimeMaterials()
        {
            foreach (RuntimeMaterialInfo runtimeInfo in _runtimeMaterials)
            {
                if (runtimeInfo?.Material != null)
                    Destroy(runtimeInfo.Material);
            }

            _runtimeMaterials.Clear();
            foreach (RendererInfo info in _rendererInfos)
            {
                info.RuntimeMaterials = null;
            }
        }

        private IEnumerator WarmupMaterialsRoutine()
        {
            // 근접한 순간 모든 Cutout 셰이더 변형이 한꺼번에 최초 준비되면 큰 프레임 정지가
            // 발생할 수 있다. 모델 초기화 직후 한 재질씩 GPU 패스를 준비해 비용을 분산한다.
            foreach (RuntimeMaterialInfo runtimeInfo in _runtimeMaterials)
            {
                if (runtimeInfo?.Material != null)
                    runtimeInfo.Material.SetPass(0);

                yield return null;
            }

            _warmupCoroutine = null;
        }

        private void StopWarmup()
        {
            if (_warmupCoroutine == null)
                return;

            StopCoroutine(_warmupCoroutine);
            _warmupCoroutine = null;
        }

        private static IEnumerable<Material> EnumerateMaterialAndParents(Material material)
        {
            var visited = new HashSet<Material>();
            Material current = material;
            while (current != null && visited.Add(current))
            {
                yield return current;
                current = GetMaterialParent(current);
            }
        }

        private static Material GetMaterialParent(Material material)
        {
            if (material == null || MaterialParentProperty == null ||
                !MaterialParentProperty.CanRead)
                return null;

            return MaterialParentProperty.GetValue(material) as Material;
        }

        private static void DetachMaterialVariant(Material material)
        {
            if (material == null || MaterialParentProperty == null ||
                !MaterialParentProperty.CanWrite)
                return;

            if (GetMaterialParent(material) != null)
                MaterialParentProperty.SetValue(material, null);
        }

        private static bool IsSameMaterialSet(Material[] left, Material[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }
    }
}
