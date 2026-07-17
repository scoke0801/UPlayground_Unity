using UnityEngine;
using UPlayGround.CameraSystem;

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
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "UPlayGround/카메라/Settings")]
    public class CameraSettings : ScriptableObject
    {
        [Header("=== 등록 카메라 모드 ===")]
        [Tooltip("등록할 카메라 모드를 토글/정렬한다. 비워두면 코드 기본값(전체)이 사용된다. " +
                 "실제 인스턴스 생성은 CameraManager의 팩토리가 담당하므로 미구현 모드(Cinematic 등)는 무시된다.")]
        public CameraModeType[] enabledModes;

        [Header("=== 기본 카메라 ===")]
        public Vector3 defaultOffset = new Vector3(0f, 1f, 0f);
        public Vector3 combatOffset = new Vector3(0.15f, 1.1f, 0f);
        public float offsetSmoothTime = 0.3f;

        [Header("거리")]
        public float defaultDistance = 5.1f;
        public float combatDistance = 5.7f;
        public float minDistance = 3f;
        public float maxDistance = 8.5f;

        [Header("회전")]
        [Tooltip("마우스 delta(프레임당 픽셀 누적값) 기준 회전 스칼라. 게임패드에는 적용되지 않는다.")]
        public float rotationSpeed = 20f;
        public float minVerticalAngle = -30f;   // 음수 = 위쪽, 양수 = 아래쪽
        public float maxVerticalAngle = 70f;

        [Header("게임패드 룩 (각속도 °/s)")]
        [Tooltip("게임패드 우측 스틱 풀 입력 시 좌우 회전 속도(초당 도). 스틱은 정규화 축이라 마우스(rotationSpeed)와 별개의 각속도로 적분한다.")]
        public float gamepadYawSpeed = 220f;
        [Tooltip("게임패드 우측 스틱 풀 입력 시 상하 회전 속도(초당 도). 보통 yaw보다 약간 낮게 둔다.")]
        public float gamepadPitchSpeed = 140f;

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
        [Tooltip("장애물이 새로 들어왔을 때 카메라 거리가 줄어드는 스무딩 시간. 0이면 즉시 당긴다.")]
        public float collisionOccludedSmoothTime = 0.035f;
        [Tooltip("장애물이 사라졌을 때 원래 거리로 복귀하는 스무딩 시간.")]
        public float collisionReturnSpeed = 0.38f;
        [Tooltip("이 시간보다 짧게 스친 충돌은 무시해 얇은 모서리에서 카메라가 튀는 것을 줄인다.")]
        public float collisionMinimumOcclusionTime = 0.025f;
        [Tooltip("충돌 중 더 가까운 거리로 당겨진 뒤, 바로 다시 멀어지지 않고 유지하는 시간.")]
        public float collisionSmoothingHoldTime = 0.08f;
        [Tooltip("충돌 해제 판정에 사용하는 거리 여유값.")]
        public float collisionReleaseHysteresis = 0.22f;
        [Tooltip("충돌 보정 거리가 한 프레임에 변할 수 있는 최대 속도. 0 이하면 제한하지 않는다.")]
        public float collisionMaxDistanceChangeSpeed = 18f;
        [Tooltip("플레이어 캡슐 때문에 전방 카메라로 전환될 때의 블렌드 스무딩 시간.")]
        public float frontCameraBlendInSmoothTime = 0.08f;
        [Tooltip("전방 카메라에서 후방 카메라로 복귀할 때의 블렌드 스무딩 시간.")]
        public float frontCameraBlendOutSmoothTime = 0.25f;

        [Header("=== 충돌 검사 (MultiProbe) ===")]
        public bool useMultiProbe = true;
        public int collisionProbeCount = 6;
        public float collisionSkinWidth = 0.08f;
        [Range(0f, 1f)]
        public float minNormalAlignment = 0.5f;

        [Header("=== FOV ===")]
        public float fovExplore = 52f;
        public float fovCombat = 58f;
        public float fovLockOn = 58f;
        public float fovSmoothTime = 0.22f;

        [Header("=== 동적 FOV (속도 기반) ===")]
        public bool enableSpeedFOV = true;
        public float speedFOVMax = 4f;
        public float speedForMaxFOV = 8f;
        public float speedFOVSmoothTime = 0.3f;

        [Header("=== Look-ahead (진행방향 선행) ===")]
        public bool enableLookAhead = true;
        public float lookAheadDistance = 1.2f;
        public float lookAheadSpeedRef = 5f;
        public float lookAheadSmoothTime = 0.25f;
        [Range(0f, 1f)]
        public float lockOnLookAheadMultiplier = 0.05f;

        [Header("=== Floor Rescue (바닥 보정) ===")]
        public bool enableFloorRescue = true;
        public float floorRescueDropThreshold = 1f;
        public float groundClearance = 0.3f;
        public LayerMask floorRescueLayerMask;

        [Header("=== 카메라 정렬 ===")]
        public float alignSpeed = 2.2f;
        public float alignDuration = 0.35f;
        public float explorePitch = 25f;
        public float combatPitch = 25f;

        [Header("=== 락온 ===")]
        public float lockOnRange = 16f;
        [Tooltip("현재 락온 대상 유지 한계 거리. lockOnRange보다 작으면 lockOnRange를 사용한다.")]
        public float lockOnReleaseRange = 20f;
        [Tooltip("현재 락온 대상이 유지 조건에서 벗어나도 즉시 해제하지 않고 유지하는 시간.")]
        public float lockOnLostGraceTime = 0.35f;
        public float lockOnDistance = 5.7f;
        public float lockOnTransitionDuration = 0.3f;
        public float targetSwitchCooldown = 0.15f;

        [Header("=== 락온 포커스 스무딩 ===")]
        public float lockOnFocusSmoothTime = 0.15f;

        [Header("=== 락온 타겟팅 우선순위 ===")]
        public LockOnPriorityMode lockOnPriorityMode = LockOnPriorityMode.CameraDirection;
        [Tooltip("현재 타겟이 계속 유효할 때 점수에서 유지 보너스를 준다.")]
        public float lockOnCurrentTargetBonus = 0.25f;

        [Header("=== 락온 타겟 전환 ===")]
        public bool lockOnSwitchWrap = true;
        public float lockOnSwitchScreenWeight = 1f;
        public float lockOnSwitchCenterWeight = 0.35f;
        public float lockOnSwitchDistanceWeight = 0.25f;
        [Tooltip("락온 중 마우스를 좌우로 빠르게 움직이면(플릭) 그 방향의 대상으로 전환한다.")]
        public bool lockOnMouseFlickSwitch = true;
        [Tooltip("전환 발동에 필요한 마우스 X 델타 누적치(픽셀). 클수록 더 크게 움직여야 전환된다.")]
        [Min(1f)]
        public float lockOnFlickThreshold = 150f;
        [Tooltip("플릭 누적치 감쇠 속도(픽셀/초). 이 속도보다 느린 마우스 이동으로는 전환이 발동하지 않는다.")]
        [Min(0f)]
        public float lockOnFlickDecay = 600f;

        [Header("=== 락온 가시성 검증 ===")]
        [Tooltip("신규 락온 후보가 카메라에서 보이지 않으면 제외한다.")]
        public bool lockOnRequireLineOfSight = true;
        [Tooltip("락온 가시성 SphereCast 반지름. 0이면 Raycast로 검사한다.")]
        public float lockOnLineOfSightRadius = 0.12f;

        [Header("=== 락온 쌍 프레이밍 ===")]
        public bool enableLockOnPairFraming = false;
        [Range(0f, 1f)]
        public float lockOnPairFocusRatio = 0.05f;
        public float lockOnMaxFocusOffsetFromPlayer = 0.5f;
        public float lockOnPairFocusSmoothTime = 0.3f;

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

        [Header("=== 락온 거리 피팅(상단·공중 대상) ===")]
        [Tooltip("상단/공중 대상이 피치 클램프만으로 화면에 안 담길 때, 카메라 거리를 늘려 플레이어와 대상을 모두 프레임에 담는다.")]
        public bool enableLockOnFitDistance = true;
        [Tooltip("프레이밍 안전 영역 비율(0.3~1). 1=프러스텀 가장자리, 0.8=80% 안쪽에 대상을 가둔다.")]
        [Range(0.3f, 1f)]
        public float lockOnFitSafeFraction = 0.78f;
        [Tooltip("거리 피팅 시 도달 가능한 최대 거리. 필요 시 일반 maxDistance를 넘어선다.")]
        public float lockOnFitMaxDistance = 13f;
        [Tooltip("대상 콜라이더 월드 상단에 더할 머리 위 여백(m).")]
        public float lockOnFitTopPadding = 0.4f;
        [Tooltip("이 높이차(대상 상단 - 피벗) 이상일 때만 거리 피팅을 시작한다(m). 미세 진동 방지용.")]
        public float lockOnFitMinHeightDiff = 1.0f;
        [Tooltip("거리 피팅 수렴 스무딩 시간.")]
        public float lockOnFitSmoothTime = 0.35f;

        [Header("=== 다수 적 줌아웃 ===")]
        public float crowdZoomOutDistance = 7.4f;
        public float crowdDetectRadius = 12f;
        public int crowdEnemyThreshold = 3;
        public float crowdZoomSmoothTime = 0.4f;

        [Header("=== 대형 몬스터 시야 확장 ===")]
        public bool enableMonsterSizeFOV = true;
        [Tooltip("이 크기 이하의 몬스터는 추가 FOV/거리 확장 대상에서 제외한다.")]
        public float monsterSizeReference = 2.2f;
        [Tooltip("이 크기 이상이면 monsterSizeFOVMax와 monsterSizeDistanceMax를 모두 적용한다.")]
        public float monsterSizeForMaxFOV = 7f;
        [Tooltip("범위 내 최대 몬스터 크기에 따라 추가할 최대 FOV.")]
        public float monsterSizeFOVMax = 7f;
        [Tooltip("범위 내 최대 몬스터 크기에 따라 추가할 최대 카메라 거리.")]
        public float monsterSizeDistanceMax = 1.6f;
        public float monsterSizeDistanceSmoothTime = 0.32f;

        [Header("=== 전투 카메라 접근성 기본값 ===")]
        [Range(0f, 2f)] public float combatCameraShakeScale = 0.85f;
        [Range(0f, 1f)] public float combatCameraAutoCorrectionScale = 0.6f;
        [Range(0f, 1f)] public float combatCameraSequenceIntensity = 0.85f;
    }
}
