using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerDashAttackState : PlayerActorState
    {
        
        public override string StateName => "JumpAttack";

        private AttackData _attackData;
        private PlayerEquipment _equipment;
        
        public PlayerDashAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            //_attackData = _combat.GetJumpAttack();

            gameActor.MoveAnimType = BaseMoveAnimType.Run;

            SnapToLockOnTarget();

            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);
            playerActor.GetCombat()?.ExecuteDashAttack();

            var state = gameActor.Animator.PlayMotion(AnimKey.DashAttack_1, 0.1f);
            if (state != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackAnimationEnd;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimationEnd;
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);
            
            base.OnExit(toState);
        }
        private void SnapToLockOnTarget()
        {
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            if (lockOnTarget == null) return;

            Vector3 dir = (lockOnTarget.position - gameActor.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                motor.SetRotation(Quaternion.LookRotation(dir.normalized));
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
        
        protected override AnimKey? RequiredMotionKey => AnimKey.DashAttack_1;

        public override bool CanTransitionState(string stateName)
        {
            if (HasRequiredMotion() == false) return false;
            if (stateName == "Hit") return false;
            return true;
        }

        public override void UpdateState(float deltaTime)
        {
            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dash) != null)
            {
                if (playerController.TryTransitionToState(new PlayerDashState(controller)))
                {
                    return;
                }
            }
        }

        // public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        // {
        //     // Lock-On 타겟이 있으면 스냅과 무관하게 항상 타겟 쪽을 바라보도록 보정
        //     Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
        //     if (lockOnTarget != null)
        //     {
        //         Vector3 directionToTarget = (lockOnTarget.position - gameActor.transform.position).normalized;
        //         directionToTarget.y = 0f;
        //         
        //         if (directionToTarget.sqrMagnitude > 0.01f)
        //         {
        //             Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        //             currentRotation = Quaternion.Slerp(currentRotation, targetRotation, deltaTime * 10f);
        //         }
        //     }
        //     currentRotation = currentRotation.normalized;
        // }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 경사로 이동 보정: 현재 속도를 지면 기울기에 맞게 재지향
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                currentVelocity.y = 0f;
                
                // 부드럽게 목표 속도로 이동
                currentVelocity = Vector3.Lerp(
                    currentVelocity, 
                    Vector3.zero, 
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
            

            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;
            currentVelocity += rootMotionVel;
        }
    }
}
