using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.State;
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
        
        [Header("AI Components")]
        [SerializeField] private EnemyDetection _detection;
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private EnemyCombat _combat;

        protected float _maxHealth = 0.0f;
        protected float _currentHealth = 0.0f;
        
        protected UI_ActorHpBar _uiHpBar;
        
        public event Action<float, float> OnHealthChanged; // (current, max)
        public EnemyDetection Detection => _detection;
        public EnemyBrain Brain => _brain;
        public EnemyCombat Combat => _combat;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        
        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Monster;

            _maxHealth = _stats.maxHealth;
            _currentHealth = _maxHealth;
            
            // AI 컴포넌트 자동 할당
            if (_detection == null)
                _detection = GetComponent<EnemyDetection>();
            
            if (_brain == null)
                _brain = GetComponent<EnemyBrain>();
            
            if (_combat == null)
                _combat = GetComponent<EnemyCombat>();
            
            if(_poiseStat == null)
                _poiseStat = GetComponent<PoiseStat>();
        }

        protected override void Start()
        {
            base.Start();
            
            // AttachHpUI();
        }

        private void AttachHpUI()
        {
            if (_uiHpBar != null)
            {
                return;
            }
            
            _uiHpBar = UIManager.Instance.CreateHpBar(this);
            if (_uiHpBar != null)
            {
                OnHealthChanged += _uiHpBar.UpdateHealth;

                if (_poiseStat != null)
                {
                    _poiseStat.ConnectUiBar(_uiHpBar);
                }
            }
            _uiHpBar.UpdateHealth(_currentHealth, _maxHealth);
        }
        #region IDamageable Implementation
        
        public void TakeDamage(AttackData attackData)
        {
            if (_combat.IsGuarding)
            {
                // Guard State가 처리하도록 위임
                if (MovementController.CurrentState is EnemyGuardState guardState)
                {
                    guardState.OnAttackBlocked(attackData);
                    return; // 데미지 처리 중단
                }
            }

            if (!CanTakeDamage())
            {
                Debug.Log($"[MonsterActor] {gameObject.name}는 현재 데미지를 받을 수 없습니다.");
                return;
            }
            
            float finalDamage = attackData.damage;
            
            // 크리티컬 처리
            if (attackData.criticalMultiplier > 1.0f)
            {
                finalDamage *= attackData.criticalMultiplier;
                Debug.Log($"[MonsterActor] 크리티컬 히트! 데미지: {finalDamage}");
            }
            
            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);

            if (_uiHpBar == null)
            {
                AttachHpUI();
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            
            // 페이즈 업데이트
            _brain?.UpdatePhase(GetHealthPercent());
            
            Debug.Log($"[MonsterActor] {gameObject.name}가 {finalDamage} 데미지를 받았습니다! (남은 체력: {_currentHealth}/{_maxHealth})");
            
            _detection.AcquireTarget(attackData.attacker?.transform);
            
            // 피격 이펙트, 사운드, 넉백 등 추가 가능
            OnDamaged(attackData);
            
            // 사망 처리
            if (_currentHealth <= 0)
            {
                OnDeath(attackData);
            }
        }

        public void OnTakeFinishAttack(Vector3 attackDirection)
        {
            _currentHealth = 0;

            if (_uiHpBar == null)
            {
                AttachHpUI();
            }
            MovementController.AddVelocity(attackDirection.normalized * 30.0f);
            
            VitalOrbManager.Instance.TrySpawn(VitalOrbTrigger.FinishAttackHit, transform.position);

            OnDeath(null);
        }
        
        public bool IsAlive()
        {
            return _currentHealth > 0;
        }
        
        public bool CanTakeDamage()
        {
            // if (MovementController.CurrentState.StateName == "Hit")
            //     return false;
            return IsAlive() && !_isInvincible;
        }
        
        public Transform GetTransform()
        {
            return transform;
        }

        public void LockOn()
        {
            _lockOnDecal?.SetActive(true);
        }

        public void UnLockOn()
        {
            _lockOnDecal?.SetActive(false);
        }

        public float GetHealthPercent()
        {
            return _currentHealth / _maxHealth;
        }

        public float GetCurrentHealth()
        {
            return _currentHealth;
        }

        #endregion
        
        #region Health Management
        
        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive())
                return;
            
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            float actualHeal = _currentHealth - oldHealth;
            
            if (_uiHpBar == null)
            {
                AttachHpUI();
            }
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[MonsterActor] {gameObject.name} 체력 회복: +{actualHeal:F1} HP (현재: {_currentHealth:F1}/{_maxHealth})");
        }
        
        /// <summary>
        /// 체력 직접 설정
        /// </summary>
        public void SetHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);
            
            if (_currentHealth <= 0 && IsAlive())
            {
                OnDeath(null);
            }
        }
        
        #endregion
        /// <summary>
        /// 피격 시 호출 (이펙트, 사운드 등)
        /// </summary>
        protected virtual void OnDamaged(AttackData attackData)
        {
            // Poise 판정 — Poise가 소진됐을 때만 Hit State 진입
            bool poiseBroken = true;
            if (_poiseStat != null)
            { 
                _poiseStat.TakePoiseDamage(attackData?.poiseDamage ?? 0f);
                poiseBroken = _poiseStat.IsPoiseBroken;
            }
            
            if (attackData != null &&  poiseBroken == true)
            {
                switch (attackData.reactionType)
                {
                    case AttackReactionType.KnockBack:
                        MovementController.AddImpulse(attackData.attackDirection.normalized * attackData.knockbackForce);
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
                        MovementController.AddImpulse(launchDir * attackData.knockbackForce 
                                                      + Vector3.up * attackData.airborneForce);
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
            
            Debug.Log($"[MonsterActor] 피격! PoiseBroken={poiseBroken}, HitPoint: {attackData?.hitPoint}");
        }
        
        /// <summary>
        /// 사망 시 호출
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[MonsterActor] {gameObject.name} 사망!");

            // 그룹에서 제거 — 슬롯/레지스트리 정리
            _brain?.Group?.UnregisterMember(this);
            
            MovementController.TransitionToState(new EnemyDeathState(MovementController));

            if (_uiHpBar != null)
            {
                OnHealthChanged -= _uiHpBar.UpdateHealth;
                Destroy(_uiHpBar.gameObject);
            }
            
            //_dissolveController.StartDissolve(3f);
            // KCC 캡슐 콜라이더 충돌 비활성화
            MovementController.Motor.SetCapsuleCollisionsActivation(false);
            //MovementController.Motor.enabled = false;
        }

        public void PlayDissolveAndDestroy(float duration)
        {
            // 사망 애니메이션
            // 아이템 드롭
            // 사망 처리
            _dissolveController.StartDissolve(duration);
            
            //Destroy(gameObject, duration);
        }
        
        /// <summary>
        /// 무적 상태 설정 (디버깅/테스트용)
        /// </summary>
        public void SetInvincible(bool invincible)
        {
            _isInvincible = invincible;
        }

    }
}