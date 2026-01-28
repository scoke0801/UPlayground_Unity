using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;
using UPlayGround.GameActor.Animation;

namespace UPlayGround.GameActor.Component
{
    [System.Serializable]
    public class CombatStats
    {
        public float heavyAttackDamage = 30f;
        public float comboWindow = 0.8f; // 콤보 입력 가능 시간
    }
    
    [System.Serializable]
    public class ComboData
    {
        [Tooltip("공격 애니메이션 키")]
        public AnimKey animKey;
        
        [Tooltip("공격 데미지")]
        public float damage;
        
        [Tooltip("공격 중 끊을 수 있는지 여부")]
        public bool canBeInterrupted;
    }
    
    public class AttackData
    {
        public AnimKey animKey;
        public float damage;
        public float duration;
        public bool canBeInterrupted;
        
        public Vector3 hitPoint;        // 공격 적중 위치
        public GameObject hitTarget;     // 피격 대상
        public float criticalMultiplier; // 크리티컬 배율
        public bool isCounterAttack;     // 카운터 공격 여부
    }
    
    /// <summary>
    /// 플레이어의 전투 관련 데이터와 로직
    /// State는 "언제" 공격할지 결정하고
    /// Component는 "어떤" 공격을 실행하는지 처리
    /// </summary>
    public class PlayerCombat : PlayerActorComponent
    {
        private enum AttackState
        {
            NormalAttack = 0,
            HeavyAttack = 1,
        }
        [FormerlySerializedAs("equipment")]
        [Header("References")]
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private ActorAnimator _actorAnimator;
        
        [FormerlySerializedAs("stats")]
        [Header("Combat Data")]
        [SerializeField] private CombatStats _stats;
        [SerializeField] private ComboData[] _comboChain;
        [SerializeField] private ComboData[] _heavyComoboChain;
        
        // 현재 전투 상태
        public int CurrentComboIndex { get; private set; }
        public float LastAttackTime { get; private set; }
        public bool CanCombo { get; private set; }

        private AttackState _attackState = AttackState.NormalAttack;
        
        // 이벤트
        public event System.Action<AttackData> OnAttackStarted;
        public event System.Action<AttackData> OnAttackHit;
        public event System.Action OnComboReset;
        
        private void Awake()
        {
            if (_equipment == null)
                _equipment = GetComponent<PlayerEquipment>();
            
            if(_actorAnimator == null)
                _actorAnimator = GetComponent<ActorAnimator>();
        }
        
        /// <summary>
        /// 일반 공격 실행
        /// State에서 호출: playerCombat.ExecuteAttack()
        /// </summary>
        public AttackData ExecuteAttack()
        {
            if (!_equipment.IsRightWeaponEquipped)
            {
                return null;
            }

            if (_attackState == AttackState.HeavyAttack)
            {
                ResetCombo();
            }
    
            _attackState = AttackState.NormalAttack;
            // 콤보 체인 체크
            if (CanContinueCombo())
            {
                CurrentComboIndex++;
            }
            else
            {
                CurrentComboIndex = 0;
            }
            
            // ComboData를 AttackData로 변환
            var comboData = _comboChain[CurrentComboIndex];
            var attackData = ConvertToAttackData(comboData);
            
            LastAttackTime = Time.time;
            CanCombo = true;
            
            OnAttackStarted?.Invoke(attackData);
            
            return attackData;
        }
        
        /// <summary>
        /// 강공격 실행
        /// </summary>
        public AttackData ExecuteHeavyAttack()
        {
            if (!_equipment.IsLeftWeaponEquipped)
            {
                return null;
            }
            
            if (_attackState == AttackState.NormalAttack)
            {
                ResetCombo();
            }
            _attackState = AttackState.HeavyAttack;
            
            // 콤보 체인 체크
            if (CanContinueCombo())
            {
                CurrentComboIndex++;
            }
            else
            {
                CurrentComboIndex = 0;
            }
            
            // ComboData를 AttackData로 변환
            var comboData = _heavyComoboChain[CurrentComboIndex];
            var attackData = ConvertToAttackData(comboData);
            
            LastAttackTime = Time.time;
            CanCombo = true;
            
            OnAttackStarted?.Invoke(attackData);
            
            return attackData;
        }
        
        /// <summary>
        /// ComboData를 AttackData로 변환
        /// </summary>
        private AttackData ConvertToAttackData(ComboData comboData)
        {
            float duration = GetAnimationDuration(comboData.animKey);
            
            return new AttackData
            {
                animKey = comboData.animKey,
                damage = comboData.damage,
                duration = duration,
                canBeInterrupted = comboData.canBeInterrupted
            };
        }
        
        /// <summary>
        /// AnimKey에 해당하는 AnimationClip의 duration 가져오기
        /// </summary>
        private float GetAnimationDuration(AnimKey animKey)
        {
            if (_actorAnimator == null)
            {
                Debug.LogWarning($"[PlayerCombat] ActorAnimator가 없습니다. 기본값 1.0 사용");
                return 1.0f;
            }
            
            float duration = _actorAnimator.GetAnimationDuration(animKey);
            
            if (duration <= 0)
            {
                Debug.LogWarning($"[PlayerCombat] {animKey}의 duration을 가져올 수 없습니다. 기본값 1.0 사용");
                return 1.0f;
            }
            
            return duration;
        }
        
        /// <summary>
        /// 콤보 계속 가능한지 체크
        /// </summary>
        private bool CanContinueCombo()
        {
            int length = (_attackState == AttackState.NormalAttack) ? _comboChain.Length : _heavyComoboChain.Length;
            
            float timeSinceLastAttack = Time.time - LastAttackTime;
            return timeSinceLastAttack < _stats.comboWindow 
                   && CurrentComboIndex < length - 1;
        }
        /// <summary>
        /// 콤보 윈도우 열기 (애니메이션 이벤트에서 호출)
        /// </summary>
        public void OpenComboWindow()
        {
            CanCombo = true;
        }
        
        /// <summary>
        /// 콤보 리셋
        /// </summary>
        public void ResetCombo()
        {
            LastAttackTime = Time.time;
            CurrentComboIndex = 0;
            CanCombo = false;
            OnComboReset?.Invoke();
        }
        
        /// <summary>
        /// State에서 호출할 조건 체크 메서드들
        /// </summary>
        public bool IsAttacking()
        {
            float timeSinceLastAttack = Time.time - LastAttackTime;
            var currentCombo = _comboChain[CurrentComboIndex];
            float duration = GetAnimationDuration(currentCombo.animKey);
            return timeSinceLastAttack < duration;
        }
        
        public bool CanAttack()
        {
            return !_equipment.IsRightWeaponEquipped;
        }
        /// <summary>
        /// 현재 콤보의 애니메이션 키 가져오기
        /// </summary>
        public AnimKey GetCurrentAttackAnimKey()
        {
            return _comboChain[CurrentComboIndex].animKey;
        }
    }
}