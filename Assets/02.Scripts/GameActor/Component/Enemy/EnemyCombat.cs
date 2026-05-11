using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.Component
{
    public class EnemyCombat : MonoBehaviour
    {
        private const string HeavyAttackTelegraphFXKey = "EnemyHeavyAttackTelegraph_Circle";

        [Header("Combat Settings")]
        [SerializeField] private EnemyAttackDataSO _attackData;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private LayerMask _targetLayer;

        [Header("Motion Warp Settings")]
        [Tooltip("워프 최소 거리. 이 거리 이내에 이미 있으면 워프 미적용")]
        [SerializeField] private float _warpMinDistance = 0.3f;
        [Tooltip("워프 최대 거리. 이 거리를 초과한 타겟에게는 워프 미적용")]
        [SerializeField] private float _warpMaxDistance = 6f;
        [Tooltip("워프 최대 속도. 남은 시간 내 도달 불가 거리면 워프 자체를 미적용")]
        [SerializeField] private float _warpMaxSpeed = 18f;

        [Header("Telegraph Settings")]
        [SerializeField] private bool _alignTelegraphToGround = true;
        [SerializeField] private LayerMask _telegraphGroundLayers = -1;
        [SerializeField] private float _telegraphGroundProbeHeight = 2f;
        [SerializeField] private float _telegraphGroundProbeDistance = 6f;
        [SerializeField] private float _telegraphGroundYOffset = 0.03f;

        public float WarpMinDistance => _warpMinDistance;
        public float WarpMaxDistance => _warpMaxDistance;
        public float WarpMaxSpeed    => _warpMaxSpeed;

        private MonsterActor _ownerActor;
        private IDamageable _ownerDamageable;
        private EnemyDetection _detection;

        private EnemyAttackInfo _currentSkill;
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private readonly Dictionary<EnemyAttackInfo, float> _skillCooldowns = new Dictionary<EnemyAttackInfo, float>();
        private readonly List<EnemyAttackInfo> _expiredCooldowns = new List<EnemyAttackInfo>();
        private int _currentHitPhaseIndex = 0;

        private SkillType _reservedSkillType = SkillType.None;
        private bool _isCollisionEnabled;

        private readonly List<Transform> _spawnedUnits = new List<Transform>();
        private readonly List<IDamageable> _skillTargets = new List<IDamageable>();
        private readonly List<EnemyAttackInfo> _keysToProcess = new List<EnemyAttackInfo>();
        private readonly List<GameObject> _telegraphInstances = new List<GameObject>();
        private int _telegraphHitPhaseIndex = 0;

        // ── Motion Warp 상태 ──────────────────────────────────────────
        // 진실 소스는 MotionWarpController. 본 클래스는 호환 프록시만 노출한다.
        private MotionWarpController _motionWarp;

        public float WarpRemainingTime => _motionWarp != null ? _motionWarp.WarpRemainingTime : 0f;
        public float WarpDuration      => _motionWarp != null ? _motionWarp.WarpDuration : 0f;
        public bool  IsMotionWarping   => _motionWarp != null && _motionWarp.IsMotionWarping;
        // ──────────────────────────────────────────────────────────────

        public EnemyAttackDataSO AttackData       => _attackData;
        public EnemyAttackInfo   CurrentSkill     => _currentSkill;
        public bool              IsPossibleCollide => _isCollisionEnabled;
        public SkillType         ReservedSkillType => _reservedSkillType;
        public List<IDamageable> SkillTargetList  => _skillTargets;
        public bool              IsGuarding { get; set; } = false;

        /// <summary> 현재 AttackState에서 히트한 대상 수 </summary>
        public int LastHitCount => _hitTargets.Count;

        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;

            _ownerDamageable = GetComponent<IDamageable>();
            _detection       = GetComponent<EnemyDetection>();
            _ownerActor      = GetComponent<MonsterActor>();

            // 워프 진실 소스는 MotionWarpController. 컴포넌트가 없으면 즉시 부착.
            _motionWarp = GetComponent<MotionWarpController>();
            if (_motionWarp == null)
                _motionWarp = gameObject.AddComponent<MotionWarpController>();
        }

        private void Update()
        {
            UpdateCooldowns();
            // 워프 타이머는 MotionWarpController.Update 가 처리.
        }

        private void UpdateCooldowns()
        {
            // 1. 순회할 키들을 별도의 리스트에 복사합니다. (딕셔너리 변경에 안전함)
            _keysToProcess.Clear();
            foreach (var key in _skillCooldowns.Keys)
            {
                _keysToProcess.Add(key);
            }

            _expiredCooldowns.Clear();

            // 2. 복사된 리스트를 순회하며 쿨타임을 갱신합니다.
            foreach (var skill in _keysToProcess)
            {
                _skillCooldowns[skill] -= Time.deltaTime;
        
                // 3. 만료된 경우 삭제 리스트에 추가
                if (_skillCooldowns[skill] <= 0f)
                    _expiredCooldowns.Add(skill);
            }

            // 4. 만료된 스킬을 실제 딕셔너리에서 제거
            foreach (var skill in _expiredCooldowns)
                _skillCooldowns.Remove(skill);
        }

        /// <summary> MotionEvent_MotionWarp.Execute()에서 호출. warpDuration = endTime - startTime. </summary>
        public void BeginMotionWarp(float warpDuration) => _motionWarp?.BeginMotionWarp(warpDuration);

        /// <summary> MotionEvent_MotionWarp.OnCompleteEvent()에서 호출. </summary>
        public void EndMotionWarp() => _motionWarp?.EndMotionWarp();

        private SkillConditionContext CreateContext(float distanceToTarget)
        {
            float currentHealth = 100f;
            float maxHealth     = 100f;

            if (_ownerDamageable != null)
            {
                currentHealth = _ownerDamageable.GetHealthPercent() * 100f;
                maxHealth     = 100f;
            }

            return new SkillConditionContext
            {
                CurrentHealth        = currentHealth,
                MaxHealth            = maxHealth,
                DistanceToTarget     = distanceToTarget,
                AllyCount            = GetAllyCount(),
                SpawnedUnitCount     = GetActiveSpawnedCount(),
                HasTarget            = distanceToTarget < float.MaxValue,
                CasterTransform      = transform,
                AllyLayer            = _detection != null ? _detection.AllyLayer : default,
                AllyDetectionRadius  = _detection != null ? _detection.AllyDetectionRadius : 10f
            };
        }

        public int GetActiveSpawnedCount()
        {
            _spawnedUnits.RemoveAll(t => t == null || !t.GetComponent<IDamageable>()?.IsAlive() == true);
            return _spawnedUnits.Count;
        }

        public void RegisterSpawnedUnit(Transform unit)
        {
            if (unit != null && !_spawnedUnits.Contains(unit))
                _spawnedUnits.Add(unit);
        }

        private int GetAllyCount()
        {
            if (_detection == null) return 0;
            return _detection.GetAllyCount();
        }

        public EnemyAttackInfo SelectAndExecuteSkill(float distanceToTarget)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
            {
                return null;
            }

            _spawnedUnits.RemoveAll(unit => unit == null);

            var availableSkills = GetAvailableSkills(distanceToTarget);
            if (availableSkills == null || availableSkills.Count == 0)
                return null;

            _currentSkill = _attackData.SelectRandomSkill(availableSkills);

            if (_currentSkill != null)
            {
                _skillCooldowns[_currentSkill] = _currentSkill.cooldown;
                ExecuteSkill(_currentSkill);
            }

            return _currentSkill;
        }

        private List<EnemyAttackInfo> GetAvailableSkills(float distanceToTarget)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
                return null;

            var context         = CreateContext(distanceToTarget);
            var availableSkills = new List<EnemyAttackInfo>();

            foreach (var skill in _attackData.skills)
            {
                if (skill.isAerialSkill)                     continue;
                if (_skillCooldowns.ContainsKey(skill))       continue;
                if (!skill.IsInRange(distanceToTarget))       continue;
                if (!skill.CheckCondition(context))           continue;

                availableSkills.Add(skill);
            }

            return availableSkills;
        }

        public bool HasAvailableSkillAtDistance(float distanceToTarget)
            => GetAvailableSkills(distanceToTarget)?.Count > 0;

        private void ExecuteSkill(EnemyAttackInfo skill)
        {
            _skillTargets.Clear();

            if (skill.skillType == SkillType.Attack)
            {
                var target     = _detection?.CurrentTarget;
                var damageable = target?.GetComponent<IDamageable>();
                if (damageable != null)
                    _skillTargets.Add(damageable);
                return;
            }

            var conditions = skill.conditionGroup?.conditions;
            if (conditions == null || conditions.Count == 0)
            {
                if (_ownerDamageable != null)
                    _skillTargets.Add(_ownerDamageable);
                return;
            }

            for (int i = 0; i < conditions.Count; ++i)
            {
                switch (conditions[i].type)
                {
                    case ConditionType.SelfHealthBased:
                        if (_ownerDamageable != null)
                            _skillTargets.Add(_ownerDamageable);
                        break;

                    case ConditionType.InjuredAllyNearby:
                        CacheInjuredAllies(conditions[i]);
                        break;

                    default: break;
                }
            }

            if (_skillTargets.Count == 0 && _ownerDamageable != null)
                _skillTargets.Add(_ownerDamageable);
        }

        private void CacheInjuredAllies(SkillCondition condition)
        {
            if (_detection == null) return;

            float      radius  = condition.maxRange > 0f ? condition.maxRange : _detection.AllyDetectionRadius;
            Collider[] allies  = Physics.OverlapSphere(transform.position, radius, _detection.AllyLayer);

            foreach (var ally in allies)
            {
                if (ally.transform == transform) continue;

                var damageable = ally.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive()) continue;

                float hp = damageable.GetHealthPercent();
                if (hp >= condition.minHealthPercent && hp <= condition.maxHealthPercent)
                    _skillTargets.Add(damageable);
            }
        }

        public void CheckMeleeAttackHit()
        {
            if (_currentSkill == null || _currentSkill.baseInfo.attackType != AttackType.Melee)
                return;

            var     phase          = _currentSkill.baseInfo.GetHitPhase(_currentHitPhaseIndex);
            Vector3 attackPosition = _attackOrigin.position
                + _attackOrigin.forward * phase.attackOffset.z
                + _attackOrigin.right   * phase.attackOffset.x
                + _attackOrigin.up      * phase.attackOffset.y;

            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, phase.attackRadius, _targetLayer);

            foreach (var hitCollider in hitColliders)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>()
                                      ?? hitCollider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;
                if (_hitTargets.Contains(damageable)) continue;

                if (phase.hitHeightRange > 0f)
                {
                    float closestY   = hitCollider.ClosestPoint(attackPosition).y;
                    float heightDiff = Mathf.Abs(closestY - attackPosition.y);
                    if (heightDiff > phase.hitHeightRange) continue;
                }

                var attackData = new AttackData
                {
                    damage             = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f),
                    poiseDamage        = phase.poiseDamage,
                    criticalMultiplier = 1.0f,
                    hitPoint           = hitCollider.ClosestPoint(attackPosition),
                    attackDirection    = _attackOrigin.forward,
                    reactionType       = phase.reactionType,
                    hitParticleName    = phase.hitParticleName,
                    pullForce          = phase.pullForce,
                    airborneForce      = phase.airborneForce,
                    hitPhaseIndex      = _currentHitPhaseIndex,
                    knockbackForce     = phase.knockBackForce,
                    knockbackDrag      = phase.knockBackDrag,
                    grabDuration          = phase.grabDuration,
                    hitHeightRange        = phase.hitHeightRange,
                    attacker              = _ownerActor,
                    victimForcedAnimKey   = phase.victimForcedAnimKey,
                };

                _hitTargets.Add(damageable);
                damageable.TakeDamage(attackData);
            }
        }

        public void SetCurrentSkill(EnemyAttackInfo skill)
        {
            _currentSkill         = skill;
            _currentHitPhaseIndex = 0;
        }

        public void ClearHitTargets()     => _hitTargets.Clear();

        public void BeginCurrentSkillTelegraph()
        {
            ClearTelegraphs();

            if (_currentSkill == null || !_currentSkill.useTelegraph)
                return;

            if (_currentSkill.telegraphShape != TelegraphShape.Circle)
            {
                Debug.LogWarning($"[EnemyCombat] 현재 Circle 텔레그래프만 지원합니다: {_currentSkill.telegraphShape}");
                return;
            }

            _telegraphHitPhaseIndex = 0;
            Vector3 position = GetTelegraphPosition(_telegraphHitPhaseIndex);
            GameObject instance = GameObjectManager.Instance.ShowFX(HeavyAttackTelegraphFXKey, position, Quaternion.identity, null, 0f);
            if (instance == null) return;

            ApplyTelegraphScale(instance, _telegraphHitPhaseIndex);
            RegisterTelegraph(instance);
        }

        public void UpdateTelegraphs()
        {
            if (_telegraphInstances.Count == 0) return;

            for (int i = _telegraphInstances.Count - 1; i >= 0; i--)
            {
                GameObject instance = _telegraphInstances[i];
                if (instance == null)
                {
                    _telegraphInstances.RemoveAt(i);
                    continue;
                }

                instance.transform.position = GetTelegraphPosition(_telegraphHitPhaseIndex);
                ApplyTelegraphScale(instance, _telegraphHitPhaseIndex);
            }
        }

        public void RegisterTelegraph(GameObject instance)
        {
            if (instance != null && !_telegraphInstances.Contains(instance))
                _telegraphInstances.Add(instance);
        }

        public void UnregisterTelegraph(GameObject instance)
        {
            if (instance == null) return;

            _telegraphInstances.Remove(instance);
        }

        public void ClearTelegraphs()
        {
            for (int i = _telegraphInstances.Count - 1; i >= 0; i--)
            {
                if (_telegraphInstances[i] != null)
                    Destroy(_telegraphInstances[i]);
            }

            _telegraphInstances.Clear();
        }

        public void SetEnableCollision(bool isCollisionEnable) =>
            _isCollisionEnabled = isCollisionEnable;

        public void SetTargetLayer(LayerMask targetLayer) =>
            _targetLayer = targetLayer;

        public void SetHitPhaseIndex(int index) =>
            _currentHitPhaseIndex = index;

        public Vector3 GetCurrentAttackPosition()
        {
            return GetAttackPosition(_currentHitPhaseIndex);
        }

        public Vector3 GetAttackPosition(int hitPhaseIndex)
        {
            if (_currentSkill == null) return _attackOrigin.position;
            var phase = _currentSkill.baseInfo.GetHitPhase(hitPhaseIndex);
            return _attackOrigin.position
                + _attackOrigin.forward * phase.attackOffset.z
                + _attackOrigin.right   * phase.attackOffset.x
                + _attackOrigin.up      * phase.attackOffset.y;
        }

        public float GetCurrentAttackRadius()
        {
            return GetAttackRadius(_currentHitPhaseIndex);
        }

        public float GetAttackRadius(int hitPhaseIndex)
        {
            if (_currentSkill == null) return 0f;
            return _currentSkill.baseInfo.GetHitPhase(hitPhaseIndex).attackRadius;
        }

        private Vector3 GetTelegraphPosition(int hitPhaseIndex)
        {
            Vector3 position = GetAttackPosition(hitPhaseIndex);
            if (!_alignTelegraphToGround) return position;

            Vector3 origin = position + Vector3.up * _telegraphGroundProbeHeight;
            float distance = _telegraphGroundProbeHeight + _telegraphGroundProbeDistance;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, _telegraphGroundLayers, QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y + _telegraphGroundYOffset;
                return position;
            }

            position.y += _telegraphGroundYOffset;
            return position;
        }

        private void ApplyTelegraphScale(GameObject instance, int hitPhaseIndex)
        {
            if (instance == null || _currentSkill == null) return;

            float scale = Mathf.Max(0.01f, GetAttackRadius(hitPhaseIndex) * _currentSkill.telegraphRadiusScale);
            instance.transform.localScale = Vector3.one * scale;
        }
    }
}
