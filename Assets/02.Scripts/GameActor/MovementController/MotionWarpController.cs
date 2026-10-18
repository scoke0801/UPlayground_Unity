using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.Debugging;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    public partial class MotionWarpController : MonoBehaviour, IDebugGizmoProvider
    {
        private const float DefaultContactBuffer = 0.08f;
        private const float CloseRangeStopBuffer = 0.12f;
        private const float CloseRangeTangentRetention = 0f;


        // ── 타겟 데이터 모델 (Phase 1 통합 + Phase 4 멀티 키) ──────────────
        // _targets: 현재 컨트롤러가 알고 있는 모든 키 → 타겟 매핑.
        // _activeKey: 현재 워프 윈도우/평가에 사용되는 키.
        // _activeTarget: _targets[_activeKey] 의 캐시 — 기존 코드 경로 호환용.
        public const string DefaultTargetKey = "primary";
        private readonly Dictionary<string, MotionWarpTarget> _targets = new();
        private string _activeKey = DefaultTargetKey;
        private MotionWarpTarget _activeTarget = MotionWarpTarget.None;
        private Vector3 _snapshotPosition;
        private Vector3 _snapshotContactCenter;

        public string ActiveKey => _activeKey;
        public MotionWarpTarget GetTarget(string key)
            => key != null && _targets.TryGetValue(key, out var t) ? t : MotionWarpTarget.None;
        // ──────────────────────────────────────────────────────────────

        // ── 공격 1회 스코프 타겟 잠금 ────────────────────────────────────
        // 한 공격 모션 안에서 워프 타겟이 다른 액터로 갈아타면 회전이 여러 번 튀어 조작감이 어색해진다.
        // BeginTargetLock ~ EndTargetLock 구간에서는 키별로 "처음 잡힌 타겟"만 유지하고,
        // 이후 다른 anchor 로의 SetTarget(모션 타임라인의 MotionEvent_MotionWarp 재결정 등)은 무시한다.
        // 대상 없음과 파괴된 대상도 확정 결과로 유지해 같은 공격에서 재선정하지 않는다.
        // targetKey 가 다른 윈도우(도약-착지 등)는 저작 의도이므로 키 단위로 각각 잠근다.
        private readonly HashSet<string> _lockedTargetKeys = new();
        private bool _targetLockActive;
        // ──────────────────────────────────────────────────────────────

        private bool _isApplicable;
        private bool _translationSuppressed;
        private float _blendWeight;
        private MotionWarpWindowSettings _windowSettings = MotionWarpWindowSettings.Default(0f);
        private bool _hasWindowSettings;
        private string _lastFailureReason = string.Empty;
        private float _lastArrivalError;

        // 애니메이션 타임라인이 있는 액터는 평가된 포즈 시간을 권위값으로 사용한다.
        // Animator가 없는 직접 호출 호스트만 명시적 타이머를 사용한다.
        private float _warpRemainingTime;
        private float _warpTotalDuration;
        private bool _usesAnimationTimeline;
        private float _previousAnimationProgress;
        private float _currentAnimationProgress;
        private bool _pendingEnd;
        private uint _sessionSequence;
        private MotionWarpHandle _activeHandle;
        private MotionWarpEndReason _lastEndReason = MotionWarpEndReason.Interrupted;
        private MotionWarpConstraintFlags _constraintFlags;
        private MotionWarpSourceQuality _rootMotionSourceQuality = MotionWarpSourceQuality.MissingProfile;
        private Vector3 _motorStepStartPosition;
        private Vector3 _requestedMotorDelta;
        private bool _hasMotorStep;
        private bool _hasRequestedMotorDelta;
        // OOR 누적 시간. 임계 초과 시 자동 캔슬.
        private float _outOfRangeAccumulator;
        // ──────────────────────────────────────────────────────────────

        // ── 회전 보간 시작점 (Phase 3) ───────────────────────────────────
        // 워프가 처음 applicable 한 프레임의 회전을 캡처해 곡선 보간의 기점으로 사용.
        private Quaternion _warpStartRotation = Quaternion.identity;
        private bool _warpStartCaptured;
        // ──────────────────────────────────────────────────────────────

        private MotionWarpRootMotionBakeProfile _activeTrajectoryProfile;
        private Quaternion _trajectoryBasisRotation = Quaternion.identity;

        // ── 타겟 속도 추적 (Phase 4 Predictive) ──────────────────────────
        // 활성 타겟의 이전 위치 / 추정 속도. Predictive 정책에서 미래 위치 가산용.
        private Vector3 _targetPreviousPosition;
        private Vector3 _targetVelocity;
        private bool _hasTargetVelocityHistory;
        // ──────────────────────────────────────────────────────────────

        // ── IDamageable 캐시 (Phase 6 perf) ──────────────────────────────
        // anchor 변경 시점에만 GetComponent 재실행. EvaluateVelocity 핫패스 보호.
        private IDamageable _cachedDamageable;
        private Transform _cachedDamageableAnchor;
        // ──────────────────────────────────────────────────────────────

        // ── CapsuleCollider 캐시 ──────────────────────────────────────────
        // 도착 지점·데드존·접근 클램프가 한 프레임에 자/타겟 캡슐을 여러 번 조회한다.
        // IDamageable 캐시와 같은 이유로 anchor 변경 시점에만 재조회한다.
        private CapsuleCollider _selfCapsule;
        private CapsuleCollider _cachedTargetCapsule;
        private Transform _cachedTargetCapsuleAnchor;
        // ──────────────────────────────────────────────────────────────

        // 히트스톱 등 로컬 타임스케일 반영용. 없으면 Time.deltaTime 폴백.
        private GameActor _actor;

        /// <summary>
        /// 워프가 명시적으로 캔슬될 때 발화 (정상 종료에서는 미발화).
        /// 정책: 헛스윙 마무리 — 핸들러 없이도 잔여 루트모션이 자연스럽게 재생되도록 한다.
        /// 핸들러는 디버깅/통계/특수 후속 처리에만 사용.
        /// </summary>
        public event System.Action<WarpCancelReason> OnWarpCancelled;

        private void OnEnable()
        {
            if (Application.isPlaying)
                DebugGizmoBridge.RegisterProvider(this);
        }

        private void OnDisable()
        {
            CompleteWarpSession(MotionWarpEndReason.Disabled);
            DebugGizmoBridge.UnregisterProvider(this);
        }

        public bool HasTarget => _activeTarget.IsValid;
        public Vector3 TargetPosition => GetCurrentTargetPosition();
        public bool IsApplicable => _isApplicable;
        public string LastFailureReason => _lastFailureReason;
        public float LastArrivalError => _lastArrivalError;
        public MotionWarpSourceQuality RootMotionSourceQuality => _rootMotionSourceQuality;
        public MotionWarpConstraintFlags ConstraintFlags => _constraintFlags;
        public MotionWarpEndReason LastEndReason => _lastEndReason;
        public MotionWarpHandle ActiveHandle => _activeHandle;
        public bool IsPendingEnd => _pendingEnd;
        // 블렌드와 KCC 충돌은 예상 끝점 도달을 보장하지 않는다.
        public bool CanGuaranteeArrival => false;
        public bool ShouldOverrideVerticalVelocity =>
            IsMotionWarping
            && _hasWindowSettings
            && _isApplicable
            && !_translationSuppressed
            && _windowSettings.ResolveYPolicy() != WarpYPolicy.IgnoreY;

        public bool  IsMotionWarping   => _activeHandle.IsValid;
        public float WarpRemainingTime => _warpRemainingTime;
        public float WarpDuration      => _warpTotalDuration;

        // 매 프레임 갱신되는 클립 재생 속도 배율. 외부(AttackState)에서 ActorAnimator.Speed 에 적용해 풋슬라이딩을 줄인다.
        // desiredSpeed / baseSpeed 로 새 K를 계산 → 캐시/사전 베이크 불필요, 첫 프레임부터 동작.
        public float WarpPlayRateScale { get; private set; } = 1f;

        private Vector3 GetCurrentTargetPosition()
        {
            if (!_activeTarget.IsValid) return Vector3.zero;
            return _activeTarget.follow ? _activeTarget.ResolveWorldPosition() : _snapshotPosition;
        }

        /// <summary>
        /// 타겟 anchor 가 파괴되었거나 IDamageable 이 사망 상태인지 판정.
        /// IDamageable 은 anchor 변경 시점에만 재조회 (매 프레임 GetComponent 회피).
        /// </summary>
        private bool IsTargetUnreachableLifecycle()
        {
            if (!_activeTarget.IsValid || !_activeTarget.anchor.gameObject.activeInHierarchy) return true;
            return _cachedDamageable != null && !_cachedDamageable.IsAlive();
        }

        private void CacheTargetComponents(Transform anchor)
        {
            _cachedDamageableAnchor = anchor;
            _cachedDamageable = anchor != null ? anchor.GetComponentInParent<IDamageable>() : null;
            _cachedTargetCapsuleAnchor = anchor;
            _cachedTargetCapsule = anchor != null ? anchor.GetComponentInParent<CapsuleCollider>() : null;
        }

        private void Awake()
        {
            // GameActor 는 같은 root GameObject에 있다 (AMC.EnsureReferences 와 동일 가정).
            _actor = GetComponent<GameActor>();
            _selfCapsule = GetComponent<CapsuleCollider>();
        }

        private void Update()
        {
            float dt = _actor != null ? _actor.DeltaTime : Time.deltaTime;
            if (!_usesAnimationTimeline && IsMotionWarping && !_pendingEnd)
            {
                _warpRemainingTime = Mathf.Max(0f, _warpRemainingTime - dt);
                if (_warpRemainingTime <= 0f)
                    _pendingEnd = true;
            }

            UpdateTargetVelocity(dt);
        }

        private void UpdateTargetVelocity(float dt)
        {
            // 워프 비활성이면 속도 추정 불필요. 히스토리 초기화 후 조기 종료.
            if (!IsMotionWarping || !_activeTarget.IsValid || dt <= 0f)
            {
                _hasTargetVelocityHistory = false;
                _targetVelocity = Vector3.zero;
                return;
            }

            Vector3 currentPos = _activeTarget.anchor.position;
            if (_hasTargetVelocityHistory)
            {
                // 단일 프레임 차분. 노이즈가 큰 프로젝트에서는 EMA 로 후속 개선 가능.
                _targetVelocity = (currentPos - _targetPreviousPosition) / dt;
            }
            _targetPreviousPosition = currentPos;
            _hasTargetVelocityHistory = true;
        }

        /// <summary>
        /// MotionEvent_MotionWarp.Execute 에서 호출. warpDuration = endTime - startTime.
        /// </summary>
        public void BeginMotionWarp(float warpDuration)
        {
            if (!_activeHandle.IsValid)
            {
                _activeHandle = CreateNextHandle();
                _warpRemainingTime = Mathf.Max(0f, warpDuration);
                _warpTotalDuration = Mathf.Max(0f, warpDuration);
                _usesAnimationTimeline = false;
            }
            _outOfRangeAccumulator = 0f;
        }

        /// <summary>
        /// MotionEvent_MotionWarp.OnCompleteEvent (정상 종료) 또는 외부에서 조기 종료 시 호출.
        /// 정상 종료 / 조기 종료를 구분할 수 없으므로 캔슬 이벤트는 발화하지 않는다.
        /// 명시적 캔슬은 Cancel(reason) 또는 ClearTarget 경로를 사용할 것.
        /// </summary>
        public void EndMotionWarp()
        {
            CompleteWarpSession(MotionWarpEndReason.Interrupted);
        }

        /// <summary>상태 전환으로 현재 워프 세션을 종료하고 부분 측정값을 폐기한다.</summary>
        public void EndMotionWarpForStateExit()
        {
            CompleteWarpSession(MotionWarpEndReason.StateExited);
        }

        /// <summary>
        /// 명시적 사유로 즉시 캔슬. 워프 중일 때만 OnWarpCancelled 발화.
        /// </summary>
        public void Cancel(WarpCancelReason reason)
        {
            bool wasWarping = IsMotionWarping;
            MotionWarpEndReason endReason = reason == WarpCancelReason.TargetLost
                ? MotionWarpEndReason.TargetLost
                : MotionWarpEndReason.Interrupted;
            CompleteWarpSession(endReason);
            if (wasWarping)
                OnWarpCancelled?.Invoke(reason);
        }

        public MotionWarpHandle BeginWarpWindow(MotionWarpWindowSettings settings)
            => BeginWarpWindow(settings, DefaultTargetKey);

        public MotionWarpHandle BeginWarpWindow(MotionWarpWindowSettings settings, string key)
        {
            if (_activeHandle.IsValid)
                CompleteWarpSession(MotionWarpEndReason.Superseded);

            _activeHandle = CreateNextHandle();
            _pendingEnd = false;
            _constraintFlags = MotionWarpConstraintFlags.None;
            _lastEndReason = MotionWarpEndReason.Interrupted;
            _lastArrivalError = 0f;

            // 키가 바뀌면 활성 타겟 캐시 갱신 + 속도 히스토리 리셋.
            if (!string.IsNullOrEmpty(key) && key != _activeKey)
            {
                _activeKey = key;
                _activeTarget = _targets.TryGetValue(_activeKey, out var t) ? t : MotionWarpTarget.None;
                _hasTargetVelocityHistory = false;
            }

            _windowSettings = settings;
            _windowSettings.translationWeight = Mathf.Clamp01(_windowSettings.translationWeight);
            _windowSettings.rotationWeight = Mathf.Clamp01(_windowSettings.rotationWeight);
            _hasWindowSettings = true;
            // 누적 trajectory는 윈도우 시작 자세 기준이다. 회전 워프가 진행돼도
            // frameBase와 remainingBase가 같은 공간에 남도록 세션 동안 basis를 고정한다.
            _trajectoryBasisRotation = transform.rotation;
            _warpTotalDuration = Mathf.Max(0f, settings.duration);
            _warpRemainingTime = _warpTotalDuration;
            _usesAnimationTimeline = TryReadAnimationProgress(out _);
            _previousAnimationProgress = 0f;
            _currentAnimationProgress = 0f;

            // settings 의 정책을 _activeTarget 에 반영. World 공간 + offset 적용.
            bool useSnapshot = settings.targetPolicy == MotionWarpTargetPolicy.Snapshot;
            _activeTarget.follow = !useSnapshot;
            _activeTarget.offset = settings.targetOffset;
            _activeTarget.space  = WarpTargetSpace.World;
            _targets[_activeKey] = _activeTarget; // dict 와 캐시 동기화

            if (_activeTarget.IsValid && useSnapshot)
            {
                _snapshotPosition = _activeTarget.ResolveWorldPosition();
                _snapshotContactCenter = ResolveTargetCenterWorld(
                    _snapshotPosition,
                    _activeTarget.anchor);
            }

            _isApplicable = false;
            _translationSuppressed = false;
            _lastFailureReason = string.Empty;

            _warpStartCaptured = false;       // 시작 위치/회전을 첫 applicable 프레임에 다시 캡처


            // 실제 리그와 클립에 대응하는 프로필만 사용한다. 누락 시 원본 이동과 독립 회전만 남긴다.
            if (TryResolveBakedRootMotion(
                    settings,
                    out _activeTrajectoryProfile))
            {
                _rootMotionSourceQuality =
                    MotionWarpSourceQuality.BakedTrajectory;
            }
            else
            {
                _activeTrajectoryProfile = null;
                _rootMotionSourceQuality = MotionWarpSourceQuality.MissingProfile;
            }
            BeginEndpointSession();

            // 대상 거리에 따라 모션의 재생 속도를 바꾸지 않는다.
            WarpPlayRateScale = 1f;
            return _activeHandle;
        }

        private MotionWarpHandle CreateNextHandle()
        {
            _sessionSequence++;
            if (_sessionSequence == 0)
                _sessionSequence = 1;
            return new MotionWarpHandle(GetInstanceID(), _sessionSequence);
        }

        /// <summary>현재 세션과 일치하는 완료 요청을 다음 KCC 이동 소비 뒤로 예약한다.</summary>
        public bool RequestEndWarpWindow(MotionWarpHandle handle)
        {
            if (!handle.IsValid || handle != _activeHandle)
                return false;
            _pendingEnd = true;
            return true;
        }

        /// <summary>핸들을 전달할 수 없는 레거시 호출자가 현재 세션의 정상 종료를 예약한다.</summary>
        public void RequestEndWarpWindow()
        {
            if (_activeHandle.IsValid)
                _pendingEnd = true;
        }

        private bool TryResolveBakedRootMotion(in MotionWarpWindowSettings settings,
            out MotionWarpRootMotionBakeProfile trajectoryProfile)
        {
            trajectoryProfile = null;
            ActorAnimator source = MotionSource;
            Animator animator = source?.GetAnimancerComponent()?.Animator;
            if (animator == null || settings.bakedProfiles == null) return false;
            string signature = MotionWarpTrajectory.BuildSourceSignature(source.CurrentMotionSet);
            foreach (MotionWarpRootMotionBakeProfile profile in settings.bakedProfiles)
            {
                if (profile == null || !profile.HasTrajectory
                    || profile.formatVersion != MotionEvent_MotionWarp.CurrentBakeFormatVersion
                    || !profile.Matches(animator.avatar, animator.transform.lossyScale)
                    || !MotionWarpTrajectory.MatchesSource(profile, source.CurrentMotionSet, signature)) continue;
                trajectoryProfile = profile;
                return true;
            }
            return false;
        }

        public void EndWarpWindow()
        {
            CompleteWarpSession(MotionWarpEndReason.Interrupted);
        }

        private void CompleteWarpSession(
            MotionWarpEndReason reason)
        {
            if (!_activeHandle.IsValid && !_hasWindowSettings)
                return;

            _activeTrajectoryProfile = null;
            _trajectoryBasisRotation = Quaternion.identity;

            _hasWindowSettings = false;
            _windowSettings = MotionWarpWindowSettings.Default(0f);
            _isApplicable = false;
            WarpPlayRateScale = 1f;
            _warpRemainingTime = 0f;
            _warpTotalDuration = 0f;
            _usesAnimationTimeline = false;
            _previousAnimationProgress = 0f;
            _currentAnimationProgress = 0f;
            _pendingEnd = false;
            _activeHandle = default;
            _lastEndReason = reason;
            _outOfRangeAccumulator = 0f;
            _hasTargetVelocityHistory = false;
            _hasMotorStep = false;
            _hasRequestedMotorDelta = false;
        }

        /// <summary>KCC 스텝 시작 위치를 기록해 워프 요청량과 실제 이동량을 비교한다.</summary>
        public void BeginMotorStep(Vector3 position)
        {
            if (!IsMotionWarping)
                return;
            _motorStepStartPosition = position;
            _requestedMotorDelta = Vector3.zero;
            _hasMotorStep = true;
            _hasRequestedMotorDelta = false;
            bool active = IsMotionWarping;
            float remaining = _warpRemainingTime;
            float duration = _warpTotalDuration;
            UpdateAnimationProgress(ref active, ref remaining, ref duration);
            OriginalStepDelta = CorrectionStepDelta = ActualMotorStepDelta = Vector3.zero;
            RotationCorrectionDegrees = 0f;
        }

        /// <summary>KCC가 최종 합성한 이동 요청을 현재 스텝 진단값으로 기록한다.</summary>
        public void RecordRequestedMotorVelocity(Vector3 velocity, float deltaTime)
        {
            if (!_hasMotorStep || !IsMotionWarping || deltaTime <= 0f)
                return;
            _requestedMotorDelta = velocity * deltaTime;
            _hasRequestedMotorDelta = true;
        }

        /// <summary>KCC 실제 이동을 반영한 뒤 PendingEnd 세션을 안전하게 종료한다.</summary>
        public void CompleteMotorStep(Vector3 position, Vector3 characterUp)
        {
            if (!_hasMotorStep)
                return;

            ActualMotorStepDelta = position - _motorStepStartPosition;
            if (_hasRequestedMotorDelta)
            {
                Vector3 requestedPlanar = Vector3.ProjectOnPlane(
                    _requestedMotorDelta,
                    characterUp);
                Vector3 appliedPlanar = Vector3.ProjectOnPlane(
                    position - _motorStepStartPosition,
                    characterUp);
                float blockedDistance = (requestedPlanar - appliedPlanar).magnitude;
                if (requestedPlanar.magnitude > 0.001f && blockedDistance > 0.01f)
                {
                    _constraintFlags |= MotionWarpConstraintFlags.KccBlocked;
                    _translationBlocked = true;
                    RejectTranslation("KCC 충돌로 이동 보정 종료");
                }
            }

            _hasMotorStep = false;
            _hasRequestedMotorDelta = false;
            UpdateArrivalError(position, characterUp);
            MotorStepEvaluated?.Invoke(this);

            if (_pendingEnd)
            {
                MotionWarpEndReason reason =
                    (_constraintFlags & MotionWarpConstraintFlags.KccBlocked) != 0
                        ? MotionWarpEndReason.Blocked
                        : MotionWarpEndReason.Completed;
                CompleteWarpSession(
                    reason);
            }
        }

        /// <summary>KCC가 없는 직접 이동 호스트가 마지막 이동 적용 후 PendingEnd를 소비한다.</summary>
        public void CompleteDirectMotionStep(Vector3 position)
        {
            UpdateArrivalError(position, transform.up);
            if (_pendingEnd)
                CompleteWarpSession(MotionWarpEndReason.Completed);
        }

        private void UpdateArrivalError(
            Vector3 currentPosition,
            Vector3 characterUp)
        {
            if (!_activeTarget.IsValid || !_hasWindowSettings)
                return;

            Vector3 targetWorld = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;
            Vector3 targetCenter = _windowSettings.arrivalMode == WarpArrivalMode.ContactShell
                ? ResolveTargetCenterWorld(targetWorld, _activeTarget.anchor)
                : targetWorld;
            Vector3 selfCenter = GetSelfCapsuleCenterPosition(currentPosition);
            Vector3 desiredRoot = ResolveArrivalWorld(
                _windowSettings,
                currentPosition,
                selfCenter,
                targetWorld,
                targetCenter);
            _lastArrivalError = Vector3.ProjectOnPlane(
                desiredRoot - currentPosition,
                characterUp).magnitude;
        }

        /// <summary>
        /// 공격 1회 스코프의 타겟 잠금을 시작한다. 공격 상태 진입 / 콤보 이어치기처럼
        /// "새 공격이 시작되는" 시점에 호출하면, 그 모션 안에서는 키별 첫 타겟만 유지된다.
        /// 재호출은 이전 스코프를 버리고 새로 시작하는 의미다(콤보 각 타격은 다시 타겟팅 허용).
        /// </summary>
        public void BeginTargetLock()
            => BeginTargetLock(Vector3.zero);

        /// <summary>대상 선택과 별개로 이번 공격의 수동 조준 방향을 고정한다.</summary>
        public void BeginTargetLock(Vector3 attackDirection)
        {
            _targetLockActive = true;
            _attackTrackingCommitted = false;
            _attackIntentForward = Vector3.ProjectOnPlane(attackDirection, transform.up).normalized;
            _lockedTargetKeys.Clear();
        }

        /// <summary>
        /// 타겟 잠금 해제. 공격 상태 이탈 시 호출한다.
        /// </summary>
        public void EndTargetLock()
        {
            _targetLockActive = false;
            _attackIntentForward = Vector3.zero;
            _lockedTargetKeys.Clear();
        }

        /// <summary>이번 공격에서 대상 없음까지 포함하여 선정이 끝난 키인지 확인한다.</summary>
        public bool HasLockedTargetSelection(string key)
            => _targetLockActive && _lockedTargetKeys.Contains(string.IsNullOrEmpty(key) ? DefaultTargetKey : key);

        /// <summary>
        /// 잠금 스코프에서 이 키의 타겟 전환을 막아야 하는가.
        /// 첫 선택 이후에는 빈 선택·사망·같은 anchor 재설정까지 막는다.
        /// </summary>
        private bool IsTargetSwitchLocked(string key, Transform newAnchor)
        {
            // 빈 선택과 사망한 대상도 이번 공격의 확정 결과다.
            return _targetLockActive && _lockedTargetKeys.Contains(key);
        }

        public void SetTarget(Transform target, bool useSnapshot = true)
            => SetTarget(DefaultTargetKey, target, useSnapshot);

        public void SetTarget(string key, Transform target, bool useSnapshot = true)
        {
            string useKey = string.IsNullOrEmpty(key) ? DefaultTargetKey : key;
            if (IsTargetSwitchLocked(useKey, target)) return;
            if (_targetLockActive)
                _lockedTargetKeys.Add(useKey);

            var t = new MotionWarpTarget
            {
                anchor = target,
                offset = _hasWindowSettings ? _windowSettings.targetOffset : Vector3.zero,
                space  = WarpTargetSpace.World,
                follow = !useSnapshot,
            };
            _targets[useKey] = t;
            CacheTargetComponents(target);
            // 활성 키와 같을 때만 캐시/스냅샷 갱신.
            if (useKey == _activeKey)
            {
                _activeTarget = t;
                _snapshotPosition = target != null ? t.ResolveWorldPosition() : Vector3.zero;
                _snapshotContactCenter = target != null
                    ? ResolveTargetCenterWorld(_snapshotPosition, target)
                    : Vector3.zero;
                _isApplicable = false;
                _translationSuppressed = false;
                _blendWeight = 0f;
                _lastFailureReason = string.Empty;
                _hasTargetVelocityHistory = false;
            }
        }

        /// <summary>
        /// MotionWarpTarget 직접 주입. AnchorLocal/AnchorForward 같은 공간 옵션을 사용할 때.
        /// </summary>
        public void SetTarget(MotionWarpTarget target)
            => SetTarget(DefaultTargetKey, target);

        public void SetTarget(string key, MotionWarpTarget target)
        {
            string useKey = string.IsNullOrEmpty(key) ? DefaultTargetKey : key;
            if (IsTargetSwitchLocked(useKey, target.anchor)) return;
            if (_targetLockActive)
                _lockedTargetKeys.Add(useKey);

            _targets[useKey] = target;
            CacheTargetComponents(target.anchor);
            if (useKey == _activeKey)
            {
                _activeTarget = target;
                _snapshotPosition = target.IsValid && !target.follow
                    ? target.ResolveWorldPosition()
                    : Vector3.zero;
                _snapshotContactCenter = target.IsValid && !target.follow
                    ? ResolveTargetCenterWorld(
                        _snapshotPosition,
                        target.anchor)
                    : Vector3.zero;
                _isApplicable = false;
                _translationSuppressed = false;
                _blendWeight = 0f;
                _lastFailureReason = string.Empty;
                _hasTargetVelocityHistory = false;
            }
        }

        /// <summary>
        /// 모든 키의 타겟을 제거하고 워프 윈도우/타이머를 종료. (Hit/Death 등 전면 리셋용)
        /// </summary>
        public void ClearTarget()
        {
            bool wasWarping = IsMotionWarping;

            _targets.Clear();
            _lockedTargetKeys.Clear(); // 전면 리셋 — 다음 SetTarget 이 새 첫 타겟이 된다
            _activeTarget = MotionWarpTarget.None;
            _snapshotPosition = Vector3.zero;
            _isApplicable = false;
            _translationSuppressed = false;
            _blendWeight = 0f;
            _outOfRangeAccumulator = 0f;
            _warpStartCaptured = false;
            _hasTargetVelocityHistory = false;

            EndWarpWindow();
            _warpRemainingTime = 0f;
            if (wasWarping)
                OnWarpCancelled?.Invoke(WarpCancelReason.ManualClear);
        }

        /// <summary>
        /// 특정 키의 타겟만 제거. 활성 키와 다르면 워프 흐름은 영향 없음.
        /// </summary>
        public void ClearTarget(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _targets.Remove(key);
            _lockedTargetKeys.Remove(key);
            if (key == _activeKey)
            {
                bool wasWarping = IsMotionWarping;
                _activeTarget = MotionWarpTarget.None;
                _snapshotPosition = Vector3.zero;
                _isApplicable = false;
                _translationSuppressed = false;
                _blendWeight = 0f;
                _hasTargetVelocityHistory = false;
                EndWarpWindow();
                _warpRemainingTime = 0f;
                if (wasWarping)
                    OnWarpCancelled?.Invoke(WarpCancelReason.ManualClear);
            }
        }

        private bool TryReadAnimationProgress(out float progress)
        {
            progress = 0f;
            ActorAnimator animator = MotionSource;
            if (animator == null
                || !animator.IsPlayingMotionSet
                || _windowSettings.windowEndTime
                   <= _windowSettings.windowStartTime)
                return false;

            float animationTime = animator.CurrentEvaluatedMotionSetTime;
            progress = MotionWarpMath.ResolveWindowProgress(
                animationTime,
                _windowSettings.windowStartTime,
                _windowSettings.windowEndTime);
            float remainingAnimationTime = Mathf.Max(
                0f,
                _windowSettings.windowEndTime - animationTime);
            float timelineRate = animator.EffectiveMotionTimelineRate;
            if (timelineRate > 0.0001f)
                _warpRemainingTime = remainingAnimationTime / timelineRate;
            return true;
        }

        private void UpdateAnimationProgress(
            ref bool isWarping,
            ref float remainingTime,
            ref float totalDuration)
        {
            isWarping &= IsMotionWarping;
            if (!isWarping)
                return;

            _previousAnimationProgress = _currentAnimationProgress;
            if (_usesAnimationTimeline)
            {
                if (!TryReadAnimationProgress(out _currentAnimationProgress))
                {
                    if (_pendingEnd)
                    {
                        _currentAnimationProgress = 1f;
                        _warpRemainingTime = 0f;
                    }
                    else
                    {
                        CompleteWarpSession(
                            MotionWarpEndReason.SourceChanged);
                        isWarping = false;
                        return;
                    }
                }
                _currentAnimationProgress = Mathf.Max(
                    _previousAnimationProgress,
                    _currentAnimationProgress);
            }
            else
            {
                _currentAnimationProgress = _warpTotalDuration > 0f
                    ? Mathf.Clamp01(
                        1f - _warpRemainingTime / _warpTotalDuration)
                    : 1f;
            }

            remainingTime = _warpRemainingTime;
            totalDuration = _warpTotalDuration;
        }

        public Vector3 EvaluateVelocity(
            Vector3 rootVelocity,
            Vector3 currentPosition,
            bool isWarping,
            float remainingTime,
            float totalDuration,
            float minDistance,
            float maxDistance,
            float maxSpeed,
            float deltaTime,
            Action cancelWarp = null)
        {
            return EvaluateEndpointVelocity(rootVelocity, currentPosition, deltaTime);
        }

        /// <summary>
        /// 공격 루트모션이 타겟 캡슐 안쪽으로 계속 밀고 들어가면 KCC가 접선 방향으로 투영하며
        /// 타겟 주변을 미끄러지는 현상이 생긴다. 타겟 표면 앞에서 접근 성분만 제한한다.
        /// </summary>
        public Vector3 ClampApproachVelocity(Vector3 velocity, Vector3 currentPosition, float deltaTime)
        {
            if (!_activeTarget.IsValid || deltaTime <= 0f)
                return velocity;

            Vector3 selfPosition = GetSelfCapsuleCenterPosition(currentPosition);
            Vector3 toTarget = GetHorizontalTargetOffset(selfPosition, _activeTarget.anchor);
            float distance = toTarget.magnitude;
            if (distance <= 0.0001f)
                return velocity;

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontalVelocity.sqrMagnitude <= 0.0001f)
                return velocity;

            Vector3 targetDirection = toTarget / distance;
            float approachSpeed = Vector3.Dot(horizontalVelocity, targetDirection);
            if (approachSpeed <= 0f)
                return velocity;

            float desiredDistance = GetCombinedHorizontalRadius(_activeTarget.anchor) + DefaultContactBuffer;
            float allowedApproachSpeed = Mathf.Max(0f, (distance - desiredDistance) / deltaTime);
            if (approachSpeed <= allowedApproachSpeed)
                return velocity;

            _constraintFlags |= MotionWarpConstraintFlags.ApproachClamped;

            Vector3 approach = targetDirection * allowedApproachSpeed;
            Vector3 tangent = horizontalVelocity - targetDirection * approachSpeed;

            if (distance <= desiredDistance + CloseRangeStopBuffer)
                tangent *= CloseRangeTangentRetention;

            Vector3 clampedHorizontal = approach + tangent;
            return new Vector3(clampedHorizontal.x, velocity.y, clampedHorizontal.z);
        }

        private Vector3 GetSelfCapsuleCenterPosition(Vector3 currentPosition)
        {
            if (_selfCapsule == null)
                return currentPosition;

            Vector3 centerOffset =
                _selfCapsule.transform.TransformPoint(_selfCapsule.center) - transform.position;
            return currentPosition + centerOffset;
        }

        private Vector3 ResolveArrivalWorld(
            in MotionWarpWindowSettings settings,
            Vector3 currentPosition,
            Vector3 selfCenter,
            Vector3 targetWorld,
            Vector3 targetCenter)
        {
            if (settings.arrivalMode == WarpArrivalMode.TargetCenter)
                return targetWorld;

            Quaternion targetRotation = _activeTarget.anchor != null
                ? _activeTarget.anchor.rotation
                : Quaternion.identity;
            if (settings.arrivalMode == WarpArrivalMode.AuthoredWarpPoint)
                return targetWorld
                       + targetRotation * settings.localArrivalOffset;

            CapsuleCollider targetCapsule = GetTargetCapsule(
                _activeTarget.anchor);
            return MotionWarpMath.ResolveContactShellDestination(
                currentPosition,
                selfCenter,
                GetHorizontalRadius(_selfCapsule),
                targetCenter,
                targetRotation,
                GetHorizontalRadius(targetCapsule),
                settings.desiredStandOff,
                settings.localArrivalOffset);
        }

        private Vector3 ResolveTargetCenterWorld(
            Vector3 resolvedTargetWorld,
            Transform target)
        {
            if (target == null)
                return resolvedTargetWorld;
            CapsuleCollider targetCapsule = GetTargetCapsule(target);
            if (targetCapsule == null)
                return resolvedTargetWorld;
            Vector3 authoredOffset = resolvedTargetWorld - target.position;
            return targetCapsule.transform.TransformPoint(targetCapsule.center)
                   + authoredOffset;
        }

        /// <summary>
        /// 타겟 캡슐을 anchor 변경 시점에만 재조회한다.
        /// 도착 지점·데드존·접근 클램프가 한 프레임에 반복 호출하므로
        /// 캐시가 없으면 GetComponent 가 핫패스에서 여러 번 돈다.
        /// </summary>
        private CapsuleCollider GetTargetCapsule(Transform target)
        {
            if (target == null)
                return null;
            if (target != _cachedTargetCapsuleAnchor)
            {
                _cachedTargetCapsuleAnchor = target;
                _cachedTargetCapsule = target.GetComponent<CapsuleCollider>()
                                       ?? target.GetComponentInParent<CapsuleCollider>();
            }

            return _cachedTargetCapsule;
        }

        private Vector3 GetHorizontalTargetOffset(Vector3 currentPosition, Transform target)
        {
            Vector3 targetPosition = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;

            CapsuleCollider targetCapsule = GetTargetCapsule(target);
            if (targetCapsule != null)
                targetPosition = targetCapsule.transform.TransformPoint(targetCapsule.center);

            Vector3 toTarget = targetPosition - currentPosition;
            toTarget.y = 0f;
            return toTarget;
        }

        private float GetCombinedHorizontalRadius(Transform target)
        {
            float selfRadius = GetHorizontalRadius(_selfCapsule);
            float targetRadius = GetHorizontalRadius(
                GetTargetCapsule(target));

            return selfRadius + targetRadius;
        }

        private static float GetHorizontalRadius(CapsuleCollider capsule)
        {
            if (capsule == null)
                return 0.35f;

            Vector3 scale = capsule.transform.lossyScale;
            return capsule.direction switch
            {
                0 => capsule.radius * Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)),
                1 => capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)),
                _ => capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y))
            };
        }

        /// <summary>이동 프로필의 수용 여부와 독립적으로 현재 세션의 조준 방향을 반환한다.</summary>
        public bool TryGetFacingDirection(
            Vector3 currentPosition,
            bool isWarping,
            float remainingTime,
            float minDistance,
            float maxDistance,
            float maxSpeed,
            out Vector3 direction)
        {
            direction = Vector3.zero;
            if (!isWarping || !IsMotionWarping || !_rotationApplicable || _trackingCommitted)
                return false;

            if (!_sessionHasTarget)
            {
                direction = _attackIntentForward;
                return direction.sqrMagnitude > .0001f;
            }
            if (!_activeTarget.IsValid || IsTargetUnreachableLifecycle()) return false;

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(0f);

            if (settings.rotationWeight <= 0f)
                return false;

            // 구간의 저장값이 유일한 기준이다. 호출 상태의 레거시 거리 인자로 덮어쓰지 않는다.
            maxDistance = settings.maxDistance;

            Vector3 targetWorld = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;
            Vector3 targetCenter = settings.arrivalMode
                == WarpArrivalMode.ContactShell
                    ? (_activeTarget.follow
                        ? ResolveTargetCenterWorld(
                            targetWorld,
                            _activeTarget.anchor)
                        : _snapshotContactCenter)
                    : targetWorld;
            Vector3 toTarget = targetCenter - currentPosition;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // 도달 가능성(maxSpeed×remainingTime)은 회전 게이트에서 제외 — 이동 경로(EvaluateVelocity)와 일관되게,
            // 사거리(min/max) 안이면 도달 못 하는 거리라도 타겟 방향으로 회전 호밍한다(허공 스윙 방지).
            bool contactShell = settings.arrivalMode
                                == WarpArrivalMode.ContactShell;
            if (dist > maxDistance
                || toTarget.sqrMagnitude <= 0.01f)
                return false;

            direction = toTarget.normalized;
            return true;
        }

        /// <summary>원본 회전 델타에 이번 구간이 소비한 조준 보정만 더한다.</summary>
        public bool TryEvaluateRotation(
            Quaternion currentRotation,
            Vector3 currentPosition,
            bool isWarping,
            float remainingTime,
            float totalDuration,
            float minDistance,
            float maxDistance,
            float maxSpeed,
            out Quaternion newRotation)
        {
            newRotation = currentRotation;
            if (!TryGetFacingDirection(currentPosition, isWarping, remainingTime, minDistance, maxDistance, maxSpeed, out Vector3 dir))
                return false;

            // 첫 applicable 프레임에 startRotation 캡처.
            if (!_warpStartCaptured)
            {
                _warpStartRotation = currentRotation;
                _warpStartCaptured = true;
            }

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(0f);

            float t;
            if (IsMotionWarping
                && _usesAnimationTimeline
                && TryReadAnimationProgress(out float sampledProgress))
            {
                t = sampledProgress;
            }
            else
            {
                t = totalDuration > 0f
                    ? Mathf.Clamp01(1f - remainingTime / totalDuration)
                    : 1f;
            }

            // 곡선 알파 (없으면 EaseOut 폴백).
            float alpha = settings.rotationCurve != null && settings.rotationCurve.length > 0
                ? Mathf.Clamp01(settings.rotationCurve.Evaluate(t))
                : 1f - (1f - t) * (1f - t);
            alpha *= settings.rotationWeight;

            // 매 프레임 현재 회전을 기준으로 제한하면 Live 타겟을 따라 상한을 계속 넘는다.
            Vector3 startForward = _warpStartRotation * Vector3.forward;
            Vector3 intent = _attackIntentForward.sqrMagnitude > .0001f ? _attackIntentForward : startForward;
            float inputYaw = Vector3.SignedAngle(startForward, intent, transform.up);
            float assistLimit = Mathf.Clamp(settings.maxWarpAngle, 0f, 180f);
            float assistYaw = Mathf.Clamp(Vector3.SignedAngle(intent, dir, transform.up), -assistLimit, assistLimit);
            // 수동 방향 전환은 보정 상한에 포함하지 않는다. 적이 더하는 각도만 창의 제한을 따른다.
            float yaw = inputYaw + assistYaw;
            float increment = Mathf.Max(0f, alpha - _rotationAlpha);
            _rotationAlpha = Mathf.Max(_rotationAlpha, alpha);
            RotationCorrectionDegrees = yaw * increment;
            newRotation = (Quaternion.AngleAxis(RotationCorrectionDegrees, transform.up) * currentRotation).normalized;
            return increment > 0f;
        }

        /// <summary>전신 공격의 회전 구간을 타임라인이 소유하는지 확인한다. 공격 시작 때만 조회한다.</summary>
        public static bool HasAuthoredRotation(MotionSet set)
        {
            if (set == null) return false;
            if (HasRotationWindow(set.globalEvents)) return true;
            if (set.motions == null) return false;
            foreach (var motion in set.motions)
                if (HasRotationWindow(motion?.events)) return true;
            return false;
        }

        private static bool HasRotationWindow(List<MotionEventBase> events)
        {
            if (events == null) return false;
            foreach (var evt in events)
                if (evt is MotionEvent_MotionWarp warp && warp.endTime > warp.startTime
                    && warp.ResolveSettings().rotationWeight > 0f) return true;
            return false;
        }

        // ── 디버그 / 모니터링 노출 (Phase 5) ──────────────────────────────
        public float BlendWeight => _blendWeight;
        public float OutOfRangeAccumulator => _outOfRangeAccumulator;
        public Vector3 TargetVelocity => _targetVelocity;
        public bool HasActiveWindow => _hasWindowSettings;
        public bool IsTranslationSuppressed => _translationSuppressed;
        public MotionWarpWindowSettings ActiveWindowSettings => _windowSettings;
        public MotionWarpTarget ActiveTarget => _activeTarget;
        public Vector3 SnapshotPosition => _snapshotPosition;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoTargetColor = new(0.20f, 0.85f, 0.30f);
        [SerializeField] private Color _gizmoMinMaxColor = new(0.85f, 0.70f, 0.10f);
        [SerializeField] private Color _gizmoReachColor  = new(0.30f, 0.55f, 0.95f);
        [SerializeField] private Color _gizmoPredictColor = new(0.95f, 0.30f, 0.65f);

        // 매 프레임 string interpolation 알로케이션 회피용 공유 빌더.
        private static readonly System.Text.StringBuilder _gizmoLabelSb = new(256);

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;
            if (!_activeTarget.IsValid) return;
            if (DebugGizmoBridge.ShouldSuppressLocalGizmos(DebugGizmoCategory.Movement, gameObject, DebugGizmoContentType.MotionWarp))
                return;

            Vector3 selfPos = transform.position;
            Vector3 targetPos = _activeTarget.follow ? _activeTarget.ResolveWorldPosition() : _snapshotPosition;

            // 1) anchor 라인 — 자기 → 활성 타겟 위치.
            Gizmos.color = _gizmoTargetColor;
            Gizmos.DrawLine(selfPos, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.18f);

            // Snapshot 위치(Snapshot 정책일 때 라이브 anchor 위치와 차이를 시각화).
            if (!_activeTarget.follow && _activeTarget.anchor != null)
            {
                Gizmos.color = new Color(_gizmoTargetColor.r, _gizmoTargetColor.g, _gizmoTargetColor.b, 0.4f);
                Gizmos.DrawLine(_activeTarget.anchor.position, _snapshotPosition);
                Gizmos.DrawWireCube(_snapshotPosition, Vector3.one * 0.12f);
            }

            if (!_hasWindowSettings) return;

            // 2) min/max 디스크 — 자기 위치 기준.
            float minD = _windowSettings.minDistance;
            float maxD = _windowSettings.maxDistance;
            Gizmos.color = _gizmoMinMaxColor;
            DrawWireDisc(selfPos, minD);
            DrawWireDisc(selfPos, maxD);

            // 3) 도달 가능 영역 — maxSpeed × 남은 시간.
            float reach = _windowSettings.maxSpeed * Mathf.Max(_warpRemainingTime, 0f);
            if (reach > 0.01f)
            {
                Gizmos.color = _gizmoReachColor;
                DrawWireDisc(selfPos, reach);
            }

            // 4) Predictive 가산점.
            if (_windowSettings.targetPolicy == MotionWarpTargetPolicy.Predictive
                && _hasTargetVelocityHistory
                && _warpRemainingTime > 0f)
            {
                Vector3 predicted = targetPos + _targetVelocity * Mathf.Clamp01(_windowSettings.predictionFactor) * _warpRemainingTime;
                Gizmos.color = _gizmoPredictColor;
                Gizmos.DrawLine(targetPos, predicted);
                Gizmos.DrawWireSphere(predicted, 0.14f);
            }

            // 5) 디버그 텍스트 — 진행도/blend/OOR. StringBuilder 공유로 string interpolation 알로케이션 회피.
            float t = _warpTotalDuration > 0f ? 1f - (_warpRemainingTime / _warpTotalDuration) : 0f;
            _gizmoLabelSb.Clear();
            _gizmoLabelSb.Append("warp: t=").AppendFormat("{0:F2}", t)
                         .Append(" blend=").AppendFormat("{0:F2}", _blendWeight)
                         .Append(" OOR=").AppendFormat("{0:F2}", _outOfRangeAccumulator).Append("s\n")
                         .Append("key=").Append(_activeKey)
                         .Append(" policy=").Append(_windowSettings.targetPolicy)
                         .Append(" mod=").Append(_windowSettings.modifierType).Append('\n');
            if (_isApplicable)
                _gizmoLabelSb.Append("applicable");
            else
                _gizmoLabelSb.Append("not applicable: ").Append(_lastFailureReason);
            UnityEditor.Handles.Label(selfPos + Vector3.up * 2.2f, _gizmoLabelSb.ToString());
        }

        private static void DrawWireDisc(Vector3 center, float radius, int segments = 36)
        {
            if (radius <= 0f) return;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 cur = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }
#endif

        #region Debug Gizmo

        public DebugGizmoCategory Category => DebugGizmoCategory.Movement;
        public DebugGizmoContentType ContentType => DebugGizmoContentType.MotionWarp;
        public UnityEngine.Object Owner => this;
        public bool IsAvailable => this != null && isActiveAndEnabled && _activeTarget.IsValid;

        public void CollectSnapshot(DebugGizmoFrameSnapshot snapshot)
        {
            if (!_activeTarget.IsValid)
                return;

            snapshot.texts.Add(new DebugGizmoTextEntry
            {
                owner = this,
                category = Category,
                position = transform.position,
                text = $"warp active={IsMotionWarping} source={_rootMotionSourceQuality} constraints={_constraintFlags} error={_lastArrivalError:F3}m",
            });
        }

        public void DrawGizmos(DebugGizmoDrawContext context)
        {
            if (!_activeTarget.IsValid)
                return;

            Vector3 selfPos = transform.position;
            Vector3 targetPos = _activeTarget.follow ? _activeTarget.ResolveWorldPosition() : _snapshotPosition;

            Gizmos.color = new Color(0.20f, 0.85f, 0.30f);
            Gizmos.DrawLine(selfPos, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.18f);

            if (!_activeTarget.follow && _activeTarget.anchor != null)
            {
                Gizmos.color = new Color(0.20f, 0.85f, 0.30f, 0.4f);
                Gizmos.DrawLine(_activeTarget.anchor.position, _snapshotPosition);
                Gizmos.DrawWireCube(_snapshotPosition, Vector3.one * 0.12f);
            }

            if (_hasWindowSettings)
            {
                context.DrawWireDisc(selfPos, _windowSettings.minDistance, new Color(0.85f, 0.70f, 0.10f));
                context.DrawWireDisc(selfPos, _windowSettings.maxDistance, new Color(0.85f, 0.70f, 0.10f));

                float reach = _windowSettings.maxSpeed * Mathf.Max(_warpRemainingTime, 0f);
                if (reach > 0.01f)
                    context.DrawWireDisc(selfPos, reach, new Color(0.30f, 0.55f, 0.95f));

                if (_windowSettings.targetPolicy == MotionWarpTargetPolicy.Predictive
                    && _hasTargetVelocityHistory
                    && _warpRemainingTime > 0f)
                {
                    Vector3 predicted = targetPos + _targetVelocity * Mathf.Clamp01(_windowSettings.predictionFactor) * _warpRemainingTime;
                    Gizmos.color = new Color(0.95f, 0.30f, 0.65f);
                    Gizmos.DrawLine(targetPos, predicted);
                    Gizmos.DrawWireSphere(predicted, 0.14f);
                }
            }

            float t = _warpTotalDuration > 0f ? 1f - (_warpRemainingTime / _warpTotalDuration) : 0f;
            var label = context.LabelBuilder;
            label.Clear();
            label.Append("warp: t=").AppendFormat("{0:F2}", t)
                 .Append(" blend=").AppendFormat("{0:F2}", _blendWeight)
                 .Append(" OOR=").AppendFormat("{0:F2}", _outOfRangeAccumulator).Append("s\n")
                 .Append("key=").Append(_activeKey);
            if (_hasWindowSettings)
            {
                label.Append(" policy=").Append(_windowSettings.targetPolicy)
                     .Append(" mod=").Append(_windowSettings.modifierType);
            }
            label.Append('\n').Append(_isApplicable ? "applicable" : $"not applicable: {_lastFailureReason}");
            context.DrawLabel(selfPos + Vector3.up * 2.2f, label.ToString());
        }

        #endregion
    }
}
