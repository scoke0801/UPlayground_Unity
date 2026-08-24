using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>Addressable 캐릭터 모델을 격리된 스테이지에 표시하는 UI 프리뷰 렌더러.</summary>
    [DisallowMultipleComponent]
    public sealed class UICharacterPreviewRenderer : MonoBehaviour
    {
        [Header("출력")]
        [SerializeField] private RawImage _display;
        [SerializeField, Min(256)] private int _textureSize = 1024;

        [Header("카메라")]
        [SerializeField, Range(15f, 60f)] private float _fieldOfView = 30f;
        [SerializeField, Range(0.75f, 1.25f)] private float _cameraDistanceScale = 1.03f;
        [SerializeField, Range(0.5f, 1f)] private float _horizontalFramingWeight = 0.62f;
        [Tooltip("양수일수록 모델이 프리뷰 화면 위쪽에 배치됩니다.")]
        [SerializeField, Range(-0.15f, 0.15f)] private float _modelVerticalOffset = 0.045f;
        [SerializeField] private float _initialYaw;

        [Header("조명")]
        [SerializeField, Min(0f)] private float _keyLightIntensity = 1.25f;
        [SerializeField, Min(0f)] private float _fillLightIntensity = 0.65f;
        [SerializeField] private Color _keyLightColor = new(1f, 0.93f, 0.84f, 1f);
        [SerializeField] private Color _fillLightColor = new(0.55f, 0.72f, 1f, 1f);

        private GameObject _previewStage;
        private Transform _characterRoot;
        private Transform _loadingRoot;
        private Camera _previewCamera;
        private RenderTexture _renderTexture;
        private GameObject _currentPreviewCharacter;
        private IAssetLease<GameObject> _currentModelLease;
        private GameObject[] _currentPreservedInactiveObjects;
        private Renderer[] _currentPreservedDisabledRenderers;
        private CancellationTokenSource _loadCancellation;
        private float _currentRotation;
        private bool _isPreviewVisible;

        /// <summary>모델 프리뷰와 초상화 폴백의 표시 전환을 화면에 알린다.</summary>
        public event Action<bool> PreviewVisibilityChanged;
        /// <summary>모델을 표시할 수 없을 때 초상화 폴백을 허용하도록 알린다.</summary>
        public event Action PreviewLoadFailed;

        public bool IsPreviewVisible => _isPreviewVisible;

        private void Awake()
        {
            Initialize(_display);
        }

        /// <summary>출력 RawImage를 연결하고 프리뷰 전용 렌더 스테이지를 준비한다.</summary>
        public void Initialize(RawImage display)
        {
            if (display != null)
                _display = display;

            EnsureRenderStage();
            if (_display == null)
                return;

            _display.texture = _renderTexture;
            _display.raycastTarget = true;
            _display.enabled = _isPreviewVisible;
        }

        /// <summary>현재 플레이어 캐릭터의 모델을 프리뷰한다.</summary>
        public void ShowPreview()
        {
            CharacterActorType type = UISvc.Party?.ActiveCharacterType
                                      ?? CharacterActorType.None;
            ShowPreview(type);
        }

        /// <summary>선택한 캐릭터의 Addressable 모델을 비동기로 교체한다.</summary>
        public void ShowPreview(CharacterActorType characterType)
        {
            CancelPendingLoad();

            if (characterType == CharacterActorType.None)
            {
                ReleaseCurrentPreview();
                return;
            }
            if (!EnsureRenderStage())
            {
                ReleaseCurrentPreview();
                PreviewLoadFailed?.Invoke();
                return;
            }

            _loadCancellation = new CancellationTokenSource();
            LoadPreviewAsync(characterType, _loadCancellation.Token).Forget();
        }

        /// <summary>프리뷰 카메라와 모델 임대를 즉시 해제한다.</summary>
        public void HidePreview()
        {
            CancelPendingLoad();
            ReleaseCurrentPreview();
        }

        /// <summary>현재 모델을 지정한 Y축 각도만큼 회전한다.</summary>
        public void RotateCharacter(float yawDelta)
        {
            if (_currentPreviewCharacter == null)
                return;

            _currentRotation = Mathf.Repeat(_currentRotation + yawDelta, 360f);
            _currentPreviewCharacter.transform.localRotation =
                Quaternion.Euler(0f, _currentRotation, 0f);
        }

        /// <summary>기존 프리뷰 소비자가 사용할 출력 텍스처를 반환한다.</summary>
        public RenderTexture GetRenderTexture()
        {
            EnsureRenderStage();
            return _renderTexture;
        }

        private async UniTaskVoid LoadPreviewAsync(
            CharacterActorType characterType,
            CancellationToken cancellationToken)
        {
            IAssetLease<GameObject> acquiredLease = null;
            GameObject preparedInstance = null;
            GameObject[] preservedInactiveObjects = null;
            Renderer[] preservedDisabledRenderers = null;
            try
            {
                IUIPartyService party = UISvc.Party;
                if (party == null)
                    throw new InvalidOperationException("UI 파티 서비스가 없습니다.");

                string owner = $"{nameof(UICharacterPreviewRenderer)}.{GetInstanceID()}";
                acquiredLease = await party.AcquireCharacterPreviewModelAsync(
                    characterType,
                    owner,
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (acquiredLease?.Asset == null)
                    throw new InvalidOperationException("프리뷰 모델 에셋이 비어 있습니다.");

                preparedInstance = Instantiate(
                    acquiredLease.Asset,
                    _loadingRoot,
                    false);
                preparedInstance.SetActive(false);
                PreparePreviewInstance(preparedInstance);
                CapturePrefabVisibilityState(
                    preparedInstance,
                    out preservedInactiveObjects,
                    out preservedDisabledRenderers);

                preparedInstance.transform.SetParent(_characterRoot, false);
                _currentRotation = _initialYaw;
                preparedInstance.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.Euler(0f, _currentRotation, 0f));
                preparedInstance.transform.localScale = Vector3.one;

                _previewStage.SetActive(true);
                preparedInstance.SetActive(true);

                if (!PlayIdleMotion(preparedInstance))
                {
                    throw new InvalidOperationException(
                        $"{characterType} 프리뷰 대기 모션을 재생할 수 없습니다.");
                }
                RestorePrefabVisibilityState(
                    preservedInactiveObjects,
                    preservedDisabledRenderers);
                FrameCamera(preparedInstance);

                GameObject previousInstance = _currentPreviewCharacter;
                IAssetLease<GameObject> previousLease = _currentModelLease;
                _currentModelLease = acquiredLease;
                acquiredLease = null;
                _currentPreviewCharacter = preparedInstance;
                _currentPreservedInactiveObjects = preservedInactiveObjects;
                _currentPreservedDisabledRenderers = preservedDisabledRenderers;
                preparedInstance = null;
                preservedInactiveObjects = null;
                preservedDisabledRenderers = null;

                ReleasePreviewInstance(previousInstance, previousLease);
                _previewCamera.enabled = true;
                SetPreviewVisible(true);
            }
            catch (OperationCanceledException)
            {
                // 선택 변경과 화면 닫기는 정상적인 취소 경로다.
            }
            catch (Exception exception)
            {
                ReleaseCurrentPreview();
                PreviewLoadFailed?.Invoke();
                Debug.LogError(
                    $"[UICharacterPreviewRenderer] {characterType} 모델 프리뷰 로드 실패: " +
                    exception.Message,
                    this);
            }
            finally
            {
                ReleasePreviewInstance(preparedInstance, acquiredLease);
            }
        }

        private bool EnsureRenderStage()
        {
            if (_previewStage != null)
                return _previewCamera != null && _renderTexture != null;

            int previewLayer = LayerMask.NameToLayer("CharacterPreview");
            if (previewLayer < 0)
            {
                Debug.LogError(
                    "[UICharacterPreviewRenderer] CharacterPreview 레이어가 없습니다.",
                    this);
                return false;
            }

            _previewStage = new GameObject(
                $"CharacterPreviewStage_{GetInstanceID()}");
            _previewStage.hideFlags = HideFlags.DontSave;
            _previewStage.SetActive(false);

            _characterRoot = new GameObject("CharacterRoot").transform;
            _characterRoot.SetParent(_previewStage.transform, false);

            _loadingRoot = new GameObject("LoadingRoot").transform;
            _loadingRoot.SetParent(_previewStage.transform, false);
            _loadingRoot.gameObject.SetActive(false);

            GameObject cameraObject = new("PreviewCamera", typeof(Camera));
            cameraObject.transform.SetParent(_previewStage.transform, false);
            _previewCamera = cameraObject.GetComponent<Camera>();
            _previewCamera.enabled = false;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = Color.clear;
            _previewCamera.cullingMask = 1 << previewLayer;
            _previewCamera.fieldOfView = _fieldOfView;
            _previewCamera.allowHDR = true;
            _previewCamera.allowMSAA = true;

            CreatePreviewLight(
                "KeyLight",
                new Vector3(35f, -35f, 0f),
                _keyLightColor,
                _keyLightIntensity,
                previewLayer);
            CreatePreviewLight(
                "FillLight",
                new Vector3(20f, 145f, 0f),
                _fillLightColor,
                _fillLightIntensity,
                previewLayer);

            int size = Mathf.Max(256, _textureSize);
            _renderTexture = new RenderTexture(
                size,
                size,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = $"RT_CharacterPreview_{GetInstanceID()}",
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _renderTexture.Create();
            _previewCamera.targetTexture = _renderTexture;
            return true;
        }

        private void CreatePreviewLight(
            string objectName,
            Vector3 eulerAngles,
            Color color,
            float intensity,
            int previewLayer)
        {
            GameObject lightObject = new(objectName, typeof(Light));
            lightObject.transform.SetParent(_previewStage.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(eulerAngles);

            Light previewLight = lightObject.GetComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.color = color;
            previewLight.intensity = intensity;
            previewLight.shadows = LightShadows.None;
            previewLight.cullingMask = 1 << previewLayer;
        }

        private static void PreparePreviewInstance(GameObject instance)
        {
            SetLayerRecursively(instance, LayerMask.NameToLayer("CharacterPreview"));

            PlayerEquipment[] equipmentComponents =
                instance.GetComponentsInChildren<PlayerEquipment>(true);
            for (int i = 0; i < equipmentComponents.Length; i++)
                equipmentComponents[i].HideEquipmentForPresentationPreview();

            MonoBehaviour[] behaviours =
                instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || IsPreviewAnimationComponent(behaviour))
                    continue;

                behaviour.enabled = false;
            }

            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animators[i].updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true;
                rigidbodies[i].useGravity = false;
            }

            AudioSource[] audioSources =
                instance.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
                audioSources[i].enabled = false;

            ParticleSystem[] particles =
                instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                renderers[i].receiveShadows = false;
            }
        }

        private static void CapturePrefabVisibilityState(
            GameObject instance,
            out GameObject[] inactiveObjects,
            out Renderer[] disabledRenderers)
        {
            Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
            var inactive = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject target = transforms[i].gameObject;
                if (target != instance && !target.activeSelf)
                    inactive.Add(target);
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            var disabled = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                if (!renderers[i].enabled)
                    disabled.Add(renderers[i]);

            inactiveObjects = inactive.ToArray();
            disabledRenderers = disabled.ToArray();
        }

        private static void RestorePrefabVisibilityState(
            GameObject[] inactiveObjects,
            Renderer[] disabledRenderers)
        {
            if (inactiveObjects != null)
            {
                for (int i = 0; i < inactiveObjects.Length; i++)
                {
                    GameObject target = inactiveObjects[i];
                    if (target != null && target.activeSelf)
                        target.SetActive(false);
                }
            }

            if (disabledRenderers == null)
                return;

            for (int i = 0; i < disabledRenderers.Length; i++)
            {
                Renderer renderer = disabledRenderers[i];
                if (renderer != null && renderer.enabled)
                    renderer.enabled = false;
            }
        }

        private static bool IsPreviewAnimationComponent(MonoBehaviour behaviour)
        {
            if (behaviour is ActorAnimator)
                return true;

            Type type = behaviour.GetType();
            return type.Namespace == "Animancer"
                   && type.Name == "AnimancerComponent";
        }

        private static bool PlayIdleMotion(GameObject instance)
        {
            PlayerActorAnimator animator =
                instance.GetComponentInChildren<PlayerActorAnimator>(true);
            return animator != null && animator.TryPlayPresentationIdle();
        }

        private void FrameCamera(GameObject instance)
        {
            if (!TryCalculateRenderableBounds(instance, out Bounds bounds))
            {
                bounds = new Bounds(
                    instance.transform.position + Vector3.up,
                    new Vector3(1f, 2f, 1f));
            }

            float aspect = _renderTexture != null && _renderTexture.height > 0
                ? (float)_renderTexture.width / _renderTexture.height
                : 1f;
            float distance = CalculateCameraDistance(
                bounds.extents,
                _fieldOfView,
                aspect,
                _cameraDistanceScale,
                _horizontalFramingWeight);
            Vector3 target = bounds.center
                             - Vector3.up * bounds.size.y * _modelVerticalOffset;
            _previewCamera.transform.SetPositionAndRotation(
                target + Vector3.forward * distance,
                Quaternion.LookRotation(Vector3.back, Vector3.up));
            _previewCamera.nearClipPlane = Mathf.Max(
                0.01f,
                distance - bounds.size.magnitude);
            _previewCamera.farClipPlane =
                distance + bounds.size.magnitude * 2f;
        }

        internal static float CalculateCameraDistance(
            Vector3 extents,
            float verticalFieldOfView,
            float aspect,
            float distanceScale,
            float horizontalWeight)
        {
            float halfVerticalRadians =
                Mathf.Deg2Rad * Mathf.Clamp(verticalFieldOfView, 1f, 179f) * 0.5f;
            float verticalDistance = extents.y / Mathf.Tan(halfVerticalRadians);
            float horizontalHalfRadians = Mathf.Atan(
                Mathf.Tan(halfVerticalRadians) * Mathf.Max(0.01f, aspect));
            float horizontalDistance = extents.x
                                       / Mathf.Tan(horizontalHalfRadians)
                                       * Mathf.Clamp(horizontalWeight, 0.5f, 1f);
            return Mathf.Max(0.1f, Mathf.Max(verticalDistance, horizontalDistance))
                   * Mathf.Clamp(distanceScale, 0.75f, 1.25f);
        }

        private static bool TryCalculateRenderableBounds(
            GameObject instance,
            out Bounds bounds)
        {
            if (TryCalculateHumanoidBounds(instance, out bounds))
                return true;

            SkinnedMeshRenderer[] characterRenderers =
                instance.GetComponentsInChildren<SkinnedMeshRenderer>(false);
            if (TryEncapsulateBounds(characterRenderers, out bounds))
                return true;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(false);
            return TryEncapsulateBounds(renderers, out bounds);
        }

        private static bool TryCalculateHumanoidBounds(
            GameObject instance,
            out Bounds bounds)
        {
            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null || animator.avatar == null || !animator.isHuman)
                    continue;

                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (head == null || hips == null || leftFoot == null || rightFoot == null)
                    continue;

                float floor = Mathf.Min(leftFoot.position.y, rightFoot.position.y);
                float skeletalHeight = head.position.y - floor;
                if (skeletalHeight <= 0.1f)
                    continue;

                Vector3 min = Vector3.Min(leftFoot.position, rightFoot.position);
                Vector3 max = Vector3.Max(leftFoot.position, rightFoot.position);
                EncapsulatePoint(ref min, ref max, hips.position);
                EncapsulatePoint(ref min, ref max, head.position);
                EncapsulateHumanoidBone(
                    animator,
                    HumanBodyBones.LeftHand,
                    ref min,
                    ref max);
                EncapsulateHumanoidBone(
                    animator,
                    HumanBodyBones.RightHand,
                    ref min,
                    ref max);

                min.y = floor - skeletalHeight * 0.04f;
                max.y = head.position.y + skeletalHeight * 0.26f;
                float horizontalPadding = skeletalHeight * 0.08f;
                min.x -= horizontalPadding;
                max.x += horizontalPadding;
                min.z -= horizontalPadding;
                max.z += horizontalPadding;

                bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return true;
            }

            bounds = default;
            return false;
        }

        private static void EncapsulateHumanoidBone(
            Animator animator,
            HumanBodyBones bone,
            ref Vector3 min,
            ref Vector3 max)
        {
            Transform transform = animator.GetBoneTransform(bone);
            if (transform != null)
                EncapsulatePoint(ref min, ref max, transform.position);
        }

        private static void EncapsulatePoint(
            ref Vector3 min,
            ref Vector3 max,
            Vector3 point)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        private static bool TryEncapsulateBounds<T>(
            T[] renderers,
            out Bounds bounds)
            where T : Renderer
        {
            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            Transform transform = target.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        private void CancelPendingLoad()
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }

        private void ReleaseCurrentPreview()
        {
            if (_previewCamera != null)
                _previewCamera.enabled = false;
            if (_display != null)
                _display.enabled = false;

            SetPreviewVisible(false);
            IAssetLease<GameObject> modelLease = _currentModelLease;
            _currentModelLease = null;
            _currentPreservedInactiveObjects = null;
            _currentPreservedDisabledRenderers = null;
            GameObject previewCharacter = _currentPreviewCharacter;
            _currentPreviewCharacter = null;
            ReleasePreviewInstance(previewCharacter, modelLease);

            if (_previewStage != null)
                _previewStage.SetActive(false);
        }

        private static void ReleasePreviewInstance(
            GameObject instance,
            IAssetLease<GameObject> modelLease)
        {
            if (instance == null)
            {
                modelLease?.Dispose();
                return;
            }

            instance.SetActive(false);
            Destroy(instance);
            ReleaseModelLeaseAfterDestroyAsync(modelLease).Forget();
        }

        private static async UniTaskVoid ReleaseModelLeaseAfterDestroyAsync(
            IAssetLease<GameObject> modelLease)
        {
            if (modelLease == null)
                return;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            modelLease.Dispose();
        }

        private void SetPreviewVisible(bool visible)
        {
            if (_isPreviewVisible == visible)
                return;

            _isPreviewVisible = visible;
            if (_display != null)
                _display.enabled = visible;
            PreviewVisibilityChanged?.Invoke(visible);
        }

        private void LateUpdate()
        {
            RestorePrefabVisibilityState(
                _currentPreservedInactiveObjects,
                _currentPreservedDisabledRenderers);
        }

        private void OnDestroy()
        {
            CancelPendingLoad();
            ReleaseCurrentPreview();

            if (_previewCamera != null)
                _previewCamera.targetTexture = null;
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
            if (_previewStage != null)
            {
                Destroy(_previewStage);
                _previewStage = null;
            }
        }
    }
}
