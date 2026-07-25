using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 의미 키 → 텍스트 색상 팔레트.
    /// 대사 본문의 <c>[c:key]...[/c]</c> 마크업이 참조하며, 톤 조정을 이 에셋 한 곳에서 끝내기 위해 존재합니다.
    /// 화자 색을 다루는 <see cref="SpeakerColorTableSO"/>와 책임이 다르므로 별도 에셋으로 분리합니다.
    /// DialogueManager가 Addressables로 로드해 UI에 제공합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/Dialogue Palette", fileName = "DialoguePalette")]
    public class DialoguePaletteSO : ScriptableObject
    {
        // Addressables 등록 키 — SO 에셋의 Address 값과 일치해야 합니다.
        public const string AddressableKey = "DialoguePalette";

        [System.Serializable]
        public class NamedColorEntry
        {
            [Tooltip("대사에서 [c:key] 로 참조할 의미 키. 예: emphasis, item, danger")]
            public string key;
            public Color color = Color.white;
        }

        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private List<NamedColorEntry> entries = new();

        private Dictionary<string, Color> _colorMap;

        /// <summary>등록되지 않은 키에 사용할 폴백 색상.</summary>
        public Color DefaultColor => defaultColor;

        private void OnEnable() => BuildMap();

        public bool TryGet(string key, out Color color)
        {
            if (_colorMap == null) BuildMap();

            if (!string.IsNullOrEmpty(key) && _colorMap.TryGetValue(key, out color))
                return true;

            color = defaultColor;
            return false;
        }

        public Color GetColor(string key) => TryGet(key, out Color color) ? color : defaultColor;

        private void BuildMap()
        {
            _colorMap = new Dictionary<string, Color>();
            foreach (var entry in entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.key))
                    _colorMap[entry.key] = entry.color;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => BuildMap();
#endif
    }
}
