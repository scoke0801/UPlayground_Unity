using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 현재 포즈를 BakeMesh로 캡처해 알파 페이드 잔상을 만든다.
    /// </summary>
    [Serializable]
    [MotionEventMeta("Afterimage", Category = "VFX / SFX", CategoryOrder = 10,
        Description = "현재 모델 포즈를 복제해 알파 잔상으로 남깁니다.",
        Aliases = new[] { "ghost", "after image", "alpha", "잔상", "알파" })]
    public class AfterimageEvent : MotionEventBase
    {
        [Tooltip("복제할 자식 오브젝트 이름. 비워두면 활성 CharacterModelData 모델을 사용한다.")]
        public string targetObjectName;

        [Tooltip("잔상이 유지되는 동안의 기본 알파.")]
        [Range(0f, 1f)]
        public float alpha = 0.45f;

        [Tooltip("잔상 생성 간격(A초). 0 이하면 한 번에 생성한다.")]
        [Min(0f)]
        public float spawnInterval = 0.05f;

        [Tooltip("이 이벤트 구간 안에서 생성할 최대 잔상 수(B개).")]
        [Min(1)]
        public int spawnCount = 1;

        [Tooltip("이 이벤트 러너가 보관할 잔상 풀 크기. 부족하면 가장 오래된 잔상을 재사용한다.")]
        [Min(1)]
        public int poolSize = 8;

        [Tooltip("각 잔상이 이 시간(C초)만큼 유지된 뒤 페이드아웃한다.")]
        [Min(0f)]
        public float holdDuration = 0.2f;

        [Tooltip("잔상 색상 틴트. 알파도 최종 알파에 곱해진다.")]
        public Color tintColor = Color.white;

        [Tooltip("유지 시간이 끝난 뒤 잔상이 사라지는 시간. 0 이하면 즉시 제거한다.")]
        [Min(0f)]
        public float fadeOutDuration = 0.35f;

        [Tooltip("생성 위치 보정. 복제 대상의 로컬 축 기준으로 적용된다.")]
        public Vector3 offset;

        [Tooltip("생성 회전 보정.")]
        public Vector3 rotationOffset;

        private AfterimageEventRunner _runner;

        public override string GetDisplayName() => "Afterimage";

        public override string GetShortLabel()
        {
            string targetLabel = string.IsNullOrEmpty(targetObjectName) ? "Model" : targetObjectName;
            return $"Afterimage: {targetLabel} x{Mathf.Max(1, spawnCount)}";
        }

        public override void Execute(GameObject target)
        {
            if (target == null)
                return;

            Transform source = ResolveSource(target);
            if (source == null)
            {
                Debug.LogWarning($"[AfterimageEvent] 복제할 대상을 찾을 수 없습니다. target={target.name}, objectName={targetObjectName}");
                return;
            }

            _runner = target.GetOrAddComponent<AfterimageEventRunner>();
            _runner.Play(new AfterimageEventSettings
            {
                source = source,
                alpha = alpha,
                spawnInterval = spawnInterval,
                spawnCount = spawnCount,
                poolSize = poolSize,
                holdDuration = holdDuration,
                fadeOutDuration = fadeOutDuration,
                tintColor = tintColor,
                offset = offset,
                rotationOffset = rotationOffset
            });
        }

        public override void OnCompleteEvent(GameObject target)
        {
            _runner?.StopSpawning();
            _runner = null;
        }

        private Transform ResolveSource(GameObject target)
        {
            if (!string.IsNullOrEmpty(targetObjectName))
                return FindTransformByName(target.transform, targetObjectName);

            var modelData = target.GetComponentInChildren<CharacterModelData>(includeInactive: false);
            if (modelData != null)
                return modelData.transform;

            var actorAnimator = target.GetComponentInChildren<ActorAnimator>(includeInactive: false);
            if (actorAnimator != null)
                return actorAnimator.transform;

            return target.transform;
        }

        private static Transform FindTransformByName(Transform parent, string objectName)
        {
            if (parent == null || string.IsNullOrEmpty(objectName))
                return null;

            foreach (var child in parent.GetComponentsInChildren<Transform>(includeInactive: false))
            {
                if (child.name == objectName)
                    return child;
            }

            return null;
        }

        private struct AfterimageEventSettings
        {
            public Transform source;
            public float alpha;
            public float spawnInterval;
            public int spawnCount;
            public int poolSize;
            public float holdDuration;
            public float fadeOutDuration;
            public Color tintColor;
            public Vector3 offset;
            public Vector3 rotationOffset;
        }

        private sealed class AfterimageEventRunner : MonoBehaviour
        {
            private static readonly int ColorID = Shader.PropertyToID("_Color");
            private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
            private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
            private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
            private static readonly int SurfaceID = Shader.PropertyToID("_Surface");
            private static readonly int BlendID = Shader.PropertyToID("_Blend");
            private static readonly int CullID = Shader.PropertyToID("_Cull");
            private static readonly int TransparentModeID = Shader.PropertyToID("_TransparentMode");
            private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
            private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
            private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");
            private static readonly Dictionary<Material, Material> s_afterimageMaterialCache = new();
            private static Material s_invisibleMaterial;

            private readonly List<SourceRendererInfo> _sourceRenderers = new();
            private readonly List<AfterimageInstance> _pool = new();
            private Coroutine _spawnRoutine;
            private int _sequence;

            public void Play(AfterimageEventSettings settings)
            {
                StopSpawning();
                CacheSourceRenderers(settings.source);
                EnsurePool(Mathf.Max(1, settings.poolSize));
                _spawnRoutine = StartCoroutine(SpawnRoutine(settings));
            }

            public void StopSpawning()
            {
                if (_spawnRoutine == null)
                    return;

                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            private IEnumerator SpawnRoutine(AfterimageEventSettings settings)
            {
                int count = Mathf.Max(1, settings.spawnCount);
                float interval = Mathf.Max(0f, settings.spawnInterval);

                for (int i = 0; i < count; i++)
                {
                    if (settings.source == null)
                        break;

                    SpawnAfterimage(settings);

                    if (i >= count - 1)
                        break;

                    if (interval <= 0f)
                        continue;

                    float elapsed = 0f;
                    while (elapsed < interval)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }

                _spawnRoutine = null;
            }

            private void SpawnAfterimage(AfterimageEventSettings settings)
            {
                if (_sourceRenderers.Count == 0)
                    return;

                var instance = GetPooledInstance();
                if (instance == null)
                    return;

                Vector3 position = settings.source.position + settings.source.TransformDirection(settings.offset);
                Quaternion rotation = settings.source.rotation * Quaternion.Euler(settings.rotationOffset);
                int sequence = ++_sequence;

                instance.Capture(_sourceRenderers, position, rotation, settings.alpha, settings.tintColor, sequence);
                StartCoroutine(FadeAfterHoldRoutine(instance, sequence, settings.holdDuration, settings.fadeOutDuration));
            }

            private IEnumerator FadeAfterHoldRoutine(AfterimageInstance instance, int sequence, float holdDuration, float fadeOutDuration)
            {
                float elapsed = 0f;
                float safeHoldDuration = Mathf.Max(0f, holdDuration);
                while (elapsed < safeHoldDuration)
                {
                    if (instance == null || instance.Sequence != sequence)
                        yield break;

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (instance == null || instance.Sequence != sequence)
                    yield break;

                yield return instance.FadeOut(sequence, Mathf.Max(0f, fadeOutDuration));
            }

            // 주의: 각 렌더러의 source 루트 대비 상대 행렬(localToSource)을 Play 시점에 1회 캐싱한다.
            // 본(bone)은 BakeMesh가 매 스폰 포즈를 반영하지만, 렌더러 트랜스폼 자체가 애니메이션 중
            // source 루트 대비 이동하면 어긋난다(일반 리그에선 정적이라 무해).
            private void CacheSourceRenderers(Transform source)
            {
                _sourceRenderers.Clear();
                if (source == null)
                    return;

                foreach (var renderer in source.GetComponentsInChildren<Renderer>(includeInactive: false))
                {
                    if (renderer == null || renderer is ParticleSystemRenderer)
                        continue;

                    var materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                        continue;

                    if (!TryBuildAfterimageMaterials(materials, out var afterimageMaterials, out var colorPropertyId, out var baseColor))
                        continue;

                    if (renderer is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null)
                    {
                        _sourceRenderers.Add(new SourceRendererInfo(source, skinnedMeshRenderer, afterimageMaterials, colorPropertyId, baseColor));
                        continue;
                    }

                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                        _sourceRenderers.Add(new SourceRendererInfo(source, renderer, meshFilter, afterimageMaterials, colorPropertyId, baseColor));
                }
            }

            private void EnsurePool(int poolSize)
            {
                while (_pool.Count < poolSize)
                {
                    var instanceRoot = new GameObject($"AfterimagePool_{_pool.Count}");
                    instanceRoot.SetActive(false);
                    _pool.Add(new AfterimageInstance(instanceRoot));
                }
            }

            private AfterimageInstance GetPooledInstance()
            {
                AfterimageInstance oldest = null;
                foreach (var instance in _pool)
                {
                    if (!instance.IsActive)
                        return instance;

                    if (oldest == null || instance.Sequence < oldest.Sequence)
                        oldest = instance;
                }

                oldest?.Deactivate();
                return oldest;
            }

            private void OnDestroy()
            {
                StopSpawning();
                StopAllCoroutines();

                foreach (var instance in _pool)
                    instance.Dispose();

                _pool.Clear();
                _sourceRenderers.Clear();
            }

            private static bool TryBuildAfterimageMaterials(Material[] sourceMaterials, out Material[] afterimageMaterials, out int colorPropertyId, out Color baseColor)
            {
                afterimageMaterials = null;
                colorPropertyId = -1;
                baseColor = Color.white;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                    return false;

                var result = new Material[sourceMaterials.Length];
                bool hasVisibleSlot = false;
                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material source = sourceMaterials[i];
                    if (source == null)
                    {
                        afterimageMaterials = null;
                        return false;
                    }

                    if (IsTransparentSourceMaterial(source))
                    {
                        result[i] = GetInvisibleMaterial();
                        continue;
                    }

                    result[i] = GetAfterimageMaterial(source);
                    if (!hasVisibleSlot)
                        baseColor = ResolveSourceColor(source);

                    hasVisibleSlot = true;
                }

                if (!hasVisibleSlot)
                    return false;

                afterimageMaterials = result;
                colorPropertyId = ResolveColorPropertyId(result[0]);
                return true;
            }

            private static Material GetAfterimageMaterial(Material source)
            {
                if (source != null && s_afterimageMaterialCache.TryGetValue(source, out var cachedMaterial))
                    return cachedMaterial;

                var material = new Material(ResolveAfterimageShader())
                {
                    name = source != null ? $"{source.name}_Afterimage_Runtime" : "Afterimage_Unlit_Runtime",
                    hideFlags = HideFlags.DontSave
                };
                ApplyAfterimageRenderState(material);
                CopySourceVisualProperties(source, material);

                if (source != null)
                    s_afterimageMaterialCache[source] = material;

                return material;
            }

            private static Material GetInvisibleMaterial()
            {
                if (s_invisibleMaterial != null)
                    return s_invisibleMaterial;

                s_invisibleMaterial = new Material(ResolveAfterimageShader())
                {
                    name = "Afterimage_Invisible_Runtime",
                    hideFlags = HideFlags.DontSave
                };
                ApplyAfterimageRenderState(s_invisibleMaterial);
                SetMaterialColor(s_invisibleMaterial, Color.clear);
                return s_invisibleMaterial;
            }

            private static Shader ResolveAfterimageShader()
            {
                // Shader.Find는 빌드에 포함된(참조 머티리얼 or Always Included Shaders) 셰이더만 반환한다.
                // 네 후보가 모두 빌드에서 스트리핑되면 null → new Material(null)로 매젠타가 되므로 명시 로깅한다.
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Standard");
                if (shader == null)
                    Debug.LogError("[AfterimageEvent] 잔상용 셰이더를 찾을 수 없습니다. " +
                                   "'Universal Render Pipeline/Unlit'을 Project Settings > Graphics의 Always Included Shaders에 등록하세요.");
                return shader;
            }

            private static void ApplyAfterimageRenderState(Material material)
            {
                if (material == null)
                    return;

                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                if (material.HasProperty(SurfaceID))
                    material.SetFloat(SurfaceID, 1f);
                if (material.HasProperty(BlendID))
                    material.SetFloat(BlendID, 0f);
                if (material.HasProperty(TransparentModeID))
                    material.SetFloat(TransparentModeID, 2f);
                if (material.HasProperty(SrcBlendID))
                    material.SetInt(SrcBlendID, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (material.HasProperty(DstBlendID))
                    material.SetInt(DstBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty(ZWriteID))
                    material.SetInt(ZWriteID, 0);
                if (material.HasProperty(CullID))
                    material.SetInt(CullID, (int)UnityEngine.Rendering.CullMode.Back);

                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.DisableKeyword("_ALPHATEST_ON");
            }

            private static int ResolveColorPropertyId(Material material)
            {
                if (material == null)
                    return -1;

                if (material.HasProperty(BaseColorID))
                    return BaseColorID;

                if (material.HasProperty(ColorID))
                    return ColorID;

                return -1;
            }

            private static void SetMaterialColor(Material material, Color color)
            {
                if (material == null)
                    return;

                if (material.HasProperty(BaseColorID))
                    material.SetColor(BaseColorID, color);

                if (material.HasProperty(ColorID))
                    material.SetColor(ColorID, color);
            }

            private static void CopySourceVisualProperties(Material source, Material destination)
            {
                if (destination == null)
                    return;

                Texture texture = ResolveMainTexture(source);
                if (texture != null)
                {
                    if (destination.HasProperty(BaseMapID))
                        destination.SetTexture(BaseMapID, texture);
                    if (destination.HasProperty(MainTexID))
                        destination.SetTexture(MainTexID, texture);
                }

                Color sourceColor = ResolveSourceColor(source);
                sourceColor.a = 1f;
                SetMaterialColor(destination, sourceColor);
            }

            private static Texture ResolveMainTexture(Material material)
            {
                if (material == null)
                    return null;

                if (material.HasProperty(BaseMapID))
                    return material.GetTexture(BaseMapID);

                if (material.HasProperty(MainTexID))
                    return material.GetTexture(MainTexID);

                return material.mainTexture;
            }

            private static Color ResolveSourceColor(Material material)
            {
                if (material == null)
                    return Color.white;

                if (material.HasProperty(BaseColorID))
                    return material.GetColor(BaseColorID);

                if (material.HasProperty(ColorID))
                    return material.GetColor(ColorID);

                return Color.white;
            }

            private static bool IsTransparentSourceMaterial(Material material)
            {
                if (material == null)
                    return true;

                if (material.HasProperty(TransparentModeID) && material.GetFloat(TransparentModeID) >= 2f)
                    return true;

                string materialName = material.name;
                if (!string.IsNullOrEmpty(materialName) && materialName.Contains("Transparent"))
                    return true;

                string shaderName = material.shader != null ? material.shader.name : string.Empty;
                if (shaderName.Contains("Transparent") || shaderName.Contains("Trans"))
                    return true;

                return false;
            }

            private readonly struct SourceRendererInfo
            {
                public readonly Renderer renderer;
                public readonly SkinnedMeshRenderer skinnedRenderer;
                public readonly MeshFilter meshFilter;
                public readonly Material[] materials;
                public readonly Matrix4x4 localToSource;
                public readonly int colorPropertyId;
                public readonly Color baseColor;
                public readonly bool isSkinned;

                public SourceRendererInfo(Transform sourceRoot, SkinnedMeshRenderer renderer, Material[] afterimageMaterials, int colorPropertyId, Color baseColor)
                {
                    this.renderer = renderer;
                    skinnedRenderer = renderer;
                    meshFilter = null;
                    materials = afterimageMaterials;
                    localToSource = sourceRoot.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    this.colorPropertyId = colorPropertyId;
                    this.baseColor = baseColor;
                    isSkinned = true;
                }

                public SourceRendererInfo(Transform sourceRoot, Renderer renderer, MeshFilter meshFilter, Material[] afterimageMaterials, int colorPropertyId, Color baseColor)
                {
                    this.renderer = renderer;
                    skinnedRenderer = null;
                    this.meshFilter = meshFilter;
                    materials = afterimageMaterials;
                    localToSource = sourceRoot.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    this.colorPropertyId = colorPropertyId;
                    this.baseColor = baseColor;
                    isSkinned = false;
                }
            }

            private sealed class AfterimageInstance
            {
                private readonly GameObject _root;
                private readonly List<Part> _parts = new();
                private float _alpha;
                private Color _tintColor = Color.white;

                public bool IsActive { get; private set; }
                public int Sequence { get; private set; }

                public AfterimageInstance(GameObject root)
                {
                    _root = root;
                }

                public void Capture(
                    IReadOnlyList<SourceRendererInfo> sourceRenderers,
                    Vector3 position,
                    Quaternion rotation,
                    float alpha,
                    Color tintColor,
                    int sequence)
                {
                    EnsurePartCount(sourceRenderers.Count);

                    _root.transform.SetPositionAndRotation(position, rotation);
                    _root.SetActive(true);
                    _alpha = Mathf.Clamp01(alpha);
                    _tintColor = tintColor;
                    Sequence = sequence;
                    IsActive = true;

                    for (int i = 0; i < _parts.Count; i++)
                    {
                        bool enabled = i < sourceRenderers.Count;
                        _parts[i].SetActive(enabled);
                        if (!enabled)
                            continue;

                        _parts[i].Capture(sourceRenderers[i], _alpha, _tintColor);
                    }
                }

                public IEnumerator FadeOut(int sequence, float duration)
                {
                    float startAlpha = _alpha;
                    if (duration <= 0f)
                    {
                        SetAlpha(0f);
                        Deactivate();
                        yield break;
                    }

                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        if (Sequence != sequence)
                            yield break;

                        elapsed += Time.unscaledDeltaTime;
                        SetAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration)));
                        yield return null;
                    }

                    if (Sequence != sequence)
                        yield break;

                    SetAlpha(0f);
                    Deactivate();
                }

                public void Deactivate()
                {
                    IsActive = false;
                    _root.SetActive(false);
                }

                public void Dispose()
                {
                    foreach (var part in _parts)
                        part.Dispose();

                    _parts.Clear();
                    if (_root != null)
                        Destroy(_root);
                }

                private void EnsurePartCount(int count)
                {
                    while (_parts.Count < count)
                    {
                        var partRoot = new GameObject($"AfterimagePart_{_parts.Count}");
                        partRoot.transform.SetParent(_root.transform, false);
                        _parts.Add(new Part(partRoot));
                    }
                }

                private void SetAlpha(float alpha)
                {
                    _alpha = Mathf.Clamp01(alpha);
                    foreach (var part in _parts)
                    {
                        if (part.IsActive)
                            part.SetAlpha(_alpha, _tintColor);
                    }
                }
            }

            private sealed class Part
            {
                private readonly GameObject _root;
                private readonly MeshFilter _meshFilter;
                private readonly MeshRenderer _meshRenderer;
                private readonly MaterialPropertyBlock _propertyBlock = new();
                private Mesh _bakedMesh;
                private Material[] _materials;
                private int _colorPropertyId = -1;
                private Color _baseColor = Color.white;

                public bool IsActive { get; private set; }

                public Part(GameObject root)
                {
                    _root = root;
                    _meshFilter = root.AddComponent<MeshFilter>();
                    _meshRenderer = root.AddComponent<MeshRenderer>();
                }

                public void Capture(SourceRendererInfo source, float alpha, Color tintColor)
                {
                    if (source.renderer == null)
                    {
                        SetActive(false);
                        return;
                    }

                    Matrix4x4 matrix = source.localToSource;
                    _root.transform.localPosition = matrix.GetColumn(3);
                    _root.transform.localRotation = matrix.rotation;
                    _root.transform.localScale = matrix.lossyScale;

                    if (source.isSkinned)
                    {
                        _bakedMesh ??= new Mesh { name = "AfterimageBakedMesh" };
                        _bakedMesh.Clear();
                        source.skinnedRenderer.BakeMesh(_bakedMesh);
                        _meshFilter.sharedMesh = _bakedMesh;
                    }
                    else
                    {
                        _meshFilter.sharedMesh = source.meshFilter != null ? source.meshFilter.sharedMesh : null;
                    }

                    _meshRenderer.sharedMaterials = source.materials;
                    _materials = source.materials;
                    _colorPropertyId = source.colorPropertyId;
                    _baseColor = source.baseColor;
                    SetAlpha(alpha, tintColor);
                    SetActive(true);
                }

                public void SetAlpha(float alpha, Color tintColor)
                {
                    if (_colorPropertyId == -1 || _materials == null)
                        return;

                    float visibleAlpha = Mathf.Clamp01(alpha) * tintColor.a;
                    Color color = new Color(
                        _baseColor.r * tintColor.r,
                        _baseColor.g * tintColor.g,
                        _baseColor.b * tintColor.b,
                        visibleAlpha);

                    // 렌더러 전체 MPB는 모든 서브메시에 같은 색을 덮어써 invisible 슬롯(투명 원본)까지
                    // 보이게 만든다. 슬롯 단위 MPB로 적용하고 invisible 슬롯은 Color.clear로 고정한다.
                    for (int i = 0; i < _materials.Length; i++)
                    {
                        Color slotColor = ReferenceEquals(_materials[i], s_invisibleMaterial) ? Color.clear : color;
                        _meshRenderer.GetPropertyBlock(_propertyBlock, i);
                        _propertyBlock.SetColor(_colorPropertyId, slotColor);
                        _meshRenderer.SetPropertyBlock(_propertyBlock, i);
                    }
                }

                public void SetActive(bool active)
                {
                    IsActive = active;
                    _root.SetActive(active);
                }

                public void Dispose()
                {
                    if (_bakedMesh != null)
                        Destroy(_bakedMesh);
                }

            }
        }
    }
}
