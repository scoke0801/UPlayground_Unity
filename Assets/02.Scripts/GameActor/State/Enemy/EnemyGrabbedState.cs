using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 적 잡힘 상태.
    /// Grab 공격에 피격 시 진입. 일정 시간 행동 불능.
    /// 적은 연타 탈출이 없고, 지속 시간이 끝나면 자동 해제.
    /// </summary>
    public class EnemyGrabbedState : GameActorState
    {
        public override string StateName => "Grabbed";

        private readonly AttackData _attackData;
        private float _remainingDuration;

        public EnemyGrabbedState(ActorMovementController controller, AttackData attackData)
            : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionState(string stateName)
        {
            // 잡힌 동안은 Hit으로도 전환 불가 (추가 경직 무시)
            return stateName is "Death";
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _remainingDuration = _attackData?.grabDuration ?? 1.5f;

            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.Grabbed)
                ? AnimKey.Grabbed
                : AnimKey.Hit_F;

            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void UpdateState(float deltaTime)
        {
            _remainingDuration -= deltaTime;

            if (_remainingDuration <= 0f)
            {
                Escape();
            }
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
