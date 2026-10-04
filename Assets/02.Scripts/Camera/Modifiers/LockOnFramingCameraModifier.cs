using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>피벗 추적 후, 연출 합성 전에 락온 화면 데드존을 적용한다.</summary>
    public sealed class LockOnFramingCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private readonly CameraDeadZoneTracker _tracker = new CameraDeadZoneTracker();
        private readonly CameraObstructionFraming _obstruction = new CameraObstructionFraming();
        private Transform _previousTarget;

        public int Priority => 750;

        /// <summary>모드 재진입 시 이전 대상의 추적 관성을 제거한다.</summary>
        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams) => Reset();

        /// <summary>연출 모드로 넘어갈 때 추적을 중단한다.</summary>
        public void OnExit(CameraContext context) => Reset();

        /// <summary>충돌로 줄어든 거리를 고려하여 흔들림 없는 구도를 보정한다.</summary>
        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.LockOn == null || context.Settings == null)
                return;

            CameraPose stablePose = frame.Pose;
            float desiredDistance = stablePose.Distance;
            stablePose.Distance = Mathf.Min(desiredDistance, context.Collision?.CurrentDistance ?? desiredDistance);
            if (context.Collision != null)
                stablePose.Distance = Mathf.Min(stablePose.Distance, context.Collision.GetAvailableDistance(
                    stablePose.PivotPosition, stablePose.CameraRotation * Vector3.back, desiredDistance));
            stablePose.CameraPosition = stablePose.PivotPosition
                                        + stablePose.CameraRotation * Vector3.back * stablePose.Distance;
            stablePose.FieldOfView = context.DistanceController?.BaseFOV ?? context.Settings.fovLockOn;
            // 가림 중에도 충돌로 당겨진 카메라를 반영해야 벽 앞에서 시야가 복구된 것을 감지한다.
            context.LockOn.SetStableView(stablePose);
            if (!context.LockOn.CanTrack || context.MainCamera == null || context.Target == null
                || context.IsInputLocked || context.LookAtOverride != null
                || (context.RotationTransition?.IsActive ?? false))
            {
                Reset();
                return;
            }

            if (_previousTarget != context.LockOn.CurrentTarget)
            {
                _tracker.Reset();
                _obstruction.Reset();
                _previousTarget = context.LockOn.CurrentTarget;
            }

            if (_obstruction.TryTrack(ref stablePose, context, desiredDistance, frame.DeltaTime))
                _tracker.Reset();
            else
                _tracker.Track(ref stablePose, context.LockOn.FocusPosition, context.Settings,
                    context.MainCamera.aspect, frame.DeltaTime,
                    context.LockOn.CurrentTarget.position - context.Target.position, frame.LockOnFramingPitch);
            if (context.Collision != null)
                stablePose.Distance = Mathf.Min(stablePose.Distance, context.Collision.GetAvailableDistance(
                    stablePose.PivotPosition, stablePose.CameraRotation * Vector3.back, desiredDistance));
            stablePose.CameraPosition = stablePose.PivotPosition
                + stablePose.CameraRotation * Vector3.back * stablePose.Distance;
            context.LockOn.SetStableView(stablePose);

            frame.State.CurrentYaw = stablePose.Yaw;
            frame.State.CurrentPitch = stablePose.Pitch;
            stablePose.Distance = desiredDistance;
            stablePose.CameraPosition = stablePose.PivotPosition
                                        + stablePose.CameraRotation * Vector3.back * desiredDistance;
            frame.Pose = stablePose;
        }

        private void Reset()
        {
            _tracker.Reset();
            _obstruction.Reset();
            _previousTarget = null;
        }
    }
}
