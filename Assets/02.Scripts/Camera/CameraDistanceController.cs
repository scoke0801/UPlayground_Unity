using System.Collections.Generic;
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
        private System.Func<Vector3> _playerVelocityProvider;

        // FOV
        private float _baseFOV;
        private float _targetFOV;
        private float _fovVelocity;
        public float BaseFOV => _baseFOV;

        // 군중 줌아웃
        private bool _crowdActive;
        private float _crowdDistance;
        private float _crowdVelocity;

        // 대형 몬스터 시야 확장
        private bool _sizeDistanceActive;
        private float _sizeDistance;
        private float _sizeDistanceVelocity;

        // 락온 거리 스무딩
        private bool _lockOnActive;
        private float _lockOnDistance;
        private float _lockOnVelocity;

        // UpdateFOV/EvaluateDistance가 같은 프레임에 중복 물리 쿼리하지 않게 한다.
        private int _nearbyMetricsFrame = -1;
        private int _nearbyEnemyCount;
        private float _nearbyMaxMonsterSize;
        private readonly HashSet<Transform> _metricTargets = new HashSet<Transform>();
        private readonly Collider[] _nearbyHitBuffer = new Collider[96];
        private readonly List<Collider> _colliderBuffer = new List<Collider>(16);

        public CameraDistanceController(CameraSettings settings, Transform player, LayerMask lockOnLayer, float initialFOV)
        {
            _s = settings;
            _player = player;
            _lockOnLayer = lockOnLayer;
            _baseFOV = initialFOV;
            _targetFOV = settings.fovExplore;
            _crowdDistance = settings.defaultDistance;
            _sizeDistance = settings.defaultDistance;
            _lockOnDistance = settings.defaultDistance;
        }

        /// <summary>
        /// FOV를 상태에 맞게 부드럽게 전환한다.
        /// </summary>
        public void SetPlayerVelocityProvider(System.Func<Vector3> provider)
        {
            _playerVelocityProvider = provider;
        }

        public void UpdateFOV(bool isLockOn, bool isCombat)
        {
            UpdateNearbyEnemyMetrics(isCombat);

            float baseTarget;
            if (isLockOn)
                baseTarget = _s.fovLockOn;
            else if (isCombat)
                baseTarget = _s.fovCombat;
            else
                baseTarget = _s.fovExplore;

            float addFov = 0f;
            if (_s.enableSpeedFOV && _playerVelocityProvider != null)
            {
                Vector3 velocity = _playerVelocityProvider.Invoke();
                float speed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
                addFov = Mathf.Clamp01(speed / Mathf.Max(_s.speedForMaxFOV, 0.01f)) * _s.speedFOVMax;
            }

            if (_s.enableMonsterSizeFOV && isCombat)
                addFov += EvaluateMonsterSizeFactor() * _s.monsterSizeFOVMax;

            _targetFOV = baseTarget + addFov;
            float smoothTime = _s.enableSpeedFOV ? _s.speedFOVSmoothTime : _s.fovSmoothTime;
            _baseFOV = Mathf.SmoothDamp(_baseFOV, _targetFOV, ref _fovVelocity, smoothTime);
        }

        /// <summary>
        /// 다수 적 줌아웃 + 대형 몬스터 시야 확장 + 전투/락온 거리 보정.
        /// 반환: 보정된 targetDistance. -1이면 유저 줌 유지.
        /// </summary>
        public float EvaluateDistance(bool isLockOn, bool isCombat, float currentTargetDist)
        {
            UpdateNearbyEnemyMetrics(isCombat);
            UpdateCrowdZoom(isCombat);
            UpdateMonsterSizeDistance(isCombat);

            if (isLockOn)
            {
                // 락온 진입 시 현재 거리에서 출발하도록 초기화
                if (!_lockOnActive)
                {
                    _lockOnDistance = currentTargetDist;
                    _lockOnVelocity = 0f;
                    _lockOnActive = true;
                }

                float target = _s.lockOnDistance;
                if (_crowdActive)
                    target = Mathf.Max(target, _crowdDistance);
                if (_sizeDistanceActive)
                    target = Mathf.Max(target, _sizeDistance);

                target = Mathf.Clamp(target, _s.minDistance, _s.maxDistance);
                _lockOnDistance = Mathf.SmoothDamp(_lockOnDistance, target, ref _lockOnVelocity, _s.lockOnTransitionDuration);
                return _lockOnDistance;
            }

            if (_lockOnActive)
            {
                _lockOnActive = false;
                _lockOnVelocity = 0f;
            }

            if (_crowdActive || _sizeDistanceActive)
            {
                float target = _crowdActive ? _crowdDistance : _s.defaultDistance;
                if (_sizeDistanceActive)
                    target = Mathf.Max(target, _sizeDistance);
                return Mathf.Clamp(target, _s.minDistance, _s.maxDistance);
            }

            return -1f; // 유저 줌 존중
        }

        private void UpdateCrowdZoom(bool isCombat)
        {
            if (!isCombat || _player == null)
            {
                _crowdActive = false;
                _crowdDistance = Mathf.SmoothDamp(_crowdDistance, _s.defaultDistance, ref _crowdVelocity, _s.crowdZoomSmoothTime);
                return;
            }

            if (_nearbyEnemyCount >= _s.crowdEnemyThreshold)
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

        private void UpdateMonsterSizeDistance(bool isCombat)
        {
            if (!isCombat || !_s.enableMonsterSizeFOV || _player == null)
            {
                _sizeDistanceActive = false;
                _sizeDistance = Mathf.SmoothDamp(
                    _sizeDistance,
                    _s.defaultDistance,
                    ref _sizeDistanceVelocity,
                    _s.monsterSizeDistanceSmoothTime);
                return;
            }

            float sizeFactor = EvaluateMonsterSizeFactor();
            _sizeDistanceActive = sizeFactor > 0.001f;
            float target = _s.defaultDistance + sizeFactor * _s.monsterSizeDistanceMax;
            _sizeDistance = Mathf.SmoothDamp(
                _sizeDistance,
                target,
                ref _sizeDistanceVelocity,
                _s.monsterSizeDistanceSmoothTime);
        }

        private float EvaluateMonsterSizeFactor()
        {
            float minSize = Mathf.Max(0.01f, _s.monsterSizeReference);
            float maxSize = Mathf.Max(minSize + 0.01f, _s.monsterSizeForMaxFOV);
            return Mathf.InverseLerp(minSize, maxSize, _nearbyMaxMonsterSize);
        }

        private void UpdateNearbyEnemyMetrics(bool isCombat)
        {
            if (_nearbyMetricsFrame == Time.frameCount)
                return;

            _nearbyMetricsFrame = Time.frameCount;
            _nearbyEnemyCount = 0;
            _nearbyMaxMonsterSize = 0f;
            _metricTargets.Clear();

            if (!isCombat || _player == null)
                return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                _player.position,
                _s.crowdDetectRadius,
                _nearbyHitBuffer,
                _lockOnLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _nearbyHitBuffer[i];
                if (hit == null)
                    continue;

                if (hit.transform == _player || hit.transform.IsChildOf(_player))
                    continue;

                var dmg = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive())
                    continue;

                Transform metricRoot = ResolveMetricRoot(hit, dmg);
                if (metricRoot == null || !_metricTargets.Add(metricRoot))
                    continue;

                _nearbyEnemyCount++;
                _nearbyMaxMonsterSize = Mathf.Max(_nearbyMaxMonsterSize, EvaluateTargetMaxSize(metricRoot, hit));
            }
        }

        private static Transform ResolveMetricRoot(Collider hit, IDamageable damageable)
        {
            MonsterActor monster = hit.GetComponentInParent<MonsterActor>();
            if (monster != null)
                return monster.transform;

            if (damageable is UnityEngine.Component component)
                return component.transform;

            return hit.transform;
        }

        private float EvaluateTargetMaxSize(Transform root, Collider fallbackCollider)
        {
            float maxSize = 0f;
            _colliderBuffer.Clear();
            root.GetComponentsInChildren(_colliderBuffer);
            foreach (var collider in _colliderBuffer)
            {
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;

                Vector3 size = collider.bounds.size;
                maxSize = Mathf.Max(maxSize, size.x, size.y, size.z);
            }

            if (maxSize <= 0f && fallbackCollider != null)
            {
                Vector3 size = fallbackCollider.bounds.size;
                maxSize = Mathf.Max(size.x, size.y, size.z);
            }

            _colliderBuffer.Clear();
            return maxSize;
        }
    }
}
