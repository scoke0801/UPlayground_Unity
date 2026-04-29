using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;

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

        [Header("Combat State — Threat Detection")]
        [Tooltip("주변 위협(aggro 중인 적) 자동 탐색 반경")]
        [SerializeField] private float _threatDetectionRange  = 20f;
        [Tooltip("위협 탐색 주기 (초)")]
        [SerializeField] private float _threatCheckInterval   = 0.5f;

        [Header("Hit Detection Settings")]
        [SerializeField] private LayerMask _targetLayerMask = -1;
        [SerializeField] private bool      _showHitDebug    = true;

        [Header("Homing — Search Range")]
        [Tooltip("락온 상태: 호밍 탐색 반경")]
        [SerializeField] private float _lockOnSnapSearchRange = 2f;
        [Tooltip("락온 상태: 호밍 탐색 각도")]
        [SerializeField] private float _lockOnSnapSearchAngle = 60f;

        [Space(4)]
        [Tooltip("자유 전투: 호밍 탐색 반경")]
        [SerializeField] private float _freeSnapSearchRange = 3.5f;
        [Tooltip("자유 전투: 호밍 탐색 각도")]
        [SerializeField] private float _freeSnapSearchAngle = 80f;

        [Header("Motion Warp Settings")]
        [Tooltip("워프 최소 거리. 이 거리 이내의 적에게는 워프 미적용 (씹힘 방지)")]
        [SerializeField] private float _warpMinDistance = 0.3f;
        [Tooltip("워프 최대 거리. 이 거리를 초과한 적에게는 워프 미적용")]
        [SerializeField] private float _warpMaxDistance = 4f;
        [Tooltip("워프 최대 속도. 클램프 후 남은 시간 내 도달 불가 거리면 워프 자체를 미적용")]
        [SerializeField] private float _warpMaxSpeed    = 18f;

        public float WarpMinDistance => _warpMinDistance;
        public float WarpMaxDistance => _warpMaxDistance;
        public float WarpMaxSpeed    => _warpMaxSpeed;

        [Header("Finish Attack Settings")]
        [SerializeField] private float _finishAttackSearchRange     = 0.5f;
        [SerializeField] private float _finishAttackSearchAngle     = 90f;
        [SerializeField] private float _finishAttackDamageThreshold = 30f;

        // ── 퍼펙트 도지 ──────────────────────────────────────────────
        [Header("Perfect Dodge Settings")]
        [Tooltip("도지 시작 후 이 시간(초) 내에 피격 시도가 감지되면 퍼펙트 도지로 판정")]
        [SerializeField] private float _perfectDodgeWindow = 0.25f;

        private float _perfectDodgeWindowEnd = -999f;

        /// <summary> 퍼펙트 도지 판정 창이 열려 있는지 여부 </summary>
        public bool IsPerfectDodgeWindow => Time.time <= _perfectDodgeWindowEnd;

        /// <summary> PlayerDodgeState.OnEnter에서 호출. 퍼펙트 도지 판정 창을 연다. </summary>
        public void OpenPerfectDodgeWindow()
            => _perfectDodgeWindowEnd = Time.time + _perfectDodgeWindow;

        /// <summary> 퍼펙트 도지 발동 후 중복 방지를 위해 창을 즉시 닫는다. </summary>
        public void ClosePerfectDodgeWindow()
            => _perfectDodgeWindowEnd = -999f;
        // ──────────────────────────────────────────────────────────────

        [Header("Hit Feedback — Punch Strength")]
        [SerializeField] private float _punchStrengthLight = 0.08f;
        [SerializeField] private float _punchStrengthHeavy = 0.18f;
        [SerializeField] private float _punchStrengthSkill = 0.22f;

        [Header("Hit Feedback — Punch Duration")]
        [SerializeField] private float _punchDurationLight = 0.12f;
        [SerializeField] private float _punchDurationHeavy = 0.18f;
        [SerializeField] private float _punchDurationSkill = 0.20f;

        [Header("Hit Feedback — Shake Keys")]
        [SerializeField] private CameraShakeIdType _shakeKeyLight = CameraShakeIdType.LiteHit;
        [SerializeField] private CameraShakeIdType _shakeKeyHeavy = CameraShakeIdType.HeavyHit;
        // ──────────────────────────────────────────────────────────────

        public float GetSnapSearchRange(bool isLockedOn) =>
            isLockedOn ? _lockOnSnapSearchRange : _freeSnapSearchRange;

        public float GetSnapSearchAngle(bool isLockedOn) =>
            isLockedOn ? _lockOnSnapSearchAngle : _freeSnapSearchAngle;

        public event Action<bool> OnChangeCombatState;

        private AttackData        _currentAttackData;
        private AttackInfoBase    _currentAttackInfoBase;
        private AttackState       _attackState         = AttackState.NormalAttack;
        private float             _lastCombatEventTime = -999f;
        private bool              _isCollideCollisionEnable;
        private PlayerActor       _playerActor;
        private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private bool              _cachedCombatState;
        private float             _threatCheckTimer;

        // ── Motion Warp 상태 ──────────────────────────────────────────
        // MotionEvent_MotionWarp.Execute() 시 워프 구간 길이(endTime-startTime)를 주입.
        // 매 프레임 deltaTime만큼 소모하며, 0 이하가 되면 워프 비활성.
        private float _warpRemainingTime;
        private float _warpTotalDuration;

        /// <summary> 워프 이벤트 구간 내 남은 시간. PlayerAttackState의 속력 역산에 사용. </summary>
        public float WarpRemainingTime => _warpRemainingTime;
        /// <summary> BeginMotionWarp 시 주입된 전체 워프 구간 길이. EaseOut 진행도 계산에 사용. </summary>
        public float WarpDuration      => _warpTotalDuration;
        public bool  IsMotionWarping   => _warpRemainingTime > 0f;
        // ──────────────────────────────────────────────────────────────

        public bool IsGuarding    = false;
        public bool IsInCombat    => Time.time - _lastCombatEventTime < _combatStateDuration;
        public bool IsPossibleCollide => _isCollideCollisionEnable;

        // ── 가드 내구도 ───────────────────────────────────────────────
        [Header("Guard Settings")]
        [SerializeField] private int   _maxGuardCount   = 3;
        [SerializeField] private float _guardResetDelay = 3f;
        [Tooltip("퍼펙트 가드 후 반격 입력을 받는 창 길이 (초)")]
        [SerializeField] private float _perfectGuardCounterWindow = 1.5f;
        [Tooltip("패리 후 반격 입력을 받는 창 길이 (초)")]
        [SerializeField] private float _parryCounterWindow = 1.5f;

        private int   _guardHitCount;
        private float _guardEndTime = -999f;
        private float _perfectGuardCounterEndTime = -999f;
        private float _parryCounterEndTime = -999f;

        public bool IsGuardBroken { get; private set; }
        public int  GuardHitCount => _guardHitCount;
        public int  MaxGuardCount => _maxGuardCount;

        /// <summary> 퍼펙트 가드 반격 창이 열려 있는지 여부 </summary>
        public bool IsPerfectGuardCounterAvailable => Time.time <= _perfectGuardCounterEndTime;

        /// <summary> 퍼펙트 가드 성공 시 호출. 반격 입력 창을 연다. </summary>
        public void OpenPerfectGuardCounterWindow()
            => _perfectGuardCounterEndTime = Time.time + _perfectGuardCounterWindow;

        /// <summary> 반격 창을 즉시 닫는다 (반격 실행 후 중복 방지) </summary>
        public void ClosePerfectGuardCounterWindow()
            => _perfectGuardCounterEndTime = -999f;

        /// <summary> 패리 반격 창이 열려 있는지 여부 </summary>
        public bool IsParryCounterAvailable => Time.time <= _parryCounterEndTime;

        /// <summary> 패리 성공 시 호출. 반격 입력 창을 연다. </summary>
        public void OpenParryCounterWindow()
            => _parryCounterEndTime = Time.time + _parryCounterWindow;

        /// <summary> 패리 반격 창을 즉시 닫는다 </summary>
        public void CloseParryCounterWindow()
            => _parryCounterEndTime = -999f;

        public AttackData CurrentAttackData => _currentAttackData;
        public int        CurrentComboIndex { get; private set; }
        public float      LastAttackTime    { get; private set; }
        public bool       CanCombo          { get; private set; }

        public event Action<AttackData>                        OnAttackStarted;
        public event Action<AttackData>                        OnAttackHit;
        public event Action                                    OnComboReset;
        
        private void Awake()
        {
            _playerActor   = GetComponent<PlayerActor>();
            // PlayerEquipment / ActorAnimator는 Model 하위에 있으므로 GetComponentInChildren 사용.
            // 최초에는 인스펙터 직렬화 값이 있으면 유지, 없으면 자동 탐색한다.
            if (_equipment     == null) _equipment     = GetComponentInChildren<PlayerEquipment>();
            if (_actorAnimator == null) _actorAnimator = GetComponentInChildren<ActorAnimator>();
        }

        /// <summary>
        /// 캐릭터 교체 시 PlayerActor.RefreshForCharacter에서 호출.
        /// 활성 Model의 PlayerEquipment / ActorAnimator를 재탐색한다.
        /// </summary>
        public void RefreshComponentReferences()
        {
            _equipment     = GetComponentInChildren<PlayerEquipment>();
            _actorAnimator = GetComponentInChildren<ActorAnimator>();
        }

        private void Update()
        {
            // 워프 남은 시간 소모
            if (_warpRemainingTime > 0f)
                _warpRemainingTime -= Time.deltaTime;

            if (IsPossibleCollide)
                PerformHitDetection();

            UpdateCombatState();
        }

        private void UpdateCombatState()
        {
            // 주기적 위협 탐색: aggro 중인 적이 있으면 전투 상태 타임스탬프 갱신
            _threatCheckTimer += Time.deltaTime;
            if (_threatCheckTimer >= _threatCheckInterval)
            {
                _threatCheckTimer = 0f;
                if (HasThreatNearby())
                    _lastCombatEventTime = Time.time;
            }

            // 전투 상태 변화 감지 → 이벤트 단일 발화
            bool current = IsInCombat;
            if (_cachedCombatState != current)
            {
                _cachedCombatState = current;
                OnChangeCombatState?.Invoke(current);
            }
        }

        private bool HasThreatNearby()
        {
            var brains = GetEnemyBrainsInRadius(_threatDetectionRange);
            foreach (var brain in brains)
            {
                if (brain.HasAggroTarget) return true;
            }
            return false;
        }

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

        public bool CanGuard() => Time.time - _guardEndTime >= _guardResetDelay;

        public void OnGuardStart()
        {
            IsGuardBroken  = false;
            _guardHitCount = 0;
        }

        public void OnGuardBreakConfirmed() => _guardEndTime = Time.time;

        public void ResetGuardCount()
        {
            _guardHitCount = 0;
            IsGuardBroken  = false;
            _guardEndTime  = -999f;
        }

        public void RefreshCombatState()
        {
            _lastCombatEventTime = Time.time;
            // 상태 변화 이벤트는 UpdateCombatState()에서 단일 발화
        }

        /// <summary>
        /// 전투 상태를 즉시 강제 해제한다. (예: 보스 사망, 안전 구역 진입)
        /// </summary>
        public void ForceExitCombat()
        {
            if (!IsInCombat) return;
            _lastCombatEventTime = -999f;
            // UpdateCombatState()에서 다음 프레임에 이벤트 발화
        }

        /// <summary>
        /// MotionEvent_MotionWarp.Execute()에서 호출.
        /// warpDuration = 이벤트의 endTime - startTime.
        /// </summary>
        public void BeginMotionWarp(float warpDuration)
        {
            _warpRemainingTime = warpDuration;
            _warpTotalDuration = warpDuration;
        }

        /// <summary>
        /// MotionEvent_MotionWarp.OnCompleteEvent()에서 호출.
        /// </summary>
        public void EndMotionWarp() => _warpRemainingTime = 0f;

        #region Execute Attack

        public AttackData ExecuteAttack(bool isCombo)
        {
            if (_attackState == AttackState.HeavyAttack) ResetCombo();
            _attackState      = AttackState.NormalAttack;
            CurrentComboIndex = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
             _playerActor.Tags?.AddTag(GameplayTagId.Combo_Light);
            _currentAttackData = ConvertToAttackData(_attackData.liteComboAttackList[CurrentComboIndex], AttackKind.NormalAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteHeavyAttack(bool isCombo)
        {
            if (_attackState == AttackState.NormalAttack) ResetCombo();
            _attackState      = AttackState.HeavyAttack;
            CurrentComboIndex = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0; 
            _playerActor.Tags?.AddTag(GameplayTagId.Combo_Heavy);
            _currentAttackData = ConvertToAttackData(_attackData.heavyComboAttackList[CurrentComboIndex], AttackKind.HeavyAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }
        
        public float[] GetChargeStageThresholds()
        {
            int stageCount = _attackData.chargeStageThresholds?.Count ?? 0;
            if (stageCount <= 1) return System.Array.Empty<float>();

            var configured = _attackData.chargeStageThresholds;
            int needed     = stageCount - 1;

            if (configured != null && configured.Count == needed)
                return configured.ToArray();

            var result = new float[needed];
            for (int i = 0; i < needed; i++)
                result[i] = (float)(i + 1) / stageCount;
            return result;
        }

        public AnimKey GetFirstChargeAttackAnimKey() => _attackData.chargeAnimKey;

        public (string key, ActorSocketType socket, Vector3 offset) GetFullChargeVfxData()
            => (_attackData.fullChargeVfxKey, _attackData.fullChargeVfxSocket, _attackData.fullChargeVfxOffset);

        public AttackData ExecuteChargeAttack(int stageIndex, float chargeRatio)
        {
            if (_attackData.chargeStages == null || _attackData.chargeStages.Count == 0) return null;
            _attackState = AttackState.ChargeAttack;
            ResetCombo();

            // stageIndex = InfiniteLoopStageIndex (0 = 1단계 차지, 1 = 2단계 차지 ...)
            // chargeStages 배열에서 해당 단계의 데이터를 사용한다.
            // hitPhaseIndex는 항상 0으로 시작 (각 스테이지의 첫 번째 히트 페이즈)
            int clampedStage = Mathf.Clamp(stageIndex, 0, _attackData.chargeStages.Count - 1);

            _currentAttackData = ConvertToChargeAttackData(_attackData.chargeStages[clampedStage], chargeRatio, 0);
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
            data.damage *= Mathf.Lerp(1.0f, 1.5f, chargeRatio);
            return data;
        }

        public AttackData ExecuteCounterAttack()
        {
            var source = _attackData.counterAttack?.baseInfo != null
                ? _attackData.counterAttack
                : (_attackData.heavyComboAttackList.Count > 0 ? _attackData.heavyComboAttackList[0] : null);

            if (source == null) return null;

            _attackState = AttackState.HeavyAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.HeavyAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteEntryAttack()
        {
            var source = _attackData.entryAttack?.baseInfo != null
                ? _attackData.entryAttack
                : (_attackData.liteComboAttackList.Count > 0 ? _attackData.liteComboAttackList[0] : null);

            if (source == null) return null;

            _attackState = AttackState.NormalAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.NormalAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteParryCounterAttack()
        {
            var source = _attackData.parryCounterAttack?.baseInfo != null
                ? _attackData.parryCounterAttack
                : (_attackData.counterAttack?.baseInfo != null
                    ? _attackData.counterAttack
                    : (_attackData.heavyComboAttackList.Count > 0 ? _attackData.heavyComboAttackList[0] : null));

            if (source == null) return null;

            _attackState = AttackState.HeavyAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.HeavyAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteSkillAttack(int skillIndex)
        {
            if (_attackData.skillAttackList.Count <= skillIndex) return null;
            _currentAttackData = ConvertToAttackData(_attackData.skillAttackList[skillIndex], AttackKind.SkillAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteJumpAttack(bool isCombo = false)
        {
            if (_attackData.jumpAttackList == null || _attackData.jumpAttackList.Count == 0) return null;
            if (_attackState != AttackState.JumpAttack) ResetCombo();
            _attackState      = AttackState.JumpAttack;
            CurrentComboIndex = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            CurrentComboIndex = Mathf.Clamp(CurrentComboIndex, 0, _attackData.jumpAttackList.Count - 1);
            _currentAttackData = ConvertToAttackData(_attackData.jumpAttackList[CurrentComboIndex], AttackKind.JumpAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        // jumpAttackList의 마지막 항목을 피니시 공격으로 실행
        public AttackData ExecuteJumpFinishAttack()
        {
            if (_attackData.jumpAttackList == null || _attackData.jumpAttackList.Count == 0) return null;
            _attackState      = AttackState.JumpAttack;
            CurrentComboIndex = _attackData.jumpAttackList.Count - 1;
            _currentAttackData = ConvertToAttackData(_attackData.jumpAttackList[CurrentComboIndex], AttackKind.JumpAttack);
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

        public AttackData ExecuteJumpDashAttack()
        {
            if (_attackData.dashAttackList == null || _attackData.dashAttackList.Count == 0) return null;
            _currentAttackData = ConvertToAttackData(_attackData.dashAttackList[0], AttackKind.DashAttack);
            _currentAttackData.animKey = AnimKey.JumpDashAttack_1;
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
                hitPhaseIndex          = 0,
                attackKind             = attackKind,
                victimForcedAnimKey    = phase0.victimForcedAnimKey,
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

            // 첫 번째 히트 정보만 피드백(킬캠 등)에 사용
            bool    hitOccurred   = false;
            Vector3 firstHitPoint = Vector3.zero;
            Vector3 firstHitDir   = Vector3.zero;
            GameObject firstHitTarget = null;

            _currentAttackData.attacker = _playerActor;

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
                    float closestY = hit.ClosestPoint(origin).y;
                    if (Mathf.Abs(closestY - origin.y) > _currentAttackData.hitHeightRange)
                        continue;
                }

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                         ?? hit.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.CanTakeDamage() || _hitTargets.Contains(damageable))
                    continue;

                Vector3 hitPoint   = hit.ClosestPoint(origin);
                Vector3 attackDir  = (hit.transform.position - transform.position).normalized;

                // 공유 AttackData에 퍼-타겟 정보 기록 (TakeDamage 및 이벤트 수신자 참조용)
                _currentAttackData.hitTarget       = hit.gameObject;
                _currentAttackData.hitPoint        = hitPoint;
                _currentAttackData.attackDirection = attackDir;

                _hitTargets.Add(damageable);
                damageable.TakeDamage(_currentAttackData);
                ShowDamageFloater(_currentAttackData);
                GameObjectManager.Instance.ShowFX(_currentAttackData.hitParticleName, hitPoint);
                OnAttackHit?.Invoke(_currentAttackData);

                if (!hitOccurred)
                {
                    hitOccurred      = true;
                    firstHitPoint    = hitPoint;
                    firstHitDir      = attackDir;
                    firstHitTarget   = hit.gameObject;
                }
            }

            if (hitOccurred)
            {
                // 피드백(킬캠, 히트스톱)은 첫 번째 히트 기준으로 적용
                _currentAttackData.hitTarget       = firstHitTarget;
                _currentAttackData.hitPoint        = firstHitPoint;
                _currentAttackData.attackDirection = firstHitDir;
                ApplyHitFeedback();
            }
        }

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

        private void ApplyHitFeedback()
        {
            // 패리 반격 창이 열려 있으면 Execute(PlayerGuard) 슬로우를 보호한다.
            // foreach 도중 패리가 발동됐거나 실행 순서상 뒤늦게 호출되는 경우 모두 차단.
            if (IsParryCounterAvailable) return;

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

            var orbTrigger = kind is AttackKind.HeavyAttack or AttackKind.ChargeAttack
                ? VitalOrbTrigger.HeavyAttackHit
                : VitalOrbTrigger.LightAttackHit;
            VitalOrbManager.Instance.TrySpawn(orbTrigger, _currentAttackData.hitPoint);

            switch (kind)
            {
                case AttackKind.ChargeAttack:
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

                default:
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
            _currentAttackData.damage          = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f);
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
                AttackState.ChargeAttack => 0,
                _                        => 0,
            };
            return CurrentComboIndex < length - 1;
        }

        public void OpenComboWindow()  => CanCombo = true;
        public void CloseComboWindow() => CanCombo = false;

        /// <summary>
        /// 캐릭터 교체 시 공격 데이터 SO를 교체하고 콤보를 초기화한다.
        /// </summary>
        public void RefreshAttackData(PlayerAttackDataSO newData)
        {
            _attackData = newData;
            ResetCombo();
        }

        public void ResetCombo()
        {
            LastAttackTime    = Time.time;
            CurrentComboIndex = 0;
            CanCombo          = false;
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Light);
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Heavy);
            OnComboReset?.Invoke();
            InputManager.Instance.InputBuffer.Clear();
        }
        #endregion

        #region Finish Attack

        public Transform FindFinishableTarget()
        {
            Vector3    origin  = transform.position;
            Vector3    forward = transform.forward;
            Collider[] hits    = Physics.OverlapSphere(origin, _finishAttackSearchRange, _targetLayerMask);

            Transform bestTarget   = null;
            float     bestDistSq   = float.MaxValue;
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
                    bestDistSq = distSq;
                    bestTarget = hit.transform;
                }
            }
            return bestTarget;
        }

        public List<EnemyBrain> GetEnemyBrainsInRadius(float radius)
        {
            var        result = new List<EnemyBrain>();
            Collider[] hits   = Physics.OverlapSphere(transform.position, radius, _targetLayerMask);
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

        #region Homing Target Search

        public Transform FindAttackSnapTarget(float hitRange, float hitAngle, bool isLockedOn)
        {
            Vector3 origin  = transform.position;
            Vector3 forward = transform.forward;

            if (HasTargetInRange(origin, forward, hitRange, hitAngle))
                return null;

            float      searchRange = GetSnapSearchRange(isLockedOn);
            float      searchAngle = GetSnapSearchAngle(isLockedOn);
            Collider[] hits        = Physics.OverlapSphere(origin, searchRange, _targetLayerMask);

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
                    bestDistSq = distSq;
                    bestTarget = hit.transform;
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
