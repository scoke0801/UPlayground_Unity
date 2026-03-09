using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 카메라 시스템의 모든 튜닝 값을 담는 ScriptableObject.
    /// 에디터에서 실시간 조정 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "UPlayGround/Camera/Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("=== 기본 카메라 ===")]
        public Vector3 defaultOffset = new Vector3(0f, 1f, 0f);
        public Vector3 combatOffset = new Vector3(0.05f, 1f, 0f);
        public float offsetSmoothTime = 0.35f;

        [Header("거리")]
        public float defaultDistance = 4.2f;
        public float minDistance = 3.7f;
        public float maxDistance = 4.5f;

        [Header("회전")]
        public float rotationSpeed = 20f;
        public float minVerticalAngle = 0f;
        public float maxVerticalAngle = 50f;

        [Header("줌")]
        public float zoomSpeed = 0.5f;

        [Header("스무딩")]
        public float positionSmoothTime = 0.1f;
        public float rotationSmoothTime = 0.1f;

        [Header("=== 충돌 ===")]
        public float collisionOffset = 0.15f;
        public float cameraRadius = 0.25f;
        public float collisionReturnSpeed = 0.12f;

        [Header("=== FOV ===")]
        public float fovExplore = 45f;
        public float fovCombat = 50f;
        public float fovLockOn = 50f;
        public float fovSmoothTime = 0.25f;

        [Header("=== 카메라 정렬 ===")]
        public float alignSpeed = 3f;
        public float alignDuration = 0.5f;
        public float explorePitch = 25f;
        public float combatPitch = 25f;

        [Header("=== 락온 ===")]
        public float lockOnRange = 13f;
        public float lockOnDistance = 4.2f;
        public float lockOnMidPointWeight = 0.35f;
        public float lockOnYSmoothTime = 0.3f;
        public float lockOnTransitionDuration = 0.3f;
        public float targetSwitchCooldown = 0.15f;

        [Header("락온 고저차 감쇠")]
        public float lockOnHeightDampFactor = 0.42f;
        public float lockOnPitchMin = 15f;
        public float lockOnPitchMax = 80f;
        public float lockOnPitchSpeed = 8f;

        [Header("=== 다수 적 줌아웃 ===")]
        public float crowdZoomOutDistance = 7f;
        public float crowdDetectRadius = 10f;
        public int crowdEnemyThreshold = 3;
        public float crowdZoomSmoothTime = 0.4f;
    }
}
