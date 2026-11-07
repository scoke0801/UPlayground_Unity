using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemyStunState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Stun;
        public override bool BlocksBehaviorTree => true;

        private readonly HitContext _hit;
        private float _remainingDuration;

        public EnemyStunState(ActorMovementController controller, in HitContext hit = default) : base(controller)
        {
            _hit = hit;
        }

        public override bool CanTransitionState(ActorStateId fromState)
            => fromState is not (ActorStateId.Death
                or ActorStateId.Grabbed
                or ActorStateId.SpecialBreakVictim);

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _remainingDuration = _hit.ReactionDuration > 0f ? _hit.ReactionDuration : 2.5f;
            gameActor.Animator.PlayMotion(GetStunAnimKey(), 0.15f);
        }

        public override void UpdateState(float deltaTime)
        {
            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0f)
                controller.TransitionToState(ActorStateId.Idle);
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

        private UPlayGround.Gameplay.Tag.GameplayTag GetStunAnimKey()
        {
            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Stun, true)) return UPlayGround.Data.Actor.Animation.MotionTags.Stun;
            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak, true)) return UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak;
            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Hit_F, true)) return UPlayGround.Data.Actor.Animation.MotionTags.Hit_F;
            return UPlayGround.Data.Actor.Animation.MotionTags.Idle;
        }
    }
}
