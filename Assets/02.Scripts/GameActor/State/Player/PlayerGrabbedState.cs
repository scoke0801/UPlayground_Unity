using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 플레이어 잡힘 상태.
    /// Grab 공격에 피격 시 진입. 일정 시간 행동 불능.
    /// </summary>
    public class PlayerGrabbedState : PlayerActorState
    {
        public override string StateName => "Grabbed";

        private readonly AttackData _attackedData;
        private float _remainingDuration;
        private float _elapsedTime;

        public PlayerGrabbedState(ActorMovementController controller, AttackData attackedData)
            : base(controller)
        {
            _attackedData = attackedData;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _remainingDuration = _attackedData?.grabDuration ?? 1.5f;
            _elapsedTime = 0f;

            playerActor.GetCombat()?.RefreshCombatState();

            // Grabbed 애니메이션이 있으면 재생, 없으면 Hit_F 폴백
            AnimKey animKey = gameActor.Animator.HasMotion(AnimKey.Grabbed)
                ? AnimKey.Grabbed
                : AnimKey.Hit_F;

            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void UpdateState(float deltaTime)
        {
            _elapsedTime += deltaTime;
            _remainingDuration -= deltaTime;

            if (_remainingDuration <= 0f)
            {
                Escape();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 잡힌 동안 회전 불가
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 잡힌 동안 이동 불가
            currentVelocity = Vector3.zero;
        }

        private readonly AttackData _attackData;
        private bool _isDescending;
        private bool _hasLanded;
        private float _airTimer;

        private const float FORCE_DESCEND_TIME = 0.5f;
        private const float SLAM_DOWN_SPEED    = 28f;
        private void Escape()
        {
            // 탈출 애니가 있으면 재생
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
            //
            // // 탈출 시 공격자 반대 방향으로 밀림
            // if (_attackedData?.attacker != null)
            // {
            //     Vector3 escapeDir = (motor.TransientPosition - _attackedData.attacker.transform.position).normalized;
            //     escapeDir.y = 0f;
            // }
        }

        private void TransitionOut()
        {
            controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}
