using UnityEngine;
using UPlayGround.Combat;
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

        private readonly HitContext _hit;
        private float _remainingDuration;

        public EnemyGrabbedState(ActorMovementController controller, in HitContext hit)
            : base(controller)
        {
            _hit = hit;
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName is "Death";
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _remainingDuration = _hit.GrabDuration;

            if (_hit.Attacker != null)
                _hit.Attacker.OnForcedMotionReleased += Escape;

            AnimKey animKey;
            if (_hit.VictimForcedAnimKey != AnimKey.None &&
                gameActor.Animator.HasMotion(_hit.VictimForcedAnimKey))
                animKey = _hit.VictimForcedAnimKey;
            else if (gameActor.Animator.HasMotion(AnimKey.Grabbed))
                animKey = AnimKey.Grabbed;
            else
                animKey = AnimKey.Hit_F;

            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);

            if (_hit.Attacker != null)
                _hit.Attacker.OnForcedMotionReleased -= Escape;
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
