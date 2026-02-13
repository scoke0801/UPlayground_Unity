using UnityEngine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 AI 의사결정 - 상태 전환 로직 관리
    /// </summary>
    public class EnemyBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyDetection _detection;
        [SerializeField] private ActorMovementController _movementController;
        
        [Header("Behavior Settings")]
        [SerializeField] private float _attackRange = 2.5f;
        [SerializeField] private float _chaseSpeedMultiplier = 1.2f;
        [SerializeField] private float _decisionInterval = 0.1f;
        
        [Header("Patrol Settings")]
        [SerializeField] private bool _enablePatrol = true;
        [SerializeField] private float _patrolRadius = 5f;
        [SerializeField] private float _patrolWaitTime = 2f;
        
        private float _decisionTimer;
        private Vector3 _spawnPosition;
        
        public float AttackRange => _attackRange;
        public float ChaseSpeedMultiplier => _chaseSpeedMultiplier;
        public float PatrolRadius => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;
        public Vector3 SpawnPosition => _spawnPosition;
        public bool EnablePatrol => _enablePatrol;
        
        private void Awake()
        {
            if (_detection == null)
                _detection = GetComponent<EnemyDetection>();
            
            if (_movementController == null)
                _movementController = GetComponent<ActorMovementController>();
            
            _spawnPosition = transform.position;
        }

        private void Update()
        {
            _decisionTimer += Time.deltaTime;
            
            if (_decisionTimer >= _decisionInterval)
            {
                _decisionTimer = 0f;
                MakeDecision();
            }
        }

        private void MakeDecision()
        {
            if (_movementController == null || _movementController.CurrentState == null)
                return;
            
            string currentStateName = _movementController.CurrentState.StateName;
            
            // Death 상태면 더 이상 의사결정 하지 않음
            if (currentStateName == "Death")
                return;
            
            // Hit 상태면 잠시 대기 (피격 애니메이션 재생 중)
            if (currentStateName == "Hit")
                return;
            
            // 타겟이 있는 경우
            if (_detection.HasTarget)
            {
                HandleCombatBehavior(currentStateName);
            }
            // 타겟이 없는 경우
            else
            {
                HandleIdleBehavior(currentStateName);
            }
        }

        private void HandleCombatBehavior(string currentStateName)
        {
            float distanceToTarget = _detection.DistanceToTarget;
            
            // 공격 범위 내
            if (distanceToTarget <= _attackRange)
            {
                // TODO: Phase 2에서 Attack 상태 추가 시 활성화
                // if (currentStateName != "Attack")
                // {
                //     _movementController.TransitionToState(new EnemyAttackState(_movementController));
                // }
                
                // 현재는 Chase 상태 유지
                if (currentStateName != "Chase")
                {
                    _movementController.TransitionToState(new EnemyChaseState(_movementController, this, _detection));
                }
            }
            // 추적 범위
            else
            {
                if (currentStateName != "Chase")
                {
                    _movementController.TransitionToState(new EnemyChaseState(_movementController, this, _detection));
                }
            }
        }

        private void HandleIdleBehavior(string currentStateName)
        {
            if (_enablePatrol)
            {
                if (currentStateName != "Patrol" && currentStateName != "Idle")
                {
                    _movementController.TransitionToState(new EnemyPatrolState(_movementController, this));
                }
            }
            else
            {
                if (currentStateName != "Idle")
                {
                    _movementController.TransitionToState(new EnemyIdleState(_movementController));
                }
            }
        }

        /// <summary>
        /// 순찰 포인트 생성 (스폰 위치 기준 랜덤)
        /// </summary>
        public Vector3 GetRandomPatrolPoint()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
            Vector3 randomPoint = _spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            return randomPoint;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 공격 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            
            // 순찰 범위
            if (_enablePatrol)
            {
                Gizmos.color = Color.cyan;
                Vector3 center = Application.isPlaying ? _spawnPosition : transform.position;
                Gizmos.DrawWireSphere(center, _patrolRadius);
            }
        }
#endif
    }
}