using System;
using System.Collections.Generic;
using UnityEngine;

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

    [CreateAssetMenu(fileName = "CameraSnapshotProfile", menuName = "UPlayGround/SO/Camera/Camera Snapshot Profile")]
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
        public int priority = 0;
        public CameraSnapshotInterruptPolicy interruptPolicy = CameraSnapshotInterruptPolicy.Restart;
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

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(sequenceName))
                sequenceName = name;

            entryBlendDuration = Mathf.Max(0f, entryBlendDuration);

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
