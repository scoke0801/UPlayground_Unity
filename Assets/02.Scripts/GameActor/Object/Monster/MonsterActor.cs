using System;
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
        [Header("Monster Stats")]
        [Tooltip("등급. ActorDefinitionSO 주입 시 덮어쓰며, 정의 없이 씬 배치된 경우 이 값을 폴백으로 사용.")]
        [SerializeField] private MonsterActorGrade _grade = MonsterActorGrade.Normal;
        [Min(1)]
        [Tooltip("기준 레벨. ActorDefinitionSO 주입 시 덮어쓰며, 정의 없이 씬 배치된 경우 이 값을 폴백으로 사용.")]
        [SerializeField] private int _level = 1;
        [SerializeField] private bool _isInvincible = false;
        [SerializeField] private GameObject _lockOnDecal = null;
        [SerializeField] private PoiseStat _poiseStat = null;
        [SerializeField] private MonsterBreakGauge _breakGauge = null;
        
        [Header("Drop")]
        [SerializeField] private EnemyDropTableSO _dropTable;

        [Header("Recruit")]
        [Tooltip("처치 시 파티에 합류시킬 캐릭터 타입. None이면 합류 없음.")]
        [SerializeField] private CharacterActorType _recruitableAs = CharacterActorType.None;

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

            // BreakExposed는 자체 상태/모션이 고정되므로 일반 리액션(물리·상태 전환)을 건너뛴다.
            // 단, 피격 플래시는 일반 피격과 동일하게 유지한다.
            if (_breakGauge != null && _breakGauge.IsExposed)
            {
                _colorChanger.OnHit();
                return;
            }

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
                else if (attackData?.reactionType == AttackReactionType.Knockdown)
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

        protected virtual void OnDeath(AttackData attackData)
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[MonsterActor] {gameObject.name} 사망!");

            GetComponent<EncounterReplayRecorder>()?.EndAndSave("death", "몬스터 사망");
            AIController?.Group?.UnregisterMember(this);
            MovementController.TransitionToState(new EnemyDeathState(MovementController));

            SpawnDropItems();
            TryRecruitToParty();

            if (_uiHpBar != null)
            {
                OnHealthChanged -= _uiHpBar.UpdateHealth;
                Destroy(_uiHpBar.gameObject);
            }
            
            MovementController.Motor.SetCapsuleCollisionsActivation(false);
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

            if (definition == null) return;

            // 메타(등급/레벨)는 정의가 권위 소스. 정의 값으로 덮어쓴다.
            _grade = definition.grade;
            _level = Mathf.Max(1, definition.level);

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

            if (definition.poiseData != null && _poiseStat != null)
                _poiseStat.Init(definition.poiseData);

            if (definition.breakGaugeData != null && _breakGauge == null)
            {
                _breakGauge = gameObject.AddComponent<MonsterBreakGauge>();
                BindBreakGauge();
            }

            if (definition.breakGaugeData != null && _breakGauge != null)
                _breakGauge.Init(definition.breakGaugeData);

            if (definition.dropTable != null)
                _dropTable = definition.dropTable;

            if (definition.attackData != null && _combat != null)
                _combat.Init(definition.attackData);

            if (definition.behaviorData != null && _groundAIController != null)
                _groundAIController.Init(definition.behaviorData);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_breakGauge == null) return;
            _breakGauge.OnBreakExposed -= OnBreakExposed;
            _breakGauge.OnBreakRecovered -= OnBreakRecovered;
        }

        private void OnBreakExposed(MonsterBreakGauge breakGauge)
        {
            if (_isDead || MovementController == null) return;
            string stateName = MovementController.CurrentState?.StateName;
            if (stateName is "Death" or "Grabbed" or "BreakExposed") return;
            MovementController.TransitionToState(new EnemyBreakExposedState(MovementController, breakGauge));
        }

        private void OnBreakRecovered(MonsterBreakGauge breakGauge)
        {
            if (_isDead || MovementController == null) return;
            if (MovementController.CurrentState?.StateName == "BreakExposed")
                MovementController.TransitionToState(new EnemyIdleState(MovementController));
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
