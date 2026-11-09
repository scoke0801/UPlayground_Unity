using UnityEngine;

namespace UPlayGround.Data
{
    public partial class CameraSettings
    {
        [Header("활공 속도감")]
        public bool enableGlidingSpeedFeedback = true;
        [Range(0f, 15f)] public float glidingFOVMaxAdd = 7f;
        [Min(0.01f)] public float glidingFeedbackBlendTime = 0.25f;
        public Material glidingSpeedLineMaterial;
        [Range(8, 96)] public int glidingSpeedLineCount = 40;
        [Range(0f, 1f)] public float glidingSpeedLineOpacity = 0.22f;
        [Range(0.5f, 0.95f)] public float glidingSpeedLineInnerRadius = 0.72f;
        [Range(0.01f, 0.3f)] public float glidingSpeedLineLength = 0.14f;
        [Range(0.0005f, 0.01f)] public float glidingSpeedLineWidth = 0.0015f;
        [Min(0.01f)] public float glidingSpeedLineLifetime = 0.45f;
    }
}
