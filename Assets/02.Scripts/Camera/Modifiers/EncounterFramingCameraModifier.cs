using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.CameraSystem
{
    /// <summary>소탕 무리의 중심을 피벗에 반영하고 교전 진입 시에만 피치를 전환한다.</summary>
    public sealed class EncounterFramingCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private Vector3 _offset;
        private Vector3 _velocity;
        private CombatEncounterKind _previousKind;
        private float _pitchRemaining;
        private float _transitionStartedAt;
        public int Priority => 710;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            _offset = _velocity = Vector3.zero;
            _previousKind = CameraRuntimeServices.Adapter.EncounterKind;
            _pitchRemaining = context?.Settings?.GetEncounterProfile(_previousKind).transitionTime ?? 0f;
            _transitionStartedAt = Time.unscaledTime;
        }
        public void OnExit(CameraContext context) { _offset = _velocity = Vector3.zero; _pitchRemaining = 0f; }

        public void Apply(ref CameraFrame frame)
        {
            var context = frame.Context;
            if (context?.Settings == null || context.Target == null || context.IsInputLocked || frame.DeltaTime <= 0f) return;
            var kind = CameraRuntimeServices.Adapter.EncounterKind;
            var profile = context.Settings.GetEncounterProfile(kind);
            if (_previousKind != kind) { _previousKind = kind; _pitchRemaining = profile.transitionTime; _transitionStartedAt = Time.unscaledTime; }
            if (context.LastManualInputTime > _transitionStartedAt) _pitchRemaining = 0f;
            Vector3 desired = Vector3.zero;
            var distance = context.DistanceController;
            if (kind == CombatEncounterKind.Sweep && distance != null && distance.NearbyEnemyCount > 0)
                desired = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(distance.NearbyCenter - context.Target.position,
                    Vector3.up) * context.Settings.crowdPivotRatio, context.Settings.crowdPivotMaxDistance);
            desired *= CameraRuntimeServices.Adapter.UserPreferences.AutoCorrectionScale;
            _offset = Vector3.SmoothDamp(_offset, desired, ref _velocity, profile.transitionTime, Mathf.Infinity, frame.DeltaTime);
            var pose = frame.Pose;
            pose.PivotPosition += _offset;
            if (_pitchRemaining > 0f && !(context.LockOn?.IsActive ?? false))
            {
                float step = Mathf.Clamp01(frame.DeltaTime / Mathf.Max(frame.DeltaTime, _pitchRemaining));
                pose.Pitch = Mathf.LerpAngle(pose.Pitch, profile.pitch, step * CameraRuntimeServices.Adapter.UserPreferences.AutoCorrectionScale);
                frame.State.CurrentPitch = pose.Pitch;
                pose.CameraRotation = Quaternion.Euler(pose.Pitch, pose.Yaw, 0f);
                _pitchRemaining -= frame.DeltaTime;
            }
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
            frame.Pose = pose;
        }
    }
}
