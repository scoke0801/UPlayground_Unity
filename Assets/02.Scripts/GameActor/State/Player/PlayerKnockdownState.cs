using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerKnockdownState : PlayerActorState
    {
        public override string StateName => "Knockdown";
        public override bool GrantsInvincibility => _invincibleTimer > 0f;

        private readonly AttackData _attackData;
        private bool _getupStarted;
        private float _downTimer;
        private float _invincibleTimer;

        public PlayerKnockdownState(ActorMovementController controller, AttackData attackData = null) : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            playerActor.GetCombat()?.RefreshCombatState();
            _getupStarted = false;
            _downTimer = _attackData?.reactionDuration > 0f ? _attackData.reactionDuration : 1.0f;
            _invincibleTimer = 0.4f;

            AnimKey animKey = playerActor.Animator.HasMotion(AnimKey.Knockdown, true)
                ? AnimKey.Knockdown
                : AnimKey.Knockback;
            var state = playerActor.Animator.PlayMotion(animKey, 0.1f);
            if (state != null)
                state.OwnedEvents.OnEnd = BeginGetup;
        }

        public override void UpdateState(float deltaTime)
        {
            if (_invincibleTimer > 0f)
                _invincibleTimer -= deltaTime;

            if (_getupStarted) return;

            _downTimer -= deltaTime;
            bool dodgeBuffered = InputManager.Instance.InputBuffer.HasInput(PlayerAction.Dodge);
            if (_downTimer <= 0f || dodgeBuffered)
                BeginGetup();
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

        private void BeginGetup()
        {
            if (_getupStarted) return;
            _getupStarted = true;
            _invincibleTimer = Mathf.Max(_invincibleTimer, 0.3f);

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dodge) != null)
            {
                controller.TransitionToState(new PlayerDodgeState(controller));
                return;
            }

            if (playerActor.Animator.HasMotion(AnimKey.Knockdown_Getup, true))
            {
                var state = playerActor.Animator.PlayMotion(AnimKey.Knockdown_Getup, 0.1f);
                if (state != null)
                {
                    state.OwnedEvents.OnEnd = TransitionOut;
                    return;
                }
            }

            TransitionOut();
        }

        private void TransitionOut()
        {
            controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}
