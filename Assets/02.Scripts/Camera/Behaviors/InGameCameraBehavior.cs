using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

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

        public InGameCameraBehavior()
        {
            AddModifier(new RotationTransitionCameraModifier());        // 100
            AddModifier(new LockOnCameraModifier());                    // 200
            AddModifier(new AlignCameraModifier());                     // 300
            AddModifier(new OffsetCameraModifier());                    // 400 (LookAhead 포함)
            AddModifier(new DistanceFovCameraModifier());               // 500
            AddModifier(new EffectRotationInjectCameraModifier());      // 600
            AddModifier(new PitchClampCameraModifier());                // 650
            AddModifier(new LockOnReleaseSmoothingCameraModifier());    // 660
            AddModifier(new FollowCameraModifier());                    // 700
            AddModifier(new CollisionCameraModifier());                 // 800
            AddModifier(new EffectPositionFovCameraModifier());         // 850
        }

        public override void HandleInput(CameraContext context, float deltaTime)
        {
            if (context?.Settings == null || context.State == null) return;
            if (InputManager.Instance.CurrentLayer != InputLayer.Level_0) return;
            if (Cursor.visible || context.IsInputLocked) return;

            var input = InputManager.Instance;
            if (input == null) return;

            CameraState state = context.State;
            bool isLockOn = context.LockOn?.IsActive ?? false;

            if (!isLockOn && !context.IsAligning)
            {
                if (input.GetAction(InputMapNames.PlayerAction, PlayerAction.Look, out InputAction lookAction))
                {
                    Vector2 look = lookAction.ReadValue<Vector2>();
                    ResolveLookSettings(out float sensitivityX, out float sensitivityY, out bool invertY);

                    float rotationUnit = context.Settings.rotationSpeed * 0.01f;
                    state.CurrentYaw += look.x * rotationUnit * sensitivityX;
                    state.CurrentPitch -= look.y * rotationUnit * sensitivityY * (invertY ? -1f : 1f);
                    if (look.sqrMagnitude > 0.0001f)
                        context.NotifyManualCameraInput?.Invoke();

                    float slopeOffset = context.ComputeSlopePitchOffset?.Invoke() ?? 0f;
                    float dynamicMin = context.Settings.minVerticalAngle + slopeOffset;
                    state.CurrentPitch = Mathf.Clamp(state.CurrentPitch, dynamicMin, context.Settings.maxVerticalAngle);
                }
            }

            if (input.GetAction(InputMapNames.PlayerAction, PlayerAction.Zoom, out InputAction zoomAction))
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

        private static void ResolveLookSettings(out float sensitivityX, out float sensitivityY, out bool invertY)
        {
            var settingsManager = SettingsManager.Instance;
            var settingsData = settingsManager != null && settingsManager.IsLoaded ? settingsManager.Data : null;

            sensitivityX = settingsData != null ? Mathf.Clamp(settingsData.sensitivityX, 1, 10) / 5f : 1f;
            sensitivityY = settingsData != null ? Mathf.Clamp(settingsData.sensitivityY, 1, 10) / 5f : 1f;
            invertY = settingsData != null && settingsData.invertY;
        }
    }
}
