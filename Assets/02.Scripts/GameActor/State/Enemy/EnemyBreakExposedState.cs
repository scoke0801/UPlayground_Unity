using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemyBreakExposedState : GameActorState
    {
        public override string StateName => "BreakExposed";
        public override bool BlocksBehaviorTree => true;

        private readonly MonsterBreakGauge _breakGauge;

        public EnemyBreakExposedState(ActorMovementController controller, MonsterBreakGauge breakGauge) : base(controller)
        {
            _breakGauge = breakGauge;
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed" or "SpecialBreakVictim";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();

            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.GuardBreak, true)
                ? AnimKey.GuardBreak
                : AnimKey.Hit_F;
            gameActor.Animator.PlayMotion(animKey, 0.15f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_breakGauge == null || !_breakGauge.IsExposed)
                controller.TransitionToState(new EnemyIdleState(controller));
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround) return;
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }
    }
}
