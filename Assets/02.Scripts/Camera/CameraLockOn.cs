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
    /// 락온 대상의 선택·유지·가시성과 안정된 추적 포커스를 관리한다.
    /// 화면 구도는 CameraDeadZoneTracker가 별도로 계산한다.
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
        private float _lastSwitchTime;

        private ILockOnTarget _targetProvider;
        private float _targetLostTimer;
        private float _occludedTimer;
        private Vector3 _activeFocusPos;
        private Vector3 _activeFocusVelocity;
        private Vector3 _pivotOffset;
        private Vector3 _pivotOffsetVelocity;
        private readonly List<TargetInfo> _candidateInfos = new List<TargetInfo>(128);
        private readonly Collider[] _candidateColliders = new Collider[256];
        private readonly RaycastHit[] _visibilityHits = new RaycastHit[32];
        private CameraPose _stableView;
        private bool _hasStableView;

        /// <summary>지형 가림 중에는 마지막 가시 포커스를 유지한다.</summary>
        public Vector3 FocusPosition => _activeFocusPos;
        public bool CanTrack { get; private set; }
        // 대상 정렬용 임시 구조체
        private struct TargetInfo
        {
            public Transform transform;
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
            CollectTargets(requireLineOfSight: true);
            if (_targets.Count == 0) return false;
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
            return true;
        }

        public void Release()
        {
            NotifyUnLockOn(CurrentTarget);
            CurrentTarget = null;
            _targetCollider = null;
            IsActive = false;
            _targets.Clear();
            _targetProvider = null;
            CanTrack = false;
            _activeFocusVelocity = Vector3.zero;
            _targetLostTimer = 0f;
            _occludedTimer = 0f;
        }

        // ── 대상 전환 ──

        /// <summary>기존 좌우 전환 입력을 화면 방향으로 변환한다.</summary>
        public void SwitchTarget(int direction) => SwitchTarget(new Vector2(direction, 0f));

        /// <summary>화면에서 입력한 방향의 후보로 한 번 전환한다.</summary>
        public void SwitchTarget(Vector2 direction)
        {
            if (!IsActive || direction.sqrMagnitude < 0.001f) return;
            if (Time.unscaledTime - _lastSwitchTime < _settings.targetSwitchCooldown) return;

            CollectTargets(requireLineOfSight: true);
            if (_targets.Count == 0) return;

            Transform nextTarget = SelectSwitchTarget(direction);
            if (nextTarget == null || nextTarget == CurrentTarget)
                return;

            NotifyUnLockOn(CurrentTarget);
            SetTarget(nextTarget);
            _lastSwitchTime = Time.unscaledTime;
        }

        /// <summary>대상 유효성과 가시성을 검사하고 연출과 독립된 추적 위치를 갱신한다.</summary>
        public void UpdateTarget(float deltaTime, bool suspendTracking)
        {
            CanTrack = false;
            if (!IsActive)
                return;
            if (_player == null)
            {
                Release();
                return;
            }

            if (!IsAliveTarget(CurrentTarget))
            {
                bool canSwitch = CurrentTarget != null && CurrentTarget.gameObject.activeInHierarchy
                                 && CameraRuntimeServices.Adapter.TryResolveTarget(CurrentTarget, out CameraTargetInfo lostTarget)
                                 && !lostTarget.IsAlive;
                if (suspendTracking || !canSwitch || !_settings.lockOnAutoSwitchOnDeath
                    || !TryFindNext(requireLineOfSight: true))
                {
                    Release();
                    return;
                }
            }

            if (suspendTracking)
                return;

            float elapsed = Mathf.Max(0f, deltaTime);
            bool isInRange = Vector3.Distance(_player.position, CurrentTarget.position) <= GetReleaseRange();
            _targetLostTimer = isInRange ? 0f : _targetLostTimer + elapsed;
            if (!isInRange && _targetLostTimer >= Mathf.Max(0f, _settings.lockOnLostGraceTime))
            {
                Release();
                return;
            }

            bool isVisible = !_settings.lockOnRequireLineOfSight || HasLineOfSight(CurrentTarget);
            _occludedTimer = isVisible ? 0f : _occludedTimer + elapsed;
            if (!isVisible)
            {
                if (_occludedTimer >= Mathf.Max(0f, _settings.lockOnOcclusionGraceTime))
                    Release();
                return;
            }

            CanTrack = true;
            if (elapsed > 0f)
                _activeFocusPos = Vector3.SmoothDamp(_activeFocusPos, GetTargetFocusPosition(CurrentTarget),
                    ref _activeFocusVelocity, Mathf.Max(0.001f, _settings.lockOnFocusSmoothTime),
                    Mathf.Infinity, elapsed);
        }
        // ── Public 조회 ──

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
            _targetProvider = GetLockOnTarget(t);
            _activeFocusPos = GetCurrentTargetFocusPosition();
            _activeFocusVelocity = Vector3.zero;
            CanTrack = true;
            _occludedTimer = 0f;
            _targetLostTimer = 0f;
        }

        private Vector3 GetCurrentTargetFocusPosition()
        {
            if (CurrentTarget == null)
                return Vector3.zero;

            return GetTargetFocusPosition(CurrentTarget);
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
                    return true;
                }

                return false;
            }

            NotifyUnLockOn(previousTarget);
            SetTarget(nextTarget);
            return true;
        }

        private int CollectColliderCount(Vector3 origin)
        {
            return Physics.OverlapSphereNonAlloc(origin, _settings.lockOnRange, _candidateColliders,
                _lockOnLayer, QueryTriggerInteraction.Collide);
        }

        private void CollectTargets(bool requireLineOfSight)
        {
            Vector3 origin = _player.position;
            _targets.Clear();
            _targetSet.Clear();

            Vector3 priorityForwardXZ = GetPriorityForwardXZ();

            float maxRange = Mathf.Max(_settings.lockOnRange, 0.001f);
            float cameraWeight = Mathf.Max(0f, _settings.lockOnAcquireDirectionWeight);

            List<TargetInfo> infos = _candidateInfos;
            infos.Clear();

            CollectTargetCandidates(
                CollectColliderCount(origin),
                origin,
                priorityForwardXZ,
                maxRange,
                cameraWeight,
                requireLineOfSight,
                infos);

            infos.Sort((a, b) => a.sortScore.CompareTo(b.sortScore));
            foreach (var info in infos)
                _targets.Add(info.transform);
        }

        private void CollectTargetCandidates(
            int hitCount,
            Vector3 origin,
            Vector3 priorityForwardXZ,
            float maxRange,
            float cameraWeight,
            bool requireLineOfSight,
            List<TargetInfo> infos)
        {
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider hit = _candidateColliders[hitIndex];
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

                Vector3 p = GetTargetFocusPosition(candidate);
                Vector3 toTargetXZ = new Vector3(p.x - origin.x, 0f, p.z - origin.z);
                float distXZ = toTargetXZ.magnitude;

                // distScore: 0(바로 옆) ~ 1(최대 사거리)
                float distScore = distXZ / maxRange;

                // angleScore: 0(기준 방향 정면) ~ 1(기준 방향 뒤쪽)
                float dot = distXZ > 0.001f
                    ? Vector3.Dot(priorityForwardXZ, toTargetXZ / distXZ)
                    : 1f;
                float angleScore = (1f - dot) * 0.5f;
                Vector3 viewport = ProjectTarget(p);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f
                    || viewport.y < 0f || viewport.y > 1f)
                    continue;
                if (_settings.lockOnPriorityMode == LockOnPriorityMode.CameraDirection)
                    angleScore = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f).magnitude;

                var candidateInfo = new LockOnCandidate(
                    candidate,
                    p,
                    distXZ,
                    distScore,
                    angleScore,
                    candidate == CurrentTarget,
                    lockOnTarget != null ? lockOnTarget.LockOnPriority : 0f);
                float sortScore = EvaluateTargetScore(candidateInfo, cameraWeight);

                infos.Add(new TargetInfo { transform = candidate, sortScore = sortScore });
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

        private Transform SelectSwitchTarget(Vector2 direction)
        {
            if (_camera == null || _player == null || CurrentTarget == null)
                return null;

            Vector3 currentViewport = ProjectTarget(GetTargetFocusPosition(CurrentTarget));
            Vector2 origin = currentViewport.z > 0f ? (Vector2)currentViewport : new Vector2(0.5f, 0.5f);
            Transform best = FindDirectionalSwitchCandidate(direction.normalized, origin, allowWrap: false);
            if (best == null && _settings.lockOnSwitchWrap)
                best = FindDirectionalSwitchCandidate(direction.normalized, origin, allowWrap: true);

            return best;
        }

        private Transform FindDirectionalSwitchCandidate(Vector2 direction, Vector2 origin, bool allowWrap)
        {
            Transform best = null;
            float bestScore = float.MaxValue;
            float maxRange = Mathf.Max(_settings.lockOnRange, 0.001f);

            foreach (Transform candidate in _targets)
            {
                if (candidate == null || candidate == CurrentTarget)
                    continue;

                Vector3 viewport = ProjectTarget(GetTargetFocusPosition(candidate));
                if (viewport.z <= 0f)
                    continue;

                Vector2 delta = (Vector2)viewport - origin;
                float directionalDot = Vector2.Dot(delta.normalized, direction);
                bool isDirectional = directionalDot > 0.5f;
                if (!isDirectional && !allowWrap)
                    continue;

                float screenGap = allowWrap && !isDirectional
                    ? 1f + delta.magnitude
                    : delta.magnitude * (2f - directionalDot);
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
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            if (Vector3.Distance(_player.position, t.position) > _settings.lockOnRange) return false;
            return IsAliveTarget(t);
        }

        private bool IsAliveTarget(Transform t)
        {
            if (t == null || !t.gameObject.activeInHierarchy) return false;

            if (CameraRuntimeServices.Adapter.TryResolveTarget(
                    t,
                    out CameraTargetInfo target))
            {
                return target.IsAlive && target.IsHostileToPlayer
                       && (t != CurrentTarget || _targetProvider == null || _targetProvider.CanLockOn);
            }

            ILockOnTarget lockOnTarget = t == CurrentTarget ? _targetProvider : GetLockOnTarget(t);
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

            Vector3 origin = _hasStableView ? _stableView.CameraPosition : _camera != null
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
            int count = radius > 0f
                ? Physics.SphereCastNonAlloc(origin, radius, direction, _visibilityHits, distance,
                    _lineOfSightLayer, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origin, direction, _visibilityHits, distance,
                    _lineOfSightLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (IsBlockingLineOfSightHit(_visibilityHits[i].transform, target, _player))
                    return false;
            }
            // 버퍼가 가득 차면 벽을 놓쳤을 가능성이 있으므로 신규 획득을 보수적으로 제한한다.
            return count < _visibilityHits.Length;
        }

        /// <summary>
        /// 거리 피팅 프레이밍(LockOnFitDistance)용 대상 좌표를 제공한다.
        /// focus = 공통 추적 포커스, top = 대상 콜라이더 월드 상단 + 머리 위 여백.
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

            ILockOnTarget lockOnTarget = target == CurrentTarget ? _targetProvider : GetLockOnTarget(target);
            if (lockOnTarget != null)
                return lockOnTarget.FocusPosition;

            CapsuleCollider capsule = target == CurrentTarget ? _targetCollider
                : target.GetComponent<CapsuleCollider>() ?? target.GetComponentInChildren<CapsuleCollider>();
            return capsule != null ? capsule.bounds.center : target.position;
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

        /// <summary>선택·가림 검사가 렌더 흔들림에 반응하지 않도록 기본 구도를 보관한다.</summary>
        public void SetStableView(CameraPose pose)
        {
            _stableView = pose;
            _hasStableView = true;
        }

        private Vector3 ProjectTarget(Vector3 position)
        {
            if (!_hasStableView)
                return _camera != null ? _camera.WorldToViewportPoint(position) : Vector3.back;
            bool isVisible = CameraDeadZoneTracker.TryProject(_stableView, position,
                _camera != null ? _camera.aspect : 1f, out Vector2 viewport);
            return new Vector3(viewport.x, viewport.y, isVisible ? 1f : -1f);
        }
    }
}
