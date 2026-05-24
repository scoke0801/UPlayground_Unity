using UnityEngine;

namespace UPlayGround.Data
{
    public enum LockOnPriorityMode
    {
        MovementDirection,
        CameraDirection,
        Distance
    }

    /// <summary>
    /// 카메라 시스템의 모든 튜닝 값을 담는 ScriptableObject.
    /// 에디터에서 실시간 조정 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "UPlayGround/Camera/Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("=== 기본 카메라 ===")]
        public Vector3 defaultOffset = new Vector3(0f, 1f, 0f);
        public Vector3 combatOffset = new Vector3(0.25f, 1f, 0f);
        public float offsetSmoothTime = 0.35f;

        [Header("거리")]
        public float defaultDistance = 5f;
        public float combatDistance = 5.2f;
        public float minDistance = 3.2f;
        public float maxDistance = 7f;

        [Header("회전")]
        public float rotationSpeed = 20f;
        public float minVerticalAngle = -30f;   // 음수 = 위쪽, 양수 = 아래쪽
        public float maxVerticalAngle = 70f;

        [Header("경사 보정")]
        [Tooltip("경사면 감지 레이캐스트 거리")]
        public float slopeCheckDistance = 1.5f;
        [Tooltip("경사에 따라 피치 하한을 얼마나 끌어올릴지 (0 = 보정 없음, 1 = 완전 추종)")]
        [Range(0f, 1f)]
        public float slopePitchCorrectionStrength = 0.5f;
        [Tooltip("경사 보정 스무딩 속도")]
        public float slopeCorrectionSmoothTime = 0.3f;

        [Header("줌")]
        public float zoomSpeed = 0.5f;

        [Header("스무딩")]
        public float positionSmoothTime = 0.1f;
        public float rotationSmoothTime = 0.1f;

        [Header("=== 충돌 ===")]
        public float collisionOffset = 0.15f;
        public float cameraRadius = 0.25f;
        public float collisionReturnSpeed = 0.12f;

        [Header("=== 충돌 검사 (MultiProbe) ===")]
        public bool useMultiProbe = true;
        public int collisionProbeCount = 6;
        public float collisionSkinWidth = 0.08f;
        [Range(0f, 1f)]
        public float minNormalAlignment = 0.5f;

        [Header("=== FOV ===")]
        public float fovExplore = 50f;
        public float fovCombat = 54f;
        public float fovLockOn = 50f;
        public float fovSmoothTime = 0.25f;

        [Header("=== 동적 FOV (속도 기반) ===")]
        public bool enableSpeedFOV = true;
        public float speedFOVMax = 6f;
        public float speedForMaxFOV = 8f;
        public float speedFOVSmoothTime = 0.3f;

        [Header("=== Look-ahead (진행방향 선행) ===")]
        public bool enableLookAhead = true;
        public float lookAheadDistance = 1.2f;
        public float lookAheadSpeedRef = 5f;
        public float lookAheadSmoothTime = 0.25f;
        [Range(0f, 1f)]
        public float lockOnLookAheadMultiplier = 0.1f;

        [Header("=== Floor Rescue (바닥 보정) ===")]
        public bool enableFloorRescue = true;
        public float floorRescueDropThreshold = 1f;
        public float groundClearance = 0.3f;
        public LayerMask floorRescueLayerMask;

        [Header("=== 카메라 정렬 ===")]
        public float alignSpeed = 3f;
        public float alignDuration = 0.5f;
        public float explorePitch = 25f;
        public float combatPitch = 25f;

        [Header("=== 락온 ===")]
        public float lockOnRange = 13f;
        public float lockOnDistance = 4f;
        public float lockOnMidPointWeight = 0.35f;
        public float lockOnYSmoothTime = 0.3f;
        public float lockOnTransitionDuration = 0.3f;
        public float targetSwitchCooldown = 0.15f;

        [Header("=== 락온 포커스 스무딩 ===")]
        public float lockOnFocusSmoothTime = 0.15f;

        [Header("=== 락온 차폐 자동 리포지션 ===")]
        public bool enableLockOnSideFlip = true;
        public float sustainedCollisionSec = 0.4f;
        public float sideFlipCooldown = 1f;
        public float sideFlipSmoothTime = 0.2f;

        [Header("=== 락온 타겟팅 우선순위 ===")]
        public LockOnPriorityMode lockOnPriorityMode = LockOnPriorityMode.CameraDirection;

        [Header("락온 고저차 감쇠")]
        public float lockOnHeightDampFactor = 0.42f;
        public float lockOnPitchMin = 15f;
        public float lockOnPitchMax = 80f;
        public float lockOnPitchSpeed = 8f;

        [Header("락온 오비탈 각도 오프셋")]
        [Tooltip("거리에 따른 카메라 오프셋 각도 (x=거리, y=각도)")]
        public AnimationCurve lockOnOffsetAngleByDistance = new AnimationCurve(
            new Keyframe(0f, 15f), new Keyframe(8f, 25f), new Keyframe(15f, 15f));
        [Tooltip("최소 오프셋 각도 (가까운 거리에서 유지할 최소 각)")]
        public float lockOnMinOffsetAngle = 5f;
        [Tooltip("최대 오프셋 각도 (화면 이탈 방지 상한)")]
        public float lockOnMaxOffsetAngle = 40f;
        [Tooltip("자유 궤도 시작 거리 (이 거리부터 freeFactor 증가)")]
        public float freeOrbitStartDistance = 6f;
        [Tooltip("완전 자유 궤도 거리 (이 거리에서 freeFactor=1)")]
        public float freeOrbitFullDistance = 14f;
        [Tooltip("적 이동 시 오프셋이 따라가는 민감도 커브 (x=거리, y=0~1)")]
        public AnimationCurve lockOnOvercomeSensitivity = new AnimationCurve(
            new Keyframe(0f, 0.2f), new Keyframe(20f, 1f));
        [Tooltip("오비탈 오프셋 수렴 스무딩 시간")]
        public float lockOnOrbitSmoothTime = 0.15f;

        [Header("=== 다수 적 줌아웃 ===")]
        public float crowdZoomOutDistance = 7f;
        public float crowdDetectRadius = 10f;
        public int crowdEnemyThreshold = 3;
        public float crowdZoomSmoothTime = 0.4f;
    }
}
