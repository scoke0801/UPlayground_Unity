using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 전용 카메라 모드.
    /// PrimaryTarget은 화자, SecondaryTarget은 청자/플레이어로 사용한다.
    /// </summary>
    public class DialogueCameraMode : ICameraMode
    {
        private Transform _speaker;
        private Transform _listener;
        private Vector3 _offset;
        private float _distance;
        private float _fieldOfView;
        private DialogueCameraSettingsSO _fallbackSettings;

        public CameraModeType ModeType => CameraModeType.Dialogue;
        public int Priority => 50;
        public bool AllowsPlayerLookInput => false;
        public bool AllowsZoomInput => false;
        public bool AllowsLockOnInput => false;
        public bool UseCollision => true;

        public void OnEnter(CameraRuntimeContext context, CameraModeEnterParams enterParams)
        {
            _speaker = enterParams.PrimaryTarget;
            _listener = enterParams.SecondaryTarget != null ? enterParams.SecondaryTarget : context.Target;

            DialogueCameraSettingsSO settings = GetSettings(context);
            _offset = enterParams.Offset == default ? settings.listenerShoulderOffset : enterParams.Offset;
            _distance = settings.ClampDistance(enterParams.Duration > 0f ? enterParams.Duration : settings.twoShotDistance);
            _fieldOfView = settings.fieldOfView;

            context.IsInputLocked = true;
            context.LockOn?.Release();
        }

        public void OnExit(CameraRuntimeContext context)
        {
            context.IsInputLocked = false;
        }

        public void HandleInput(CameraRuntimeContext context, float deltaTime)
        {
        }

        public CameraRigPose EvaluatePose(CameraRuntimeContext context, float deltaTime, CameraEffectState effectState)
        {
            Transform target = _speaker != null ? _speaker : context.Target;
            if (target == null || context.MainCamera == null)
                return default;

            DialogueCameraSettingsSO settings = GetSettings(context);
            Vector3 lookAt = target.position + settings.speakerLookAtOffset;
            Vector3 baseForward = ResolveDialogueForward(target, _listener);
            Quaternion baseRotation = Quaternion.LookRotation(baseForward, Vector3.up);
            Vector3 desiredOffset = _offset.sqrMagnitude > 0.001f ? _offset.normalized : settings.listenerShoulderOffset.normalized;
            float desiredDistance = settings.ClampDistance(_distance);
            Vector3 desiredPosition = lookAt + baseRotation * desiredOffset * desiredDistance;

            Quaternion lookRotation = Quaternion.LookRotation(lookAt - desiredPosition, Vector3.up);
            float blendTime = Mathf.Max(0.01f, settings.speakerCutBlendTime);
            float blendFactor = 1f - Mathf.Exp(-(1f / blendTime) * deltaTime);
            Vector3 cameraPosition = Vector3.Lerp(
                context.MainCamera.transform.position,
                desiredPosition,
                blendFactor);
            Quaternion cameraRotation = Quaternion.Slerp(
                context.MainCamera.transform.rotation,
                lookRotation,
                blendFactor);

            cameraPosition += effectState.positionDelta;

            Vector3 euler = cameraRotation.eulerAngles;
            float fov = _fieldOfView + effectState.fovDelta;

            return new CameraRigPose
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

        private DialogueCameraSettingsSO GetSettings(CameraRuntimeContext context)
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
