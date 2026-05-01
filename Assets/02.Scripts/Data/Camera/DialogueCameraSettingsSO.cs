using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 대화 카메라 모드 전용 튜닝 값.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueCameraSettings", menuName = "UPlayGround/Camera/Dialogue Settings")]
    public class DialogueCameraSettingsSO : ScriptableObject
    {
        public const string AddressableKey = "DialogueCameraSettings";

        [Header("구도")]
        public Vector3 speakerLookAtOffset = new Vector3(0f, 1.45f, 0f);
        public Vector3 listenerShoulderOffset = new Vector3(0.45f, 1.55f, -2.8f);

        [Header("거리")]
        [Min(0.1f)] public float twoShotDistance = 3.2f;
        [Min(0.1f)] public float minDistance = 2.2f;
        [Min(0.1f)] public float maxDistance = 5.5f;

        [Header("블렌드")]
        [Min(0.01f)] public float speakerCutBlendTime = 0.35f;

        [Header("렌즈")]
        [Range(10f, 90f)] public float fieldOfView = 45f;

        public static DialogueCameraSettingsSO CreateRuntimeDefault()
        {
            var settings = CreateInstance<DialogueCameraSettingsSO>();
            settings.hideFlags = HideFlags.HideAndDontSave;
            return settings;
        }

        public float ClampDistance(float distance)
        {
            float min = Mathf.Max(0.1f, minDistance);
            float max = Mathf.Max(min, maxDistance);
            return Mathf.Clamp(distance, min, max);
        }
    }
}
