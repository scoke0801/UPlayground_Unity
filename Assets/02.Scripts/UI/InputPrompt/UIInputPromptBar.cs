using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 현재 장치에서 사용할 수 있는 액션만 [글리프] 라벨 형태로 표시하는 공용 프롬프트 바.
    /// 장치 전환과 리바인딩을 즉시 반영하며, 유효한 항목이 없으면 표시 루트만 숨긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIInputPromptBar : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            public string mapName = InputMapNames.UI;
            public string actionName;
            public string label;
            public DevicePromptFilter deviceFilter = DevicePromptFilter.Any;
        }

        [Header("프롬프트")]
        [SerializeField] private List<Entry> _entries = new();
        [SerializeField] private InputGlyphDataSO _glyphData;

        [Header("레이아웃")]
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField, Min(12f)] private float _fontSize = 18f;
        [SerializeField, Min(12f)] private float _glyphSize = 28f;
        [SerializeField, Min(0f)] private float _itemSpacing = 8f;
        [SerializeField, Min(0f)] private float _entrySpacing = 24f;
        [SerializeField] private Color _textColor = new(0.78f, 0.82f, 0.88f, 1f);

        private IInputService _input;

        public IReadOnlyList<Entry> Entries => _entries;
        public bool HasVisibleEntries { get; private set; }

        private void Awake()
        {
            EnsureContentRoot();
        }

        private void OnEnable()
        {
            _input = Svc.Input;
            if (_input != null)
            {
                _input.OnActiveDeviceChanged += OnActiveDeviceChanged;
                _input.OnBindingsChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.OnActiveDeviceChanged -= OnActiveDeviceChanged;
                _input.OnBindingsChanged -= Refresh;
                _input = null;
            }
        }

        private void OnActiveDeviceChanged(ActiveInputDevice device) => Refresh();

        public void Refresh()
        {
            if (_input == null)
                _input = Svc.Input;

            EnsureContentRoot();
            ClearGeneratedItems();

            if (_input == null)
            {
                SetContentVisible(false);
                return;
            }

            ActiveInputDevice device = _input.ActiveDevice;
            GamepadBrand brand = _input.GamepadBrand;
            int visibleCount = 0;

            foreach (Entry entry in _entries)
            {
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.actionName)
                    || !MatchesFilter(entry.deviceFilter, device))
                {
                    continue;
                }

                InputGlyphResult result = InputGlyphResolver.Resolve(
                    entry.mapName,
                    entry.actionName,
                    device,
                    brand,
                    _glyphData);
                if (!result.IsValid)
                    continue;

                BuildEntry(entry, result);
                visibleCount++;
            }

            HasVisibleEntries = visibleCount > 0;
            SetContentVisible(HasVisibleEntries);
        }

        private void EnsureContentRoot()
        {
            if (_contentRoot != null)
                return;

            var content = new GameObject(
                "PromptContent",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            content.transform.SetParent(transform, false);
            _contentRoot = (RectTransform)content.transform;
            _contentRoot.anchorMin = Vector2.zero;
            _contentRoot.anchorMax = Vector2.one;
            _contentRoot.offsetMin = Vector2.zero;
            _contentRoot.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = _entrySpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void ClearGeneratedItems()
        {
            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _contentRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private void BuildEntry(Entry entry, InputGlyphResult result)
        {
            var item = new GameObject(
                string.IsNullOrWhiteSpace(entry.actionName) ? "Prompt" : entry.actionName,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            item.transform.SetParent(_contentRoot, false);

            HorizontalLayoutGroup layout = item.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = _itemSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = item.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < result.Count; i++)
            {
                if (i > 0)
                    AddText(item.transform, "+", _fontSize);
                AddGlyph(item.transform, result.Parts[i]);
            }

            if (!string.IsNullOrWhiteSpace(entry.label))
                AddText(item.transform, entry.label, _fontSize);
        }

        private void AddGlyph(Transform parent, in GlyphPart part)
        {
            if (part.HasSprite)
            {
                var glyph = new GameObject(
                    "Glyph",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(LayoutElement));
                glyph.transform.SetParent(parent, false);
                Image image = glyph.GetComponent<Image>();
                image.sprite = part.Sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;

                LayoutElement element = glyph.GetComponent<LayoutElement>();
                element.preferredWidth = _glyphSize;
                element.preferredHeight = _glyphSize;
                return;
            }

            AddText(parent, part.Text, _fontSize);
        }

        private void AddText(Transform parent, string value, float size)
        {
            var label = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(ContentSizeFitter));
            label.transform.SetParent(parent, false);

            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = _textColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            ContentSizeFitter fitter = label.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void SetContentVisible(bool visible)
        {
            HasVisibleEntries = visible;
            if (_contentRoot != null)
                _contentRoot.gameObject.SetActive(visible);

            if (TryGetComponent(out LayoutElement layoutElement))
                layoutElement.ignoreLayout = !visible;
        }

        internal static bool MatchesFilter(
            DevicePromptFilter filter,
            ActiveInputDevice device)
        {
            return filter switch
            {
                DevicePromptFilter.GamepadOnly => device == ActiveInputDevice.Gamepad,
                DevicePromptFilter.KeyboardMouseOnly => device == ActiveInputDevice.KeyboardMouse,
                _ => true,
            };
        }
    }
}
