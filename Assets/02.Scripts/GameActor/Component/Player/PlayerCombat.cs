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
using UPlayGround.UI;

namespace UPlayGround.Component
{
    /// <summary>
    /// 플레이어의 전투 관련 데이터와 로직.
    /// State는 "언제" 공격할지 결정하고
    /// Component는 "어떤" 공격을 실행하는지 처리한다.
    /// </summary>
    public class PlayerCombat : PlayerActorComponent
    {
        private enum AttackState
        {
            NormalAttack = 0,
            HeavyAttack  = 1,
            JumpAttack,
            DashAttack,
            SkillAttack,
            ChargeAttack,
        }

        [FormerlySerializedAs("equipment")]
        [Header("References")]
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private ActorAnimator   _actorAnimator;

        [FormerlySerializedAs("stats")]
        [Header("Combat Data")]
        [SerializeField] private PlayerAttackDataSO _attackData;

        [Header("Combat State")]
        [SerializeField] private float _combatStateDuration = 30f;

        [Header("Hit Detection Settings")]
        [SerializeField] private LayerMask _targetLayerMask = -1;
        [SerializeField] private bool      _showHitDebug    = true;

        [Header("Attack Snap Settings")]
        [Tooltip("락온 상태: 스냅 탐색 반경")]
        [SerializeField] private float _lockOnSnapSearchRange = 2f;
        [Tooltip("락온 상태: 스냅 탐색 각도")]
        [SerializeField] private float _lockOnSnapSearchAngle = 60f;

        [Space(4)]
        [Tooltip("자유 전투: 스냅 탐색 반경")]
        [SerializeField] private float _freeSnapSearchRange = 3.5f;
        [Tooltip("자유 전투: 스냅 탐색 각도")]
        [SerializeField] private float _freeSnapSearchAngle = 80f;

        [Space(4)]
        [SerializeField] private float _snapMoveSpeed    = 8f;
        [SerializeField] private float _snapStopDistance = 1.2f;

        [Header("Finish Attack Settings")]
        [SerializeField] private float _finishAttackSearchRange       = 0.5f;
        [SerializeField] private float _finishAttackSearchAngle       = 90f;
        [SerializeField] private float _finishAttackDamageThreshold   = 30f;

        // ── 히트 피드백 설정 ──────────────────────────────────────────
        // 공격 종류별 피드백 수치를 인스펙터에서 직접 튜닝할 수 있도록 분리한다.
        // 값을 0으로 두면 해당 효과를 비활성화한 것과 같다.
        [Header("Hit Feedback — Punch Strength")]
        [Tooltip("약 공격 히트 시 카메라 펀치 강도")]
        [SerializeField] private float _punchStrengthLight    = 0.08f;
        [Tooltip("강/대시/점프 공격 히트 시 카메라 펀치 강도")]
        [SerializeField] private float _punchStrengthHeavy    = 0.18f;
        [Tooltip("스킬 공격 히트 시 카메라 펀치 강도")]
        [SerializeField] private float _punchStrengthSkill    = 0.22f;

        [Header("Hit Feedback — Punch Duration")]
        [SerializeField] private float _punchDurationLight    = 0.12f;
        [SerializeField] private float _punchDurationHeavy    = 0.18f;
        [SerializeField] private float _punchDurationSkill    = 0.20f;

        [Header("Hit Feedback — Shake Keys")]
        [Tooltip("약 공격 쉐이크 DB 키")]
        [SerializeField] private string _shakeKeyLight  = "LiteHit";
        [Tooltip("강 공격 쉐이크 DB 키")]
        [SerializeField] private string _shakeKeyHeavy  = "HeavyHit";
        // ──────────────────────────────────────────────────────────────

        public float SnapMoveSpeed    => _snapMoveSpeed;
        public float SnapStopDistance => _snapStopDistance;

        public float GetSnapSearchRange(bool isLockedOn) =>
            isLockedOn ? _lockOnSnapSearchRange : _freeSnapSearchRange;

        public float GetSnapSearchAngle(bool isLockedOn) =>
            isLockedOn ? _lockOnSnapSearchAngle : _freeSnapSearchAngle;

        public event Action<bool> OnChangeCombatState;

        private AttackData     _currentAttackData;
        private AttackInfoBase _currentAttackInfoBase;
        private AttackState    _attackState = AttackState.NormalAttack;
        private float          _lastCombatEventTime = -999f;
        private bool           _isCollideCollisionEnable;
        private PlayerActor    _playerActor;
        private List<IDamageable> _hitTargets = new List<IDamageable>();

        public bool IsGuarding  = false;
        public bool IsInCombat => Time.time - _lastCombatEventTime < _combatStateDuration;

        // ── 가드 내구도 ───────────────────────────────────────────────
        // 연속으로 막을 수 있는 횟수. 초과 시 가드 브레이크 발생.
        [Header("Guard Settings")]
        [SerializeField] private int   _maxGuardCount    = 3;
        [SerializeField] private float _guardResetDelay  = 3f; // 가드 해제 후 이 시간이 지나면 카운트 초기화

        private int   _guardHitCount;
        private float _guardEndTime = -999f;

        public bool IsGuardBroken   { get; private set; }
        public int  GuardHitCount   => _guardHitCount;
        public int  MaxGuardCount   => _maxGuardCount;

        public AttackData CurrentAttackData => _currentAttackData;
        public int  CurrentComboIndex { get; private set; }
        public float LastAttackTime   { get; private set; }
        public bool  CanCombo         { get; private set; }
        public bool  IsPossibleCollide => _isCollideCollisionEnable;

        public event Action<AttackData> OnAttackStarted;
        public event Action<AttackData> OnAttackHit;
        public event Action             OnComboReset;

        private void Awake()
        {
            if (_equipment == null)
                _equipment = GetComponent<PlayerEquipment>();
            if (_actorAnimator == null)
                _actorAnimator = GetComponent<ActorAnimator>();
            _playerActor = GetComponent<PlayerActor>();
        }

        private void Update()
        {
            if (IsPossibleCollide)
                PerformHitDetection();
        }

        // 가드 브레이크 조건: 누적 횟수가 한계 도달
        public bool IsGuardBreak(AttackData incomingAttack)
        {
            if (IsGuardBroken) return true;

            _guardHitCount++;

            if (_guardHitCount >= _maxGuardCount)
            {
                IsGuardBroken = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 가드를 올릴 수 있는지 확인.
        /// 가드 브레이크 후 _guardResetDelay가 지나기 전에는 가드 불가.
        /// </summary>
        public bool CanGuard()
        {
            return Time.time - _guardEndTime >= _guardResetDelay;
        }

        /// <summary> 가드 시작 시 호출 — 카운트 유지, 브레이크 플래그만 해제 </summary>
        public void OnGuardStart()
        {
            IsGuardBroken  = false;
            _guardHitCount = 0;
        }

        /// <summary> 가드 브레이크 확정 시 호출 — 쿨타임 타이머 시작 </summary>
        public void OnGuardBreakConfirmed()
        {
            _guardEndTime = Time.time;
        }

        /// <summary> 완전 초기화 (부활, 씬 전환 등에서 사용) </summary>
        public void ResetGuardCount()
        {
            _guardHitCount = 0;
            IsGuardBroken  = false;
            _guardEndTime  = -999f;
        }

        public void RefreshCombatState()
        {
            bool prev = IsInCombat;
            _lastCombatEventTime = Time.time;
            if (prev != IsInCombat)
                OnChangeCombatState?.Invoke(IsInCombat);
        }

        #region Execute Attack

        public AttackData ExecuteAttack(bool isCombo)
        {
            if (_attackState == AttackState.HeavyAttack) ResetCombo();
            _attackState       = AttackState.NormalAttack;
            CurrentComboIndex  = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            _currentAttackData = ConvertToAttackData(_attackData.liteComboAttackList[CurrentComboIndex], AttackKind.NormalAttack);
            LastAttackTime     = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteHeavyAttack(bool isCombo)
        {
            if (_attackState == AttackState.NormalAttack) ResetCombo();
            _attackState       = AttackState.HeavyAttack;
            CurrentComboIndex  = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            _currentAttackData = ConvertToAttackData(_attackData.heavyComboAttackList[CurrentComboIndex], AttackKind.HeavyAttack);
            LastAttackTime     = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// 차지 단계 전환 임계값 배열을 반환한다.
        /// chargeStageThresholds가 비어 있으면 chargeStages.Count 기준으로 균등 분배한다.
        /// </summary>
        public float[] GetChargeStageThresholds()
        {
            int stageCount = _attackData.chargeStageThresholds?.Count ?? 0;
            if (stageCount <= 1) return System.Array.Empty<float>();

            var configured = _attackData.chargeStageThresholds;
            int needed = stageCount - 1;

            if (configured != null && configured.Count == needed)
                return configured.ToArray();

            // 균등 분배
            var result = new float[needed];
            for (int i = 0; i < needed; i++)
                result[i] = (float)(i + 1) / stageCount;
            return result;
        }

        /// <summary>
        /// 차지 공격 AnimKey를 반환한다.
        /// ChargeState 진입 시 어떤 MotionSet을 재생할지 결정하는 데 사용한다.
        /// </summary>
        public AnimKey GetFirstChargeAttackAnimKey()
        {
            return _attackData.chargeAnimKey;
        }

        /// <summary>
        /// 풀 차지 VFX 데이터 (키, 소켓, 오프셋)를 반환한다.
        /// </summary>
        public (string key, ActorSocketType socket, Vector3 offset) GetFullChargeVfxData()
            => (_attackData.fullChargeVfxKey, _attackData.fullChargeVfxSocket, _attackData.fullChargeVfxOffset);

        /// <summary>
        /// stageIndex: 차지 단계
        /// chargeRatio(0~1): 스테이지 내 차지 진행도 — 데미지 추가 스케일에만 사용.
        /// </summary>
        public AttackData ExecuteChargeAttack(int stageIndex, float chargeRatio)
        {
            if (_attackData.chargeStages == null || _attackData.chargeStages.Count == 0) return null;

            _attackState = AttackState.ChargeAttack;
            ResetCombo();

            int count = _attackData.chargeStages[0].hitPhases.Count;
            Debug.Log($"StageIndex: {stageIndex}, count: {count}");
            int index = Mathf.Clamp(stageIndex, 0, count - 1);

            _currentAttackData = ConvertToChargeAttackData(_attackData.chargeStages[0], chargeRatio, index);

            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        private AttackData ConvertToChargeAttackData(ChargeStageData stage, float chargeRatio, int phaseIndex)
        {
            _currentAttackInfoBase = null;
            var phase = stage.GetHitPhase(phaseIndex);

            var data = new AttackData
            {
                animKey          = _attackData.chargeAnimKey,
                damage           = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f),
                poiseDamage      = phase.poiseDamage,
                canBeInterrupted = stage.canBeInterrupted,
                reactionType     = phase.reactionType,
                hitRange         = phase.attackRadius,
                hitAngle         = stage.hitAngle,
                hitHeightOffset  = phase.attackOffset.y,
                hitHeightRange   = phase.hitHeightRange,
                hitParticleName  = phase.hitParticleName,
                pullForce        = phase.pullForce,
                knockbackForce   = phase.knockBackForce,
                knockbackDrag    = phase.knockBackDrag,
                airborneForce    = phase.airborneForce,
                hitPhaseIndex    = 0,
                attackKind       = AttackKind.ChargeAttack,
            };

            // 스테이지 내 차지 진행도에 비례해 데미지 추가 스케일 (1.0배 ~ 1.5배)
            data.damage *= Mathf.Lerp(1.0f, 1.5f, chargeRatio);
            return data;
        }

        public AttackData ExecuteSkillAttack(int skillIndex)
        {
            if (_attackData.skillAttackList.Count <= skillIndex) return null;
            _currentAttackData = ConvertToAttackData(_attackData.skillAttackList[skillIndex], AttackKind.SkillAttack);
            LastAttackTime     = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteJumpAttack()
        {
            if (_attackData.jumpAttackList == null || _attackData.jumpAttackList.Count == 0) return null;
            _currentAttackData = ConvertToAttackData(_attackData.jumpAttackList[0], AttackKind.JumpAttack);
            ResetCombo();
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteDashAttack()
        {
            if (_attackData.dashAttackList == null || _attackData.dashAttackList.Count == 0) return null;
            _currentAttackData = ConvertToAttackData(_attackData.dashAttackList[0], AttackKind.DashAttack);
            ResetCombo();
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public void SetupFinishAttackData()
        {
            _currentAttackInfoBase = null;
            _currentAttackData     = new AttackData
            {
                animKey          = AnimKey.FinishAttack,
                damage           = 9999f,
                poiseDamage      = 9999f,
                canBeInterrupted = false,
                reactionType     = AttackReactionType.Knockdown,
                hitRange         = 1.5f,
                hitAngle         = 90f,
                hitHeightOffset  = 1.0f,
                hitParticleName  = "HeavyHit",
                knockbackForce   = 0f,
                attackKind       = AttackKind.FinishAttack,
            };
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
        }

        private AttackData ConvertToAttackData(PlayerAttackInfo attackInfo, AttackKind attackKind)
        {
            _currentAttackInfoBase = attackInfo.baseInfo;
            var phase0 = attackInfo.baseInfo.GetHitPhase(0);
            
            return new AttackData
            {
                animKey          = attackInfo.baseInfo.animKey,
                damage           = UPlayGround.Util.ApplyRandomValue(phase0.damage, -0.2f, 0.2f),
                poiseDamage      = phase0.poiseDamage,
                canBeInterrupted = attackInfo.canBeInterrupted,
                reactionType     = phase0.reactionType,
                hitRange         = phase0.attackRadius,
                hitAngle         = attackInfo.hitAngle,
                hitHeightOffset  = phase0.attackOffset.y,
                hitHeightRange   = phase0.hitHeightRange,
                hitParticleName  = phase0.hitParticleName,
                pullForce        = phase0.pullForce,
                knockbackForce   = phase0.knockBackForce,
                knockbackDrag    = phase0.knockBackDrag,
                airborneForce    = phase0.airborneForce,
                hitPhaseIndex    = 0,
                attackKind       = attackKind,
            };
        }

        #endregion

        #region Hit Detection

        public void ClearHitTargets() => _hitTargets.Clear();

        public void PerformHitDetection()
        {
            if (_currentAttackData == null)
            {
                Debug.LogWarning("[PlayerCombat] 현재 공격 정보가 없습니다.");
                return;
            }

            Vector3    origin = transform.position + Vector3.up * _currentAttackData.hitHeightOffset;
            Collider[] hits   = Physics.OverlapSphere(origin, _currentAttackData.hitRange, _targetLayerMask);

            bool hitOccurred = false;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                Vector3 dirFlat = hit.transform.position - transform.position;
                dirFlat.y = 0f;
                if (dirFlat.sqrMagnitude > 0.001f)
                {
                    if (Vector3.Angle(transform.forward, dirFlat) > _currentAttackData.hitAngle)
                        continue;
                }

                if (_currentAttackData.hitHeightRange > 0f)
                {
                    if (Mathf.Abs(hit.transform.position.y - origin.y) > _currentAttackData.hitHeightRange)
                        continue;
                }

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                      ?? hit.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.CanTakeDamage() || _hitTargets.Contains(damageable))
                    continue;

                _hitTargets.Add(damageable);
                _currentAttackData.hitTarget       = hit.gameObject;
                _currentAttackData.hitPoint        = hit.ClosestPoint(origin);
                _currentAttackData.attackDirection = (hit.transform.position - transform.position).normalized;
                _currentAttackData.attacker        = _playerActor;

                damageable.TakeDamage(_currentAttackData);
                ShowDamageFloater(_currentAttackData);

                GameObjectManager.Instance.ShowFX(_currentAttackData.hitParticleName, _currentAttackData.hitPoint);
                OnAttackHit?.Invoke(_currentAttackData);
                hitOccurred = true;

                Debug.Log($"[PlayerCombat] 히트! Target: {hit.gameObject.name}, Damage: {_currentAttackData.damage}");
            }

            if (hitOccurred)
                ApplyHitFeedback();
        }

        /// <summary>
        /// Heavy / Skill / Finish → Critical 스타일, 그 외 → Normal.
        /// </summary>
        private void ShowDamageFloater(AttackData attackData)
        {
            var style = attackData.attackKind is AttackKind.HeavyAttack
                                              or AttackKind.SkillAttack
                                              or AttackKind.FinishAttack
                                              or AttackKind.ChargeAttack
                ? FloatStyle.Critical
                : FloatStyle.Normal;

            UIManager.Instance.ShowDamageFloater(attackData.hitPoint, attackData.damage, style);
        }

        /// <summary>
        /// 히트 성공 시 공격 종류(AttackKind)에 따라 피드백을 차별화한다.
        ///
        /// 분기 기준:
        ///  - FinishAttack / KillHit → KillCam (별도 처리)
        ///  - Heavy / Dash / Jump    → 강한 펀치 + 강한 히트스탑 + HeavyHit 쉐이크
        ///  - Skill                  → 가장 강한 펀치 + Critical 히트스탑
        ///  - Normal (그 외)         → 가벼운 펀치 + Light 히트스탑 + LiteHit 쉐이크
        /// </summary>
        private void ApplyHitFeedback()
        {
            GameHitStopManager.Instance.ResetActorTimeScale();

            bool isKillHit = _currentAttackData.hitTarget != null
                && !(_currentAttackData.hitTarget.GetComponent<IDamageable>()?.IsAlive() ?? true);

            if (isKillHit)
            {
                CameraManager.Instance.TryKillCam(_currentAttackData.hitTarget.transform);
                return;
            }

            var kind = _currentAttackData.attackKind;
            var dir  = _currentAttackData.attackDirection;

            // VitalOrb 트리거
            var orbTrigger = kind is AttackKind.HeavyAttack or AttackKind.ChargeAttack
                ? VitalOrbTrigger.HeavyAttackHit
                : VitalOrbTrigger.LightAttackHit;
            VitalOrbManager.Instance.TrySpawn(orbTrigger, _currentAttackData.hitPoint);

            switch (kind)
            {
                case AttackKind.ChargeAttack:  // 차지: 스킬 수준의 강한 피드백
                case AttackKind.SkillAttack:
                    CameraManager.Instance.Punch(dir, _punchStrengthSkill, _punchDurationSkill);
                    CameraManager.Instance.StartShake(_shakeKeyHeavy);
                    GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Critical);
                    break;

                case AttackKind.HeavyAttack:
                case AttackKind.DashAttack:
                case AttackKind.JumpAttack:
                    CameraManager.Instance.Punch(dir, _punchStrengthHeavy, _punchDurationHeavy);
                    CameraManager.Instance.StartShake(_shakeKeyHeavy);
                    GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Heavy);
                    break;

                default: // NormalAttack
                    CameraManager.Instance.Punch(dir, _punchStrengthLight, _punchDurationLight);
                    CameraManager.Instance.StartShake(_shakeKeyLight);
                    GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Light);
                    break;
            }
        }

        #endregion

        public void SetEnableCollision(bool isCollisionEnable) =>
            _isCollideCollisionEnable = isCollisionEnable;

        public void SetHitPhaseIndex(int index)
        {
            if (_currentAttackData == null || _currentAttackInfoBase == null) return;
            var phase = _currentAttackInfoBase.GetHitPhase(index);
            _currentAttackData.hitPhaseIndex   = index;
            _currentAttackData.damage = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f);
            _currentAttackData.poiseDamage     = phase.poiseDamage;
            _currentAttackData.reactionType    = phase.reactionType;
            _currentAttackData.hitRange        = phase.attackRadius;
            _currentAttackData.hitHeightOffset = phase.attackOffset.y;
            _currentAttackData.hitHeightRange  = phase.hitHeightRange;
            _currentAttackData.hitParticleName = phase.hitParticleName;
            _currentAttackData.pullForce       = phase.pullForce;
            _currentAttackData.airborneForce   = phase.airborneForce;
            _currentAttackData.knockbackForce  = phase.knockBackForce;
            _currentAttackData.knockbackDrag   = phase.knockBackDrag;
        }

        private float GetAnimationDuration(AnimKey animKey)
        {
            if (_actorAnimator == null) return 1.0f;
            float duration = _actorAnimator.GetMotionSetDuration(animKey);
            return duration > 0 ? duration : 1.0f;
        }

        #region Combo

        private bool CanContinueCombo()
        {
            int length = _attackState switch
            {
                AttackState.NormalAttack => _attackData.liteComboAttackList.Count,
                AttackState.HeavyAttack  => _attackData.heavyComboAttackList.Count,
                AttackState.JumpAttack   => _attackData.jumpAttackList.Count,
                AttackState.DashAttack   => _attackData.dashAttackList.Count,
                AttackState.SkillAttack  => _attackData.skillAttackList.Count,
                AttackState.ChargeAttack => 0, // 차지 공격은 콤보 없음
                _                        => 0,
            };
            return CurrentComboIndex < length - 1;
        }

        public void OpenComboWindow()  => CanCombo = true;
        public void CloseComboWindow() => CanCombo = false;

        public void ResetCombo()
        {
            LastAttackTime    = Time.time;
            CurrentComboIndex = 0;
            CanCombo          = false;
            OnComboReset?.Invoke();
            InputManager.Instance.InputBuffer.Clear();
        }

        #endregion

        #region Finish Attack

        public Transform FindFinishableTarget()
        {
            Vector3 origin  = transform.position;
            Vector3 forward = transform.forward;
            Collider[] hits = Physics.OverlapSphere(origin, _finishAttackSearchRange, _targetLayerMask);

            Transform bestTarget = null;
            float     bestDistSq = float.MaxValue;
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                Vector3 dir = hit.transform.position - origin;
                dir.y = 0f;
                if (Vector3.Angle(forward, dir) > _finishAttackSearchAngle) continue;

                MonsterActor monsterActor = hit.GetComponent<MonsterActor>()
                                         ?? hit.GetComponentInParent<MonsterActor>();
                if (monsterActor == null || !monsterActor.CanTakeDamage()) continue;
                if (monsterActor.Grade == MonsterActorGrade.Weak) continue;
                if (monsterActor.GetCurrentHealth() > _finishAttackDamageThreshold) continue;

                if (lockOnTarget != null && hit.transform == lockOnTarget)
                    return hit.transform;

                float distSq = dir.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq  = distSq;
                    bestTarget  = hit.transform;
                }
            }
            return bestTarget;
        }

        public List<EnemyBrain> GetEnemyBrainsInRadius(float radius)
        {
            var result  = new List<EnemyBrain>();
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

        #region Attack Snap

        public Transform FindAttackSnapTarget(float hitRange, float hitAngle, bool isLockedOn)
        {
            Vector3 origin  = transform.position;
            Vector3 forward = transform.forward;

            if (HasTargetInRange(origin, forward, hitRange, hitAngle))
                return null;

            float searchRange = GetSnapSearchRange(isLockedOn);
            float searchAngle = GetSnapSearchAngle(isLockedOn);
            Collider[] hits   = Physics.OverlapSphere(origin, searchRange, _targetLayerMask);

            Transform bestTarget = null;
            float     bestDistSq = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                Vector3 dirToTarget = hit.transform.position - origin;
                dirToTarget.y = 0f;
                if (Vector3.Angle(forward, dirToTarget) > searchAngle) continue;

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                      ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;

                float distSq = dirToTarget.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq  = distSq;
                    bestTarget  = hit.transform;
                }
            }
            return bestTarget;
        }

        private bool HasTargetInRange(Vector3 origin, Vector3 forward, float range, float angle)
        {
            Collider[] hits = Physics.OverlapSphere(origin, range, _targetLayerMask);
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                Vector3 dir = hit.transform.position - origin;
                dir.y = 0f;
                if (Vector3.Angle(forward, dir) > angle) continue;

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                      ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.CanTakeDamage()) return true;
            }
            return false;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (!_showHitDebug || _currentAttackData == null) return;

            Vector3 origin  = transform.position + Vector3.up * _currentAttackData.hitHeightOffset;
            Vector3 forward = transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, _currentAttackData.hitRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0,  _currentAttackData.hitAngle, 0) * forward * _currentAttackData.hitRange);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, -_currentAttackData.hitAngle, 0) * forward * _currentAttackData.hitRange);
            Gizmos.DrawLine(origin, origin + forward * _currentAttackData.hitRange);
        }
    }
}
