using UnityEngine;

namespace UPlayGround.Data.Pet
{
    /// <summary>지상 추종·경로 재계산·안전 복귀의 조정값.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Follow Profile")]
    public sealed class PetFollowProfileSO : ScriptableObject
    {
        [Min(0.05f)] public float stopDistance = 0.5f;
        [Min(0.05f)] public float resumeDistance = 1.2f;
        [Min(0.05f)] public float rearDistance = 1.8f;
        [Min(0.05f)] public float sideDistance = 1f;
        [Min(0.05f)] public float walkSpeed = 2.5f;
        [Min(0.05f)] public float runSpeed = 8f;
        [Min(0.05f)] public float runDistance = 4f;
        [Min(0.05f)] public float acceleration = 12f;
        [Min(0.05f)] public float gravity = 25f;
        [Min(0.05f)] public float rotationSpeed = 540f;
        [Min(0.05f)] public float decisionInterval = 0.25f;
        [Min(0.05f)] public float pathRefreshDistance = 0.5f;
        [Min(0.05f)] public float cornerDistance = 0.3f;
        [Min(0.05f)] public float stuckSeconds = 3f;
        [Min(0.01f)] public float progressDistance = 0.1f;
        [Min(0.05f)] public float returnDistance = 20f;
        [Min(0.05f)] public float recallDelay = 1f;
        [Min(0.05f)] public float recallCooldown = 3f;
        [Min(0.05f)] public float swimExitDelay = 0.3f;
        [Min(0.05f)] public float boardingDuration = 0.25f;
        [Min(0.05f)] public float recoveryInterval = 2f;
        [Min(0.05f)] public float navSampleRadius = 1.5f;
        [Min(0.05f)] public float maxGroundDifference = 1f;
        [Min(0.01f), Tooltip("NavMesh 베이크 지면의 높이 오차 허용값. 축소 캡슐 크기와 독립적으로 조정한다.")]
        public float navGroundTolerance = 0.25f;

        /// <summary>축소된 펫 반경 대신 베이크 오차와 층 구분 한계로 지면 투영을 검사한다.</summary>
        public bool IsGroundProjectionValid(float sampledHeight, float groundHeight) =>
            Mathf.Abs(sampledHeight - groundHeight) <= Mathf.Min(navGroundTolerance, maxGroundDifference);

        /// <summary>베이크 높이 오차를 제외한 지면 방향 거리로 경로 지점 도달을 판단한다.</summary>
        public bool IsPathCornerReached(Vector3 position, Vector3 corner, Vector3 up) =>
            Vector3.ProjectOnPlane(position - corner, up).sqrMagnitude <= cornerDistance * cornerDistance;
        [Range(0f, 89f)] public float maxSlope = 45f;
        [Min(0.01f)] public float capsuleRadius = 0.22f;
        [Min(0.1f)] public float capsuleHeight = 0.8f;
        public int navAreaMask = -1;
        public int navAgentTypeId;
        public LayerMask groundMask;
        public LayerMask obstructionMask;
    }
}
