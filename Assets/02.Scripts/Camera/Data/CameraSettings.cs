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
        public float minDistance = 3f;
        public float maxDistance = 8.5f;

        [Header("회전")]
        [Tooltip("마우스 delta(프레임당 픽셀 누적값) 기준 회전 스칼라. 게임패드에는 적용되지 않는다.")]
        public float rotationSpeed = 20f;
        [Tooltip("비락온 수동 카메라가 캐릭터 아래쪽으로 내려가는 피치 하한입니다.")]
        public float minVerticalAngle = -30f;
        [Tooltip("비락온 수동 카메라가 캐릭터 위쪽으로 올라가는 피치 상한입니다.")]
        public float maxVerticalAngle = 70f;

        [Header("게임패드 룩 (각속도 °/s)")]
        [Tooltip("게임패드 우측 스틱 풀 입력 시 좌우 회전 속도(초당 도). 스틱은 정규화 축이라 마우스(rotationSpeed)와 별개의 각속도로 적분한다.")]
        public float gamepadYawSpeed = 220f;
        [Tooltip("게임패드 우측 스틱 풀 입력 시 상하 회전 속도(초당 도). 보통 yaw보다 약간 낮게 둔다.")]
        public float gamepadPitchSpeed = 140f;

        [Header("줌")]
        public float zoomSpeed = 0.5f;

        [Header("스무딩")]
        public float positionSmoothTime = 0.1f;
        [Tooltip("락온·명시적 정렬·연출 회전의 추가 스무딩 시간입니다. 비락온 자유 궤도 입력에는 적용하지 않습니다.")]
        public float rotationSmoothTime = 0.1f;

        [Header("=== 충돌 ===")]
        public float collisionOffset = 0.15f;
        public float cameraRadius = 0.25f;
        [Tooltip("장애물이 사라졌을 때 원래 거리로 복귀하는 스무딩 시간.")]
        public float collisionReturnSpeed = 0.38f;
        [Tooltip("충돌 중 더 가까운 거리로 당겨진 뒤, 바로 다시 멀어지지 않고 유지하는 시간.")]
        public float collisionSmoothingHoldTime = 0.08f;
        [Tooltip("충돌 중 프로브 거리가 이 값보다 작게 흔들리면 같은 거리로 간주한다(m). 메시 모서리의 미세 당겨짐을 줄인다.")]
        [Min(0f)]
        public float collisionDistanceDeadZone = 0.04f;
        [Tooltip("충돌 보정 거리가 한 프레임에 변할 수 있는 최대 속도. 0 이하면 제한하지 않는다.")]
        public float collisionMaxDistanceChangeSpeed = 18f;
        public float collisionSkinWidth = 0.08f;

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

        [Header("=== 탐색 구도 ===")]
        public bool enableTraversalComposition = true;
        [Tooltip("전투 중 진행방향 LookAhead의 배율. 락온 중에는 lockOnLookAheadMultiplier가 우선한다.")]
        [Range(0f, 1f)]
        public float combatLookAheadMultiplier = 0.35f;
        [Tooltip("접지 이동 중 전방 지형을 읽는 최대 거리(m).")]
        [Min(0f)]
        public float groundLookAheadDistance = 2.5f;
        [Tooltip("전방 지형 높이차를 피벗 Y에 반영하는 비율.")]
        [Range(0f, 1f)]
        public float groundLookAheadStrength = 0.55f;
        [Tooltip("전방 지형으로 인한 피벗 높이 보정의 절댓값 상한(m).")]
        [Min(0f)]
        public float groundLookAheadMaxHeight = 1.1f;
        [Tooltip("전방 지형 탐색 레이의 시작 높이(m).")]
        [Min(0f)]
        public float groundProbeHeight = 2f;
        [Tooltip("전방 지형 탐색 레이가 시작점 아래로 검사하는 추가 깊이(m).")]
        [Min(0f)]
        public float groundProbeDepth = 4f;
        [Tooltip("상승 중 피벗이 위쪽을 미리 보는 최대 거리(m).")]
        [Min(0f)]
        public float airborneRiseLookAhead = 0.45f;
        [Tooltip("낙하 중 피벗이 착지 방향을 미리 보는 최대 거리(m).")]
        [Min(0f)]
        public float airborneFallLookAhead = 1.1f;
        [Tooltip("공중 구도 보정이 시작되는 수직 속도(m/s).")]
        [Min(0f)]
        public float airborneEffectStartSpeed = 2f;
        [Tooltip("공중 구도 보정이 최대가 되는 수직 속도(m/s).")]
        [Min(0.01f)]
        public float airborneSpeedForMax = 12f;
        [Tooltip("캐릭터 높이 변화가 이 범위 안이면 피벗 Y를 유지한다(m).")]
        [Min(0f)]
        public float verticalTrackingDeadZone = 0.18f;
        [Min(0.01f)] public float groundedVerticalSmoothTime = 0.10f;
        [Min(0.01f)] public float airborneRiseVerticalSmoothTime = 0.18f;
        [Min(0.01f)] public float airborneFallVerticalSmoothTime = 0.28f;
        [Tooltip("공중/낙하 중 추가되는 최대 카메라 거리(m).")]
        [Min(0f)]
        public float airborneDistanceMaxAdd = 1.2f;
        [Tooltip("공중/낙하 중 추가되는 최대 FOV.")]
        [Min(0f)]
        public float airborneFOVMaxAdd = 4f;
        [Min(0.01f)] public float airborneDistanceSmoothTime = 0.28f;
        [Min(0.01f)] public float airborneFOVSmoothTime = 0.25f;

        [Header("=== 카메라 정렬 ===")]
        public float alignDuration = 0.35f;
        public float explorePitch = 25f;
        public float combatPitch = 25f;

        [Header("=== 이동 자동 리센터링 ===")]
        [Tooltip("수동 Look 입력이 끝난 뒤 이동 방향으로 카메라를 자동 정렬합니다. 수동 궤도 조작을 우선하려면 끕니다.")]
        public bool enableAutoRecentering = false;
        [Tooltip("마지막 수동 카메라 입력 후 자동 리센터링을 시작하기까지의 시간(초).")]
        [Min(0f)]
        public float recenterInputDelay = 1.1f;
        [Tooltip("자동 리센터링을 허용하는 최소 평면 이동 속도(m/s).")]
        [Min(0f)]
        public float recenterMinPlanarSpeed = 1f;
        [Tooltip("이동 방향으로 yaw가 수렴하는 시간. 클수록 플레이어 조작을 덜 방해한다.")]
        [Min(0.01f)]
        public float recenterYawSmoothTime = 0.75f;
        [Tooltip("탐색/전투 기본 pitch로 수렴하는 시간.")]
        [Min(0.01f)]
        public float recenterPitchSmoothTime = 1.35f;
        [Tooltip("전투 중 자동 리센터링 강도 배율.")]
        [Range(0f, 1f)]
        public float combatRecenterMultiplier = 0.45f;

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

        [Header("=== 락온 피벗 구도 ===")]
        [Tooltip("플레이어와 대상을 함께 담기 위해 카메라 피벗을 대상 쪽으로 이동합니다. 락온 시 카메라 전진을 피하려면 비활성화합니다.")]
        public bool enableLockOnPairFraming = false;
        [Range(0f, 1f)]
        public float lockOnPairFocusRatio = 0.35f;
        public float lockOnMaxFocusOffsetFromPlayer = 1.5f;
        public float lockOnPairFocusSmoothTime = 0.25f;

        [Header("락온 고저차 감쇠")]
        public float lockOnHeightDampFactor = 0.42f;
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

        [Header("=== 락온 거리 피팅(플레이어·대상) ===")]
        [Tooltip("플레이어 기준 피벗은 유지하고, 두 대상이 안전 영역에 다 담기지 않을 때만 카메라 거리를 늘립니다. 카메라를 앞으로 당기지는 않습니다.")]
        public bool enableLockOnFitDistance = true;
        [Tooltip("프레이밍 안전 영역 비율(0.3~1). 1=프러스텀 가장자리, 0.8=80% 안쪽에 대상을 가둔다.")]
        [Range(0.3f, 1f)]
        public float lockOnFitSafeFraction = 0.78f;
        [Tooltip("거리 피팅 시 도달 가능한 최대 거리. 필요 시 일반 maxDistance를 넘어선다.")]
        public float lockOnFitMaxDistance = 13f;
        [Tooltip("대상 콜라이더 월드 상단에 더할 머리 위 여백(m).")]
        public float lockOnFitTopPadding = 0.4f;
        [Tooltip("대상 포커스는 항상 거리 피팅에 사용합니다. 대상 상단은 이 높이차(대상 상단 - 피벗) 이상일 때만 추가로 검사합니다(m).")]
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
