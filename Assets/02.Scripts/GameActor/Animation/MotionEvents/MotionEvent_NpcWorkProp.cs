using System;
using UnityEngine;
using UPlayGround.Components.LivingWorld;

namespace UPlayGround.Data.Event
{
    /// <summary>평가된 손의 접촉 프레임에서 일과가 예약한 실물 도구를 집거나 내려놓는다.</summary>
    [Serializable]
    [MotionEventDescriptor("NpcWorkProp", "Utility", 0, "예약된 현장 소품을 접촉 프레임에 인계합니다.", "NPC", "소품", "접촉")]
    public sealed class NpcWorkPropEvent : MotionEventBase
    {
        public bool take = true;
        public override bool RequiresPostEvaluation => true;
        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => take ? "작업 도구 집기" : "작업 도구 놓기";
        public override void Execute(GameObject target) => NpcRoutine.TransferSceneProp(target, take);
        public override void OnCompleteEvent(GameObject target) { }
    }
}
