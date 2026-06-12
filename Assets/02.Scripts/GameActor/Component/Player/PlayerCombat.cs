using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Combat;
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
    public class PlayerCombat : PlayerActorComponent, UPlayGround.Combat.ICombatCollisionExecutor
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

        private struct CharacterComboState
        {
            public int CurrentComboIndex;
            public int NormalComboIndex;   // 약 체인 보존 인덱스(-1 = 미시작)
            public int HeavyComboIndex;    // 강 체인 보존 인덱스(-1 = 미시작)
            public float LastAttackTime;
            public bool CanCombo;
            public AttackState AttackState;
            public AnimKey LastAttackAnimKey;
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
        [SerializeField] private float _lockOnSnapSearchRange = 8f;
        [Tooltip("락온 상태: 호밍 탐색 각도")]
        [SerializeField] private float _lockOnSnapSearchAngle = 60f;

        [Space(4)]
        [Tooltip("자유 전투: 호밍 탐색 반경")]
        [SerializeField] private float _freeSnapSearchRange = 5f;
        [Tooltip("자유 전투: 호밍 탐색 각도")]
        [SerializeField] private float _freeSnapSearchAngle = 80f;
        [Tooltip("비락온 공격 시작 시 주변 적 방향으로 부드럽게 돌기 위한 탐색 각도")]
        [SerializeField] private float _freeAttackFacingSearchAngle = 180f;

        [Header("Motion Warp Settings")]
        [Tooltip("워프 최소 거리. 이 거리 이내의 적에게는 워프 미적용 (씹힘 방지)")]
        [SerializeField] private float _warpMinDistance = 0.3f;
        [Tooltip("워프 최대 거리. 이 거리를 초과한 적에게는 워프 미적용")]
        [SerializeField] private float _warpMaxDistance = 7f;
        [Tooltip("워프 최대 속도. 클램프 후 남은 시간 내 도달 불가 거리면 워프 자체를 미적용")]
        [SerializeField] private float _warpMaxSpeed    = 22f;

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
        // §5.2 등장 변형 — ExecuteEntryAttack/PeekEntryAttackAnimKey가 타깃 상태로 변형을 고르도록 보관.
        private MonsterActor      _pendingEntryTarget;
        private MonsterActor      _currentSpecialBreakTarget;
        private float             _currentSpecialBreakDamageByMaxHpRate;
        private float             _currentSpecialBreakFixedDamage;
        private float             _currentSpecialBreakMinReferenceHealth;
        private AttackState       _attackState         = AttackState.NormalAttack;
        // 약/강 콤보 체인별 보존 인덱스. -1 = 미시작. 약↔강 전환 시 서로 리셋하지 않고 각자 진행도 유지.
        // (ResetCombo에서만 -1 초기화 → 콤보가 실제로 끝날 때만 리셋)
        private int               _normalComboIndex    = -1;
        private int               _heavyComboIndex     = -1;
        private CharacterActorType _comboCharacterType = CharacterActorType.None;
        private readonly Dictionary<CharacterActorType, CharacterComboState> _comboStatesByCharacter = new();
        private PlayerActor       _playerActor;
        private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private readonly Collider[] _threatOverlapBuffer = new Collider[128];
        private readonly Collider[] _hitOverlapBuffer = new Collider[128];
        private readonly List<CombatHit> _detectedHits = new List<CombatHit>(32);
        private PlayerCombatStateTracker _combatStateTracker;
        private CombatActionRunner _actionRunner;

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
        public bool IsInCombat    => _combatStateTracker != null && _combatStateTracker.IsInCombat;
        // P3 3차: 충돌 윈도우의 단일 소유는 CombatActionRunner의 instance. 자체 플래그를 두지 않고 runner를 읽는다.
        public bool IsPossibleCollide => _actionRunner != null && _actionRunner.IsCollisionActive;

        /// <summary>
        /// 캔슬(인터럽트) 허용 구간 여부. 현재 규칙: 히트박스 콜리전이 비활성인 구간
        /// (윈드업/리커버리/멀티히트 간격). 액티브 히트 구간에는 닫힌다.
        /// </summary>
        public bool IsCancelWindowOpen => !IsPossibleCollide;

        /// <summary> 현재 진행 중인 공격의 액션 러너 히트 페이즈 인덱스(0-기준). </summary>
        public int CurrentHitPhaseIndex => _actionRunner != null ? _actionRunner.CurrentPhaseIndex : 0;

        /// <summary>
        /// 현재 공격의 마지막 히트 페이즈 인덱스(0-기준). 페이즈가 없으면 0.
        /// 이동 후딜 캔슬 게이트에서 "마지막 히트 이후(리커버리)" 판정에 사용한다.
        /// </summary>
        public int LastHitPhaseIndex =>
            _currentResidualHitPhases != null && _currentResidualHitPhases.Count > 0
                ? _currentResidualHitPhases.Count - 1
                : 0;

        // ── 가드 내구도 ───────────────────────────────────────────────
        [Header("Guard Settings")]
        [SerializeField] private int   _maxGuardCount   = 3;
        [SerializeField] private float _guardResetDelay = 3f;
        [Tooltip("퍼펙트 가드 후 반격 입력을 받는 창 길이 (초)")]
        [SerializeField] private float _perfectGuardCounterWindow = 1.5f;
        [Tooltip("패리 후 반격 입력을 받는 창 길이 (초)")]
        [SerializeField] private float _parryCounterWindow = 1.5f;
        [Tooltip("퍼펙트 회피 후 회피 카운터 입력을 받는 창 길이 (초)")]
        [SerializeField] private float _dodgeCounterWindow = 1.2f;
        [Tooltip("어시스트 스왑(§4.3) 직후 입장 캐릭터가 적 공격을 패리로 받는 창 길이 (초)")]
        [SerializeField] private float _assistParryWindow = 0.4f;

        private int   _guardHitCount;
        private float _guardEndTime = -999f;
        private float _perfectGuardCounterEndTime = -999f;
        private float _parryCounterEndTime = -999f;
        private float _dodgeCounterEndTime = -999f;
        private float _assistParryWindowEnd = -999f;
        private GameActor _dodgeCounterTarget;

        public bool IsGuardBroken { get; private set; }
        public int  GuardHitCount => _guardHitCount;
        public int  MaxGuardCount => _maxGuardCount;

        /// <summary> 퍼펙트 가드 반격 창이 열려 있는지 여부 </summary>
        public bool IsPerfectGuardCounterAvailable => Time.time <= _perfectGuardCounterEndTime;

        /// <summary> 퍼펙트 가드 성공 시 호출. 반격 입력 창을 연다. </summary>
        public void OpenPerfectGuardCounterWindow(float durationOverride = -1f)
        {
            float duration = durationOverride > 0f ? durationOverride : _perfectGuardCounterWindow;
            _perfectGuardCounterEndTime = Time.time + Mathf.Max(0f, duration);
        }

        /// <summary> 반격 창을 즉시 닫는다 (반격 실행 후 중복 방지) </summary>
        public void ClosePerfectGuardCounterWindow()
            => _perfectGuardCounterEndTime = -999f;

        /// <summary> 패리 반격 창이 열려 있는지 여부 </summary>
        public bool IsParryCounterAvailable => Time.time <= _parryCounterEndTime;

        /// <summary> 패리 성공 시 호출. 반격 입력 창을 연다. </summary>
        public void OpenParryCounterWindow(float durationOverride = -1f)
        {
            float duration = durationOverride > 0f ? durationOverride : _parryCounterWindow;
            _parryCounterEndTime = Time.time + Mathf.Max(0f, duration);
        }

        /// <summary> 패리 반격 창을 즉시 닫는다 </summary>
        public void CloseParryCounterWindow()
            => _parryCounterEndTime = -999f;

        public bool IsDodgeCounterAvailable => Time.time <= _dodgeCounterEndTime;
        public GameActor DodgeCounterTarget => IsDodgeCounterAvailable ? _dodgeCounterTarget : null;

        public void OpenDodgeCounterWindow(AttackData incomingAttack, float durationOverride = -1f)
        {
            float duration = durationOverride > 0f ? durationOverride : _dodgeCounterWindow;
            _dodgeCounterEndTime = Time.time + Mathf.Max(0f, duration);
            _dodgeCounterTarget = incomingAttack?.attacker;
        }

        public bool ConsumeDodgeCounterWindow()
        {
            if (!IsDodgeCounterAvailable)
                return false;

            CloseDodgeCounterWindow();
            return true;
        }

        public void CloseDodgeCounterWindow()
        {
            _dodgeCounterEndTime = -999f;
            _dodgeCounterTarget = null;
        }

        /// <summary> 어시스트 패리(§4.3) 윈도우가 열려 있는지 여부 </summary>
        public bool IsAssistParryWindow => Time.time <= _assistParryWindowEnd;

        /// <summary> 어시스트 패리 윈도우 기본 길이(초). 폴백 타이머 계산에 사용. </summary>
        public float AssistParryWindowDuration => Mathf.Max(0f, _assistParryWindow);

        /// <summary> 어시스트 스왑 발동 시 호출. 입장 캐릭터에 패리 판정 창을 연다. </summary>
        public void OpenAssistParryWindow(float durationOverride = -1f)
        {
            float duration = durationOverride > 0f ? durationOverride : _assistParryWindow;
            _assistParryWindowEnd = Time.time + Mathf.Max(0f, duration);
        }

        /// <summary> 어시스트 패리 창을 즉시 닫는다(패리 성공/폴백 후 중복 방지). </summary>
        public void CloseAssistParryWindow()
            => _assistParryWindowEnd = -999f;

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

            bool canUseCombatData = _currentAttackData != null && playbackSnapshot.Key == _currentAttackData.animKey;
            if (_currentAttackData != null && !canUseCombatData)
                Debug.LogWarning($"[ResidualAttack] Snapshot will be visual-only: animation key mismatch. state={stateName}, playbackKey={playbackSnapshot.Key}, attackAnimKey={_currentAttackData.animKey}, kind={_currentAttackData.attackKind}");

            snapshot = new PlayerResidualAttackSnapshot(
                _playerActor,
                sourceModel,
                sourceModel.characterType,
                canUseCombatData ? CopyAttackData(_currentAttackData) : null,
                canUseCombatData ? _currentAttackInfoBase : null,
                canUseCombatData ? _currentResidualHitPhases : null,
                playbackSnapshot,
                _playerActor.GetAttackTargetLayerMask(),
                sourceModel.transform.position,
                sourceModel.transform.rotation,
                canUseCombatData ? _currentFinishTarget : null,
                canUseCombatData ? _currentSpecialBreakTarget : null,
                canUseCombatData ? _currentSpecialBreakDamageByMaxHpRate : 0f,
                canUseCombatData ? _currentSpecialBreakFixedDamage : 0f,
                canUseCombatData ? _currentSpecialBreakMinReferenceHealth : 0f);

            Debug.Log($"[ResidualAttack] Snapshot created. character={sourceModel.characterType}, state={stateName}, playbackKey={playbackSnapshot.Key}, attackAnimKey={_currentAttackData?.animKey}, kind={_currentAttackData?.attackKind}, visualOnly={!canUseCombatData}, hitRange={_currentAttackData?.hitRange}, hitAngle={_currentAttackData?.hitAngle}, hitPhase={_currentAttackData?.hitPhaseIndex}, hasInfoBase={_currentAttackInfoBase != null}, hitPhaseCount={_currentResidualHitPhases?.Count ?? 0}, finishTarget={_currentFinishTarget != null}, specialBreakTarget={_currentSpecialBreakTarget != null}");
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

            _combatStateTracker = gameObject.GetOrAddComponent<PlayerCombatStateTracker>();
            _combatStateTracker.Configure(
                _combatStateDuration,
                _threatDetectionRange,
                _threatCheckInterval,
                _targetLayerMask);
            _combatStateTracker.OnChangeCombatState -= HandleCombatStateChanged;
            _combatStateTracker.OnChangeCombatState += HandleCombatStateChanged;

            _actionRunner = gameObject.GetOrAddComponent<CombatActionRunner>();
            _actionRunner.SetCollisionExecutor(this);
            OnAttackStarted -= HandleAttackStartedForRunner;
            OnAttackStarted += HandleAttackStartedForRunner;
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

            _combatStateTracker?.Tick();
            UpdateBreakInteractionTarget();
        }

        // ── Break Interaction 게이팅 ──────────────────────────────────
        // '노출된 모든 적'이 아니라 '지금 F를 누르면 실제로 브레이크될 단일 적'에게만
        // 상호작용 UI를 표시한다. 선정 기준은 FindSpecialBreakAttackTarget과 동일 소스를 사용한다.
        private MonsterActor _currentBreakInteractionTarget;
        private float _breakInteractionTickTimer;
        private const float BreakInteractionTickInterval = 0.1f;

        private void UpdateBreakInteractionTarget()
        {
            // 매 프레임 Physics.OverlapSphere를 도는 비용을 막기 위해 100ms 단위로 갱신.
            _breakInteractionTickTimer += Time.deltaTime;
            if (_breakInteractionTickTimer < BreakInteractionTickInterval) return;
            _breakInteractionTickTimer = 0f;

            // 노출된 적이 하나도 없으면 물리 탐색 없이 즉시 정리(상시 비용 0).
            if (MonsterActor.ExposedMonsters.Count == 0)
            {
                SetBreakInteractionTarget(null);
                return;
            }

            // 플레이어가 F를 누를 수 없는 상태(피격·스턴·사망·잡힘)에선 상호작용 UI도 숨긴다.
            string playerState = _playerActor.PlayerController?.CurrentState?.StateName;
            if (playerState is "Hit" or "Stun" or "Death" or "Grabbed" or "Knockdown")
            {
                SetBreakInteractionTarget(null);
                return;
            }

            Transform targetTf = FindSpecialBreakAttackTarget();
            MonsterActor target = targetTf != null
                ? targetTf.GetComponent<MonsterActor>() ?? targetTf.GetComponentInParent<MonsterActor>()
                : null;
            SetBreakInteractionTarget(target);
        }

        private void OnDisable()
        {
            // 컷씬·씬 전환 등으로 비활성화될 때 상호작용 UI가 남지 않도록 정리.
            SetBreakInteractionTarget(null);
        }

        private void SetBreakInteractionTarget(MonsterActor target)
        {
            if (_currentBreakInteractionTarget == target) return;

            // Unity의 != null은 파괴된 오브젝트에 false를 반환하므로 안전.
            if (_currentBreakInteractionTarget != null)
                _currentBreakInteractionTarget.SetBreakInteractionActive(false);

            _currentBreakInteractionTarget = target;

            if (_currentBreakInteractionTarget != null)
                _currentBreakInteractionTarget.SetBreakInteractionActive(true);
        }

        private void HandleCombatStateChanged(bool isInCombat)
            => OnChangeCombatState?.Invoke(isInCombat);

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
            _combatStateTracker?.NotifyCombatEvent();
            // 상태 변화 이벤트는 UpdateCombatState()에서 단일 발화
        }

        /// <summary>
        /// 전투 상태를 즉시 강제 해제한다. (예: 보스 사망, 안전 구역 진입)
        /// </summary>
        public void ForceExitCombat()
        {
            _combatStateTracker?.ForceExitCombat();
            // UpdateCombatState()에서 다음 프레임에 이벤트 발화
        }

        /// <summary>
        /// MotionEvent_MotionWarp.Execute()에서 호출.
        /// warpDuration = 이벤트의 endTime - startTime.
        /// </summary>
        public void BeginMotionWarp(float warpDuration)
        {
            _motionWarp?.BeginMotionWarp(warpDuration);
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.MotionWarpStarted, _currentAttackData?.hitPhaseIndex ?? 0);
        }

        /// <summary>
        /// MotionEvent_MotionWarp.OnCompleteEvent()에서 호출.
        /// </summary>
        public void EndMotionWarp()
        {
            _motionWarp?.EndMotionWarp();
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.MotionWarpEnded, _currentAttackData?.hitPhaseIndex ?? 0);
        }

        #region Execute Attack

        public AttackData ExecuteAttack(bool isCombo)
        {
            ClearResidualAttackContext();
            _attackState      = AttackState.NormalAttack;          // 전환(ResetCombo 호출 제거 — 강 체인 보존)
            CurrentComboIndex = _normalComboIndex;                 // 약 체인 보존 인덱스 복원(-1 = 미시작)
            // stale 콤보 윈도우 닫기: 전환 시 ResetCombo가 하던 CanCombo=false 대체.
            // advance 평가 전에 닫아, 캔슬 경로(isCombo=false)가 이전 공격의 열린 윈도우에 기대지 않게 한다.
            CanCombo          = false;
            CurrentComboIndex = (CurrentComboIndex >= 0 && isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            _normalComboIndex = CurrentComboIndex;                 // 약 체인 저장
            // 태그 상호배타: ResetCombo(반대태그 제거)가 사라졌으므로 직접 반대태그 제거 후 추가.
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Heavy);
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
            _attackState      = AttackState.HeavyAttack;           // 전환(ResetCombo 호출 제거 — 약 체인 보존)
            CurrentComboIndex = _heavyComboIndex;                  // 강 체인 보존 인덱스 복원(-1 = 미시작)
            CanCombo          = false;                             // stale 콤보 윈도우 닫기(ExecuteAttack과 동일)
            CurrentComboIndex = (CurrentComboIndex >= 0 && isCombo && CanContinueCombo()) ? CurrentComboIndex + 1 : 0;
            _heavyComboIndex  = CurrentComboIndex;                 // 강 체인 저장
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Light);
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

        /// <summary> 차지(홀드) 도중 캔슬 가능한 입력 액션 마스크. </summary>
        public PlayerInterruptAction GetChargeInterruptActions() => _attackData.chargeInterruptActions;

        public (string key, ActorSocketType socket, Vector3 offset) GetFullChargeVfxData()
            => (_attackData.fullChargeVfxKey, _attackData.fullChargeVfxSocket, _attackData.fullChargeVfxOffset);

        public AttackData ExecuteChargeAttack(int stageIndex, float chargeRatio)
        {
            ClearResidualAttackContext();
            if (_attackData.chargeStages == null || _attackData.chargeStages.Count == 0) return null;
            _attackState = AttackState.ChargeAttack;
            ResetCombo();

            // 연계 라우트 prefix용 Charge 토큰 기록(예: 차지 → 스킬1). 차지 릴리즈는 별도 상태이므로
            // 여기서 push해야 트래커에 Charge가 남는다.
            _playerActor?.ComboInputTracker.Push(ComboInputToken.Charge);

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
                interruptActions = stage.interruptActions,
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
                reactionData     = phase.reactionProfile?.Resolve(),
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
            // 퍼펙트 가드 반격임을 명시 — 적중 시 몬스터 '가벼운 밀쳐냄' 판정에 사용(SO 작성 의존 제거).
            _currentAttackData.isCounterAttack = true;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// §5.2 등장 변형 — PartyManager가 잡은 등장 타깃을 보관한다.
        /// PlayerActor.ConsumeEntryAttackQueue가 TryStartEntryAttack 직전 호출.
        /// </summary>
        public void SetPendingEntryTarget(MonsterActor target) => _pendingEntryTarget = target;

        public AttackData ExecuteEntryAttack()
        {
            ClearResidualAttackContext();
            var source = SelectEntryAttackInfo();
            _pendingEntryTarget = null; // 1회 소비 후 폐기(스테일 타깃 방지)

            if (source == null) return null;

            var comboState = CaptureComboState();
            _currentAttackData = ConvertToAttackData(source, AttackKind.NormalAttack);
            RestoreComboState(comboState);
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        /// <summary>
        /// §5.2 타깃 적 상태로 등장 변형을 선택한다. 매칭 없으면 기본 entryAttack → 약공 첫 번째 폴백.
        /// (공중 변형 우선, 다음 그로기 변형)
        /// </summary>
        private PlayerAttackInfo SelectEntryAttackInfo()
        {
            if (_attackData == null) return null;

            // 변형은 명시 토글로만 활성(baseInfo는 Unity가 항상 인스턴스화하므로 null 검사로 미설정 구분 불가).
            if (_attackData.useEntryAttackVsAirborne
                && IsEntryTargetAirborne(_pendingEntryTarget)
                && _attackData.entryAttackVsAirborne != null)
                return _attackData.entryAttackVsAirborne;
            if (_attackData.useEntryAttackVsGroggy
                && IsEntryTargetGroggy(_pendingEntryTarget)
                && _attackData.entryAttackVsGroggy != null)
                return _attackData.entryAttackVsGroggy;

            if (_attackData.entryAttack?.baseInfo != null)
                return _attackData.entryAttack;
            return _attackData.liteComboAttackList.Count > 0 ? _attackData.liteComboAttackList[0] : null;
        }

        private static bool IsEntryTargetAirborne(MonsterActor target)
            => target != null && target.ActorController?.CurrentState?.StateName == "Airborne";

        private static bool IsEntryTargetGroggy(MonsterActor target)
        {
            if (target == null) return false;
            string s = target.ActorController?.CurrentState?.StateName;
            if (s is "Stun" or "Knockdown") return true;
            return target.BreakGauge != null && target.BreakGauge.IsExposed;
        }

        public AttackData ExecuteSwapEvadeCounterAttack()
        {
            ClearResidualAttackContext();
            var source = _attackData.swapEvadeCounterAttack?.baseInfo != null
                ? _attackData.swapEvadeCounterAttack
                : (_attackData.entryAttack?.baseInfo != null
                    ? _attackData.entryAttack
                    : (_attackData.liteComboAttackList.Count > 0 ? _attackData.liteComboAttackList[0] : null));

            if (source == null) return null;

            var comboState = CaptureComboState();
            _currentAttackData = ConvertToAttackData(source, AttackKind.NormalAttack);
            RestoreComboState(comboState);
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
            // 패리 반격임을 명시 — 적중 시 몬스터 '가벼운 밀쳐냄' 판정에 사용(SO 작성 의존 제거).
            _currentAttackData.isCounterAttack = true;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        public AttackData ExecuteSkillAttack(int skillIndex)
        {
            ClearResidualAttackContext();
            if (!TryResolveSkill(skillIndex, out PlayerSkillResolveResult resolved)) return null;

            _attackState = AttackState.SkillAttack;
            ResetComboPreserveChains();
            _currentAttackData = ConvertToAttackData(resolved.AttackInfo, AttackKind.SkillAttack);
            _currentAttackData.animKey = resolved.AnimKey;
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        // ── 연계 라우트 (Combo Route) ────────────────────────────────
        /// <summary> 현재 캐릭터 공격 데이터의 연계 라우트 목록(없으면 null). </summary>
        public IReadOnlyList<ComboRouteEntry> ComboRoutes
            => _attackData != null ? _attackData.comboRoutes : null;

        /// <summary>
        /// 라우트 자원(스킬 게이지) 충족 여부. Resolve의 resourceFilter로 전달한다.
        /// 소비하지 않고 가용 여부만 확인한다.
        /// </summary>
        public bool CanAffordRoute(ComboRouteEntry route)
        {
            if (route == null) return false;
            if (route.skillGaugeIndex < 0) return true;
            if (!PlayerSkillGauge.IsValidSkillSlot(route.skillGaugeIndex)) return false;
            var gauge = _playerActor != null ? _playerActor.SkillGauge : null;
            return gauge == null || gauge.CanUseSkill(route.skillGaugeIndex);
        }

        /// <summary>
        /// 연계 라우트로 공격을 실행한다. PlayerAttackState가 Resolve 매칭 후 호출.
        /// 패턴 마지막 토큰으로 AttackKind를 결정하고, 게이지를 소비한다.
        /// 연계는 단발이므로 약/강 분기 메모리는 보존하되 진행 인덱스는 종료한다(설계 §8).
        /// </summary>
        public AttackData ExecuteComboRoute(ComboRouteEntry route)
        {
            if (route == null || route.attackInfo?.baseInfo == null) return null;
            ClearResidualAttackContext();

            // Resolve 단계에서 CanAffordRoute로 가용 확인됨 — 여기서 실제 소비.
            if (route.skillGaugeIndex >= 0)
                _playerActor?.SkillGauge?.ConsumeSkill(route.skillGaugeIndex);

            AttackKind kind = RouteAttackKind(route.LastToken);
            _attackState = kind == AttackKind.HeavyAttack ? AttackState.HeavyAttack
                         : kind == AttackKind.SkillAttack ? AttackState.SkillAttack
                         :                                  AttackState.NormalAttack;

            ResetComboPreserveChains();

            _currentAttackData = ConvertToAttackData(route.attackInfo, kind);
            LastAttackTime = Time.time;
            RefreshCombatState();
            OnAttackStarted?.Invoke(_currentAttackData);
            return _currentAttackData;
        }

        private static AttackKind RouteAttackKind(ComboInputToken lastToken) => lastToken switch
        {
            ComboInputToken.HeavyAttack => AttackKind.HeavyAttack,
            ComboInputToken.Charge      => AttackKind.HeavyAttack,
            ComboInputToken.Skill1      => AttackKind.SkillAttack,
            ComboInputToken.Skill2      => AttackKind.SkillAttack,
            ComboInputToken.Dash        => AttackKind.DashAttack,
            _                           => AttackKind.NormalAttack,
        };

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
                interruptActions = PlayerInterruptAction.None,
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
            _currentSpecialBreakMinReferenceHealth = Mathf.Max(0f, specialBreakAttack.minReferenceHealth);
            _currentAttackData = new AttackData
            {
                animKey = ResolveSpecialBreakMotionKey(specialBreakAttack),
                damage = _currentSpecialBreakFixedDamage,
                poiseDamage = 0f,
                breakDamage = 0f,
                interruptActions = PlayerInterruptAction.None,
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
                interruptActions = attackInfo.interruptActions,
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
                guaranteedReaction     = phase0.guaranteedReaction,
                reactionData           = phase0.reactionProfile?.Resolve(),
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
                interruptActions = source.interruptActions,
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
                guaranteedReaction = source.guaranteedReaction,
                hitPhaseIndex = source.hitPhaseIndex,
                reactionData = source.reactionData,
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

            if (_actionRunner != null
                && _actionRunner.IsCollisionActive
                && _currentAttackData.hitPhaseIndex != _actionRunner.CurrentPhaseIndex)
            {
                SetHitPhaseIndex(_actionRunner.CurrentPhaseIndex);
            }

            Vector3 origin = transform.position + Vector3.up * _currentAttackData.hitHeightOffset;
            var hitShape = new MeleeHitShape(
                transform,
                origin,
                transform.forward,
                _currentAttackData.hitRange,
                _currentAttackData.hitAngle,
                _currentAttackData.hitHeightRange,
                _targetLayerMask);
            CombatHitDetector.DetectMeleeHits(hitShape, _hitOverlapBuffer, _hitTargets, _detectedHits);

            // 첫 번째 히트 정보만 피드백(킬캠 등)에 사용
            bool    hitOccurred   = false;
            Vector3 firstHitPoint = Vector3.zero;
            Vector3 firstHitDir   = Vector3.zero;
            GameObject firstHitTarget = null;

            _currentAttackData.attacker = _playerActor;

            foreach (CombatHit hit in _detectedHits)
            {
                // 공유 AttackData에 퍼-타겟 정보 기록 (TakeDamage 및 이벤트 수신자 참조용)
                _currentAttackData.hitTarget       = hit.HitObject;
                _currentAttackData.hitPoint        = hit.HitPoint;
                _currentAttackData.attackDirection = hit.AttackDirection;

                _hitTargets.Add(hit.Damageable);
                hit.Damageable.TakeDamage(_currentAttackData);
                ShowAttackHitFeedback(_currentAttackData);
                OnAttackHit?.Invoke(_currentAttackData);

                if (!hitOccurred)
                {
                    hitOccurred      = true;
                    firstHitPoint    = hit.HitPoint;
                    firstHitDir      = hit.AttackDirection;
                    firstHitTarget   = hit.HitObject;
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

        private void ShowAttackHitFeedback(AttackData attackData)
        {
            var context = new CombatFeedbackContext(
                attackData,
                attackData.hitPoint,
                attackData.attackDirection,
                attackData.hitTarget,
                attackData.damage,
                CombatFeedbackDispatcher.GetPlayerAttackFloaterStyle(attackData.attackKind),
                GetHitFxKey(attackData));

            CombatFeedbackDispatcher.ShowDamageFloater(context);
            CombatFeedbackDispatcher.ShowHitFx(context);
        }

        private void ApplyHitFeedback()
        {
            // 패리 반격 창이 열려 있으면 Execute(PlayerGuard) 슬로우를 보호한다.
            // foreach 도중 패리가 발동됐거나 실행 순서상 뒤늦게 호출되는 경우 모두 차단.
            if (IsParryCounterAvailable) return;

            CombatFeedbackDispatcher.ApplyPlayerAttackHitFeedback(
                _currentAttackData,
                CreatePlayerAttackHitFeedbackProfile());
        }

        // ── P4: 외부 소스(투사체/AOE) attacker-side 피드백 통일 ──────────────
        // 근접과 동일한 연출 정책(CombatFeedbackDispatcher)을 외부 공격이 재사용한다.
        // _currentAttackData가 아니라 전달된 attackData로 동작해 투사체/AOE의 실제 공격 정보를 반영한다.

        /// <summary>외부 소스의 단일 히트 연출 — 데미지 숫자 + 히트 VFX. 대상마다 호출한다.</summary>
        public void ShowExternalHitFeedback(AttackData attackData)
        {
            if (attackData == null) return;
            ShowAttackHitFeedback(attackData);
        }

        /// <summary>외부 소스의 임팩트 연출 — 히트스톱/카메라/바이탈오브/킬캠. 공격 1회당 호출(AOE는 1회로 제한).</summary>
        public void ApplyExternalAttackImpact(AttackData attackData)
        {
            if (attackData == null) return;
            // 근접 ApplyHitFeedback과 동일하게 패리 반격 슬로우를 보호한다.
            if (IsParryCounterAvailable) return;

            CombatFeedbackDispatcher.ApplyPlayerAttackHitFeedback(
                attackData,
                CreatePlayerAttackHitFeedbackProfile());
        }

        private PlayerAttackHitFeedbackProfile CreatePlayerAttackHitFeedbackProfile()
        {
            return new PlayerAttackHitFeedbackProfile(
                _punchStrengthLight,
                _punchStrengthHeavy,
                _punchStrengthSkill,
                _punchDurationLight,
                _punchDurationHeavy,
                _punchDurationSkill,
                _shakeKeyLight,
                _shakeKeyHeavy);
        }

        #endregion

        public void SetEnableCollision(bool isCollisionEnable)
        {
            // forwarding이 곧 윈도우의 권위 쓰기 — runner instance를 갱신한다(직접 호출자 PlayerChargeState 포함).
            _actionRunner?.HandleTimelineEvent(
                isCollisionEnable ? CombatTimelineEventType.BeginCollision : CombatTimelineEventType.EndCollision,
                _currentAttackData?.hitPhaseIndex ?? 0);
        }

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
            _currentAttackData.reactionData     = phase.reactionProfile?.Resolve();
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.HitPhaseChanged, index);
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
            _currentSpecialBreakMinReferenceHealth = 0f;
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

        public void OpenComboWindow()
        {
            CanCombo = true;
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.ComboWindowOpened, _currentAttackData?.hitPhaseIndex ?? 0);
        }

        public void CloseComboWindow()
        {
            CanCombo = false;
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.ComboWindowClosed, _currentAttackData?.hitPhaseIndex ?? 0);
        }

        private void HandleAttackStartedForRunner(AttackData attackData)
            => _actionRunner?.StartAction(attackData);

        public bool CanUseStoredCombo(bool isHeavyAttack)
        {
            AttackState desiredState = isHeavyAttack ? AttackState.HeavyAttack : AttackState.NormalAttack;
            return CanCombo
                   && _attackState == desiredState
                   && CurrentComboIndex < GetComboLength(desiredState) - 1;
        }

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

        /// <summary> 등장 공격 AnimKey 조회 (ExecuteEntryAttack과 동일한 변형/폴백 체인). </summary>
        public AnimKey PeekEntryAttackAnimKey()
        {
            return SelectEntryAttackInfo()?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 스왑 회피 카운터 AnimKey 조회 (ExecuteSwapEvadeCounterAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekSwapEvadeCounterAttackAnimKey()
        {
            var source = _attackData?.swapEvadeCounterAttack?.baseInfo != null
                ? _attackData.swapEvadeCounterAttack
                : (_attackData?.entryAttack?.baseInfo != null
                    ? _attackData.entryAttack
                    : (_attackData != null && _attackData.liteComboAttackList.Count > 0
                        ? _attackData.liteComboAttackList[0]
                        : null));
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
            return TryResolveSkill(skillIndex, out PlayerSkillResolveResult resolved)
                ? resolved.AnimKey
                : AnimKey.None;
        }

        private bool TryResolveSkill(int skillIndex, out PlayerSkillResolveResult resolved)
        {
            PlayerSkillContext context = CreateSkillContext();
            return PlayerSkillResolver.TryResolve(_attackData, skillIndex, context, out resolved);
        }

        private PlayerSkillContext CreateSkillContext()
        {
            bool isGrounded = _playerActor == null
                              || _playerActor.PlayerController == null
                              || _playerActor.PlayerController.Motor == null
                              || _playerActor.PlayerController.Motor.GroundingStatus.IsStableOnGround;
            var gauge = _playerActor != null ? _playerActor.SkillGauge : null;
            return new PlayerSkillContext(
                isGrounded,
                _playerActor != null ? _playerActor.Tags : null,
                gauge != null ? gauge.CurrentGauge : 0f,
                gauge != null ? gauge.MaxGauge : 0f);
        }

        /// <summary>
        /// 다음 콤보 인덱스를 미리 계산 (인덱스를 변경하지 않음).
        /// 해당 체인의 보존 인덱스(_normalComboIndex/_heavyComboIndex)를 기준으로 Execute와 동일한 규칙으로 예측한다.
        /// 미시작(-1) 또는 isCombo==false 또는 끝까지 진행했으면 0, 그 외 보존 인덱스+1.
        /// (크로스타입 전환 시에도 Execute가 상대 체인을 리셋하지 않으므로 peek도 보존 인덱스를 따라야 일치한다.)
        /// </summary>
        private int PeekNextComboIndex(AttackState desiredState, bool isCombo)
        {
            int length = desiredState switch
            {
                AttackState.NormalAttack => _attackData.liteComboAttackList.Count,
                AttackState.HeavyAttack  => _attackData.heavyComboAttackList.Count,
                _                        => 0,
            };
            if (length <= 0) return 0;

            int baseIndex = desiredState switch
            {
                AttackState.NormalAttack => _normalComboIndex,
                AttackState.HeavyAttack  => _heavyComboIndex,
                _                        => CurrentComboIndex,
            };

            bool canContinue = baseIndex >= 0 && baseIndex < length - 1;
            int nextIndex = (isCombo && canContinue) ? baseIndex + 1 : 0;
            return Mathf.Clamp(nextIndex, 0, length - 1);
        }
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 모델 교체 직전 현재 캐릭터의 조작 콤보 상태를 저장한다.
        /// 잔류 러너의 후속 히트는 이 상태를 갱신하지 않는다.
        /// </summary>
        public void SaveComboState(CharacterActorType characterType)
        {
            if (characterType == CharacterActorType.None) return;

            _comboStatesByCharacter[characterType] = CaptureComboState();
            _comboCharacterType = characterType;
        }

        /// <summary>
        /// 캐릭터 교체 시 공격 데이터 SO를 교체하고, 캐릭터별 콤보 상태를 복원한다.
        /// </summary>
        public void RefreshAttackData(
            PlayerAttackDataSO newData,
            CharacterActorType characterType,
            bool preserveComboState = true,
            float comboStateMaxCarryTime = 1.8f)
        {
            if (_comboCharacterType != CharacterActorType.None && _comboCharacterType != characterType)
                SaveComboState(_comboCharacterType);

            _attackData = newData;
            _comboCharacterType = characterType;

            // 캐릭터별 연계 토큰 간격을 트래커에 반영.
            if (_playerActor != null && newData != null)
                _playerActor.ComboInputTracker.LinkWindow = newData.comboLinkWindow;

            if (preserveComboState && TryRestoreComboState(characterType, comboStateMaxCarryTime))
                return;

            ResetCombo();
        }

        public void RefreshAttackData(PlayerAttackDataSO newData)
        {
            RefreshAttackData(newData, _comboCharacterType, false);
        }

        public void ResetCombo()
        {
            ResetCombo(true, true);
        }

        /// <summary>
        /// 콤보 인덱스/윈도우/태그/입력버퍼는 초기화하되 약·강 체인 분기 메모리(_normalComboIndex/_heavyComboIndex)는 보존한다.
        /// 공격 상태 재진입(크로스타입 캔슬 등)에서 호출 — 진짜 콤보 종료가 아니므로 분기 진행도를 잇기 위함.
        /// </summary>
        public void ResetComboPreserveChains()
        {
            ResetCombo(true, false);
        }

        private void ResetCombo(bool clearInputBuffer, bool resetChains)
        {
            LastAttackTime    = Time.time;
            CurrentComboIndex = 0;
            if (resetChains)
            {
                // 약/강 체인 보존 인덱스 초기화 — 콤보가 실제로 끝나는 경로에서만(피격/타임아웃/Idle 복귀/점프 등).
                _normalComboIndex = -1;
                _heavyComboIndex  = -1;
            }
            CanCombo          = false;
            ApplyComboTags();
            OnComboReset?.Invoke();
            if (clearInputBuffer)
                InputManager.Instance.InputBuffer.Clear();
        }

        private CharacterComboState CaptureComboState()
        {
            return new CharacterComboState
            {
                CurrentComboIndex = CurrentComboIndex,
                NormalComboIndex = _normalComboIndex,
                HeavyComboIndex = _heavyComboIndex,
                LastAttackTime = LastAttackTime,
                CanCombo = CanCombo,
                AttackState = _attackState,
                LastAttackAnimKey = _currentAttackData != null ? _currentAttackData.animKey : AnimKey.None,
            };
        }

        private bool TryRestoreComboState(CharacterActorType characterType, float maxCarryTime)
        {
            if (characterType == CharacterActorType.None)
                return false;

            if (!_comboStatesByCharacter.TryGetValue(characterType, out var state))
                return false;

            bool isExpired = maxCarryTime > 0f && Time.time - state.LastAttackTime > maxCarryTime;
            if (isExpired)
            {
                _comboStatesByCharacter.Remove(characterType);
                return false;
            }

            _attackState = state.AttackState;
            CurrentComboIndex = Mathf.Clamp(state.CurrentComboIndex, 0, Mathf.Max(0, GetComboLength(_attackState) - 1));
            _normalComboIndex = Mathf.Clamp(state.NormalComboIndex, -1, GetComboLength(AttackState.NormalAttack) - 1);
            _heavyComboIndex  = Mathf.Clamp(state.HeavyComboIndex,  -1, GetComboLength(AttackState.HeavyAttack) - 1);
            LastAttackTime = state.LastAttackTime;
            CanCombo = state.CanCombo || (state.LastAttackAnimKey != AnimKey.None && GetComboLength(_attackState) > 1);
            ApplyComboTags();
            return true;
        }

        private void RestoreComboState(CharacterComboState state)
        {
            _attackState = state.AttackState;
            CurrentComboIndex = state.CurrentComboIndex;
            _normalComboIndex = state.NormalComboIndex;
            _heavyComboIndex  = state.HeavyComboIndex;
            LastAttackTime = state.LastAttackTime;
            CanCombo = state.CanCombo;
            ApplyComboTags();
        }

        private int GetComboLength(AttackState attackState)
        {
            if (_attackData == null) return 0;

            return attackState switch
            {
                AttackState.NormalAttack => _attackData.liteComboAttackList?.Count ?? 0,
                AttackState.HeavyAttack  => _attackData.heavyComboAttackList?.Count ?? 0,
                AttackState.JumpAttack   => _attackData.jumpAttackList?.Count ?? 0,
                AttackState.DashAttack   => _attackData.dashAttackList?.Count ?? 0,
                AttackState.SkillAttack  => _attackData.skillAttackList?.Count ?? 0,
                AttackState.ChargeAttack => 0,
                _                        => 0,
            };
        }

        private void ApplyComboTags()
        {
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Light);
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Heavy);

            if (CurrentComboIndex <= 0) return;

            if (_attackState == AttackState.NormalAttack)
                _playerActor.Tags?.AddTag(GameplayTagId.Combo_Light);
            else if (_attackState == AttackState.HeavyAttack)
                _playerActor.Tags?.AddTag(GameplayTagId.Combo_Heavy);
        }
        #endregion

        #region Finish Attack

        public bool IsFinishableTarget(Transform target, bool requirePositionCheck = true)
        {
            MonsterActor monsterActor = ResolveFinishTarget(target);
            if (monsterActor == null || !monsterActor.CanTakeDamage()) return false;
            if (monsterActor.Grade == MonsterActorGrade.Weak) return false;
            if (monsterActor.GetCurrentHealth() > _finishAttackDamageThreshold) return false;

            if (!requirePositionCheck)
                return true;

            Vector3 dir = monsterActor.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > _finishAttackSearchRange * _finishAttackSearchRange) return false;
            if (dir.sqrMagnitude <= 0.001f) return true;

            return Vector3.Angle(transform.forward, dir) <= _finishAttackSearchAngle;
        }

        public Transform FindFinishableTarget()
        {
            Vector3    origin  = transform.position;
            Collider[] hits    = Physics.OverlapSphere(origin, _finishAttackSearchRange, _targetLayerMask);

            Transform bestTarget   = null;
            float     bestDistSq   = float.MaxValue;
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                MonsterActor monsterActor = ResolveFinishTarget(hit.transform);
                if (monsterActor == null || !IsFinishableTarget(monsterActor.transform)) continue;

                Vector3 dir = monsterActor.transform.position - origin;
                dir.y = 0f;

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

        private static MonsterActor ResolveFinishTarget(Transform target)
        {
            if (target == null) return null;
            return target.GetComponent<MonsterActor>()
                   ?? target.GetComponentInParent<MonsterActor>();
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
                if (monsterActor == null
                    || !monsterActor.CanTakeDamage()
                    || monsterActor.BreakGauge == null
                    || !monsterActor.BreakGauge.IsExposed)
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
            return FindAttackSnapTargetInternal(
                hitRange,
                hitAngle,
                GetSnapSearchRange(isLockedOn),
                GetSnapSearchAngle(isLockedOn),
                skipIfAlreadyCovered: true);
        }

        public Transform FindFreeAttackFacingTarget(float hitRange, float hitAngle)
        {
            float searchAngle = Mathf.Max(_freeSnapSearchAngle, _freeAttackFacingSearchAngle);
            return FindAttackSnapTargetInternal(
                hitRange,
                hitAngle,
                _freeSnapSearchRange,
                searchAngle,
                skipIfAlreadyCovered: false);
        }

        private Transform FindAttackSnapTargetInternal(
            float hitRange,
            float hitAngle,
            float searchRange,
            float searchAngle,
            bool skipIfAlreadyCovered)
        {
            Vector3 origin  = transform.position;
            Vector3 forward = transform.forward;

            if (skipIfAlreadyCovered && HasTargetInRange(origin, forward, hitRange, hitAngle))
                return null;

            searchAngle = Mathf.Clamp(searchAngle, 0f, 180f);
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
