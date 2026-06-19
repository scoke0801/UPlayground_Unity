using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// speakerId → 텍스트 색상 매핑 테이블.
    /// Monologue / System 채널에서 화자 색상을 일괄 관리합니다.
    /// 등록되지 않은 speakerId는 defaultColor(흰색)로 표시됩니다.
    /// DialogueManager가 Addressables로 로드해 관리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/Speaker Color Table", fileName = "SpeakerColorTable")]
    public class SpeakerColorTableSO : ScriptableObject
    {
        // Addressables 등록 키 — SO 에셋의 Address 값과 일치해야 합니다.
        public const string AddressableKey = "SpeakerColorTable";

        [System.Serializable]
        public class SpeakerColorEntry
        {
            public string speakerId;
            public Color  color = Color.white;
        }

        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private List<SpeakerColorEntry> entries = new();

        private Dictionary<string, Color> _colorMap;

        private void OnEnable() => BuildMap();

        public Color GetColor(string speakerId)
        {
            if (_colorMap == null) BuildMap();

            if (!string.IsNullOrEmpty(speakerId) && _colorMap.TryGetValue(speakerId, out var color))
                return color;

            return defaultColor;
        }

        private void BuildMap()
        {
            _colorMap = new Dictionary<string, Color>();
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.speakerId))
                    _colorMap[entry.speakerId] = entry.color;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => BuildMap();
#endif
    }
}
