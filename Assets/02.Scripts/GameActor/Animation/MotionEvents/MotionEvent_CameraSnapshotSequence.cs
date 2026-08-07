using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UPlayGround.Data;
using UPlayGround.CameraSystem;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class CameraSnapshotSequenceEvent : MotionEventBase
    {
        [FormerlySerializedAs("ignoreWhenEnemy")]
        [Tooltip("몬스터가 이 모션을 재생할 때의 처리. 금지는 검증 오류이며 런타임에서도 차단된다.")]
        public MotionEventEnemyExecutionPolicy enemyExecutionPolicy =
            MotionEventEnemyExecutionPolicy.Ignored;
        public CameraSnapshotProfile profile;
        public bool overrideActorAnchor;
        public CameraSnapshotActorReference actorAnchor = CameraSnapshotActorReference.ActivePlayer();
        public bool overrideLookAtTarget;
        public CameraSnapshotActorReference lookAtTarget = CameraSnapshotActorReference.None();
        public bool restorePreviousOnComplete = true;

        private readonly struct ActiveSnapshotState
        {
            public readonly CameraSnapshotProfile profile;
            public readonly bool restorePreviousOnComplete;

            public ActiveSnapshotState(
                CameraSnapshotProfile profile,
                bool restorePreviousOnComplete)
            {
                this.profile = profile;
                this.restorePreviousOnComplete = restorePreviousOnComplete;
            }
        }

        [NonSerialized]
        private Dictionary<int, ActiveSnapshotState> _activeStates;

        public override string GetDisplayName() => "Camera Snapshot Sequence";

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => enemyExecutionPolicy;

        public override string GetShortLabel()
        {
            return profile != null ? $"CamSeq: {profile.sequenceName}" : "CamSeq: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (MotionEventEnemyScope.ShouldSkip(target, EnemyExecutionPolicy))
                return;

            CameraManager cameraManager = CameraManager.Instance;
            if (profile == null || cameraManager == null)
                return;

            if (!cameraManager.PushCameraSnapshotSequence(
                profile,
                overrideActorAnchor ? actorAnchor : null,
                overrideLookAtTarget ? lookAtTarget : null))
                return;

            _activeStates ??= new Dictionary<int, ActiveSnapshotState>();
            _activeStates[MotionEventEnemyScope.GetTargetKey(target)] = new ActiveSnapshotState(
                profile,
                restorePreviousOnComplete);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            int targetKey = MotionEventEnemyScope.GetTargetKey(target);
            if (_activeStates == null
                || !_activeStates.TryGetValue(targetKey, out ActiveSnapshotState state))
                return;

            _activeStates.Remove(targetKey);
            if (_activeStates.Count == 0)
                _activeStates = null;

            if (!state.restorePreviousOnComplete)
                return;

            CameraManager.Instance?.StopCameraSnapshotSequence(state.profile);
        }
    }
}
