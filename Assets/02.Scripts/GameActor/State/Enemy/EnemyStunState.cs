using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemyStunState : GameActorState
    {
        public override string StateName => "Stun";
        public override bool BlocksBehaviorTree => true;

        private readonly AttackData _attackData;
        private float _remainingDuration;

        public EnemyStunState(ActorMovementController controller, AttackData attackData = null) : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _remainingDuration = _attackData?.reactionDuration > 0f ? _attackData.reactionDuration : 1.5f;
            gameActor.Animator.PlayMotion(GetStunAnimKey(), 0.15f);
        }

        public override void UpdateState(float deltaTime)
        {
            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0f)
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

        private AnimKey GetStunAnimKey()
        {
            if (gameActor.Animator.HasMotion(AnimKey.GuardBreak, true)) return AnimKey.GuardBreak;
            if (gameActor.Animator.HasMotion(AnimKey.Hit_F, true)) return AnimKey.Hit_F;
            return AnimKey.Idle;
        }
    }
}
