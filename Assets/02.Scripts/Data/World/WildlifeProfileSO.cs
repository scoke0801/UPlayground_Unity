using UnityEngine;

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
        [Min(0.01f)] public float Acceleration = 2;
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

        private void OnValidate()
        {
            RoamRadius = Mathf.Max(0.1f, RoamRadius);
            FleeRadius = Mathf.Max(RoamRadius, FleeRadius);
            MoveSpeed = Mathf.Max(0.001f, MoveSpeed);
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
            Acceleration = Mathf.Max(0.01f, Acceleration);
            NeighbourRadius = Mathf.Max(0, NeighbourRadius);
            PersonalSpace = Mathf.Max(0, PersonalSpace);
            WalkCycleSpeed = Mathf.Max(0.001f, WalkCycleSpeed);
            RunCycleSpeed = Mathf.Max(0.001f, RunCycleSpeed);
            PlaybackSpeedRange.x = Mathf.Max(0.01f, PlaybackSpeedRange.x);
            PlaybackSpeedRange.y = Mathf.Max(PlaybackSpeedRange.x, PlaybackSpeedRange.y);
            GroundHeightTolerance = Mathf.Max(0.01f, GroundHeightTolerance);
            PathProbeSpacing = Mathf.Max(0.1f, PathProbeSpacing);
        }
    }
}
