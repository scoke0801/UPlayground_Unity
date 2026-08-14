using System.Collections.Generic;
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

        [Tooltip("투샷/와이드처럼 두 인물을 담는 구도의 거리 상한(m). 일반 샷의 maxDistance와 별도로 둔다.")]
        [Min(0.1f)] public float maxFramingDistance = 9f;

        [Header("블렌드 시간")]
        [Tooltip("화자 전환 등 hard cut에 가까운 즉시 전환 시간(초). 0이면 즉시 스냅.")]
        [Min(0f)] public float cutInstantTime = 0f;

        [Tooltip("같은 화자 내 부드러운 보정 시간(초). 일반 라인 전환 기본값.")]
        [Min(0f)] public float softBlendTime = 0f;

        [Tooltip("대화 진입/종료 establishing 블렌드 시간(초).")]
        [Min(0.01f)] public float establishBlendTime = 0.6f;

        [Tooltip("대화 진입 첫 샷을 즉시 컷하지 않고 establishBlendTime으로 붙인다. " +
                 "끄면(기본) 기존처럼 즉시 컷 — InGame 카메라에서 날아오는 느낌이 싫을 때 유지한다. " +
                 "인트로 시퀀스가 발동하는 라인에는 적용되지 않는다.")]
        public bool establishBlendOnEnter = false;

        [Tooltip("[Deprecated] softBlendTime을 사용하세요. 기존 에셋 호환을 위해 유지됩니다.")]
        [Min(0.01f)] public float speakerCutBlendTime = 0.35f;

        [Header("렌즈")]
        [Range(10f, 90f)] public float fieldOfView = 45f;

        [Header("인트로 시퀀스")]
        [Tooltip("대화 진입 시 플레이어→화자를 1회 부드럽게 패닝하는 인트로 사용 여부. " +
                 "대화 세션당 1회만 발동하며, 화자 전환이나 녹화 카메라 왕복으로는 재발동하지 않는다.")]
        public bool enableIntroSequence = true;

        [Tooltip("인트로 시작 시 플레이어(청자)를 바라보며 멈춰 있는 시간(초).")]
        [Min(0f)] public float introPlayerHoldTime = 0.6f;

        [Tooltip("플레이어 → 화자(대상)로 부드럽게 패닝하는 시간(초).")]
        [Min(0.01f)] public float introPanDuration = 0.8f;

        [Header("자동 디렉터")]
        [Tooltip("가상선(180° 룰) 유지. 대화 세션 시작 시 축과 카메라 쪽을 고정하고 화자가 바뀌어도 반대편으로 넘어가지 않는다.")]
        public bool enforce180Rule = true;

        [Tooltip("두 인물의 수평 방향이 최초 캐시한 가상선에서 이 각도(도) 이상 벗어나면 축을 다시 잡는다. " +
                 "인물이 대화 중 이동하는 경우를 위한 안전장치이며, 카메라 쪽(SideSign)은 유지되어 시선 매칭은 깨지지 않는다.")]
        [Range(15f, 179f)] public float axisRecaptureAngle = 75f;

        [Tooltip("선택지 노드에서 두 인물을 함께 담는 투샷으로 전환(가독성).")]
        public bool choicePhaseTwoShot = true;

        [Header("짧은 라인 컷 억제")]
        [Tooltip("이 글자 수 이하의 대사를 '짧은 라인'으로 본다.")]
        [Min(0)] public int shortLineThreshold = 12;

        [Tooltip("짧은 라인이 이 횟수 이상 연속되면 화자마다 컷하지 않고 투샷으로 묶는다. 0이면 억제하지 않는다.")]
        [Min(0)] public int shortLineTwoShotCount = 3;

        [Header("샷 프리셋")]
        [Tooltip("샷 종류별 구도. 비워 두면 위의 기본 구도 값으로 자동 생성한다(기존 동작과 동일).")]
        public List<DialogueShotPreset> shotPresets = new List<DialogueShotPreset>();

        private Dictionary<DialogueShotType, DialogueShotPreset> _presetCache;

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

        /// <summary>투샷/와이드처럼 두 인물을 담는 구도의 거리 클램프.</summary>
        public float ClampFramingDistance(float distance)
        {
            float min = Mathf.Max(0.1f, minDistance);
            float max = Mathf.Max(min, maxFramingDistance);
            return Mathf.Clamp(distance, min, max);
        }

        /// <summary>
        /// 샷 프리셋을 해석한다. 저작된 항목이 있으면 그것을, 없으면 기본 구도에서 파생한 내장 기본값을 돌려준다.
        /// </summary>
        public DialogueShotPreset ResolvePreset(DialogueShotType shotType)
        {
            if (shotType == DialogueShotType.Auto)
                shotType = DialogueShotType.OverTheShoulderSpeaker;

            if (_presetCache == null || _presetCache.Count == 0)
                RebuildPresetCache();

            if (_presetCache.TryGetValue(shotType, out DialogueShotPreset preset) && preset != null)
                return preset;

            return _presetCache[DialogueShotType.OverTheShoulderSpeaker];
        }

        /// <summary>인스펙터에서 프리셋을 수정한 뒤 런타임 캐시를 버린다.</summary>
        public void InvalidatePresetCache() => _presetCache = null;

        private void RebuildPresetCache()
        {
            _presetCache = new Dictionary<DialogueShotType, DialogueShotPreset>();

            foreach (DialogueShotType type in System.Enum.GetValues(typeof(DialogueShotType)))
            {
                if (type == DialogueShotType.Auto)
                    continue;

                _presetCache[type] = BuildDefaultPreset(type);
            }

            // 저작된 프리셋이 내장 기본값을 덮는다.
            for (int i = 0; i < shotPresets.Count; i++)
            {
                DialogueShotPreset authored = shotPresets[i];
                if (authored == null || authored.shotType == DialogueShotType.Auto)
                    continue;

                _presetCache[authored.shotType] = authored;
            }
        }

        /// <summary>
        /// 기본 구도 필드(speakerLookAtOffset/listenerShoulderOffset/twoShotDistance/fieldOfView)에서
        /// 샷별 기본 프리셋을 파생한다. OTS 계열은 기존 동작과 완전히 동일한 값이 나온다.
        /// </summary>
        private DialogueShotPreset BuildDefaultPreset(DialogueShotType shotType)
        {
            var preset = new DialogueShotPreset
            {
                shotType = shotType,
                shoulderOffset = listenerShoulderOffset,
                distance = twoShotDistance,
                fieldOfView = fieldOfView,
                lookAtOffset = speakerLookAtOffset,
                framesBothActors = false,
                separationFitScale = 1.15f
            };

            switch (shotType)
            {
                case DialogueShotType.OverTheShoulderSpeaker:
                case DialogueShotType.OverTheShoulderListener:
                    break;

                case DialogueShotType.Reaction:
                    // 리액션은 어깨 걸침을 줄이고 조금 더 붙는다.
                    preset.shoulderOffset = new Vector3(
                        listenerShoulderOffset.x * 0.6f,
                        listenerShoulderOffset.y,
                        listenerShoulderOffset.z);
                    preset.distance = twoShotDistance * 0.85f;
                    break;

                case DialogueShotType.Closeup:
                    preset.shoulderOffset = new Vector3(
                        listenerShoulderOffset.x * 0.5f,
                        listenerShoulderOffset.y * 0.95f,
                        listenerShoulderOffset.z);
                    preset.distance = twoShotDistance * 0.7f;
                    preset.fieldOfView = Mathf.Max(10f, fieldOfView - 6f);
                    break;

                case DialogueShotType.TwoShot:
                    // 가상선 옆에서 두 인물을 함께 잡는다(x 성분 우세).
                    preset.shoulderOffset = new Vector3(2.2f, listenerShoulderOffset.y * 0.85f, -1.4f);
                    preset.distance = twoShotDistance * 1.45f;
                    preset.framesBothActors = true;
                    preset.separationFitScale = 1.15f;
                    break;

                case DialogueShotType.Wide:
                    preset.shoulderOffset = new Vector3(2.6f, listenerShoulderOffset.y * 1.35f, -2.2f);
                    preset.distance = twoShotDistance * 2f;
                    preset.fieldOfView = Mathf.Min(90f, fieldOfView + 6f);
                    preset.framesBothActors = true;
                    preset.separationFitScale = 1.4f;
                    break;
            }

            return preset;
        }

        private void OnEnable()
        {
            if (softBlendTime <= 0f)
                softBlendTime = speakerCutBlendTime > 0f ? speakerCutBlendTime : 0.3f;
        }

        private void OnValidate()
        {
            _presetCache = null;
        }
    }
}
