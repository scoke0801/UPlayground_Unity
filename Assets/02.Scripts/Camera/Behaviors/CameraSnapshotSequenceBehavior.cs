using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    public class CameraSnapshotSequenceBehavior : ICameraBehavior
    {
        private CameraSnapshotProfile _profile;
        private CameraSnapshotActorReference _actorAnchor;
        private CameraSnapshotActorReference _lookAtTarget;
        private int _shotIndex;
        private float _shotElapsed;
        private CameraPose _fromPose;
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
        public bool RequiresPrimaryTarget => false;
        public CameraSnapshotProfile ActiveProfile => _profile;
        public int ActivePriority => _profile != null ? _profile.priority : 0;
        public bool IsCompleted => _completed;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            _profile = enterParams.SnapshotProfile;
            _actorAnchor = enterParams.HasSnapshotActorAnchorOverride
                ? enterParams.SnapshotActorAnchor
                : _profile != null
                    ? _profile.actorAnchor
                    : CameraSnapshotActorReference.ActivePlayer();
            _lookAtTarget = enterParams.HasSnapshotLookAtTargetOverride
                ? enterParams.SnapshotLookAtTarget
                : _profile != null
                    ? _profile.lookAtTarget
                    : CameraSnapshotActorReference.None();
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
                _fromPose = CameraPose.FromCamera(
                    context.MainCamera,
                    context.CameraPivot,
                    context.State.CurrentYaw,
                    context.State.CurrentPitch,
                    context.State.TargetDistance);
                _hasPose = true;
            }
        }

        public void OnExit(CameraContext context)
        {
            context.IsInputLocked = false;
        }

        public void HandleInput(CameraContext context, float deltaTime)
        {
        }

        public CameraPose EvaluatePose(CameraContext context, float deltaTime, CameraEffectState effectState)
        {
            if (_profile == null || _profile.shots == null || _profile.shots.Count == 0 || context.MainCamera == null)
                return CameraPose.FromCamera(context.MainCamera, context.CameraPivot, context.State.CurrentYaw, context.State.CurrentPitch, context.State.TargetDistance);

            float dt = (_profile.useUnscaledTime ? Time.unscaledDeltaTime : deltaTime) * Mathf.Max(0.01f, _profile.playbackSpeed);

            EnsureInitialPose(context, effectState);

            if (_isEntryBlending)
                return EvaluateEntryBlend(context, effectState, dt);

            _shotElapsed += Mathf.Max(0f, dt);
            AdvanceCompletedShots(context, effectState);

            if (_completed)
                return CameraPose.FromCamera(context.MainCamera, context.CameraPivot, context.State.CurrentYaw, context.State.CurrentPitch, context.State.TargetDistance);

            CameraSnapshotShot targetShot = _profile.shots[Mathf.Clamp(_shotIndex, 0, _profile.shots.Count - 1)];
            float duration = Mathf.Max(0.01f, targetShot.duration);
            float rawT = Mathf.Clamp01(_shotElapsed / duration);
            if (_profile.applyFirstShotImmediately && _shotIndex == 0)
                rawT = 1f;

            float t = targetShot.blendCurve != null ? Mathf.Clamp01(targetShot.blendCurve.Evaluate(rawT)) : rawT;
            CameraPose toPose = BuildPoseFromShot(context, targetShot, effectState);
            return LerpPose(_fromPose, toPose, targetShot, t);
        }

        private CameraPose EvaluateEntryBlend(CameraContext context, CameraEffectState effectState, float deltaTime)
        {
            CameraSnapshotShot firstShot = _profile.shots[0];
            CameraPose toPose = BuildPoseFromShot(context, firstShot, effectState);
            float duration = Mathf.Max(0.01f, _profile.entryBlendDuration);
            _entryBlendElapsed += Mathf.Max(0f, deltaTime);

            float rawT = Mathf.Clamp01(_entryBlendElapsed / duration);
            float t = _profile.entryBlendCurve != null ? Mathf.Clamp01(_profile.entryBlendCurve.Evaluate(rawT)) : rawT;
            CameraPose pose = LerpPose(_fromPose, toPose, firstShot, t);

            if (rawT >= 1f)
            {
                _isEntryBlending = false;
                _shotElapsed = 0f;
                _fromPose = toPose;
            }

            return pose;
        }

        private void EnsureInitialPose(CameraContext context, CameraEffectState effectState)
        {
            if (_hasPose)
                return;

            CameraSnapshotShot firstShot = _profile.shots[0];
            _fromPose = _profile.applyFirstShotImmediately
                ? BuildPoseFromShot(context, firstShot, effectState)
                : CameraPose.FromCamera(
                    context.MainCamera,
                    context.CameraPivot,
                    context.State.CurrentYaw,
                    context.State.CurrentPitch,
                    context.State.TargetDistance);
            _hasPose = true;
        }

        private void AdvanceCompletedShots(CameraContext context, CameraEffectState effectState)
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

        private CameraPose BuildPoseFromShot(CameraContext context, CameraSnapshotShot shot, CameraEffectState effectState)
        {
            Transform actorAnchor = CameraSnapshotActorReferenceResolver.Resolve(_actorAnchor, context.Target);
            Transform lookAtTarget = CameraSnapshotActorReferenceResolver.Resolve(_lookAtTarget);

            shot.ResolveWorldPose(actorAnchor, out Vector3 position, out Quaternion rotation);

            if (lookAtTarget != null)
            {
                Vector3 lookDir = lookAtTarget.position - position;
                if (lookDir.sqrMagnitude > 0.001f)
                    rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            }

            Vector3 pivotPosition = actorAnchor != null ? actorAnchor.position : position;
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
            return new CameraPose
            {
                PivotPosition = pivotPosition,
                CameraPosition = position,
                CameraRotation = rotation,
                Yaw = euler.y,
                Pitch = euler.x,
                Distance = actorAnchor != null ? Vector3.Distance(pivotPosition, position) + effectState.distanceDelta : 0f,
                FieldOfView = shot.fieldOfView + effectState.fovDelta
            };
        }

        private CameraPose LerpPose(CameraPose from, CameraPose to, CameraSnapshotShot targetShot, float t)
        {
            if (targetShot != null && targetShot.moveType == CameraSnapshotMoveType.OrbitAroundAnchor)
                return OrbitPose(from, to, targetShot, t);

            return LerpLinearPose(from, to, t);
        }

        private static CameraPose LerpLinearPose(CameraPose from, CameraPose to, float t)
        {
            return new CameraPose
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

        private CameraPose OrbitPose(CameraPose from, CameraPose to, CameraSnapshotShot targetShot, float t)
        {
            Vector3 center = ResolveOrbitCenter(to);
            Vector3 fromOffset = from.CameraPosition - center;
            Vector3 toOffset = to.CameraPosition - center;

            Vector2 fromPlanar = new Vector2(fromOffset.x, fromOffset.z);
            Vector2 toPlanar = new Vector2(toOffset.x, toOffset.z);

            if (fromPlanar.sqrMagnitude < 0.0001f || toPlanar.sqrMagnitude < 0.0001f)
                return LerpLinearPose(from, to, t);

            float fromAngle = Mathf.Atan2(fromPlanar.x, fromPlanar.y) * Mathf.Rad2Deg;
            float toAngle = Mathf.Atan2(toPlanar.x, toPlanar.y) * Mathf.Rad2Deg;
            float deltaAngle = ResolveOrbitDelta(fromAngle, toAngle, targetShot.orbitDirection);
            float angle = (fromAngle + deltaAngle * t) * Mathf.Deg2Rad;

            float radius = Mathf.Lerp(fromPlanar.magnitude, toPlanar.magnitude, t);
            float height = Mathf.Lerp(fromOffset.y, toOffset.y, t);

            Vector3 cameraPosition = center + new Vector3(
                Mathf.Sin(angle) * radius,
                height,
                Mathf.Cos(angle) * radius);

            Quaternion rotation = Quaternion.Slerp(from.CameraRotation, to.CameraRotation, t);
            if (targetShot.keepLookAtTargetDuringBlend)
            {
                Vector3 lookDirection = center - cameraPosition;
                if (lookDirection.sqrMagnitude > 0.001f)
                    rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }

            Vector3 euler = rotation.eulerAngles;
            return new CameraPose
            {
                PivotPosition = Vector3.Lerp(from.PivotPosition, to.PivotPosition, t),
                CameraPosition = cameraPosition,
                CameraRotation = rotation,
                Yaw = euler.y,
                Pitch = euler.x,
                Distance = Vector3.Distance(center, cameraPosition),
                FieldOfView = Mathf.Lerp(from.FieldOfView, to.FieldOfView, t)
            };
        }

        private Vector3 ResolveOrbitCenter(CameraPose to)
        {
            Transform lookAtTarget = CameraSnapshotActorReferenceResolver.Resolve(_lookAtTarget);
            if (lookAtTarget != null)
                return lookAtTarget.position;

            Transform actorAnchor = CameraSnapshotActorReferenceResolver.Resolve(_actorAnchor);
            if (actorAnchor != null)
                return actorAnchor.position;

            return to.PivotPosition;
        }

        private static float ResolveOrbitDelta(float fromAngle, float toAngle, CameraSnapshotOrbitDirection direction)
        {
            float delta = Mathf.DeltaAngle(fromAngle, toAngle);
            switch (direction)
            {
                case CameraSnapshotOrbitDirection.Clockwise:
                    if (delta < 0f)
                        delta += 360f;
                    break;
                case CameraSnapshotOrbitDirection.CounterClockwise:
                    if (delta > 0f)
                        delta -= 360f;
                    break;
                case CameraSnapshotOrbitDirection.Shortest:
                default:
                    break;
            }

            return delta;
        }
    }
}
