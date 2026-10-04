using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;
using UPlayGround.MovementController;

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
            public bool HasRuntimeMaterials;
        }

        private ActorPresentation _presentation;

        private sealed class RuntimeMaterialInfo
        {
            public Material Material;
            public float BaseAlphaMaskScale;
            public float BaseAlphaMaskValue;
        }
        private const string LilToonCutoutShaderName = "Hidden/lilToonCutout";
        private const string LilToonCutoutOutlineShaderName = "Hidden/lilToonCutoutOutline";
        private const string LilToonCutoutPassShaderName = "Hidden/ltspass_cutout";
        private const string KeepAliveResourcePath = "Rendering/LilToonDissolveKeepAlive";
        private const string MultiDitherKeepAliveResourcePath = "Rendering/LilToonMultiDitherKeepAlive";
        private const string DitherResourcePath = "Rendering/CameraDitherNoise";
        private const string DitherKeyword = "ETC1_EXTERNAL_ALPHA";
        private const string AlphaMaskKeyword = "_COLOROVERLAY_ON";
        private const string MultiCutoutKeyword = "UNITY_UI_ALPHACLIP";
        private const string MultiTransparentKeyword = "UNITY_UI_CLIP_RECT";

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
        private static Material _multiDitherTemplate;
        private static Shader _baseCutoutPassShader;
        private static Texture2D _ditherTexture;
        private static readonly Dictionary<int, Texture2D> ScaledDitherTextures = new();

        [Header("카메라 근접 디더")]
        [Tooltip("카메라가 KCC 캡슐 안쪽으로 들어온 뒤 렌더링을 중단할 깊이.")]
        [SerializeField, Min(0f)] private float _insideHideDistance = 0.03f;
        [Tooltip("카메라의 니어 클립면에서 몸 캡슐까지의 거리가 이 값 이하일 때 최대로 페이드한다.")]
        [SerializeField, Min(0.01f)] private float _maximumFadeDistance = 0.12f;
        [Tooltip("몸 캡슐에서 페이드를 시작하는 거리. 무기와 머리카락의 바운드는 제외한다.")]
        [SerializeField, Min(0.02f)] private float _fadeStartDistance = 0.65f;
        [Tooltip("근접 상태의 최대 투명도. 1이면 카메라가 몸에 닿기 전에 완전히 사라져 점무늬 잔상을 남기지 않는다.")]
        [SerializeField, Range(0f, 1f)] private float _maximumTransparency = 1f;
        [Tooltip("화면 픽셀 기준 디더 점 크기. Play Mode에서 변경하면 다음 평가 시점(최대 재검사 간격)에 반영된다.")]
        [SerializeField, Range(1, 4)] private int _ditherPixelScale = 1;
        [SerializeField, Min(0f)] private float _fadeSpeed = 8f;
        [Tooltip("완전히 보이는 동안 카메라 거리와 Renderer Bounds를 다시 검사할 간격(초)")]
        [SerializeField, Min(0.02f)] private float _fullyVisibleEvaluationInterval = 0.05f;

        private readonly List<RendererInfo> _rendererInfos = new();
        private readonly List<RuntimeMaterialInfo> _runtimeMaterials = new();
        private readonly List<Material> _sharedMaterialBuffer = new();
        private Camera _camera;
        private ICameraViewService _cameraViewService;
        private CapsuleCollider _actorCapsule;
        private ActorMovementController _movementController;
        private float _visibility = 1f;
        private float _fullyVisibleEvaluationTimer;
        private float _lastAppliedVisibility = float.NaN;
        private int _appliedDitherPixelScale;
        private bool _isCameraInside;
        private bool _isSuppressedByCameraMode;
        private bool _runtimePrepared;
        private Coroutine _warmupCoroutine;

        private void Awake()
        {
            _presentation = GetComponent<ActorPresentation>();
            RefreshRenderers();
        }

        private void OnEnable()
        {
            ResolveCamera();
            _fullyVisibleEvaluationTimer = Mathf.Max(0.02f, _fullyVisibleEvaluationInterval);
            _lastAppliedVisibility = float.NaN;
            if (_rendererInfos.Count > 0 && !_runtimePrepared)
                PrepareRuntimeMaterials();
        }

        private void LateUpdate()
        {
            UpdateCameraModeSuppression();
            if (_isSuppressedByCameraMode)
                return;

            if (_rendererInfos.Count == 0)
                return;

            if (_camera == null || !_camera.isActiveAndEnabled)
                ResolveCamera();

            if (_camera == null)
            {
                ApplyVisibility(1f, false);
                return;
            }

            // 대부분의 액터는 완전히 보이는 상태다. 이때 비싼 Renderer.bounds 결합은
            // 짧은 주기로만 수행하고, 실제 페이드 중에는 매 프레임 갱신해 부드러움을 유지한다.
            if (_visibility >= 0.999f && !_isCameraInside)
            {
                _fullyVisibleEvaluationTimer += Time.unscaledDeltaTime;
                float evaluationInterval = Mathf.Max(0.02f, _fullyVisibleEvaluationInterval);
                if (_fullyVisibleEvaluationTimer < evaluationInterval)
                    return;

                _fullyVisibleEvaluationTimer %= evaluationInterval;
            }
            else
            {
                _fullyVisibleEvaluationTimer = 0f;
            }

            UpdateDitherPixelScale();

            Vector3 cameraPosition = _camera.transform.position;
            if (!TryGetFadeDistance(
                    cameraPosition,
                    out float cameraDistance,
                    out Vector3 visualCenter))
            {
                ApplyVisibility(1f, false);
                return;
            }

            if (IsCameraInsideActor(cameraPosition, visualCenter))
            {
                EnsureRuntimeMaterials();
                _visibility = 0f;
                ApplyVisibility(0f, true);
                return;
            }

            float targetVisibility = EvaluateTargetVisibility(cameraDistance);
            if (targetVisibility < 0.999f)
                EnsureRuntimeMaterials();

            _visibility = _fadeSpeed <= 0f
                ? targetVisibility
                : Mathf.MoveTowards(_visibility, targetVisibility, _fadeSpeed * Time.unscaledDeltaTime);

            ApplyVisibility(_visibility, false);
            if (targetVisibility >= 0.999f && _visibility >= 0.999f && _runtimePrepared)
                RestoreOriginalMaterials();
        }

        private float EvaluateTargetVisibility(float cameraDistance)
        {
            float maximumFadeDistance = Mathf.Max(0f, _maximumFadeDistance);
            float fadeStartDistance = Mathf.Max(_fadeStartDistance, maximumFadeDistance + 0.01f);
            float normalized = Mathf.InverseLerp(maximumFadeDistance, fadeStartDistance, cameraDistance);
            float smoothDistance = normalized * normalized * (3f - 2f * normalized);
            return 1f - Mathf.Clamp01(_maximumTransparency) * (1f - smoothDistance);
        }

        private void UpdateCameraModeSuppression()
        {
            bool shouldSuppress = TryGetCameraMode(out CameraModeType cameraMode)
                                  && IsDialogueCameraMode(cameraMode);
            if (_isSuppressedByCameraMode == shouldSuppress)
                return;

            _isSuppressedByCameraMode = shouldSuppress;
            if (shouldSuppress)
            {
                ApplyVisibility(1f, false);
                RestoreOriginalMaterials();
                return;
            }

            _lastAppliedVisibility = float.NaN;
            _fullyVisibleEvaluationTimer = Mathf.Max(
                0.02f,
                _fullyVisibleEvaluationInterval);
            ResolveCamera();
        }

        private bool TryGetCameraMode(out CameraModeType cameraMode)
        {
            if (_cameraViewService is UnityEngine.Object cameraServiceObject
                && cameraServiceObject == null)
            {
                _cameraViewService = null;
            }

            if (_cameraViewService == null)
                Services.TryGet(out _cameraViewService);

            if (_cameraViewService == null)
            {
                cameraMode = CameraModeType.InGame;
                return false;
            }

            cameraMode = _cameraViewService.CurrentCameraMode;
            return true;
        }

        private static bool IsDialogueCameraMode(CameraModeType cameraMode)
        {
            // 대화 카메라의 근접 배치는 의도된 클로즈업이다. TPS 시야 확보용 효과를
            // 겹치면 피사체가 디더로 훼손되므로 절차/녹화 대화 모드 모두 제외한다.
            return cameraMode == CameraModeType.Dialogue
                   || cameraMode == CameraModeType.DialogueCameraReplay;
        }

        private bool TryGetFadeDistance(
            Vector3 cameraPosition,
            out float cameraDistance,
            out Vector3 visualCenter)
        {
            // 무기와 흔들리는 머리카락의 AABB로 전신을 흐리면 빈 공간에서도 페이드가 발생한다.
            // 내부 숨김과 같은 캡슐을 기준으로 삼아 접근/관통 구간을 연속적으로 연결한다.
            if (TryGetActorCapsule())
            {
                visualCenter = _actorCapsule.transform.TransformPoint(_actorCapsule.center);
                cameraDistance = GetCapsuleSignedDistance(_actorCapsule, cameraPosition);
                cameraDistance -= _camera != null ? _camera.nearClipPlane : 0f;
                return true;
            }

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

        private void ResolveActorCapsule()
        {
            _movementController = GetComponent<ActorMovementController>();
            _actorCapsule = _movementController?.Motor != null
                ? _movementController.Motor.Capsule
                : GetComponent<CapsuleCollider>();
        }

        private bool TryGetActorCapsule()
        {
            // KCC의 초기화가 Awake보다 늦어도 계층을 매 프레임 다시 탐색하지 않는다.
            if (_actorCapsule == null && _movementController?.Motor != null)
                _actorCapsule = _movementController.Motor.Capsule;
            return _actorCapsule != null && _actorCapsule.enabled &&
                   _actorCapsule.gameObject.activeInHierarchy;
        }

        private bool IsCameraInsideActor(
            Vector3 cameraPosition,
            Vector3 visualCenter)
        {
            if (!TryGetActorCapsule())
            {
                // KCC가 없는 예외 액터는 기존 중심 기반 판정을 유지한다.
                return Vector3.Distance(cameraPosition, visualCenter) <=
                       _insideHideDistance;
            }

            float signedDistance =
                GetCapsuleSignedDistance(_actorCapsule, cameraPosition);
            return signedDistance <= -_insideHideDistance;
        }

        private static float GetCapsuleSignedDistance(
            CapsuleCollider capsule,
            Vector3 worldPosition)
        {
            Transform capsuleTransform = capsule.transform;
            Vector3 scale = capsuleTransform.lossyScale;
            Vector3 axisLocal;
            float axisScale;
            float radiusScale;

            switch (capsule.direction)
            {
                case 0:
                    axisLocal = Vector3.right;
                    axisScale = Mathf.Abs(scale.x);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.y),
                        Mathf.Abs(scale.z));
                    break;
                case 2:
                    axisLocal = Vector3.forward;
                    axisScale = Mathf.Abs(scale.z);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y));
                    break;
                default:
                    axisLocal = Vector3.up;
                    axisScale = Mathf.Abs(scale.y);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.z));
                    break;
            }

            Vector3 axis =
                capsuleTransform.TransformDirection(axisLocal).normalized;
            Vector3 center = capsuleTransform.TransformPoint(capsule.center);
            float radius = capsule.radius * radiusScale;
            float height = Mathf.Max(
                capsule.height * axisScale,
                radius * 2f);
            float segmentHalfLength = height * 0.5f - radius;
            Vector3 start = center - axis * segmentHalfLength;
            Vector3 end = center + axis * segmentHalfLength;
            Vector3 closest = ClosestPointOnSegment(
                worldPosition,
                start,
                end);
            return Vector3.Distance(worldPosition, closest) - radius;
        }

        private static Vector3 ClosestPointOnSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSqr = segment.sqrMagnitude;
            if (lengthSqr <= Mathf.Epsilon)
                return start;

            float t = Mathf.Clamp01(
                Vector3.Dot(point - start, segment) / lengthSqr);
            return start + segment * t;
        }

        private void OnDisable()
        {
            StopWarmup();
            RestoreOriginalMaterials();
            ReleaseRuntimeMaterials();
            _isSuppressedByCameraMode = false;
            _runtimePrepared = false;
            _fullyVisibleEvaluationTimer = 0f;
            _lastAppliedVisibility = float.NaN;
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
            ResetRendererBindings();
            ResolveActorCapsule();

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer is ParticleSystemRenderer ||
                    renderer is UnityEngine.VFX.VFXRenderer)
                    continue;

                Material[] originals = renderer.sharedMaterials;
                if (!HasConvertibleSlot(originals))
                    continue;

                bool originalForceRenderingOff = renderer.forceRenderingOff;
                ActorPresentation presentation = ResolvePresentation();
                if (presentation != null
                    && presentation.IsHidden
                    && presentation.TryGetOriginalForceRenderingOff(
                        renderer,
                        out bool presentationOriginal))
                {
                    originalForceRenderingOff = presentationOriginal;
                }

                _rendererInfos.Add(new RendererInfo
                {
                    Renderer = renderer,
                    OriginalMaterials = originals,
                    OriginalForceRenderingOff = originalForceRenderingOff
                });
            }

            PrepareRuntimeMaterials();
        }

        /// <summary>
        /// 캐릭터 스왑처럼 하위 렌더러 계층과 장비 캐시가 갱신되기 전에 호출한다.
        /// 모든 디더용 임시 머티리얼을 즉시 원본으로 되돌리고 추적 목록을 비운다.
        /// </summary>
        public void RestoreOriginalMaterialsImmediately()
        {
            ResetRendererBindings();
        }

        private void ResetRendererBindings()
        {
            StopWarmup();
            RestoreOriginalMaterials();
            ReleaseRuntimeMaterials();
            _rendererInfos.Clear();
            _visibility = 1f;
            _isCameraInside = false;
            _runtimePrepared = false;
            // 렌더러 바인딩이 통째로 갈리므로 직전 적용값 캐시도 함께 무효화한다.
            // (OnEnable / 머티리얼 해제 경로와 동일한 처리)
            _lastAppliedVisibility = float.NaN;
            _fullyVisibleEvaluationTimer = 0f;
        }

        private void PrepareRuntimeMaterials()
        {
            if (_runtimePrepared)
                return;

            // 카메라 근접 디더는 런타임 전용이다. Prefab/Inspector 프리뷰 같은
            // 비재생 컨텍스트에서는 임시 머티리얼을 만들지 않는다.
            if (!Application.isPlaying)
                return;

            int pixelScale = Mathf.Clamp(_ditherPixelScale, 1, 4);
            Texture2D ditherTexture = GetDitherTexture(pixelScale);
            if (ditherTexture == null)
            {
                Debug.LogError(
                    "[ActorCameraProximityDither] " +
                    "CameraDitherNoise 텍스처를 불러오지 못했습니다.",
                    this);
                return;
            }

            _runtimePrepared = true;
            _appliedDitherPixelScale = pixelScale;

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
                if (info.Renderer == null || info.RuntimeMaterials == null || info.HasRuntimeMaterials ||
                    !HasMaterialSet(info.Renderer, info.OriginalMaterials))
                    continue;

                info.Renderer.sharedMaterials = info.RuntimeMaterials;
                info.HasRuntimeMaterials = true;
            }
        }

        private void ApplyVisibility(float visibility, bool cameraInside)
        {
            visibility = Mathf.Clamp01(visibility);
            bool visibilityChanged = float.IsNaN(_lastAppliedVisibility) ||
                                     !Mathf.Approximately(_lastAppliedVisibility, visibility);
            bool insideChanged = _isCameraInside != cameraInside;
            _visibility = visibility;

            if (!visibilityChanged && !insideChanged)
                return;

            if (visibilityChanged)
            {
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

                _lastAppliedVisibility = visibility;
            }

            if (!insideChanged)
                return;

            _isCameraInside = cameraInside;
            RefreshVisibilitySuppression();
        }

        public void RefreshVisibilitySuppression()
        {
            bool presentationHidden = ResolvePresentation()?.IsHidden == true;
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer != null)
                {
                    info.Renderer.forceRenderingOff =
                        info.OriginalForceRenderingOff
                        || _isCameraInside
                        || presentationHidden;
                }
            }
        }

        private ActorPresentation ResolvePresentation()
        {
            if (_presentation == null)
                _presentation = GetComponent<ActorPresentation>();
            return _presentation;
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
                if (!IsSupportedLilToonShader(shaderName))
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

        private static bool IsSupportedLilToonShader(string shaderName)
        {
            if (string.Equals(shaderName, "lilToon", StringComparison.Ordinal))
                return true;

            if (IsLilToonMultiShader(shaderName))
                return true;

            if (!shaderName.StartsWith(
                    "Hidden/lilToon",
                    StringComparison.Ordinal))
            {
                return false;
            }

            // 아래 변형은 일반 lilToon Cutout과 패스/프로퍼티 구성이 다르다.
            // 기본 Cutout으로 폴백하면 무기·특수 재질이 분홍색 또는 왜곡된 형태로
            // 출력될 수 있으므로 명시적으로 변환 대상에서 제외한다.
            return shaderName.IndexOf("Multi", StringComparison.OrdinalIgnoreCase) < 0 &&
                   shaderName.IndexOf("Lite", StringComparison.OrdinalIgnoreCase) < 0 &&
                   shaderName.IndexOf("Fur", StringComparison.OrdinalIgnoreCase) < 0 &&
                   shaderName.IndexOf("Gem", StringComparison.OrdinalIgnoreCase) < 0 &&
                   shaderName.IndexOf("Refraction", StringComparison.OrdinalIgnoreCase) < 0 &&
                   shaderName.IndexOf("Tessellation", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsLilToonMultiShader(string shaderName)
        {
            return string.Equals(
                       shaderName,
                       "_lil/lilToonMulti",
                       StringComparison.Ordinal) ||
                   string.Equals(
                       shaderName,
                       "Hidden/lilToonMultiOutline",
                       StringComparison.Ordinal);
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
            bool isMulti = IsLilToonMultiShader(cutoutShader.name);
            Material multiTemplate = isMulti ? ResolveMultiDitherTemplate() : null;

            Material material;
            if (multiTemplate != null)
            {
                // Multi는 Player 빌드에 실제 포함된 디더 keep-alive 머티리얼을 복제해
                // 디더 변형 키워드 조합을 보존하고 외형 프로퍼티만 소스에서 복사한다.
                // 소스 셰이더로 교체하면 Addressables 번들의 스트리핑된 변형으로
                // 폴백할 수 있어 템플릿 셰이더를 그대로 유지한다. MultiOutline 소스도
                // 디더 중에는 아웃라인 없이 템플릿 셰이더로 렌더링한다.
                material = new Material(multiTemplate)
                {
                    name = $"{source.name} (Camera Dither)",
                    hideFlags = HideFlags.DontSave
                };
                material.CopyMatchingPropertiesFromMaterial(source);
                DetachMaterialVariant(material);
            }
            else
            {
                // Hidden/lilToonCutout은 Hidden/ltspass_cutout의 패스를 UsePass로
                // 가져온다. Unity 6에서 래퍼 머티리얼을 복제하면 CreateWithMaterial의
                // 패스 해시 계산 중 두 셰이더의 LocalKeywordSpace가 섞여
                // IncompatibleKeywordSpace 어서션이 발생한다. 실제 패스 셰이더로
                // 직접 만들면 머티리얼과 패스의 키워드 공간이 일치한다.
                Shader passShader = ResolveBaseCutoutPassShader();
                if (passShader == null)
                    return null;

                material = new Material(passShader)
                {
                    name = $"{source.name} (Camera Dither)",
                    hideFlags = HideFlags.DontSave
                };
                material.CopyMatchingPropertiesFromMaterial(source);
            }

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

            // Opaque는 원래 텍스처/색의 알파를 무시한다. Cutout 전환 후 그 알파를
            // 되살리면 얼굴/머리 재질별 점 밀도가 달라지고 뒤쪽 면이 비쳐 보인다.
            if (IsOpaqueMaterial(source))
            {
                material.SetFloat(AlphaMaskModeID, 1f);
                baseAlphaMaskScale = 0f;
                baseAlphaMaskValue = 1f;
            }
            else if (alphaMaskMode == 0)
            {
                // 투명 재질은 원래 알파를 보존한 채 동일한 화면 패턴으로 감쇠한다.
                material.SetFloat(AlphaMaskModeID, 2f);
                baseAlphaMaskScale = 0f;
                baseAlphaMaskValue = 1f;
            }

            material.SetFloat(AlphaMaskScaleID, baseAlphaMaskScale);
            material.SetFloat(AlphaMaskValueID, baseAlphaMaskValue);
            SetKeywordIfExists(material, DitherKeyword, true);
            SetKeywordIfExists(material, AlphaMaskKeyword, true);
            if (isMulti)
            {
                // lilToonMulti의 렌더 모드는 프로퍼티만으로 바뀌지 않는다.
                // UNITY_UI_ALPHACLIP이 lil_replace_keywords.hlsl에서
                // LIL_RENDER 1(Cutout)로 치환되어야 디더 알파가 discard에 반영된다.
                SetKeywordIfExists(material, MultiTransparentKeyword, false);
                SetKeywordIfExists(material, MultiCutoutKeyword, true);
            }

            return new RuntimeMaterialInfo
            {
                Material = material,
                BaseAlphaMaskScale = baseAlphaMaskScale,
                BaseAlphaMaskValue = baseAlphaMaskValue
            };
        }

        private static bool IsOpaqueMaterial(Material material)
        {
            if (IsLilToonMultiShader(material.shader.name))
                return Mathf.RoundToInt(material.GetFloat(TransparentModeID)) == 0;

            string shaderName = material.shader.name;
            return shaderName.IndexOf("Cutout", StringComparison.OrdinalIgnoreCase) < 0 &&
                   shaderName.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static Material ResolveMultiDitherTemplate()
        {
            if (_multiDitherTemplate == null)
                _multiDitherTemplate =
                    Resources.Load<Material>(MultiDitherKeepAliveResourcePath);

            if (_multiDitherTemplate == null ||
                _multiDitherTemplate.shader == null ||
                !IsLilToonMultiShader(_multiDitherTemplate.shader.name))
            {
                return null;
            }

            return _multiDitherTemplate;
        }

        private static Shader ResolveBaseCutoutPassShader()
        {
            if (_baseCutoutPassShader == null)
                _baseCutoutPassShader = Shader.Find(LilToonCutoutPassShaderName);

            return _baseCutoutPassShader;
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
            if (!IsSupportedLilToonShader(sourceName))
                return null;

            // Multi는 하나의 셰이더가 _TransparentMode에 따라 Opaque/Cutout/
            // Transparent 패스를 전환한다. 일반 lilToonCutout으로 치환하면
            // Multi 전용 프로퍼티 구성이 깨지므로 원래 셰이더를 유지한다.
            if (IsLilToonMultiShader(sourceName))
                return sourceShader;

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

        private void UpdateDitherPixelScale()
        {
            if (!_runtimePrepared)
                return;

            int pixelScale = Mathf.Clamp(_ditherPixelScale, 1, 4);
            if (_appliedDitherPixelScale == pixelScale)
                return;

            Texture2D ditherTexture = GetDitherTexture(pixelScale);
            if (ditherTexture == null)
                return;

            foreach (RuntimeMaterialInfo runtimeInfo in _runtimeMaterials)
            {
                if (runtimeInfo?.Material != null)
                    runtimeInfo.Material.SetTexture(DitherTexID, ditherTexture);
            }

            _appliedDitherPixelScale = pixelScale;
        }

        private static Texture2D GetDitherTexture(int pixelScale)
        {
            pixelScale = Mathf.Clamp(pixelScale, 1, 4);
            if (ScaledDitherTextures.TryGetValue(
                    pixelScale,
                    out Texture2D cachedTexture) &&
                cachedTexture != null)
                return cachedTexture;

            if (_ditherTexture == null)
                _ditherTexture =
                    Resources.Load<Texture2D>(DitherResourcePath);

            if (_ditherTexture == null)
                return null;

            Color32[] sourcePixels = _ditherTexture.GetPixels32();
            byte[] sourceThresholds = BuildDitherThresholds(sourcePixels);
            int sourceWidth = _ditherTexture.width;
            int sourceHeight = _ditherTexture.height;
            int scaledWidth = sourceWidth * pixelScale;
            int scaledHeight = sourceHeight * pixelScale;
            var scaledPixels = new byte[scaledWidth * scaledHeight];
            for (int y = 0; y < scaledHeight; y++)
            {
                int sourceY = y / pixelScale;
                for (int x = 0; x < scaledWidth; x++)
                {
                    int sourceX = x / pixelScale;
                    scaledPixels[y * scaledWidth + x] =
                        sourceThresholds[sourceY * sourceWidth + sourceX];
                }
            }

            var scaledTexture = new Texture2D(
                scaledWidth,
                scaledHeight,
                TextureFormat.R8,
                false,
                true)
            {
                name =
                    $"{_ditherTexture.name} Pixel Scale {pixelScale}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave
            };
            scaledTexture.SetPixelData(scaledPixels, 0);
            scaledTexture.Apply(false, true);
            ScaledDitherTextures[pixelScale] = scaledTexture;
            return scaledTexture;
        }

        private static byte[] BuildDitherThresholds(Color32[] sourcePixels)
        {
            // 생성 이미지의 명암 분포는 확률 분포가 아니다. 밝기 순위를 균등한 8비트
            // 임계값으로 바꿔 visibility 0.5가 실제로 절반의 픽셀을 남기도록 한다.
            var nextRanks = new int[256];
            var indices = new int[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                nextRanks[sourcePixels[i].r]++;
                indices[i] = i;
            }

            int rank = 0;
            for (int i = 0; i < nextRanks.Length; i++)
            {
                int count = nextRanks[i];
                nextRanks[i] = rank;
                rank += count;
            }

            // 동률 픽셀을 행 순서대로 나누면 가로 띠가 생긴다. 고정 시드로 분산하며
            // UnityEngine.Random 상태나 매 프레임의 패턴에는 영향을 주지 않는다.
            var random = new System.Random(0);
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                (indices[i], indices[other]) = (indices[other], indices[i]);
            }

            var thresholds = new byte[sourcePixels.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];
                thresholds[index] = (byte)(nextRanks[sourcePixels[index].r]++ * 256 / indices.Length);
            }
            return thresholds;
        }

        private void RestoreOriginalMaterials()
        {
            foreach (RendererInfo info in _rendererInfos)
            {
                if (info.Renderer == null)
                    continue;

                info.Renderer.forceRenderingOff =
                    info.OriginalForceRenderingOff
                    || ResolvePresentation()?.IsHidden == true;
                if (!info.HasRuntimeMaterials)
                    continue;

                if (HasMaterialSet(info.Renderer, info.RuntimeMaterials))
                    info.Renderer.sharedMaterials = info.OriginalMaterials;
                info.HasRuntimeMaterials = false;
            }
        }

        private bool HasMaterialSet(Renderer renderer, Material[] expected)
        {
            if (expected == null)
                return false;
            renderer.GetSharedMaterials(_sharedMaterialBuffer);
            if (_sharedMaterialBuffer.Count != expected.Length)
                return false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (_sharedMaterialBuffer[i] != expected[i])
                    return false;
            }
            return true;
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
                if (runtimeInfo?.Material != null &&
                    CanWarmupWithSetPass(runtimeInfo.Material.shader))
                {
                    runtimeInfo.Material.SetPass(0);
                }

                yield return null;
            }

            _warmupCoroutine = null;
        }

        private static bool CanWarmupWithSetPass(Shader shader)
        {
            if (shader == null)
                return false;

            // Hidden/lilToonCutout 계열은 Hidden/ltspass_cutout을 UsePass로
            // 공유한다. Unity 6에서 래퍼 Material.SetPass를 직접 호출하면 두
            // 셰이더의 LocalKeywordSpace가 다르다는 엔진 Assert가 발생한다.
            // 실제 렌더링 시에는 URP가 올바른 패스를 선택하므로 워밍업만 생략한다.
            return !shader.name.StartsWith(
                "Hidden/lilToonCutout",
                StringComparison.Ordinal);
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
