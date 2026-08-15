using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 전용 카메라 모드.
    ///
    /// 라인마다 DialogueShotDirector가 샷/전환을 결정하고 DialogueShotComposer가 포즈를 만든다.
    /// 이 모드는 결정된 목표 포즈로의 보간과 인트로 시퀀스 재생만 담당한다.
    /// 가상선·인트로 소진 같은 대화 전체 상태는 CameraContext.DialogueSession이 소유한다
    /// (모드는 Replay 왕복 때마다 OnEnter/OnExit가 반복되므로 상태를 들고 있으면 안 된다).
    /// </summary>
    public class DialogueCameraBehavior : ICameraBehavior
    {
        private DialogueShotRequest _request;
        private DialogueShotType _shotType = DialogueShotType.OverTheShoulderSpeaker;
        private DialogueCameraSettingsSO _fallbackSettings;

        // 현재 출력 중인 포즈 — 실제 카메라 transform을 되읽지 않는다.
        // 되읽으면 쉐이크 등 이펙트 델타가 다음 프레임 보간 입력으로 섞여 흔들림이 끌려다닌다.
        private Vector3 _currentPosition;
        private Vector3 _currentLookAt;
        private Quaternion _currentRotation = Quaternion.identity;
        private float _currentFieldOfView = 45f;
        private bool _hasCurrentPose;

        private float _blendTime;

        // 인트로 시퀀스: 대화 세션당 1회, 플레이어(청자) → 화자로 부드럽게 패닝
        private bool _introActive;
        private float _introElapsed;

        public CameraModeType ModeType => CameraModeType.Dialogue;
        public int Priority => 50;
        public bool AllowsPlayerLookInput => false;
        public bool AllowsZoomInput => false;
        public bool AllowsLockOnInput => false;
        public bool UseCollision => true;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            if (enterParams == null || !enterParams.HasDialogueShot)
            {
                Debug.LogError("[DialogueCameraBehavior] DialogueShotRequest 없이 대화 카메라에 진입했습니다.");
                return;
            }

            _request = enterParams.DialogueShot;

            if (_request.Listener == null)
                _request.Listener = context.Target;

            DialogueCameraSettingsSO settings = GetSettings(context);
            DialogueShotSession session = context.DialogueSession;

            if (session == null)
            {
                Debug.LogError("[DialogueCameraBehavior] 활성 대화 세션이 없습니다.");
                return;
            }

            // 인물이 대화 중 이동해 가상선이 크게 틀어졌으면 축만 다시 잡는다(카메라 쪽은 유지).
            session.RefreshAxisIfDeviated(settings.axisRecaptureAngle);

            bool isSessionStart = session.LineIndex == 0;

            DialogueShotDirector.Decision decision = DialogueShotDirector.Decide(settings, session, _request);
            _shotType = decision.Shot;
            _blendTime = DialogueShotDirector.ResolveBlendTime(settings, decision.Transition);

            // 인트로는 진행 중에 다음 라인이 들어오면 그대로 취소된다(OnEnter가 다시 결정하므로).
            _introActive = decision.PlayIntro;
            _introElapsed = 0f;

            if (isSessionStart && (decision.PlayIntro || !settings.establishBlendOnEnter))
            {
                // 대화 진입은 즉시 컷 — InGame 카메라에서 날아오거나 빙글 도는 현상 방지.
                _hasCurrentPose = false;
            }
            else
            {
                // 라인 전환은 카메라가 실제로 있는 위치에서 블렌드를 시작한다.
                // Replay(녹화) 노드를 거쳐 돌아온 경우에도 마지막 프레임에서 자연스럽게 이어진다.
                // 매 프레임이 아니라 진입 시 1회만 읽으므로 이펙트 델타가 보간에 되먹임되지 않는다.
                SeedFromCamera(context, settings);
            }

            if (decision.PlayIntro)
                session.IntroConsumed = true;

            session.ConsecutiveShortLines = decision.ConsecutiveShortLines;
            session.LastSubject = decision.Subject;
            session.LastSpeaker = _request.Speaker;
            session.LastShotType = decision.Shot;
            session.LineIndex++;

            context.IsInputLocked = true;
            context.LockOn?.Release();
        }

        public void OnExit(CameraContext context)
        {
            context.IsInputLocked = false;
            _introActive = false;

            // 세션 상태는 건드리지 않는다. Dialogue ↔ Replay 전환에서도 OnExit가 불리므로
            // 여기서 초기화하면 대화 도중 인트로가 다시 재생되고 가상선이 풀린다.
        }

        /// <summary>같은 라인의 중복 진입인지. CameraManager의 재진입 no-op 가드가 사용한다.</summary>
        public bool IsSameShot(in DialogueShotRequest request) => _request.Matches(request);

        public void HandleInput(CameraContext context, float deltaTime)
        {
        }

        public CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState)
        {
            if (context.MainCamera == null)
                return default;

            if (_request.Speaker == null && _request.Listener == null && context.Target == null)
                return default;

            DialogueCameraSettingsSO settings = GetSettings(context);
            DialogueShotSession session = context.DialogueSession;

            DialogueShotComposer.FramedPose targetPose = DialogueShotComposer.Compose(
                context, settings, session, _request, _shotType, UseCollision);

            if (!_hasCurrentPose)
            {
                // 첫 프레임은 즉시 목표 포즈로 컷한다.
                // InGame 카메라 위치에서 날아오거나 빙글 도는 현상 방지.
                ApplyPoseImmediate(targetPose);
            }

            if (_introActive)
            {
                EvaluateIntro(context, settings, session, targetPose, deltaTime);
            }
            else
            {
                BlendTowards(targetPose, deltaTime);
            }

            return BuildPose(effectState);
        }

        /// <summary>
        /// 인트로: 플레이어(청자)를 한 번 바라보고 멈춤 → 화자로 부드럽게 팬 → 화자 고정.
        /// 두 포즈 모두 Composer가 같은 가상선 위에서 만들기 때문에 팬 도중 선을 넘지 않는다.
        /// </summary>
        private void EvaluateIntro(
            CameraContext context,
            DialogueCameraSettingsSO settings,
            DialogueShotSession session,
            in DialogueShotComposer.FramedPose targetPose,
            float deltaTime)
        {
            _introElapsed += deltaTime;

            DialogueShotComposer.FramedPose listenerPose = DialogueShotComposer.Compose(
                context, settings, session, _request, DialogueShotType.OverTheShoulderListener, UseCollision);

            float hold = Mathf.Max(0f, settings.introPlayerHoldTime);
            float pan = Mathf.Max(0.01f, settings.introPanDuration);

            if (_introElapsed <= hold)
            {
                ApplyPoseImmediate(listenerPose);
                return;
            }

            if (_introElapsed <= hold + pan)
            {
                float t = Mathf.SmoothStep(0f, 1f, (_introElapsed - hold) / pan);
                _currentLookAt = Vector3.Lerp(listenerPose.LookAt, targetPose.LookAt, t);
                _currentPosition = Vector3.Lerp(listenerPose.Position, targetPose.Position, t);
                _currentRotation = Quaternion.Slerp(listenerPose.Rotation, targetPose.Rotation, t);
                _currentFieldOfView = Mathf.Lerp(listenerPose.FieldOfView, targetPose.FieldOfView, t);
                return;
            }

            // 인트로 종료 — 화자 구도로 고정하고 평상 추종으로 전환
            _introActive = false;
            ApplyPoseImmediate(targetPose);
        }

        /// <summary>
        /// 결정된 전환 시간으로 현재 포즈를 목표 포즈에 붙인다.
        /// blendTime이 0이면(Cut) 즉시 스냅한다.
        /// </summary>
        private void BlendTowards(in DialogueShotComposer.FramedPose targetPose, float deltaTime)
        {
            if (_blendTime <= 0.0001f)
            {
                ApplyPoseImmediate(targetPose);
                return;
            }

            float factor = 1f - Mathf.Exp(-(1f / _blendTime) * deltaTime);
            _currentPosition = Vector3.Lerp(_currentPosition, targetPose.Position, factor);
            _currentLookAt = Vector3.Lerp(_currentLookAt, targetPose.LookAt, factor);
            _currentRotation = Quaternion.Slerp(_currentRotation, targetPose.Rotation, factor);
            _currentFieldOfView = Mathf.Lerp(_currentFieldOfView, targetPose.FieldOfView, factor);
        }

        /// <summary>현재 카메라 위치/회전을 보간 시작점으로 채택한다.</summary>
        private void SeedFromCamera(CameraContext context, DialogueCameraSettingsSO settings)
        {
            Transform cameraTransform = context.MainCamera != null ? context.MainCamera.transform : null;
            if (cameraTransform == null)
            {
                _hasCurrentPose = false;
                return;
            }

            _currentPosition = cameraTransform.position;
            _currentRotation = cameraTransform.rotation;
            _currentLookAt = cameraTransform.position + cameraTransform.forward * settings.twoShotDistance;
            _currentFieldOfView = context.MainCamera.fieldOfView;
            _hasCurrentPose = true;
        }

        private void ApplyPoseImmediate(in DialogueShotComposer.FramedPose pose)
        {
            _currentPosition = pose.Position;
            _currentLookAt = pose.LookAt;
            _currentRotation = pose.Rotation;
            _currentFieldOfView = pose.FieldOfView;
            _hasCurrentPose = true;
        }

        /// <summary>이펙트 델타는 보간이 끝난 뒤 출력 직전에만 더한다(다음 프레임 보간에 되먹임되지 않도록).</summary>
        private CameraPose BuildPose(CameraEffectState effectState)
        {
            Vector3 cameraPosition = _currentPosition + effectState.positionDelta;
            Vector3 euler = _currentRotation.eulerAngles;

            return new CameraPose
            {
                PivotPosition = _currentLookAt,
                CameraPosition = cameraPosition,
                CameraRotation = _currentRotation,
                Yaw = euler.y + effectState.yawDelta,
                Pitch = euler.x + effectState.pitchDelta,
                Distance = Vector3.Distance(_currentLookAt, cameraPosition) + effectState.distanceDelta,
                FieldOfView = _currentFieldOfView + effectState.fovDelta
            };
        }

        private DialogueCameraSettingsSO GetSettings(CameraContext context)
        {
            if (context.DialogueSettings != null)
                return context.DialogueSettings;

            if (_fallbackSettings == null)
                _fallbackSettings = DialogueCameraSettingsSO.CreateRuntimeDefault();

            return _fallbackSettings;
        }
    }
}
