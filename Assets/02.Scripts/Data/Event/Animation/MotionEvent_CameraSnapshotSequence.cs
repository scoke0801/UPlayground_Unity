using System;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class CameraSnapshotSequenceEvent : MotionEventBase
    {
        public CameraSnapshotProfile profile;
        public string actorAnchorName;
        public string lookAtTargetName;
        public bool restorePreviousOnComplete = true;

        public override string GetDisplayName() => "Camera Snapshot Sequence";

        public override string GetShortLabel()
        {
            return profile != null ? $"CamSeq: {profile.sequenceName}" : "CamSeq: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (profile == null || CameraManager.Instance == null) return;

            Transform actorAnchor = ResolveTransform(target, actorAnchorName);
            if (actorAnchor == null && target != null)
                actorAnchor = target.transform;

            Transform lookAtTarget = ResolveTransform(target, lookAtTargetName);
            CameraManager.Instance.PushCameraSnapshotSequence(profile, actorAnchor, lookAtTarget);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (!restorePreviousOnComplete) return;

            CameraManager.Instance?.StopCameraSnapshotSequence(profile);
        }

        private static Transform ResolveTransform(GameObject target, string transformName)
        {
            if (target == null || string.IsNullOrEmpty(transformName))
                return null;

            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            foreach (var tr in transforms)
            {
                if (tr.name == transformName)
                    return tr;
            }

            return null;
        }
    }
}
