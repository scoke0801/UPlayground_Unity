using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.Component
{
    public class EnemyCombat : MonoBehaviour
    {
        private const string DefaultCircleTelegraphFXKey = "EnemyHeavyAttackTelegraph_Circle";
        private const float  DefaultDangerRingDuration   = 0.6f;

        private sealed class TelegraphInstance
        {
            public GameObject instance;
            public int hitPhaseIndex;
            public bool lockPosition;
            public Vector3 lockedPosition;
            public Quaternion lockedRotation;
        }

        [Header("Combat Settings")]
        [HideInInspector, SerializeField] private EnemyAttackDataSO _attackData;
        [SerializeField] private Transform _attackOrigin;
        [HideInInspector, SerializeField] private LayerMask _targetLayer;

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
        private EnemyAttackCategory _reservedAttackCategory = EnemyAttackCategory.None;
        private bool _isCollisionEnabled;

        private readonly List<Transform> _spawnedUnits = new List<Transform>();
        private readonly List<IDamageable> _skillTargets = new List<IDamageable>();
        private readonly List<EnemyAttackInfo> _keysToProcess = new List<EnemyAttackInfo>();
        private readonly List<TelegraphInstance> _telegraphInstances = new List<TelegraphInstance>();
        private readonly Dictionary<int, Vector3> _telegraphHitPositions = new Dictionary<int, Vector3>();

        // Danger Ring UI — 공격당 1개. 바닥 텔레그래프와 독립.
        private UI_DangerRing _dangerRing;

        // ── Motion Warp 상태 ──────────────────────────────────────────
        // 진실 소스는 MotionWarpController. 본 클래스는 호환 프록시만 노출한다.
        private MotionWarpController _motionWarp;

        public float WarpRemainingTime => _motionWarp != null ? _motionWarp.WarpRemainingTime : 0f;
        public float WarpDuration      => _motionWarp != null ? _motionWarp.WarpDuration : 0f;
        public bool  IsMotionWarping   => _motionWarp != null && _motionWarp.IsMotionWarping;
        // ──────────────────────────────────────────────────────────────

        public EnemyAttackDataSO AttackData       => _attackData;
        public EnemyAttackInfo   CurrentSkill     => _currentSkill;
        public int               CurrentLevel     => _ownerActor != null ? _ownerActor.Level : 1;
        public bool              IsPossibleCollide => _isCollisionEnabled;
        public SkillType         ReservedSkillType => _reservedSkillType;
        public EnemyAttackCategory ReservedAttackCategory => _reservedAttackCategory;
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
            if (_ownerActor?.Definition != null)
                Init(_ownerActor.Definition);

            // 워프 진실 소스는 MotionWarpController. 컴포넌트가 없으면 즉시 부착.
            _motionWarp = GetComponent<MotionWarpController>();
            if (_motionWarp == null)
                _motionWarp = gameObject.AddComponent<MotionWarpController>();
        }

        /// <summary> MonsterActor.SetDefinition() 등에서 공격 데이터를 주입할 때 사용. </summary>
        public void Init(EnemyAttackDataSO data)
        {
            if (data != null)
                _attackData = data;
        }

        public void Init(ActorDefinitionSO definition)
        {
            if (definition == null) return;

            Init(definition.attackData);
            if (_ownerActor != null)
                SetTargetLayer(_ownerActor.GetAttackTargetLayerMask());
            else if (definition.targetLayerMask.value != 0)
                SetTargetLayer(definition.targetLayerMask);
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
                CurrentLevel        = CurrentLevel,
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
            return SelectAndExecuteSkill(distanceToTarget, ConsumeReservedAttackCategory());
        }

        public EnemyAttackInfo SelectAndExecuteSkill(float distanceToTarget, EnemyAttackCategory attackCategory)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
            {
                return null;
            }

            _spawnedUnits.RemoveAll(unit => unit == null);

            var availableSkills = GetAvailableSkills(distanceToTarget, attackCategory);
            if (availableSkills == null || availableSkills.Count == 0)
                return null;

            _currentSkill = _attackData.SelectRandomSkill(availableSkills);

            if (_currentSkill != null)
            {
                ClearTelegraphHitPositions();
                _skillCooldowns[_currentSkill] = _currentSkill.cooldown;
                ExecuteSkill(_currentSkill);
            }

            return _currentSkill;
        }

        private List<EnemyAttackInfo> GetAvailableSkills(float distanceToTarget, EnemyAttackCategory attackCategory = EnemyAttackCategory.None)
        {
            if (_attackData == null || _attackData.skills.Count == 0)
                return null;

            var context         = CreateContext(distanceToTarget);
            var availableSkills = new List<EnemyAttackInfo>();

            foreach (var skill in _attackData.skills)
            {
                if (skill.isAerialSkill)                     continue;
                if (_skillCooldowns.ContainsKey(skill))       continue;
                if (!skill.CanUse(distanceToTarget, context)) continue;
                if (!MatchesAttackCategory(skill, attackCategory)) continue;

                availableSkills.Add(skill);
            }

            return availableSkills;
        }

        public bool HasAvailableSkillAtDistance(float distanceToTarget)
            => GetAvailableSkills(distanceToTarget)?.Count > 0;

        public bool HasAvailableSkillAtDistance(float distanceToTarget, EnemyAttackCategory attackCategory)
            => GetAvailableSkills(distanceToTarget, attackCategory)?.Count > 0;

        public void ReserveAttackCategory(EnemyAttackCategory attackCategory)
        {
            _reservedAttackCategory = attackCategory;
        }

        private EnemyAttackCategory ConsumeReservedAttackCategory()
        {
            var category = _reservedAttackCategory;
            _reservedAttackCategory = EnemyAttackCategory.None;
            return category;
        }

        private static bool MatchesAttackCategory(EnemyAttackInfo skill, EnemyAttackCategory attackCategory)
        {
            if (attackCategory == EnemyAttackCategory.None)
                return true;

            return skill != null
                   && (skill.attackCategory == attackCategory || skill.attackCategory == EnemyAttackCategory.None);
        }

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
                    breakDamage        = phase.breakDamage,
                    reactionDuration   = phase.reactionDuration,
                    forceReaction      = phase.forceReaction,
                    forceBreakExpose   = phase.forceBreakExpose,
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
                    defenseType           = _currentSkill != null ? _currentSkill.defenseType : AttackDefenseType.Parryable,
                };

                _hitTargets.Add(damageable);
                damageable.TakeDamage(attackData);
            }
        }

        public void SetCurrentSkill(EnemyAttackInfo skill)
        {
            _currentSkill         = skill;
            _currentHitPhaseIndex = 0;
            ClearTelegraphHitPositions();
        }

        public void ClearHitTargets()     => _hitTargets.Clear();

        public void BeginCurrentSkillTelegraph()
        {
            BeginTelegraph(0, false);
        }

        /// <summary>
        /// 공격 예고 디스패처. 바닥 원형 FX(useTelegraph)와 Danger Ring(useDangerRing)을
        /// 각자 플래그로 독립 분기한다. 바닥 텔레그래프가 꺼져 있어도 Danger Ring은 단독 출력될 수 있다.
        /// </summary>
        public void BeginTelegraph(int hitPhaseIndex, bool lockPositionOnStart)
        {
            ClearTelegraphs();

            if (_currentSkill == null)
                return;

            int clampedHitPhaseIndex = GetClampedHitPhaseIndex(hitPhaseIndex);

            // 분기 1: 바닥 원형 FX 텔레그래프 — useTelegraph 일 때만
            if (_currentSkill.useTelegraph)
                BeginGroundTelegraph(clampedHitPhaseIndex, lockPositionOnStart);

            // 분기 2: Danger Ring UI — useDangerRing 일 때만 (텔레그래프와 무관)
            if (_currentSkill.useDangerRing)
                BeginDangerRing();
        }

        private void BeginGroundTelegraph(int clampedHitPhaseIndex, bool lockPositionOnStart)
        {
            if (_currentSkill.telegraphShape != TelegraphShape.Circle)
            {
                Debug.LogWarning($"[EnemyCombat] 현재 Circle 텔레그래프만 지원합니다: {_currentSkill.telegraphShape}");
                return;
            }

            Vector3 position = GetTelegraphPosition(clampedHitPhaseIndex);
            Quaternion rotation = GetTelegraphRotation();
            string fxKey = GetTelegraphFXKey(_currentSkill);

            GameObject instance = GameObjectManager.Instance.ShowFX(fxKey, position, rotation, null, 0f);
            if (instance == null) return;

            _telegraphHitPositions[clampedHitPhaseIndex] = position;
            ApplyTelegraphScale(instance, clampedHitPhaseIndex);
            RegisterTelegraph(instance, clampedHitPhaseIndex, lockPositionOnStart, position, rotation);
        }

        private void BeginDangerRing()
        {
            float duration = ResolveDangerRingDuration(_currentSkill);
            _dangerRing = UIManager.Instance?.CreateDangerRing(_ownerActor, _currentSkill, duration);
        }

        private float ResolveDangerRingDuration(EnemyAttackInfo skill)
        {
            // 1순위: 타임라인의 다음 Collision/투사체 발사 이벤트 중 더 먼저 시작되는 것까지 자동 산출 — 수동 오써링 불필요.
            // 수축이 가장 작아지는 순간이 실제 타격(Collision) 또는 투사체 발사와 자동 정렬된다.
            // UI_DangerRing.TryGetCollisionProgress와 반드시 동일한 목표 선택 규칙을 사용해야 한다.
            if (_ownerActor?.Animator != null &&
                _ownerActor.Animator.TryGetTimeUntilNextEvent<BeginCollisionEvent, SpawnProjectileEvent>(out float untilTarget) &&
                untilTarget > 0f)
                return untilTarget;

            // 2순위: 명시 오버라이드 (공격자 타임라인에 Collision/투사체 이벤트가 모두 없는 경우 등).
            if (skill != null && skill.dangerRingDuration > 0f)
                return skill.dangerRingDuration;

            return DefaultDangerRingDuration;
        }

        public void UpdateTelegraphs()
        {
            if (_telegraphInstances.Count == 0) return;

            for (int i = _telegraphInstances.Count - 1; i >= 0; i--)
            {
                TelegraphInstance entry = _telegraphInstances[i];
                GameObject instance = entry.instance;
                if (instance == null)
                {
                    _telegraphInstances.RemoveAt(i);
                    continue;
                }

                if (!entry.lockPosition)
                {
                    Vector3 position = GetTelegraphPosition(entry.hitPhaseIndex);
                    instance.transform.SetPositionAndRotation(
                        position,
                        GetTelegraphRotation());
                    _telegraphHitPositions[entry.hitPhaseIndex] = position;
                }
                else
                {
                    instance.transform.SetPositionAndRotation(entry.lockedPosition, entry.lockedRotation);
                }

                ApplyTelegraphScale(instance, entry.hitPhaseIndex);
            }
        }

        public void RegisterTelegraph(GameObject instance)
        {
            RegisterTelegraph(instance, 0, false, instance != null ? instance.transform.position : default, instance != null ? instance.transform.rotation : Quaternion.identity);
        }

        private void RegisterTelegraph(GameObject instance, int hitPhaseIndex, bool lockPosition, Vector3 lockedPosition, Quaternion lockedRotation)
        {
            if (instance == null || ContainsTelegraph(instance)) return;

            _telegraphInstances.Add(new TelegraphInstance
            {
                instance       = instance,
                hitPhaseIndex  = GetClampedHitPhaseIndex(hitPhaseIndex),
                lockPosition   = lockPosition,
                lockedPosition = lockedPosition,
                lockedRotation = lockedRotation,
            });
        }

        public void UnregisterTelegraph(GameObject instance)
        {
            if (instance == null) return;

            for (int i = _telegraphInstances.Count - 1; i >= 0; i--)
            {
                if (_telegraphInstances[i].instance == instance)
                    _telegraphInstances.RemoveAt(i);
            }
        }

        public void ClearTelegraphs()
        {
            for (int i = _telegraphInstances.Count - 1; i >= 0; i--)
            {
                if (_telegraphInstances[i]?.instance != null)
                    Destroy(_telegraphInstances[i].instance);
            }

            _telegraphInstances.Clear();

            // Danger Ring 정리 (바닥 FX와 함께)
            if (_dangerRing != null)
            {
                _dangerRing.Release();
                _dangerRing = null;
            }
        }

        public void ClearTelegraphHitPositions()
        {
            _telegraphHitPositions.Clear();
        }

        public void SetEnableCollision(bool isCollisionEnable)
        {
            _isCollisionEnabled = isCollisionEnable;

            // 충돌 판정이 켜지는 순간 = 실제 타격 순간. Danger Ring 수축을 최소 크기로 완료/해제한다.
            if (isCollisionEnable)
                CompleteDangerRing();
        }

        public void CompleteDangerRing()
        {
            if (_dangerRing == null)
                return;

            _dangerRing.CompleteNow();
            _dangerRing = null;
        }

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
            if (_currentSkill.useTelegraphPositionForHit
                && _telegraphHitPositions.TryGetValue(GetClampedHitPhaseIndex(hitPhaseIndex), out Vector3 telegraphPosition))
            {
                return telegraphPosition;
            }

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
            Vector3 position = GetRawTelegraphPosition(hitPhaseIndex);
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

        private Vector3 GetRawTelegraphPosition(int hitPhaseIndex)
        {
            if (_currentSkill == null) return _attackOrigin.position;

            if (_currentSkill.telegraphAnchorType == TelegraphAnchorType.TargetPosition)
            {
                Transform target = _detection != null && _detection.HasTarget ? _detection.CurrentTarget : null;
                if (target != null)
                    return target.position;
            }

            var phase = _currentSkill.baseInfo.GetHitPhase(hitPhaseIndex);
            return _attackOrigin.position
                + _attackOrigin.forward * phase.attackOffset.z
                + _attackOrigin.right   * phase.attackOffset.x
                + _attackOrigin.up      * phase.attackOffset.y;
        }

        private Quaternion GetTelegraphRotation()
        {
            Vector3 forward = _attackOrigin != null ? _attackOrigin.forward : transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(forward.normalized)
                : Quaternion.identity;
        }

        private int GetClampedHitPhaseIndex(int hitPhaseIndex)
        {
            int count = _currentSkill?.baseInfo?.hitPhases?.Count ?? 0;
            if (count <= 0) return 0;
            return Mathf.Clamp(hitPhaseIndex, 0, count - 1);
        }

        private static string GetTelegraphFXKey(EnemyAttackInfo skill)
        {
            if (!string.IsNullOrWhiteSpace(skill?.telegraphFXKey))
                return skill.telegraphFXKey;

            return skill?.telegraphShape switch
            {
                TelegraphShape.Circle => DefaultCircleTelegraphFXKey,
                _ => DefaultCircleTelegraphFXKey,
            };
        }

        private bool ContainsTelegraph(GameObject instance)
        {
            for (int i = 0; i < _telegraphInstances.Count; i++)
            {
                if (_telegraphInstances[i].instance == instance)
                    return true;
            }

            return false;
        }

        private void ApplyTelegraphScale(GameObject instance, int hitPhaseIndex)
        {
            if (instance == null || _currentSkill == null) return;

            float scale = Mathf.Max(0.01f, GetAttackRadius(hitPhaseIndex) * _currentSkill.telegraphRadiusScale);
            instance.transform.localScale = Vector3.one * scale;
        }
    }
}
