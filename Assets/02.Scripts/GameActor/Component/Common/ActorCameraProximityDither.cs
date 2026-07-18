using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Components
{
    /// <summary>
    /// 카메라가 액터의 lilToon 렌더러에 가까워질 때 Cutout 디더로 가시성을 낮춘다.
    /// 카메라가 렌더러 Bounds 안에 들어오면 해당 액터의 lilToon 렌더러 드로우를 중단한다.
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
        }

        private const string LilToonCutoutShaderName = "Hidden/lilToonCutout";
        private const string LilToonCutoutOutlineShaderName = "Hidden/lilToonCutoutOutline";
        private const string KeepAliveResourcePath = "Rendering/LilToonDissolveKeepAlive";
        private const string DitherKeyword = "ETC1_EXTERNAL_ALPHA";

        private static readonly int TransparentModeID = Shader.PropertyToID("_TransparentMode");
        private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
        private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int AlphaToMaskID = Shader.PropertyToID("_AlphaToMask");
        private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");
        private static readonly int UseDitherID = Shader.PropertyToID("_UseDither");
        private static readonly int DitherTexID = Shader.PropertyToID("_DitherTex");
        private static readonly int DitherMaxValueID = Shader.PropertyToID("_DitherMaxValue");
        private static readonly int DistanceFadeID = Shader.PropertyToID("_DistanceFade");
        private static readonly int DistanceFadeColorID = Shader.PropertyToID("_DistanceFadeColor");
        private static readonly int DistanceFadeModeID = Shader.PropertyToID("_DistanceFadeMode");
        private static readonly PropertyInfo MaterialParentProperty =
            typeof(Material).GetProperty(
                "parent",
                BindingFlags.Instance | BindingFlags.Public);

        private static Shader _baseCutoutShader;
        private static Texture2D _ditherTexture;

        [Header("카메라 근접 디더")]
        [SerializeField, Min(0f)] private float _hiddenDistance = 0.05f;
        [SerializeField, Min(0.01f)] private float _visibleDistance = 0.85f;
        [SerializeField, Min(0f)] private float _fadeSpeed = 8f;

        private readonly List<RendererInfo> _rendererInfos = new();
        private readonly List<RuntimeMaterialInfo> _runtimeMaterials = new();
        private Camera _camera;
        private float _visibility = 1f;
        private bool _isCameraInside;
        private bool _runtimePrepared;

        private void Awake()
        {
            RefreshRenderers();
        }

        private void OnEnable()
        {
            ResolveCamera();
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
            float nearestDistanceSqr = float.PositiveInfinity;
            bool isInside = false;

            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer == null || !info.Renderer.enabled ||
                    !info.Renderer.gameObject.activeInHierarchy)
                    continue;

                Bounds bounds = info.Renderer.bounds;
                isInside |= bounds.Contains(cameraPosition);
                nearestDistanceSqr = Mathf.Min(nearestDistanceSqr, bounds.SqrDistance(cameraPosition));
            }

            if (float.IsPositiveInfinity(nearestDistanceSqr))
            {
                ApplyVisibility(1f, false);
                return;
            }

            if (isInside)
            {
                EnsureRuntimeMaterials();
                _visibility = 0f;
                ApplyVisibility(0f, true);
                return;
            }

            float distance = Mathf.Sqrt(nearestDistanceSqr);
            float range = Mathf.Max(_visibleDistance, _hiddenDistance + 0.01f);
            float normalized = Mathf.InverseLerp(_hiddenDistance, range, distance);
            float targetVisibility = normalized * normalized * (3f - 2f * normalized);
            if (targetVisibility < 0.999f)
                EnsureRuntimeMaterials();

            _visibility = _fadeSpeed <= 0f
                ? targetVisibility
                : Mathf.MoveTowards(_visibility, targetVisibility, _fadeSpeed * Time.unscaledDeltaTime);

            ApplyVisibility(_visibility, false);
            if (targetVisibility >= 0.999f && _visibility >= 0.999f && _runtimePrepared)
            {
                RestoreOriginalMaterials();
                ReleaseRuntimeMaterials();
                _runtimePrepared = false;
            }
        }

        private void OnDisable()
        {
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
        }

        private void EnsureRuntimeMaterials()
        {
            if (_runtimePrepared)
                return;

            _runtimePrepared = true;
            Texture2D ditherTexture = GetOrCreateDitherTexture();
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

                    RuntimeMaterialInfo runtimeInfo = CreateDitherMaterial(source, ditherTexture);
                    if (runtimeInfo == null)
                        continue;

                    runtimeSet[i] = runtimeInfo.Material;
                    _runtimeMaterials.Add(runtimeInfo);
                    hasConvertedSlot = true;
                }

                if (!hasConvertedSlot)
                    continue;

                info.RuntimeMaterials = runtimeSet;
                info.Renderer.sharedMaterials = runtimeSet;
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

                Color fadeColor = runtimeInfo.Material.GetColor(DistanceFadeColorID);
                fadeColor.a = visibility;
                runtimeInfo.Material.SetColor(DistanceFadeColorID, fadeColor);
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
                if (shaderName.IndexOf("lilToon", StringComparison.OrdinalIgnoreCase) < 0 ||
                    shaderName.IndexOf("Lite", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (current.HasProperty(UseDitherID) &&
                    current.HasProperty(DistanceFadeID) &&
                    current.HasProperty(DistanceFadeColorID) &&
                    current.HasProperty(DistanceFadeModeID))
                    return true;
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

            var material = new Material(source)
            {
                name = $"{source.name} (Camera Dither)",
                hideFlags = HideFlags.DontSave
            };
            DetachMaterialVariant(material);
            ChangeShaderPreservingCompatibleKeywords(material, cutoutShader);

            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetFloat(TransparentModeID, 1f);
            material.SetFloat(CutoffID, 0.5f);
            material.SetInt(SrcBlendID, (int)BlendMode.One);
            material.SetInt(DstBlendID, (int)BlendMode.Zero);
            material.SetInt(AlphaToMaskID, 1);
            material.SetInt(ZWriteID, 1);
            material.SetFloat(UseDitherID, 1f);
            material.SetTexture(DitherTexID, ditherTexture);
            material.SetFloat(DitherMaxValueID, 63f);

            // Distance Fade를 항상 100% 적용되는 최종 알파 배율로 사용한다.
            // lilToon 내부 순서는 Main/AlphaMask -> Distance Fade alpha -> Dither이므로
            // 투명 머리카락과 기존 알파 마스크를 훼손하지 않고 전체 가시도를 제어할 수 있다.
            material.SetVector(DistanceFadeID, new Vector4(-1f, 0f, 1f, 0f));
            material.SetInt(DistanceFadeModeID, 0);
            Color fadeColor = material.GetColor(DistanceFadeColorID);
            fadeColor.a = 1f;
            material.SetColor(DistanceFadeColorID, fadeColor);
            SetKeywordIfExists(material, DitherKeyword, true);
            return new RuntimeMaterialInfo
            {
                Material = material
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

        private static void ChangeShaderPreservingCompatibleKeywords(
            Material material,
            Shader shader)
        {
            if (material.shader == shader)
                return;

            string[] sourceKeywords = material.shaderKeywords;
            material.shader = shader;
            material.shaderKeywords = Array.Empty<string>();
            foreach (string keywordName in sourceKeywords)
                SetKeywordIfExists(material, keywordName, true);
        }

        private static Texture2D GetOrCreateDitherTexture()
        {
            if (_ditherTexture != null)
                return _ditherTexture;

            // 8x8 Bayer 행렬. 화면 픽셀 단위 64단계 임계값으로 사용해
            // 4x4 패턴보다 반복과 단계 전환이 덜 눈에 띄게 한다.
            byte[] pixels =
            {
                 0, 32,  8, 40,  2, 34, 10, 42,
                48, 16, 56, 24, 50, 18, 58, 26,
                12, 44,  4, 36, 14, 46,  6, 38,
                60, 28, 52, 20, 62, 30, 54, 22,
                 3, 35, 11, 43,  1, 33,  9, 41,
                51, 19, 59, 27, 49, 17, 57, 25,
                15, 47,  7, 39, 13, 45,  5, 37,
                63, 31, 55, 23, 61, 29, 53, 21
            };

            _ditherTexture = new Texture2D(8, 8, TextureFormat.R8, false, true)
            {
                name = "UPlayGround Camera Dither 8x8",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };
            _ditherTexture.SetPixelData(pixels, 0);
            _ditherTexture.Apply(false, true);
            return _ditherTexture;
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
