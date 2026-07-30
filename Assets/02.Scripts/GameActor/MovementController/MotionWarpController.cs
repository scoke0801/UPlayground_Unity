using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround;
using UPlayGround.Debugging;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    public class MotionWarpController : MonoBehaviour, IDebugGizmoProvider
    {
        private const float DefaultContactBuffer = 0.08f;
        private const float CloseRangeStopBuffer = 0.12f;
        private const float CloseRangeTangentRetention = 0f;

        // OOR 누적 시간이 이 값을 초과하면 자동 캔슬.
        private const float OutOfRangeCancelThreshold = 0.1f;

        // ── 타겟 데이터 모델 (Phase 1 통합 + Phase 4 멀티 키) ──────────────
        // _targets: 현재 컨트롤러가 알고 있는 모든 키 → 타겟 매핑.
        // _activeKey: 현재 워프 윈도우/평가에 사용되는 키.
        // _activeTarget: _targets[_activeKey] 의 캐시 — 기존 코드 경로 호환용.
        public const string DefaultTargetKey = "primary";
        private readonly Dictionary<string, MotionWarpTarget> _targets = new();
        private string _activeKey = DefaultTargetKey;
        private MotionWarpTarget _activeTarget = MotionWarpTarget.None;
        private Vector3 _snapshotPosition;
        private Vector3 _snapshotTargetCenter;
        private Vector3 _warpStartPosition;
        private Vector3 _warpStartForward;
        private bool _hasObstacleLimit;
        private Vector3 _obstacleLimitedArrival;

        public string ActiveKey => _activeKey;
        public MotionWarpTarget GetTarget(string key)
            => key != null && _targets.TryGetValue(key, out var t) ? t : MotionWarpTarget.None;
        // ──────────────────────────────────────────────────────────────

        // ── 공격 1회 스코프 타겟 잠금 ────────────────────────────────────
        // 한 공격 모션 안에서 워프 타겟이 다른 액터로 갈아타면 회전이 여러 번 튀어 조작감이 어색해진다.
        // BeginTargetLock ~ EndTargetLock 구간에서는 키별로 "처음 잡힌 타겟"만 유지하고,
        // 이후 다른 anchor 로의 SetTarget(모션 타임라인의 MotionEvent_MotionWarp 재결정 등)은 무시한다.
        // 잠긴 타겟이 파괴/무효화되면 잠금이 풀려 다음 SetTarget 이 다시 채운다.
        // targetKey 가 다른 윈도우(도약-착지 등)는 저작 의도이므로 키 단위로 각각 잠근다.
        private readonly HashSet<string> _lockedTargetKeys = new();
        private bool _targetLockActive;
        // ──────────────────────────────────────────────────────────────

        private bool _feasibilityChecked;
        private bool _isApplicable;
        private float _blendWeight;
        private MotionWarpWindowSettings _windowSettings = MotionWarpWindowSettings.Default(0f);
        private bool _hasWindowSettings;
        private string _lastFailureReason = string.Empty;
        private float _lastArrivalError;

        // ── 워프 타이머 (Combat 에서 이전) ──────────────────────────────
        // MotionEvent_MotionWarp.Execute 시 BeginMotionWarp 로 주입되고,
        // 매 프레임 deltaTime 만큼 소모하며 0 이하가 되면 워프 비활성.
        private float _warpRemainingTime;
        private float _warpAuthoredRemainingTime;
        private float _warpTotalDuration;
        private float _warpEvaluationRate = 1f;
        private float _lastWarpEvaluationTime;
        private bool _hasWarpEvaluationTime;
        // OOR 누적 시간. 임계 초과 시 자동 캔슬.
        private float _outOfRangeAccumulator;
        // ──────────────────────────────────────────────────────────────

        // ── 회전 보간 시작점 (Phase 3) ───────────────────────────────────
        // 워프가 처음 applicable 한 프레임의 회전을 캡처해 곡선 보간의 기점으로 사용.
        private Quaternion _warpStartRotation = Quaternion.identity;
        private bool _warpStartCaptured;
        // ──────────────────────────────────────────────────────────────

        // ── delta-warp 모델 ─────────────────────────────────────────────
        // 윈도우 동안 누적되는 "순수 애니메이션 루트 변위" — 매 프레임 raw DeltaPosition 을
        // 그 프레임의 액터 회전 역변환으로 애니메이션 로컬프레임에 투영해 합산(회전 불변).
        // 스티어링/월드 facing 과 무관한 베이크 데이터와 런타임 측정에 사용한다.
        private Vector3 _accumRootLocal;
        private float   _accumRootPath;   // 누적 경로 길이(스칼라)
        private RootMotionTotal _activeTotal;
        private bool    _hasActiveTotal;  // true면 에디터 베이크 기반 정확 delta-warp.
        private Vector3 _accumulatedCorrection;
        private float _correctionBudget;
        private bool _correctionBudgetInitialized;
        // ──────────────────────────────────────────────────────────────

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

        // 히트스톱 등 로컬 타임스케일 반영용. 없으면 Time.deltaTime 폴백.
        private GameActor _actor;

        // 윈도우의 "순수 애니메이션 루트 변위" 총량 — facing-불변 고유 로컬프레임 기준.
        // (rawHoriz = R·localRoot 를 매 프레임 Inverse(R) 로 투영·합산하므로 액터 회전 R 이 해석적으로
        //  소거된다. 따라서 시작 프레임 스냅샷이 아니라 클립 고유의 로컬 변위. 액터 스티어/회전과 무관.)
        private readonly struct RootMotionTotal
        {
            // 클립 고유 수평 총 변위(facing-불변). normalized 만 방향 추정에 사용 — 굽은 루트 클립이면
            // 방향이 "평균 로컬 헤딩" 으로 무뎌지나 PathLen 은 항상 정확.
            public readonly Vector3 LocalTotal;
            public readonly float   PathLen;    // 총 경로 길이(스칼라)
            public RootMotionTotal(Vector3 localTotal, float pathLen)
            {
                LocalTotal = localTotal;
                PathLen    = pathLen;
            }
            public bool IsValid => PathLen > 0.0001f;
        }
        // ──────────────────────────────────────────────────────────────

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
            DebugGizmoBridge.UnregisterProvider(this);
        }

        public bool HasTarget => _activeTarget.IsValid;
        public Vector3 TargetPosition => GetCurrentTargetPosition();
        public bool IsApplicable => _isApplicable;
        public string LastFailureReason => _lastFailureReason;
        public float LastArrivalError => _lastArrivalError;

        public bool  IsMotionWarping   => _warpRemainingTime > 0f;
        public float WarpRemainingTime => _warpRemainingTime;
        public float WarpDuration      => _warpTotalDuration;

        // 매 프레임 갱신되는 클립 재생 속도 배율. 외부(AttackState)에서 ActorAnimator.Speed 에 적용해 풋슬라이딩을 줄인다.
        // 피드백 루프 차단: DeltaPosition 에 이미 반영된 이전 K(_prevWarpK)로 역산해 Speed=1 기준 속도를 도출한 뒤
        // desiredSpeed / baseSpeed 로 새 K를 계산 → 캐시/사전 베이크 불필요, 첫 프레임부터 동작.
        public float WarpPlayRateScale { get; private set; } = 1f;
        private float _prevWarpK = 1f;
        private Vector3 GetCurrentTargetPosition()
        {
            if (!_activeTarget.IsValid) return Vector3.zero;
            return _activeTarget.follow ? ResolveTargetCenter() : _snapshotTargetCenter;
        }

        private Vector3 ResolveTargetCenter()
        {
            if (!_activeTarget.IsValid)
                return Vector3.zero;

            if (_hasWindowSettings
                && _windowSettings.arrivalMode != WarpArrivalMode.TargetCenter)
            {
                CapsuleCollider targetCapsule = GetTargetCapsule(_activeTarget.anchor);
                Vector3 center = targetCapsule != null
                    ? targetCapsule.transform.TransformPoint(targetCapsule.center)
                    : _activeTarget.anchor.position;
                return center + _windowSettings.targetOffset;
            }

            return _activeTarget.ResolveWorldPosition()
                 + (_hasWindowSettings ? _windowSettings.targetOffset : Vector3.zero);
        }

        private Vector3 ResolveArrivalPosition(Vector3 attackerStart, Vector3 targetCenter)
        {
            if (!_hasWindowSettings
                || _windowSettings.arrivalMode == WarpArrivalMode.TargetCenter)
            {
                return targetCenter;
            }

            if (_windowSettings.arrivalMode == WarpArrivalMode.AuthoredWarpPoint)
                return ResolveAuthoredWarpPoint(attackerStart, targetCenter);

            float selfRadius = GetSelfHorizontalRadius();
            float targetRadius = GetHorizontalRadius(GetTargetCapsule(_activeTarget.anchor));
            Quaternion targetRotation = _activeTarget.anchor != null
                ? _activeTarget.anchor.rotation
                : Quaternion.identity;
            return MotionWarpArrivalUtility.ResolveContactShell(
                attackerStart,
                targetCenter,
                selfRadius,
                targetRadius,
                _windowSettings.desiredStandOff,
                _windowSettings.localArrivalOffset,
                targetRotation);
        }

        private Vector3 ResolveAuthoredWarpPoint(Vector3 attackerStart, Vector3 targetCenter)
        {
            Transform targetAnchor = _activeTarget.anchor;
            Quaternion targetRotation = targetAnchor != null ? targetAnchor.rotation : Quaternion.identity;
            Vector3 approach = targetCenter - attackerStart;
            approach.y = 0f;
            Vector3 approachDirection = approach.sqrMagnitude > 0.000001f
                ? approach.normalized
                : transform.forward;

            Transform targetPointTransform = null;
            if (targetAnchor != null && !string.IsNullOrWhiteSpace(_windowSettings.targetTransformPath))
                targetPointTransform = targetAnchor.Find(_windowSettings.targetTransformPath);

            Vector3 targetPoint;
            if (targetPointTransform != null)
            {
                targetPoint = targetPointTransform.TransformPoint(_windowSettings.targetPointOffset);
            }
            else
            {
                float targetRadius = GetHorizontalRadius(GetTargetCapsule(targetAnchor));
                targetPoint = targetCenter
                            - approachDirection * (targetRadius + Mathf.Max(0f, _windowSettings.desiredStandOff))
                            + targetRotation * _windowSettings.targetPointOffset;
            }

            Vector3 sourcePointLocal = _windowSettings.warpPointProvider switch
            {
                WarpPointProvider.StaticTransform => _windowSettings.authoredWarpPointLocal,
                WarpPointProvider.Bone => ResolveBoneWarpPointLocal(),
                _ => Vector3.zero,
            };

            return MotionWarpArrivalUtility.ResolveAuthoredWarpPoint(
                targetPoint,
                sourcePointLocal,
                transform.rotation,
                _windowSettings.localArrivalOffset,
                targetRotation);
        }

        private Vector3 ResolveBoneWarpPointLocal()
        {
            Animator animator = _actor != null && _actor.Animator != null
                ? _actor.Animator.GetAnimator
                : null;
            Transform bone = animator != null && animator.isHuman
                ? animator.GetBoneTransform(_windowSettings.warpPointBone)
                : null;
            if (bone == null)
                return _windowSettings.authoredWarpPointLocal;

            Vector3 worldPoint = bone.TransformPoint(_windowSettings.warpPointBoneOffset);
            return transform.InverseTransformPoint(worldPoint);
        }

        private Vector3 LimitArrivalByObstacle(Vector3 currentPosition, Vector3 desiredArrival, out bool limited)
        {
            limited = false;
            Vector3 movement = desiredArrival - currentPosition;
            movement.y = 0f;
            float distance = movement.magnitude;
            if (distance <= 0.0001f)
                return desiredArrival;

            ActorMovementController movementController = GetComponent<ActorMovementController>();
            CapsuleCollider capsule = movementController != null && movementController.Motor != null
                ? movementController.Motor.Capsule
                : GetComponent<CapsuleCollider>();
            if (capsule == null)
                return desiredArrival;

            Vector3 up = movementController != null && movementController.Motor != null
                ? movementController.Motor.CharacterUp
                : transform.up;
            float radius = GetHorizontalRadius(capsule);
            float height = capsule.height * Mathf.Abs(capsule.transform.lossyScale.y);
            Vector3 bottom = currentPosition + up * radius;
            Vector3 top = currentPosition + up * Mathf.Max(radius, height - radius);
            RaycastHit[] hits = Physics.CapsuleCastAll(
                bottom,
                top,
                radius,
                movement / distance,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float nearest = distance;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform))
                    continue;
                if (_activeTarget.anchor != null
                    && (hit.transform.IsChildOf(_activeTarget.anchor)
                        || _activeTarget.anchor.IsChildOf(hit.transform)))
                    continue;
                nearest = Mathf.Min(nearest, hits[i].distance);
            }

            if (nearest >= distance)
                return desiredArrival;

            limited = true;
            return currentPosition + movement.normalized * Mathf.Max(0f, nearest - DefaultContactBuffer);
        }

        private static CapsuleCollider GetTargetCapsule(Transform target)
            => target == null
                ? null
                : target.GetComponent<CapsuleCollider>() ?? target.GetComponentInParent<CapsuleCollider>();

        private float GetSelfHorizontalRadius()
        {
            ActorMovementController movementController = GetComponent<ActorMovementController>();
            CapsuleCollider capsule = movementController != null && movementController.Motor != null
                ? movementController.Motor.Capsule
                : GetComponent<CapsuleCollider>();
            return GetHorizontalRadius(capsule);
        }

        /// <summary>
        /// 타겟 anchor 가 파괴되었거나 IDamageable 이 사망 상태인지 판정.
        /// IDamageable 은 anchor 변경 시점에만 재조회 (매 프레임 GetComponent 회피).
        /// </summary>
        private bool IsTargetUnreachableLifecycle()
        {
            if (!_activeTarget.IsValid) return true; // anchor null
            Transform anchor = _activeTarget.anchor;
            if (anchor != _cachedDamageableAnchor)
            {
                _cachedDamageableAnchor = anchor;
                _cachedDamageable = anchor.GetComponent<IDamageable>()
                                 ?? anchor.GetComponentInParent<IDamageable>();
            }
            return _cachedDamageable != null && !_cachedDamageable.IsAlive();
        }

        private void Awake()
        {
            // GameActor 는 같은 root GameObject에 있다 (AMC.EnsureReferences 와 동일 가정).
            _actor = GetComponent<GameActor>();
        }

        private void Update()
        {
            // 히트스톱 로컬 타임스케일 반영. _actor 미존재 시(스탠드얼론 테스트 등) Time.deltaTime 폴백.
            float dt = _actor != null ? _actor.DeltaTime : Time.deltaTime;
            if (_warpRemainingTime > 0f)
            {
                float warpClockDelta = dt;
                if (_actor != null
                    && _actor.Animator != null
                    && _actor.Animator.IsPlayingMotionSet)
                {
                    float evaluatedTime = _actor.Animator.EvaluatedMotionTime;
                    warpClockDelta = _hasWarpEvaluationTime
                        ? MotionWarpArrivalUtility.ResolveForwardTimeDelta(
                            _lastWarpEvaluationTime,
                            evaluatedTime)
                        : 0f;
                    _lastWarpEvaluationTime = evaluatedTime;
                    _hasWarpEvaluationTime = true;

                    if (dt > 0.000001f && warpClockDelta > 0f)
                        _warpEvaluationRate = warpClockDelta / dt;
                }
                _warpAuthoredRemainingTime = Mathf.Max(
                    0f,
                    _warpAuthoredRemainingTime - warpClockDelta);
                _warpRemainingTime =
                    MotionWarpArrivalUtility.ResolvePhysicalRemainingTime(
                        _warpAuthoredRemainingTime,
                        _warpEvaluationRate);
            }

            UpdateTargetVelocity(dt);
        }

        private void UpdateTargetVelocity(float dt)
        {
            // 워프 비활성이면 속도 추정 불필요. 히스토리 초기화 후 조기 종료.
            if (_warpRemainingTime <= 0f || !_activeTarget.IsValid || dt <= 0f)
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
            _warpRemainingTime = warpDuration;
            _warpAuthoredRemainingTime = warpDuration;
            _warpTotalDuration = warpDuration;
            _warpEvaluationRate = 1f;
            _outOfRangeAccumulator = 0f;
            _warpStartCaptured = false;
            _lastWarpEvaluationTime =
                _actor != null && _actor.Animator != null
                    ? _actor.Animator.EvaluatedMotionTime
                    : 0f;
            _hasWarpEvaluationTime =
                _actor != null && _actor.Animator != null;
            // 새 워프 윈도우 시작 — 속도 히스토리는 다음 프레임부터 다시 누적.
            _hasTargetVelocityHistory = false;
        }

        /// <summary>
        /// MotionEvent_MotionWarp.OnCompleteEvent (정상 종료) 또는 외부에서 조기 종료 시 호출.
        /// 정상 종료 / 조기 종료를 구분할 수 없으므로 캔슬 이벤트는 발화하지 않는다.
        /// 명시적 캔슬은 Cancel(reason) 또는 ClearTarget 경로를 사용할 것.
        /// </summary>
        public void EndMotionWarp()
        {
            _warpRemainingTime = 0f;
            _warpAuthoredRemainingTime = 0f;
            _outOfRangeAccumulator = 0f;
            _hasWarpEvaluationTime = false;
            // 조기 종료일 수 있으므로 부분 측정 저장을 막는다. 자연 완료 경로는
            // EndWarpWindow 가 이 호출보다 먼저 실행되어 이미 캐시 저장을 마친 뒤다.
        }

        /// <summary>
        /// 명시적 사유로 즉시 캔슬. 워프 중일 때만 OnWarpCancelled 발화.
        /// </summary>
        public void Cancel(WarpCancelReason reason)
        {
            bool wasWarping = _warpRemainingTime > 0f;
            _warpRemainingTime = 0f;
            _warpAuthoredRemainingTime = 0f;
            _outOfRangeAccumulator = 0f;
            _hasWarpEvaluationTime = false;
            if (wasWarping)
                OnWarpCancelled?.Invoke(reason);
        }

        public void BeginWarpWindow(MotionWarpWindowSettings settings)
            => BeginWarpWindow(settings, DefaultTargetKey);

        public void BeginWarpWindow(MotionWarpWindowSettings settings, string key)
        {
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

            // 정책은 이벤트가 소유하지만 타겟이 가진 offset 공간은 보존한다.
            bool useSnapshot = settings.targetPolicy == MotionWarpTargetPolicy.Snapshot;
            _activeTarget.follow = !useSnapshot;
            _targets[_activeKey] = _activeTarget; // dict 와 캐시 동기화

            _warpStartPosition = transform.position;
            _warpStartForward = transform.forward;
            _hasObstacleLimit = false;
            if (_activeTarget.IsValid)
            {
                _snapshotTargetCenter = ResolveTargetCenter();
                _snapshotPosition = ResolveArrivalPosition(_warpStartPosition, _snapshotTargetCenter);
                _obstacleLimitedArrival = LimitArrivalByObstacle(
                    _warpStartPosition,
                    _snapshotPosition,
                    out _hasObstacleLimit);
                if (_hasObstacleLimit)
                    _snapshotPosition = _obstacleLimitedArrival;
            }

            _feasibilityChecked = false;
            _isApplicable = false;
            _lastFailureReason = string.Empty;

            // ── delta-warp 윈도우 초기화: 캐시 키 조립 + 히트 조회 + 누적기 리셋 ──
            _warpStartCaptured = false;       // 시작 위치/회전을 첫 applicable 프레임에 다시 캡처
            _accumRootLocal = Vector3.zero;
            _accumRootPath  = 0f;
            _accumulatedCorrection = Vector3.zero;
            _correctionBudget = 0f;
            _correctionBudgetInitialized = false;

            // 1순위: 에디터 베이크 시드. 콤보/스킬처럼 캐시가 못 데워지는 경우에도 첫 시전부터 정확 모드.
            //         베이크는 실제 액터 프리팹의 DeltaPosition 누적이라 런타임과 동일 정의·스케일(변환 불필요).
            if (settings.bakedValid && settings.bakedPathLen > 0.0001f)
            {
                _activeTotal = new RootMotionTotal(settings.bakedLocalTotal, settings.bakedPathLen);
                _hasActiveTotal = true;
            }
            // 베이크가 없으면 항상 결정적 원본 예상 도착 폴백을 사용한다.
            // 세션 첫 실행 후 캐시로 알고리즘이 바뀌면 동일 공격의 체감이 달라지므로 지연 캐시는 사용하지 않는다.
            else
            {
                _hasActiveTotal = false;
            }

            // K는 EvaluateVelocity 에서 매 프레임 갱신 — 여기서는 초기화만.
            WarpPlayRateScale = 1f;
            _prevWarpK = 1f;
        }

        public void EndWarpWindow()
        {
            _hasActiveTotal = false;
            _accumRootLocal = Vector3.zero;
            _accumRootPath = 0f;
            _accumulatedCorrection = Vector3.zero;
            _correctionBudget = 0f;
            _correctionBudgetInitialized = false;

            _hasWindowSettings = false;
            _windowSettings = MotionWarpWindowSettings.Default(0f);
            _feasibilityChecked = false;
            _isApplicable = false;
            _lastFailureReason = string.Empty;
            WarpPlayRateScale = 1f;
            _prevWarpK = 1f;
            _hasObstacleLimit = false;
            _hasWarpEvaluationTime = false;
        }

        /// <summary>
        /// 공격 1회 스코프의 타겟 잠금을 시작한다. 공격 상태 진입 / 콤보 이어치기처럼
        /// "새 공격이 시작되는" 시점에 호출하면, 그 모션 안에서는 키별 첫 타겟만 유지된다.
        /// 재호출은 이전 스코프를 버리고 새로 시작하는 의미다(콤보 각 타격은 다시 타겟팅 허용).
        /// </summary>
        public void BeginTargetLock()
        {
            _targetLockActive = true;
            _lockedTargetKeys.Clear();
        }

        /// <summary>
        /// 타겟 잠금 해제. 공격 상태 이탈 시 호출한다.
        /// </summary>
        public void EndTargetLock()
        {
            _targetLockActive = false;
            _lockedTargetKeys.Clear();
        }

        /// <summary>
        /// 잠금 스코프에서 이 키의 타겟 전환을 막아야 하는가.
        /// 아직 잠기지 않았거나(첫 타겟), 잠긴 타겟이 무효/파괴됐거나, 같은 anchor 재설정이면 허용.
        /// </summary>
        private bool IsTargetSwitchLocked(string key, Transform newAnchor)
        {
            if (!_targetLockActive || newAnchor == null) return false;
            if (!_lockedTargetKeys.Contains(key)) return false;

            MotionWarpTarget locked = GetTarget(key);
            if (!locked.IsValid) return false;          // 잠긴 타겟이 사라짐 → 재설정 허용
            return locked.anchor != newAnchor;          // 다른 액터로의 전환만 차단
        }

        public void SetTarget(Transform target, bool useSnapshot = true)
            => SetTarget(DefaultTargetKey, target, useSnapshot);

        public void SetTarget(string key, Transform target, bool useSnapshot = true)
        {
            string useKey = string.IsNullOrEmpty(key) ? DefaultTargetKey : key;
            if (IsTargetSwitchLocked(useKey, target)) return;
            if (_targetLockActive && target != null)
                _lockedTargetKeys.Add(useKey);

            var t = new MotionWarpTarget
            {
                anchor = target,
                offset = Vector3.zero,
                space  = WarpTargetSpace.World,
                follow = !useSnapshot,
            };
            _targets[useKey] = t;
            // 활성 키와 같을 때만 캐시/스냅샷 갱신.
            if (useKey == _activeKey)
            {
                _activeTarget = t;
                _snapshotPosition = target != null ? t.ResolveWorldPosition() : Vector3.zero;
                _snapshotTargetCenter = _snapshotPosition;
                _feasibilityChecked = false;
                _isApplicable = false;
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
            if (_targetLockActive && target.anchor != null)
                _lockedTargetKeys.Add(useKey);

            _targets[useKey] = target;
            if (useKey == _activeKey)
            {
                _activeTarget = target;
                _snapshotPosition = target.IsValid && !target.follow
                    ? target.ResolveWorldPosition()
                    : Vector3.zero;
                _snapshotTargetCenter = _snapshotPosition;
                _feasibilityChecked = false;
                _isApplicable = false;
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
            bool wasWarping = _warpRemainingTime > 0f;

            _targets.Clear();
            _lockedTargetKeys.Clear(); // 전면 리셋 — 다음 SetTarget 이 새 첫 타겟이 된다
            _activeTarget = MotionWarpTarget.None;
            _snapshotPosition = Vector3.zero;
            _feasibilityChecked = false;
            _isApplicable = false;
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
                bool wasWarping = _warpRemainingTime > 0f;
                _activeTarget = MotionWarpTarget.None;
                _snapshotPosition = Vector3.zero;
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = 0f;
                _hasTargetVelocityHistory = false;
                EndWarpWindow();
                _warpRemainingTime = 0f;
                if (wasWarping)
                    OnWarpCancelled?.Invoke(WarpCancelReason.ManualClear);
            }
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
            if (deltaTime <= 0f)
                return rootVelocity;

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(totalDuration);

            totalDuration = settings.duration > 0f ? settings.duration : totalDuration;
            float authoredRemainingTime = isWarping
                ? _warpAuthoredRemainingTime
                : remainingTime;
            float authoredTotalDuration = _warpTotalDuration > 0f
                ? _warpTotalDuration
                : totalDuration;

            // 증폭 전 순수 루트 속도를 보존한다 — delta-warp 의 누적/캐시(윈도우 총 루트모션)와
            // 잔여 보정 추정은 "애니메이터가 만든 원본 루트모션" 을 기준으로 해야 하기 때문.
            Vector3 rawRootVelocity = rootVelocity;
            Vector3 rawHoriz = new Vector3(rawRootVelocity.x, 0f, rawRootVelocity.z);
            float rawFrameDist = rawHoriz.magnitude * deltaTime;

            // ── delta-warp 누적: 윈도우 전체에 걸쳐(타겟/적용성 게이트 "이전") 측정한다.
            //    minDistance 안쪽·OOR 프레임까지 포함해야 캐시 총량이 "순수 애니메이션 루트모션"(타겟 독립)이 된다.
            //    raw(증폭 전)를 액터 회전 역변환으로 애니메이션 로컬프레임에 투영 → facing 무관 누적.
            if (isWarping && rawFrameDist > 0f)
            {
                _accumRootPath  += rawFrameDist;
                _accumRootLocal += Quaternion.Inverse(transform.rotation) * (rawHoriz * deltaTime);
            }

            // 루트모션 속도 증폭: 타겟 게이트보다 "앞" 에서 적용한다.
            // 타겟 없는 단독 증폭이면 아래 early-return 으로 증폭된 rootVelocity 가 그대로 반환된다.
            // 타겟이 있으면(아래 delta-warp 경로) 증폭값이 gainHoriz(원본 재생 항)로 흡수되어
            // amplify 와 타겟 워프가 같은 파이프라인에서 합성된다. amplify off 면 gain=1.
            if (isWarping && settings.amplifyEnabled)
                rootVelocity = ApplyRootMotionAmplify(
                    rootVelocity,
                    settings,
                    authoredRemainingTime,
                    authoredTotalDuration);

            if (!_activeTarget.IsValid || !isWarping)
            {
                _feasibilityChecked = false;
                _isApplicable = false;
                _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);
                _outOfRangeAccumulator = 0f;
                _lastFailureReason = !_activeTarget.IsValid ? "Target 없음" : "워프 비활성";
                return rootVelocity;
            }

            // 타겟 사망/파괴 감지 → 즉시 캔슬 (TargetLost).
            if (IsTargetUnreachableLifecycle())
            {
                _isApplicable = false;
                _lastFailureReason = "타겟 사망/파괴";
                Cancel(WarpCancelReason.TargetLost);
                cancelWarp?.Invoke();
                return rootVelocity;
            }

            // settings / totalDuration 는 함수 상단에서 이미 해석됨 (증폭 패스와 공유).
            if (settings.overrideDistance)
            {
                minDistance = settings.minDistance;
                maxDistance = settings.maxDistance;
                maxSpeed = settings.maxSpeed;
            }

            // 거리/회전 게이트는 타겟 중심을, Translation은 도착 Pose를 사용한다.
            Vector3 targetCenter = _activeTarget.follow
                ? ResolveTargetCenter()
                : _snapshotTargetCenter;

            // Predictive: 추정 속도 × predictionFactor × 남은 시간 만큼 미래 위치를 미리 가산.
            if (settings.targetPolicy == MotionWarpTargetPolicy.Predictive
                && _hasTargetVelocityHistory
                && remainingTime > 0f)
            {
                float factor = Mathf.Clamp01(settings.predictionFactor);
                targetCenter += _targetVelocity * factor * remainingTime;
            }

            Vector3 targetWorld;
            if (_activeTarget.follow)
            {
                Vector3 liveArrival = ResolveArrivalPosition(
                    _warpStartPosition,
                    targetCenter);
                targetWorld = LimitArrivalByObstacle(
                    currentPosition,
                    liveArrival,
                    out _hasObstacleLimit);
                _obstacleLimitedArrival = targetWorld;
            }
            else
            {
                targetWorld = _snapshotPosition;
            }

            Vector3 toTargetCenter = targetCenter - currentPosition;
            toTargetCenter.y = 0f;
            Vector3 toTarget = targetWorld - currentPosition;
            toTarget.y = 0f;

            float targetDistance = toTargetCenter.magnitude;
            float remainingDist = toTarget.magnitude;
            if (!_feasibilityChecked)
            {
                // 사거리(min/max) 밖이면 캔슬. "maxSpeed×duration 내 도달 불가"는 더 이상 캔슬 사유가 아니다:
                // maxDistance 로 이미 상한이 걸려 있고, 도달 못 하는 거리라도 ClampHorizontal 의 maxSpeed 클램프로
                // "붙을 수 있는 데까지 최대속도 접근" 하는 편이 워프를 통째로 죽이고 허공을 치는 것보다 낫다.
                bool outOfRange = targetDistance < minDistance || targetDistance > maxDistance;

                if (outOfRange)
                {
                    cancelWarp?.Invoke();
                    _isApplicable = false;
                    _lastFailureReason = "거리 범위 이탈";
                    _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);
                    return rootVelocity;
                }

                _feasibilityChecked = true;
            }

            if (targetDistance < minDistance || targetDistance > maxDistance || toTargetCenter.sqrMagnitude <= 0.0001f)
            {
                _isApplicable = false;
                _lastFailureReason = toTargetCenter.sqrMagnitude <= 0.0001f ? "타겟 거리 0" : "이동 중 거리 범위 이탈";
                _blendWeight = Mathf.MoveTowards(_blendWeight, 0f, deltaTime * 12f);

                // OOR 누적 시간 임계 초과 시 명시 캔슬.
                _outOfRangeAccumulator += deltaTime;
                if (_outOfRangeAccumulator >= OutOfRangeCancelThreshold)
                {
                    Cancel(WarpCancelReason.OutOfRangeTimeout);
                    cancelWarp?.Invoke();
                }
                return rootVelocity;
            }

            // 정상 범위로 복귀 — 누적값 리셋.
            _outOfRangeAccumulator = 0f;

            _isApplicable = true;
            _lastFailureReason = string.Empty;
            _lastArrivalError = remainingDist;
            _blendWeight = Mathf.MoveTowards(_blendWeight, 1f, deltaTime * 15f);

            float t = authoredTotalDuration > 0f
                ? 1f - (authoredRemainingTime / authoredTotalDuration)
                : 1f;
            t = Mathf.Clamp01(t);
            float eased = 1f - (1f - t) * (1f - t);

            bool translationAllowed = settings.translationWeight > 0f;
            {
                if (settings.noTranslationWithinReach > 0f
                    && remainingDist <= settings.noTranslationWithinReach)
                {
                    translationAllowed = false;
                    _lastFailureReason = "도착 오차 Dead Zone: Rotation만 적용";
                }
                else if (!MotionWarpArrivalUtility.IsWithinWarpAngle(
                             _warpStartForward,
                             targetCenter - _warpStartPosition,
                             settings.maxWarpAngle))
                {
                    translationAllowed = false;
                    _lastFailureReason = "Translation 허용 각도 초과";
                }
                else if (settings.translationEndLeadTime > 0f
                         && authoredRemainingTime <= settings.translationEndLeadTime)
                {
                    translationAllowed = false;
                    _lastFailureReason = "Translation 조기 종료";
                }
            }

            Vector3 targetVelocity = settings.modifierType switch
            {
                MotionWarpModifierType.DeltaWarp => EvaluateDeltaWarpVelocity(
                    rootVelocity, toTarget, rawFrameDist, authoredRemainingTime,
                    deltaTime, maxSpeed, settings),
                MotionWarpModifierType.Scale => EvaluateScaleVelocity(rootVelocity, toTarget, remainingDist, remainingTime, maxSpeed),
                MotionWarpModifierType.Skew => EvaluateSkewVelocity(rootVelocity, toTarget, remainingDist, remainingTime, deltaTime, maxSpeed, eased),
                _ => EvaluateAdditiveVelocity(rootVelocity, toTarget, remainingDist, remainingTime, deltaTime, maxSpeed, eased)
            };

            float curveWeight = settings.translationCurve != null && settings.translationCurve.length > 0
                ? Mathf.Clamp01(settings.translationCurve.Evaluate(t))
                : 1f;
            float translationWeight = translationAllowed
                ? settings.translationWeight * curveWeight
                : 0f;
            float effectiveTranslationWeight = _blendWeight * translationWeight;
            Vector3 blended = Vector3.Lerp(rootVelocity, targetVelocity, effectiveTranslationWeight);

            // 후보 속도가 아니라 실제 블렌딩되어 KCC에 전달되는 수평 변위만
            // DeltaWarp 보정 예산으로 기록한다.
            if (settings.modifierType == MotionWarpModifierType.DeltaWarp
                && effectiveTranslationWeight > 0f
                && deltaTime > 0f)
            {
                Vector3 appliedCorrection = (blended - rootVelocity) * deltaTime;
                appliedCorrection.y = 0f;
                _accumulatedCorrection += appliedCorrection;
            }

            // Y축 정책: ignoreY bool 과 yPolicy enum 호환 매핑 후 분기.
            WarpYPolicy yPol = settings.ResolveYPolicy();
            float dy = targetWorld.y - currentPosition.y;
            float horizon = remainingTime > 0.01f ? remainingTime : deltaTime;
            float matchYSpeed = horizon > 0.0001f ? dy / horizon : 0f;
            blended.y = MotionWarpArrivalUtility.ResolveVerticalVelocity(
                rootVelocity.y,
                matchYSpeed,
                _blendWeight * translationWeight,
                yPol,
                eased);

            // ── 재생 속도 배율 갱신 (캐시 불필요, 매 프레임) ────────────────────────────────
            // DeltaPosition 에는 이전 프레임에 설정한 Graph.Speed(= _prevWarpK)가 이미 곱해져 있다.
            // _prevWarpK 로 역산해 Speed=1 기준 기저 속도를 구한 뒤 desiredSpeed / baseSpeed 로 K 계산.
            // 1-프레임 래그(이전 K 역산)는 안정적이며 플레이어에게 보이지 않는다.
            float rawHorizSpeed = rawHoriz.magnitude;
            if (settings.usePlaybackRateWarp
                && translationAllowed
                && rawHorizSpeed > 0.001f
                && remainingTime > 0.01f)
            {
                float baseHorizSpeed = rawHorizSpeed / _prevWarpK;
                float desiredHorizSpeed = remainingDist / remainingTime;
                float minRate = Mathf.Max(0.01f, Mathf.Min(settings.playbackRateRange.x, settings.playbackRateRange.y));
                float maxRate = Mathf.Max(minRate, Mathf.Max(settings.playbackRateRange.x, settings.playbackRateRange.y));
                float newK = Mathf.Clamp(desiredHorizSpeed / baseHorizSpeed, minRate, maxRate);
                WarpPlayRateScale = newK;
                _prevWarpK = newK;
            }
            else
            {
                WarpPlayRateScale = 1f;
                _prevWarpK = 1f;
            }
            // ──────────────────────────────────────────────────────────────────────────────

            return blended;
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

            Vector3 approach = targetDirection * allowedApproachSpeed;
            Vector3 tangent = horizontalVelocity - targetDirection * approachSpeed;

            if (distance <= desiredDistance + CloseRangeStopBuffer)
                tangent *= CloseRangeTangentRetention;

            Vector3 clampedHorizontal = approach + tangent;
            return new Vector3(clampedHorizontal.x, velocity.y, clampedHorizontal.z);
        }

        private Vector3 GetSelfCapsuleCenterPosition(Vector3 currentPosition)
        {
            CapsuleCollider selfCapsule = GetComponent<CapsuleCollider>();
            if (selfCapsule == null)
                return currentPosition;

            Vector3 centerOffset = selfCapsule.transform.TransformPoint(selfCapsule.center) - transform.position;
            return currentPosition + centerOffset;
        }

        private Vector3 GetHorizontalTargetOffset(Vector3 currentPosition, Transform target)
        {
            Vector3 targetPosition = _activeTarget.follow
                ? _activeTarget.ResolveWorldPosition()
                : _snapshotPosition;

            CapsuleCollider targetCapsule = target.GetComponent<CapsuleCollider>()
                                            ?? target.GetComponentInParent<CapsuleCollider>();
            if (targetCapsule != null)
                targetPosition = targetCapsule.transform.TransformPoint(targetCapsule.center);

            Vector3 toTarget = targetPosition - currentPosition;
            toTarget.y = 0f;
            return toTarget;
        }

        private float GetCombinedHorizontalRadius(Transform target)
        {
            float selfRadius = GetSelfHorizontalRadius();
            float targetRadius = GetHorizontalRadius(GetTargetCapsule(target));

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

        /// <summary>
        /// delta-warp: 원본 루트 델타를 재생(gainHoriz)하면서, 타겟까지의 잔여 보정을
        /// "이 프레임이 차지하는 루트모션 비율(share)" 만큼 분배해 더한다.
        /// 보정이 루트모션 크기에 비례 분배되므로 애니메이션의 가속–감속 커브가 워프를 구동하고,
        /// 누적 합이 타겟에 수렴해 정확 착지한다(잔여 기준 폐루프 → 스티어링/Live 타겟 드리프트 흡수).
        ///
        /// - 유효한 에디터 베이크: 위 정확 모드.
        /// - 베이크 없음: 현재 원본 속도와 남은 시간으로 예상 도착점을 계산하는 결정적 제한 보정.
        /// amplify 가 켜지면 gainHoriz 가 증폭돼 "더 빠르고 펀치감 있는 접근" 이 되지만 착지점은
        /// 여전히 타겟(캐시는 amplify 무관한 순수 애니메이션 총량 저장).
        /// 반환은 수평 속도(.y 는 호출부 Y 정책이 덮어씀). maxSpeed 로 수평 클램프(폐루프가 다음 프레임 보상).
        /// </summary>
        private Vector3 EvaluateDeltaWarpVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float rawFrameDist,
            float remainingTime,
            float deltaTime,
            float maxSpeed,
            in MotionWarpWindowSettings settings)
        {
            Vector3 gainHoriz = new Vector3(rootVelocity.x, 0f, rootVelocity.z);

            if (_hasActiveTotal && _activeTotal.PathLen > 0.0001f)
            {
                // 정확 모드 — 잔여 기준 폐루프.
                float remainingPath = Mathf.Max(rawFrameDist, _activeTotal.PathLen - _accumRootPath);
                Vector3 localDir = _activeTotal.LocalTotal.sqrMagnitude > 1e-6f
                    ? _activeTotal.LocalTotal.normalized
                    : Vector3.forward;
                // 남은 raw 변위를 현재 회전으로 추정(스티어링 흡수).
                Vector3 remainingRawWorld = transform.rotation * (localDir * remainingPath);
                remainingRawWorld.y = 0f;
                Vector3 correctionTotal = toTarget - remainingRawWorld; // 남은 구간서 메울 총 보정
                float correctionReferenceDistance =
                    MotionWarpArrivalUtility.ResolveCorrectionReferenceDistance(
                        remainingPath,
                        toTarget.magnitude);
                EnsureCorrectionBudget(correctionReferenceDistance, settings);
                correctionTotal =
                    MotionWarpArrivalUtility.LimitAccumulatedCorrection(
                        _accumulatedCorrection,
                        correctionTotal,
                        _correctionBudget);
                // remainingPath==0 (rawFrameDist==0 && accum>=PathLen, 예: settle 꼬리 + Live 타겟)이면
                // 0/0 → NaN 이 KCC 로 전파된다. 이 경우 share=1 로 디그레이드 —
                // remainingRawWorld≈0 → correctionTotal≈toTarget → 마지막 간격을 즉시 메운다(maxSpeed 클램프).
                float share = remainingPath > 1e-5f ? Mathf.Clamp01(rawFrameDist / remainingPath) : 1f;

                Vector3 frameWarped = gainHoriz * deltaTime + correctionTotal * share; // 프레임 변위
                // 주의: 큰 보정이 마지막 프레임에 집중되면 이 maxSpeed 클램프가 잔여 오프셋을 남길 수 있다
                // (다음 프레임이 없어 보상 불가). 폐루프가 평소 분산시키므로 드묾.
                Vector3 baseline = ClampHorizontal(
                    gainHoriz,
                    rootVelocity.y,
                    maxSpeed);
                Vector3 result = ClampHorizontal(
                    frameWarped / deltaTime,
                    rootVelocity.y,
                    maxSpeed);
                return ApplyCorrectionBudgetToResult(
                    baseline,
                    result,
                    deltaTime);
            }

            // 베이크/캐시가 없는 첫 실행도 원본 예상 도착점 대비 제한 보정을 사용한다.
            float fallbackHorizon = Mathf.Max(
                remainingTime,
                Mathf.Max(deltaTime, 0.0001f));
            Vector3 predictedRemaining = gainHoriz * fallbackHorizon;
            float fallbackReferenceDistance =
                MotionWarpArrivalUtility.ResolveCorrectionReferenceDistance(
                    predictedRemaining.magnitude,
                    toTarget.magnitude);
            EnsureCorrectionBudget(fallbackReferenceDistance, settings);
            Vector3 remainingCorrection =
                MotionWarpArrivalUtility.LimitAccumulatedCorrection(
                    _accumulatedCorrection,
                    toTarget - predictedRemaining,
                    _correctionBudget);
            Vector3 fallbackBaseline = ClampHorizontal(
                gainHoriz,
                rootVelocity.y,
                maxSpeed);
            Vector3 fallbackResult = ClampHorizontal(
                gainHoriz + remainingCorrection / fallbackHorizon,
                rootVelocity.y,
                maxSpeed);
            return ApplyCorrectionBudgetToResult(
                fallbackBaseline,
                fallbackResult,
                deltaTime);
        }

        private void EnsureCorrectionBudget(
            float correctionReferenceDistance,
            in MotionWarpWindowSettings settings)
        {
            if (_correctionBudgetInitialized)
                return;

            _correctionBudget =
                MotionWarpArrivalUtility.ResolveCorrectionBudget(
                    correctionReferenceDistance,
                    settings.maxCorrectionDistance,
                    settings.maxCorrectionRatio);
            _correctionBudgetInitialized = true;
        }

        private Vector3 ApplyCorrectionBudgetToResult(
            Vector3 baselineVelocity,
            Vector3 warpedVelocity,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return baselineVelocity;

            Vector3 candidateStep =
                (warpedVelocity - baselineVelocity) * deltaTime;
            candidateStep.y = 0f;
            float scale =
                MotionWarpArrivalUtility.ResolveCorrectionStepScale(
                    _accumulatedCorrection,
                    candidateStep,
                    _correctionBudget);
            return Vector3.Lerp(
                baselineVelocity,
                warpedVelocity,
                scale);
        }

        // 수평 성분만 maxSpeed 로 클램프하고 Y 는 전달값 유지.
        private static Vector3 ClampHorizontal(Vector3 velocity, float y, float maxSpeed)
        {
            Vector3 h = new Vector3(velocity.x, 0f, velocity.z);
            if (maxSpeed > 0f && h.magnitude > maxSpeed)
                h = h.normalized * maxSpeed;
            return new Vector3(h.x, y, h.z);
        }

        /// <summary>
        /// 루트모션 고유 속도 곡선을 게인으로 증폭한다 (타겟 무관 직교 패스).
        /// 게인 커브는 정규화 워프 진행도 t(0~1)를 배율로 매핑하며, 수평(XZ)에만 적용하고
        /// Y(중력/루트 수직)는 보존한다. 결과 수평 속력은 amplifyMaxSpeed 로 자체 클램프.
        /// </summary>
        private static Vector3 ApplyRootMotionAmplify(
            Vector3 rootVelocity,
            in MotionWarpWindowSettings settings,
            float remainingTime,
            float totalDuration)
        {
            var curve = settings.amplifyGainCurve;
            if (curve == null || curve.length == 0)
                return rootVelocity;

            // 워프 윈도우 진행도 — EvaluateVelocity 본문의 t 정의와 동일 공식.
            float t = totalDuration > 0f ? 1f - (remainingTime / totalDuration) : 1f;
            float gain = Mathf.Max(0f, curve.Evaluate(Mathf.Clamp01(t)));

            Vector3 horiz = new Vector3(rootVelocity.x, 0f, rootVelocity.z) * gain;
            float ceil = settings.amplifyMaxSpeed > 0f ? settings.amplifyMaxSpeed : float.MaxValue;
            if (horiz.magnitude > ceil)
                horiz = horiz.normalized * ceil;

            return new Vector3(horiz.x, rootVelocity.y, horiz.z);
        }

        private static Vector3 EvaluateAdditiveVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float remainingTime,
            float deltaTime,
            float maxSpeed,
            float eased)
        {
            float baseSpeed = remainingTime > 0.01f
                ? remainingDist / remainingTime
                : remainingDist / deltaTime;

            float warpSpeed = Mathf.Lerp(baseSpeed * 1.3f, baseSpeed * 0.7f, eased);
            warpSpeed = Mathf.Clamp(warpSpeed, 0f, maxSpeed);

            Vector3 warpVelocity = toTarget.normalized * warpSpeed;
            return new Vector3(warpVelocity.x, rootVelocity.y, warpVelocity.z);
        }

        private static Vector3 EvaluateScaleVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float remainingTime,
            float maxSpeed)
        {
            Vector3 rootHorizontal = new Vector3(rootVelocity.x, 0f, rootVelocity.z);
            float rootSpeed = rootHorizontal.magnitude;
            float desiredSpeed = remainingTime > 0.01f ? remainingDist / remainingTime : maxSpeed;
            desiredSpeed = Mathf.Clamp(desiredSpeed, 0f, maxSpeed);

            if (rootSpeed <= 0.01f)
            {
                Vector3 fallback = toTarget.normalized * desiredSpeed;
                return new Vector3(fallback.x, rootVelocity.y, fallback.z);
            }

            float scale = desiredSpeed / rootSpeed;
            Vector3 scaled = toTarget.normalized * rootSpeed * scale;
            return new Vector3(scaled.x, rootVelocity.y, scaled.z);
        }

        private static Vector3 EvaluateSkewVelocity(
            Vector3 rootVelocity,
            Vector3 toTarget,
            float remainingDist,
            float remainingTime,
            float deltaTime,
            float maxSpeed,
            float eased)
        {
            Vector3 rootHorizontal = new Vector3(rootVelocity.x, 0f, rootVelocity.z);
            Vector3 targetDir = toTarget.normalized;

            float desiredSpeed = remainingTime > 0.01f
                ? remainingDist / remainingTime
                : remainingDist / deltaTime;
            desiredSpeed = Mathf.Clamp(desiredSpeed, 0f, maxSpeed);

            float rootSpeed = rootHorizontal.magnitude;
            float preservedSpeed = Mathf.Clamp(rootSpeed, 0f, maxSpeed);
            float skewSpeed = Mathf.Lerp(preservedSpeed, desiredSpeed, Mathf.Lerp(0.55f, 0.95f, eased));
            skewSpeed = Mathf.Clamp(skewSpeed, 0f, maxSpeed);

            Vector3 skewVelocity = targetDir * skewSpeed;
            return new Vector3(skewVelocity.x, rootVelocity.y, skewVelocity.z);
        }

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
            if (!_activeTarget.IsValid || !isWarping || !_isApplicable)
                return false;

            MotionWarpWindowSettings settings = _hasWindowSettings
                ? _windowSettings
                : MotionWarpWindowSettings.Default(0f);

            if (settings.rotationWeight <= 0f)
                return false;

            if (settings.overrideDistance)
            {
                minDistance = settings.minDistance;
                maxDistance = settings.maxDistance;
                maxSpeed = settings.maxSpeed;
            }

            Vector3 targetWorld = GetCurrentTargetPosition();
            Vector3 toTarget = targetWorld - currentPosition;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // 도달 가능성(maxSpeed×remainingTime)은 회전 게이트에서 제외 — 이동 경로(EvaluateVelocity)와 일관되게,
            // 사거리(min/max) 안이면 도달 못 하는 거리라도 타겟 방향으로 회전 호밍한다(허공 스윙 방지).
            if (dist < minDistance || dist > maxDistance || toTarget.sqrMagnitude <= 0.01f)
                return false;

            direction = toTarget.normalized;
            return true;
        }

        /// <summary>
        /// 워프 회전 보간 결과를 한 번에 계산해 반환.
        /// rotationCurve 가 있으면 정규화 시간 t 의 곡선 알파로 Slerp(startRotation, targetRotation, alpha).
        /// 없으면 EaseOut(1-(1-t)^2) 폴백.
        /// startRotation 은 워프가 처음 applicable 해진 프레임에 캡처된다.
        /// </summary>
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

            // 정규화 시간 t.
            float duration = settings.duration > 0f ? settings.duration : totalDuration;
            float authoredRemainingTime = isWarping
                ? _warpAuthoredRemainingTime
                : remainingTime;
            float authoredDuration = _warpTotalDuration > 0f
                ? _warpTotalDuration
                : duration;
            float t = authoredDuration > 0f
                ? 1f - (authoredRemainingTime / authoredDuration)
                : 1f;
            t = Mathf.Clamp01(t);

            // 곡선 알파 (없으면 EaseOut 폴백).
            float alpha = settings.rotationCurve != null && settings.rotationCurve.length > 0
                ? Mathf.Clamp01(settings.rotationCurve.Evaluate(t))
                : 1f - (1f - t) * (1f - t);
            alpha *= settings.rotationWeight;

            Quaternion target = Quaternion.LookRotation(dir);
            newRotation = Quaternion.Slerp(_warpStartRotation, target, alpha).normalized;
            return true;
        }

        // ── 디버그 / 모니터링 노출 (Phase 5) ──────────────────────────────
        public float BlendWeight => _blendWeight;
        public float OutOfRangeAccumulator => _outOfRangeAccumulator;
        public Vector3 TargetVelocity => _targetVelocity;
        public bool HasActiveWindow => _hasWindowSettings;
        public MotionWarpWindowSettings ActiveWindowSettings => _windowSettings;
        public MotionWarpTarget ActiveTarget => _activeTarget;
        public Vector3 SnapshotPosition => _snapshotPosition;
        public Vector3 CurrentTargetCenter => GetCurrentTargetPosition();
        public Vector3 CurrentDesiredArrival => !_activeTarget.IsValid
            ? Vector3.zero
            : _activeTarget.follow
                ? ResolveArrivalPosition(_warpStartPosition, ResolveTargetCenter())
                : _snapshotPosition;
        public float CurrentArrivalShellRadius => !_activeTarget.IsValid
            ? 0f
            : GetSelfHorizontalRadius()
              + GetHorizontalRadius(GetTargetCapsule(_activeTarget.anchor))
              + Mathf.Max(0f, _windowSettings.desiredStandOff);

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
                text = $"warp active={_warpRemainingTime > 0f} blend={_blendWeight:F2} oor={_outOfRangeAccumulator:F2}",
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
