using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
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
        
        [Header("Attack Snap Settings")]
        [SerializeField] private float _snapSearchRange = 2f;    // 자석 탐색 범위
        [SerializeField] private float _snapSearchAngle = 60f;   // 정면 기준 탐색 각도
        [SerializeField] private float _snapMoveSpeed = 6f;      // 스냅 보정 속도 (루트모션에 합산)
        [SerializeField] private float _snapStopDistance = 1.2f;  // 이 거리 이내면 스냅 종료

        [Header("Finish Attack Settings")]
        [SerializeField] private float _finishAttackSearchRange = 0.5f;
        [SerializeField] private float _finishAttackSearchAngle = 90f;
        [SerializeField] private float _finishAttackDamageThreshold = 30f; // 이 값 이하 HP면 처형 가능

        public float SnapSearchRange => _snapSearchRange;
        public float SnapMoveSpeed => _snapMoveSpeed;
        public float SnapStopDistance => _snapStopDistance;
        public event Action<bool> OnChangeCombatState;
        
        // 현재 공격 정보 (히트 판정용)
        private AttackData _currentAttackData;
        
        // 현재 공격의 AttackInfoBase (멀티 히트 Phase 조회용)
        private AttackInfoBase _currentAttackInfoBase;
        
        private AttackState _attackState = AttackState.NormalAttack;
        private float _lastCombatEventTime = -999f;
        
        // 공격 충돌 감지가 가능한 상태인가?
        // - 애니메이션 이벤트로 적절한 상태에 설정
        private bool _isCollideCollisionEnable;
        private PlayerActor _playerActor;
        
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
            
            _playerActor = GetComponent<PlayerActor>();
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
        /// PlayerAttackInfo를 AttackData로 변환.
        /// Phase[0] 데이터를 초기값으로 세팅하고, 런타임에 SetHitPhaseIndex()로 갱신된다.
        /// </summary>
        private AttackData ConvertToAttackData(PlayerAttackInfo attackInfo)
        {
            _currentAttackInfoBase = attackInfo.baseInfo;
            var phase0 = attackInfo.baseInfo.GetHitPhase(0);

            return new AttackData
            {
                animKey          = attackInfo.baseInfo.animKey,
                damage           = phase0.damage,
                poiseDamage      = phase0.poiseDamage,
                canBeInterrupted = attackInfo.canBeInterrupted,
                reactionType     = phase0.reactionType,
                hitRange         = phase0.attackRadius,
                hitAngle         = attackInfo.hitAngle,
                hitHeightOffset  = phase0.attackOffset.y,
                hitParticleName  = phase0.hitParticleName,
                pullForce        = phase0.pullForce,
                knockbackForce   = phase0.knockBackForce,
                airborneForce    = phase0.airborneForce,
                hitPhaseIndex    = 0,
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
                    _currentAttackData.attacker = _playerActor;
                    
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
                // 킬 감지: 마지막 타격으로 적이 사망했는지 체크
                bool isKillHit = _currentAttackData.hitTarget != null 
                    && !(_currentAttackData.hitTarget.GetComponent<IDamageable>()?.IsAlive() ?? true);

                if (isKillHit)
                {
                    // 킬캠 시도 (확률/쿨다운 내부 체크)
                    CameraManager.Instance.TryKillCam(_currentAttackData.hitTarget.transform);
                }
                else
                {
                    // 일반 히트: 방향성 카메라 펀치 + 쉐이크 + 히트 스탑
                    CameraManager.Instance.Punch(_currentAttackData.attackDirection, 0.12f, 0.12f);
                    CameraManager.Instance.StartShake("LiteHit");
                    GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Medium);
                }
            }
        }
        
        public void SetEnableCollision(bool isCollisionEnable)
        {
            _isCollideCollisionEnable = isCollisionEnable;
        }

        /// <summary>
        /// BeginCollisionEvent에서 호출 — 현재 히트 Phase 인덱스를 AttackData에 반영한다.
        /// </summary>
        public void SetHitPhaseIndex(int index)
        {
            if (_currentAttackData == null || _currentAttackInfoBase == null) return;

            var phase = _currentAttackInfoBase.GetHitPhase(index);
            _currentAttackData.hitPhaseIndex    = index;
            _currentAttackData.damage           = phase.damage;
            _currentAttackData.poiseDamage      = phase.poiseDamage;
            _currentAttackData.reactionType     = phase.reactionType;
            _currentAttackData.hitRange         = phase.attackRadius;
            _currentAttackData.hitHeightOffset  = phase.attackOffset.y;
            _currentAttackData.hitParticleName  = phase.hitParticleName;
            _currentAttackData.pullForce        = phase.pullForce;
            _currentAttackData.airborneForce    = phase.airborneForce;
            _currentAttackData.knockbackForce   = phase.knockBackForce;
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
        
        #region Finish Attack

        /// <summary>
        /// 처형 가능한 타겟을 탐색한다.
        /// 정면 기준 각도 내, 지정 범위 안에서 HP가 임계값 이하인 가장 가까운 IDamageable을 반환.
        /// 없으면 null.
        /// </summary>
        public Transform FindFinishableTarget()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            Collider[] hits = Physics.OverlapSphere(origin, _finishAttackSearchRange, _targetLayerMask);

            Transform bestTarget = null;
            float bestDistSq = float.MaxValue;
            
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();
            
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 dir = hit.transform.position - origin;
                dir.y = 0f;
                if (Vector3.Angle(forward, dir) > _finishAttackSearchAngle)
                    continue;

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                         ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage())
                    continue;

                if (damageable.GetCurrentHealth() > _finishAttackDamageThreshold)
                    continue;

                float distSq = dir.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = hit.transform;
                }
                
                // 락온 대상이 가장 먼저.
                if (lockOnTarget != null && hit.transform == lockOnTarget)
                {
                    return hit.transform;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 반경 내의 모든 EnemyBrain 컴포넌트를 반환한다. (FinishAttackState의 freeze용)
        /// </summary>
        public List<EnemyBrain> GetEnemyBrainsInRadius(float radius)
        {
            var result = new List<EnemyBrain>();

            Collider[] hits = Physics.OverlapSphere(transform.position, radius, _targetLayerMask);
            foreach (var hit in hits)
            {
                EnemyBrain brain = hit.GetComponent<EnemyBrain>()
                                    ?? hit.GetComponentInParent<EnemyBrain>();
                if (brain != null && !result.Contains(brain))
                    result.Add(brain);
            }

            return result;
        }

        #endregion

        #region Attack Snap (Target Magnetism)

        /// <summary>
        /// 공격 시 자석 보정 대상을 탐색한다.
        /// 현재 히트 범위 내에 적이 있으면 null (보정 불필요).
        /// 히트 범위 밖 ~ 자석 범위 내에 적이 있으면 가장 가까운 적의 Transform 반환.
        /// </summary>
        public Transform FindAttackSnapTarget(float hitRange, float hitAngle)
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            // 1) 히트 범위 내 적이 있는지 체크 — 있으면 보정 불필요
            if (HasTargetInRange(origin, forward, hitRange, hitAngle))
                return null;

            // 2) 자석 범위에서 탐색
            Collider[] hits = Physics.OverlapSphere(origin, _snapSearchRange, _targetLayerMask);

            Transform bestTarget = null;
            float bestDistSq = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 dirToTarget = hit.transform.position - origin;
                dirToTarget.y = 0f;

                // 각도 필터
                float angle = Vector3.Angle(forward, dirToTarget);
                if (angle > _snapSearchAngle)
                    continue;

                // IDamageable 체크
                IDamageable damageable = hit.GetComponent<IDamageable>() 
                                         ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage())
                    continue;

                float distSq = dirToTarget.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = hit.transform;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 주어진 범위/각도 내에 히트 가능한 대상이 있는지 체크
        /// </summary>
        private bool HasTargetInRange(Vector3 origin, Vector3 forward, float range, float angle)
        {
            Collider[] hits = Physics.OverlapSphere(origin, range, _targetLayerMask);
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 dir = hit.transform.position - origin;
                dir.y = 0f;
                if (Vector3.Angle(forward, dir) > angle)
                    continue;

                IDamageable damageable = hit.GetComponent<IDamageable>() 
                                         ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.CanTakeDamage())
                    return true;
            }
            return false;
        }

        #endregion
        
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