using UnityEngine;
using UPlayGround.Data.Enum;
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
        
        [Header("Combat Settings")]
        [SerializeField] private float _optimalCombatDistance = 2.5f;   // 선호하는 전투 거리
        [SerializeField] private float _minCombatDistance = 1.5f;       // 최소 전투 거리
        [SerializeField] private bool _maintainDistance = true;        // 거리 유지 여부

        private float _decisionTimer;
        private float _lastAttackTime;
        private float _lastSkillCheckTime;
        private Vector3 _spawnPosition;
        
        // AttackData에서 동적으로 가져오기
        private float _maxAttackRange;
        private float _skillCheckInterval = 0.5f; 
        
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
                
                // 최적 전투 거리를 최대 범위의 80%로 자동 설정
                if (_optimalCombatDistance > _maxAttackRange)
                {
                    _optimalCombatDistance = _maxAttackRange * 0.8f;
                }
            }
            else
            {
                _maxAttackRange = 2.5f;
            }
            
            _lastAttackTime = -(_combat?.AttackData?.globalCooldown ?? 1f);
            _lastSkillCheckTime = 0f;
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
            
            if (Time.time - _lastSkillCheckTime >= _skillCheckInterval)
            {
                _lastSkillCheckTime = Time.time;
                
                if (TryUnCombatSkill())
                {
                    return; // 긴급 스킬 사용 시 다른 행동 취소
                }
            }
            
            if (_detection.HasTarget)
            {
                HandleCombatBehavior(currentStateName);
            }
            else
            {
                HandleIdleBehavior(currentStateName);
            }
        }
        
        /// <summary>
        /// 스킬 사용 시도 (힐, 버프 등)
        /// </summary>
        private bool TryUnCombatSkill()
        {
            if (_combat == null || _combat.AttackData == null)
                return false;
            
            // 타겟 필요 없는 스킬
            // 거리를 float.MaxValue로 설정하여 거리 조건 무시
            var urgentSkill = _combat.SelectAndExecuteSkill(float.MaxValue);
            
            if (urgentSkill != null)
            {
                // Heal이나 Buff 같은 비공격 스킬인지 확인
                if (urgentSkill.skillType == SkillType.Heal || 
                    urgentSkill.skillType == SkillType.Buff)
                {
                    _lastAttackTime = Time.time;
                    _movementController.TransitionToState(
                        new EnemyAttackState(_movementController, _combat, this, _detection));
                    
                    Debug.Log($"[EnemyBrain] 긴급 스킬 사용: {urgentSkill.skillType}");
                    return true;
                }
            }
            
            return false;
        }

        private void HandleCombatBehavior(string currentStateName)
        {
            float distanceToTarget = _detection.DistanceToTarget;
            
            if (CanUseSkill())
            {
                // 현재 거리에서 사용 가능한 스킬이 있는지 미리 체크
                if (HasAvailableSkillAtDistance(distanceToTarget))
                {
                    // 스킬 사용
                    _lastAttackTime = Time.time;
                    _movementController.TransitionToState(
                        new EnemyAttackState(_movementController, _combat, this, _detection));
                    return;
                }
            }
            
            HandleDistanceBasedMovement(currentStateName, distanceToTarget);
        }
        
        /// <summary>
        /// 거리 기반 이동 처리
        /// </summary>
        private void HandleDistanceBasedMovement(string currentStateName, float distanceToTarget)
        {
            // 너무 가까움 - 후퇴
            if (_maintainDistance && distanceToTarget < _minCombatDistance)
            {
                if (currentStateName != "Retreat")
                {
                    // TODO: EnemyRetreatState 구현 또는 Chase의 방향 반전
                    Debug.Log("[EnemyBrain] 너무 가까움 - 후퇴");
                }
            }
            // 최적 거리보다 멀음 - 접근
            else if (distanceToTarget > _optimalCombatDistance)
            {
                if (currentStateName != "Chase")
                {
                    _movementController.TransitionToState(
                        new EnemyChaseState(_movementController, this, _detection));
                }
            }
            // 최적 거리 - 대기 (원거리 공격 대기)
            else if (_maintainDistance)
            {
                if (currentStateName != "Idle")
                {
                    _movementController.TransitionToState(new EnemyIdleState(_movementController));
                }
            }
            // 거리 유지 안함 - 계속 추격
            else
            {
                if (currentStateName != "Chase")
                {
                    _movementController.TransitionToState(
                        new EnemyChaseState(_movementController, this, _detection));
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
        
        /// <summary>
        /// 스킬 사용 가능 여부 (글로벌 쿨다운)
        /// </summary>
        private bool CanUseSkill()
        {
            if (_combat == null || _combat.AttackData == null)
                return false;
            
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }
        
        /// <summary>
        /// 현재 거리에서 사용 가능한 스킬이 있는지 확인
        /// </summary>
        private bool HasAvailableSkillAtDistance(float distance)
        {
            if (_combat == null || _combat.AttackData == null)
                return false;

            return _combat.HasAvailableSkillAtDistance(distance);
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
        
        /// <summary>
        /// 전투 스타일 설정 (편의 기능)
        /// </summary>
        public void SetCombatStyle(EnemyCombatStyle style)
        {
            switch (style)
            {
                case EnemyCombatStyle.Melee:
                    _maintainDistance = false;
                    _optimalCombatDistance = 2f;
                    _minCombatDistance = 0.5f;
                    break;
                    
                case EnemyCombatStyle.Ranged:
                    _maintainDistance = true;
                    _optimalCombatDistance = _maxAttackRange * 0.7f;
                    _minCombatDistance = _maxAttackRange * 0.5f;
                    break;
                    
                case EnemyCombatStyle.Balanced:
                    _maintainDistance = true;
                    _optimalCombatDistance = _maxAttackRange * 0.5f;
                    _minCombatDistance = 2f;
                    break;
                    
                case EnemyCombatStyle.Support:
                    _maintainDistance = true;
                    _optimalCombatDistance = _maxAttackRange * 0.8f;
                    _minCombatDistance = _maxAttackRange * 0.6f;
                    _skillCheckInterval = 0.3f; // 스킬 체크 빈도 증가
                    break;
            }
        }
    }
}