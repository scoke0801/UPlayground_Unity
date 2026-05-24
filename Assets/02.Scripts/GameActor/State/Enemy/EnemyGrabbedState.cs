using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 적 잡힘 상태.
    /// Grab 공격에 피격 시 진입. 일정 시간 행동 불능.
    /// 공격자가 FireForcedMotionReleased()를 호출하면 즉시 해제되며,
    /// 호출 없이 grabDuration이 만료되면 자동 탈출한다.
    /// </summary>
    public class EnemyGrabbedState : EnemyActorState
    {
        public override string StateName => "Grabbed";
        public override bool BlocksBehaviorTree => true;

        private readonly AttackData _attackData;
        private float _remainingDuration;

        public EnemyGrabbedState(ActorMovementController controller, AttackData attackData)
            : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName is "Death";
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _remainingDuration = _attackData?.grabDuration ?? 1.5f;

            if (_attackData?.attacker != null)
                _attackData.attacker.OnForcedMotionReleased += Escape;

            AnimKey animKey;
            if (_attackData?.victimForcedAnimKey != AnimKey.None &&
                gameActor.Animator.HasMotion(_attackData.victimForcedAnimKey))
                animKey = _attackData.victimForcedAnimKey;
            else if (gameActor.Animator.HasMotion(AnimKey.Grabbed))
                animKey = AnimKey.Grabbed;
            else
                animKey = AnimKey.Hit_F;

            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);

            if (_attackData?.attacker != null)
                _attackData.attacker.OnForcedMotionReleased -= Escape;
        }

        public override void UpdateState(float deltaTime)
        {
            _remainingDuration -= deltaTime;

            if (_remainingDuration <= 0f)
                Escape();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * 2f * deltaTime));
            }
        }

        private void Escape()
        {
            if (_remainingDuration < -99f) return;
            _remainingDuration = float.MinValue;

            if (gameActor.Animator.HasMotion(AnimKey.Grabbed_End))
            {
                var state = gameActor.Animator.PlayMotion(AnimKey.Grabbed_End, 0.1f);
                if (state != null)
                    state.OwnedEvents.OnEnd = TransitionOut;
                else
                    TransitionOut();
            }
            else
            {
                TransitionOut();
            }
        }

        private void TransitionOut()
        {
            controller.TransitionToState(new EnemyIdleState(controller));
        }
    }
}
