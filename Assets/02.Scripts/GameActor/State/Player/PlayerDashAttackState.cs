using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerDashAttackState : PlayerActorState
    {
        
        public override string StateName => "JumpAttack";

        private AttackData _attackData;
        private float _timer;
        private bool _hasHit;

        public PlayerDashAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            //_attackData = _combat.GetJumpAttack();
            _timer = 0f;
            _hasHit = false;

            var state = gameActor.Animator.PlayMotion(AnimKey.DashAttack_1, 0.1f);
            if (state != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;
            
            base.OnExit(toState);
        }
        private void OnAttackAnimationEnd()
        {
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
            else
            {
                controller.TransitionToState(new PlayerIdleState(controller));
            }
        }


        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            // 착지 시 또는 모션 종료 시 → 복귀
            if (motor.GroundingStatus.IsStableOnGround)
            {
                //OnLanded();
                return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Lock-On 타겟이 있으면 스냅과 무관하게 항상 타겟 쪽을 바라보도록 보정
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget != null)
            {
                Vector3 directionToTarget = (lockOnTarget.position - gameActor.transform.position).normalized;
                directionToTarget.y = 0f;
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRotation, deltaTime * 10f);
                }
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 경사로 이동 보정: 현재 속도를 지면 기울기에 맞게 재지향
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
              
                // 지면 노멀을 고려한 타겟 속도 계산
                Vector3 moveInputVector = playerController.MoveInputVector;
                Vector3 inputRight = Vector3.Cross(moveInputVector, motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(
                    motor.GroundingStatus.GroundNormal, 
                    inputRight).normalized * moveInputVector.magnitude;
            
                Vector3 targetMovementVelocity = reorientedInput * controller.MaxSprintMoveSpeed;

                // 부드럽게 목표 속도로 이동
                currentVelocity = Vector3.Lerp(
                    currentVelocity, 
                    targetMovementVelocity, 
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));

            }
            

            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;
            currentVelocity += rootMotionVel;
        }

        private void OnLanded()
        {
            // 착지 시 충격파 히트박스 발동
            // _combat.ExecuteHitbox(_attackData);

            // 착지 모션이 있다면
            // gameActor.Animator.PlayMotion(AnimKey.JumpAttackLand, 0.1f);

            controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}