using System;
using UnityEngine;
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
        [SerializeField] private EnemyStatsSO _stats;
        [SerializeField] private bool _isInvincible = false;
        [SerializeField] private GameObject _lockOnDecal = null;
        [SerializeField] private PoiseStat _poiseStat = null;
        
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
        
        public event Action<float, float> OnHealthChanged; // (current, max)
        public EnemyDetection Detection => _detection;
        public IEnemyAIController AIController => _groundAIController != null ? _groundAIController : _flyingAIController;
        public EnemyAIController GroundAIController => _groundAIController;
        public EnemyFlyingAIController FlyingAIController => _flyingAIController;
        public EnemyCombat Combat => _combat;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public MonsterActorGrade Grade => _stats != null ? _stats.grade : MonsterActorGrade.Normal;
        public EnemyStatsSO Stat => _stats;
        
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

            float finalDamage = attackData.damage * attackerPower * (1f - defenseRate);

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

            if (attackData != null && poiseBroken)
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
                        MovementController.AddImpulse(
                            launchDir * attackData.knockbackForce + Vector3.up * attackData.airborneForce,
                            attackData.knockbackDrag);
                        MovementController.Motor.ForceUnground();
                        break;
                    }

                    case AttackReactionType.Grab:
                        break;
                }
            }

            if (poiseBroken)
            {
                if (attackData?.reactionType == AttackReactionType.Airborne)
                    MovementController.TransitionToState(new EnemyAirborneState(MovementController));
                else if (attackData?.reactionType == AttackReactionType.Grab)
                    MovementController.TransitionToState(new EnemyGrabbedState(MovementController, attackData));
                else
                    MovementController.TransitionToState(new EnemyHitState(MovementController, attackData));
            }

            _colorChanger.OnHit();
        }

        protected virtual void OnDeath(AttackData attackData)
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[MonsterActor] {gameObject.name} 사망!");

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

            if (definition.stats != null)
                _stats         = definition.stats;

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

            if (definition.dropTable != null)
                _dropTable = definition.dropTable;
        }

        /// <summary>
        /// 플레이어 패리에 의해 공격이 무효화됐을 때 호출.
        /// AI 컨트롤러에 패리 알림 후 경직 상태로 강제 전환한다.
        /// </summary>
        public void OnParried()
        {
            Debug.Log($"[MonsterActor] {gameObject.name} 패리당함!");

            AIController?.OnParried();

            // 패리 경직: Light 반응 타입으로 EnemyHitState 전환
            var staggerData = new AttackData
            {
                reactionType   = AttackReactionType.Light,
                damage         = 0f,
                poiseDamage    = 0f,
                knockbackForce = 0f,
            };
            MovementController.TransitionToState(new EnemyHitState(MovementController, staggerData));
        }

        private void ResetHealthFromStats()
        {
            _maxHealth     = Stats.MaxHealth;
            _currentHealth = _maxHealth;
            _isDead        = false;
        }
    }
}
