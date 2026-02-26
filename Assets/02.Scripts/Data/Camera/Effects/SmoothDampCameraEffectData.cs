using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "SmoothDampCameraEffect", menuName = "UPlayGround/SO/CameraEffect/SmoothDamp")]
    public class SmoothDampCameraEffectData : CameraEffectData
    {
        [Header("SmoothDamp Override")]
        [Tooltip("위치 SmoothDamp 시간 오버라이드")]
        public float positionSmoothTime = 0.3f;

        [Tooltip("회전 SmoothDamp 시간 오버라이드")]
        public float rotationSmoothTime = 0.3f;

        public override ICameraEffect CreateEffect() => new SmoothDampCameraEffect(this);
    }
}
