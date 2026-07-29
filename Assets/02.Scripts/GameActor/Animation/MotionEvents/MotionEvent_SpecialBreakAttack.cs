using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("SpecialBreakAttack", "Combat", 0, "브레이크 특수공격 피해 적용 타이밍을 발생시킵니다.", "break", "special", "groggy", "브레이크", "특수공격")]
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
