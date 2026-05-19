using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class SpecialBreakAttackEvent : MotionEventBase
    {
        public override string GetDisplayName() => "SpecialBreakAttack";

        public override string GetShortLabel() => "SpecialBreakHit";

        public override void Execute(GameObject target)
        {
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
