using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround
{
    public partial class MonsterActor : GameActor, IDamageable
    {  
        [Header("Monster Stats")]
        [SerializeField] private EnemyStatsSO _stats;
        [SerializeField] private bool _isInvincible = false;
        [SerializeField] private GameObject _lockOnDecal = null;
  
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
            }
            _uiHpBar.UpdateHealth(_currentHealth, _maxHealth);
        }
        #region IDamageable Implementation
        
        public void TakeDamage(AttackData attackData)
        {
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
            if (attackData != null && attackData.reactionType == AttackReactionType.KnockBack)
            {
                MovementController.AddVelocity(attackData.attackDirection.normalized * 15.0f);
            }
            // 피격 이펙트 재생
            // 피격 사운드 재생
            // 넉백 처리
            // 피격 애니메이션 재생
            MovementController.TransitionToState(new EnemyHitState(MovementController));
            
            _colorChanger.OnHit();
            
            Debug.Log($"[MonsterActor] 피격! HitPoint: {attackData.hitPoint}");
        }
        
        /// <summary>
        /// 사망 시 호출
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[MonsterActor] {gameObject.name} 사망!");
            
            MovementController.TransitionToState(new EnemyDeathState(MovementController));

            // 사망 애니메이션
            // 아이템 드롭
            // 사망 처리
            _dissolveController.StartDissolve(3f);
            
            if (_uiHpBar != null)
            {
                OnHealthChanged -= _uiHpBar.UpdateHealth;
                Destroy(_uiHpBar.gameObject);
            }
            
            // 임시: 3초 후 제거
            Destroy(gameObject, 3f);
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