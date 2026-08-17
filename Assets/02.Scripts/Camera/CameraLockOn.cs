using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 락온용 포커스/앵커/우선순위를 제공하는 선택 인터페이스.
    /// 구현하지 않은 대상은 호스트의 ICameraRuntimeAdapter가 반환한 루트 Transform을 사용한다.
    /// </summary>
    public interface ILockOnTarget
    {
        Transform Transform { get; }
        Vector3 FocusPosition { get; }
        Vector3 UIAnchorPosition { get; }
        bool CanLockOn { get; }
        float LockOnPriority { get; }
        float BoundsSize { get; }
    }

    /// <summary>
    /// LockOn 시스템 전체 로직: 대상 탐색/전환/해제, 거리 기반 오비탈 추적 회전, 전환 연출.
    /// CameraManager에서 매 프레임 UpdateRotation / UpdateTransition을 호출한다.
    /// </summary>
    public class CameraLockOn
    {
        public bool IsActive { get; private set; }
        public Transform CurrentTarget { get; private set; }

        private readonly CameraSettings _settings;
        private readonly Transform _player;
        private readonly UnityEngine.Camera _camera;
        private readonly LayerMask _lockOnLayer;
        private readonly LayerMask _lineOfSightLayer;
        private System.Func<Vector3> _playerVelocityProvider;

        // 내부 상태
        private CapsuleCollider _targetCollider;
        private readonly List<Transform> _targets = new List<Transform>();
        private readonly HashSet<Transform> _targetSet = new HashSet<Transform>();
        private int _currentIndex = -1;
        private float _lastSwitchTime;

        // 포커스 스무딩
        private float _smoothY;
        private float _targetLostTimer;

        // 오비탈 오프셋
        private float _signedOffsetAngle;
        private float _offsetAngleVelocity;
        private float _lastEnemyYaw;
        private float _freeFactor;
        private float _freeFactorVelocity;
        private bool _orbitInitialized;
        private bool _wasSkipping; // skip→active 전환 감지 (복귀 시 부드러운 재보간)
        private Vector3 _activeFocusPos;
        private Vector3 _activeFocusVelocity;
        private Vector3 _pivotOffset;
        private Vector3 _pivotOffsetVelocity;

        private const float FREE_FACTOR_SMOOTH_TIME = 0.15f;
        private const float ORBIT_FREE_PULL_MAX_SMOOTH = 5f;
        private const float SIGN_DEAD_ZONE_DEG = 0.5f;
        private const float OVERCOME_DEADZONE_DEG = 0.5f;
        private const float ORBIT_SMOOTH_MIN_MULT = 0.3f;
        private const float ORBIT_OFFSET_MAX_DELTA = 45f;

        // 해제 전환 연출
        private bool _isTransitioning;
        private float _transitionTimer;
        private float _transitionYaw, _transitionPitch;

        // 대상 정렬용 임시 구조체
        private struct TargetInfo
        {
            public Transform transform;
            public float distanceSq;
            public float sortScore; // 거리 + 카메라 방향 가중치 합산
        }

        private readonly struct LockOnCandidate
        {
            public readonly Transform transform;
            public readonly Vector3 position;
            public readonly float distanceXZ;
            public readonly float distanceScore;
            public readonly float angleScore;
            public readonly bool isCurrentTarget;
            public readonly float lockOnPriority;

            public LockOnCandidate(
                Transform transform,
                Vector3 position,
                float distanceXZ,
                float distanceScore,
                float angleScore,
                bool isCurrentTarget,
                float lockOnPriority)
            {
                this.transform = transform;
                this.position = position;
                this.distanceXZ = distanceXZ;
                this.distanceScore = distanceScore;
                this.angleScore = angleScore;
                this.isCurrentTarget = isCurrentTarget;
                this.lockOnPriority = lockOnPriority;
            }
        }

        public CameraLockOn(
            CameraSettings settings,
            Transform player,
            UnityEngine.Camera camera,
            LayerMask lockOnLayer,
            LayerMask lineOfSightLayer)
        {
            _settings = settings;
            _player = player;
            _camera = camera;
            _lockOnLayer = lockOnLayer;
            _lineOfSightLayer = lineOfSightLayer;
        }

        public void SetPlayerVelocityProvider(System.Func<Vector3> provider)
        {
            _playerVelocityProvider = provider;
        }

        // ── 토글 ──

        /// <summary>
        /// 락온 시도. 성공 시 true.
        /// </summary>
        public bool TryActivate()
        {
            CollectTargets(requireLineOfSight: false);
            if (_targets.Count == 0) return false;

            _currentIndex = 0;
            SetTarget(_targets[0]);
            IsActive = true;
            return true;
        }

        public bool TryRestoreTarget(Transform target)
        {
            if (target == null || _player == null)
                return false;

            if (!IsAliveTarget(target))
                return false;

            float distance = Vector3.Distance(_player.position, target.position);
            if (distance > GetReleaseRange())
                return false;

            SetTarget(target);
            IsActive = true;
            _currentIndex = -1;
            return true;
        }

        public void Release()
        {
            NotifyUnLockOn(CurrentTarget);
            CurrentTarget = null;
            _targetCollider = null;
            IsActive = false;
            _targets.Clear();
            _currentIndex = -1;
            _isTransitioning = false;
            _orbitInitialized = false;
            _wasSkipping = false;
            _signedOffsetAngle = 0f;
            _offsetAngleVelocity = 0f;
            _freeFactor = 0f;
            _freeFactorVelocity = 0f;
            _activeFocusVelocity = Vector3.zero;
            _targetLostTimer = 0f;
        }

        // ── 대상 전환 ──

        public void SwitchTarget(int direction)
        {
            if (Time.time - _lastSwitchTime < _settings.targetSwitchCooldown) return;

            CollectTargets(requireLineOfSight: false);
            if (_targets.Count <= 1) return;

            Transform nextTarget = SelectSwitchTarget(direction);
            if (nextTarget == null || nextTarget == CurrentTarget)
                return;

            NotifyUnLockOn(CurrentTarget);
            SetTarget(nextTarget);
            _currentIndex = _targets.IndexOf(nextTarget);
            _lastSwitchTime = Time.time;
        }

        // ── 추적 회전 (LateUpdate에서 호출) ──

        /// <summary>
        /// 락온 대상을 향한 카메라 회전을 계산한다.
        /// skipCondition이 true이면 회전을 건너뛴다 (입력 잠금, LookAt 오버라이드 등).
        /// 대상 소실 시 전환 연출을 시작한다.
        /// </summary>
        /// <returns>전환 연출이 끝나 Release + CameraAlign이 필요하면 true</returns>
        public bool UpdateRotation(ref float yaw, ref float pitch, bool skipRotation)
        {
            // 유효성 체크: 스킵 조건 중 대상이 죽었으면 바로 해제
            if (!IsAliveTarget(CurrentTarget) && skipRotation)
            {
                Release();
                return false;
            }

            if (skipRotation || !IsActive || CurrentTarget == null)
            {
                if (IsActive && CurrentTarget != null)
                    _wasSkipping = true;
                return false;
            }

            // skip→active 복귀 첫 프레임: 현재 yaw에서 부드럽게 재보간
            if (_wasSkipping)
            {
                _wasSkipping = false;
                _orbitInitialized = false;
                _offsetAngleVelocity = 0f;
            }

            if (_isTransitioning)
                return false;

            // 유효성 체크
            if (!IsAliveTarget(CurrentTarget))
            {
                if (!TryFindNext(requireLineOfSight: false))
                {
                    StartTransition(yaw, pitch);
                    return false;
                }
            }

            float dist = Vector3.Distance(_player.position, CurrentTarget.position);
            float releaseRange = GetReleaseRange();
            if (dist > releaseRange)
            {
                _targetLostTimer += Time.deltaTime;
                if (_targetLostTimer >= Mathf.Max(0f, _settings.lockOnLostGraceTime))
                {
                    if (!TryFindNext(requireLineOfSight: false))
                    {
                        StartTransition(yaw, pitch);
                    }
                    return false;
                }
            }
            else
            {
                _targetLostTimer = 0f;
            }

            float heightOffset = _targetCollider != null ? _targetCollider.height * 0.25f : 1f;
            Vector3 targetFocus = CurrentTarget.position;
            targetFocus.y -= heightOffset;
            _activeFocusPos = Vector3.SmoothDamp(
                _activeFocusPos,
                targetFocus,
                ref _activeFocusVelocity,
                _settings.lockOnFocusSmoothTime);
            _smoothY = _activeFocusPos.y;

            // XZ 방향
            Vector3 toTargetXZ = new Vector3(
                _activeFocusPos.x - _player.position.x, 0f,
                _activeFocusPos.z - _player.position.z);
            float flatDist = toTargetXZ.magnitude;
            float enemyYaw = flatDist > 0.001f
                ? Mathf.Atan2(toTargetXZ.x, toTargetXZ.z) * Mathf.Rad2Deg
                : yaw;

            // 첫 프레임 초기화
            if (!_orbitInitialized)
            {
                _lastEnemyYaw = enemyYaw;
                _signedOffsetAngle = Mathf.DeltaAngle(enemyYaw, yaw);
                _offsetAngleVelocity = 0f;
                _freeFactor = 0f;
                _freeFactorVelocity = 0f;
                _orbitInitialized = true;
            }

            // FreeFactor (거리 기반, smoothstep)
            float rawFreeFactor = Mathf.InverseLerp(_settings.freeOrbitStartDistance, _settings.freeOrbitFullDistance, flatDist);
            rawFreeFactor = rawFreeFactor * rawFreeFactor * (3f - 2f * rawFreeFactor);
            _freeFactor = Mathf.SmoothDamp(_freeFactor, rawFreeFactor, ref _freeFactorVelocity, FREE_FACTOR_SMOOTH_TIME);
            float freeFactor = Mathf.Clamp01(_freeFactor);

            // Overcome 로직: 적이 이동하면 오프셋 각도가 자연스럽게 따라감
            float overcomeSensitivity = (_settings.lockOnOvercomeSensitivity != null && _settings.lockOnOvercomeSensitivity.length > 0)
                ? _settings.lockOnOvercomeSensitivity.Evaluate(flatDist) : 1f;
            overcomeSensitivity *= (1f - freeFactor);
            float deltaYaw = Mathf.DeltaAngle(_lastEnemyYaw, enemyYaw);
            if (Mathf.Abs(deltaYaw) > OVERCOME_DEADZONE_DEG)
            {
                float prevOffset = _signedOffsetAngle;
                _signedOffsetAngle -= deltaYaw * overcomeSensitivity;
                // 부호 반전 방지: Overcome이 0을 넘어가면 0으로 클램핑
                if (prevOffset > 0f && _signedOffsetAngle < 0f) _signedOffsetAngle = 0f;
                if (prevOffset < 0f && _signedOffsetAngle > 0f) _signedOffsetAngle = 0f;
            }

            // 목표 오프셋 각도 (거리 커브)
            float curveMag = (_settings.lockOnOffsetAngleByDistance != null && _settings.lockOnOffsetAngleByDistance.length > 0)
                ? _settings.lockOnOffsetAngleByDistance.Evaluate(flatDist) : 15f;

            // FOV 기반 화면 이탈 방지 최대 안전 각도
            float maxSafeMag = _settings.lockOnMaxOffsetAngle;
            if (_camera != null)
            {
                float camDist = _settings.lockOnDistance;
                float hFovRad = 2f * Mathf.Atan(Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _camera.aspect);
                float frustumHalfWidth = camDist * Mathf.Tan(hFovRad * 0.5f);
                float sinAngle = frustumHalfWidth * 0.35f / Mathf.Max(flatDist, 0.1f);
                maxSafeMag = Mathf.Min(Mathf.Asin(Mathf.Clamp(sinAngle, 0f, 1f)) * Mathf.Rad2Deg, _settings.lockOnMaxOffsetAngle);
            }

            float currentMinAngle = Mathf.Lerp(_settings.lockOnMinOffsetAngle, 0f, freeFactor);
            float targetMag = Mathf.Clamp(curveMag, currentMinAngle, maxSafeMag);

            // 부호 결정 (데드존 안에선 현재 부호 유지)
            float sign = _signedOffsetAngle > SIGN_DEAD_ZONE_DEG ? 1f :
                         _signedOffsetAngle < -SIGN_DEAD_ZONE_DEG ? -1f :
                         _signedOffsetAngle >= 0f ? 1f : -1f;
            float targetSignedAngle = targetMag * sign;
            _lastEnemyYaw = enemyYaw;

            // 적응형 SmoothDamp: 차이가 클수록 빠르게 수렴
            float offsetDelta = Mathf.Abs(targetSignedAngle - _signedOffsetAngle);
            float adaptiveSmoothTime = Mathf.Lerp(
                _settings.lockOnOrbitSmoothTime * ORBIT_SMOOTH_MIN_MULT,
                _settings.lockOnOrbitSmoothTime,
                1f - Mathf.Clamp01(offsetDelta / ORBIT_OFFSET_MAX_DELTA));
            float pullSmoothTime = Mathf.Lerp(adaptiveSmoothTime, ORBIT_FREE_PULL_MAX_SMOOTH, freeFactor);
            _signedOffsetAngle = Mathf.SmoothDamp(
                _signedOffsetAngle, targetSignedAngle, ref _offsetAngleVelocity, pullSmoothTime);
            yaw = enemyYaw + _signedOffsetAngle;

            // Pitch (고저차 감쇠, target 직접 기준)
            float heightDiff = _smoothY - _player.position.y;
            float rawPitch = Mathf.Atan2(
                -heightDiff * _settings.lockOnHeightDampFactor,
                Mathf.Max(flatDist, 0.001f)) * Mathf.Rad2Deg;

            // 고저차가 만드는 실제 시선각을 그대로 사용한다. 상·하단 대상에 별도 pitch 제한을 두지 않는다.
            pitch = Mathf.LerpAngle(pitch, rawPitch, Time.deltaTime * _settings.lockOnPitchSpeed);

            return false;
        }

        // ── 전환 연출 ──

        /// <summary>
        /// 전환 연출 업데이트. Release + CameraAlign이 필요하면 true.
        /// </summary>
        public bool UpdateTransition(ref float yaw, ref float pitch, bool skipCondition)
        {
            if (skipCondition || !_isTransitioning)
                return false;

            _transitionTimer -= Time.deltaTime;

            if (_transitionTimer > 0f)
            {
                // Phase 1: 현재 방향 유지
                yaw = _transitionYaw;
                pitch = _transitionPitch;
                return false;
            }

            // Phase 2: 완료 → Release 후 Align 요청
            _isTransitioning = false;
            Release();
            return true; // caller가 StartCameraAlign 호출
        }

        // ── Public 조회 ──

        public bool IsTransitioning => _isTransitioning;
        public bool HasResidualPivotOffset =>
            _pivotOffset.sqrMagnitude > 0.0004f || _pivotOffsetVelocity.sqrMagnitude > 0.0004f;
        public Vector3 CurrentPivotOffset => _pivotOffset;

        public Vector3 EvaluatePivotOffset(float deltaTime)
        {
            Vector3 targetOffset = Vector3.zero;
            if (IsActive && CurrentTarget != null && _settings.enableLockOnPairFraming)
            {
                Vector3 playerPos = _player.position;
                Vector3 toTarget = GetTargetFocusPosition(CurrentTarget) - playerPos;
                toTarget.y = 0f;
                float targetDistance = toTarget.magnitude;
                if (targetDistance > 0.001f)
                {
                    float desiredOffset = targetDistance * Mathf.Clamp01(_settings.lockOnPairFocusRatio);
                    float maxOffset = Mathf.Max(0f, _settings.lockOnMaxFocusOffsetFromPlayer);
                    targetOffset = toTarget / targetDistance * Mathf.Min(desiredOffset, maxOffset);
                }
            }

            float smoothTime = Mathf.Max(0.001f, _settings.lockOnPairFocusSmoothTime);
            _pivotOffset = Vector3.SmoothDamp(
                _pivotOffset,
                targetOffset,
                ref _pivotOffsetVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);
            _pivotOffset.y = 0f;
            return _pivotOffset;
        }

        // ── 내부 헬퍼 ──

        private void SetTarget(Transform t)
        {
            CurrentTarget = t;
            _targetCollider = t.GetComponent<CapsuleCollider>() ?? t.GetComponentInChildren<CapsuleCollider>();
            CameraRuntimeServices.Adapter.NotifyLockOnChanged(t, true);
            InitSmoothY();
            _activeFocusPos = GetCurrentTargetFocusPosition();
            _activeFocusVelocity = Vector3.zero;
            _orbitInitialized = false;
            _targetLostTimer = 0f;
        }

        private void InitSmoothY()
        {
            if (CurrentTarget == null) return;
            float h = _targetCollider != null ? _targetCollider.height * 0.25f : 1f;
            _smoothY = CurrentTarget.position.y - h;
        }

        private Vector3 GetCurrentTargetFocusPosition()
        {
            if (CurrentTarget == null)
                return Vector3.zero;

            return GetTargetFocusPosition(CurrentTarget);
        }

        private void StartTransition(float yaw, float pitch)
        {
            _isTransitioning = true;
            _transitionTimer = _settings.lockOnTransitionDuration;
            _transitionYaw = yaw;
            _transitionPitch = pitch;
        }

        private bool TryFindNext(bool requireLineOfSight)
        {
            Transform previousTarget = CurrentTarget;
            CollectTargets(requireLineOfSight);
            if (_targets.Count == 0) return false;

            Transform nextTarget = null;
            for (int i = 0; i < _targets.Count; i++)
            {
                Transform candidate = _targets[i];
                if (candidate == null || candidate == previousTarget)
                    continue;

                nextTarget = candidate;
                break;
            }

            if (nextTarget == null)
            {
                // 대체 대상이 없으면 기존 대상 유지 — 단 생존 + 해제 거리 안일 때만.
                // 해제 거리 밖 대상을 유지하면 호출부의 lost 타이머가 리셋되지 않아
                // 매 프레임 CollectTargets(OverlapSphere)가 반복되고 릴리즈 레인지가 무력화된다.
                if (previousTarget != null
                    && IsAliveTarget(previousTarget)
                    && Vector3.Distance(_player.position, previousTarget.position) <= GetReleaseRange())
                {
                    _currentIndex = _targets.IndexOf(previousTarget);
                    return true;
                }

                return false;
            }

            NotifyUnLockOn(previousTarget);
            SetTarget(nextTarget);
            _currentIndex = _targets.IndexOf(nextTarget);
            return true;
        }

        private void CollectTargets(bool requireLineOfSight)
        {
            Vector3 origin = _player.position;
            _targets.Clear();
            _targetSet.Clear();

            Vector3 priorityForwardXZ = GetPriorityForwardXZ();

            float maxRange = Mathf.Max(_settings.lockOnRange, 0.001f);
            // 카메라 방향 가중치: 같은 거리라도 정면에 있는 대상이 먼저 선택됨
            // 0~1 사이 값. 높을수록 카메라 방향 우선순위 강화
            const float cameraWeight = 0.5f;

            var infos = new List<TargetInfo>();

            CollectTargetCandidates(
                Physics.OverlapSphere(origin, _settings.lockOnRange, _lockOnLayer),
                origin,
                priorityForwardXZ,
                maxRange,
                cameraWeight,
                requireLineOfSight,
                infos);

            if (infos.Count == 0)
            {
                CollectTargetCandidates(
                    Physics.OverlapSphere(origin, _settings.lockOnRange),
                    origin,
                    priorityForwardXZ,
                    maxRange,
                    cameraWeight,
                    requireLineOfSight,
                    infos);
            }

            infos.Sort((a, b) => a.sortScore.CompareTo(b.sortScore));
            foreach (var info in infos)
                _targets.Add(info.transform);
        }

        private void CollectTargetCandidates(
            Collider[] hits,
            Vector3 origin,
            Vector3 priorityForwardXZ,
            float maxRange,
            float cameraWeight,
            bool requireLineOfSight,
            List<TargetInfo> infos)
        {
            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                if (hit.transform == _player || hit.transform.IsChildOf(_player))
                    continue;

                ILockOnTarget lockOnTarget = ResolveLockOnTarget(hit);
                bool hasRuntimeTarget = CameraRuntimeServices.Adapter.TryResolveTarget(
                    hit,
                    out CameraTargetInfo runtimeTarget);
                if (hasRuntimeTarget
                    && (!runtimeTarget.IsAlive || !runtimeTarget.IsHostileToPlayer))
                    continue;
                if (!hasRuntimeTarget && lockOnTarget == null)
                    continue;
                if (hasRuntimeTarget
                    && runtimeTarget.Root != null
                    && (runtimeTarget.Root == _player || runtimeTarget.Root.IsChildOf(_player)))
                {
                    continue;
                }
                if (lockOnTarget != null && !lockOnTarget.CanLockOn)
                    continue;

                Transform candidate = ResolveTargetTransform(
                    hit,
                    hasRuntimeTarget,
                    runtimeTarget,
                    lockOnTarget);
                if (candidate == null)
                    continue;
                if (_targetSet.Contains(candidate))
                    continue;

                if (requireLineOfSight && _settings.lockOnRequireLineOfSight && !HasLineOfSight(candidate))
                    continue;

                _targetSet.Add(candidate);

                Vector3 p = candidate.position;
                Vector3 toTargetXZ = new Vector3(p.x - origin.x, 0f, p.z - origin.z);
                float distXZ = toTargetXZ.magnitude;
                float dSq = distXZ * distXZ;

                // distScore: 0(바로 옆) ~ 1(최대 사거리)
                float distScore = distXZ / maxRange;

                // angleScore: 0(기준 방향 정면) ~ 1(기준 방향 뒤쪽)
                float dot = distXZ > 0.001f
                    ? Vector3.Dot(priorityForwardXZ, toTargetXZ / distXZ)
                    : 1f;
                float angleScore = (1f - dot) * 0.5f;

                var candidateInfo = new LockOnCandidate(
                    candidate,
                    p,
                    distXZ,
                    distScore,
                    angleScore,
                    candidate == CurrentTarget,
                    lockOnTarget != null ? lockOnTarget.LockOnPriority : 0f);
                float sortScore = EvaluateTargetScore(candidateInfo, cameraWeight);

                if (_camera != null)
                {
                    Vector3 viewport = _camera.WorldToViewportPoint(p);
                    bool outsideView = viewport.z <= 0f
                                       || viewport.x < 0f || viewport.x > 1f
                                       || viewport.y < 0f || viewport.y > 1f;
                    if (outsideView)
                        sortScore += 1f;
                }

                infos.Add(new TargetInfo { transform = candidate, distanceSq = dSq, sortScore = sortScore });
            }
        }

        private float EvaluateTargetScore(in LockOnCandidate candidate, float directionWeight)
        {
            float score = _settings.lockOnPriorityMode switch
            {
                LockOnPriorityMode.Distance => candidate.distanceScore,
                LockOnPriorityMode.MovementDirection => candidate.distanceScore + candidate.angleScore * directionWeight,
                _ => candidate.distanceScore + candidate.angleScore * directionWeight
            };

            if (candidate.isCurrentTarget)
                score -= Mathf.Max(0f, _settings.lockOnCurrentTargetBonus);
            score -= Mathf.Max(0f, candidate.lockOnPriority);

            return score;
        }

        private Transform SelectSwitchTarget(int direction)
        {
            if (_camera == null || _player == null || CurrentTarget == null)
                return null;

            _targets.RemoveAll(t => t == null || !IsValidTarget(t));
            if (_targets.Count == 0) { Release(); return null; }

            Vector3 currentViewport = _camera.WorldToViewportPoint(CurrentTarget.position);
            Transform best = FindDirectionalSwitchCandidate(direction, currentViewport.x, allowWrap: false);
            if (best == null && _settings.lockOnSwitchWrap)
                best = FindDirectionalSwitchCandidate(direction, currentViewport.x, allowWrap: true);

            return best;
        }

        private Transform FindDirectionalSwitchCandidate(int direction, float currentX, bool allowWrap)
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            float maxRange = Mathf.Max(_settings.lockOnRange, 0.001f);
            float dir = Mathf.Sign(direction == 0 ? 1 : direction);

            foreach (Transform candidate in _targets)
            {
                if (candidate == null || candidate == CurrentTarget)
                    continue;

                Vector3 viewport = _camera.WorldToViewportPoint(candidate.position);
                if (viewport.z <= 0f)
                    continue;

                float deltaX = viewport.x - currentX;
                bool isDirectional = dir > 0f ? deltaX > 0.001f : deltaX < -0.001f;
                if (!isDirectional && !allowWrap)
                    continue;

                float screenGap = allowWrap && !isDirectional
                    ? 1f + Mathf.Abs(deltaX)
                    : Mathf.Abs(deltaX);
                float centerGap = Mathf.Abs(viewport.x - 0.5f);
                float distScore = Vector3.Distance(_player.position, candidate.position) / maxRange;
                float score =
                    screenGap * Mathf.Max(0f, _settings.lockOnSwitchScreenWeight)
                    + centerGap * Mathf.Max(0f, _settings.lockOnSwitchCenterWeight)
                    + distScore * Mathf.Max(0f, _settings.lockOnSwitchDistanceWeight);

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private bool IsValidTarget(Transform t)
        {
            if (t == null) return false;
            if (Vector3.Distance(_player.position, t.position) > _settings.lockOnRange) return false;
            return IsAliveTarget(t);
        }

        private static bool IsAliveTarget(Transform t)
        {
            if (t == null) return false;

            if (CameraRuntimeServices.Adapter.TryResolveTarget(
                    t,
                    out CameraTargetInfo target))
            {
                return target.IsAlive && target.IsHostileToPlayer;
            }

            ILockOnTarget lockOnTarget = GetLockOnTarget(t);
            return lockOnTarget != null && lockOnTarget.CanLockOn;
        }

        private float GetReleaseRange()
        {
            return Mathf.Max(_settings.lockOnRange, _settings.lockOnReleaseRange);
        }

        private bool HasLineOfSight(Transform target)
        {
            if (target == null || _lineOfSightLayer.value == 0)
                return true;

            Vector3 origin = _camera != null
                ? _camera.transform.position
                : _player != null
                    ? _player.position + Vector3.up * 1.4f
                    : Vector3.zero;
            Vector3 focus = GetTargetFocusPosition(target);
            Vector3 toFocus = focus - origin;
            float distance = toFocus.magnitude;
            if (distance <= 0.01f)
                return true;

            Vector3 direction = toFocus / distance;
            float radius = Mathf.Max(0f, _settings.lockOnLineOfSightRadius);
            bool blocked = radius > 0f
                ? Physics.SphereCast(origin, radius, direction, out RaycastHit sphereHit, distance, _lineOfSightLayer, QueryTriggerInteraction.Ignore)
                  && IsBlockingLineOfSightHit(sphereHit.transform, target, _player)
                : Physics.Raycast(origin, direction, out RaycastHit rayHit, distance, _lineOfSightLayer, QueryTriggerInteraction.Ignore)
                  && IsBlockingLineOfSightHit(rayHit.transform, target, _player);

            return !blocked;
        }

        /// <summary>
        /// 거리 피팅 프레이밍(LockOnFitDistance)용 대상 좌표를 제공한다.
        /// focus = 추적 포커스(하반신), top = 대상 콜라이더 월드 상단 + 머리 위 여백.
        /// 락온 비활성 또는 대상 없음이면 false.
        /// </summary>
        public bool TryGetTargetFramingPoints(float topPadding, out Vector3 focus, out Vector3 top)
        {
            focus = Vector3.zero;
            top = Vector3.zero;
            if (!IsActive || CurrentTarget == null)
                return false;

            focus = GetCurrentTargetFocusPosition();
            top = focus;
            // bounds는 월드 공간이라 비행/대형 대상의 실제 상단을 그대로 반영한다.
            top.y = (_targetCollider != null ? _targetCollider.bounds.max.y : focus.y + 1f)
                    + Mathf.Max(0f, topPadding);
            return true;
        }

        private Vector3 GetTargetFocusPosition(Transform target)
        {
            if (target == null)
                return Vector3.zero;

            ILockOnTarget lockOnTarget = GetLockOnTarget(target);
            if (lockOnTarget != null)
                return lockOnTarget.FocusPosition;

            CapsuleCollider capsule = target.GetComponent<CapsuleCollider>()
                                      ?? target.GetComponentInChildren<CapsuleCollider>();
            float h = capsule != null ? capsule.height * 0.25f : 1f;
            Vector3 pos = target.position;
            pos.y -= h;
            return pos;
        }

        private static bool IsBlockingLineOfSightHit(Transform hit, Transform target, Transform player)
        {
            if (hit == null || target == null)
                return false;

            if (hit == target || hit.IsChildOf(target) || target.IsChildOf(hit))
                return false;

            if (player != null && (hit == player || hit.IsChildOf(player) || player.IsChildOf(hit)))
                return false;

            return true;
        }

        private static Transform ResolveTargetTransform(
            Collider hit,
            bool hasRuntimeTarget,
            CameraTargetInfo runtimeTarget,
            ILockOnTarget lockOnTarget)
        {
            if (lockOnTarget != null && lockOnTarget.Transform != null)
                return lockOnTarget.Transform;

            if (hasRuntimeTarget && runtimeTarget.Root != null)
                return runtimeTarget.Root;

            return hit != null ? hit.transform : null;
        }

        private static ILockOnTarget ResolveLockOnTarget(Collider hit)
        {
            if (hit == null)
                return null;

            return hit.GetComponent<ILockOnTarget>() ?? hit.GetComponentInParent<ILockOnTarget>();
        }

        private static ILockOnTarget GetLockOnTarget(Transform target)
        {
            if (target == null)
                return null;

            return target.GetComponent<ILockOnTarget>() ?? target.GetComponentInParent<ILockOnTarget>();
        }

        private Vector3 GetPriorityForwardXZ()
        {
            if (_settings.lockOnPriorityMode == LockOnPriorityMode.MovementDirection && _playerVelocityProvider != null)
            {
                Vector3 velocity = _playerVelocityProvider.Invoke();
                velocity.y = 0f;
                if (velocity.sqrMagnitude > 0.01f)
                    return velocity.normalized;
            }

            if (_camera != null)
            {
                Vector3 camForwardXZ = _camera.transform.forward;
                camForwardXZ.y = 0f;
                if (camForwardXZ.sqrMagnitude > 0.001f)
                    return camForwardXZ.normalized;
            }

            return Vector3.forward;
        }

        private static void NotifyUnLockOn(Transform t)
        {
            if (t != null)
                CameraRuntimeServices.Adapter.NotifyLockOnChanged(t, false);
        }
    }
}
