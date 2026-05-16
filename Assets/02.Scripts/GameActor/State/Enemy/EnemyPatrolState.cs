using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 순찰 상태 - 랜덤 포인트로 이동 후 대기
    /// 지형/몬스터 충돌 시 타임아웃으로 안전하게 다음 지점 재설정
    /// </summary>
    public class EnemyPatrolState : GameActorState
    {
        public override string StateName => "Patrol";
        
        private EnemyAIContext _context;
        
        private Vector3 _targetPosition;
        private float _patrolSpeed;
        private float _waitTimer;
        private bool _isWaiting;
        private AnimKey _lastLocoKey = AnimKey.None;
        
        // 충돌/정체 감지
        private float _stuckTimer;
        private Vector3 _lastPosition;
        private int _retryCount;
        
        private const float ARRIVAL_THRESHOLD = 0.5f;
        private const float STUCK_CHECK_INTERVAL = 0.5f;
        private const float STUCK_DISTANCE_THRESHOLD = 0.15f; // 이 거리 이내면 정체로 판단
        private const float STUCK_TIMEOUT = 2.0f; // 정체 지속 시 포기 시간
        private const int MAX_RETRY = 3;
        
        public EnemyPatrolState(ActorMovementController controller, EnemyAIContext context) : base(controller)
        {
            _context = context;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            _patrolSpeed = controller.MaxRunMoveSpeed * 0.5f;
            _isWaiting = false;
            _waitTimer = 0f;
            _stuckTimer = 0f;
            _retryCount = 0;
            _lastPosition = motor.TransientPosition;
            
            SetNewPatrolPoint();
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
            
            if (_isWaiting)
            {
                _waitTimer += deltaTime;
                
                if (_waitTimer >= _context.PatrolWaitTime)
                {
                    SetNewPatrolPoint();
                    _isWaiting = false;
                    _waitTimer = 0f;
                    _retryCount = 0;
                    _lastLocoKey = AnimKey.Walk_Slow;
                    gameActor.Animator.PlayMotion(AnimKey.Walk_Slow, 0.25f);
                }
            }
            else
            {
                float distanceToTarget = Vector3.Distance(
                    new Vector3(motor.TransientPosition.x, 0, motor.TransientPosition.z),
                    new Vector3(_targetPosition.x, 0, _targetPosition.z));
                
                if (distanceToTarget <= ARRIVAL_THRESHOLD)
                {
                    StartWaiting();
                }
                else
                {
                    _stuckTimer += deltaTime;
                    if (_stuckTimer >= STUCK_CHECK_INTERVAL)
                    {
                        CheckStuck();
                        _stuckTimer = 0f;
                    }

                    EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey,
                        EnemyLocomotionHelper.LocoStyle.WalkSlow);
                }
            }
        }
        
        /// <summary>
        /// 이동 정체 감지 - 일정 시간 동안 거의 움직이지 못하면 새 지점으로 변경
        /// </summary>
        private void CheckStuck()
        {
            float movedDistance = Vector3.Distance(
                new Vector3(motor.TransientPosition.x, 0, motor.TransientPosition.z),
                new Vector3(_lastPosition.x, 0, _lastPosition.z));
            
            if (movedDistance < STUCK_DISTANCE_THRESHOLD)
            {
                _retryCount++;
                
                if (_retryCount >= MAX_RETRY)
                {
                    // 여러 번 시도해도 갈 수 없으면 제자리에서 대기
                    StartWaiting();
                }
                else
                {
                    // 새로운 순찰 지점 재설정
                    SetNewPatrolPoint();
                }
            }
            else
            {
                // 잘 이동 중이면 리트라이 카운트 리셋
                _retryCount = 0;
            }
            
            _lastPosition = motor.TransientPosition;
        }
        
        private void StartWaiting()
        {
            _isWaiting = true;
            _waitTimer = 0f;
            _stuckTimer = 0f;
            
            gameActor.Animator.PlayMotion(AnimKey.Idle, 0.25f);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_isWaiting)
            {
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
                if (motor.GroundingStatus.IsStableOnGround)
                {
                    currentVelocity = Vector3.zero; 
                }
            }
            else
            {
                Vector3 directionToTarget = (_targetPosition - motor.TransientPosition).normalized;
                directionToTarget.y = 0;
                
                if (motor.GroundingStatus.IsStableOnGround)
                {
                    Vector3 targetVelocity = directionToTarget * _patrolSpeed;
                    
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
        
        /// <summary>
        /// KCC 이동 중 충돌 감지 - 벽이나 다른 캐릭터에 부딪히면 즉시 새 지점으로
        /// </summary>
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (_isWaiting) return;
            
            // 이동 방향과 충돌 법선이 거의 정면 충돌이면 (70도 이상)
            Vector3 moveDir = (_targetPosition - motor.TransientPosition).normalized;
            moveDir.y = 0;
            float dot = Vector3.Dot(moveDir, hitNormal);
            
            if (dot < -0.35f) // 진행 방향 정면에서 충돌
            {
                _retryCount++;
                if (_retryCount >= MAX_RETRY)
                {
                    StartWaiting();
                }
                else
                {
                    SetNewPatrolPoint();
                }
            }
        }

        private void SetNewPatrolPoint()
        {
            _targetPosition = _context.GetRandomPatrolPoint();
            _targetPosition.y = motor.TransientPosition.y;
            _lastPosition = motor.TransientPosition;
            _stuckTimer = 0f;
        }
    }
}
