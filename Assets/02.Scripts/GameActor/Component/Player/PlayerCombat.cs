using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;

namespace UPlayGround.Component
{
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
            JumpAttack,
            DashAttack,
            SkillAttack,
        }
        [FormerlySerializedAs("equipment")]
        [Header("References")]
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private ActorAnimator _actorAnimator;
        
        [FormerlySerializedAs("stats")]
        [Header("Combat Data")]
        [SerializeField] private PlayerAttackDataSO _attackData;
      
        [Header("Combat State")]
        [SerializeField] private float _combatStateDuration = 30f; // 전투 상태 유지 시간

        [Header("Hit Detection Settings")]
        [SerializeField] private LayerMask _targetLayerMask = -1; // 히트 가능한 레이어
        [SerializeField] private bool _showHitDebug = true; // 디버그 시각화

        public event Action<bool> OnChangeCombatState;
        
        // 현재 공격 정보 (히트 판정용)
        private AttackData _currentAttackData;
        
        private AttackState _attackState = AttackState.NormalAttack;
        private float _lastCombatEventTime = -999f;
        
        // 공격 충돌 감지가 가능한 상태인가?
        // - 애니메이션 이벤트로 적절한 상태에 설정
        private bool _isCollideCollisionEnable;
        
        private List<IDamageable> _hitTargets = new List<IDamageable>();
        
        // 가드 상태인가?
        public bool IsGuarding = false;
        
        /// <summary>
        /// 현재 전투 상태인지 여부
        /// </summary>
        public bool IsInCombat => Time.time - _lastCombatEventTime < _combatStateDuration;

        public AttackData CurrentAttackData => _currentAttackData;
        // 현재 전투 상태
        public int CurrentComboIndex { get; private set; }
        public float LastAttackTime { get; private set; }
        public bool CanCombo { get; private set; }
        
        public bool IsPossibleCollide => _isCollideCollisionEnable;
        
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

        private void Update()
        {
            if (IsPossibleCollide)
            {
                PerformHitDetection();
            }
        }
        
        /// <summary>
        /// Guard Break 공격인지 판정
        /// </summary>
        public bool IsGuardBreak(AttackData incomingAttack)
        {
            // [TODO] 여러 번 방어하면 깨진다거나 조치를 해볼까?
            return false;
        }
        
        /// <summary>
        /// 전투 상태 갱신 (공격/피격 시 등 전투 상태로 전환이 필요할 때 호출)
        /// </summary>
        public void RefreshCombatState()
        {
            bool prevState = IsInCombat;
            _lastCombatEventTime = Time.time;
            if (prevState != IsInCombat)
            {
                OnChangeCombatState?.Invoke(IsInCombat);
            }
        }
        
        /// <summary>
        /// 일반 공격 실행
        /// State에서 호출: playerCombat.ExecuteAttack()
        /// </summary>
        public AttackData ExecuteAttack(bool isCombo)
        {
            if (_attackState == AttackState.HeavyAttack)
            {
                ResetCombo();
            }
    
            _attackState = AttackState.NormalAttack;
            // 콤보 체인 체크
            if (isCombo && CanContinueCombo())
            {
                CurrentComboIndex++;
            }
            else
            {
                CurrentComboIndex = 0;
            }
            
            // ComboData를 AttackData로 변환
            var comboData = _attackData.liteComboAttackList[CurrentComboIndex];
            _currentAttackData = ConvertToAttackData(comboData);
            
            LastAttackTime = Time.time;

            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData );
            
            return _currentAttackData ;
        }
        
        /// <summary>
        /// 강공격 실행
        /// </summary>
        public AttackData ExecuteHeavyAttack(bool isCombo)
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
            if (isCombo && CanContinueCombo())
            {
                CurrentComboIndex++;
            }
            else
            {
                CurrentComboIndex = 0;
            }
            // ComboData를 AttackData로 변환
            var comboData = _attackData.heavyComboAttackList[CurrentComboIndex];
            _currentAttackData = ConvertToAttackData(comboData);
            
            LastAttackTime = Time.time;
            
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData );
            
            return _currentAttackData ;
        }
        
        public AttackData ExecuteSkillAttack(int skillIndex)
        {
            if (_attackData.skillAttackList.Count <= skillIndex)
            {
                return null;
            }

            var attackData = _attackData.skillAttackList[skillIndex];
            _currentAttackData = ConvertToAttackData(attackData);
            
            LastAttackTime = Time.time;
            
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            
            return _currentAttackData ;
        }
        
        /// <summary>
        /// ComboData를 AttackData로 변환
        /// </summary>
        private AttackData ConvertToAttackData(PlayerAttackInfo attackInfo)
        {
            float duration = GetAnimationDuration(attackInfo.baseInfo.animKey);
            
            return new AttackData
            {
                animKey = attackInfo.baseInfo.animKey,
                damage = attackInfo.baseInfo.damage,
                canBeInterrupted = attackInfo.canBeInterrupted,
                
                reactionType =  attackInfo.baseInfo.reactionType,
                
                hitRange = attackInfo.baseInfo.attackRadius,
                hitAngle = attackInfo.hitAngle,
                hitHeightOffset = attackInfo.baseInfo.attackOffset.y,
                
                hitParticleName = attackInfo.baseInfo.hitParticleName,
            };
        }
        /// <summary>
        /// 맞은 대상 초기화
        /// </summary>
        public void ClearHitTargets()
        {
            _hitTargets.Clear();
        }

        /// <summary>
        /// 히트 판정 실행
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
                if (damageable != null && damageable.CanTakeDamage() && !_hitTargets.Contains(damageable))
                {
                    _hitTargets.Add(damageable);
                    
                    // AttackData에 히트 정보 채우기
                    _currentAttackData.hitTarget = hit.gameObject;
                    _currentAttackData.hitPoint = hit.ClosestPoint(origin);
                    _currentAttackData.attackDirection = directionToTarget;
                    
                    // 데미지 적용
                    damageable.TakeDamage(_currentAttackData);
                    
                    GameObjectManager.Instance.ShowFX(_currentAttackData.hitParticleName,
                        _currentAttackData.hitPoint);

                    // 히트 이벤트 발생
                    OnAttackHit?.Invoke(_currentAttackData);

                    isDamageExecuted = true;
                    Debug.Log($"[PlayerCombat] 히트! Target: {hit.gameObject.name}, Damage: {_currentAttackData.damage}");
                }
            }

            if (isDamageExecuted)
            {
                // 카메라 쉐이크
                CameraManager.Instance.StartShake("LiteHit");
                
                // 히트 스탑
                GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Medium);
                 
            }
        }
        
        public void SetEnableCollision(bool isCollisionEnable)
        {
            _isCollideCollisionEnable = isCollisionEnable;
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
            
            float duration = _actorAnimator.GetMotionSetDuration(animKey);
            
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
            int length = Int32.MaxValue;
            switch (_attackState)
            {
                case AttackState.NormalAttack:
                    length = _attackData.liteComboAttackList.Count;
                    break;
                case AttackState.HeavyAttack:
                    length = _attackData.heavyComboAttackList.Count;
                    break;
                case AttackState.JumpAttack:
                    length = _attackData.jumpAttackList.Count;
                    break;
                case AttackState.DashAttack:
                    length = _attackData.dashAttackList.Count;
                    break;
                case AttackState.SkillAttack:
                    length = _attackData.skillAttackList.Count;
                    break;
                default:
                    return false;
            }
            
            return CurrentComboIndex < length - 1;
        }
        /// <summary>
        /// 콤보 윈도우 열기 (애니메이션 이벤트에서 호출)
        /// </summary>
        public void OpenComboWindow()
        {
            CanCombo = true;
        }

        public void CloseComboWindow()
        {
            CanCombo = false;
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
            InputManager.Instance.InputBuffer.Clear(); // 콤보 리셋 시 입력 버퍼 비우기
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