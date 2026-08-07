using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UPlayGround.Data.Cinematic;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>일반 MotionSet 구간에서 시전자 클론 연출 무대를 점유한다.</summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor(
        "Cinematic Stage",
        "Camera",
        0,
        "실제 액터는 제자리에 둔 채 렌더 전용 클론 무대를 시작합니다.",
        "cinematic",
        "stage",
        "연출",
        "무대")]
    public sealed class MotionEvent_CinematicStage : MotionEventBase
    {
        [FormerlySerializedAs("ignoreWhenEnemy")]
        [Tooltip("몬스터가 이 모션을 재생할 때의 처리. 금지는 검증 오류이며 런타임에서도 차단된다.")]
        public MotionEventEnemyExecutionPolicy enemyExecutionPolicy =
            MotionEventEnemyExecutionPolicy.Ignored;
        public CinematicStageSO stage;

        [NonSerialized]
        private Dictionary<int, CinematicStageTicket> _activeTickets;

        public override string GetDisplayName() => "Cinematic Stage";

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => enemyExecutionPolicy;

        public override string GetShortLabel() => stage != null
            ? $"Cinematic Stage: {stage.name}"
            : "Cinematic Stage: 미지정";

        public override void Execute(GameObject target)
        {
            if (MotionEventEnemyScope.ShouldSkip(target, EnemyExecutionPolicy))
                return;

            int targetKey = MotionEventEnemyScope.GetTargetKey(target);
            if (_activeTickets != null
                && _activeTickets.TryGetValue(targetKey, out CinematicStageTicket activeTicket))
            {
                Svc.CinematicStage?.Exit(
                    activeTicket,
                    CinematicStageExitReason.Replaced);
                _activeTickets.Remove(targetKey);
            }

            if (!CinematicStageRuntimeUtility.TryEnter(
                stage,
                target,
                target,
                null,
                out CinematicStageTicket ticket))
                return;

            _activeTickets ??= new Dictionary<int, CinematicStageTicket>();
            _activeTickets[targetKey] = ticket;
        }

        public override void OnCompleteEvent(GameObject target)
        {
            int targetKey = MotionEventEnemyScope.GetTargetKey(target);
            if (_activeTickets == null
                || !_activeTickets.TryGetValue(targetKey, out CinematicStageTicket ticket))
                return;

            _activeTickets.Remove(targetKey);
            if (_activeTickets.Count == 0)
                _activeTickets = null;

            Svc.CinematicStage?.Exit(
                ticket,
                CinematicStageExitReason.Completed);
        }
    }
}
