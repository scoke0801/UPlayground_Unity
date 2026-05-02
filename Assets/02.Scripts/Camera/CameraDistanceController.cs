using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// FOV 전환 + 전투 거리 보정 + 다수 적 줌아웃을 통합 관리한다.
    /// CameraManager.OnLateUpdate에서 매 프레임 호출.
    /// </summary>
    public class CameraDistanceController
    {
        private readonly CameraSettings _s;
        private readonly Transform _player;
        private readonly LayerMask _lockOnLayer;

        // FOV
        private float _baseFOV;
        private float _targetFOV;
        private float _fovVelocity;
        public float BaseFOV => _baseFOV;

        // 군중 줌아웃
        private bool _crowdActive;
        private float _crowdDistance;
        private float _crowdVelocity;

        // 락온 거리 스무딩
        private bool _lockOnActive;
        private float _lockOnDistance;
        private float _lockOnVelocity;

        public CameraDistanceController(CameraSettings settings, Transform player, LayerMask lockOnLayer, float initialFOV)
        {
            _s = settings;
            _player = player;
            _lockOnLayer = lockOnLayer;
            _baseFOV = initialFOV;
            _targetFOV = settings.fovExplore;
            _crowdDistance = settings.defaultDistance;
            _lockOnDistance = settings.defaultDistance;
        }

        /// <summary>
        /// FOV를 상태에 맞게 부드럽게 전환한다.
        /// </summary>
        public void UpdateFOV(bool isLockOn, bool isCombat)
        {
            if (isLockOn)
                _targetFOV = _s.fovLockOn;
            else if (isCombat)
                _targetFOV = _s.fovCombat;
            else
                _targetFOV = _s.fovExplore;

            _baseFOV = Mathf.SmoothDamp(_baseFOV, _targetFOV, ref _fovVelocity, _s.fovSmoothTime);
        }

        /// <summary>
        /// 다수 적 줌아웃 + 전투/락온 거리 보정.
        /// 반환: 보정된 targetDistance. -1이면 유저 줌 유지.
        /// </summary>
        public float EvaluateDistance(bool isLockOn, bool isCombat, float currentTargetDist)
        {
            // 군중 줌아웃
            UpdateCrowdZoom(isLockOn, isCombat);

            if (isLockOn)
            {
                // 락온 진입 시 현재 거리에서 출발하도록 초기화
                if (!_lockOnActive)
                {
                    _lockOnDistance = currentTargetDist;
                    _lockOnVelocity = 0f;
                    _lockOnActive = true;
                }
                float target = Mathf.Clamp(_s.lockOnDistance, _s.minDistance, _s.maxDistance);
                _lockOnDistance = Mathf.SmoothDamp(_lockOnDistance, target, ref _lockOnVelocity, _s.lockOnTransitionDuration);
                return _lockOnDistance;
            }

            if (_lockOnActive)
            {
                _lockOnActive = false;
                _lockOnVelocity = 0f;
            }

            if (_crowdActive)
                return Mathf.Clamp(_crowdDistance, _s.minDistance, _s.maxDistance);

            return -1f; // 유저 줌 존중
        }

        private void UpdateCrowdZoom(bool isLockOn, bool isCombat)
        {
            if (isLockOn || !isCombat || _player == null)
            {
                _crowdActive = false;
                _crowdDistance = Mathf.SmoothDamp(_crowdDistance, _s.defaultDistance, ref _crowdVelocity, _s.crowdZoomSmoothTime);
                return;
            }

            int count = CountNearbyEnemies();

            if (count >= _s.crowdEnemyThreshold)
            {
                _crowdActive = true;
                _crowdDistance = Mathf.SmoothDamp(_crowdDistance, _s.crowdZoomOutDistance, ref _crowdVelocity, _s.crowdZoomSmoothTime);
            }
            else
            {
                _crowdActive = false;
                _crowdDistance = Mathf.SmoothDamp(_crowdDistance, _s.defaultDistance, ref _crowdVelocity, _s.crowdZoomSmoothTime);
            }
        }

        private int CountNearbyEnemies()
        {
            Collider[] hits = Physics.OverlapSphere(_player.position, _s.crowdDetectRadius, _lockOnLayer);
            int count = 0;
            foreach (var hit in hits)
            {
                if (hit.transform == _player || hit.transform.IsChildOf(_player))
                    continue;
                var dmg = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.CanTakeDamage())
                    count++;
            }
            return count;
        }
    }
}
