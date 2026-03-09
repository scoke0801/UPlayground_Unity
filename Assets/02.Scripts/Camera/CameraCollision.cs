using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 멀티레이 카메라 충돌 감지 + 거리 스무딩.
    /// SphereCast의 시작점 겹침 문제를 회피하기 위해
    /// 5개 Ray(center + 사각 4모서리)를 사용한다.
    /// </summary>
    public class CameraCollision
    {
        private readonly CameraSettings _settings;
        private readonly Transform _target;

        private float _collisionDistance;
        private float _collisionDistanceVel;
        private LayerMask _collisionLayers;

        // 당김은 즉시, 복귀는 부드럽게
        private const float PULL_SPEED = 0f;

        public CameraCollision(CameraSettings settings, Transform target, LayerMask collisionLayers, float initialDistance)
        {
            _settings = settings;
            _target = target;
            _collisionLayers = collisionLayers;
            _collisionDistance = initialDistance;
        }

        /// <summary>
        /// 충돌을 고려한 실제 카메라 배치 거리를 반환한다.
        /// </summary>
        public float Evaluate(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            float blockedDistance = GetRaycastDistance(pivot, camDir, desiredDistance);

            float smoothTime = blockedDistance < _collisionDistance
                ? PULL_SPEED
                : _settings.collisionReturnSpeed;

            _collisionDistance = smoothTime > 0f
                ? Mathf.SmoothDamp(_collisionDistance, blockedDistance, ref _collisionDistanceVel, smoothTime)
                : blockedDistance;

            return _collisionDistance;
        }

        public void ResetDistance(float distance)
        {
            _collisionDistance = distance;
            _collisionDistanceVel = 0f;
        }

        private float GetRaycastDistance(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            Vector3 right = Vector3.Cross(Vector3.up, camDir).normalized;
            Vector3 up = Vector3.Cross(camDir, right).normalized;
            float r = _settings.cameraRadius;

            // center + 사각 4모서리
            Vector3[] offsets =
            {
                Vector3.zero,
                (right + up) * r,
                (-right + up) * r,
                (right - up) * r,
                (-right - up) * r,
            };

            float minDist = desiredDistance;

            foreach (Vector3 offset in offsets)
            {
                if (Physics.Raycast(pivot + offset, camDir, out RaycastHit hit, desiredDistance, _collisionLayers))
                {
                    if (hit.transform == _target || hit.transform.IsChildOf(_target))
                        continue;

                    float safe = Mathf.Max(hit.distance - _settings.collisionOffset, 0f);
                    if (safe < minDist)
                        minDist = safe;
                }
            }

            return minDist;
        }
    }
}
