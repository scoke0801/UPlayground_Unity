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
        [Min(0.05f)] public float rotationSpeed = 540f;
        [Min(0.05f)] public float decisionInterval = 0.25f;
        [Min(0.05f)] public float pathRefreshDistance = 0.5f;
        [Min(0.05f)] public float cornerDistance = 0.3f;
        [Min(0.05f)] public float stuckSeconds = 3f;
        [Min(0.01f)] public float progressDistance = 0.1f;
        [Min(0.05f)] public float returnDistance = 20f;
        [Min(0.05f)] public float recoveryInterval = 2f;
        [Min(0.05f)] public float navSampleRadius = 1.5f;
        [Min(0.05f)] public float maxGroundDifference = 1f;
        [Range(0f, 89f)] public float maxSlope = 45f;
        [Min(0.01f)] public float capsuleRadius = 0.22f;
        [Min(0.1f)] public float capsuleHeight = 0.8f;
        public int navAreaMask = -1;
        public int navAgentTypeId;
        public LayerMask groundMask;
        public LayerMask obstructionMask;
    }
}
