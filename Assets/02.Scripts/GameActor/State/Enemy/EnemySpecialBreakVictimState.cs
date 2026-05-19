using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemySpecialBreakVictimState : GameActorState
    {
        public override string StateName => "SpecialBreakVictim";
        public override bool BlocksBehaviorTree => true;

        private readonly float _duration;
        private float _remainingDuration;

        public EnemySpecialBreakVictimState(ActorMovementController controller, float duration = 1.2f) : base(controller)
        {
            _duration = Mathf.Max(0.1f, duration);
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _remainingDuration = _duration;

            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.Grabbed, true)
                ? AnimKey.Grabbed
                : AnimKey.Hit_F;
            gameActor.Animator.PlayMotion(animKey, 0.1f);
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
    }
}
