using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerStunState : PlayerActorState
    {
        public override string StateName => "Stun";

        private readonly AttackData _attackData;
        private float _remainingDuration;

        public PlayerStunState(ActorMovementController controller, AttackData attackData = null) : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            playerActor.GetCombat()?.RefreshCombatState();
            _remainingDuration = _attackData?.reactionDuration > 0f ? _attackData.reactionDuration : 1.2f;

            UPlayGround.Gameplay.Tag.GameplayTag animKey = playerActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak, true)
                ? UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak
                : UPlayGround.Data.Actor.Animation.MotionTags.Hit_F;
            playerActor.Animator.PlayMotion(animKey, 0.15f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (controller.CurrentState != this)
                return;

            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0f)
            {
                // 스턴 회복 직후 경직 내성 부여 — 깨어나자마자 재경직되는 것을 막는다.
                playerActor.GrantStaggerImmunity(PlayerActor.StaggerImmunityDuration);
                controller.TransitionToState(new PlayerIdleState(controller));
            }
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
