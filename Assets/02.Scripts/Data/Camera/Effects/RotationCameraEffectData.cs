using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "RotationCameraEffect", menuName = "UPlayGround/카메라/이펙트/Rotation")]
    public class RotationCameraEffectData : CameraEffectData
    {
        [Header("Rotation Settings")]
        [Tooltip("Yaw 회전량 (도)")]
        public float yawDelta = 0f;

        [Tooltip("Pitch 회전량 (도)")]
        public float pitchDelta = 0f;

        [Tooltip("회전 진행 커브 (x=정규화 시간, y=진행률)")]
        public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override ICameraEffect CreateEffect() => new RotationCameraEffect(this);
    }
}
