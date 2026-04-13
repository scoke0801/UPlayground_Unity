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

        // ── 히트 피드백 설정 ──────────────────────────────────────────
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
        private List<IDamageable> _hitTargets = new List<IDamageable>();
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

        private int   _guardHitCount;
        private float _guardEndTime = -999f;

        public bool IsGuardBroken { get; private set; }
        public int  GuardHitCount => _guardHitCount;
        public int  MaxGuardCount => _maxGuardCount;

        public AttackData CurrentAttackData => _currentAttackData;
        public int        CurrentComboIndex { get; private set; }
        public float      LastAttackTime    { get; private set; }
        public bool       CanCombo          { get; private set; }

        public event Action<AttackData>                        OnAttackStarted;
        public event Action<AttackData>                        OnAttackHit;
        public event Action                                    OnComboReset;
        /// <summary>시퀀스 히스토리가 변경될 때마다 발행. 인수는 현재 히스토리 스냅샷.</summary>
        public event Action<IReadOnlyList<ComboInputType>>     OnSequenceUpdated;

        // ── 콤보 힌트 ─────────────────────────────────────────────────
        /// <summary>다음 가능한 콤보 입력 힌트. GetNextComboHints()의 반환 단위.</summary>
        public readonly struct NextComboHint
        {
            /// <summary>다음에 입력해야 하는 버튼 종류</summary>
            public readonly ComboInputType NextInput;
            /// <summary>이어지는 콤보의 이름</summary>
            public readonly string ComboName;
            /// <summary>이 입력 하나로 시퀀스가 완성되는지 여부</summary>
            public readonly bool IsComplete;
            /// <summary>우선순위 (동일 NextInput 중 최고값만 노출)</summary>
            public readonly int Priority;

            public NextComboHint(ComboInputType nextInput, string comboName, bool isComplete, int priority)
            {
                NextInput  = nextInput;
                ComboName  = comboName;
                IsComplete = isComplete;
                Priority   = priority;
            }
        }

        // ── 콤보 시퀀스 추적기 ────────────────────────────────────────
        private readonly InputSequenceTracker _sequenceTracker = new();

        // ── 콤보 힌트 버퍼 (GetNextComboHints GC 방지용 재사용) ──────
        private readonly List<NextComboHint>                       _comboHintsBuffer = new();
        private readonly Dictionary<ComboInputType, NextComboHint> _comboHintsBest   = new();

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
            _sequenceTracker.Record(ComboInputType.LightAttack);
            _playerActor.Tags?.AddTag(GameplayTags.Combo_Light);
            OnSequenceUpdated?.Invoke(new List<ComboInputType>(_sequenceTracker.History));
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
            _sequenceTracker.Record(ComboInputType.HeavyAttack);
            _playerActor.Tags?.AddTag(GameplayTags.Combo_Heavy);
            OnSequenceUpdated?.Invoke(new List<ComboInputType>(_sequenceTracker.History));
            _currentAttackData = ConvertToAttackData(_attackData.heavyComboAttackList[CurrentComboIndex], AttackKind.HeavyAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// 콤보 시퀀스 엔트리로 공격을 실행한다.
        /// PlayerAttackState.GetAnimKey()에서 시퀀스 매칭 후 호출한다.
        /// </summary>
        public AttackData ExecuteComboSequence(ComboSequenceEntry entry, bool isCombo)
        {
            // 시퀀스 마지막 스텝 입력 종류로 상태와 히스토리를 결정
            var lastStep      = entry.inputSequence != null && entry.inputSequence.Count > 0
                                    ? entry.inputSequence[^1]
                                    : null;
            var lastInputType = lastStep?.inputType ?? ComboInputType.LightAttack;
            var attackKind    = lastInputType == ComboInputType.HeavyAttack
                                    ? AttackKind.HeavyAttack
                                    : AttackKind.NormalAttack;

            _attackState      = lastInputType == ComboInputType.HeavyAttack
                                    ? AttackState.HeavyAttack
                                    : AttackState.NormalAttack;
            CurrentComboIndex = (isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            _sequenceTracker.Record(lastInputType);
            OnSequenceUpdated?.Invoke(new List<ComboInputType>(_sequenceTracker.History));
            _currentAttackData = ConvertToAttackData(entry.attackInfo, attackKind);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            Debug.Log($"[PlayerCombat] 콤보 시퀀스 '{entry.sequenceName}' 실행 | 히스토리: {_sequenceTracker.ToDebugString()}");
            return _currentAttackData;
        }

        /// <summary>
        /// 현재 입력 히스토리와 Actor 태그를 기반으로 매칭되는 콤보 시퀀스를 반환한다.
        /// 매칭되는 시퀀스가 없으면 null 반환.
        /// </summary>
        public ComboSequenceEntry FindMatchingSequence(ComboInputType nextInput)
        {
            if (_attackData.comboSequences == null || _attackData.comboSequences.Count == 0)
                return null;

            // 다음 입력까지 포함한 예비 히스토리로 매칭 판정을 위해 임시 추가
            // try/finally로 예외 시에도 반드시 롤백한다.
            _sequenceTracker.Record(nextInput);

            var tags = _playerActor.Tags;
            ComboSequenceEntry best = null;

            try
            {
                foreach (var entry in _attackData.comboSequences)
                {
                    if (entry.IsEmpty) continue;
                    if (!_sequenceTracker.Matches(entry.inputSequence)) continue;
                    if (!entry.CheckTagConditions(tags)) continue;

                    if (best == null || entry.priority > best.priority)
                        best = entry;
                }
            }
            finally
            {
                // 실제 기록은 Execute*에서 하므로 임시 추가분을 반드시 되돌린다.
                _sequenceTracker.RemoveLast();
            }

            return best;
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
            data.damage *= Mathf.Lerp(1.0f, 1.5f, chargeRatio);
            return data;
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
                _currentAttackData.hitTarget = hit.gameObject;
                _currentAttackData.hitPoint = hit.ClosestPoint(origin);
                _currentAttackData.attackDirection = (hit.transform.position - transform.position).normalized;
                _currentAttackData.attacker = _playerActor;

                damageable.TakeDamage(_currentAttackData);
                ShowDamageFloater(_currentAttackData);
                GameObjectManager.Instance.ShowFX(_currentAttackData.hitParticleName, _currentAttackData.hitPoint);
                OnAttackHit?.Invoke(_currentAttackData);
                hitOccurred = true;
            }

            if (hitOccurred)
                ApplyHitFeedback();
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

        public void ResetCombo()
        {
            LastAttackTime    = Time.time;
            CurrentComboIndex = 0;
            CanCombo          = false;
            _sequenceTracker.Clear();
            _playerActor.Tags?.RemoveTag(GameplayTags.Combo_Light);
            _playerActor.Tags?.RemoveTag(GameplayTags.Combo_Heavy);
            OnSequenceUpdated?.Invoke(new List<ComboInputType>(_sequenceTracker.History)); // 빈 리스트 → HUD 페이드 아웃
            OnComboReset?.Invoke();
            InputManager.Instance.InputBuffer.Clear();
        }

        /// <summary>
        /// 약/강공격 외 입력(회피, 스킬, 점프 등)을 시퀀스 히스토리에 기록하고 이벤트를 발행한다.
        /// 해당 액션을 처리하는 State에서 콤보 시퀀스 연계가 필요할 때 호출한다.
        /// </summary>
        public void RecordSequenceInput(ComboInputType inputType)
        {
            _sequenceTracker.Record(inputType);
            OnSequenceUpdated?.Invoke(new List<ComboInputType>(_sequenceTracker.History));
        }

        /// <summary>
        /// 현재 히스토리를 prefix로 가지는 콤보 시퀀스에서, 다음으로 가능한 입력 힌트 목록을 반환한다.
        /// 태그 조건을 통과하는 시퀀스만 포함하며, 동일한 다음 입력에는 priority가 높은 시퀀스 하나만 남긴다.
        /// 반환값은 내부 버퍼이므로 캐싱하지 말 것 — 다음 호출 전까지만 유효하다.
        /// </summary>
        public List<NextComboHint> GetNextComboHints()
        {
            _comboHintsBuffer.Clear();
            _comboHintsBest.Clear();

            if (_attackData?.comboSequences == null || _attackData.comboSequences.Count == 0)
                return _comboHintsBuffer;

            var tags    = _playerActor.Tags;
            var history = _sequenceTracker.History;

            foreach (var entry in _attackData.comboSequences)
            {
                if (entry.IsEmpty) continue;
                if (!entry.CheckTagConditions(tags)) continue;

                var seq = entry.inputSequence;
                if (seq.Count <= history.Count) continue; // 현재 히스토리가 이미 이 시퀀스 길이를 넘음

                // 현재 히스토리가 이 시퀀스의 prefix인지 확인
                bool isPrefix = true;
                for (int i = 0; i < history.Count; i++)
                {
                    if (history[i] != seq[i].inputType) { isPrefix = false; break; }
                }
                if (!isPrefix) continue;

                var  nextInput  = seq[history.Count].inputType;
                bool isComplete = seq.Count == history.Count + 1; // 이 입력 하나로 시퀀스 완성
                var  hint       = new NextComboHint(nextInput, entry.sequenceName, isComplete, entry.priority);

                if (!_comboHintsBest.TryGetValue(nextInput, out var existing) || entry.priority > existing.Priority)
                    _comboHintsBest[nextInput] = hint;
            }

            _comboHintsBuffer.AddRange(_comboHintsBest.Values);
            _comboHintsBuffer.Sort((a, b) => (int)a.NextInput - (int)b.NextInput);
            return _comboHintsBuffer;
        }

        /// <summary>입력 시퀀스 히스토리를 디버그 문자열로 반환한다.</summary>
        public string GetSequenceDebugString() => _sequenceTracker.ToDebugString();

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
