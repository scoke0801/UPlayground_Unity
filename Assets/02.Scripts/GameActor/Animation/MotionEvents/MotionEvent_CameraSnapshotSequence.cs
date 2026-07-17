using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Data;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class CameraSnapshotSequenceEvent : MotionEventBase
    {
        public CameraSnapshotProfile profile;
        public bool overrideActorAnchor;
        public CameraSnapshotActorReference actorAnchor = CameraSnapshotActorReference.ActivePlayer();
        public bool overrideLookAtTarget;
        public CameraSnapshotActorReference lookAtTarget = CameraSnapshotActorReference.None();
        public bool restorePreviousOnComplete = true;

        public override string GetDisplayName() => "Camera Snapshot Sequence";

        public override string GetShortLabel()
        {
            return profile != null ? $"CamSeq: {profile.sequenceName}" : "CamSeq: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (profile == null || CameraManager.Instance == null) return;

            CameraManager.Instance.PushCameraSnapshotSequence(
                profile,
                overrideActorAnchor ? actorAnchor : null,
                overrideLookAtTarget ? lookAtTarget : null);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (!restorePreviousOnComplete) return;

            CameraManager.Instance?.StopCameraSnapshotSequence(profile);
        }
    }
}
