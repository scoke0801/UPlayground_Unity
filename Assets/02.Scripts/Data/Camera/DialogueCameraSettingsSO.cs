using UnityEngine;

namespace UPlayGround.Data
{
    /// <summary>
    /// 대화 카메라 모드 전용 튜닝 값.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueCameraSettings", menuName = "UPlayGround/카메라/Dialogue Settings")]
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

        [Header("블렌드 시간")]
        [Tooltip("화자 전환 등 hard cut에 가까운 즉시 전환 시간(초). 0이면 즉시 스냅.")]
        [Min(0f)] public float cutInstantTime = 0f;

        [Tooltip("같은 화자 내 부드러운 보정 시간(초). 일반 라인 전환 기본값.")]
        [Min(0f)] public float softBlendTime = 0f;

        [Tooltip("대화 진입/종료 establishing 블렌드 시간(초).")]
        [Min(0.01f)] public float establishBlendTime = 0.6f;

        [Tooltip("[Deprecated] softBlendTime을 사용하세요. 기존 에셋 호환을 위해 유지됩니다.")]
        [Min(0.01f)] public float speakerCutBlendTime = 0.35f;

        [Header("렌즈")]
        [Range(10f, 90f)] public float fieldOfView = 45f;

        [Header("인트로 시퀀스")]
        [Tooltip("대화 진입(InGame→Dialogue) 시 플레이어→화자를 1회 부드럽게 패닝하는 인트로 사용 여부. 화자 전환 등 재진입에는 발동하지 않는다.")]
        public bool enableIntroSequence = true;

        [Tooltip("인트로 시작 시 플레이어(청자)를 바라보며 멈춰 있는 시간(초).")]
        [Min(0f)] public float introPlayerHoldTime = 0.6f;

        [Tooltip("플레이어 → 화자(대상)로 부드럽게 패닝하는 시간(초).")]
        [Min(0.01f)] public float introPanDuration = 0.8f;

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

        private void OnEnable()
        {
            if (softBlendTime <= 0f)
                softBlendTime = speakerCutBlendTime > 0f ? speakerCutBlendTime : 0.3f;
        }
    }
}
