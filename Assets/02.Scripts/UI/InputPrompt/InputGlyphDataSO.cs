using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.InputDefine;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 컨트롤 경로(controlPath) → 키캡 글리프 스프라이트 매핑 데이터.
    /// 디자이너가 코드 없이 글리프를 추가/교체할 수 있도록 ScriptableObject로 외부화.
    ///
    /// controlPath는 InputAction.GetBindingDisplayString이 돌려주는 컨트롤 경로다.
    ///   예) 키보드 "1", 마우스 "leftButton", 게임패드 "buttonWest", "dpad/up"
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/Input/Glyph Data", fileName = "InputGlyphData")]
    public class InputGlyphDataSO : ScriptableObject
    {
        [Serializable]
        public struct GlyphEntry
        {
            [Tooltip("controlPath. 예: 1, leftButton, buttonWest, dpad/up")]
            public string controlPath;

            [Tooltip("키캡형 글리프 스프라이트")]
            public Sprite sprite;
        }

        [Header("키보드 / 마우스 글리프")]
        [SerializeField] private List<GlyphEntry> _keyboardMouseGlyphs = new();

        [Header("게임패드 글리프 (제네릭 — 기본/폴백)")]
        [SerializeField] private List<GlyphEntry> _gamepadGlyphs = new();

        [Header("게임패드 브랜드별 오버라이드 (선택 — 비우면 제네릭으로 폴백)")]
        [SerializeField] private List<GlyphEntry> _xboxGlyphs = new();
        [SerializeField] private List<GlyphEntry> _playStationGlyphs = new();
        [SerializeField] private List<GlyphEntry> _switchGlyphs = new();

        // controlPath(정규화) → Sprite. OnEnable에서 1회 빌드.
        private Dictionary<string, Sprite> _keyboardMouseLookup;
        private Dictionary<string, Sprite> _gamepadLookup;
        private Dictionary<string, Sprite> _xboxLookup;
        private Dictionary<string, Sprite> _playStationLookup;
        private Dictionary<string, Sprite> _switchLookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _keyboardMouseLookup = BuildFrom(_keyboardMouseGlyphs);
            _gamepadLookup = BuildFrom(_gamepadGlyphs);
            _xboxLookup = BuildFrom(_xboxGlyphs);
            _playStationLookup = BuildFrom(_playStationGlyphs);
            _switchLookup = BuildFrom(_switchGlyphs);
        }

        private static Dictionary<string, Sprite> BuildFrom(List<GlyphEntry> entries)
        {
            var dict = new Dictionary<string, Sprite>(entries.Count);
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.controlPath) || e.sprite == null)
                    continue;
                dict[Normalize(e.controlPath)] = e.sprite;
            }
            return dict;
        }

        // controlPath 정규화: 대소문자/슬래시 차이를 흡수. 매핑 등록과 조회가 같은 규칙을 쓰도록 1곳에 고정.
        private static string Normalize(string controlPath)
        {
            return controlPath == null ? string.Empty : controlPath.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 활성 디바이스 + 게임패드 브랜드 + controlPath에 대응하는 키캡 스프라이트를 찾는다.
        /// 게임패드는 브랜드별 오버라이드를 먼저 보고, 없으면 제네릭 세트로 폴백한다.
        /// </summary>
        public bool TryResolve(ActiveInputDevice device, GamepadBrand brand, string controlPath, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(controlPath))
                return false;

            // 도메인 리로드 비활성/에디터 일부 경로에서 OnEnable이 누락될 수 있으므로 방어.
            if (_keyboardMouseLookup == null || _gamepadLookup == null)
                BuildLookup();

            string key = Normalize(controlPath);

            if (device == ActiveInputDevice.Gamepad)
            {
                var brandLookup = BrandLookup(brand);
                if (brandLookup != null && brandLookup.TryGetValue(key, out sprite))
                    return true;
                return _gamepadLookup.TryGetValue(key, out sprite); // 제네릭 폴백
            }

            return _keyboardMouseLookup.TryGetValue(key, out sprite);
        }

        /// <summary>제네릭 브랜드(Generic)로 해석하는 단축 오버로드.</summary>
        public bool TryResolve(ActiveInputDevice device, string controlPath, out Sprite sprite)
            => TryResolve(device, GamepadBrand.Generic, controlPath, out sprite);

        private Dictionary<string, Sprite> BrandLookup(GamepadBrand brand)
        {
            switch (brand)
            {
                case GamepadBrand.Xbox: return _xboxLookup;
                case GamepadBrand.PlayStation: return _playStationLookup;
                case GamepadBrand.Switch: return _switchLookup;
                default: return null; // Generic → 제네릭 세트만 사용
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 전용: 에셋에서 추출한 controlPath 목록으로 엔트리를 동기화한다.
        /// 기존에 할당한 스프라이트는 controlPath 기준으로 보존하고, 누락 경로는 추가, 사라진 경로는 제거한다.
        /// (스프라이트 할당은 디자이너 수작업으로 남는다)
        /// </summary>
        public void EditorSyncControlPaths(IReadOnlyList<string> keyboardMousePaths,
            IReadOnlyList<string> gamepadPaths)
        {
            _keyboardMouseGlyphs = MergeEntries(_keyboardMouseGlyphs, keyboardMousePaths);
            _gamepadGlyphs = MergeEntries(_gamepadGlyphs, gamepadPaths);
            BuildLookup();
        }

        /// <summary>
        /// 에디터 전용 옵트인: 특정 브랜드 오버라이드 리스트를 게임패드 controlPath로 채운다.
        /// 해당 브랜드 전용 아트가 있을 때만 사용한다(없으면 제네릭으로 폴백되므로 채울 필요 없음).
        /// </summary>
        public void EditorSyncBrandControlPaths(GamepadBrand brand, IReadOnlyList<string> gamepadPaths)
        {
            switch (brand)
            {
                case GamepadBrand.Xbox:
                    _xboxGlyphs = MergeEntries(_xboxGlyphs, gamepadPaths);
                    break;
                case GamepadBrand.PlayStation:
                    _playStationGlyphs = MergeEntries(_playStationGlyphs, gamepadPaths);
                    break;
                case GamepadBrand.Switch:
                    _switchGlyphs = MergeEntries(_switchGlyphs, gamepadPaths);
                    break;
            }
            BuildLookup();
        }

        private static List<GlyphEntry> MergeEntries(List<GlyphEntry> existing, IReadOnlyList<string> paths)
        {
            var byPath = new Dictionary<string, Sprite>();
            foreach (var e in existing)
            {
                if (!string.IsNullOrEmpty(e.controlPath))
                    byPath[e.controlPath] = e.sprite; // 기존 스프라이트 보존
            }

            var result = new List<GlyphEntry>(paths.Count);
            foreach (var p in paths)
            {
                byPath.TryGetValue(p, out var sprite);
                result.Add(new GlyphEntry { controlPath = p, sprite = sprite });
            }
            return result;
        }

        /// <summary>글리프 카테고리(자동 연결 대상 리스트 식별용).</summary>
        public enum GlyphCategory { KeyboardMouse, Gamepad, Xbox, PlayStation, Switch }

        /// <summary>
        /// 에디터 전용: controlPath→Sprite 매핑으로 해당 카테고리 엔트리의 스프라이트를 채운다.
        /// overwriteExisting=false면 이미 스프라이트가 있는 엔트리는 건너뛴다. 할당한 개수를 반환.
        /// </summary>
        public int EditorAssignSprites(GlyphCategory category,
            IReadOnlyDictionary<string, Sprite> spritesByControlPath, bool overwriteExisting)
        {
            var list = ListFor(category);
            if (list == null) return 0;

            int assigned = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (!overwriteExisting && e.sprite != null)
                    continue;
                if (string.IsNullOrEmpty(e.controlPath))
                    continue;
                if (spritesByControlPath.TryGetValue(e.controlPath, out var sprite) && sprite != null)
                {
                    e.sprite = sprite;
                    list[i] = e; // GlyphEntry는 구조체 — 다시 대입해야 반영됨
                    assigned++;
                }
            }
            BuildLookup();
            return assigned;
        }

        private List<GlyphEntry> ListFor(GlyphCategory category)
        {
            switch (category)
            {
                case GlyphCategory.KeyboardMouse: return _keyboardMouseGlyphs;
                case GlyphCategory.Gamepad: return _gamepadGlyphs;
                case GlyphCategory.Xbox: return _xboxGlyphs;
                case GlyphCategory.PlayStation: return _playStationGlyphs;
                case GlyphCategory.Switch: return _switchGlyphs;
                default: return null;
            }
        }
#endif
    }
}
