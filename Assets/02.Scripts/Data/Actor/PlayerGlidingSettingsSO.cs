using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>슈퍼 점프와 하강 활공의 이동·접촉·전환 수치를 소유한다.</summary>
    [CreateAssetMenu(fileName = "PlayerGlidingSettings", menuName = "UPlayGround/이동/슈퍼 점프 활공")]
    public sealed class PlayerGlidingSettingsSO : ScriptableObject
    {
        [Header("도약")]
        [Min(0.1f)] public float jumpHeight = 12f;
        [Min(0.1f)] public float jumpGravity = 35f;
        [Min(0f)] public float ascentAcceleration = 4f;
        [Min(0f)] public float ascentSpeedLimit = 10f;

        [Header("접촉")]
        [Min(0.01f)] public float catchRootDistance = 0.35f;
        [Range(0f, 180f)] public float catchAngle = 25f;
        [Min(0.01f)] public float catchHandDistance = 0.12f;
        [Min(0.01f)] public float phaseTimeout = 3f;

        [Header("하강 활공")]
        [Min(0f)] public float cruiseSpeed = 10f;
        [Min(0f)] public float minimumSpeed = 6f;
        [Min(0.01f)] public float horizontalAcceleration = 8f;
        [Min(0.01f)] public float horizontalDeceleration = 6f;
        [Min(0.01f)] public float turnSpeed = 110f;
        [Min(0.01f)] public float descentSpeed = 1.6f;
        [Min(0.01f)] public float descentBlendTime = 0.22f;
        [Min(0f)] public float releaseSpeedLimit = 12f;

        [Header("자세 전환")]
        [Min(0f)] public float motionBlendTime = 0.12f;
        [Range(0f, 1f)] public float turnPoseThreshold = 0.15f;
        [Range(0f, 45f)] public float canopyBankAngle = 15f;

        public float LaunchSpeed => Mathf.Sqrt(2f * jumpGravity * jumpHeight);

        /// <summary>잘못된 런타임 수치를 조용히 보정하지 않고 시작 전에 거부한다.</summary>
        public bool HasValidValues => jumpHeight > 0f && jumpGravity > 0f
            && ascentAcceleration >= 0f && ascentSpeedLimit >= 0f
            && catchRootDistance > 0f && catchHandDistance > 0f && phaseTimeout > 0f
            && catchAngle >= 0f && catchAngle <= 180f
            && minimumSpeed >= 0f && cruiseSpeed >= minimumSpeed
            && horizontalAcceleration > 0f && horizontalDeceleration > 0f
            && turnSpeed > 0f && descentSpeed > 0f && descentBlendTime > 0f
            && releaseSpeedLimit >= 0f && motionBlendTime >= 0f;
    }
}
