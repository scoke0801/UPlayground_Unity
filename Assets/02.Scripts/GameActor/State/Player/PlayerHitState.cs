using System.Collections;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 피격 상태
    /// </summary>
    public class PlayerHitState : PlayerActorState
    {
        public override string StateName => "Hit";

        private AttackData _attackData;
        
        public PlayerHitState(ActorMovementController controller, AttackData attackData) 
            : base(controller)
        {
            _attackData = attackData;
        }

        public override bool CanTransitionToState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            var state = gameActor.Animator.PlayMotion(GetAnimKey(), 0.25f);

            if (state != null)
            {
                state.OwnedEvents.OnEnd = () => { controller.TransitionToState(new PlayerIdleState(controller)); };
            }
        }

        public override void UpdateState(float deltaTime)
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new PlayerAirborneState(controller));
                return;
            }
            
            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }
            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }

            for (int i = 0; i < 4; ++i)
            {
                if (!playerController.HasSkillInput(i)) continue;

                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Idle 상태에서는 회전 유지 (또는 부드럽게 정면으로)
            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 지면에 있으므로 경사면에 맞춰 속도 조정
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // 정지 상태로 부드럽게 감속
                Vector3 targetVelocity = Vector3.zero;
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        private AnimKey GetAnimKey()
        {
            if (_attackData.reactionType == AttackReactionType.KnockBack)
            {
                if (playerActor.Animator.HasMotion(AnimKey.Knockback, true))
                {
                    return AnimKey.Knockback;
                }
            }
            
            // 1. 플레이어에서 공격 지점을 바라보는 월드 방향 벡터 계산
            // (공격 위치 - 플레이어 위치)
            Vector3 dirToAttack = (_attackData.attackDirection - playerActor.transform.position).normalized;
           
            // 2. 월드 방향 벡터를 플레이어의 로컬 좌표계로 변환
            // 이렇게 하면 플레이어의 정면이 (0, 0, 1), 오른쪽이 (1, 0, 0)이 됩니다.
            Vector3 localDir = playerActor.transform.InverseTransformDirection(dirToAttack);
          
            // 3. 로컬 Z(앞/뒤)와 X(좌/우) 값을 비교하여 가장 큰 성분 방향을 선택
            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
            {
                // 좌우 판정
                return localDir.x > 0 ? AnimKey.Hit_R : AnimKey.Hit_L;
            }
            else
            {
                // 전후 판정
                return localDir.z > 0 ? AnimKey.Hit_F : AnimKey.Hit_B;
            }
        }
    }
}
