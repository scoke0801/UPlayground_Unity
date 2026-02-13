using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;
using UPlayGround.Animation;
using UPlayGround.Manager;

namespace UPlayGround.Component
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
        
        [Header("Hit Detection")]
        [Tooltip("히트 판정 범위 (반지름)")]
        public float hitRange = 2.0f;
        
        [Tooltip("히트 판정 각도 (전방 기준, 양쪽 각도)")]
        public float hitAngle = 60f;
        
        [Tooltip("히트 판정 높이 오프셋")]
        public float hitHeightOffset = 1.0f;
    }
    
    public class AttackData
    {
        public AnimKey animKey;
        public float damage;
        public float duration;
        public bool canBeInterrupted;
        
        // Hit Detection Data
        public float hitRange;
        public float hitAngle;
        public float hitHeightOffset;
        
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
        
        [Header("Hit Detection Settings")]
        [SerializeField] private LayerMask _targetLayerMask = -1; // 히트 가능한 레이어
        [SerializeField] private bool _showHitDebug = true; // 디버그 시각화

        // 현재 전투 상태
        public int CurrentComboIndex { get; private set; }
        public float LastAttackTime { get; private set; }
        public bool CanCombo { get; private set; }

        // 현재 공격 정보 (히트 판정용)
        private AttackData _currentAttackData;
        public AttackData CurrentAttackData => _currentAttackData;
        
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
            // if (!_equipment.IsMainWeaponEquipped)
            // {
            //     return null;
            // }

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
            _currentAttackData = ConvertToAttackData(comboData);
            
            LastAttackTime = Time.time;
            CanCombo = true;
            
            OnAttackStarted?.Invoke(_currentAttackData );
            
            return _currentAttackData ;
        }
        
        /// <summary>
        /// 강공격 실행
        /// </summary>
        public AttackData ExecuteHeavyAttack()
        {
            // if (!_equipment.IsSubWeaponEquipped)
            // {
            //     return null;
            // }
            //
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
            _currentAttackData = ConvertToAttackData(comboData);
            
            LastAttackTime = Time.time;
            CanCombo = true;
            
            OnAttackStarted?.Invoke(_currentAttackData );
            
            return _currentAttackData ;
        }
        
        /// <summary>
        /// 히트 판정 실행 (애니메이션 이벤트에서 호출)
        /// </summary>
        public void PerformHitDetection()
        {
            if (_currentAttackData == null)
            {
                Debug.LogWarning("[PlayerCombat] 현재 공격 정보가 없습니다.");
                return;
            }

            // 플레이어 위치와 방향
            Vector3 origin = transform.position + Vector3.up * _currentAttackData.hitHeightOffset;
            Vector3 forward = transform.forward;
            
            // 범위 내 모든 Collider 검출
            Collider[] hits = Physics.OverlapSphere(origin, _currentAttackData.hitRange, _targetLayerMask);
            
            List<IDamageable> hitTargets = new List<IDamageable>();
            
            //Physics.ComputePenetration()
            bool isDamageExecuted = false;
            foreach (var hit in hits)
            {
                // 자기 자신은 제외
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;
                
                // 각도 체크
                Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(forward, directionToTarget);
                
                if (angle > _currentAttackData.hitAngle)
                    continue;
                
                // IDamageable 컴포넌트 확인
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();
                
                //Physics.ComputePenetration()
                if (damageable != null && damageable.CanTakeDamage() && !hitTargets.Contains(damageable))
                {
                    hitTargets.Add(damageable);
                    
                    // AttackData에 히트 정보 채우기
                    _currentAttackData.hitTarget = hit.gameObject;
                    _currentAttackData.hitPoint = hit.ClosestPoint(origin);
                    
                    // 데미지 적용
                    damageable.TakeDamage(_currentAttackData);
                    
                    // 히트 이벤트 발생
                    OnAttackHit?.Invoke(_currentAttackData);

                    isDamageExecuted = true;
                    Debug.Log($"[PlayerCombat] 히트! Target: {hit.gameObject.name}, Damage: {_currentAttackData.damage}");
                }
            }

            if (isDamageExecuted)
            {
                CameraManager.Instance.StartShake("LiteHit");
            }
            
            if (_showHitDebug)
            {
                Debug.Log($"[PlayerCombat] 히트 판정: {hitTargets.Count}개 타겟 적중");
            }
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
                canBeInterrupted = comboData.canBeInterrupted,
                hitRange = comboData.hitRange,
                hitAngle = comboData.hitAngle,
                hitHeightOffset = comboData.hitHeightOffset
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
            return false;
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
            return !_equipment.IsMainWeaponEquipped;
        }
        /// <summary>
        /// 현재 콤보의 애니메이션 키 가져오기
        /// </summary>
        public AnimKey GetCurrentAttackAnimKey()
        {
            return _comboChain[CurrentComboIndex].animKey;
        }
        
        // 디버그 시각화
        private void OnDrawGizmosSelected()
        {
            if (!_showHitDebug || _currentAttackData == null)
                return;
            
            Vector3 origin = transform.position + Vector3.up * _currentAttackData.hitHeightOffset;
            Vector3 forward = transform.forward;
            
            // 히트 범위 구체
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, _currentAttackData.hitRange);
            
            // 히트 각도 시각화
            Gizmos.color = Color.yellow;
            Vector3 rightBoundary = Quaternion.Euler(0, _currentAttackData.hitAngle, 0) * forward * _currentAttackData.hitRange;
            Vector3 leftBoundary = Quaternion.Euler(0, -_currentAttackData.hitAngle, 0) * forward * _currentAttackData.hitRange;
            
            Gizmos.DrawLine(origin, origin + rightBoundary);
            Gizmos.DrawLine(origin, origin + leftBoundary);
            Gizmos.DrawLine(origin, origin + forward * _currentAttackData.hitRange);
        }
    }
}