using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class EnemyKnockdownState : EnemyActorState
    {
        public override string StateName => "Knockdown";
        public override bool BlocksBehaviorTree => true;

        private readonly AttackData _attackData;
        private bool _getupStarted;
        private bool _knockdownMotionEnded;
        private float _downTimer;

        public EnemyKnockdownState(ActorMovementController controller, AttackData attackData = null) : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName) => stateName is "Death" or "Grabbed";

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            controller.MotionWarp?.ClearTarget();
            _getupStarted = false;
            _knockdownMotionEnded = false;
            _downTimer = _attackData?.reactionDuration > 0f ? _attackData.reactionDuration : 1.0f;

            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.Knockdown, true)
                ? AnimKey.Knockdown
                : AnimKey.Knockback;
            var state = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (state != null)
                state.OwnedEvents.OnEnd = OnKnockdownMotionEnd;
            else
                _knockdownMotionEnded = true;
        }

        public override void UpdateState(float deltaTime)
        {
            if (_getupStarted) return;

            _downTimer -= deltaTime;
            if (_downTimer <= 0f && _knockdownMotionEnded)
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
                1f - Mathf.Exp(controller.StableMovementSharpness * -deltaTime));
        }

        private void OnKnockdownMotionEnd()
        {
            _knockdownMotionEnded = true;
        }

        private void BeginGetup()
        {
            if (_getupStarted) return;
            _getupStarted = true;

            if (gameActor.Animator.HasMotion(AnimKey.Knockdown_Getup, true))
            {
                var state = gameActor.Animator.PlayMotion(AnimKey.Knockdown_Getup, 0.1f);
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
            controller.TransitionToState(new EnemyIdleState(controller));
        }
    }
}
