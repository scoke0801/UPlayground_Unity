using UnityEngine;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "SpringDampCameraEffect", menuName = "UPlayGround/SO/CameraEffect/SpringDamp")]
    public class SpringDampCameraEffectData : CameraEffectData
    {
        [Header("Spring Settings")]
        [Tooltip("스프링 진동 주파수 (Hz)")]
        public float springFrequency = 10f;

        [Tooltip("감쇠비 (0=무감쇠 진동, 1=임계 감쇠)")]
        [Range(0f, 2f)]
        public float springDamping = 0.5f;

        [Tooltip("초기 변위 벡터")]
        public Vector3 initialDisplacement = new Vector3(0f, 0.1f, 0f);

        public override ICameraEffect CreateEffect() => new SpringDampCameraEffect(this);
    }
}
