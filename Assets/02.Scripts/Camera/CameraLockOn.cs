using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// LockOn 시스템 전체 로직: 대상 탐색/전환/해제, 추적 회전(Mid-Point Camera), 전환 연출.
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
        private System.Func<Vector3> _playerVelocityProvider;
        private System.Func<(bool isColliding, float sustainedSec)> _collisionTelemetryProvider;

        // 내부 상태
        private CapsuleCollider _targetCollider;
        private readonly List<Transform> _targets = new List<Transform>();
        private int _currentIndex = -1;
        private float _lastSwitchTime;

        // 포커스 스무딩
        private float _smoothY;

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
        private float _lastSideFlipTime = -999f;
        private float _sideFlipSignOverride;

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

        public CameraLockOn(CameraSettings settings, Transform player, UnityEngine.Camera camera, LayerMask lockOnLayer)
        {
            _settings = settings;
            _player = player;
            _camera = camera;
            _lockOnLayer = lockOnLayer;
        }

        public void SetPlayerVelocityProvider(System.Func<Vector3> provider)
        {
            _playerVelocityProvider = provider;
        }

        public void SetCollisionTelemetryProvider(System.Func<(bool isColliding, float sustainedSec)> provider)
        {
            _collisionTelemetryProvider = provider;
        }

        // ── 토글 ──

        /// <summary>
        /// 락온 시도. 성공 시 true.
        /// </summary>
        public bool TryActivate()
        {
            CollectTargets();
            if (_targets.Count == 0) return false;

            _currentIndex = 0;
            SetTarget(_targets[0]);
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
            _currentIndex = -1;
            _isTransitioning = false;
            _orbitInitialized = false;
            _wasSkipping = false;
            _signedOffsetAngle = 0f;
            _offsetAngleVelocity = 0f;
            _freeFactor = 0f;
            _freeFactorVelocity = 0f;
            _activeFocusVelocity = Vector3.zero;
            _lastSideFlipTime = -999f;
            _sideFlipSignOverride = 0f;
        }

        // ── 대상 전환 ──

        public void SwitchTarget(int direction)
        {
            if (_targets.Count <= 1) return;
            if (Time.time - _lastSwitchTime < _settings.targetSwitchCooldown) return;

            CollectTargets();
            SortByScreenX();

            _currentIndex = Mathf.Clamp(_currentIndex + direction, 0, _targets.Count - 1);

            NotifyUnLockOn(CurrentTarget);
            SetTarget(_targets[_currentIndex]);
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
            if (!IsValidTarget(CurrentTarget) && skipRotation)
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
            if (!IsValidTarget(CurrentTarget))
            {
                if (!TryFindNext())
                {
                    StartTransition(yaw, pitch);
                    return false;
                }
            }

            // 거리 체크
            float dist = Vector3.Distance(_player.position, CurrentTarget.position);
            if (dist > _settings.lockOnRange)
            {
                if (!TryFindNext())
                {
                    StartTransition(yaw, pitch);
                }
                return false;
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
            ClearSideFlipOverrideIfRecovered();
            if (Mathf.Abs(_sideFlipSignOverride) > 0.5f)
                sign = _sideFlipSignOverride;
            float targetSignedAngle = targetMag * sign;
            _lastEnemyYaw = enemyYaw;
            bool sideFlipTriggered = TryTriggerSideFlip(ref targetSignedAngle);

            // 적응형 SmoothDamp: 차이가 클수록 빠르게 수렴
            float offsetDelta = Mathf.Abs(targetSignedAngle - _signedOffsetAngle);
            float adaptiveSmoothTime = Mathf.Lerp(
                _settings.lockOnOrbitSmoothTime * ORBIT_SMOOTH_MIN_MULT,
                _settings.lockOnOrbitSmoothTime,
                1f - Mathf.Clamp01(offsetDelta / ORBIT_OFFSET_MAX_DELTA));
            float pullSmoothTime = sideFlipTriggered
                ? _settings.sideFlipSmoothTime
                : Mathf.Lerp(adaptiveSmoothTime, ORBIT_FREE_PULL_MAX_SMOOTH, freeFactor);
            _signedOffsetAngle = Mathf.SmoothDamp(
                _signedOffsetAngle, targetSignedAngle, ref _offsetAngleVelocity, pullSmoothTime);
            yaw = enemyYaw + _signedOffsetAngle;

            // Pitch (고저차 감쇠, target 직접 기준)
            float heightDiff = _smoothY - _player.position.y;
            float rawPitch = 0f;
            if (flatDist > 0.5f)
            {
                rawPitch = Mathf.Atan2(-heightDiff * _settings.lockOnHeightDampFactor, flatDist) * Mathf.Rad2Deg;
            }

            float targetPitch = Mathf.Clamp(rawPitch, _settings.lockOnPitchMin, _settings.lockOnPitchMax);

            // 거리별 Pitch 제한
            float pitchLimit = Mathf.Lerp(_settings.lockOnPitchMax * 0.5f, _settings.lockOnPitchMax,
                Mathf.Clamp01((dist - 3f) / 7f));
            targetPitch = Mathf.Clamp(targetPitch, _settings.lockOnPitchMin, pitchLimit);

            pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * _settings.lockOnPitchSpeed);
            pitch = Mathf.Clamp(pitch, _settings.minVerticalAngle, _settings.maxVerticalAngle);

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

        // ── 내부 헬퍼 ──

        private void SetTarget(Transform t)
        {
            CurrentTarget = t;
            _targetCollider = t.GetComponent<CapsuleCollider>();
            t.GetComponent<IDamageable>()?.LockOn();
            InitSmoothY();
            _activeFocusPos = GetCurrentTargetFocusPosition();
            _activeFocusVelocity = Vector3.zero;
            _sideFlipSignOverride = 0f;
            _orbitInitialized = false;
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

            float h = _targetCollider != null ? _targetCollider.height * 0.25f : 1f;
            Vector3 pos = CurrentTarget.position;
            pos.y -= h;
            return pos;
        }

        private void StartTransition(float yaw, float pitch)
        {
            _isTransitioning = true;
            _transitionTimer = _settings.lockOnTransitionDuration;
            _transitionYaw = yaw;
            _transitionPitch = pitch;
        }

        private bool TryFindNext()
        {
            NotifyUnLockOn(CurrentTarget);
            CollectTargets();
            if (_targets.Count == 0) return false;

            SetTarget(_targets[0]);
            _currentIndex = 0;
            return true;
        }

        private void CollectTargets()
        {
            Vector3 origin = _player.position;
            Collider[] hits = Physics.OverlapSphere(origin, _settings.lockOnRange, _lockOnLayer);
            _targets.Clear();

            Vector3 priorityForwardXZ = GetPriorityForwardXZ();

            float maxRange = Mathf.Max(_settings.lockOnRange, 0.001f);
            // 카메라 방향 가중치: 같은 거리라도 정면에 있는 대상이 먼저 선택됨
            // 0~1 사이 값. 높을수록 카메라 방향 우선순위 강화
            const float cameraWeight = 0.5f;

            var infos = new List<TargetInfo>();

            foreach (var hit in hits)
            {
                if (hit.transform == _player || hit.transform.IsChildOf(_player))
                    continue;

                var dmg = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive())
                    continue;

                Vector3 p = hit.transform.position;
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

                float sortScore = _settings.lockOnPriorityMode switch
                {
                    LockOnPriorityMode.Distance => distScore,
                    LockOnPriorityMode.MovementDirection => distScore + angleScore * cameraWeight,
                    _ => distScore + angleScore * cameraWeight
                };

                if (_camera != null)
                {
                    Vector3 viewport = _camera.WorldToViewportPoint(p);
                    bool outsideView = viewport.z <= 0f
                                       || viewport.x < 0f || viewport.x > 1f
                                       || viewport.y < 0f || viewport.y > 1f;
                    if (outsideView)
                        sortScore += 1f;
                }

                infos.Add(new TargetInfo { transform = hit.transform, distanceSq = dSq, sortScore = sortScore });
            }

            infos.Sort((a, b) => a.sortScore.CompareTo(b.sortScore));
            foreach (var info in infos)
                _targets.Add(info.transform);
        }

        private void SortByScreenX()
        {
            if (_camera == null || _player == null) return;

            _targets.RemoveAll(t => t == null || !IsValidTarget(t));
            if (_targets.Count == 0) { Release(); return; }

            Transform prev = CurrentTarget;
            _targets.Sort((a, b) =>
            {
                float xa = _camera.WorldToScreenPoint(a.position).x;
                float xb = _camera.WorldToScreenPoint(b.position).x;
                return xa.CompareTo(xb);
            });

            _currentIndex = _targets.IndexOf(prev);
            if (_currentIndex == -1 && _targets.Count > 0)
            {
                _currentIndex = 0;
                CurrentTarget = _targets[0];
                _targetCollider = CurrentTarget.GetComponent<CapsuleCollider>();
            }
        }

        private bool IsValidTarget(Transform t)
        {
            if (t == null) return false;
            if (Vector3.Distance(_player.position, t.position) > _settings.lockOnRange) return false;
            var dmg = t.GetComponent<IDamageable>() ?? t.GetComponentInParent<IDamageable>();
            return dmg != null && dmg.IsAlive();
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

        private void ClearSideFlipOverrideIfRecovered()
        {
            if (Mathf.Abs(_sideFlipSignOverride) <= 0.5f || _collisionTelemetryProvider == null)
                return;

            var telemetry = _collisionTelemetryProvider.Invoke();
            if (!telemetry.isColliding)
                _sideFlipSignOverride = 0f;
        }

        private bool TryTriggerSideFlip(ref float targetSignedAngle)
        {
            if (!_settings.enableLockOnSideFlip || _collisionTelemetryProvider == null)
                return false;

            var telemetry = _collisionTelemetryProvider.Invoke();
            if (!telemetry.isColliding || telemetry.sustainedSec < _settings.sustainedCollisionSec)
                return false;

            if (Time.time - _lastSideFlipTime < _settings.sideFlipCooldown)
                return false;

            _lastSideFlipTime = Time.time;
            _offsetAngleVelocity = 0f;
            float currentSign = _signedOffsetAngle >= 0f ? 1f : -1f;
            _sideFlipSignOverride = -currentSign;
            targetSignedAngle = Mathf.Abs(targetSignedAngle) * _sideFlipSignOverride;
            return true;
        }

        private static void NotifyUnLockOn(Transform t)
        {
            if (t != null) t.GetComponent<IDamageable>()?.UnLockOn();
        }
    }
}
