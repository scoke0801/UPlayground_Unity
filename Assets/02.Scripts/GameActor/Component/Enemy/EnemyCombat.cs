using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Component
{
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private EnemyAttackDataSO _attackData;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private LayerMask _targetLayer;
        
        private MonsterActor _ownerActor;
        private IDamageable _ownerDamageable;
        private EnemyDetection _detection;
        
        private EnemyAttackInfo _currentSkill;
        private readonly List<Collider> _hitTargets = new List<Collider>();
        private readonly Dictionary<EnemyAttackInfo, float> _skillCooldowns = new Dictionary<EnemyAttackInfo, float>();

        private SkillType _reservedSkillType = SkillType.None;
        private bool _isCollisionEnabled;
        
        private readonly List<Transform> _spawnedUnits = new List<Transform>();

        private readonly List<IDamageable> _skillTargets = new List<IDamageable>();
        
        public EnemyAttackDataSO AttackData => _attackData;
        public EnemyAttackInfo CurrentSkill => _currentSkill;
        public bool IsPossibleCollide => _isCollisionEnabled;
        public SkillType ReservedSkillType => _reservedSkillType;
        public List<IDamageable> SkillTargetList => _skillTargets;
        
        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;
            
            _ownerDamageable = GetComponent<IDamageable>();
            _detection = GetComponent<EnemyDetection>();
            _ownerActor = GetComponent<MonsterActor>();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        private void UpdateCooldowns()
        {
            List<EnemyAttackInfo> keysToUpdate = new List<EnemyAttackInfo>(_skillCooldowns.Keys);
            
            foreach (var skill in keysToUpdate)
            {
                _skillCooldowns[skill] -= Time.deltaTime;
                if (_skillCooldowns[skill] <= 0)
                {
                    _skillCooldowns.Remove(skill);
                }
            }
        }
        
        /// <summary>
        /// 컨텍스트 생성
        /// </summary>
        private SkillConditionContext CreateContext(float distanceToTarget)
        {
            float currentHealth = 100f;
            float maxHealth = 100f;
            
            if (_ownerDamageable != null)
            {
                currentHealth = _ownerDamageable.GetHealthPercent() * 100f;
                maxHealth = 100f;
            }
           
            return new SkillConditionContext
            {
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth,
                DistanceToTarget = distanceToTarget,
                AllyCount = GetAllyCount(),
                SpawnedUnitCount = GetActiveSpawnedCount(),
                HasTarget = distanceToTarget < float.MaxValue,
                CasterTransform = transform,
                AllyLayer = _detection != null ? _detection.AllyLayer : default,
                AllyDetectionRadius = _detection != null ? _detection.AllyDetectionRadius : 10f
            };
        }
        
        public int GetActiveSpawnedCount()
        {
            // 죽거나 삭제된 유닛 제거
            _spawnedUnits.RemoveAll(t => t == null || !t.GetComponent<IDamageable>()?.IsAlive() == true);
            return _spawnedUnits.Count;
        }

        public void RegisterSpawnedUnit(Transform unit)
        {
            if (unit != null && !_spawnedUnits.Contains(unit))
            {
                _spawnedUnits.Add(unit);
            }
        }
        
        /// <summary>
        /// 주변 아군 수 계산
        /// </summary>
        private int GetAllyCount()
        {
            if (_detection == null)
                return 0;
            
            return _detection.GetAllyCount();
        }
        
        /// <summary>
        /// 현재 거리에서 사용 가능한 스킬 선택 및 실행
        /// </summary>
        public EnemyAttackInfo SelectAndExecuteSkill(float distanceToTarget)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
            {
                Debug.LogWarning("[EnemyCombat] 스킬 데이터가 없습니다!");
                return null;
            }
            
            // 스폰한 대상 중, 제거된 액터를 목록에서 정리
            _spawnedUnits.RemoveAll(unit => unit == null);

            // 사용 가능한 스킬 필터링
            List<EnemyAttackInfo> availableSkills = GetAvailableSkills(distanceToTarget);
            if (availableSkills == null || availableSkills.Count == 0)
            {
                return null;
            }
            // 가중치 기반 스킬 선택
            _currentSkill = _attackData.SelectRandomSkill(availableSkills);

            if (_currentSkill != null)
            {
                // 스킬 쿨다운 시작
                _skillCooldowns[_currentSkill] = _currentSkill.cooldown;
                
                // 스킬 타입별 실행
                ExecuteSkill(_currentSkill);
            }

            return _currentSkill;
        }

        private List<EnemyAttackInfo> GetAvailableSkills(float distanceToTarget)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
            {
                return null;
            }
            
            SkillConditionContext context = CreateContext(distanceToTarget);
            
            // 사용 가능한 스킬 필터링
            List<EnemyAttackInfo> availableSkills = new List<EnemyAttackInfo>();
            
            foreach (var skill in _attackData.skills)
            {
                // 쿨다운 체크
                if (_skillCooldowns.ContainsKey(skill))
                    continue;
                
                // 거리 체크
                if (!skill.IsInRange(distanceToTarget))
                    continue;
                
                // 발동 조건 체크
                if (!skill.CheckCondition(context))
                    continue;
                
                availableSkills.Add(skill);
            }

            return availableSkills;
        }
        public bool HasAvailableSkillAtDistance(float distanceToTarget)
        {
            return GetAvailableSkills(distanceToTarget)?.Count > 0;
        }
        
        /// <summary>
        /// 스킬 타입별 실행 - 대상만 지정
        /// </summary>
        private void ExecuteSkill(EnemyAttackInfo skill)
        {
            _skillTargets.Clear();
            
            // 공격 스킬이면 감지된 플레이어를 타겟으로
            if (skill.skillType == SkillType.Attack)
            {
                var target = _detection?.CurrentTarget;
                var damageable = target?.GetComponent<IDamageable>();
                if (damageable != null)
                    _skillTargets.Add(damageable);
                return;
            }
            
            var conditions = skill.conditionGroup?.conditions;
            if (conditions == null || conditions.Count == 0)
            {
                // 조건 없으면 자기 자신
                if (_ownerDamageable != null)
                {
                    _skillTargets.Add(_ownerDamageable);
                }

                return;
            }

            for (int i = 0; i < conditions.Count; ++i)
            {
                switch (conditions[i].type)
                {
                    case ConditionType.SelfHealthBased:
                        if (_ownerDamageable != null)
                        {
                            _skillTargets.Add(_ownerDamageable);
                        }

                        break;
                    
                    case ConditionType.InjuredAllyNearby:
                        CacheInjuredAllies(conditions[i]);
                        break;
                    
                    default: break;
                }
            }

            // 아무런 대상을 찾지 못했다면 자신을 대상으로 하도록 지정
            if (_skillTargets.Count == 0 && _ownerDamageable != null)
            {
                _skillTargets.Add(_ownerDamageable);
            }
        }
        
        private void CacheInjuredAllies(SkillCondition condition)
        {
            if (_detection == null) return;

            float radius = condition.maxRange > 0f ? condition.maxRange : _detection.AllyDetectionRadius;
            Collider[] allies = Physics.OverlapSphere(transform.position, radius, _detection.AllyLayer);

            foreach (var ally in allies)
            {
                if (ally.transform == transform) continue;

                var damageable = ally.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive()) continue;

                float hp = damageable.GetHealthPercent();
                if (hp >= condition.minHealthPercent && hp <= condition.maxHealthPercent)
                {
                    _skillTargets.Add(damageable);
                }
            }
        }
        /// <summary>
        /// 힐 스킬 실행
        /// </summary>
        private void ExecuteHealSkill(EnemyAttackInfo skill)
        {
            // 힐 대상 캐싱
            _skillTargets.Clear();
            
            // [TODO]임시로 지정...
            _skillTargets.Add(_ownerDamageable);
        }
        
        /// <summary>
        /// [TODO]버프 스킬 실행 - 이펙트는 적당한게 있으니 이거 사용을 고려해볼까
        /// </summary>
        private void ExecuteBuffSkill(EnemyAttackInfo skill)
        {
            // TODO: 버프 시스템 구현 시 추가
            _skillTargets.Clear();
        }

        /// <summary>
        /// 근접 공격 히트 체크
        /// </summary>
        public void CheckMeleeAttackHit()
        {
            if (_currentSkill == null || _currentSkill.baseInfo.attackType != AttackType.Melee)
                return;
            
            Vector3 attackPosition = GetCurrentAttackPosition();
            
            Collider[] hitColliders = Physics.OverlapSphere(
                attackPosition, 
                _currentSkill.baseInfo.attackRadius, 
                _targetLayer);
            
            foreach (var hitCollider in hitColliders)
            {
                if (_hitTargets.Contains(hitCollider))
                    continue;
                
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null && damageable.CanTakeDamage())
                {
                    AttackData attackData = new AttackData
                    {
                        damage = _currentSkill.baseInfo.damage,
                        criticalMultiplier = 1.0f,
                        hitPoint = hitCollider.ClosestPoint(attackPosition),
                        attackDirection = _attackOrigin.forward,
                        reactionType = _currentSkill.baseInfo.reactionType,
                        hitParticleName = _currentSkill.baseInfo.hitParticleName,
                        attacker = _ownerActor
                    };
                    
                    damageable.TakeDamage(attackData);
                    _hitTargets.Add(hitCollider);
                    
                    GameObjectManager.Instance.ShowFX(
                        _currentSkill.baseInfo.hitParticleName,
                        attackData.hitPoint);
                }
            }
        }

        public void ClearHitTargets()
        {
            _hitTargets.Clear();
        }

        public void SetEnableCollision(bool isCollisionEnable)
        {
            _isCollisionEnabled = isCollisionEnable;
        }
        
        public Vector3 GetCurrentAttackPosition()
        {
            if (_currentSkill == null)
                return _attackOrigin.position;
            
            return _attackOrigin.position + 
                   _attackOrigin.forward * _currentSkill.baseInfo.attackOffset.z + 
                   _attackOrigin.right * _currentSkill.baseInfo.attackOffset.x + 
                   _attackOrigin.up * _currentSkill.baseInfo.attackOffset.y;
        }

        public float GetCurrentAttackRadius()
        {
            if (_currentSkill == null)
                return 0f;
            
            return _currentSkill.baseInfo.attackRadius;
        }
    }
}