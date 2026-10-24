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
        [Min(0.1f)] public float RoamRadius = 8;
        [Min(0.1f)] public float FleeRadius = 24;
        [Min(0.1f)] public float MoveSpeed = 0.75f;
        [Min(0.1f)] public float FleeSpeed = 3.5f;
        [Min(0)] public float AlertDistance = 6;
        [Min(0)] public float CalmDistance = 12;
        [Min(0)] public float CalmSeconds = 3;
        [Min(0.1f)] public float DecisionInterval = 0.5f;
        public Vector2 IdleSeconds = new(3, 8);
        [Min(0)] public float FlightHeightVariation = 1;
        [Min(0.1f)] public float LandingSpeed = 1.5f;
        [Min(0.1f)] public float FlightHeight = 4;
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

        private void OnValidate()
        {
            RoamRadius = Mathf.Max(0.1f, RoamRadius);
            FleeRadius = Mathf.Max(RoamRadius, FleeRadius);
            MoveSpeed = Mathf.Max(0.1f, MoveSpeed);
            FleeSpeed = Mathf.Max(MoveSpeed, FleeSpeed);
            CalmDistance = Mathf.Max(AlertDistance, CalmDistance);
            CalmSeconds = Mathf.Max(0, CalmSeconds);
            LandingSpeed = Mathf.Max(0.1f, LandingSpeed);
            FlightHeight = Mathf.Max(0.1f, FlightHeight);
            GroundClearance = Mathf.Max(0.11f, GroundClearance);
            LandingProbeDistance = Mathf.Max(0.1f, LandingProbeDistance);
            DecisionInterval = Mathf.Max(0.1f, DecisionInterval);
            IdleSeconds.x = Mathf.Max(0.1f, IdleSeconds.x);
            IdleSeconds.y = Mathf.Max(IdleSeconds.x, IdleSeconds.y);
        }
    }
}
