using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Data.World
{
    /// <summary>환경 생물의 이동·경계 반응과 기본 모션을 한곳에서 조정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/World/Wildlife Profile")]
    public sealed class WildlifeProfileSO : ScriptableObject
    {
        public bool IsFlying;
        [Tooltip("위험이 사라지면 최초 위치 아래의 안전한 지면으로 돌아와 착지합니다.")]
        public bool CanLand;
        [Tooltip("NavMesh의 단순화된 높이 대신 실제 지면에 발을 맞춥니다. 지면 레이어를 Landing Mask에 지정합니다.")]
        public bool ProjectGroundMovement;
        [Min(0.1f)] public float RoamRadius = 8;
        [Min(0.1f)] public float FleeRadius = 24;
        [Min(0.001f)] public float MoveSpeed = 0.75f;
        [Min(0.001f)] public float FleeSpeed = 3.5f;
        [Min(0)] public float AlertDistance = 6;
        [Min(0)] public float CalmDistance = 12;
        [Min(0)] public float CalmSeconds = 3;
        [Min(0.1f)] public float DecisionInterval = 0.5f;
        public Vector2 IdleSeconds = new(3, 8);
        [Min(0)] public float FlightHeightVariation = 1;
        [Min(0.1f)] public float LandingSpeed = 1.5f;
        [Min(0.1f)] public float FlightHeight = 4;
        [Tooltip("착지한 모델 원점과 지면 사이 거리입니다. 발이 원점에 있는 모델은 0.01을 사용합니다.")]
        [Min(0.01f)] public float GroundClearance = 0.15f;
        [Min(0.1f)] public float LandingProbeDistance = 20;
        public LayerMask LandingMask = Physics.DefaultRaycastLayers;
        [Range(0, 60)] public float MaxLandingSlope = 35;
        [Min(1)] public float TurnSpeed = 180;
        [Tooltip("이동 중 방향을 바꿀 때 유지할 최소 속도 비율. 멈춘 채 몸만 돌리는 것을 방지합니다.")]
        [Range(0.01f, 1)] public float TurnMoveSpeedRatio = 0.25f;
        [Min(1)] public float SimulationDistance = 90;
        [Min(0.01f)] public float CrossFade = 0.25f;
        public AnimationClip Idle;
        public AnimationClip Move;
        public AnimationClip Flee;
        public AnimationClip Feed;
        [Header("생활 리듬 · 게임용 조정값")]
        public bool UseStagedResponse;
        public Vector2 FeedSeconds = new(3, 7);
        public Vector2 ObserveSeconds = new(1, 3);
        public AnimationClip Observe;
        public AnimationClip Rest;
        [Tooltip("한 먹이 탐색 묶음의 섭식 횟수. 0이면 기존 배회를 사용합니다.")]
        public Vector2Int ForageSteps;
        [Tooltip("섭식 사이 짧게 이동할 거리(m). 매번 주변을 살핀 뒤 이동합니다.")]
        public Vector2 ForageStepDistance;
        [Tooltip("도주하는 같은 프로필의 동료를 보고 경계할 거리. 0이면 반응하지 않습니다.")]
        [Min(0)] public float SocialAlertDistance;
        [Min(0.01f)] public float Acceleration = 2;
        [Tooltip("경계 후 위협에서 비켜설 때의 속도. 0이면 MoveSpeed를 쓴다. 도주(FleeSpeed)보다 느린 종종걸음 단계다.")]
        [Min(0)] public float AvoidSpeed;
        [Tooltip("도주 중 최고 속도까지 오르는 가속. 0이면 Acceleration을 쓴다. 놀라서 튀어나가는 느낌을 위해 걷기 가속보다 크게 둔다.")]
        [Min(0)] public float FleeAcceleration;
        [Tooltip("도주 중 진행 방향과 몸 방향이 어긋날 때 유지할 최소 속도 비율. 값이 클수록 돌면서도 달린다.")]
        [Range(0.01f, 1)] public float FleeTurnMoveSpeedRatio = 0.25f;
        [Tooltip("위협의 접근 속도로 계산한 충돌 예상 시간(초)이 이 값보다 짧으면 FleeDistance 밖에서도 도주한다. 0이면 거리만 본다.")]
        [Min(0)] public float FleeTimeToContact;
        [Tooltip("섭식 중 머리를 들기까지 먹는 시간(초). y가 0이면 섭식 중 경계를 하지 않는다.")]
        public Vector2 FeedBurstSeconds;
        [Tooltip("섭식 중 머리를 들고 주변을 살피는 시간(초). 경계 클립이 없으면 Idle을 쓴다.")]
        public Vector2 VigilanceSeconds = new(0.8f, 2);
        [Header("도주 방식 · 개체 차 · 무리 전파")]
        [Tooltip("도주 중 판단마다 좌우로 번갈아 꺾는 각도(도). 토끼·쥐처럼 지그재그로 달아나는 종. 0이면 직선 도주.")]
        [Range(0, 80)] public float ZigZagAngle;
        [Tooltip("개체마다 고정된 도주 방향 편차의 최대각(도). 닭·거위처럼 무리가 사방으로 흩어지는 종. 0이면 편차 없음.")]
        [Range(0, 120)] public float ScatterAngle;
        [Tooltip("개체마다 경계·회피·도주 거리에 곱하는 편차 비율(±). 0.25면 개체별 0.75~1.25배로 대담한 개체와 겁 많은 개체가 섞인다.")]
        [Range(0, 0.6f)] public float BoldnessVariance;
        [Tooltip("같은 프로필의 동료가 도주하면 따라 도주할 거리. 0이면 따라 도주하지 않는다. 동료가 도망친 위협에서 같이 도망친다.")]
        [Min(0)] public float SocialFleeDistance;
        [Tooltip("동료의 도주를 보고 따라 도주하기까지의 반응 지연(초). 한 박자씩 어긋나게 퍼지는 경보를 만든다.")]
        public Vector2 AlarmDelaySeconds = new(0.1f, 0.4f);
        [Tooltip("위협이 이 거리 안에서 정면으로 대치하면 위협 모션을 재생한다. 0이면 사용하지 않는다.")]
        [Min(0)] public float ThreatenDistance;
        public AnimationClip Threaten;
        [Min(0)] public float AvoidDistance;
        [Min(0)] public float FleeDistance;
        [Tooltip("달팽이처럼 빠른 도주 대신 제자리에서 멈추는 비전투 반응입니다.")]
        public bool FreezesWhenApproached;
        [Min(0)] public float NeighbourRadius;
        [Min(0)] public float PersonalSpace = 1;
        [Range(0, 1)] public float Cohesion = 0.25f;
        [Tooltip("클립 1배속의 월드 기준 속도. 발 접지 영상을 보고 보정해야 하며 실측 동물 속도가 아닙니다.")]
        [Min(0.001f)] public float WalkCycleSpeed = 0.75f;
        [Min(0.001f)] public float RunCycleSpeed = 3.5f;
        public Vector2 PlaybackSpeedRange = new(0.35f, 1.8f);
        [Min(0.01f)] public float GroundHeightTolerance = 0.6f;
        [Min(0.1f)] public float PathProbeSpacing = 1;

        [Header("작은 새 움직임 · 켜면 아래 값을 사용")]
        [Tooltip("작은 새의 바운딩 비행(날갯짓-활공 반복), 이륙·착지 급박동, 착지 후 호핑을 켭니다. 나비 등 다른 비행 생물은 끕니다.")]
        public bool UseBirdMotion;
        [Tooltip("순항 중 날갯짓을 이어가는 시간(초).")]
        public Vector2 FlapBurstSeconds = new(0.4f, 0.8f);
        [Tooltip("날개를 몸에 붙이거나 편 채 미끄러지는 시간(초). 이 동안 고도가 살짝 떨어진다.")]
        public Vector2 GlideSeconds = new(0.15f, 0.35f);
        [Tooltip("활공 때 고정할 비행 클립의 정규화 시간. 날개가 펼쳐진 중립 구간을 쓴다.")]
        [Range(0, 1)] public float GlidePoseNormalizedTime = 0.25f;
        [Tooltip("날갯짓 구간에서 오르고 활공 구간에서 내려가는 상하 진폭(m).")]
        [Min(0)] public float UndulationHeight = 0.2f;
        [Min(0.1f)] public float FleeFlapSpeed = 1.3f;
        [Tooltip("이륙·착지 직전에 날갯짓이 빨라지는 배율.")]
        [Min(0.1f)] public float BurstFlapSpeed = 1.7f;
        [Tooltip("지면 이륙 때 위협의 반대 방향으로 비스듬히 오르는 수평 거리(m). 0이면 수직 상승.")]
        [Min(0)] public float TakeoffForwardDistance = 2;
        [Tooltip("착지한 새가 짧게 뛰는 간격(초). 0이면 호핑하지 않는다.")]
        public Vector2 HopIntervalSeconds = new(1.5f, 4);
        [Tooltip("한 번 뛰는 수평 거리(m).")]
        public Vector2 HopDistance = new(0.12f, 0.3f);
        [Tooltip("웅크렸다 뛰는 클립. 웅크림 구간이 끝나는 시점에 도약이 시작되도록 HopWindupSeconds와 맞춘다.")]
        public AnimationClip Hop;
        [Tooltip("뛰기 전 웅크리는 시간(초). Hop 클립의 웅크림 길이와 같아야 한다.")]
        [Min(0)] public float HopWindupSeconds = 0.14f;
        [Min(0.05f)] public float HopSeconds = 0.22f;
        [Min(0)] public float HopHeight = 0.07f;
        [Tooltip("착지점에서 벗어날 수 있는 최대 반경(m).")]
        [Min(0)] public float HopRadius = 0.8f;

        [Header("사냥 · 0 HP는 장식 생물")]
        [Min(0)] public float MaxHealth;
        [Tooltip("월드 기준 피격 영역 최소 크기(m). 몸 충돌 크기는 유지하며 작은 동물의 공격 높이를 보정합니다. 0은 모델 크기를 사용합니다.")]
        public Vector3 MinimumHurtboxSize;
        public EnemyDropTableSO DropTable;
        public AnimationClip Death;
        [Min(0.1f)] public float DeathRemainsSeconds = 3;

        private void OnValidate()
        {
            RoamRadius = Mathf.Max(0.1f, RoamRadius);
            FleeRadius = Mathf.Max(RoamRadius, FleeRadius);
            MoveSpeed = Mathf.Max(0.001f, MoveSpeed);
            TurnMoveSpeedRatio = Mathf.Clamp(TurnMoveSpeedRatio, 0.01f, 1);
            MinimumHurtboxSize = Vector3.Max(Vector3.zero, MinimumHurtboxSize);
            FleeSpeed = Mathf.Max(MoveSpeed, FleeSpeed);
            CalmDistance = Mathf.Max(AlertDistance, CalmDistance);
            CalmSeconds = Mathf.Max(0, CalmSeconds);
            LandingSpeed = Mathf.Max(0.1f, LandingSpeed);
            FlightHeight = Mathf.Max(0.1f, FlightHeight);
            GroundClearance = Mathf.Max(0.01f, GroundClearance);
            LandingProbeDistance = Mathf.Max(0.1f, LandingProbeDistance);
            DecisionInterval = Mathf.Max(0.1f, DecisionInterval);
            IdleSeconds.x = Mathf.Max(0.1f, IdleSeconds.x);
            IdleSeconds.y = Mathf.Max(IdleSeconds.x, IdleSeconds.y);
            AvoidDistance = Mathf.Clamp(AvoidDistance, 0, AlertDistance);
            FleeDistance = Mathf.Clamp(FleeDistance, 0, AvoidDistance);
            FeedSeconds.x = Mathf.Max(0.1f, FeedSeconds.x);
            FeedSeconds.y = Mathf.Max(FeedSeconds.x, FeedSeconds.y);
            ObserveSeconds.x = Mathf.Max(0.1f, ObserveSeconds.x);
            ObserveSeconds.y = Mathf.Max(ObserveSeconds.x, ObserveSeconds.y);
            ForageSteps.x = Mathf.Clamp(ForageSteps.x, 0, 32);
            ForageSteps.y = Mathf.Clamp(ForageSteps.y, ForageSteps.x, 32);
            ForageStepDistance.x = Mathf.Clamp(ForageStepDistance.x, 0, RoamRadius);
            ForageStepDistance.y = Mathf.Clamp(ForageStepDistance.y, ForageStepDistance.x, RoamRadius);
            SocialAlertDistance = Mathf.Max(0, SocialAlertDistance);
            Acceleration = Mathf.Max(0.01f, Acceleration);
            AvoidSpeed = Mathf.Clamp(AvoidSpeed, 0, FleeSpeed);
            FleeAcceleration = Mathf.Max(0, FleeAcceleration);
            FeedBurstSeconds.x = Mathf.Max(0, FeedBurstSeconds.x);
            FeedBurstSeconds.y = Mathf.Max(FeedBurstSeconds.x, FeedBurstSeconds.y);
            VigilanceSeconds.x = Mathf.Max(0.1f, VigilanceSeconds.x);
            VigilanceSeconds.y = Mathf.Max(VigilanceSeconds.x, VigilanceSeconds.y);
            AlarmDelaySeconds.x = Mathf.Max(0, AlarmDelaySeconds.x);
            AlarmDelaySeconds.y = Mathf.Max(AlarmDelaySeconds.x, AlarmDelaySeconds.y);
            ThreatenDistance = Mathf.Clamp(ThreatenDistance, 0, AvoidDistance);
            NeighbourRadius = Mathf.Max(0, NeighbourRadius);
            PersonalSpace = Mathf.Max(0, PersonalSpace);
            WalkCycleSpeed = Mathf.Max(0.001f, WalkCycleSpeed);
            RunCycleSpeed = Mathf.Max(0.001f, RunCycleSpeed);
            PlaybackSpeedRange.x = Mathf.Max(0.01f, PlaybackSpeedRange.x);
            PlaybackSpeedRange.y = Mathf.Max(PlaybackSpeedRange.x, PlaybackSpeedRange.y);
            GroundHeightTolerance = Mathf.Max(0.01f, GroundHeightTolerance);
            PathProbeSpacing = Mathf.Max(0.1f, PathProbeSpacing);
            FlapBurstSeconds.x = Mathf.Max(0.05f, FlapBurstSeconds.x);
            FlapBurstSeconds.y = Mathf.Max(FlapBurstSeconds.x, FlapBurstSeconds.y);
            GlideSeconds.x = Mathf.Max(0.05f, GlideSeconds.x);
            GlideSeconds.y = Mathf.Max(GlideSeconds.x, GlideSeconds.y);
            HopIntervalSeconds.x = Mathf.Max(0, HopIntervalSeconds.x);
            HopIntervalSeconds.y = Mathf.Max(HopIntervalSeconds.x, HopIntervalSeconds.y);
            HopDistance.x = Mathf.Max(0.01f, HopDistance.x);
            HopDistance.y = Mathf.Max(HopDistance.x, HopDistance.y);
        }
    }
}
