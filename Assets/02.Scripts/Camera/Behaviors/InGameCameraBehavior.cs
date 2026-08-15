using UnityEngine;
using UnityEngine.InputSystem;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 기본 플레이 카메라 Behavior. 추적/락온/거리·FOV/충돌을 Modifier 파이프라인으로 산출한다.
    /// InGameCameraMode를 대체한다. 포즈 계산은 CameraBehaviorBase의 파이프라인이 담당하고,
    /// 룩/줌 입력 처리만 이 클래스가 직접 보유한다.
    /// </summary>
    public sealed class InGameCameraBehavior : CameraBehaviorBase
    {
        public override CameraModeType ModeType => CameraModeType.InGame;
        public override int Priority => 0;
        public override bool AllowsPlayerLookInput => true;
        public override bool AllowsZoomInput => true;
        public override bool AllowsLockOnInput => true;
        public override bool UseCollision => true;

        // 락온 플릭 전환용 마우스 X 델타 누적치
        private float _flickAccum;

        public InGameCameraBehavior()
        {
            AddModifier(new RotationTransitionCameraModifier());        // 100
            AddModifier(new LockOnCameraModifier());                    // 200
            AddModifier(new AlignCameraModifier());                     // 300
            AddModifier(new OffsetCameraModifier());                    // 400 (LookAhead 포함)
            AddModifier(new DistanceFovCameraModifier());               // 500
            AddModifier(new EffectRotationInjectCameraModifier());      // 600
            AddModifier(new LockOnReleaseSmoothingCameraModifier());    // 660
            AddModifier(new LockOnFitDistanceCameraModifier());         // 670 (상단·공중 대상 거리 피팅)
            AddModifier(new FollowCameraModifier());                    // 700
            AddModifier(new CollisionCameraModifier());                 // 800
            AddModifier(new EffectPositionFovCameraModifier());         // 850
        }

        public override void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            base.OnEnter(context, enterParams);

            if (context?.Settings == null || context.State == null)
                return;

            bool isLockOn = context.LockOn?.IsActive ?? false;
            bool isCombat = context.CombatStateProvider?.Invoke() ?? false;

            // 대화/연출 모드가 공유 서브시스에 남긴 FOV·충돌 암 보간 상태를
            // 플레이 카메라로 가져오지 않는다. 재진입 첫 프레임을 현재 플레이 상태로 스냅한다.
            context.DistanceController?.SnapFOV(isLockOn, isCombat, context.Motion);
            float desiredDistance = Mathf.Clamp(
                context.State.TargetDistance,
                context.Settings.minDistance,
                context.Settings.maxDistance);
            context.Collision?.ResetDistance(desiredDistance);
        }

        public override void HandleInput(CameraContext context, float deltaTime)
        {
            if (context?.Settings == null || context.State == null) return;

            // 입력이 처리되지 않는 프레임에는 플릭 누적치를 남기지 않는다.
            // 잔존 누적치가 있으면 팝업/메뉴 복귀 직후 미세한 이동만으로 대상 전환이 발동할 수 있다.
            ICameraRuntimeAdapter input = CameraRuntimeServices.Adapter;
            if (!input.IsGameplayInputActive ||
                Cursor.visible || context.IsInputLocked)
            {
                _flickAccum = 0f;
                return;
            }

            CameraState state = context.State;
            bool isLockOn = context.LockOn?.IsActive ?? false;

            if (!isLockOn && !context.IsAligning)
            {
                if (input.TryGetPlayerAction(CameraRuntimeServices.LookAction, out InputAction lookAction))
                {
                    Vector2 look = lookAction.ReadValue<Vector2>();
                    ResolveLookSettings(out float sensitivityX, out float sensitivityY, out bool invertY);

                    // 마우스 delta는 프레임당 픽셀 누적값이라 그대로 적용(시간 비의존), 게임패드 스틱은
                    // 정규화 축(-1~1)이므로 각속도(°/s)로 적분해야 프레임레이트·디바이스 독립이 된다.
                    // 마우스가 히트스톱 중에도 동작하는 것과 맞추기 위해 unscaledDeltaTime을 쓴다.
                    bool isGamepadLook = lookAction.activeControl?.device is Gamepad;
                    float yawDelta, pitchDelta;
                    if (isGamepadLook)
                    {
                        float dt = Time.unscaledDeltaTime;
                        yawDelta = look.x * context.Settings.gamepadYawSpeed * sensitivityX * dt;
                        pitchDelta = look.y * context.Settings.gamepadPitchSpeed * sensitivityY * dt;
                    }
                    else
                    {
                        float rotationUnit = context.Settings.rotationSpeed * 0.01f;
                        yawDelta = look.x * rotationUnit * sensitivityX;
                        pitchDelta = look.y * rotationUnit * sensitivityY;
                    }

                    state.CurrentYaw += yawDelta;
                    state.CurrentPitch -= pitchDelta * (invertY ? -1f : 1f);
                    state.CurrentPitch = Mathf.Clamp(
                        state.CurrentPitch,
                        context.Settings.minVerticalAngle,
                        context.Settings.maxVerticalAngle);
                    if (look.sqrMagnitude > 0.0001f)
                        context.NotifyManualCameraInput?.Invoke();
                }
            }

            // 락온 중에는 마우스 Look 델타가 카메라 회전에 쓰이지 않으므로, 좌우 플릭을 대상 전환 입력으로 사용한다.
            if (isLockOn)
                UpdateLockOnFlickSwitch(context, input);
            else
                _flickAccum = 0f;

            if (input.TryGetPlayerAction(CameraRuntimeServices.ZoomAction, out InputAction zoomAction))
            {
                float scroll = zoomAction.ReadValue<Vector2>().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    state.TargetDistance -= scroll * context.Settings.zoomSpeed;
                    state.TargetDistance = Mathf.Clamp(
                        state.TargetDistance,
                        context.Settings.minDistance,
                        context.Settings.maxDistance);
                }
            }
        }

        /// <summary>
        /// 락온 중 마우스 좌우 플릭으로 대상을 전환한다.
        /// 프레임당 X 델타를 누적하고, 임계치 도달 시 그 방향의 대상으로 전환한다.
        /// 누적치는 시간에 따라 감쇠하므로 느린 마우스 이동으로는 발동하지 않는다.
        /// </summary>
        private void UpdateLockOnFlickSwitch(CameraContext context, ICameraRuntimeAdapter input)
        {
            if (context.LockOn == null || !context.Settings.lockOnMouseFlickSwitch)
                return;
            if (!input.TryGetPlayerAction(CameraRuntimeServices.LookAction, out InputAction lookAction))
                return;

            // 게임패드 우스틱 좌우는 LockOnSwitchLeft/Right 액션 바인딩이 별도로 처리한다.
            if (lookAction.activeControl?.device is Gamepad)
            {
                _flickAccum = 0f;
                return;
            }

            _flickAccum += lookAction.ReadValue<Vector2>().x;
            _flickAccum = Mathf.MoveTowards(
                _flickAccum, 0f, context.Settings.lockOnFlickDecay * Time.unscaledDeltaTime);

            if (Mathf.Abs(_flickAccum) >= context.Settings.lockOnFlickThreshold)
            {
                context.LockOn.SwitchTarget(_flickAccum > 0f ? 1 : -1);
                _flickAccum = 0f;
            }
        }

        private static void ResolveLookSettings(out float sensitivityX, out float sensitivityY, out bool invertY)
        {
            CameraUserPreferences preferences = CameraRuntimeServices.Adapter.UserPreferences;
            sensitivityX = preferences.SensitivityX;
            sensitivityY = preferences.SensitivityY;
            invertY = preferences.InvertY;
        }
    }
}
