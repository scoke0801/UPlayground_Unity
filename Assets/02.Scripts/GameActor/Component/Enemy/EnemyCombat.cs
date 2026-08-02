using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Ability;
using UPlayGround.UI;

namespace UPlayGround.Components
{
    public readonly struct EnemyAbilitySelectionDiagnostic
    {
        public EnemyAbilitySelectionDiagnostic(
            string abilityName,
            EnemyAbilityRejectReason rejectReason,
            float score,
            AbilityActivationResult activationResult = AbilityActivationResult.Success)
        {
            AbilityName = abilityName ?? string.Empty;
            RejectReason = rejectReason;
            Score = score;
            ActivationResult = activationResult;
        }

        public string AbilityName { get; }
        public EnemyAbilityRejectReason RejectReason { get; }
        public float Score { get; }
        public AbilityActivationResult ActivationResult { get; }
        public bool Accepted => RejectReason == EnemyAbilityRejectReason.None;
    }

    [Flags]
    public enum EnemyAbilityRejectReason
    {
        None = 0,
        MissingAttackInfo = 1 << 0,
        NotAISelectable = 1 << 1,
        MissingHitPhase = 1 << 2,
        AerialMismatch = 1 << 3,
        DiveMismatch = 1 << 4,
        LevelLocked = 1 << 5,
        ConditionFailed = 1 << 6,
        CategoryMismatch = 1 << 7,
        ActivationRejected = 1 << 8,
        BlockedByStrategy = 1 << 9,
        RepetitionLimit = 1 << 10,
        OutsideEffectiveMeleeRange = 1 << 11
    }

    public interface IEnemyAbilityRandomSource
    {
        float Next01();
        int NextIndex(int count);
    }

    public sealed class UnityEnemyAbilityRandomSource : IEnemyAbilityRandomSource
    {
        public static readonly UnityEnemyAbilityRandomSource Instance = new();

        public float Next01() => UnityEngine.Random.value;
        public int NextIndex(int count) => UnityEngine.Random.Range(0, count);
    }

    /// <summary>동일 seed와 후보 순서에서 동일 결과를 만드는 Replay/테스트용 난수원.</summary>
    public sealed class SeededEnemyAbilityRandomSource : IEnemyAbilityRandomSource
    {
        private readonly System.Random _random;

        public SeededEnemyAbilityRandomSource(int seed)
        {
            _random = new System.Random(seed);
        }

        public float Next01() => (float)_random.NextDouble();
        public int NextIndex(int count) => count > 0 ? _random.Next(count) : -1;
    }

    public static class EnemyAbilitySelectionPolicy
    {
        public static bool IsAISelectableAttack(AbilityAttackInfo attackInfo) =>
            attackInfo?.aiSelectable == true
            && attackInfo.baseInfo?.HasHitPhases == true;

        public static EnemyAbilityRejectReason Evaluate(
            AbilityAttackInfo attackInfo,
            in SkillConditionContext context,
            AbilityAttackCategory attackCategory,
            bool aerialOnly,
            bool diveOnly)
        {
            if (attackInfo == null)
                return EnemyAbilityRejectReason.MissingAttackInfo;

            EnemyAbilityRejectReason reasons = EnemyAbilityRejectReason.None;
            if (!attackInfo.aiSelectable)
                reasons |= EnemyAbilityRejectReason.NotAISelectable;
            if (attackInfo.baseInfo?.HasHitPhases != true)
                reasons |= EnemyAbilityRejectReason.MissingHitPhase;
            if (attackInfo.isAerialSkill != aerialOnly)
                reasons |= EnemyAbilityRejectReason.AerialMismatch;
            if (diveOnly && !attackInfo.isDiveAttack
                || !diveOnly && aerialOnly && attackInfo.isDiveAttack)
                reasons |= EnemyAbilityRejectReason.DiveMismatch;
            if (!attackInfo.IsUnlockedForLevel(context.CurrentLevel))
                reasons |= EnemyAbilityRejectReason.LevelLocked;
            if (!attackInfo.CheckCondition(context))
                reasons |= EnemyAbilityRejectReason.ConditionFailed;
            if (attackCategory != AbilityAttackCategory.None
                && attackInfo.attackCategory != attackCategory
                && attackInfo.attackCategory != AbilityAttackCategory.None)
                reasons |= EnemyAbilityRejectReason.CategoryMismatch;
            return reasons;
        }

        public static int SelectWeightedIndex(
            IReadOnlyList<float> weights,
            IEnemyAbilityRandomSource randomSource)
        {
            if (weights == null || weights.Count == 0)
                return -1;

            randomSource ??= UnityEnemyAbilityRandomSource.Instance;
            float total = 0f;
            for (var i = 0; i < weights.Count; i++)
                total += Mathf.Max(0f, weights[i]);

            if (total <= 0f)
                return randomSource.NextIndex(weights.Count);

            float roll = Mathf.Clamp01(randomSource.Next01()) * total;
            for (var i = 0; i < weights.Count; i++)
            {
                roll -= Mathf.Max(0f, weights[i]);
                if (roll <= 0f)
                    return i;
            }

            return weights.Count - 1;
        }
    }

    public readonly struct EnemyAttackThreat
    {
        public readonly MonsterActor Source;
        public readonly EnemyCombat Combat;
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly float TimeToHit;
        public readonly bool IsCollisionActive;
        public readonly int HitPhaseIndex;

        public EnemyAttackThreat(
            MonsterActor source,
            EnemyCombat combat,
            Vector3 position,
            float radius,
            float timeToHit,
            bool isCollisionActive,
            int hitPhaseIndex)
        {
            Source = source;
            Combat = combat;
            Position = position;
            Radius = radius;
            TimeToHit = timeToHit;
            IsCollisionActive = isCollisionActive;
            HitPhaseIndex = hitPhaseIndex;
        }
    }

    public class EnemyCombat : MonoBehaviour, UPlayGround.Combat.ICombatCollisionExecutor
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
        [HideInInspector, SerializeField] private AbilitySetSO _abilitySet;
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
        private ActorAbilitySystem _abilitySystem;
        private IDamageable _ownerDamageable;
        private EnemyDetection _detection;
        private EnemyAIContext _aiContext;

        private readonly struct AbilityCandidate
        {
            public readonly GameplayAbilitySO Ability;
            public readonly AbilityAttackInfo AttackInfo;
            public readonly float Score;

            public AbilityCandidate(
                GameplayAbilitySO ability,
                AbilityAttackInfo attackInfo,
                float score)
            {
                Ability = ability;
                AttackInfo = attackInfo;
                Score = score;
            }
        }

        private GameplayAbilitySO _currentAbility;
        private AbilityAttackInfo _currentSkill;
        private MotionSetAsset _currentAbilityMotionAsset;
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private int _currentHitPhaseIndex = 0;

        private SkillType _reservedSkillType = SkillType.None;
        private AbilityAttackCategory _reservedAttackCategory = AbilityAttackCategory.None;

        private readonly List<Transform> _spawnedUnits = new List<Transform>();
        private readonly List<IDamageable> _skillTargets = new List<IDamageable>();
        private readonly List<AbilityCandidate> _abilityCandidates = new();
        private readonly List<float> _abilitySelectionWeights = new();
        private readonly List<EnemyAbilitySelectionDiagnostic> _abilitySelectionDiagnostics = new();
        private readonly HashSet<GameplayAbilitySO> _visitedAbilities = new();
        private IEnemyAbilityRandomSource _abilityRandomSource =
            UnityEnemyAbilityRandomSource.Instance;
        private GameplayAbilitySO _lastSelectedAbility;
        private int _consecutiveAbilitySelections;
        private readonly List<TelegraphInstance> _telegraphInstances = new List<TelegraphInstance>();
        private readonly List<CombatHit> _detectedMeleeHits = new List<CombatHit>(32);
        private readonly Dictionary<int, Vector3> _telegraphHitPositions = new Dictionary<int, Vector3>();
        private CombatHitboxSet _hitboxSet;
        private string _requestedHitboxGroupId;
        private IReadOnlyList<string> _requestedHitboxGroupIds;
        private int _lastMeleeHitCheckFrame = -1;
        private int _meleeHitCheckRequestedFrame = -1;
        private float _lastTelegraphStartTime = -999f;
        private float _lastTelegraphDuration;
        private int _lastTelegraphHitPhaseIndex;
        private float _lastCollisionStartTime = -999f;

        // Danger Ring UI — 공격당 1개. 바닥 텔레그래프와 독립.
        private IActorDangerRingView _dangerRing;

        // ── Motion Warp 상태 ──────────────────────────────────────────
        // 진실 소스는 MotionWarpController. 본 클래스는 호환 프록시만 노출한다.
        private MotionWarpController _motionWarp;
        private CombatActionRunner _actionRunner;

        public float WarpRemainingTime => _motionWarp != null ? _motionWarp.WarpRemainingTime : 0f;
        public float WarpDuration      => _motionWarp != null ? _motionWarp.WarpDuration : 0f;
        public bool  IsMotionWarping   => _motionWarp != null && _motionWarp.IsMotionWarping;
        // ──────────────────────────────────────────────────────────────

        public AbilitySetSO      AbilitySet       => _abilitySet;
        public GameplayAbilitySO CurrentAbility   => _currentAbility;
        public AbilityAttackInfo CurrentSkill     => _currentSkill;
        public MotionSetAsset CurrentMotionAsset
        {
            get
            {
                if (_currentAbilityMotionAsset != null)
                    return _currentAbilityMotionAsset;
                return ActorAbilityMotionResolver.TryResolve(
                    _ownerActor,
                    _currentSkill,
                    out MotionSetAsset motionAsset)
                    ? motionAsset
                    : null;
            }
        }
        public int               CurrentLevel     => _ownerActor != null ? _ownerActor.Level : 1;
        // P3 3차: 충돌 윈도우의 단일 소유는 CombatActionRunner의 instance. 자체 플래그를 두지 않고 runner를 읽는다.
        public bool              IsPossibleCollide => _actionRunner != null && _actionRunner.IsCollisionActive;
        public SkillType         ReservedSkillType => _reservedSkillType;
        public AbilityAttackCategory ReservedAttackCategory => _reservedAttackCategory;
        public List<IDamageable> SkillTargetList  => _skillTargets;
        public bool              IsGuarding { get; set; } = false;
        public IReadOnlyList<EnemyAbilitySelectionDiagnostic> LastAbilitySelectionDiagnostics =>
            _abilitySelectionDiagnostics;

        public string BuildAbilitySelectionDiagnosticSummary()
        {
            if (_abilitySelectionDiagnostics.Count == 0)
                return "후보 평가 기록 없음";

            var parts = new string[_abilitySelectionDiagnostics.Count];
            for (var i = 0; i < _abilitySelectionDiagnostics.Count; i++)
            {
                EnemyAbilitySelectionDiagnostic diagnostic =
                    _abilitySelectionDiagnostics[i];
                parts[i] = diagnostic.Accepted
                    ? $"{diagnostic.AbilityName}:Eligible({diagnostic.Score:0.##})"
                    : $"{diagnostic.AbilityName}:{diagnostic.RejectReason}"
                      + $"/{diagnostic.ActivationResult}";
            }

            return string.Join(", ", parts);
        }

        public void SetAbilityRandomSource(IEnemyAbilityRandomSource randomSource)
        {
            _abilityRandomSource = randomSource ?? UnityEnemyAbilityRandomSource.Instance;
        }

        /// <summary> 현재 AttackState에서 히트한 대상 수 </summary>
        public int LastHitCount => _hitTargets.Count;

        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;

            _ownerDamageable = GetComponent<IDamageable>();
            _detection       = GetComponent<EnemyDetection>();
            _aiContext       = GetComponent<EnemyAIContext>();
            _ownerActor      = GetComponent<MonsterActor>();
            _abilitySystem   = _ownerActor?.Abilities;
            if (_ownerActor?.Definition != null)
                Init(_ownerActor.Definition);
            else
                Init(_abilitySet);

            // 워프 진실 소스는 MotionWarpController. 컴포넌트가 없으면 즉시 부착.
            _motionWarp = GetComponent<MotionWarpController>();
            if (_motionWarp == null)
                _motionWarp = gameObject.AddComponent<MotionWarpController>();
            _actionRunner = gameObject.GetOrAddComponent<CombatActionRunner>();
            _actionRunner.SetCollisionExecutor(this);
            _hitboxSet = gameObject.GetOrAddComponent<CombatHitboxSet>();
            _hitboxSet.Refresh();
        }

        /// <summary>액터가 사용할 공용 AbilitySet을 주입한다.</summary>
        public void Init(AbilitySetSO abilitySet)
        {
            if (abilitySet == null) return;

            _abilitySet = abilitySet;
            _abilitySystem ??= _ownerActor?.Abilities;
            if (_abilitySystem != null && _abilitySystem.AbilitySet != _abilitySet)
                _abilitySystem.SetAbilitySet(_abilitySet);
        }

        public void Init(ActorDefinitionSO definition)
        {
            if (definition == null) return;

            Init(definition.EffectiveAbilitySet);
            if (_ownerActor != null)
                SetTargetLayer(_ownerActor.GetAttackTargetLayerMask());
            else if (definition.targetLayerMask.value != 0)
                SetTargetLayer(definition.targetLayerMask);
        }

        private void LateUpdate()
        {
            // 히트 검출은 LateUpdate에서 수행한다(갓 적용된 애니메이션 포즈를 읽기 위함). 단, 공격 상태가
            // 이번 프레임에 검출을 요청했을 때만 수행해 "상태 틱 게이트"를 보존한다. 이렇게 해야 공격이
            // 중단되어 충돌 윈도우(IsPossibleCollide)가 닫히지 않은 채 누수돼도(예: 카운터 피격 중단)
            // 상태가 멈추면 요청이 없어 유령 히트가 발생하지 않는다.
            if (_meleeHitCheckRequestedFrame == Time.frameCount)
                CheckMeleeAttackHit();
        }

        /// <summary>
        /// 공격 상태(Update 단계)가 "이번 프레임 근접 히트 검출 필요"를 표시한다.
        /// 실제 Overlap 질의는 LateUpdate에서 수행해 갓 적용된 포즈를 읽는다(1프레임 지연 제거).
        /// </summary>
        public void RequestMeleeHitCheck() => _meleeHitCheckRequestedFrame = Time.frameCount;

        /// <summary> MotionEvent_MotionWarp.Execute()에서 호출. warpDuration = endTime - startTime. </summary>
        public void BeginMotionWarp(float warpDuration)
        {
            _motionWarp?.BeginMotionWarp(warpDuration);
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.MotionWarpStarted, _currentHitPhaseIndex);
        }

        /// <summary> MotionEvent_MotionWarp.OnCompleteEvent()에서 호출. </summary>
        public void EndMotionWarp()
        {
            _motionWarp?.EndMotionWarp();
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.MotionWarpEnded, _currentHitPhaseIndex);
        }

        // 메서드 그룹을 매 프레임 delegate로 변환하면 KCC UpdateVelocity 핫패스에서 GC 할당이 발생한다.
        // EvaluateVelocity 등에 넘길 때는 이 캐시를 사용할 것.
        private System.Action _endMotionWarpAction;
        public System.Action EndMotionWarpAction => _endMotionWarpAction ??= EndMotionWarp;

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

        public AbilityAttackInfo SelectAndExecuteSkill(float distanceToTarget)
        {
            return SelectAndExecuteSkill(distanceToTarget, ConsumeReservedAttackCategory());
        }

        public AbilityAttackInfo SelectAndExecuteSkill(
            float distanceToTarget,
            AbilityAttackCategory attackCategory)
        {
            _spawnedUnits.RemoveAll(unit => unit == null);

            List<AbilityCandidate> available =
                GetAvailableAbilities(distanceToTarget, attackCategory, false, false);
            if (available.Count == 0)
                return null;

            AbilityCandidate selected = SelectWeighted(available);
            if (!SetCurrentAbility(selected.Ability))
                return null;

            ExecuteSkill(_currentSkill);
            return _currentSkill;
        }

        private List<AbilityCandidate> GetAvailableAbilities(
            float distanceToTarget,
            AbilityAttackCategory attackCategory,
            bool aerialOnly,
            bool diveOnly)
        {
            _abilityCandidates.Clear();
            _abilitySelectionDiagnostics.Clear();
            _visitedAbilities.Clear();
            if (_abilitySet == null || _abilitySystem == null)
                return _abilityCandidates;

            SkillConditionContext context = CreateContext(distanceToTarget);
            foreach (GameplayAbilitySO ability in _abilitySet.GetRuntimeAbilities())
            {
                if (ability == null || !_visitedAbilities.Add(ability))
                    continue;
                if (!TryEvaluateAbility(
                        ability,
                        out AbilityAttackInfo attackInfo,
                        out AbilityActivationResult activationResult))
                {
                    _abilitySelectionDiagnostics.Add(new EnemyAbilitySelectionDiagnostic(
                        ability.name,
                        EnemyAbilityRejectReason.ActivationRejected,
                        0f,
                        activationResult));
                    continue;
                }

                EnemyAbilityRejectReason rejectReason =
                    EnemyAbilitySelectionPolicy.Evaluate(
                        attackInfo,
                        in context,
                        attackCategory,
                        aerialOnly,
                        diveOnly);
                if (rejectReason == EnemyAbilityRejectReason.None
                    && attackInfo.baseInfo.attackType == AttackType.Melee
                    && !EnemyAttackRangePolicy.CoversDistance(
                        ability,
                        attackInfo,
                        distanceToTarget,
                        CurrentLevel,
                        attackCategory,
                        aerialOnly,
                        diveOnly,
                        useMeleeApproachRange: true,
                        personalSpaceDistance: _aiContext?.PersonalSpaceDistance
                                               ?? EnemyAttackRangePolicy.DefaultPersonalSpaceDistance))
                {
                    rejectReason |= EnemyAbilityRejectReason.OutsideEffectiveMeleeRange;
                }
                if (rejectReason != EnemyAbilityRejectReason.None)
                {
                    _abilitySelectionDiagnostics.Add(new EnemyAbilitySelectionDiagnostic(
                        ability.name,
                        rejectReason,
                        0f));
                    continue;
                }

                EnemyCombatStrategySO strategy = ResolveCombatStrategy();
                float selectionScore = aerialOnly
                    ? attackInfo.aerialSkillWeight
                    : attackInfo.selectionWeight;
                EnemyAbilityRejectReason strategyRejectReason =
                    EvaluateStrategy(ability, strategy, ref selectionScore);
                if (strategyRejectReason != EnemyAbilityRejectReason.None)
                {
                    _abilitySelectionDiagnostics.Add(new EnemyAbilitySelectionDiagnostic(
                        ability.name,
                        strategyRejectReason,
                        0f));
                    continue;
                }

                _abilityCandidates.Add(new AbilityCandidate(
                    ability,
                    attackInfo,
                    Mathf.Max(0f, selectionScore)));
                _abilitySelectionDiagnostics.Add(new EnemyAbilitySelectionDiagnostic(
                    ability.name,
                    EnemyAbilityRejectReason.None,
                    Mathf.Max(0f, selectionScore)));
            }

            return _abilityCandidates;
        }

        private EnemyCombatStrategySO ResolveCombatStrategy() =>
            _aiContext?.CurrentPhase?.combatStrategyOverride
            ?? _aiContext?.BehaviorData?.combatStrategy;

        private EnemyAbilityRejectReason EvaluateStrategy(
            GameplayAbilitySO ability,
            EnemyCombatStrategySO strategy,
            ref float score)
        {
            if (strategy == null || ability == null)
                return EnemyAbilityRejectReason.None;

            if (HasAnyAbilityTag(ability, strategy.blockedAbilityTags))
                return EnemyAbilityRejectReason.BlockedByStrategy;

            if (HasAnyAbilityTag(ability, strategy.preferredAbilityTags))
                score += Mathf.Max(0f, strategy.preferredTagScoreBonus);

            if (ability != _lastSelectedAbility)
                return EnemyAbilityRejectReason.None;

            if (strategy.maxConsecutiveSameAbility > 0
                && _consecutiveAbilitySelections >= strategy.maxConsecutiveSameAbility)
                return EnemyAbilityRejectReason.RepetitionLimit;

            score *= Mathf.Clamp01(strategy.repeatedAbilityScoreMultiplier);
            return EnemyAbilityRejectReason.None;
        }

        private static bool HasAnyAbilityTag(
            GameplayAbilitySO ability,
            IReadOnlyList<UPlayGround.Gameplay.Tag.GameplayTag> requestedTags)
        {
            if (ability?.abilityTagIds == null
                || requestedTags == null
                || requestedTags.Count == 0)
                return false;

            for (var requestedIndex = 0; requestedIndex < requestedTags.Count; requestedIndex++)
            {
                for (var abilityIndex = 0; abilityIndex < ability.abilityTagIds.Count; abilityIndex++)
                {
                    if (ability.abilityTagIds[abilityIndex] == requestedTags[requestedIndex])
                        return true;
                }
            }

            return false;
        }

        private bool TryEvaluateAbility(
            GameplayAbilitySO ability,
            out AbilityAttackInfo attackInfo,
            out AbilityActivationResult activationResult)
        {
            attackInfo = null;
            if (ability == null || _abilitySystem == null)
            {
                activationResult = AbilityActivationResult.InvalidDefinition;
                return false;
            }

            activationResult = _abilitySystem.EvaluateAbility(
                ability,
                IsGrounded(),
                ResolveAbilityTarget(),
                out AbilityVariantDefinition variant);
            if (activationResult != AbilityActivationResult.Success)
                return false;

            if (UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                    variant,
                    out attackInfo))
                return true;

            activationResult = AbilityActivationResult.MissingExecutionData;
            return false;
        }

        public bool CanActivateAbility(GameplayAbilitySO ability) =>
            TryEvaluateAbility(ability, out _, out _);

        private bool TryActivateAbility(
            GameplayAbilitySO ability,
            out AbilityAttackInfo attackInfo)
        {
            attackInfo = null;
            _currentAbilityMotionAsset = null;
            if (ability == null || _abilitySystem == null)
                return false;

            MotionSetAsset resolvedMotionAsset = null;
            AbilityAttackInfo resolvedAttackInfo = null;
            AbilityActivationResult prepare = _abilitySystem.TryPrepareAbility(
                ability,
                IsGrounded(),
                ResolveAbilityTarget(),
                candidateVariant =>
                    UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                        candidateVariant,
                        out resolvedAttackInfo)
                    && ActorAbilityMotionResolver.TryResolve(
                        _ownerActor,
                        resolvedAttackInfo,
                        out resolvedMotionAsset),
                out AbilityExecutionHandle handle,
                out AbilityVariantDefinition variant);
            if (prepare != AbilityActivationResult.Success)
                return false;

            // 실행 데이터 사전검증은 TryPrepareAbility 내부에서 기존 Ability 취소 전에 끝난다.
            // 따라서 모션 누락은 현재 행동을 끊거나 비용/쿨다운을 소모하지 않는다.
            attackInfo = resolvedAttackInfo;

            AbilityActivationResult commit = _abilitySystem.Commit(handle);
            if (commit != AbilityActivationResult.Success)
            {
                attackInfo = null;
                _abilitySystem.Abort(handle);
                return false;
            }

            _currentAbilityMotionAsset = resolvedMotionAsset;
            return true;
        }

        private bool IsGrounded()
        {
            ActorMovementController movement =
                _ownerActor != null
                    ? _ownerActor.GetComponent<ActorMovementController>()
                    : null;
            return movement?.Motor == null
                   || movement.Motor.GroundingStatus.IsStableOnGround;
        }

        private GameActor ResolveAbilityTarget()
        {
            Transform target = _detection != null && _detection.HasTarget
                ? _detection.CurrentTarget
                : null;
            return target != null ? target.GetComponentInParent<GameActor>() : null;
        }

        public void CompleteCurrentAbility() =>
            _abilitySystem?.EndActiveAbility(true);

        public void CancelCurrentAbility() =>
            _abilitySystem?.EndActiveAbility(false);

        public bool HasAvailableSkillAtDistance(float distanceToTarget)
            => GetAvailableAbilities(
                distanceToTarget,
                AbilityAttackCategory.None,
                false,
                false).Count > 0;

        public bool HasAvailableSkillAtDistance(float distanceToTarget, AbilityAttackCategory attackCategory)
            => GetAvailableAbilities(
                distanceToTarget,
                attackCategory,
                false,
                false).Count > 0;

        public float GetMaxAttackRange() => ResolveMaxAttackRange();

        public float GetMaxMeleeAttackRange() =>
            ResolveMaxAttackRange(AttackType.Melee, groundOnly: true);

        public float GetPreferredMeleeApproachDistance(
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None)
        {
            float maxRange = ResolveMaxAttackRange(
                AttackType.Melee,
                groundOnly: true,
                attackCategory);
            return maxRange > 0f ? Mathf.Max(0.1f, maxRange - 0.1f) : 0f;
        }

        private float ResolveMaxAttackRange(
            AttackType? attackType = null,
            bool groundOnly = false,
            AbilityAttackCategory attackCategory = AbilityAttackCategory.None)
        {
            float maxRange = 0f;
            if (_abilitySet == null)
                return maxRange;

            foreach (GameplayAbilitySO ability in _abilitySet.GetRuntimeAbilities())
            {
                if (ability?.variants == null)
                    continue;

                for (var i = 0; i < ability.variants.Count; i++)
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                            ability.variants[i],
                            out AbilityAttackInfo attackInfo)
                        || !EnemyAbilitySelectionPolicy.IsAISelectableAttack(attackInfo)
                        || (attackType.HasValue
                            && attackInfo.baseInfo.attackType != attackType.Value)
                        || (groundOnly && attackInfo.isAerialSkill)
                        || (attackCategory != AbilityAttackCategory.None
                            && attackInfo.attackCategory != attackCategory
                            && attackInfo.attackCategory != AbilityAttackCategory.None))
                        continue;

                    maxRange = Mathf.Max(
                        maxRange,
                        EnemyAttackRangePolicy.ResolveEffectiveMaxDistance(
                            ability,
                            attackInfo,
                            _aiContext?.PersonalSpaceDistance
                            ?? EnemyAttackRangePolicy.DefaultPersonalSpaceDistance));
                }
            }

            return maxRange;
        }

        public bool HasAttackType(AttackType attackType)
        {
            if (_abilitySet == null)
                return false;

            foreach (GameplayAbilitySO ability in _abilitySet.GetRuntimeAbilities())
            {
                if (ability?.variants == null)
                    continue;
                for (int i = 0; i < ability.variants.Count; i++)
                    if (UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                            ability.variants[i],
                            out AbilityAttackInfo attackInfo)
                        && ActorAbilityMotionResolver.TryResolve(
                            _ownerActor,
                            attackInfo,
                            out _)
                        && EnemyAbilitySelectionPolicy.IsAISelectableAttack(attackInfo)
                        && attackInfo.baseInfo.attackType == attackType)
                        return true;
            }
            return false;
        }

        public void ReserveAttackCategory(AbilityAttackCategory attackCategory)
        {
            _reservedAttackCategory = attackCategory;
        }

        private AbilityAttackCategory ConsumeReservedAttackCategory()
        {
            var category = _reservedAttackCategory;
            _reservedAttackCategory = AbilityAttackCategory.None;
            return category;
        }

        private void ExecuteSkill(AbilityAttackInfo skill)
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

            if (_actionRunner != null
                && _actionRunner.IsCollisionActive
                && _currentHitPhaseIndex != _actionRunner.CurrentPhaseIndex)
            {
                SetHitPhaseIndex(_actionRunner.CurrentPhaseIndex);
            }

            var phase = _currentSkill.baseInfo.GetHitPhase(_currentHitPhaseIndex);
            if (_hitboxSet == null || !_hitboxSet.IsActive)
                return;

            // 프레임당 1회만 검출한다(스윕 기준 형상 이중 커밋 방지). LateUpdate 폴링과
            // 상태/애니메이션 이벤트가 같은 프레임에 함께 들어와도 안전하게 한다.
            if (_lastMeleeHitCheckFrame == Time.frameCount)
                return;
            _lastMeleeHitCheckFrame = Time.frameCount;

            // 무적 플레이어도 전달해 방어 레이어가 퍼펙트 도지/대시 회피를 판정한다.
            _hitboxSet.DetectActiveGroup(
                transform,
                _targetLayer,
                _hitTargets,
                _detectedMeleeHits,
                includeInvincibleTargets: true);

            foreach (CombatHit hit in _detectedMeleeHits)
            {
                var attackData = new AttackData
                {
                    damage             = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f),
                    poiseDamage        = phase.poiseDamage,
                    breakDamage        = phase.breakDamage,
                    reactionDuration   = phase.reactionDuration,
                    forceReaction      = phase.forceReaction,
                    forceBreakExpose   = phase.forceBreakExpose,
                    criticalMultiplier = 1.0f,
                    hitPoint           = hit.HitPoint,
                    attackDirection    = _attackOrigin.forward,
                    reactionType       = phase.reactionType,
                    hitParticleName    = phase.hitParticleName,
                    pullForce          = phase.pullForce,
                    airborneForce      = phase.airborneForce,
                    hitPhaseIndex      = _currentHitPhaseIndex,
                    knockbackForce     = phase.knockBackForce,
                    knockbackDrag      = phase.knockBackDrag,
                    grabDuration          = phase.grabDuration,
                    attacker              = _ownerActor,
                    victimForcedMotionSlot   = phase.victimForcedMotionSlot,
                    guaranteedReaction    = phase.guaranteedReaction,
                    defenseType           = _currentSkill != null ? _currentSkill.defenseType : AttackDefenseType.Parryable,
                    reactionData          = phase.reactionProfile?.Resolve(),
                };

                // 무적/회피 중인 대상은 TakeDamage까지 전달해 방어 판정과 피드백만 처리한다.
                // 같은 collision window 안에서 무적이 끝나면 실제 피격될 수 있어야 하므로 소비 대상으로 기록하지 않는다.
                bool consumeHitTarget = hit.Damageable.CanTakeDamage();
                hit.Damageable.ReceiveHit(HitRequest.FromAttackData(attackData));
                if (consumeHitTarget)
                    _hitTargets.Add(hit.Damageable);
            }
        }

        public bool TrySelectAerialAbility(
            float distanceToTarget,
            bool diveOnly,
            out GameplayAbilitySO ability)
        {
            ability = null;
            List<AbilityCandidate> available = GetAvailableAbilities(
                distanceToTarget,
                AbilityAttackCategory.None,
                true,
                diveOnly);
            if (available.Count == 0)
                return false;

            ability = SelectWeighted(available).Ability;
            return ability != null;
        }

        public bool HasAvailableAerialAbility(
            float distanceToTarget,
            bool diveOnly) =>
            GetAvailableAbilities(
                distanceToTarget,
                AbilityAttackCategory.None,
                true,
                diveOnly).Count > 0;

        public bool SetCurrentAbility(GameplayAbilitySO ability)
        {
            if (!TryActivateAbility(ability, out AbilityAttackInfo attackInfo))
                return false;

            _currentAbility       = ability;
            _currentSkill         = attackInfo;
            _currentHitPhaseIndex = 0;
            _lastTelegraphStartTime = -999f;
            _lastTelegraphDuration = 0f;
            _lastTelegraphHitPhaseIndex = 0;
            _lastCollisionStartTime = -999f;
            ClearTelegraphHitPositions();

            StartRunnerActionForSkill(attackInfo);
            return true;
        }

        private AbilityCandidate SelectWeighted(
            List<AbilityCandidate> candidates)
        {
            _abilitySelectionWeights.Clear();
            for (int i = 0; i < candidates.Count; i++)
                _abilitySelectionWeights.Add(candidates[i].Score);

            int selectedIndex = EnemyAbilitySelectionPolicy.SelectWeightedIndex(
                _abilitySelectionWeights,
                _abilityRandomSource);
            AbilityCandidate selected =
                candidates[Mathf.Clamp(selectedIndex, 0, candidates.Count - 1)];
            if (selected.Ability == _lastSelectedAbility)
                _consecutiveAbilitySelections++;
            else
            {
                _lastSelectedAbility = selected.Ability;
                _consecutiveAbilitySelections = 1;
            }

            return selected;
        }

        /// <summary>
        /// 선택된 스킬로 runner 액션(CurrentAction)을 시작한다.
        /// 지상(SelectAndExecuteSkill)·비행(SetCurrentSkill) 모든 적 공격이 이 경로를 통과해야
        /// IsPossibleCollide(= runner.IsCollisionActive)가 동작한다. (P3 3차)
        /// </summary>
        private void StartRunnerActionForSkill(AbilityAttackInfo skill)
        {
            if (skill?.baseInfo == null) return;
            _actionRunner?.StartAction(new AttackData
            {
                attacker = _ownerActor,
                motionAsset = CurrentMotionAsset,
                hitPhaseIndex = _currentHitPhaseIndex,
                defenseType = skill.defenseType,
            });
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
            _lastTelegraphStartTime = Time.time;
            _lastTelegraphDuration = ResolveDangerRingDuration(_currentSkill);
            _lastTelegraphHitPhaseIndex = clampedHitPhaseIndex;

            // 분기 1: 바닥 원형 FX 텔레그래프 — useTelegraph 일 때만
            if (_currentSkill.useTelegraph)
                BeginGroundTelegraph(clampedHitPhaseIndex, lockPositionOnStart);

            // 분기 2: Danger Ring UI — useDangerRing 일 때만 (텔레그래프와 무관)
            if (ShouldShowDangerRing(_currentSkill))
                BeginDangerRing();
        }

        private bool ShouldShowDangerRing(AbilityAttackInfo skill)
        {
            if (skill == null)
                return false;

            return skill.useDangerRing;
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

            GameObject instance = ActorSvc.Objects.ShowFX(fxKey, position, rotation, null, 0f);
            if (instance == null) return;

            _telegraphHitPositions[clampedHitPhaseIndex] = position;
            ApplyTelegraphScale(instance, clampedHitPhaseIndex);
            RegisterTelegraph(instance, clampedHitPhaseIndex, lockPositionOnStart, position, rotation);
        }

        private void BeginDangerRing()
        {
            float duration = ResolveDangerRingDuration(_currentSkill);
            _dangerRing = ActorSvc.UI?.CreateDangerRing(_ownerActor, _currentSkill, duration);
        }

        private float ResolveDangerRingDuration(AbilityAttackInfo skill)
        {
            // 1순위: 타임라인의 다음 Collision/투사체 발사 이벤트 중 더 먼저 시작되는 것까지 자동 산출 — 수동 오써링 불필요.
            // 수축이 가장 작아지는 순간이 실제 타격(Collision) 또는 투사체 발사와 자동 정렬된다.
            // IActorDangerRingView.TryGetCollisionProgress와 반드시 동일한 목표 선택 규칙을 사용해야 한다.
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

        public void CancelCurrentAction()
        {
            CancelCurrentAbility();
            _actionRunner?.CancelCurrentAction();
            ClearHitTargets();
            ClearTelegraphs();
            ClearTelegraphHitPositions();
            _motionWarp?.Cancel(WarpCancelReason.ExternalEnd);
        }

        public void SetEnableCollision(bool isCollisionEnable)
        {
            if (isCollisionEnable)
                BeginHitboxWindow();
            else
            {
                _hitboxSet?.EndGroup();
                // 윈도우 종료 시 그룹 요청을 비워 다음 윈도우에 직전 공격의 그룹이 잔존하지 않게 한다.
                _requestedHitboxGroupId = null;
                _requestedHitboxGroupIds = null;
            }

            // forwarding이 곧 윈도우의 권위 쓰기 — runner instance를 갱신한다.
            _actionRunner?.HandleTimelineEvent(
                isCollisionEnable ? CombatTimelineEventType.BeginCollision : CombatTimelineEventType.EndCollision,
                _currentHitPhaseIndex);

            // 충돌 판정이 켜지는 순간 = 실제 타격 순간. Danger Ring 수축을 최소 크기로 완료/해제한다.
            if (isCollisionEnable)
            {
                _lastCollisionStartTime = Time.time;
                CompleteDangerRing();
            }
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

        // ICombatCollisionExecutor — runner가 SetTargetLayerMask로 호출하므로 SetTargetLayer로 위임한다.
        public void SetTargetLayerMask(LayerMask targetLayerMask) => SetTargetLayer(targetLayerMask);

        public void SetHitboxGroup(string hitboxGroupId)
        {
            _requestedHitboxGroupId = string.IsNullOrWhiteSpace(hitboxGroupId)
                ? null
                : hitboxGroupId.Trim();
            _requestedHitboxGroupIds = null;
        }

        public void SetHitboxGroups(IReadOnlyList<string> hitboxGroupIds)
        {
            _requestedHitboxGroupIds = hitboxGroupIds != null && hitboxGroupIds.Count > 0
                ? hitboxGroupIds
                : null;
        }

        public void SetHitPhaseIndex(int index)
        {
            _currentHitPhaseIndex = index;
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.HitPhaseChanged, index);
        }

        /// <summary>
        /// 현재 Ability의 지정 히트 페이즈를 투사체용 공격 데이터로 스냅샷한다.
        /// </summary>
        public AttackData CreateProjectileAttackData(int hitPhaseIndex)
        {
            if (_currentSkill?.baseInfo == null)
                return null;

            AttackData data = PlayerAttackController.CreateFromAbility(
                _currentSkill,
                AttackKind.SkillAttack,
                hitPhaseIndex);
            if (data == null)
                return null;

            data.motionAsset = _currentAbilityMotionAsset;
            data.criticalMultiplier = 1f;
            data.attacker = _ownerActor;
            data.isProjectile = true;
            return data;
        }

        public ProjectileDefinitionSO GetProjectileDefinition(int hitPhaseIndex) =>
            _currentSkill?.baseInfo?.GetHitPhase(hitPhaseIndex)?.projectileDefinition;

        private void BeginHitboxWindow()
        {
            HitPhaseData phase = _currentSkill?.baseInfo?.GetHitPhase(_currentHitPhaseIndex);
            string groupId = !string.IsNullOrWhiteSpace(_requestedHitboxGroupId)
                ? _requestedHitboxGroupId
                : phase?.hitboxGroupId;
            List<string> groupIds = HitboxGroupIds.Normalize(groupId, _requestedHitboxGroupIds);
            bool activated;
            if (_hitboxSet == null)
                activated = false;
            else if (groupIds != null && groupIds.Count > 0)
                activated = _hitboxSet.BeginGroups(groupIds);
            else
                activated = _hitboxSet.BeginGroup(groupId);

            if (!activated)
            {
                Debug.LogError(
                    $"[EnemyCombat] 필수 HitBox 그룹 '{HitboxGroupIds.Describe(groupId, groupIds)}'을 찾지 못해 공격 판정을 중단합니다.",
                    this);
            }
        }

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
                + _attackOrigin.forward * phase.impactOffset.z
                + _attackOrigin.right   * phase.impactOffset.x
                + _attackOrigin.up      * phase.impactOffset.y;
        }

        public float GetCurrentThreatRadius()
        {
            return GetThreatRadius(_currentHitPhaseIndex);
        }

        public bool TryGetSwapEvadeThreat(
            Vector3 playerPosition,
            float beforeHitWindow,
            float afterHitGrace,
            float radiusPadding,
            out EnemyAttackThreat threat)
        {
            threat = default;
            if (_ownerActor == null || !_ownerActor.IsAlive()) return false;
            if (_currentSkill == null) return false;

            bool collisionGrace = IsPossibleCollide
                                  && Time.time - _lastCollisionStartTime <= Mathf.Max(0f, afterHitGrace);
            bool telegraphWindow = TryGetSwapEvadeTimeToHit(out float timeToHit)
                                   && !IsPossibleCollide
                                   && timeToHit >= 0f
                                   && timeToHit <= Mathf.Max(0f, beforeHitWindow);
            if (!collisionGrace && !telegraphWindow)
                return false;

            int hitPhaseIndex = collisionGrace
                ? _currentHitPhaseIndex
                : (_lastTelegraphStartTime > 0f ? _lastTelegraphHitPhaseIndex : _currentHitPhaseIndex);
            Vector3 attackPosition = GetAttackPosition(hitPhaseIndex);
            float radius = GetThreatRadius(hitPhaseIndex) + Mathf.Max(0f, radiusPadding);
            if (radius <= 0f) return false;

            Vector3 delta = playerPosition - attackPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > radius * radius)
                return false;

            threat = new EnemyAttackThreat(
                _ownerActor,
                this,
                attackPosition,
                radius,
                timeToHit,
                IsPossibleCollide,
                hitPhaseIndex);
            return true;
        }

        private bool TryGetSwapEvadeTimeToHit(out float timeToHit)
        {
            if (_lastTelegraphStartTime > 0f)
            {
                timeToHit = (_lastTelegraphStartTime + _lastTelegraphDuration) - Time.time;
                return true;
            }

            if (_ownerActor?.Animator != null &&
                _ownerActor.Animator.TryGetTimeUntilNextEvent<BeginCollisionEvent, SpawnProjectileEvent>(out timeToHit) &&
                timeToHit > 0f)
            {
                return true;
            }

            timeToHit = 0f;
            return false;
        }

        public float GetThreatRadius(int hitPhaseIndex)
        {
            if (_currentSkill == null) return 0f;
            return _currentSkill.baseInfo.GetHitPhase(hitPhaseIndex).targetingRange;
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
                + _attackOrigin.forward * phase.impactOffset.z
                + _attackOrigin.right   * phase.impactOffset.x
                + _attackOrigin.up      * phase.impactOffset.y;
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

        private static string GetTelegraphFXKey(AbilityAttackInfo skill)
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

            float scale = Mathf.Max(0.01f, GetThreatRadius(hitPhaseIndex) * _currentSkill.telegraphRadiusScale);
            instance.transform.localScale = Vector3.one * scale;
        }
    }
}
