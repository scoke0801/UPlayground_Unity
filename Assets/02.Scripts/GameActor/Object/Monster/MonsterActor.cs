using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.AI.Debugging;
using UPlayGround.Components;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Diagnostics;
using UPlayGround.Group;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;
using UPlayGround.UI;
using UnityEngine.Serialization;
using Random = System.Random;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround
{
    public partial class MonsterActor : GameActor, ICombatResolvable
    {
        public event System.Action<MonsterActor> OnDied;
        public bool LastDeathWasSpecialBreak { get; private set; }

        [Tooltip("등급. ActorDefinitionSO 주입 시 덮어쓰며, 정의 없이 씬 배치된 경우 이 값을 폴백으로 사용.")]
        [HideInInspector, SerializeField] private MonsterActorGrade _grade = MonsterActorGrade.Normal;
        [Min(1)]
        [Tooltip("기준 레벨. ActorDefinitionSO 주입 시 덮어쓰며, 정의 없이 씬 배치된 경우 이 값을 폴백으로 사용.")]
        [HideInInspector, SerializeField] private int _level = 1;
        [SerializeField] private bool _isInvincible = false;
        [SerializeField] private GameObject _lockOnDecal = null;
        [SerializeField] private PoiseStat _poiseStat = null;
        [SerializeField] private MonsterBreakGauge _breakGauge = null;
        
        [HideInInspector, SerializeField] private EnemyDropTableSO _dropTable;

        [Tooltip("처치 시 파티에 합류시킬 캐릭터 타입. None이면 합류 없음.")]
        [HideInInspector, SerializeField] private CharacterActorType _recruitableAs = CharacterActorType.None;

        [Tooltip("처치 시 출전 파티 전원에게 지급할 경험치. 0이면 지급 없음.")]
        [HideInInspector, SerializeField] private long _expReward = 0;

        [Tooltip("처치 시 지급할 골드. 0이면 지급 없음.")]
        [HideInInspector, SerializeField] private int _goldReward = 0;

        // 재스폰 레벨 스케일링이 주입하는 런타임 보상. 음수면 미설정(기본 보상 사용).
        private long _runtimeExpReward = -1;
        private int _runtimeGoldReward = -1;

        [Header("AI Components")]
        [SerializeField] private EnemyDetection _detection;
        [FormerlySerializedAs("_brain")]
        [FormerlySerializedAs("_aiController")]
        [SerializeField] private EnemyAIController _groundAIController;
        [SerializeField] private EnemyFlyingAIController _flyingAIController;
        [SerializeField] private EnemyCombat _combat;

        protected float _maxHealth =>
            AbilitySystem?.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth) ?? 0f;

        protected float _currentHealth
        {
            get => AbilitySystem?.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Vital.Health) ?? 0f;
            set => AbilitySystem?.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, value);
        }
        protected bool _isDead = false;
        private IDisposable _lockOnSimulationLease;
        private int _externalHitReactionSuppressionCount;
        private AbilityExecutionHandle _triggeredReactionHandle;
        private ActorStateId? _triggeredReactionState;
        private bool _reactionAbilityCoverageWarned;
        
        protected IActorHpBarView _uiHpBar;
        private IActorBreakInteractionView _breakInteraction;   // 노출(브레이크 가능) 동안만 존재하는 F키 상호작용 UI

        // 기본 Airborne 수치(7~8)는 피격 경직으로 처리하고, 전용 launch급 공격만 공중 상태로 보낸다.
        private const float MinAirborneStateForce = 10f;
        
        public event Action<float, float> OnHealthChanged; // (current, max)
        public EnemyDetection Detection => _detection;
        public IEnemyAIController AIController => _groundAIController != null ? _groundAIController : _flyingAIController;
        public EnemyAIController GroundAIController => _groundAIController;
        public EnemyFlyingAIController FlyingAIController => _flyingAIController;
        public EnemyCombat Combat => _combat;
        public MonsterBreakGauge BreakGauge => _breakGauge;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public MonsterActorGrade Grade => _grade;
        public int Level => Mathf.Max(1, _level);
        public CharacterActorType RecruitableAs => _recruitableAs;
        public long BaseExpReward => _expReward;
        public int BaseGoldReward => _goldReward;

        /// <summary>
        /// 현재 행동불능/리액션 상태. 데미지 해석 시 통합 취약 배율(Vulnerability Multiplier) 산출에 쓰인다.
        /// 적 상태 진입(<see cref="State.EnemyActorState.OnEnter"/>)에서 갱신되며, 일반 상태로 복귀하면 None이 된다.
        /// </summary>
        public CombatReactionState CurrentReactionState { get; private set; } = CombatReactionState.None;

        /// <summary> 적 상태 머신이 상태 진입 시점에 호출한다(상태 진입과 동기). 외부 임의 호출 금지. </summary>
        internal void SetCurrentReactionState(CombatReactionState state) => CurrentReactionState = state;

        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Monster | ActorType.Combat;

            AbilitySystem.InitializeDefaultAttributes();
            ResetHealthFromStats();

            if (_detection == null) _detection = GetComponent<EnemyDetection>();
            if (_groundAIController == null) _groundAIController = GetComponent<EnemyAIController>();
            if (_flyingAIController == null) _flyingAIController = GetComponent<EnemyFlyingAIController>();
            if (_combat    == null) _combat    = GetComponent<EnemyCombat>();
            if (_poiseStat == null) _poiseStat = GetComponent<PoiseStat>();
            if (_breakGauge == null) _breakGauge = GetComponent<MonsterBreakGauge>();
            BindBreakGauge();
            // Definition 없이 직접 배치된 몬스터는 프리팹에 직렬화된 AbilitySet을 사용한다.
            _combat?.Init(_combat.AbilitySet);
            ApplyDefinitionData(Definition);
        }

        private void OnEnable()
        {
            // 비활성 자식은 활성화되는 시점에 Awake 캐시가 완성된다.
            // 부모 그룹의 Start 수집이 먼저 실행됐더라도 여기서 바인딩을 재시도한다.
            var group = GetComponentInParent<MonsterGroupController>();
            group?.EnsureMemberRegistered(this);
            SubscribeReactionAbilityTriggers();
        }

        protected override void Start()
        {
            // 프리팹에서 AI가 실수로 비활성화되면 EnemyAIController.ManagedTick이 돌지 않아
            // 첫 공격 뒤 행동 쿨다운이 영원히 0에 고정된다. 런타임 Freeze는 Start 이후에만
            // 발생하므로, 초기 구성 단계에서 지상 AI를 활성 상태로 정규화한다.
            if (_groundAIController != null && !_groundAIController.enabled)
                _groundAIController.enabled = true;

            base.Start();
        }

        private void AttachHpUI()
        {
            if (_uiHpBar != null) return;
            
            _uiHpBar = ActorSvc.UI.CreateHpBar(this);
            if (_uiHpBar != null)
            {
                OnHealthChanged += _uiHpBar.UpdateHealth;

                if (_poiseStat != null)
                    _poiseStat.ConnectUiBar(_uiHpBar);

                if (_breakGauge != null)
                    _breakGauge.ConnectUiBar(_uiHpBar);
            }
                
            _uiHpBar?.UpdateHealth(_currentHealth, _maxHealth);
        }

        #region IDamageable Implementation
        
        public CombatResult ReceiveHit(in HitRequest request)
            => CombatResolutionPipeline.Execute(this, request);

        public bool CanResolveHit(in HitRequest request)
        {
            if (request.IsSpecialBreak)
                return IsAlive() && _breakGauge != null && _breakGauge.IsExposed;

            if (_combat != null && _combat.IsGuarding)
            {
                if (MovementController.CurrentState is EnemyGuardState guardState)
                {
                    // AttackData 재구체화는 실제 가드 블록 처리에만 필요하므로 이 분기 안에서 할당한다.
                    guardState.OnAttackBlocked(request.ToReactionData());
                    return false;
                }
            }

            if (!CanTakeDamage())
            {
                RuntimeLog.TraceThrottled(
                    RuntimeLogCategory.Combat,
                    GetInstanceID(),
                    1f,
                    $"[MonsterActor] {gameObject.name}는 현재 데미지를 받을 수 없습니다.",
                    this);
                return false;
            }

            return true;
        }

        public CombatResult ResolveHit(in HitRequest request)
            => CombatResolutionPipeline.ResolveMonsterHit(this, request, BreakGauge);

        public CombatResult ApplyResolvedHit(in HitRequest request, in CombatResult combatResult)
        {
            DamageResult damageResult = combatResult.Damage;
            float finalDamage = combatResult.FinalDamage;

            if (damageResult.IsCritical)
            {
                RuntimeLog.Trace(
                    RuntimeLogCategory.Combat,
                    $"[MonsterActor] 크리티컬 히트! 데미지: {finalDamage}",
                    this);
            }

            AbilitySystem.ApplyResolvedDamage(finalDamage, request.Attacker?.AbilitySystem);

            // 충돌음은 피격자 소유. 이 지점이 몬스터가 받는 모든 피해(근접·투사체·잔류 판정)의 단일 깔때기다.
            if (finalDamage > 0f)
                CombatFeedbackDispatcher.PlayDamageImpact(combatResult);

            if (_uiHpBar == null) AttachHpUI();

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            AIController?.UpdatePhase(GetHealthPercent());

            if (request.IsSpecialBreak)
                return ApplySpecialBreakResult(request, combatResult);

            _detection?.AcquireTarget(combatResult.Hit.Attacker?.transform);

            // 순간 GameplayEvent는 현재 상태와 무관하게 매 피격마다 발급한다.
            // State.Hit 등에서의 중복 리액션은 Ability blockAny가 차단해야
            // 같은 프레임의 두 번째 피격 사건 자체가 유실되지 않는다.
            // 외부 리액션 억제 스코프는 기존 계약대로 사건 발급도 억제한다.
            bool canIssueHitTrigger = _externalHitReactionSuppressionCount <= 0;
            ReactionDecision reactionDecision = OnDamaged(
                combatResult.Hit,
                out ResourceChangeSet appliedResources);
            if (_currentHealth <= 0)
                OnDeath();
            if (IsAlive() && canIssueHitTrigger)
            {
                GameplayTag triggerTag = ResolveMonsterHitTrigger(
                    combatResult.Hit,
                    reactionDecision);
                if (triggerTag.IsValid())
                {
                    WarnIfReactionAbilityMissing(triggerTag, reactionDecision);
                    Abilities?.IssueTriggerEvent(
                        triggerTag,
                        combatResult.Hit.Attacker,
                        this,
                        new HitReactionTriggerPayload(
                            combatResult.Hit,
                            reactionDecision.TargetState));
                }
            }
            return CombatResolutionPipeline.WithMonsterAppliedResources(
                combatResult,
                reactionDecision,
                -appliedResources.PoiseDelta,
                -appliedResources.BreakDelta);
        }

        public void OnTakeFinishAttack(Vector3 attackDirection)
        {
            AbilitySystem.ApplyResolvedDamage(_currentHealth, null);

            if (_uiHpBar == null) AttachHpUI();

            if (MovementController != null)
            {
                // 수직 성분을 남기면 AddVelocity가 ForceUnground를 호출해 사체가 공중으로 솟는다.
                Vector3 finishDir = KnockbackDirectionResolver.ResolveHorizontal(
                    attackDirection,
                    null,
                    transform,
                    MovementController.Motor != null
                        ? MovementController.Motor.CharacterUp
                        : Vector3.up);
                MovementController.QueueVelocityChange(finishDir * 30.0f);
            }
            ActorSvc.Combat?.TrySpawnVitalOrb(VitalOrbTrigger.FinishAttackHit, transform.position);
            OnDeath();
        }

        /// <summary>
        /// 브레이크 특수공격 전용 데미지 진입점.
        /// 호출자는 타겟이 `BreakGauge.IsExposed` 상태임을 보장해야 한다.
        /// 일반 데미지 가드(`_isInvincible`, Guard, `OnDamaged` 흐름)를 의도적으로 우회한다.
        /// </summary>
        public void OnTakeSpecialBreakAttack(GameActor attacker, float damageByMaxHpRate, float fixedDamage, float minReferenceHealth)
        {
            if (!IsAlive()) return;

            Vector3 hitPoint = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;
            ReceiveHit(HitRequest.CreateSpecialBreak(
                attacker,
                this,
                damageByMaxHpRate,
                fixedDamage,
                minReferenceHealth,
                hitPoint));
            if (attacker is PlayerActor player)
                player.TrySpawnWeightRecovery(hitPoint, VitalOrbTrigger.FinishAttackHit, true);
        }

        private CombatResult ApplySpecialBreakResult(
            in HitRequest request,
            in CombatResult combatResult)
        {
            _breakGauge?.ConsumeBySpecialAttack();
            CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);
            CombatFeedbackDispatcher.ShowDamageFloater(
                CombatFeedbackContext.FromCombatResult(combatResult, transform.position));

            if (_currentHealth <= 0)
            {
                LastDeathWasSpecialBreak = true;
                OnDeath();
                return combatResult;
            }

            // 생존 시 — 브레이크 공격 마무리로 넘어뜨린다. Knockdown 모션이 없으면 Stun.
            if (MovementController != null)
            {
                // 브레이크 마무리 연출 튜닝값: 날아가는 거리 / 날아가는 시간 / 최대속도 / 누워있는 시간
                const float breakKnockbackDistance = 2.5f;
                const float breakKnockbackDuration = 0.35f;
                const float breakMaxKnockbackSpeed = 12f;
                const float breakDownDuration = 2.0f;

                bool hasKnockdown = Animator != null && Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown, true);
                MovementController.TransitionToState(hasKnockdown
                    ? new EnemyKnockdownState(
                        MovementController,
                        overrideDownDuration: breakDownDuration,
                        knockbackDistance: breakKnockbackDistance,
                        knockbackDuration: breakKnockbackDuration,
                        maxKnockbackSpeed: breakMaxKnockbackSpeed,
                        knockbackSource: request.Attacker != null ? request.Attacker.transform : null)
                    : new EnemyStunState(MovementController));
            }

            CombatReactionState state = MovementController != null
                                        && Animator != null
                                        && Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown, true)
                ? CombatReactionState.Knockdown
                : CombatReactionState.Stun;
            return CombatResolutionPipeline.WithReaction(
                combatResult,
                new ReactionDecision(true, true, false, state));
        }
        
        public bool IsAlive()          => _currentHealth > 0;
        public bool CanTakeDamage()
            => IsAlive() && !_isInvincible && !(MovementController?.CurrentState?.GrantsInvincibility ?? false);
        public Transform GetTransform() => transform;

        public void LockOn()
        {
            if (_lockOnDecal != null) _lockOnDecal.SetActive(true);
            _lockOnSimulationLease ??=
                ActorSvc.Simulation?.AcquireActiveLease(this, this, "LockOn");
        }

        public void UnLockOn()
        {
            if (_lockOnDecal != null) _lockOnDecal.SetActive(false);
            _lockOnSimulationLease?.Dispose();
            _lockOnSimulationLease = null;
        }

        public float GetHealthPercent() => _currentHealth / _maxHealth;
        public float GetCurrentHealth() => _currentHealth;

        #endregion
        
        #region Health Management
        
        public void ApplyHealingEffect(float amount)
        {
            if (!IsAlive()) return;
            
            float oldHealth   = _currentHealth;
            AbilitySystem.ApplyHealing(amount);
            float actualHeal  = _currentHealth - oldHealth;

            if (actualHeal <= 0f) return;

            if (_uiHpBar == null) AttachHpUI();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // 힐 위치: Center 소켓 우선, 없으면 루트 위치
            Vector3 floaterPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;
            ActorSvc.UI.ShowDamageFloaterHeal(floaterPos, actualHeal, FloatStyle.MonsterHeal);

            RuntimeLog.Trace(
                RuntimeLogCategory.Combat,
                $"[MonsterActor] {gameObject.name} 체력 회복: +{actualHeal:F1} HP (현재: {_currentHealth:F1}/{_maxHealth})",
                this);
        }
        
        public void SetHealth(float health)
        {
            bool wasAlive = IsAlive();
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);

            if (_uiHpBar == null) AttachHpUI();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            AIController?.UpdatePhase(GetHealthPercent());

            if (wasAlive && _currentHealth <= 0f)
                OnDeath();
        }
        
        #endregion

        protected virtual ReactionDecision OnDamaged(
            in HitContext hit,
            out ResourceChangeSet appliedResources)
        {
            float poiseDamageApplied = 0f;
            float breakDamageApplied = 0f;
            bool poiseBrokenNow = false;
            if (_poiseStat != null)
            {
                float previousPoise = _poiseStat.CurrentPoise;
                poiseBrokenNow = _poiseStat.TakePoiseDamage(hit.PoiseDamage);
                poiseDamageApplied = Mathf.Max(0f, previousPoise - _poiseStat.CurrentPoise);
            }

            bool isPoiseBroken = _poiseStat != null && _poiseStat.IsPoiseBroken;
            GetComponent<EnemyTacticalMemory>()?.NotifyTookDamage(hit, isPoiseBroken);
            ResolveOwningGroup()?.Memory?.NotifyMemberTookDamage();
            breakDamageApplied = _breakGauge != null ? _breakGauge.TakeBreakDamage(hit) : 0f;

            // 노출(브레이크 가능) 중에도 무방비 경직 없이 정상 리액션한다.
            // 받는 피해 증가(DamageTakenMultiplier)는 TakeDamage 단계에서 이미 적용된다.

            // [카운터 반격 적중] 패리/퍼펙트 가드 반격이 적중하면 무조건 '가벼운 밀쳐냄'을 적용한다.
            // 패리 직후 몬스터는 Stun 상태이고 CanPlayHitReaction이 "Stun"을 제외하므로 일반 리액션 경로로는
            // 어떤 힘도 적용되지 않는다. 또한 카운터는 패리 '보상'이므로 일반 피격 경직을 막는 등급 정책
            // (allowHit=false 등)에 묶이면 안 된다 → 정책 게이트 없이 직접 shove를 적용한다.
            if (hit.IsCounterAttack)
            {
                ApplyBreakStyleShove(hit.Attacker);
                CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);
                appliedResources = new ResourceChangeSet(0f, -poiseDamageApplied, -breakDamageApplied);
                return new ReactionDecision(true, true, false, CombatReactionState.Hit);
            }

            ReactionDecision reactionDecision = ReactionResolver.ResolveMonsterReaction(
                new MonsterReactionQuery(
                    poiseBrokenNow,
                    CanPlayHitReaction(hit),
                    ShouldEnterAirborneState(hit),
                    CanEnterKnockdownState(hit),
                    Grade,
                    Definition != null ? Definition.EffectiveCombatReactionPolicy : null),
                hit);

            bool appliedReactionForce = false;
            if (reactionDecision.ShouldApplyForce)
            {
                switch (hit.ReactionType)
                {
                    case AttackReactionType.KnockBack:
                        MovementController.AddPlanarKnockback(
                            ResolveKnockbackDirection(hit) * hit.KnockbackForce,
                            hit.KnockbackDrag);
                        appliedReactionForce = true;
                        break;

                    case AttackReactionType.Pull:
                        if (hit.Attacker != null)
                        {
                            Vector3 pullDir = (hit.Attacker.transform.position - transform.position).normalized;
                            pullDir.y = 0f;
                            MovementController.QueueVelocityChange(pullDir * hit.PullForce);
                        }
                        appliedReactionForce = true;

                        break;

                    case AttackReactionType.Airborne:
                    {
                        Vector3 launchDir = ResolveKnockbackDirection(hit);
                        Vector3 planarVelocity = launchDir * hit.KnockbackForce;
                        if (ShouldEnterAirborneState(hit))
                        {
                            MovementController.AddLaunch(
                                hit.AirborneForce,
                                planarVelocity,
                                hit.KnockbackDrag,
                                VerticalLaunchVelocityPolicy.Replace);
                        }
                        else
                        {
                            MovementController.AddPlanarKnockback(
                                planarVelocity,
                                hit.KnockbackDrag);
                        }
                        appliedReactionForce = true;
                        break;
                    }

                    case AttackReactionType.Grab:
                        break;
                }
            }

            // [Poise 브레이크 밀쳐냄] 일반 경직(Light/Hit/Heavy)으로 Poise가 깨졌고 공격 자체에 변위 반응이
            // 없을 때, 기존 반응 상태(보통 Stun)는 그대로 두고 '가벼운 밀쳐냄' 임펄스만 더한다.
            // 즉 펀ish 윈도우(무력화)는 유지하면서 넉백만 추가하는 break처럼 동작. 임펄스는 상태와 독립 적용된다.
            bool isPlainPoiseBreak = poiseBrokenNow
                && reactionDecision.ShouldEnterState
                && !appliedReactionForce
                && hit.ReactionType is AttackReactionType.Light
                    or AttackReactionType.Hit
                    or AttackReactionType.Heavy;

            if (reactionDecision.ShouldEnterState && isPlainPoiseBreak)
                ApplyShoveImpulse(hit.Attacker);

            // 리액션 상태 전환은 태그 트리거 Ability 경로가 단독으로 수행한다(저작 축 단일화).
            // 여기서 직접 TransitionToState 하던 폴백은 제거됐다 — 두 축이 같은 리액션을
            // 각자 표현하면 저작이 중복되고 어느 쪽이 이겼는지 추적할 수 없다.

            CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);
            appliedResources = new ResourceChangeSet(0f, -poiseDamageApplied, -breakDamageApplied);
            return reactionDecision;
        }

        private bool CanPlayHitReaction(in HitContext hit)
        {
            if (_externalHitReactionSuppressionCount > 0)
                return false;

            var state = MovementController?.CurrentState;
            if (state == null || state.SuppressesHitReaction)
                return false;

            return state.StateId is not (ActorStateId.Death or ActorStateId.Hit or ActorStateId.Airborne
                       or ActorStateId.Knockdown or ActorStateId.Stun or ActorStateId.Grabbed
                       or ActorStateId.SpecialBreakVictim)
                   && state.CanPlayHitReaction(hit);
        }

        /// <summary>
        /// 발급할 피격 트리거 태그를 고른다.
        ///
        /// 강인도 브레이크는 공격 자체의 리액션 태그로 보내면 안 된다.
        /// 리액션 Ability는 ownerTagRequirement.blockAny로 State.Hit 등을 막고 있어서,
        /// 콤보 도중(이미 Hit 상태)에 강인도가 깨진 경우 활성화가 거부되고 스턴이 유실된다.
        /// 전용 PoiseBreak 트리거를 받을 Ability가 있으면 그쪽으로 보낸다.
        /// </summary>
        private GameplayTag ResolveMonsterHitTrigger(
            in HitContext hit,
            in ReactionDecision reactionDecision)
        {
            // 리액션 상태로 들어가지 않는 피격도 사건 자체는 발급한다(피격 트리거 Ability용).
            // 이 경우 보정할 상태가 없으므로 공격의 리액션 타입을 그대로 쓴다.
            if (!reactionDecision.ShouldEnterState)
                return ResolveMonsterHitTrigger(hit.ReactionType);

            // 공격이 요구하지 않은 행동 불능 상태로 귀결됐다면 그 원인은 강인도 브레이크다.
            bool poiseBreakReaction =
                reactionDecision.TargetState is CombatReactionState.Stun
                    or CombatReactionState.Knockdown
                && hit.ReactionType is not (AttackReactionType.Stun
                    or AttackReactionType.Knockdown);

            if (poiseBreakReaction
                && Abilities != null
                && Abilities.TryGetRequestTriggerAbility(
                    GameplayTags.Trigger_Monster_Hit_PoiseBreak,
                    out _))
            {
                return GameplayTags.Trigger_Monster_Hit_PoiseBreak;
            }

            // 실행될 Ability와 실제 진입 상태의 범주를 맞춘다.
            // 태그를 공격의 리액션 타입에서만 뽑으면, 승격 조건 미달로 상태가 강등된 경우
            // (예: airborneForce 부족 → Airborne 요청이지만 Hit 상태) 엉뚱한 Ability가 실행된다.
            return reactionDecision.TargetState switch
            {
                CombatReactionState.Airborne => GameplayTags.Trigger_Monster_Hit_Airborne,
                CombatReactionState.Grabbed => GameplayTags.Trigger_Monster_Hit_Grab,
                CombatReactionState.Knockdown => GameplayTags.Trigger_Monster_Hit_Knockdown,
                CombatReactionState.Stun => GameplayTags.Trigger_Monster_Hit_Stun,
                _ => ResolveHitCategoryTrigger(hit.ReactionType),
            };
        }

        /// <summary>
        /// 일반 Hit 상태로 들어갈 때의 강도 구분. Light/Heavy/KnockBack/Pull은 보존하고,
        /// 승격에 실패해 강등된 Airborne/Knockdown/Stun/Grab은 기본 Hit으로 접는다.
        /// </summary>
        private static GameplayTag ResolveHitCategoryTrigger(
            AttackReactionType reactionType) => reactionType switch
        {
            AttackReactionType.Light => GameplayTags.Trigger_Monster_Hit_Light,
            AttackReactionType.Heavy => GameplayTags.Trigger_Monster_Hit_Heavy,
            AttackReactionType.KnockBack => GameplayTags.Trigger_Monster_Hit_KnockBack,
            AttackReactionType.Pull => GameplayTags.Trigger_Monster_Hit_Pull,
            _ => GameplayTags.Trigger_Monster_Hit_Hit,
        };

        private static GameplayTag ResolveMonsterHitTrigger(
            AttackReactionType reactionType) => reactionType switch
        {
            AttackReactionType.Light => GameplayTags.Trigger_Monster_Hit_Light,
            AttackReactionType.Hit => GameplayTags.Trigger_Monster_Hit_Hit,
            AttackReactionType.Heavy => GameplayTags.Trigger_Monster_Hit_Heavy,
            AttackReactionType.KnockBack => GameplayTags.Trigger_Monster_Hit_KnockBack,
            AttackReactionType.Stun => GameplayTags.Trigger_Monster_Hit_Stun,
            AttackReactionType.Pull => GameplayTags.Trigger_Monster_Hit_Pull,
            AttackReactionType.Airborne => GameplayTags.Trigger_Monster_Hit_Airborne,
            AttackReactionType.Knockdown => GameplayTags.Trigger_Monster_Hit_Knockdown,
            AttackReactionType.Grab => GameplayTags.Trigger_Monster_Hit_Grab,
            _ => default,
        };

        /// <summary>
        /// 리액션이 필요한데 해당 트리거를 받을 Ability가 AbilitySet에 없으면 알린다.
        /// 폴백이 사라졌으므로 이 경우 몬스터는 아무 반응도 하지 않는다 — 조용히 넘어가면 안 된다.
        /// </summary>
        private void WarnIfReactionAbilityMissing(
            GameplayTag triggerTag,
            in ReactionDecision reactionDecision)
        {
            if (_reactionAbilityCoverageWarned || !reactionDecision.ShouldEnterState)
                return;

            if (Abilities != null
                && Abilities.TryGetRequestTriggerAbility(triggerTag, out _))
            {
                return;
            }

            _reactionAbilityCoverageWarned = true;
            Debug.LogError(
                $"[MonsterActor] '{gameObject.name}'의 AbilitySet에 피격 리액션 Ability가 없어 "
                + $"리액션이 재생되지 않습니다. trigger={triggerTag.TagName}. "
                + "GA_Monster_Hit_* Ability를 AbilitySet에 추가하세요.",
                this);
        }

        private void SubscribeReactionAbilityTriggers()
        {
            if (Abilities == null)
                return;
            Abilities.AbilityTriggerRequested -= OnReactionAbilityTriggerRequested;
            Abilities.AbilityTriggerRequested += OnReactionAbilityTriggerRequested;
            if (MovementController != null)
            {
                MovementController.OnStateChanged -= OnReactionAbilityStateChanged;
                MovementController.OnStateChanged += OnReactionAbilityStateChanged;
            }
        }

        private void UnsubscribeReactionAbilityTriggers()
        {
            if (Abilities != null)
                Abilities.AbilityTriggerRequested -= OnReactionAbilityTriggerRequested;
            if (MovementController != null)
                MovementController.OnStateChanged -= OnReactionAbilityStateChanged;
        }

        private void OnReactionAbilityTriggerRequested(AbilityTriggerRequest request)
        {
            if (!request.TriggerTag.IsChildOf(GameplayTags.Trigger_Monster_Hit))
                return;

            if (!request.TriggerEvent.HasValue
                || request.TriggerEvent.Value.Payload is not HitReactionTriggerPayload payload
                || payload.ReactionState == CombatReactionState.None
                || MovementController == null)
            {
                Abilities?.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.InvalidDefinition);
                return;
            }

            // 이전 트리거 리액션 실행을 먼저 명시적으로 회수한다.
            // concurrency(CancelExisting)가 대신 정리해 주기를 기대하면
            // 정책을 바꾸는 순간 실행 핸들이 조용히 누수된다.
            ReleaseTriggeredReaction(false);

            GameActorState state = CreateTriggeredMonsterReactionState(payload);
            if (state == null)
            {
                Abilities.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.InvalidDefinition);
                return;
            }

            bool grounded = MovementController.Motor == null
                || MovementController.Motor.GroundingStatus.IsStableOnGround;
            AbilityActivationResult prepared = Abilities.TryPrepareAbility(
                request.Ability,
                grounded,
                null,
                out AbilityExecutionHandle handle,
                out _,
                request.TriggerEvent);
            if (prepared != AbilityActivationResult.Success)
            {
                Abilities.ReportTriggerRejected(request.Ability, prepared);
                return;
            }

            // 상태 전환보다 Commit을 먼저 수행한다(spec §4).
            // 반대로 두면 Commit 실패 시 "리액션 상태에는 들어갔는데 Ability는 없는"
            // 상태가 되고, 핸들이 없어 종료 훅이 그 실행을 영영 회수하지 못한다.
            AbilityActivationResult committed = Abilities.Commit(handle);
            if (committed != AbilityActivationResult.Success)
            {
                Abilities.Abort(handle);
                Abilities.ReportTriggerRejected(request.Ability, committed);
                return;
            }

            _triggeredReactionHandle = handle;
            _triggeredReactionState = state.StateId;

            if (!MovementController.TryTransitionToState(state))
            {
                // 커밋 롤백: 상태 없이 활성 실행만 남지 않도록 즉시 종료한다.
                ReleaseTriggeredReaction(false);
                Abilities.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.StateTransitionRejected);
                return;
            }

            Abilities.BindActiveExecutionToTrigger(handle, request);
        }

        /// <summary>진행 중인 트리거 리액션 실행을 종료하고 추적 상태를 비운다.</summary>
        private void ReleaseTriggeredReaction(bool completed)
        {
            if (!_triggeredReactionHandle.IsValid)
            {
                _triggeredReactionState = null;
                return;
            }

            AbilityExecutionHandle handle = _triggeredReactionHandle;
            _triggeredReactionHandle = default;
            _triggeredReactionState = null;
            Abilities?.EndAbility(handle, completed);
        }

        private GameActorState CreateTriggeredMonsterReactionState(
            in HitReactionTriggerPayload payload) => payload.ReactionState switch
        {
            CombatReactionState.Airborne =>
                new EnemyAirborneState(MovementController, payload.Hit),
            CombatReactionState.Grabbed =>
                new EnemyGrabbedState(MovementController, payload.Hit),
            CombatReactionState.Stun =>
                new EnemyStunState(MovementController, payload.Hit),
            CombatReactionState.Knockdown =>
                new EnemyKnockdownState(MovementController, payload.Hit),
            CombatReactionState.Hit =>
                new EnemyHitState(MovementController, payload.Hit),
            _ => null,
        };

        private void OnReactionAbilityStateChanged(
            GameActorState previous,
            GameActorState current)
        {
            if (!_triggeredReactionHandle.IsValid
                || !_triggeredReactionState.HasValue
                || previous?.StateId != _triggeredReactionState.Value
                || current?.StateId == _triggeredReactionState.Value)
                return;

            bool completed = current?.StateId != ActorStateId.Death;
            ReleaseTriggeredReaction(completed);
        }

        private bool ShouldEnterAirborneState(in HitContext hit)
        {
            if (hit.ReactionType != AttackReactionType.Airborne)
                return false;

            if (hit.AirborneForce >= MinAirborneStateForce)
                return true;

            return false;
        }

        private bool CanEnterKnockdownState(in HitContext hit)
        {
            if (hit.ReactionType != AttackReactionType.Knockdown)
                return false;

            return Animator != null && Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Knockdown, true);
        }

        protected virtual void OnDeath()
        {
            if (_isDead) return;
            _isDead = true;
            Abilities?.HandleOwnerDeath();

            RuntimeLog.Trace(
                RuntimeLogCategory.Combat,
                $"[MonsterActor] {gameObject.name} 사망!",
                this);

            CombatTelemetrySession.NotifyMonsterDeath(this);
            GetComponent<EncounterReplayRecorder>()?.EndAndSave("death", "몬스터 사망");
            ResolveOwningGroup()?.UnregisterMember(this);
            MovementController.TransitionToState(new EnemyDeathState(MovementController));

            NotifyQuestMonsterKill();
            NotifyRecipeMonsterKill();
            NotifyWorldStateKill();
            NotifyCodexKill();
            SpawnDropItems();
            GrantPartyExp();
            GrantGold();
            TryRecruitToParty();
            OnDied?.Invoke(this);

            if (_uiHpBar != null)
            {
                ReleaseHpBar();
            }

            UnregisterExposed();
            HideBreakInteraction();

            MovementController.Motor.SetCapsuleCollisionsActivation(false);
        }

        private MonsterGroupController ResolveOwningGroup()
            => AIController?.Group
               ?? GetComponentInParent<MonsterGroupController>(includeInactive: true);

        private void NotifyQuestMonsterKill()
        {
            ActorSvc.QuestProgress?.NotifyMonsterKill(ActorId);
        }

        private void NotifyRecipeMonsterKill()
        {
            ActorSvc.RecipeProgress?.NotifyMonsterKill(ActorId);
        }

        /// <summary>
        /// 몬스터 처치를 월드 상태에 기록한다.
        /// 재스폰 대상(일반 필드 몬스터)이면 재스폰 예약으로, 아니면(보스/합류 몬스터 등) 영구 처치로 기록한다.
        /// 재스폰으로 생성된 몬스터는 SceneEntityId가 없지만 MonsterRespawnManager가 guid를 복원한다.
        /// </summary>
        private void NotifyWorldStateKill()
        {
            var entityId = GetComponent<SceneEntityId>();
            string guid = entityId != null && entityId.HasGuid ? entityId.Guid : null;
            ActorSvc.MonsterLifecycle?.RecordDeath(this, guid);
        }

        private void NotifyCodexKill()
        {
            ActorSvc.MonsterCodex?.RecordKill(ActorId, CurrentElement);
        }

        private void TryRecruitToParty()
        {
            if (_recruitableAs == CharacterActorType.None) return;
            Svc.Party?.UnlockCharacter(_recruitableAs);
        }

        private void GrantPartyExp()
        {
            long exp = _runtimeExpReward >= 0 ? _runtimeExpReward : _expReward;
            if (exp <= 0) return;
            float multiplier = Svc.MonsterCodexReader?.GetExpMultiplier(ActorId) ?? 1f;
            double adjusted = Math.Round(exp * (double)multiplier, MidpointRounding.AwayFromZero);
            long granted = adjusted >= long.MaxValue ? long.MaxValue : (long)Math.Max(0d, adjusted);
            Svc.Party?.AwardBattleExp(granted);
        }

        private void GrantGold()
        {
            int gold = _runtimeGoldReward >= 0 ? _runtimeGoldReward : _goldReward;
            if (gold <= 0 || Svc.Inventory == null) return;
            Svc.Inventory.Gold += gold;
        }

        private void SpawnDropItems()
        {
            if (_dropTable == null) return;

            var items = Svc.Item.GetDropItemList(_dropTable);
            foreach (var item in items)
            {
                ActorSvc.Objects.SpawnItem(item, transform.position);
            }
        }

        public void PlayDissolveAndDestroy(float duration)
        {
            MovementController.Motor.enabled = false;
            _dissolveController.StartDissolve(duration);
        }
        
        public void SetInvincible(bool invincible) => _isInvincible = invincible;

        public void SetExternalHitReactionSuppressed(bool suppressed)
        {
            _externalHitReactionSuppressionCount = suppressed
                ? _externalHitReactionSuppressionCount + 1
                : Mathf.Max(0, _externalHitReactionSuppressionCount - 1);
        }

        /// <summary>
        /// ActorDefinitionSO 주입 시 stats/poiseData를 재적용한다.
        /// Awake 이후 ActorSpawnManager가 호출하므로 HP도 함께 갱신.
        /// </summary>
        public override void SetDefinition(ActorDefinitionSO definition)
        {
            base.SetDefinition(definition);
            ApplyDefinitionData(definition);
        }

        /// <summary>
        /// 재스폰 레벨 스케일링용 런타임 레벨 오버라이드.
        /// definition.monsterScaling 기반으로 스탯을 재계산해 베이스 값을 교체하고 HP를 풀로 리셋한다.
        /// 전투 중이 아닌 갓 스폰된(또는 씬 로드 직후) 몬스터에만 호출할 것.
        /// scaling이 없으면 스탯은 그대로 두고 레벨 표기만 바꾼다.
        /// </summary>
        /// <param name="runtimeLevel">적용할 레벨 (1 이상)</param>
        /// <param name="difficultyOverride">0보다 크면 scaling의 difficultyMultiplier 대신 사용</param>
        public void ApplyRuntimeLevel(int runtimeLevel, float difficultyOverride = 0f)
        {
            runtimeLevel = Mathf.Max(1, runtimeLevel);

            MonsterScalingSO scaling = Definition != null ? Definition.EffectiveMonsterScaling : null;
            if (scaling == null)
            {
                _level = runtimeLevel;
                RuntimeLog.Trace(
                    RuntimeLogCategory.Combat,
                    $"[MonsterActor] {gameObject.name} monsterScaling이 없어 레벨 표기만 변경 (Lv.{runtimeLevel})",
                    this);
                return;
            }

            var stats = MonsterStatCalculator.CalculateAtLevel(scaling, Definition, runtimeLevel, difficultyOverride);
            AbilitySystem.SetAttributeBases(stats);

            _level = runtimeLevel;

            // Health/Poise/Break는 ASC Attribute 기본값 교체 결과를 각 런타임 뷰가 읽는다.
            ResetHealthFromStats();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// 재스폰 레벨에 맞춘 런타임 보상(경험치/골드)을 주입한다. 음수는 0으로 취급.
        /// 미호출 시 정의 기본 보상을 사용한다.
        /// </summary>
        public void SetRuntimeRewards(long expReward, int goldReward)
        {
            _runtimeExpReward = System.Math.Max(0, expReward);
            _runtimeGoldReward = Mathf.Max(0, goldReward);
        }

        /// <summary>
        /// 이번 스폰 인스턴스의 파티 영입만 비활성화한다.
        /// 공유 ActorDefinition/Profile의 일반 몬스터 영입 설정은 유지해야 하는 사이클 보스가 사용한다.
        /// </summary>
        public void SuppressRuntimePartyRecruitment()
        {
            _recruitableAs = CharacterActorType.None;
        }

        private void ApplyDefinitionData(ActorDefinitionSO definition)
        {
            if (definition == null) return;

            // 메타(등급/레벨)는 정의가 권위 소스. 정의 값으로 덮어쓴다.
            _grade = definition.EffectiveGrade;
            _level = definition.EffectiveLevel;
            _recruitableAs = definition.EffectiveRecruitableAs;
            _expReward = definition.EffectiveExpReward;
            _goldReward = definition.EffectiveGoldReward;

            // ActorDefinition의 런타임 Attribute 기본값은 Profile만 권위로 사용한다.
            if (definition.attributeProfile != null)
            {
                AbilitySystem.InitializeDefaultAttributes();
                if (!AbilitySystem.InitializeAttributes(
                        definition.attributeProfile, out string profileError))
                {
                    Debug.LogError(
                        $"[MonsterActor] {definition.name} Attribute Profile 적용 실패: " +
                        profileError,
                        definition.attributeProfile);
                }
            }
            else
            {
                AbilitySystem.InitializeDefaultAttributes();
                Debug.LogError(
                    $"[MonsterActor] {definition.name}에 Attribute Profile이 없습니다.",
                    definition);
            }

            ResetHealthFromStats();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            _poiseStat?.Init(definition);

            if (definition.EffectiveBreakGaugeData != null && _breakGauge == null)
            {
                _breakGauge = gameObject.AddComponent<MonsterBreakGauge>();
                BindBreakGauge();
            }

            _breakGauge?.Init(definition);

            _dropTable = definition.EffectiveDropTable;

            Abilities?.SetAbilitySet(definition.EffectiveAbilitySet);
            _combat?.Init(definition);

            _groundAIController?.Init(definition);
        }

        protected override void OnDestroy()
        {
            UnsubscribeReactionAbilityTriggers();
            _lockOnSimulationLease?.Dispose();
            _lockOnSimulationLease = null;
            ReleaseHpBar();
            base.OnDestroy();
            UnregisterExposed();
            HideBreakInteraction();
            if (_breakGauge == null) return;
            _breakGauge.OnBreakExposed -= OnBreakExposed;
            _breakGauge.OnBreakRecovered -= OnBreakRecovered;
        }

        private void ReleaseHpBar()
        {
            if (_uiHpBar == null)
                return;

            OnHealthChanged -= _uiHpBar.UpdateHealth;
            _poiseStat?.ConnectUiBar(null);
            _breakGauge?.ConnectUiBar(null);
            _uiHpBar.Release();
            _uiHpBar = null;
        }

        // 현재 노출(브레이크 가능) 중인 몬스터 레지스트리.
        // 프롬프트는 "노출됨"이 아니라 "플레이어가 실제로 브레이크 가능한 단일 타겟"에게만 표시되므로,
        // PlayerCombat 드라이버가 매 프레임 이 목록을 게이트로 삼아 현재 타겟을 선정한다.
        private static readonly List<MonsterActor> _exposedMonsters = new List<MonsterActor>();
        public static IReadOnlyList<MonsterActor> ExposedMonsters => _exposedMonsters;

        // 도메인 리로드 비활성(Enter Play Mode Options) 환경에서 이전 세션의 destroyed 참조가 잔존하지 않도록 초기화.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _exposedMonsters.Clear();

        private void OnBreakExposed(MonsterBreakGauge breakGauge)
        {
            if (_isDead) return;
            // 무방비 경직 없음 — 적은 계속 정상 행동하고, '브레이크 공격 가능'만 레지스트리에 등록한다.
            // 실제 프롬프트 표시 여부는 PlayerCombat 드라이버가 거리·각도·락온으로 판정한다.
            RegisterExposed();
        }

        private void OnBreakRecovered(MonsterBreakGauge breakGauge)
        {
            UnregisterExposed();
            HideBreakInteraction();
        }

        private void RegisterExposed()
        {
            if (!_exposedMonsters.Contains(this))
                _exposedMonsters.Add(this);
        }

        private void UnregisterExposed()
        {
            _exposedMonsters.Remove(this);
        }

        /// <summary>
        /// PlayerCombat 드라이버가 호출 — 이 몬스터가 현재 브레이크 타겟이면 true.
        /// </summary>
        public void SetBreakInteractionActive(bool active)
        {
            if (active) ShowBreakInteraction();
            else HideBreakInteraction();
        }

        private void ShowBreakInteraction()
        {
            if (_breakInteraction != null || _isDead || ActorSvc.UI == null) return;
            _breakInteraction = ActorSvc.UI.CreateBreakInteraction(this);
        }

        private void HideBreakInteraction()
        {
            if (_breakInteraction == null) return;
            _breakInteraction.Release();
            _breakInteraction = null;
        }

        private void BindBreakGauge()
        {
            if (_breakGauge == null) return;
            _breakGauge.OnBreakExposed -= OnBreakExposed;
            _breakGauge.OnBreakExposed += OnBreakExposed;
            _breakGauge.OnBreakRecovered -= OnBreakRecovered;
            _breakGauge.OnBreakRecovered += OnBreakRecovered;

            if (_uiHpBar != null)
                _breakGauge.ConnectUiBar(_uiHpBar);
        }

        /// <summary>
        /// 넉백/에어본 임펄스에 사용할 수평 방향. AttackDirection은 히트박스 스윕 델타에서 유도되어
        /// 수직 성분을 포함할 수 있으므로 그대로 쓰면 ForceUnground로 인해 피격자가 솟구친다.
        /// </summary>
        private Vector3 ResolveKnockbackDirection(in HitContext hit)
        {
            return KnockbackDirectionResolver.ResolveHorizontal(
                hit.AttackDirection,
                hit.Attacker != null ? hit.Attacker.transform : null,
                transform,
                MovementController != null && MovementController.Motor != null
                    ? MovementController.Motor.CharacterUp
                    : Vector3.up);
        }

        // 가벼운 밀쳐냄(shove) 튜닝값. 이동 거리 ≈ Force/Drag (≈1.2m). 브레이크 마무리(2.5m+2초 넘어짐)보다 약하다.
        private const float BreakShoveForce = 12f;
        private const float BreakShoveDrag  = 10f;

        /// <summary>
        /// 공격자 반대 방향으로 가벼운 밀쳐냄 임펄스를 가한다. 적용한 방향을 반환한다.
        /// 임펄스는 상태와 독립적으로 KCC에 합산되므로(ActorMovementController.UpdateVelocity) 어떤 반응 상태와도 공존한다.
        /// </summary>
        private Vector3 ApplyShoveImpulse(GameActor attacker)
        {
            Vector3 dir = attacker != null
                ? transform.position - attacker.transform.position
                : -transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f) dir = -transform.forward;
            dir = dir.normalized;

            MovementController?.AddPlanarKnockback(
                dir * BreakShoveForce,
                BreakShoveDrag);
            return dir;
        }

        /// <summary>
        /// [카운터 적중 전용] 공격자 반대 방향으로 밀려나며 Knockback 스태거를 거쳐 복귀한다.
        /// 패리 직후 몬스터가 Stun에 묶여 있어도 스태거로 전환해 '밀려남'이 시각적으로 드러나게 한다.
        /// EnemyHitState는 속도를 즉시 0으로 만들지 않고 감속만 하므로 임펄스가 유지된다
        /// (매 프레임 속도를 덮어쓰는 EnemyKnockdownState와 달리 보존됨).
        /// </summary>
        public void ApplyBreakStyleShove(GameActor attacker)
        {
            if (MovementController == null) return;

            Vector3 dir = ApplyShoveImpulse(attacker);

            // 카운터 셸브는 합성 입력이라 HitRequest 경계를 거쳐 HitContext로 변환한다(드문 경로라 할당 허용).
            // EnemyHitState는 ReactionType(KnockBack→Knockback 애니)만 읽으므로 그 값 보존이 핵심이다.
            var shoveData = new AttackData
            {
                attacker        = attacker,
                reactionType    = AttackReactionType.KnockBack,
                damage          = 0f,
                poiseDamage     = 0f,
                knockbackForce  = BreakShoveForce,
                knockbackDrag   = BreakShoveDrag,
                attackDirection = dir,
            };
            HitContext shoveHit = HitContext.Create(HitRequest.FromAttackData(shoveData), this);
            MovementController.TransitionToState(new EnemyHitState(MovementController, shoveHit));
        }

        /// <summary>
        /// 플레이어 패리에 의해 공격이 무효화됐을 때 호출.
        /// AI 컨트롤러에 패리 알림 후 스턴 상태로 강제 전환한다.
        /// </summary>
        public void OnParried()
        {
            RuntimeLog.Trace(
                RuntimeLogCategory.Combat,
                $"[MonsterActor] {gameObject.name} 패리당함!",
                this);

            AIController?.OnParried();

            // 패리 스턴은 reactionDuration 미지정(=기본 2.5초) 외 EnemyStunState가 읽는 필드가 없으므로
            // 기본 HitContext 생성자와 동작이 동일하다(합성 AttackData 불필요).
            MovementController.TransitionToState(new EnemyStunState(MovementController));
        }

        private void ResetHealthFromStats()
        {
            _currentHealth = _maxHealth;
            _isDead        = false;
        }
    }
}
