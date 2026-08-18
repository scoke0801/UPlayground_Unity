using UnityEngine;
using UnityEngine.InputSystem;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 녹화/스냅샷 캡처용 프리카메라 모드.
    /// 인게임 추적/락온/충돌을 끊고 카메라 Transform을 직접 조작한다.
    /// </summary>
    public class FreeCameraBehavior : ICameraBehavior
    {
        private Vector3 _position;
        private Quaternion _rotation;
        private float _fieldOfView;
        private float _yaw;
        private float _pitch;
        private float _moveSpeed = 6f;
        private float _lookSensitivity = 0.12f;
        private bool _initialized;
        private bool _previousPlayerActionSuppressed;
        private bool _previousPlayerActorInputSuppressed;
        private Transform _suppressedPlayerTarget;

        public CameraModeType ModeType => CameraModeType.Free;
        public int Priority => 90;
        public bool AllowsPlayerLookInput => false;
        public bool AllowsZoomInput => false;
        public bool AllowsLockOnInput => false;
        public bool UseCollision => false;
        public bool RequiresPrimaryTarget => false;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            if (context.MainCamera == null)
                return;

            _position = context.MainCamera.transform.position;
            _rotation = context.MainCamera.transform.rotation;
            _fieldOfView = context.MainCamera.fieldOfView;

            Vector3 euler = _rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizePitch(euler.x);

            _moveSpeed = enterParams.FreeCameraMoveSpeed > 0f ? enterParams.FreeCameraMoveSpeed : 6f;
            _lookSensitivity = enterParams.FreeCameraLookSensitivity > 0f ? enterParams.FreeCameraLookSensitivity : 0.12f;

            context.IsInputLocked = true;
            context.LockOn?.Release();
            SuppressPlayerInput(context);
            _initialized = true;
        }

        public void OnExit(CameraContext context)
        {
            RestorePlayerInput();
            context.IsInputLocked = false;
            _initialized = false;
        }

        public void HandleInput(CameraContext context, float deltaTime)
        {
            if (!_initialized || context.MainCamera == null)
                return;

            float dt = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : deltaTime;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * _lookSensitivity;
                _pitch -= delta.y * _lookSensitivity;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);
                _rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            if (keyboard != null)
            {
                Vector3 localMove = Vector3.zero;
                if (keyboard.wKey.isPressed) localMove += Vector3.forward;
                if (keyboard.sKey.isPressed) localMove += Vector3.back;
                if (keyboard.dKey.isPressed) localMove += Vector3.right;
                if (keyboard.aKey.isPressed) localMove += Vector3.left;
                if (keyboard.eKey.isPressed) localMove += Vector3.up;
                if (keyboard.qKey.isPressed) localMove += Vector3.down;

                if (localMove.sqrMagnitude > 1f)
                    localMove.Normalize();

                float speed = _moveSpeed;
                if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                    speed *= 4f;
                if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                    speed *= 0.25f;

                _position += _rotation * localMove * speed * dt;
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    _fieldOfView = Mathf.Clamp(_fieldOfView - scroll * 0.02f, 10f, 100f);
            }
        }

        public CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState)
        {
            if (!_initialized || context.MainCamera == null)
                return default;

            Vector3 position = _position + effectState.positionDelta;
            Quaternion rotation = Quaternion.Euler(effectState.pitchDelta, effectState.yawDelta, 0f) * _rotation;
            Vector3 euler = rotation.eulerAngles;

            context.State.CurrentYaw = euler.y;
            context.State.CurrentPitch = NormalizePitch(euler.x);
            context.State.TargetDistance = 0f;
            context.State.CurrentDistance = 0f;
            context.State.SmoothPosition = position;

            return new CameraPose
            {
                PivotPosition = position,
                CameraPosition = position,
                CameraRotation = rotation,
                Yaw = euler.y,
                Pitch = euler.x,
                Distance = 0f,
                FieldOfView = Mathf.Clamp(_fieldOfView + effectState.fovDelta, 1f, 179f)
            };
        }

        private static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }

        private void SuppressPlayerInput(CameraContext context)
        {
            ICameraRuntimeAdapter runtime = CameraRuntimeServices.Adapter;
            _previousPlayerActionSuppressed = runtime.IsPlayerActionInputSuppressed;

            runtime.SetPlayerActionInputSuppressed(true);
            runtime.ClearBufferedInput();

            _suppressedPlayerTarget = context.Target;

            _previousPlayerActorInputSuppressed =
                runtime.IsTargetInputSuppressed(_suppressedPlayerTarget);
            runtime.SetTargetInputSuppressed(_suppressedPlayerTarget, true);
        }

        private void RestorePlayerInput()
        {
            ICameraRuntimeAdapter runtime = CameraRuntimeServices.Adapter;
            runtime.SetTargetInputSuppressed(
                _suppressedPlayerTarget,
                _previousPlayerActorInputSuppressed);
            _suppressedPlayerTarget = null;
            _previousPlayerActorInputSuppressed = false;

            runtime.SetPlayerActionInputSuppressed(_previousPlayerActionSuppressed);
            runtime.ClearBufferedInput();
        }
    }
}
