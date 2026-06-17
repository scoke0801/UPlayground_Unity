using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 전용 카메라 모드.
    /// PrimaryTarget은 화자, SecondaryTarget은 청자/플레이어로 사용한다.
    /// </summary>
    public class DialogueCameraBehavior : ICameraBehavior
    {
        private Transform _speaker;
        private Transform _listener;
        private Vector3 _offset;
        private float _distance;
        private float _fieldOfView;
        private DialogueCameraSettingsSO _fallbackSettings;

        private Quaternion _currentRotation;
        private bool _isFirstFrame;
        private bool _wasInDialogue;

        // 인트로 시퀀스: 대화 진입 1회만 플레이어(청자)→화자로 부드럽게 패닝
        private bool _introActive;
        private float _introElapsed;

        private struct FramedPose
        {
            public Vector3 LookAt;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public CameraModeType ModeType => CameraModeType.Dialogue;
        public int Priority => 50;
        public bool AllowsPlayerLookInput => false;
        public bool AllowsZoomInput => false;
        public bool AllowsLockOnInput => false;
        public bool UseCollision => true;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            _speaker = enterParams.PrimaryTarget;
            _listener = enterParams.SecondaryTarget != null ? enterParams.SecondaryTarget : context.Target;

            DialogueCameraSettingsSO settings = GetSettings(context);
            _offset = enterParams.Offset == default ? settings.listenerShoulderOffset : enterParams.Offset;
            _distance = settings.ClampDistance(enterParams.Duration > 0f ? enterParams.Duration : settings.twoShotDistance);
            _fieldOfView = settings.fieldOfView;

            context.IsInputLocked = true;
            context.LockOn?.Release();
            // 대화 중 화자 전환은 부드럽게 블렌딩, 초진입(InGame→Dialogue)만 즉시 스냅
            _isFirstFrame = !_wasInDialogue;

            // 인트로(플레이어→화자 1회 팬)는 진짜 첫 진입 + 청자/화자가 모두 있을 때만 발동
            _introActive = _isFirstFrame
                && settings.enableIntroSequence
                && _speaker != null
                && _listener != null;
            _introElapsed = 0f;

            _wasInDialogue = true;
        }

        public void OnExit(CameraContext context)
        {
            context.IsInputLocked = false;
            _wasInDialogue = false;
        }

        public bool IsSameSpeaker(Transform speaker, Transform listener)
        {
            if (_speaker != speaker)
                return false;
            return listener == null || _listener == listener;
        }

        public void HandleInput(CameraContext context, float deltaTime)
        {
        }

        public CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState)
        {
            Transform target = _speaker != null ? _speaker : context.Target;
            if (target == null || context.MainCamera == null)
                return default;

            DialogueCameraSettingsSO settings = GetSettings(context);

            // 화자(대상) 클로즈업 — 인트로의 최종 구도이자 평상시 추종 포즈
            FramedPose finalPose = ComputeFramedPose(context, settings, target, _listener);

            Vector3 lookAt = finalPose.LookAt;
            Vector3 cameraPosition;
            Quaternion cameraRotation;

            if (_introActive)
            {
                // 인트로: 플레이어(청자) 바라봄 → 멈춤 → 화자로 부드럽게 팬 → 화자 고정
                _introElapsed += deltaTime;
                FramedPose playerPose = ComputeFramedPose(context, settings, _listener, target);

                float hold = Mathf.Max(0f, settings.introPlayerHoldTime);
                float pan = Mathf.Max(0.01f, settings.introPanDuration);

                if (_introElapsed <= hold)
                {
                    // 플레이어를 한 번 바라보고 멈춤
                    lookAt = playerPose.LookAt;
                    cameraPosition = playerPose.Position;
                    cameraRotation = playerPose.Rotation;
                }
                else if (_introElapsed <= hold + pan)
                {
                    // 플레이어 → 화자 부드러운 팬
                    float t = Mathf.SmoothStep(0f, 1f, (_introElapsed - hold) / pan);
                    lookAt = Vector3.Lerp(playerPose.LookAt, finalPose.LookAt, t);
                    cameraPosition = Vector3.Lerp(playerPose.Position, finalPose.Position, t);
                    cameraRotation = Quaternion.Slerp(playerPose.Rotation, finalPose.Rotation, t);
                }
                else
                {
                    // 인트로 종료 — 화자 클로즈업으로 고정하고 평상 추종으로 전환
                    _introActive = false;
                    _isFirstFrame = false;
                    cameraPosition = finalPose.Position;
                    cameraRotation = finalPose.Rotation;
                }
                _currentRotation = cameraRotation;
            }
            else if (_isFirstFrame)
            {
                // 인트로 미사용 시: 첫 프레임에 목표 위치·회전으로 즉시 컷
                // → InGame 카메라에서 날아오거나 빙글 도는 현상 방지
                _isFirstFrame = false;
                _currentRotation = finalPose.Rotation;
                cameraPosition = finalPose.Position;
                cameraRotation = finalPose.Rotation;
            }
            else
            {
                // 평상시: softBlendTime 기반 미세 보정만 수행
                float blendTime = Mathf.Max(0.01f, settings.softBlendTime);
                float blendFactor = 1f - Mathf.Exp(-(1f / blendTime) * deltaTime);
                _currentRotation = Quaternion.Slerp(_currentRotation, finalPose.Rotation, blendFactor);
                cameraPosition = Vector3.Lerp(context.MainCamera.transform.position, finalPose.Position, blendFactor);
                cameraRotation = _currentRotation;
            }

            cameraPosition += effectState.positionDelta;

            Vector3 euler = cameraRotation.eulerAngles;
            float fov = _fieldOfView + effectState.fovDelta;

            return new CameraPose
            {
                PivotPosition = lookAt,
                CameraPosition = cameraPosition,
                CameraRotation = cameraRotation,
                Yaw = euler.y + effectState.yawDelta,
                Pitch = euler.x + effectState.pitchDelta,
                Distance = Vector3.Distance(lookAt, cameraPosition) + effectState.distanceDelta,
                FieldOfView = fov
            };
        }

        /// <summary>
        /// subject(주시 대상)를 reference(반대편 인물) 기준으로 어깨 너머 구도로 잡는 포즈를 산출한다.
        /// 화자 클로즈업은 (speaker, listener), 인트로의 플레이어 컷은 (listener, speaker)로 호출한다.
        /// </summary>
        private FramedPose ComputeFramedPose(CameraContext context, DialogueCameraSettingsSO settings, Transform subject, Transform reference)
        {
            Vector3 lookAt = subject.position + settings.speakerLookAtOffset;
            Vector3 baseForward = ResolveDialogueForward(subject, reference);
            Quaternion baseRotation = Quaternion.LookRotation(baseForward, Vector3.up);
            Vector3 desiredOffset = _offset.sqrMagnitude > 0.001f ? _offset.normalized : settings.listenerShoulderOffset.normalized;
            float desiredDistance = settings.ClampDistance(_distance);

            if (UseCollision && context.Collision != null)
            {
                Vector3 camDir = baseRotation * desiredOffset;
                desiredDistance = Mathf.Max(0.1f, context.Collision.Evaluate(lookAt, camDir, desiredDistance));
            }

            Vector3 position = lookAt + baseRotation * desiredOffset * desiredDistance;
            Quaternion rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);

            return new FramedPose { LookAt = lookAt, Position = position, Rotation = rotation };
        }

        private DialogueCameraSettingsSO GetSettings(CameraContext context)
        {
            if (context.DialogueSettings != null)
                return context.DialogueSettings;

            if (_fallbackSettings == null)
                _fallbackSettings = DialogueCameraSettingsSO.CreateRuntimeDefault();

            return _fallbackSettings;
        }

        private static Vector3 ResolveDialogueForward(Transform speaker, Transform listener)
        {
            if (speaker != null && listener != null)
            {
                Vector3 dir = speaker.position - listener.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    return dir.normalized;
            }

            Vector3 fallback = speaker != null ? speaker.forward : Vector3.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        }
    }
}
