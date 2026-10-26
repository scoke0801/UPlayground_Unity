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
        private bool _canSwitchWithStick;

        public InGameCameraBehavior()
        {
            AddModifier(new RotationTransitionCameraModifier());        // 100
            AddModifier(new LockOnCameraModifier());                    // 200
            AddModifier(new AlignCameraModifier());                     // 300
            AddModifier(new OffsetCameraModifier());                    // 400 (LookAhead 포함)
            AddModifier(new EncounterFramingCameraModifier());
            AddModifier(new DistanceFovCameraModifier());               // 500
            AddModifier(new LockOnReleaseSmoothingCameraModifier());    // 660
            AddModifier(new FollowCameraModifier());                    // 700
            AddModifier(new LockOnFramingCameraModifier());             // 730
            AddModifier(new LockOnFitDistanceCameraModifier());         // 740
            AddModifier(new HitAssistCameraModifier());                  // 760
            AddModifier(new EffectRotationInjectCameraModifier());      // 790
            AddModifier(new CollisionCameraModifier());                 // 800
            AddModifier(new EffectPositionFovCameraModifier());         // 850
        }

        /// <summary>확정된 위치에서 대상 가시성을 갱신하고 연출 회전은 대상 선택 기준에서 제외한다.</summary>
        public override CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState)
        {
            CameraPose pose = base.EvaluatePose(context, deltaTime, effectState);
            if (context?.LockOn != null && !context.IsInputLocked && context.LookAtOverride == null
                && !(context.RotationTransition?.IsActive ?? false))
            {
                CameraPose stableView = pose;
                stableView.CameraRotation = Quaternion.AngleAxis(-effectState.yawDelta, Vector3.up)
                    * pose.CameraRotation * Quaternion.AngleAxis(-effectState.pitchDelta, Vector3.right);
                context.LockOn.SetStableView(stableView);
                context.LockOn.UpdateVisibility(deltaTime);
            }
            return pose;
        }

        public override void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            base.OnEnter(context, enterParams);
            _canSwitchWithStick = false;
            _flickAccum = 0f;

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

            ICameraRuntimeAdapter input = CameraRuntimeServices.Adapter;
            bool suspendLockOnInput = (context.LockOn?.IsActive ?? false)
                && (context.LookAtOverride != null || (context.RotationTransition?.IsActive ?? false));
            bool canAcceptViewInput = input.IsGameplayInputActive
                && !context.IsInputLocked && !suspendLockOnInput;
            if (canAcceptViewInput)
                ApplyExternalViewInput(context);

            // 입력이 처리되지 않는 프레임에는 플릭 누적치를 남기지 않는다.
            // 잔존 누적치가 있으면 팝업/메뉴 복귀 직후 미세한 이동만으로 대상 전환이 발동할 수 있다.
            if (!canAcceptViewInput || !input.IsCameraViewInputActive || Cursor.visible)
            {
                _flickAccum = 0f;
                _canSwitchWithStick = false;
                return;
            }

            bool isLockOn = context.LockOn?.IsActive ?? false;
            // 락온 대상 전환은 플레이어 조작이다. 호스트가 시점만 열어 둔 상태(도구 프리뷰)에서는 막는다.
            bool canSwitchLockOnTarget = !input.IsPlayerActionInputSuppressed;

            if (!isLockOn && !context.IsAligning
                && input.TryGetCameraViewAction(CameraRuntimeServices.LookAction, out InputAction lookAction))
            {
                Vector2 look = lookAction.ReadValue<Vector2>();
                if (lookAction.activeControl?.device is Gamepad)
                    ApplyStickLook(context, look);
                else
                    ApplyPointerLook(context, look);
            }

            // 락온 중에는 마우스 Look 델타가 카메라 회전에 쓰이지 않으므로, 좌우 플릭을 대상 전환 입력으로 사용한다.
            if (isLockOn && canSwitchLockOnTarget)
            {
                UpdateLockOnStickSwitch(context, input);
                UpdateLockOnFlickSwitch(context, input);
            }
            else
            {
                _flickAccum = 0f;
                _canSwitchWithStick = false;
            }

            if (!input.TryGetCameraViewAction(CameraRuntimeServices.ZoomAction, out InputAction zoomAction))
                return;

            float scroll = zoomAction.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) <= 0.01f)
                return;

            if (isLockOn)
            {
                // 휠은 화면의 높이가 아니라 기존 좌우 전환 순서로 대상을 고른다.
                if (canSwitchLockOnTarget)
                    context.LockOn.SwitchTarget(scroll > 0f ? 1 : -1);
                return;
            }

            ApplyZoom(context, scroll);
        }

        /// <summary>장치 밖에서 들어온 시점 입력(에디터 도구의 Game 뷰 조작 등)을 마우스와 같은 규칙으로 반영한다.</summary>
        /// <remarks>
        /// 입력을 넣은 쪽이 시점 조작 권한을 가지므로 커서 표시·플레이어 조작 차단과 무관하게 받는다.
        /// 락온 중 회전은 대상 추적이, 휠은 대상 전환이 소유하므로 락온 중에는 쓰지 않는다.
        /// </remarks>
        private static void ApplyExternalViewInput(CameraContext context)
        {
            if (context.LockOn?.IsActive ?? false)
                return;

            Vector2 look = context.ExternalViewLook;
            if (!context.IsAligning && look.sqrMagnitude > 0f)
                ApplyPointerLook(context, look);

            float zoom = context.ExternalViewZoom;
            if (Mathf.Abs(zoom) > 0.01f)
                ApplyZoom(context, zoom);
        }

        /// <summary>프레임당 픽셀 누적값인 마우스 델타를 시간과 무관하게 회전량으로 바꾼다.</summary>
        private static void ApplyPointerLook(CameraContext context, Vector2 pixelDelta)
        {
            ResolveLookSettings(out float sensitivityX, out float sensitivityY, out bool invertY);
            float rotationUnit = context.Settings.rotationSpeed * 0.01f;
            RotateView(
                context,
                pixelDelta.x * rotationUnit * sensitivityX,
                pixelDelta.y * rotationUnit * sensitivityY,
                invertY,
                pixelDelta);
        }

        /// <summary>정규화 축(-1~1)인 스틱 입력을 각속도(°/s)로 적분해 회전량으로 바꾼다.</summary>
        /// <remarks>마우스가 히트스톱 중에도 동작하는 것과 맞추기 위해 unscaledDeltaTime을 쓴다.</remarks>
        private static void ApplyStickLook(CameraContext context, Vector2 stick)
        {
            ResolveLookSettings(out float sensitivityX, out float sensitivityY, out bool invertY);
            float dt = Time.unscaledDeltaTime;
            RotateView(
                context,
                stick.x * context.Settings.gamepadYawSpeed * sensitivityX * dt,
                stick.y * context.Settings.gamepadPitchSpeed * sensitivityY * dt,
                invertY,
                stick);
        }

        private static void RotateView(
            CameraContext context,
            float yawDelta,
            float pitchDelta,
            bool invertY,
            Vector2 rawInput)
        {
            CameraState state = context.State;
            state.CurrentYaw += yawDelta;
            state.CurrentPitch -= pitchDelta * (invertY ? -1f : 1f);
            state.CurrentPitch = Mathf.Clamp(
                state.CurrentPitch,
                context.Settings.minVerticalAngle,
                context.Settings.maxVerticalAngle);
            if (rawInput.sqrMagnitude <= 0.0001f)
                return;

            context.LastManualInputTime = Time.unscaledTime;
            context.HitAssist.Reset();
            context.NotifyManualCameraInput?.Invoke();
        }

        private static void ApplyZoom(CameraContext context, float scroll)
        {
            CameraState state = context.State;
            state.TargetDistance = Mathf.Clamp(
                state.TargetDistance - scroll * context.Settings.zoomSpeed,
                context.Settings.minDistance,
                context.Settings.maxDistance);
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

        private void UpdateLockOnStickSwitch(CameraContext context, ICameraRuntimeAdapter input)
        {
            if (!input.TryGetPlayerAction(CameraRuntimeServices.LookAction, out InputAction look)
                || !(look.activeControl?.device is Gamepad))
            {
                // 중립에서는 activeControl이 null일 수 있다. 실제 값도 중립일 때만 재입력을 허용한다.
                _canSwitchWithStick = look == null || look.ReadValue<Vector2>().sqrMagnitude < 0.001f;
                return;
            }
            Vector2 direction = look.ReadValue<Vector2>();
            float trigger = Mathf.Clamp(context.Settings.lockOnStickSwitchThreshold, 0.1f, 1f);
            float reset = Mathf.Clamp(context.Settings.lockOnStickResetThreshold, 0f, trigger - 0.01f);
            if (direction.sqrMagnitude <= reset * reset)
                _canSwitchWithStick = true;
            if (!_canSwitchWithStick || direction.sqrMagnitude < trigger * trigger)
                return;
            _canSwitchWithStick = false;
            context.LockOn.SwitchTarget(direction);
        }
    }
}
