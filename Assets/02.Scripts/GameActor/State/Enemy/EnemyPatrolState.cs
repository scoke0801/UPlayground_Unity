using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 순찰 상태 - 랜덤 포인트로 이동 후 대기
    /// </summary>
    public class EnemyPatrolState : GameActorState
    {
        public override string StateName => "Patrol";
        
        private EnemyBrain _brain;
        
        private Vector3 _targetPosition;
        private float _patrolSpeed;
        private float _waitTimer;
        private bool _isWaiting;
        
        private const float ARRIVAL_THRESHOLD = 0.5f;
        
        public EnemyPatrolState(ActorMovementController controller, EnemyBrain brain) : base(controller)
        {
            _brain = brain;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            _patrolSpeed = controller.MaxRunMoveSpeed * 0.5f; // 천천히 이동
            _isWaiting = false;
            _waitTimer = 0f;
            
            // 첫 순찰 지점 설정
            SetNewPatrolPoint();
            
            Debug.Log("[EnemyPatrolState] 순찰 시작");
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            Debug.Log("[EnemyPatrolState] 순찰 종료");
        }

        public override void UpdateState(float deltaTime)
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            
            if (_isWaiting)
            {
                // 대기 중
                _waitTimer += deltaTime;
                
                if (_waitTimer >= _brain.PatrolWaitTime)
                {
                    // 다음 순찰 지점으로
                    SetNewPatrolPoint();
                    _isWaiting = false;
                    _waitTimer = 0f;
                    
                    gameActor.Animator.PlayAnimation(AnimKey.Walk, 0.25f);
                }
            }
            else
            {
                // 이동 중 - 목적지 도착 체크
                float distanceToTarget = Vector3.Distance(
                    new Vector3(motor.TransientPosition.x, 0, motor.TransientPosition.z),
                    new Vector3(_targetPosition.x, 0, _targetPosition.z));
                
                if (distanceToTarget <= ARRIVAL_THRESHOLD)
                {
                    // 도착 - 대기 시작
                    _isWaiting = true;
                    _waitTimer = 0f;
                    
                    gameActor.Animator.PlayAnimation(AnimKey.Idle, 0.25f);
                }
                else
                {
                    gameActor.Animator.PlayAnimation(AnimKey.Walk, 0.25f);
                }
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_isWaiting)
            {
                // 목적지를 향해 회전
                Vector3 directionToTarget = (_targetPosition - motor.TransientPosition).normalized;
                directionToTarget.y = 0;
                
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
            if (_isWaiting)
            {
                // 대기 중에는 정지
                if (motor.GroundingStatus.IsStableOnGround)
                {
                    currentVelocity = Vector3.zero; 
                }
            }
            else
            {
                // 목적지를 향해 이동
                Vector3 directionToTarget = (_targetPosition - motor.TransientPosition).normalized;
                directionToTarget.y = 0;
                
                if (motor.GroundingStatus.IsStableOnGround)
                {
                    Vector3 targetVelocity = directionToTarget * _patrolSpeed;
                    
                    // 경사면 고려
                    targetVelocity = motor.GetDirectionTangentToSurface(
                        targetVelocity,
                        motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;
                    
                    currentVelocity = Vector3.Lerp(
                        currentVelocity,
                        targetVelocity,
                        1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                }
            }
        }

        private void SetNewPatrolPoint()
        {
            _targetPosition = _brain.GetRandomPatrolPoint();
            
            // Y값은 현재 위치 기준 (지형 높이는 고려하지 않음)
            _targetPosition.y = motor.TransientPosition.y;
            
            Debug.Log($"[EnemyPatrolState] 새로운 순찰 지점: {_targetPosition}");
        }
    }
}