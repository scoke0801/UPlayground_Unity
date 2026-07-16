using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    [Serializable]
    [MotionEventMeta("SpecialBreakAttack", Category = "Combat", CategoryOrder = 0,
        Description = "브레이크 특수공격 피해 적용 타이밍을 발생시킵니다.",
        Aliases = new[] { "break", "special", "groggy", "브레이크", "특수공격" },
        Icon = "◆", Color = new[] { 1.00f, 0.35f, 0.35f })]
    public class SpecialBreakAttackEvent : MotionEventBase
    {
        public override string GetDisplayName() => "SpecialBreakAttack";

        public override string GetShortLabel() => "SpecialBreakHit";

        public override void Execute(GameObject target)
        {
            var residualTarget = target.GetComponent<ISpecialBreakAttackMotionEventTarget>()
                                 ?? target.GetComponentInParent<ISpecialBreakAttackMotionEventTarget>()
                                 ?? target.GetComponentInChildren<ISpecialBreakAttackMotionEventTarget>();
            if (residualTarget != null)
            {
                Debug.Log($"[ResidualAttack] MotionEvent SpecialBreak route. target={target.name}, handler={residualTarget.GetType().Name}");
                residualTarget.ApplySpecialBreakAttackFromMotionEvent();
                return;
            }

            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Player))
                return;

            var controller = actor.GetComponent<ActorMovementController>();
            if (controller?.CurrentState is PlayerSpecialBreakAttackState state)
                state.ApplySpecialBreakAttackFromMotionEvent();
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}
