using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 추적 상태 - 타겟을 향해 이동
    /// </summary>
    public class EnemyChaseState : GameActorState
    {
        public override string StateName => "Chase";
        
        private EnemyBrain _brain;
        private EnemyDetection _detection;
        
        private float _chaseSpeed;
        
        public EnemyChaseState(ActorMovementController controller, EnemyBrain brain, EnemyDetection detection) : base(controller)
        {
            _brain = brain;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            // 추적 속도 설정
            _chaseSpeed = controller.MaxRunMoveSpeed * _brain.ChaseSpeedMultiplier;
            
            gameActor.Animator.PlayMotion(AnimKey.Run, 0.25f);
            
            Debug.Log("[EnemyChaseState] 추적 시작");
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            Debug.Log("[EnemyChaseState] 추적 종료");
        }

        public override void UpdateState(float deltaTime)
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            
            // 타겟이 없으면 Idle로 복귀 (Brain이 판단하지만 안전장치)
            if (!_detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection.HasTarget)
            {
                // 타겟을 향해 회전
                Vector3 directionToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                directionToTarget.y = 0; // 수평 방향만
                
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }
            
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_detection.HasTarget)
            {
                // 정지
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }
            
            // 타겟을 향한 방향 계산
            Vector3 directionToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
            directionToTarget.y = 0;
            
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 목표 속도
                Vector3 targetVelocity = directionToTarget * _chaseSpeed;
                
                // 경사면 고려
                targetVelocity = motor.GetDirectionTangentToSurface(targetVelocity, motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;
                
                // 부드러운 가속
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
            else
            {
                // 공중에서는 속도 유지
                if (currentVelocity.sqrMagnitude > 0.01f)
                {
                    Vector3 airVelocity = currentVelocity;
                    airVelocity.y = 0;
                    currentVelocity = airVelocity.normalized * Mathf.Min(airVelocity.magnitude, _chaseSpeed);
                }
            }
        }
    }
}