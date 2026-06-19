using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data
{
    public enum CameraSnapshotSpace
    {
        World,
        ActorRelative
    }

    public enum CameraSnapshotInterruptPolicy
    {
        Restart,
        Ignore,
        OverrideIfHigherPriority
    }

    public enum CameraSnapshotMoveType
    {
        Linear,
        OrbitAroundAnchor
    }

    public enum CameraSnapshotOrbitDirection
    {
        Shortest,
        Clockwise,
        CounterClockwise
    }

    [Serializable]
    public struct CameraSnapshotActorReference
    {
        public bool enabled;
        public bool useActivePlayerWhenEmpty;
        public ActorIdType actorIdType;
        public string actorId;
        public ActorSocketType socketType;

        public string ResolvedActorId
        {
            get
            {
                if (actorIdType != ActorIdType.None)
                    return actorIdType.ToActorId();

                return actorId;
            }
        }

        public static CameraSnapshotActorReference ActivePlayer(ActorSocketType socketType = ActorSocketType.Center)
        {
            return new CameraSnapshotActorReference
            {
                enabled = true,
                useActivePlayerWhenEmpty = true,
                actorIdType = ActorIdType.None,
                actorId = string.Empty,
                socketType = socketType
            };
        }

        public static CameraSnapshotActorReference None()
        {
            return new CameraSnapshotActorReference
            {
                enabled = false,
                useActivePlayerWhenEmpty = false,
                actorIdType = ActorIdType.None,
                actorId = string.Empty,
                socketType = ActorSocketType.None
            };
        }
    }

    [Serializable]
    public class CameraSnapshotShot
    {
        public string shotName = "Shot";
        public CameraSnapshotSpace space = CameraSnapshotSpace.ActorRelative;
        public Vector3 position;
        public Vector3 rotationEuler;
        public float fieldOfView = 50f;
        public float duration = 1f;
        public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public CameraSnapshotMoveType moveType = CameraSnapshotMoveType.Linear;
        public CameraSnapshotOrbitDirection orbitDirection = CameraSnapshotOrbitDirection.Shortest;
        public bool keepLookAtTargetDuringBlend = true;

        public void Capture(Camera camera, Transform actorAnchor, CameraSnapshotSpace captureSpace)
        {
            if (camera == null) return;

            space = captureSpace;
            fieldOfView = camera.fieldOfView;

            if (space == CameraSnapshotSpace.ActorRelative && actorAnchor != null)
            {
                position = actorAnchor.InverseTransformPoint(camera.transform.position);
                rotationEuler = (Quaternion.Inverse(actorAnchor.rotation) * camera.transform.rotation).eulerAngles;
                return;
            }

            position = camera.transform.position;
            rotationEuler = camera.transform.rotation.eulerAngles;
        }

        public void ResolveWorldPose(Transform actorAnchor, out Vector3 worldPosition, out Quaternion worldRotation)
        {
            Quaternion localRotation = Quaternion.Euler(rotationEuler);
            if (space == CameraSnapshotSpace.ActorRelative && actorAnchor != null)
            {
                worldPosition = actorAnchor.TransformPoint(position);
                worldRotation = actorAnchor.rotation * localRotation;
                return;
            }

            worldPosition = position;
            worldRotation = localRotation;
        }
    }

    [CreateAssetMenu(fileName = "CameraSnapshotProfile", menuName = "UPlayGround/카메라/Snapshot Profile")]
    public class CameraSnapshotProfile : ScriptableObject
    {
        public string sequenceName;
        public bool useUnscaledTime = true;
        public bool restorePreviousModeOnFinish = true;
        public bool lockCameraInput = true;
        public bool releaseLockOnOnEnter = true;
        public bool applyFirstShotImmediately = true;
        public bool useCollision = false;
        public float entryBlendDuration = 0f;
        public AnimationCurve entryBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float playbackSpeed = 1f;
        public int priority = 0;
        public CameraSnapshotInterruptPolicy interruptPolicy = CameraSnapshotInterruptPolicy.Restart;
        public CameraSnapshotActorReference actorAnchor = CameraSnapshotActorReference.ActivePlayer();
        public CameraSnapshotActorReference lookAtTarget = CameraSnapshotActorReference.None();
        public List<CameraSnapshotShot> shots = new List<CameraSnapshotShot>();

        public float TotalDuration
        {
            get
            {
                float total = 0f;
                if (shots == null) return total;

                foreach (var shot in shots)
                    if (shot != null)
                        total += Mathf.Max(0f, shot.duration);

                return total;
            }
        }

        public float EffectiveTotalDuration => playbackSpeed > 0f ? TotalDuration / playbackSpeed : TotalDuration;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(sequenceName))
                sequenceName = name;

            entryBlendDuration = Mathf.Max(0f, entryBlendDuration);
            playbackSpeed = Mathf.Max(0.01f, playbackSpeed);

            if (shots == null) return;
            foreach (var shot in shots)
            {
                if (shot == null) continue;
                shot.duration = Mathf.Max(0.01f, shot.duration);
                shot.fieldOfView = Mathf.Clamp(shot.fieldOfView, 1f, 179f);
            }
        }
    }
}
