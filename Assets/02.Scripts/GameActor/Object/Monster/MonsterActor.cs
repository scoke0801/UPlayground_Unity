using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.AI.Debugging;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using UnityEngine.Serialization;
using Random = System.Random;

namespace UPlayGround
{
    public partial class MonsterActor : GameActor, IDamageable
    {  
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

        [Header("AI Components")]
        [SerializeField] private EnemyDetection _detection;
        [FormerlySerializedAs("_brain")]
        [FormerlySerializedAs("_aiController")]
        [SerializeField] private EnemyAIController _groundAIController;
        [SerializeField] private EnemyFlyingAIController _flyingAIController;
        [SerializeField] private EnemyCombat _combat;

        protected float _maxHealth = 0.0f;
        protected float _currentHealth = 0.0f;
        protected bool _isDead = false;
        
        protected UI_ActorHpBar _uiHpBar;
        private UI_BreakPrompt _breakPrompt;   // 노출(브레이크 가능) 동안만 존재하는 F키 프롬프트

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
        
        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Monster | ActorType.Combat;

            Stats.Init(null);
            ResetHealthFromStats();

            if (_detection == null) _detection = GetComponent<EnemyDetection>();
            if (_groundAIController == null) _groundAIController = GetComponent<EnemyAIController>();
            if (_flyingAIController == null) _flyingAIController = GetComponent<EnemyFlyingAIController>();
            if (_combat    == null) _combat    = GetComponent<EnemyCombat>();
            if (_poiseStat == null) _poiseStat = GetComponent<PoiseStat>();
            if (_breakGauge == null) _breakGauge = GetComponent<MonsterBreakGauge>();
            BindBreakGauge();
            ApplyDefinitionData(Definition);
        }

        protected override void Start()
        {
            base.Start();
        }

        private void AttachHpUI()
        {
            if (_uiHpBar != null) return;
            
            _uiHpBar = UIManager.Instance.CreateHpBar(this);
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
        
        public void TakeDamage(AttackData attackData)
        {
            if (_combat.IsGuarding)
            {
                if (MovementController.CurrentState is EnemyGuardState guardState)
                {
                    guardState.OnAttackBlocked(attackData);
                    return;
                }
            }

            if (!CanTakeDamage())
            {
                Debug.Log($"[MonsterActor] {gameObject.name}는 현재 데미지를 받을 수 없습니다.");
                return;
            }
            
            // 공격력·방어율 적용. 둘 다 기본값(1.0/0.0)일 때는 공격 데이터 그대로 통과한다.
            float attackerPower = attackData.attacker != null ? attackData.attacker.Stats.AttackPower : 1f;
            float defenseRate   = Mathf.Clamp01(Stats.Defense);

            float breakExposedMultiplier = _breakGauge != null ? _breakGauge.DamageTakenMultiplier : 1f;
            float finalDamage = attackData.damage * attackerPower * (1f - defenseRate) * breakExposedMultiplier;

            if (attackData.criticalMultiplier > 1.0f)
            {
                finalDamage *= attackData.criticalMultiplier;
                Debug.Log($"[MonsterActor] 크리티컬 히트! 데미지: {finalDamage}");
            }

            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);

            if (_uiHpBar == null) AttachHpUI();

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            AIController?.UpdatePhase(GetHealthPercent());
            
            _detection?.AcquireTarget(attackData.attacker?.transform);
            
            OnDamaged(attackData);
            
            if (_currentHealth <= 0)
                OnDeath(attackData);
        }

        public void OnTakeFinishAttack(Vector3 attackDirection)
        {
            _currentHealth = 0;

            if (_uiHpBar == null) AttachHpUI();

            MovementController.AddVelocity(attackDirection.normalized * 30.0f);
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(VitalOrbTrigger.FinishAttackHit, transform.position);
            OnDeath(null);
        }

        /// <summary>
        /// 브레이크 특수공격 전용 데미지 진입점.
        /// 호출자는 타겟이 `BreakGauge.IsExposed` 상태임을 보장해야 한다.
        /// 일반 데미지 가드(`_isInvincible`, Guard, `OnDamaged` 흐름)를 의도적으로 우회한다.
        /// </summary>
        public void OnTakeSpecialBreakAttack(GameActor attacker, float damageByMaxHpRate, float fixedDamage)
        {
            if (!IsAlive()) return;

            float rateDamage = _maxHealth * Mathf.Max(0f, damageByMaxHpRate);
            float finalDamage = Mathf.Max(0f, fixedDamage) + rateDamage;
            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);

            if (_uiHpBar == null) AttachHpUI();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            AIController?.UpdatePhase(GetHealthPercent());

            _breakGauge?.ConsumeBySpecialAttack();
            _colorChanger.OnHit();

            Vector3 floaterPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;
            UIManager.Instance.ShowDamageFloater(floaterPos, finalDamage, FloatStyle.Critical);

            if (_currentHealth <= 0)
            {
                var attackData = new AttackData
                {
                    attacker = attacker,
                    attackKind = AttackKind.FinishAttack,
                    reactionType = AttackReactionType.Knockdown,
                };
                OnDeath(attackData);
                return;
            }

            // 생존 시 — 브레이크 공격 마무리로 넘어뜨린다. Knockdown 모션이 없으면 Stun.
            if (MovementController != null)
            {
                bool hasKnockdown = Animator != null && Animator.HasMotion(AnimKey.Knockdown, true);
                MovementController.TransitionToState(hasKnockdown
                    ? new EnemyKnockdownState(MovementController)
                    : new EnemyStunState(MovementController));
            }
        }
        
        public bool IsAlive()          => _currentHealth > 0;
        public bool CanTakeDamage()
            => IsAlive() && !_isInvincible && !(MovementController?.CurrentState?.GrantsInvincibility ?? false);
        public Transform GetTransform() => transform;

        public void LockOn()   { if (_lockOnDecal != null) _lockOnDecal.SetActive(true); }
        public void UnLockOn() { if (_lockOnDecal != null) _lockOnDecal.SetActive(false); }

        public float GetHealthPercent() => _currentHealth / _maxHealth;
        public float GetCurrentHealth() => _currentHealth;

        #endregion
        
        #region Health Management
        
        public void Heal(float amount)
        {
            if (!IsAlive()) return;
            
            float oldHealth   = _currentHealth;
            _currentHealth    = Mathf.Min(_currentHealth + amount, _maxHealth);
            float actualHeal  = _currentHealth - oldHealth;

            if (actualHeal <= 0f) return;

            if (_uiHpBar == null) AttachHpUI();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // 힐 위치: Center 소켓 우선, 없으면 루트 위치
            Vector3 floaterPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;
            UIManager.Instance.ShowDamageFloaterHeal(floaterPos, actualHeal, FloatStyle.MonsterHeal);

            Debug.Log($"[MonsterActor] {gameObject.name} 체력 회복: +{actualHeal:F1} HP (현재: {_currentHealth:F1}/{_maxHealth})");
        }
        
        public void SetHealth(float health)
        {
            bool wasAlive = IsAlive();
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);

            if (_uiHpBar == null) AttachHpUI();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            AIController?.UpdatePhase(GetHealthPercent());

            if (wasAlive && _currentHealth <= 0f)
                OnDeath(null);
        }
        
        #endregion

        protected virtual void OnDamaged(AttackData attackData)
        {
            bool poiseBroken = true;
            if (_poiseStat != null)
            {
                _poiseStat.TakePoiseDamage(attackData?.poiseDamage ?? 0f);
                poiseBroken = _poiseStat.IsPoiseBroken;
            }

            GetComponent<EnemyTacticalMemory>()?.NotifyTookDamage(attackData, poiseBroken);
            AIController?.Group?.Memory?.NotifyMemberTookDamage();
            _breakGauge?.TakeBreakDamage(attackData);

            // 노출(브레이크 가능) 중에도 무방비 경직 없이 정상 리액션한다.
            // 받는 피해 증가(DamageTakenMultiplier)는 TakeDamage 단계에서 이미 적용된다.

            bool shouldReact = poiseBroken || (attackData?.forceReaction ?? false);
            if (attackData != null && shouldReact)
            {
                switch (attackData.reactionType)
                {
                    case AttackReactionType.KnockBack:
                        MovementController.AddImpulse(attackData.attackDirection.normalized * attackData.knockbackForce,
                            attackData.knockbackDrag);
                        break;

                    case AttackReactionType.Pull:
                        if (attackData.attacker != null)
                        {
                            Vector3 pullDir = (attackData.attacker.transform.position - transform.position).normalized;
                            pullDir.y = 0f;
                            MovementController.AddVelocity(pullDir * attackData.pullForce);
                        }

                        break;

                    case AttackReactionType.Airborne:
                    {
                        Vector3 launchDir = attackData.attackDirection.normalized;
                        launchDir.y = 0f;
                        Vector3 airborneVelocity = ShouldEnterAirborneState(attackData)
                            ? Vector3.up * attackData.airborneForce
                            : Vector3.zero;
                        MovementController.AddImpulse(
                            launchDir * attackData.knockbackForce + airborneVelocity,
                            attackData.knockbackDrag);
                        break;
                    }

                    case AttackReactionType.Grab:
                        break;
                }
            }

            if (shouldReact)
            {
                if (ShouldEnterAirborneState(attackData))
                    MovementController.TransitionToState(new EnemyAirborneState(MovementController));
                else if (attackData?.reactionType == AttackReactionType.Grab)
                    MovementController.TransitionToState(new EnemyGrabbedState(MovementController, attackData));
                else if (attackData?.reactionType == AttackReactionType.Stun)
                    MovementController.TransitionToState(new EnemyStunState(MovementController, attackData));
                else if (CanEnterKnockdownState(attackData))
                    MovementController.TransitionToState(new EnemyKnockdownState(MovementController, attackData));
                else
                    MovementController.TransitionToState(new EnemyHitState(MovementController, attackData));
            }

            _colorChanger.OnHit();
        }

        private bool ShouldEnterAirborneState(AttackData attackData)
        {
            if (attackData == null || attackData.reactionType != AttackReactionType.Airborne)
                return false;

            if (attackData.airborneForce >= MinAirborneStateForce)
                return true;

            return false;
        }

        private bool CanEnterKnockdownState(AttackData attackData)
        {
            if (attackData == null || attackData.reactionType != AttackReactionType.Knockdown)
                return false;

            return Animator != null && Animator.HasMotion(AnimKey.Knockdown, true);
        }

        protected virtual void OnDeath(AttackData attackData)
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[MonsterActor] {gameObject.name} 사망!");

            GetComponent<EncounterReplayRecorder>()?.EndAndSave("death", "몬스터 사망");
            AIController?.Group?.UnregisterMember(this);
            MovementController.TransitionToState(new EnemyDeathState(MovementController));

            NotifyQuestMonsterKill();
            SpawnDropItems();
            TryRecruitToParty();

            if (_uiHpBar != null)
            {
                OnHealthChanged -= _uiHpBar.UpdateHealth;
                Destroy(_uiHpBar.gameObject);
            }

            UnregisterExposed();
            HideBreakPrompt();

            MovementController.Motor.SetCapsuleCollisionsActivation(false);
        }

        private void NotifyQuestMonsterKill()
        {
            if (QuestManager.Instance == null)
            {
                return;
            }

            if (int.TryParse(ActorId, out int monsterId))
            {
                QuestManager.Instance.NotifyMonsterKill(monsterId);
                return;
            }

            Debug.LogWarning($"[MonsterActor] 퀘스트 처치 알림 실패: ActorId '{ActorId}'를 int ID로 변환할 수 없습니다.", this);
        }

        private void TryRecruitToParty()
        {
            if (_recruitableAs == CharacterActorType.None) return;
            PartyManager.Instance?.UnlockCharacter(_recruitableAs);
        }

        private void SpawnDropItems()
        {
            if (_dropTable == null) return;

            var items = ItemManager.Instance.GetDropItemList(_dropTable.dropItems);
            foreach (var item in items)
            {
                GameObjectManager.Instance.SpawnItem(item, transform.position);
            }
        }

        public void PlayDissolveAndDestroy(float duration)
        {
            MovementController.Motor.enabled = false;
            _dissolveController.StartDissolve(duration);
        }
        
        public void SetInvincible(bool invincible) => _isInvincible = invincible;

        /// <summary>
        /// ActorDefinitionSO 주입 시 stats/poiseData를 재적용한다.
        /// Awake 이후 ActorSpawnManager가 호출하므로 HP도 함께 갱신.
        /// </summary>
        public override void SetDefinition(ActorDefinitionSO definition)
        {
            base.SetDefinition(definition);
            ApplyDefinitionData(definition);
        }

        private void ApplyDefinitionData(ActorDefinitionSO definition)
        {
            if (definition == null) return;

            // 메타(등급/레벨)는 정의가 권위 소스. 정의 값으로 덮어쓴다.
            _grade = definition.grade;
            _level = Mathf.Max(1, definition.level);
            _recruitableAs = definition.recruitableAs;

            // statData는 자동 생성기로 보장한다. 누락 시 기본 스탯으로 초기화하고 오류를 남긴다.
            if (definition.statData != null)
                Stats.Init(definition.statData);
            else
            {
                Stats.Init(null);
                Debug.LogError($"[MonsterActor] {definition.name}에 statData가 없습니다. UPlayGround/Stat/Stat Data Generator의 전체 보정을 실행하세요.", definition);
            }

            ResetHealthFromStats();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            _poiseStat?.Init(definition);

            if (definition.breakGaugeData != null && _breakGauge == null)
            {
                _breakGauge = gameObject.AddComponent<MonsterBreakGauge>();
                BindBreakGauge();
            }

            _breakGauge?.Init(definition);

            _dropTable = definition.dropTable;

            _combat?.Init(definition);

            _groundAIController?.Init(definition);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterExposed();
            HideBreakPrompt();
            if (_breakGauge == null) return;
            _breakGauge.OnBreakExposed -= OnBreakExposed;
            _breakGauge.OnBreakRecovered -= OnBreakRecovered;
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
            HideBreakPrompt();
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
        public void SetBreakPromptActive(bool active)
        {
            if (active) ShowBreakPrompt();
            else HideBreakPrompt();
        }

        private void ShowBreakPrompt()
        {
            if (_breakPrompt != null || _isDead || UIManager.Instance == null) return;
            _breakPrompt = UIManager.Instance.CreateBreakPrompt(this);
        }

        private void HideBreakPrompt()
        {
            if (_breakPrompt == null) return;
            Destroy(_breakPrompt.gameObject);
            _breakPrompt = null;
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
        /// 플레이어 패리에 의해 공격이 무효화됐을 때 호출.
        /// AI 컨트롤러에 패리 알림 후 스턴 상태로 강제 전환한다.
        /// </summary>
        public void OnParried()
        {
            Debug.Log($"[MonsterActor] {gameObject.name} 패리당함!");

            AIController?.OnParried();

            var stunData = new AttackData
            {
                reactionType   = AttackReactionType.Stun,
                damage         = 0f,
                poiseDamage    = 0f,
                knockbackForce = 0f,
            };
            MovementController.TransitionToState(new EnemyStunState(MovementController, stunData));
        }

        private void ResetHealthFromStats()
        {
            _maxHealth     = Stats.MaxHealth;
            _currentHealth = _maxHealth;
            _isDead        = false;
        }
    }
}
