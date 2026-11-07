using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>영입 조우에서 사망하지 않은 적을 후속 연출 전까지 쓰러진 상태로 유지한다.</summary>
    public sealed class EnemyIncapacitatedState : EnemyActorState
    {
        public EnemyIncapacitatedState(ActorMovementController controller)
            : base(controller)
        {
        }

        public override ActorStateId StateId => ActorStateId.Incapacitated;
        public override bool BlocksBehaviorTree => true;

        public override bool CanTransitionState(ActorStateId fromState) =>
            fromState != ActorStateId.Death;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            gameActor.Animator.PlayMotion(ResolveIncapacitatedMotion(), 0.1f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
                return;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        private UPlayGround.Gameplay.Tag.GameplayTag ResolveIncapacitatedMotion()
        {
            if (gameActor.Animator.HasMotion(MotionTags.Knockdown, true))
                return MotionTags.Knockdown;
            if (gameActor.Animator.HasMotion(MotionTags.Stun, true))
                return MotionTags.Stun;
            if (gameActor.Animator.HasMotion(MotionTags.GuardBreak, true))
                return MotionTags.GuardBreak;
            if (gameActor.Animator.HasMotion(MotionTags.Hit_F, true))
                return MotionTags.Hit_F;
            return MotionTags.Idle;
        }
    }
}
