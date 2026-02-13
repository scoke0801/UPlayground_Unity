using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Config;
using UPlayGround.Data.Enemy;
using UPlayGround.State;

namespace UPlayGround
{
    public partial class MonsterActor : GameActor, IDamageable
    {  
        [Header("Monster Stats")]
        [SerializeField] private EnemyStatsSO _stats;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private bool _isInvincible = false;
        [SerializeField] private GameObject _lockOnDecal = null;
  
        [Header("AI Components")]
        [SerializeField] private EnemyDetection _detection;
        [SerializeField] private EnemyBrain _brain;
        [SerializeField] private EnemyCombat _combat;
        
        public EnemyDetection Detection => _detection;
        public EnemyBrain Brain => _brain;
        public EnemyCombat Combat => _combat;
        
        protected override void Awake()
        {
            base.Awake();
            _currentHealth = _maxHealth;
            
            // AI 컴포넌트 자동 할당
            if (_detection == null)
                _detection = GetComponent<EnemyDetection>();
            
            if (_brain == null)
                _brain = GetComponent<EnemyBrain>();
            
            if (_combat == null)
                _combat = GetComponent<EnemyCombat>();
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
            
            _currentHealth -= finalDamage;
            
            Debug.Log($"[MonsterActor] {gameObject.name}가 {finalDamage} 데미지를 받았습니다! (남은 체력: {_currentHealth}/{_maxHealth})");
            
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
            if (MovementController.CurrentState.StateName == "Hit")
                return false;
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

        #endregion
        
        /// <summary>
        /// 피격 시 호출 (이펙트, 사운드 등)
        /// </summary>
        protected virtual void OnDamaged(AttackData attackData)
        {
            // 피격 이펙트 재생
            // 피격 사운드 재생
            // 넉백 처리
            // 피격 애니메이션 재생
            MovementController.TransitionToState(new EnemyHitState(MovementController));
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