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

        // 내부 상태
        private CapsuleCollider _targetCollider;
        private readonly List<Transform> _targets = new List<Transform>();
        private int _currentIndex = -1;
        private float _lastSwitchTime;

        // Y축 스무딩
        private float _smoothY;
        private float _yVelocity;

        // 해제 전환 연출
        private bool _isTransitioning;
        private float _transitionTimer;
        private float _transitionYaw, _transitionPitch;

        // 대상 정렬용 임시 구조체
        private struct TargetInfo
        {
            public Transform transform;
            public float distanceSq;
        }

        public CameraLockOn(CameraSettings settings, Transform player, UnityEngine.Camera camera, LayerMask lockOnLayer)
        {
            _settings = settings;
            _player = player;
            _camera = camera;
            _lockOnLayer = lockOnLayer;
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
            _yVelocity = 0f;
            _isTransitioning = false;
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
                return false;

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

            // Y축 스무딩
            float heightOffset = _targetCollider != null ? _targetCollider.height * 0.25f : 1f;
            float rawY = CurrentTarget.position.y - heightOffset;
            _smoothY = Mathf.SmoothDamp(_smoothY, rawY, ref _yVelocity, _settings.lockOnYSmoothTime);

            Vector3 targetPos = new Vector3(CurrentTarget.position.x, _smoothY, CurrentTarget.position.z);

            // Mid-Point Camera
            Vector3 midPoint = Vector3.Lerp(_player.position, targetPos, _settings.lockOnMidPointWeight);

            // Yaw (XZ 평면)
            Vector3 dirXZ = new Vector3(midPoint.x - _player.position.x, 0f, midPoint.z - _player.position.z);
            float targetYaw = dirXZ.sqrMagnitude > 0.001f
                ? Mathf.Atan2(dirXZ.x, dirXZ.z) * Mathf.Rad2Deg
                : yaw;

            // Pitch (고저차 감쇠)
            float heightDiff = midPoint.y - _player.position.y;
            float hDist = dirXZ.magnitude;
            float rawPitch = 0f;
            if (hDist > 0.5f)
            {
                rawPitch = Mathf.Atan2(-heightDiff * _settings.lockOnHeightDampFactor, hDist) * Mathf.Rad2Deg;
            }

            float targetPitch = Mathf.Clamp(rawPitch, _settings.lockOnPitchMin, _settings.lockOnPitchMax);

            // 거리별 Pitch 제한
            float pitchLimit = Mathf.Lerp(_settings.lockOnPitchMax * 0.5f, _settings.lockOnPitchMax,
                Mathf.Clamp01((dist - 3f) / 7f));
            targetPitch = Mathf.Clamp(targetPitch, _settings.lockOnPitchMin, pitchLimit);

            // 보간
            yaw = Mathf.LerpAngle(yaw, targetYaw, Time.deltaTime * _settings.rotationSpeed);
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
        }

        private void InitSmoothY()
        {
            if (CurrentTarget == null) return;
            float h = _targetCollider != null ? _targetCollider.height * 0.25f : 1f;
            _smoothY = CurrentTarget.position.y - h;
            _yVelocity = 0f;
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

            var infos = new List<TargetInfo>();

            foreach (var hit in hits)
            {
                if (hit.transform == _player || hit.transform.IsChildOf(_player))
                    continue;

                var dmg = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.CanTakeDamage())
                    continue;

                Vector3 p = hit.transform.position;
                float dSq = (new Vector3(p.x, 0, p.z) - new Vector3(origin.x, 0, origin.z)).sqrMagnitude;
                infos.Add(new TargetInfo { transform = hit.transform, distanceSq = dSq });
            }

            infos.Sort((a, b) => a.distanceSq.CompareTo(b.distanceSq));
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
            return dmg != null && dmg.CanTakeDamage();
        }

        private static void NotifyUnLockOn(Transform t)
        {
            if (t != null) t.GetComponent<IDamageable>()?.UnLockOn();
        }
    }
}
