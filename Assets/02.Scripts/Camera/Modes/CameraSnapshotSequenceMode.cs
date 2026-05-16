using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    public class CameraSnapshotSequenceMode : ICameraMode
    {
        private CameraSnapshotProfile _profile;
        private Transform _actorAnchor;
        private Transform _lookAtTarget;
        private int _shotIndex;
        private float _shotElapsed;
        private CameraRigPose _fromPose;
        private bool _hasPose;
        private bool _completed;
        private bool _isEntryBlending;
        private float _entryBlendElapsed;

        public CameraModeType ModeType => CameraModeType.CameraSnapshotSequence;
        public int Priority => 100;
        public bool AllowsPlayerLookInput => false;
        public bool AllowsZoomInput => false;
        public bool AllowsLockOnInput => false;
        public bool UseCollision => false;
        public CameraSnapshotProfile ActiveProfile => _profile;
        public int ActivePriority => _profile != null ? _profile.priority : 0;
        public bool IsCompleted => _completed;

        public void OnEnter(CameraRuntimeContext context, CameraModeEnterParams enterParams)
        {
            _profile = enterParams.SnapshotProfile;
            _actorAnchor = enterParams.PrimaryTarget != null ? enterParams.PrimaryTarget : context.Target;
            _lookAtTarget = enterParams.SecondaryTarget;
            _shotIndex = 0;
            _shotElapsed = 0f;
            _completed = false;
            _hasPose = false;
            _entryBlendElapsed = 0f;
            _isEntryBlending = _profile != null
                               && !_profile.applyFirstShotImmediately
                               && _profile.entryBlendDuration > 0f;

            context.IsInputLocked = _profile == null || _profile.lockCameraInput;
            if (_profile == null || _profile.releaseLockOnOnEnter)
                context.LockOn?.Release();

            if (context.MainCamera != null && context.CameraPivot != null)
            {
                _fromPose = CameraRigPose.FromCamera(
                    context.MainCamera,
                    context.CameraPivot,
                    context.State.CurrentYaw,
                    context.State.CurrentPitch,
                    context.State.TargetDistance);
                _hasPose = true;
            }
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
            if (_profile == null || _profile.shots == null || _profile.shots.Count == 0 || context.MainCamera == null)
                return CameraRigPose.FromCamera(context.MainCamera, context.CameraPivot, context.State.CurrentYaw, context.State.CurrentPitch, context.State.TargetDistance);

            float dt = _profile.useUnscaledTime ? Time.unscaledDeltaTime : deltaTime;

            EnsureInitialPose(context, effectState);

            if (_isEntryBlending)
                return EvaluateEntryBlend(context, effectState, dt);

            _shotElapsed += Mathf.Max(0f, dt);
            AdvanceCompletedShots(context, effectState);

            if (_completed)
                return CameraRigPose.FromCamera(context.MainCamera, context.CameraPivot, context.State.CurrentYaw, context.State.CurrentPitch, context.State.TargetDistance);

            CameraSnapshotShot targetShot = _profile.shots[Mathf.Clamp(_shotIndex, 0, _profile.shots.Count - 1)];
            float duration = Mathf.Max(0.01f, targetShot.duration);
            float rawT = Mathf.Clamp01(_shotElapsed / duration);
            if (_profile.applyFirstShotImmediately && _shotIndex == 0)
                rawT = 1f;

            float t = targetShot.blendCurve != null ? Mathf.Clamp01(targetShot.blendCurve.Evaluate(rawT)) : rawT;
            CameraRigPose toPose = BuildPoseFromShot(context, targetShot, effectState);
            return LerpPose(_fromPose, toPose, t);
        }

        private CameraRigPose EvaluateEntryBlend(CameraRuntimeContext context, CameraEffectState effectState, float deltaTime)
        {
            CameraSnapshotShot firstShot = _profile.shots[0];
            CameraRigPose toPose = BuildPoseFromShot(context, firstShot, effectState);
            float duration = Mathf.Max(0.01f, _profile.entryBlendDuration);
            _entryBlendElapsed += Mathf.Max(0f, deltaTime);

            float rawT = Mathf.Clamp01(_entryBlendElapsed / duration);
            float t = _profile.entryBlendCurve != null ? Mathf.Clamp01(_profile.entryBlendCurve.Evaluate(rawT)) : rawT;
            CameraRigPose pose = LerpPose(_fromPose, toPose, t);

            if (rawT >= 1f)
            {
                _isEntryBlending = false;
                _shotElapsed = 0f;
                _fromPose = toPose;
            }

            return pose;
        }

        private void EnsureInitialPose(CameraRuntimeContext context, CameraEffectState effectState)
        {
            if (_hasPose)
                return;

            CameraSnapshotShot firstShot = _profile.shots[0];
            _fromPose = _profile.applyFirstShotImmediately
                ? BuildPoseFromShot(context, firstShot, effectState)
                : CameraRigPose.FromCamera(
                    context.MainCamera,
                    context.CameraPivot,
                    context.State.CurrentYaw,
                    context.State.CurrentPitch,
                    context.State.TargetDistance);
            _hasPose = true;
        }

        private void AdvanceCompletedShots(CameraRuntimeContext context, CameraEffectState effectState)
        {
            while (!_completed && _shotIndex < _profile.shots.Count)
            {
                CameraSnapshotShot currentShot = _profile.shots[_shotIndex];
                float duration = Mathf.Max(0.01f, currentShot.duration);
                if (_shotElapsed < duration)
                    return;

                _shotElapsed -= duration;
                _fromPose = BuildPoseFromShot(context, currentShot, effectState);
                _shotIndex++;

                if (_shotIndex < _profile.shots.Count)
                    continue;

                _completed = true;
                context.ActiveEnterParams?.OnComplete?.Invoke();
                if (_profile.restorePreviousModeOnFinish)
                    context.PopCameraMode?.Invoke(CameraModeEnterParams.Empty);
            }
        }

        private CameraRigPose BuildPoseFromShot(CameraRuntimeContext context, CameraSnapshotShot shot, CameraEffectState effectState)
        {
            shot.ResolveWorldPose(_actorAnchor, out Vector3 position, out Quaternion rotation);

            if (_lookAtTarget != null)
            {
                Vector3 lookDir = _lookAtTarget.position - position;
                if (lookDir.sqrMagnitude > 0.001f)
                    rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            }

            Vector3 pivotPosition = _actorAnchor != null ? _actorAnchor.position : position;
            if (_profile != null && _profile.useCollision && context.Collision != null)
            {
                Vector3 cameraOffset = position - pivotPosition;
                float desiredDistance = cameraOffset.magnitude;
                if (desiredDistance > 0.001f)
                {
                    Vector3 cameraDirection = cameraOffset / desiredDistance;
                    float resolvedDistance = context.Collision.Evaluate(pivotPosition, cameraDirection, desiredDistance);
                    position = pivotPosition + cameraDirection * resolvedDistance;
                }
            }

            position += effectState.positionDelta;
            rotation = Quaternion.Euler(effectState.pitchDelta, effectState.yawDelta, 0f) * rotation;

            Vector3 euler = rotation.eulerAngles;
            return new CameraRigPose
            {
                PivotPosition = pivotPosition,
                CameraPosition = position,
                CameraRotation = rotation,
                Yaw = euler.y,
                Pitch = euler.x,
                Distance = _actorAnchor != null ? Vector3.Distance(pivotPosition, position) + effectState.distanceDelta : 0f,
                FieldOfView = shot.fieldOfView + effectState.fovDelta
            };
        }

        private static CameraRigPose LerpPose(CameraRigPose from, CameraRigPose to, float t)
        {
            return new CameraRigPose
            {
                PivotPosition = Vector3.Lerp(from.PivotPosition, to.PivotPosition, t),
                CameraPosition = Vector3.Lerp(from.CameraPosition, to.CameraPosition, t),
                CameraRotation = Quaternion.Slerp(from.CameraRotation, to.CameraRotation, t),
                Yaw = Mathf.LerpAngle(from.Yaw, to.Yaw, t),
                Pitch = Mathf.LerpAngle(from.Pitch, to.Pitch, t),
                Distance = Mathf.Lerp(from.Distance, to.Distance, t),
                FieldOfView = Mathf.Lerp(from.FieldOfView, to.FieldOfView, t)
            };
        }
    }
}
