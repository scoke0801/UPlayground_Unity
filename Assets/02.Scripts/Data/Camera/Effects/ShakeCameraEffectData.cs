using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "ShakeCameraEffect", menuName = "UPlayGround/카메라/이펙트/Shake")]
    public class ShakeCameraEffectData : CameraEffectData
    {
        [Header("Shake Settings")]
        [Tooltip("CameraShakeData 에셋 직접 참조 (우선)")]
        public CameraShakeData shakeData;

        [Tooltip("CameraShakeDatabase에서 키로 조회 (shakeData가 null일 때 사용)")]
        public string shakeDataKey;

        public override ICameraEffect CreateEffect() => new ShakeCameraEffect(this);
    }
}
