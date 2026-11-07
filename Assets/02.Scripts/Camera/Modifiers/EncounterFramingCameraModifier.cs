using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.CameraSystem
{
    /// <summary>소탕 무리의 중심을 피벗에 반영하고 피치는 자동 보정하지 않는다(플레이어 조작 유지).</summary>
    public sealed class EncounterFramingCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private Vector3 _offset;
        private Vector3 _velocity;
        public int Priority => 710;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams) => _offset = _velocity = Vector3.zero;
        public void OnExit(CameraContext context) => _offset = _velocity = Vector3.zero;

        public void Apply(ref CameraFrame frame)
        {
            var context = frame.Context;
            if (context?.Settings == null || context.Target == null || context.IsInputLocked || frame.DeltaTime <= 0f) return;
            var kind = CameraRuntimeServices.Adapter.EncounterKind;
            var profile = context.Settings.GetEncounterProfile(kind);
            Vector3 desired = Vector3.zero;
            var distance = context.DistanceController;
            if (kind == CombatEncounterKind.Sweep && distance != null && distance.NearbyEnemyCount > 0)
                desired = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(distance.NearbyCenter - context.Target.position,
                    Vector3.up) * context.Settings.crowdPivotRatio, context.Settings.crowdPivotMaxDistance);
            desired *= CameraRuntimeServices.Adapter.UserPreferences.AutoCorrectionScale;
            _offset = Vector3.SmoothDamp(_offset, desired, ref _velocity, profile.transitionTime, Mathf.Infinity, frame.DeltaTime);
            var pose = frame.Pose;
            pose.PivotPosition += _offset;
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
            frame.Pose = pose;
        }
    }
}
