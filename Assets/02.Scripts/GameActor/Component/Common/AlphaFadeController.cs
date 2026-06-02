using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 모델 렌더러의 머티리얼 알파를 조절해 잔상/페이드아웃 표현을 만든다.
    /// 원본 머티리얼을 변경하지 않도록 런타임 인스턴스를 생성해서 사용한다.
    /// </summary>
    public sealed class AlphaFadeController : MonoBehaviour
    {
        private struct RendererInfo
        {
            public Renderer renderer;
            public Material[] originalSharedMaterials;
            public MaterialSlotInfo[] slots;
        }

        private struct MaterialSlotInfo
        {
            public Material originalMaterial;
            public int colorPropertyId;
            public Color originalColor;
        }

        private struct RuntimeMaterialInfo
        {
            public Material material;
            public int colorPropertyId;
            public Color originalColor;
        }

        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int SurfaceID = Shader.PropertyToID("_Surface");
        private static readonly int BlendID = Shader.PropertyToID("_Blend");
        private static readonly int AlphaClipID = Shader.PropertyToID("_AlphaClip");
        private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
        private static readonly int TransparentModeID = Shader.PropertyToID("_TransparentMode");
        private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");
        private static readonly int AlphaToMaskID = Shader.PropertyToID("_AlphaToMask");

        [Header("Alpha Fade")]
        [SerializeField, Range(0f, 1f)] private float _initialAlpha = 0.45f;
        [SerializeField] private Color _tintColor = Color.white;
        [SerializeField] private UnityEngine.Rendering.RenderQueue _transparentRenderQueue = UnityEngine.Rendering.RenderQueue.Transparent;
        [SerializeField] private float _lilToonTransparentMode = 2f;

        private readonly List<RendererInfo> _rendererInfos = new();
        private readonly List<Material> _instancedMaterials = new();
        private readonly List<RuntimeMaterialInfo> _runtimeMaterialInfos = new();
        private readonly Dictionary<Renderer, Material[]> _preparedMaterialSets = new();

        private bool _isPrepared;
        private float _currentAlpha = 1f;

        private void Awake()
        {
            InitializeRendererData();
        }

        private void OnDestroy()
        {
            ReleaseRuntimeMaterials();
        }

        public void InitializeRendererData()
        {
            _rendererInfos.Clear();

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer)
                    continue;

                var sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                    continue;

                bool hasFadeSlot = false;
                var slots = new MaterialSlotInfo[sharedMaterials.Length];
                for (int i = 0; i < sharedMaterials.Length; i++)
                {
                    Material material = sharedMaterials[i];
                    int colorPropertyId = ResolveColorProperty(material);
                    if (material != null && colorPropertyId != -1)
                        hasFadeSlot = true;

                    slots[i] = new MaterialSlotInfo
                    {
                        originalMaterial = material,
                        colorPropertyId = colorPropertyId,
                        originalColor = colorPropertyId != -1 ? material.GetColor(colorPropertyId) : Color.white
                    };
                }

                if (!hasFadeSlot)
                    continue;

                _rendererInfos.Add(new RendererInfo
                {
                    renderer = renderer,
                    originalSharedMaterials = sharedMaterials,
                    slots = slots
                });
            }
        }

        public void RefreshRenderers()
        {
            StopAllCoroutines();
            RestoreOriginalMaterials();
            ReleaseRuntimeMaterials();
            _preparedMaterialSets.Clear();
            _isPrepared = false;
            InitializeRendererData();
        }

        public void WarmupAlphaMaterials(float initialAlpha, Color tintColor)
        {
            _initialAlpha = Mathf.Clamp01(initialAlpha);
            _tintColor = tintColor;
            PrepareAlphaMaterials(assignToRenderers: true);
            SetAlpha(_initialAlpha);
        }

        public void StartFadeOut(float duration, bool destroyOnComplete = true, System.Action onComplete = null)
        {
            if (_rendererInfos.Count == 0)
            {
                onComplete?.Invoke();
                if (destroyOnComplete) Destroy(gameObject);
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FadeOutRoutine(Mathf.Max(0f, duration), destroyOnComplete, onComplete));
        }

        public void SetAlpha(float alpha)
        {
            _currentAlpha = Mathf.Clamp01(alpha);

            if (!_isPrepared)
                PrepareAlphaMaterials(assignToRenderers: true);

            foreach (var info in _runtimeMaterialInfos)
            {
                if (info.material == null || info.colorPropertyId == -1)
                    continue;

                Color color = info.originalColor;
                color.r *= _tintColor.r;
                color.g *= _tintColor.g;
                color.b *= _tintColor.b;
                color.a *= _currentAlpha * _tintColor.a;
                info.material.SetColor(info.colorPropertyId, color);
            }
        }

        private IEnumerator FadeOutRoutine(float duration, bool destroyOnComplete, System.Action onComplete)
        {
            float startAlpha = _currentAlpha;
            if (duration <= 0f)
            {
                SetAlpha(0f);
                onComplete?.Invoke();
                if (destroyOnComplete) Destroy(gameObject);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetAlpha(0f);
            onComplete?.Invoke();
            if (destroyOnComplete) Destroy(gameObject);
        }

        private void PrepareAlphaMaterials(bool assignToRenderers)
        {
            ReleaseRuntimeMaterials();
            _runtimeMaterialInfos.Clear();
            _preparedMaterialSets.Clear();

            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null || info.slots == null)
                    continue;

                var materials = new Material[info.slots.Length];
                for (int i = 0; i < info.slots.Length; i++)
                {
                    var slot = info.slots[i];
                    if (slot.originalMaterial == null || slot.colorPropertyId == -1)
                    {
                        materials[i] = slot.originalMaterial;
                        continue;
                    }

                    var instance = new Material(slot.originalMaterial);
                    ConvertToTransparent(instance);
                    materials[i] = instance;
                    _instancedMaterials.Add(instance);
                    _runtimeMaterialInfos.Add(new RuntimeMaterialInfo
                    {
                        material = instance,
                        colorPropertyId = slot.colorPropertyId,
                        originalColor = slot.originalColor
                    });
                }

                _preparedMaterialSets[info.renderer] = materials;
                if (assignToRenderers)
                    info.renderer.materials = materials;
            }

            _isPrepared = true;
        }

        private void ConvertToTransparent(Material material)
        {
            if (material == null)
                return;

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)_transparentRenderQueue;

            if (material.HasProperty(SurfaceID))
                material.SetFloat(SurfaceID, 1f);
            if (material.HasProperty(BlendID))
                material.SetFloat(BlendID, 0f);
            if (material.HasProperty(AlphaClipID))
                material.SetFloat(AlphaClipID, 0f);
            if (material.HasProperty(CutoffID))
                material.SetFloat(CutoffID, 0f);
            if (material.HasProperty(TransparentModeID))
                material.SetFloat(TransparentModeID, _lilToonTransparentMode);
            if (material.HasProperty(SrcBlendID))
                material.SetInt(SrcBlendID, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty(DstBlendID))
                material.SetInt(DstBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty(ZWriteID))
                material.SetInt(ZWriteID, 0);
            if (material.HasProperty(AlphaToMaskID))
                material.SetInt(AlphaToMaskID, 0);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var info in _rendererInfos)
            {
                if (info.renderer == null || info.originalSharedMaterials == null)
                    continue;

                info.renderer.sharedMaterials = info.originalSharedMaterials;
            }
        }

        private void ReleaseRuntimeMaterials()
        {
            foreach (var material in _instancedMaterials)
            {
                if (material != null)
                    Destroy(material);
            }

            _instancedMaterials.Clear();
        }

        private static int ResolveColorProperty(Material material)
        {
            if (material == null)
                return -1;

            string shaderName = material.shader != null ? material.shader.name : string.Empty;
            if (shaderName.Contains("lilToon") && material.HasProperty(ColorID))
                return ColorID;

            if (material.HasProperty(BaseColorID))
                return BaseColorID;

            return material.HasProperty(ColorID) ? ColorID : -1;
        }
    }
}
