using UnityEngine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Component
{
    public class EnemyBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyDetection _detection;
        [SerializeField] private ActorMovementController _movementController;
        [SerializeField] private EnemyCombat _combat;
        
        [Header("Behavior Settings")]
        [SerializeField] private float _chaseSpeedMultiplier = 1.2f;
        [SerializeField] private float _decisionInterval = 0.1f;
        
        [Header("Patrol Settings")]
        [SerializeField] private bool _enablePatrol = true;
        [SerializeField] private float _patrolRadius = 5f;
        [SerializeField] private float _patrolWaitTime = 2f;
        
        private float _decisionTimer;
        private float _lastAttackTime;
        private Vector3 _spawnPosition;
        
        // AttackData에서 동적으로 가져오기
        private float _maxAttackRange;
        
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
            
            if (_combat == null)
                _combat = GetComponent<EnemyCombat>();

            _spawnPosition = transform.position;
            
            // 최대 공격 범위 계산
            if (_combat != null && _combat.AttackData != null)
            {
                _maxAttackRange = _combat.AttackData.GetMaxAttackRange();
            }
            else
            {
                _maxAttackRange = 2.5f; // 기본값
            }
            
            _lastAttackTime = -_combat.AttackData.globalCooldown;
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
            
            if (currentStateName == "Death" || currentStateName == "Hit" || currentStateName == "Attack")
                return;
            
            if (_detection.HasTarget)
            {
                HandleCombatBehavior(currentStateName);
            }
            else
            {
                HandleIdleBehavior(currentStateName);
            }
        }

        private void HandleCombatBehavior(string currentStateName)
        {
            float distanceToTarget = _detection.DistanceToTarget;
            
            // 최대 공격 범위 내에 있고 쿨다운이 끝났으면 공격
            if (distanceToTarget <= _maxAttackRange && CanAttack())
            {
                if (_combat != null)
                {
                    _lastAttackTime = Time.time;
                    _movementController.TransitionToState(new EnemyAttackState(_movementController, _combat, this, _detection));
                }
            }
            else
            {
                // 추적
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
                if (currentStateName != "Patrol")
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
        
        private bool CanAttack()
        {
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }
        
        public float GetMaxAttackRange()
        {
            return _maxAttackRange;
        }
        
        public Vector3 GetRandomPatrolPoint()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
            Vector3 randomPoint = _spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            return randomPoint;
        }
    }
}