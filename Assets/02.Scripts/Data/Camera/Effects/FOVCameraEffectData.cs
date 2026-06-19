using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "FOVCameraEffect", menuName = "UPlayGround/카메라/이펙트/FOV")]
    public class FOVCameraEffectData : CameraEffectData
    {
        [Header("FOV Settings")]
        [Tooltip("FOV 변화량 (양수=넓어짐, 음수=좁아짐)")]
        public float fovDelta = 10f;

        [Tooltip("FOV 변화 커브 (x=정규화 시간, y=적용 비율)")]
        public AnimationCurve fovCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override ICameraEffect CreateEffect() => new FOVCameraEffect(this);
    }
}
