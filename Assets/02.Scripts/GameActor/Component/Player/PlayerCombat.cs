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
using UPlayGround.Manager.Combat;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;

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
        public SpecialBreakAttackAsset SpecialBreakAttackData => _specialBreakAttackData;

        [Header("Finish Attack Settings")]
        [SerializeField] private float _finishAttackSearchRange     = 0.5f;
        [SerializeField] private float _finishAttackSearchAngle     = 90f;
        [SerializeField] private float _finishAttackDamageThreshold = 30f;

        [Header("Special Break Attack Settings")]
        [SerializeField] private SpecialBreakAttackAsset _specialBreakAttackData;
        [SerializeField] private float _specialBreakAttackSearchRange = 4f;
        [SerializeField] private float _specialBreakAttackSearchAngle = 110f;

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
        private IReadOnlyList<HitPhaseData> _currentResidualHitPhases;
        private MonsterActor      _currentFinishTarget;
        private MonsterActor      _currentSpecialBreakTarget;
        private float             _currentSpecialBreakDamageByMaxHpRate;
        private float             _currentSpecialBreakFixedDamage;
        private AttackState       _attackState         = AttackState.NormalAttack;
        private float             _lastCombatEventTime = -999f;
        private bool              _isCollideCollisionEnable;
        private PlayerActor       _playerActor;
        private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private bool              _cachedCombatState;
        private float             _threatCheckTimer;
        private readonly Collider[] _threatOverlapBuffer = new Collider[128];

        // ── Motion Warp 상태 ──────────────────────────────────────────
        // 진실 소스는 MotionWarpController. 본 클래스는 호환 프록시만 노출한다.
        private MotionWarpController _motionWarp;

        /// <summary> 워프 이벤트 구간 내 남은 시간. PlayerAttackState의 속력 역산에 사용. </summary>
        public float WarpRemainingTime => _motionWarp != null ? _motionWarp.WarpRemainingTime : 0f;
        /// <summary> BeginMotionWarp 시 주입된 전체 워프 구간 길이. EaseOut 진행도 계산에 사용. </summary>
        public float WarpDuration      => _motionWarp != null ? _motionWarp.WarpDuration : 0f;
        public bool  IsMotionWarping   => _motionWarp != null && _motionWarp.IsMotionWarping;
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
        public LayerMask  WarpTargetLayer   => _targetLayerMask;

        // 0-할당 보장: targetFilter 람다를 정적 필드로 고정. 매 호출 BuildWarpResolverContext 에서
        // 재할당 없이 동일 delegate 인스턴스 재사용.
        private static readonly Func<Transform, bool> WarpDamageableFilter = static t =>
        {
            var d = t.GetComponent<IDamageable>() ?? t.GetComponentInParent<IDamageable>();
            return d != null && d.CanTakeDamage();
        };

        /// <summary>
        /// resolver 호출용 컨텍스트. 현재 공격 데이터의 hitRange/hitAngle 을 사용한다.
        /// CurrentAttackData 가 없으면 default(WarpResolverContext) 반환.
        /// </summary>
        public WarpResolverContext BuildWarpResolverContext()
        {
            if (_currentAttackData == null) return default;
            return new WarpResolverContext
            {
                origin       = transform,
                hitRange     = _currentAttackData.hitRange,
                hitAngle     = _currentAttackData.hitAngle,
                targetLayer  = _targetLayerMask,
                targetFilter = WarpDamageableFilter,
            };
        }

        public event Action<AttackData>                        OnAttackStarted;
        public event Action<AttackData>                        OnAttackHit;
        public event Action                                    OnComboReset;

        public bool TryCreateResidualAttackSnapshot(
            CharacterModelData sourceModel,
            out PlayerResidualAttackSnapshot snapshot)
        {
            snapshot = default;

            if (_playerActor == null || sourceModel == null)
            {
                Debug.LogWarning($"[ResidualAttack] Snapshot failed: actor/model missing. actor={_playerActor != null}, sourceModel={sourceModel != null}");
                return false;
            }

            string stateName = _playerActor.PlayerController?.CurrentState?.StateName;
            if (!IsResidualAttackState(stateName))
            {
                Debug.Log($"[ResidualAttack] Snapshot skipped: unsupported state. state={stateName}, animKey={_currentAttackData?.animKey}, kind={_currentAttackData?.attackKind}");
                return false;
            }

            if (_currentAttackData == null)
                Debug.Log($"[ResidualAttack] Snapshot will be visual-only: current attack data is null. state={stateName}, model={sourceModel.characterType}");

            var playbackSnapshot = _actorAnimator != null
                ? _actorAnimator.CapturePlaybackSnapshot()
                : ActorAnimator.MotionPlaybackSnapshot.Empty;
            if (!playbackSnapshot.IsValid || playbackSnapshot.Key == AnimKey.None)
            {
                Debug.LogWarning($"[ResidualAttack] Snapshot failed: playback snapshot invalid. state={stateName}, attackAnimKey={_currentAttackData?.animKey}, animator={_actorAnimator != null}");
                return false;
            }

            if (_currentAttackData != null && playbackSnapshot.Key != _currentAttackData.animKey)
                Debug.Log($"[ResidualAttack] Snapshot allows anim key mismatch for residual presentation. state={stateName}, playbackKey={playbackSnapshot.Key}, attackAnimKey={_currentAttackData.animKey}, kind={_currentAttackData.attackKind}");

            snapshot = new PlayerResidualAttackSnapshot(
                _playerActor,
                sourceModel,
                sourceModel.characterType,
                CopyAttackData(_currentAttackData),
                _currentAttackInfoBase,
                _currentResidualHitPhases,
                playbackSnapshot,
                _playerActor.GetAttackTargetLayerMask(),
                sourceModel.transform.position,
                sourceModel.transform.rotation,
                _currentFinishTarget,
                _currentSpecialBreakTarget,
                _currentSpecialBreakDamageByMaxHpRate,
                _currentSpecialBreakFixedDamage);

            Debug.Log($"[ResidualAttack] Snapshot created. character={sourceModel.characterType}, state={stateName}, playbackKey={playbackSnapshot.Key}, attackAnimKey={_currentAttackData?.animKey}, kind={_currentAttackData?.attackKind}, visualOnly={_currentAttackData == null}, hitRange={_currentAttackData?.hitRange}, hitAngle={_currentAttackData?.hitAngle}, hitPhase={_currentAttackData?.hitPhaseIndex}, hasInfoBase={_currentAttackInfoBase != null}, hitPhaseCount={_currentResidualHitPhases?.Count ?? 0}, finishTarget={_currentFinishTarget != null}, specialBreakTarget={_currentSpecialBreakTarget != null}");
            return true;
        }

        /// <summary>
        /// 외부(투사체 등)에서 히트가 성립했음을 알릴 때 호출.
        /// OnAttackHit 이벤트를 발화시켜 스킬 게이지 등 후속 처리가 이어지게 한다.
        /// </summary>
        public void NotifyAttackHit(AttackData attackData)
        {
            if (attackData == null) return;
            OnAttackHit?.Invoke(attackData);
        }
        
        private void Awake()
        {
            _playerActor   = GetComponent<PlayerActor>();
            // PlayerEquipment / ActorAnimator는 Model 하위에 있으므로 GetComponentInChildren 사용.
            // 최초에는 인스펙터 직렬화 값이 있으면 유지, 없으면 자동 탐색한다.
            if (_equipment     == null) _equipment     = GetComponentInChildren<PlayerEquipment>();
            if (_actorAnimator == null) _actorAnimator = GetComponentInChildren<ActorAnimator>();

            // 워프 진실 소스는 MotionWarpController. 컴포넌트가 없으면 즉시 부착(ActorMovementController와 동일 패턴).
            _motionWarp = GetComponent<MotionWarpController>();
            if (_motionWarp == null)
                _motionWarp = gameObject.AddComponent<MotionWarpController>();
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
            // 워프 타이머는 MotionWarpController.Update 가 처리.
            if (IsPossibleCollide)
                PerformHitDetection();

            UpdateCombatState();
            UpdateBreakPromptTarget();
        }

        // ── Break Prompt 게이팅 ───────────────────────────────────────
        // '노출된 모든 적'이 아니라 '지금 F를 누르면 실제로 브레이크될 단일 적'에게만
        // 프롬프트를 표시한다. 선정 기준은 FindSpecialBreakAttackTarget과 동일 소스를 사용한다.
        private MonsterActor _currentBreakPromptTarget;
        private float _breakPromptTickTimer;
        private const float BreakPromptTickInterval = 0.1f;

        private void UpdateBreakPromptTarget()
        {
            // 매 프레임 Physics.OverlapSphere를 도는 비용을 막기 위해 100ms 단위로 갱신.
            _breakPromptTickTimer += Time.deltaTime;
            if (_breakPromptTickTimer < BreakPromptTickInterval) return;
            _breakPromptTickTimer = 0f;

            // 노출된 적이 하나도 없으면 물리 탐색 없이 즉시 정리(상시 비용 0).
            if (MonsterActor.ExposedMonsters.Count == 0)
            {
                SetBreakPromptTarget(null);
                return;
            }

            // 플레이어가 F를 누를 수 없는 상태(피격·스턴·사망·잡힘)에선 프롬프트도 숨긴다.
            string playerState = _playerActor.PlayerController?.CurrentState?.StateName;
            if (playerState is "Hit" or "Stun" or "Death" or "Grabbed" or "Knockdown")
            {
                SetBreakPromptTarget(null);
                return;
            }

            Transform targetTf = FindSpecialBreakAttackTarget();
            MonsterActor target = targetTf != null
                ? targetTf.GetComponent<MonsterActor>() ?? targetTf.GetComponentInParent<MonsterActor>()
                : null;
            SetBreakPromptTarget(target);
        }

        private void OnDisable()
        {
            // 컷씬·씬 전환 등으로 비활성화될 때 프롬프트가 남지 않도록 정리.
            SetBreakPromptTarget(null);
        }

        private void SetBreakPromptTarget(MonsterActor target)
        {
            if (_currentBreakPromptTarget == target) return;

            // Unity의 != null은 파괴된 오브젝트에 false를 반환하므로 안전.
            if (_currentBreakPromptTarget != null)
                _currentBreakPromptTarget.SetBreakPromptActive(false);

            _currentBreakPromptTarget = target;

            if (_currentBreakPromptTarget != null)
                _currentBreakPromptTarget.SetBreakPromptActive(true);
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
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _threatDetectionRange,
                _threatOverlapBuffer,
                _targetLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _threatOverlapBuffer[i];
                if (hit == null)
                    continue;

                var monster = hit.GetComponent<MonsterActor>()
                              ?? hit.GetComponentInParent<MonsterActor>();
                if (monster?.AIController != null && monster.AIController.HasAggroTarget)
                    return true;
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
        public void BeginMotionWarp(float warpDuration) => _motionWarp?.BeginMotionWarp(warpDuration);

        /// <summary>
        /// MotionEvent_MotionWarp.OnCompleteEvent()에서 호출.
        /// </summary>
        public void EndMotionWarp() => _motionWarp?.EndMotionWarp();

        #region Execute Attack

        public AttackData ExecuteAttack(bool isCombo)
        {
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
            if (_attackData.chargeStages == null || _attackData.chargeStages.Count == 0) return null;
            _attackState = AttackState.ChargeAttack;
            ResetCombo();

            // stageIndex = InfiniteLoopStageIndex (0 = 1단계 차지, 1 = 2단계 차지 ...)
            // chargeStages 배열에서 해당 단계의 데이터를 사용한다.
            // hitPhaseIndex는 항상 0으로 시작 (각 스테이지의 첫 번째 히트 페이즈)
            int clampedStage = Mathf.Clamp(stageIndex, 0, _attackData.chargeStages.Count - 1);

            _currentAttackData = ConvertToChargeAttackData(_attackData.chargeStages[clampedStage], chargeRatio, 0);
            _currentResidualHitPhases = _attackData.chargeStages[clampedStage].hitPhases;
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
                breakDamage      = phase.breakDamage,
                reactionDuration = phase.reactionDuration,
                forceReaction    = phase.forceReaction,
                forceBreakExpose = phase.forceBreakExpose,
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
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
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

        public AttackData ExecuteSwapSpecialAttack()
        {
            ClearResidualAttackContext();
            var source = _attackData.swapSpecialAttack?.baseInfo != null
                ? _attackData.swapSpecialAttack
                : (_attackData.skillAttackList.Count > 0 && _attackData.skillAttackList[0]?.baseInfo != null
                    ? _attackData.skillAttackList[0]
                    : (_attackData.entryAttack?.baseInfo != null ? _attackData.entryAttack : null));

            if (source == null) return null;

            _attackState = AttackState.SkillAttack;
            ResetCombo();
            _currentAttackData = ConvertToAttackData(source, AttackKind.SkillAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteParryCounterAttack()
        {
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
            if (_attackData.skillAttackList.Count <= skillIndex) return null;
            _currentAttackData = ConvertToAttackData(_attackData.skillAttackList[skillIndex], AttackKind.SkillAttack);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteJumpAttack(bool isCombo = false)
        {
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
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
            ClearResidualAttackContext();
            if (_attackData.dashAttackList == null || _attackData.dashAttackList.Count == 0) return null;
            _currentAttackData = ConvertToAttackData(_attackData.dashAttackList[0], AttackKind.DashAttack);
            _currentAttackData.animKey = AnimKey.JumpDashAttack_1;
            ResetCombo();
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public void SetupFinishAttackData(Transform finishTarget = null)
        {
            ClearResidualAttackContext();
            _currentAttackInfoBase = null;
            _currentFinishTarget = finishTarget != null
                ? finishTarget.GetComponent<MonsterActor>() ?? finishTarget.GetComponentInParent<MonsterActor>()
                : null;
            _currentAttackData     = new AttackData
            {
                animKey          = AnimKey.FinishAttack,
                damage           = 9999f,
                poiseDamage      = 9999f,
                breakDamage      = 0f,
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

        public void SetupSpecialBreakAttackData(SpecialBreakAttackAsset specialBreakAttack, MonsterActor target)
        {
            // 어셋 누락 시 매직 디폴트(20% MaxHP)로 일반 모션을 발화하지 않도록 fail-fast.
            // 상태 진입은 호출부에서 막지 못해도, 여기서 데미지 흐름을 끊는다.
            if (specialBreakAttack == null)
            {
                Debug.LogError($"[PlayerCombat] SetupSpecialBreakAttackData: SpecialBreakAttackAsset이 null입니다. target={target?.name}");
                return;
            }

            ClearResidualAttackContext();
            _currentAttackInfoBase = null;
            _currentSpecialBreakTarget = target;
            _currentSpecialBreakDamageByMaxHpRate = Mathf.Max(0f, specialBreakAttack.damageByMaxHpRate);
            _currentSpecialBreakFixedDamage = Mathf.Max(0f, specialBreakAttack.fixedDamage);
            _currentAttackData = new AttackData
            {
                animKey = ResolveSpecialBreakMotionKey(specialBreakAttack),
                damage = _currentSpecialBreakFixedDamage,
                poiseDamage = 0f,
                breakDamage = 0f,
                canBeInterrupted = false,
                reactionType = AttackReactionType.Heavy,
                hitRange = 1.5f,
                hitAngle = 90f,
                hitHeightOffset = 1.0f,
                hitParticleName = "HeavyHit",
                attackKind = AttackKind.SkillAttack,
            };
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
        }

        private AnimKey ResolveSpecialBreakMotionKey(SpecialBreakAttackAsset specialBreakAttack)
        {
            if (specialBreakAttack.animKey != AnimKey.None)
                return specialBreakAttack.animKey;

            if (_actorAnimator != null && _actorAnimator.HasMotion(AnimKey.FinishAttack, true))
                return AnimKey.FinishAttack;

            return AnimKey.Attack_1;
        }

        private AttackData ConvertToAttackData(PlayerAttackInfo attackInfo, AttackKind attackKind)
        {
            _currentAttackInfoBase = attackInfo.baseInfo;
            _currentResidualHitPhases = attackInfo.baseInfo.hitPhases;
            var phase0 = attackInfo.baseInfo.GetHitPhase(0);

            return new AttackData
            {
                animKey          = attackInfo.baseInfo.animKey,
                damage           = UPlayGround.Util.ApplyRandomValue(phase0.damage, -0.2f, 0.2f),
                poiseDamage      = phase0.poiseDamage,
                breakDamage      = phase0.breakDamage,
                reactionDuration = phase0.reactionDuration,
                forceReaction    = phase0.forceReaction,
                forceBreakExpose = phase0.forceBreakExpose,
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

        private static AttackData CopyAttackData(AttackData source)
        {
            if (source == null) return null;

            return new AttackData
            {
                animKey = source.animKey,
                damage = source.damage,
                poiseDamage = source.poiseDamage,
                breakDamage = source.breakDamage,
                reactionDuration = source.reactionDuration,
                forceReaction = source.forceReaction,
                forceBreakExpose = source.forceBreakExpose,
                canBeInterrupted = source.canBeInterrupted,
                attackKind = source.attackKind,
                reactionType = source.reactionType,
                attacker = source.attacker,
                hitRange = source.hitRange,
                hitAngle = source.hitAngle,
                hitHeightOffset = source.hitHeightOffset,
                hitHeightRange = source.hitHeightRange,
                hitPoint = source.hitPoint,
                hitTarget = source.hitTarget,
                criticalMultiplier = source.criticalMultiplier,
                isCounterAttack = source.isCounterAttack,
                attackDirection = source.attackDirection,
                hitParticleName = source.hitParticleName,
                defenseType = source.defenseType,
                pullForce = source.pullForce,
                airborneForce = source.airborneForce,
                knockbackForce = source.knockbackForce,
                knockbackDrag = source.knockbackDrag,
                grabDuration = source.grabDuration,
                victimForcedAnimKey = source.victimForcedAnimKey,
                hitPhaseIndex = source.hitPhaseIndex,
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
                GameObjectManager.Instance.ShowFX(GetHitFxKey(_currentAttackData), hitPoint);
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

            GameCombatManager.Instance.GameHitStop.ResetActorTimeScale();

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
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(orbTrigger, _currentAttackData.hitPoint);

            switch (kind)
            {
                case AttackKind.ChargeAttack:
                case AttackKind.SkillAttack:
                    CameraManager.Instance.Punch(dir, _punchStrengthSkill, _punchDurationSkill);
                    CameraManager.Instance.StartShake(_shakeKeyHeavy);
                    GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.Critical);
                    break;

                case AttackKind.HeavyAttack:
                case AttackKind.DashAttack:
                case AttackKind.JumpAttack:
                    CameraManager.Instance.Punch(dir, _punchStrengthHeavy, _punchDurationHeavy);
                    CameraManager.Instance.StartShake(_shakeKeyHeavy);
                    GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.Heavy);
                    break;

                default:
                    CameraManager.Instance.Punch(dir, _punchStrengthLight, _punchDurationLight);
                    CameraManager.Instance.StartShake(_shakeKeyLight);
                    GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.Light);
                    break;
            }
        }

        #endregion

        public void SetEnableCollision(bool isCollisionEnable) =>
            _isCollideCollisionEnable = isCollisionEnable;

        public void SetTargetLayerMask(LayerMask targetLayerMask) =>
            _targetLayerMask = targetLayerMask;

        public void SetHitPhaseIndex(int index)
        {
            if (_currentAttackData == null) return;
            var phase = _currentAttackInfoBase != null
                ? _currentAttackInfoBase.GetHitPhase(index)
                : GetHitPhase(_currentResidualHitPhases, index);
            if (phase == null) return;
            _currentAttackData.hitPhaseIndex   = index;
            _currentAttackData.damage          = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f);
            _currentAttackData.poiseDamage     = phase.poiseDamage;
            _currentAttackData.breakDamage     = phase.breakDamage;
            _currentAttackData.reactionDuration = phase.reactionDuration;
            _currentAttackData.forceReaction   = phase.forceReaction;
            _currentAttackData.forceBreakExpose = phase.forceBreakExpose;
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

        private static bool IsResidualAttackState(string stateName)
        {
            return stateName is "Attack"
                or "JumpAttack"
                or "JumpDashAttack"
                or "Charge"
                or "FinishAttack"
                or "SpecialBreakAttack";
        }

        private void ClearResidualAttackContext()
        {
            _currentResidualHitPhases = null;
            _currentFinishTarget = null;
            _currentSpecialBreakTarget = null;
            _currentSpecialBreakDamageByMaxHpRate = 0f;
            _currentSpecialBreakFixedDamage = 0f;
        }

        private static HitPhaseData GetHitPhase(IReadOnlyList<HitPhaseData> phases, int index)
        {
            if (phases == null || phases.Count == 0) return null;
            return phases[Mathf.Clamp(index, 0, phases.Count - 1)];
        }

        private static string GetHitFxKey(AttackData attackData)
        {
            return !string.IsNullOrWhiteSpace(attackData?.hitParticleName)
                ? attackData.hitParticleName
                : FXKeyType.DefaultCombatHit.ToKey();
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

        // ── Peek API (side-effect-free) ───────────────────────────────
        // PlayerAttackState 진입 가능 여부 판정용. CurrentComboIndex / _attackState /
        // _currentAttackData 등 어떠한 상태도 변경하지 않는다.

        /// <summary> 다음 일반 공격이 사용할 AnimKey를 미리 조회 (side effect 없음). </summary>
        public AnimKey PeekNormalAttackAnimKey(bool isCombo)
        {
            if (_attackData == null || _attackData.liteComboAttackList == null
                || _attackData.liteComboAttackList.Count == 0)
                return AnimKey.None;

            int nextIndex = PeekNextComboIndex(AttackState.NormalAttack, isCombo);
            return _attackData.liteComboAttackList[nextIndex]?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 다음 강 공격이 사용할 AnimKey를 미리 조회 (side effect 없음). </summary>
        public AnimKey PeekHeavyAttackAnimKey(bool isCombo)
        {
            if (_attackData == null || _attackData.heavyComboAttackList == null
                || _attackData.heavyComboAttackList.Count == 0)
                return AnimKey.None;

            int nextIndex = PeekNextComboIndex(AttackState.HeavyAttack, isCombo);
            return _attackData.heavyComboAttackList[nextIndex]?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 카운터 공격 AnimKey 조회 (ExecuteCounterAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekCounterAttackAnimKey()
        {
            var source = _attackData?.counterAttack?.baseInfo != null
                ? _attackData.counterAttack
                : (_attackData != null && _attackData.heavyComboAttackList.Count > 0
                    ? _attackData.heavyComboAttackList[0]
                    : null);
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 등장 공격 AnimKey 조회 (ExecuteEntryAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekEntryAttackAnimKey()
        {
            var source = _attackData?.entryAttack?.baseInfo != null
                ? _attackData.entryAttack
                : (_attackData != null && _attackData.liteComboAttackList.Count > 0
                    ? _attackData.liteComboAttackList[0]
                    : null);
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 풀 게이지 교체 특수 공격 AnimKey 조회 (ExecuteSwapSpecialAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekSwapSpecialAttackAnimKey()
        {
            var source = _attackData?.swapSpecialAttack?.baseInfo != null
                ? _attackData.swapSpecialAttack
                : (_attackData != null && _attackData.skillAttackList.Count > 0 && _attackData.skillAttackList[0]?.baseInfo != null
                    ? _attackData.skillAttackList[0]
                    : (_attackData?.entryAttack?.baseInfo != null ? _attackData.entryAttack : null));
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 패리 반격 AnimKey 조회 (ExecuteParryCounterAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekParryCounterAttackAnimKey()
        {
            var source = _attackData?.parryCounterAttack?.baseInfo != null
                ? _attackData.parryCounterAttack
                : (_attackData?.counterAttack?.baseInfo != null
                    ? _attackData.counterAttack
                    : (_attackData != null && _attackData.heavyComboAttackList.Count > 0
                        ? _attackData.heavyComboAttackList[0]
                        : null));
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 스킬 공격 AnimKey 조회. 인덱스가 범위 밖이면 None. </summary>
        public AnimKey PeekSkillAttackAnimKey(int skillIndex)
        {
            if (_attackData == null || _attackData.skillAttackList == null) return AnimKey.None;
            if (skillIndex < 0 || skillIndex >= _attackData.skillAttackList.Count) return AnimKey.None;
            return _attackData.skillAttackList[skillIndex]?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary>
        /// 다음 콤보 인덱스를 미리 계산 (CurrentComboIndex를 변경하지 않음).
        /// 동일 attackState 안에서 isCombo==true 이고 다음 인덱스가 존재할 때만 +1, 그 외에는 0.
        /// 다른 attackState로 전환되는 경우 Execute 시점에서 ResetCombo가 일어나므로 0이 된다.
        /// </summary>
        private int PeekNextComboIndex(AttackState desiredState, bool isCombo)
        {
            if (desiredState != _attackState) return 0;

            int length = desiredState switch
            {
                AttackState.NormalAttack => _attackData.liteComboAttackList.Count,
                AttackState.HeavyAttack  => _attackData.heavyComboAttackList.Count,
                _                        => 0,
            };
            if (length <= 0) return 0;

            bool canContinue = CurrentComboIndex < length - 1;
            int nextIndex = (isCombo && canContinue) ? CurrentComboIndex + 1 : 0;
            return Mathf.Clamp(nextIndex, 0, length - 1);
        }
        // ──────────────────────────────────────────────────────────────

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

        public Transform FindSpecialBreakAttackTarget()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            float searchRange = _specialBreakAttackData != null
                ? _specialBreakAttackData.searchRange
                : _specialBreakAttackSearchRange;
            float searchAngle = _specialBreakAttackData != null
                ? _specialBreakAttackData.searchAngle
                : _specialBreakAttackSearchAngle;
            Collider[] hits = Physics.OverlapSphere(origin, searchRange, _targetLayerMask);

            Transform bestTarget = null;
            float bestDistSq = float.MaxValue;
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                MonsterActor monsterActor = hit.GetComponent<MonsterActor>()
                                            ?? hit.GetComponentInParent<MonsterActor>();
                if (monsterActor == null || monsterActor.BreakGauge == null || !monsterActor.BreakGauge.IsExposed)
                    continue;

                Vector3 dir = monsterActor.transform.position - origin;
                dir.y = 0f;
                if (dir.sqrMagnitude <= 0.001f) continue;
                if (Vector3.Angle(forward, dir) > searchAngle) continue;

                if (lockOnTarget != null &&
                    (monsterActor.transform == lockOnTarget || lockOnTarget.IsChildOf(monsterActor.transform)))
                    return monsterActor.transform;

                float distSq = dir.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = monsterActor.transform;
                }
            }

            return bestTarget;
        }

        public List<IEnemyAIController> GetEnemyAIControllersInRadius(float radius)
        {
            var        result = new List<IEnemyAIController>();
            FillEnemyAIControllersInRadius(radius, result);
            return result;
        }

        public void FillEnemyAIControllersInRadius(float radius, List<IEnemyAIController> result)
        {
            if (result == null)
                return;

            result.Clear();
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                radius,
                _threatOverlapBuffer,
                _targetLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _threatOverlapBuffer[i];
                if (hit == null)
                    continue;

                var monster = hit.GetComponent<MonsterActor>()
                              ?? hit.GetComponentInParent<MonsterActor>();
                var aiController = monster?.AIController;
                if (aiController != null && !result.Contains(aiController))
                    result.Add(aiController);
            }
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
