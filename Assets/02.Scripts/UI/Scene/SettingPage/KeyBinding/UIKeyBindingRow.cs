using System;
using System.Collections.Generic;
using TMPro;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 키 설정 목록의 한 행. 액션 이름과 두 장치의 바인딩을 동시에 보여준다.
    ///
    /// 행 자체가 <see cref="Selectable"/>이라 게임패드 내비게이션과
    /// <c>UIFocusIndicator</c>가 그대로 동작한다. 클릭·확정은 즉시 캡처가 아니라
    /// "선택"이며, 실제 키 변경은 우측 상세 패널에서 처리한다(목업 흐름).
    /// </summary>
    public sealed class UIKeyBindingRow : MonoBehaviour, ISelectHandler, IPointerEnterHandler,
        IUIFocusPresentation
    {
        /// <summary>헤더와 행의 컬럼 폭은 반드시 같은 상수를 쓴다.</summary>
        public const float KeyboardColumnWidth = 220f;
        public const float GamepadColumnWidth = 220f;
        public const float RowHeight = 54f;

        private static readonly Color RowNormal = new(1f, 1f, 1f, 0f);
        private static readonly Color RowSelected = new(0.13f, 0.24f, 0.42f, 0.95f);
        private static readonly Color SelectionAccent = new(0.32f, 0.58f, 1f, 1f);
        private static readonly Color NameText = new(0.85f, 0.89f, 0.95f, 1f);
        private static readonly Color NameTextUnbound = new(0.55f, 0.59f, 0.66f, 1f);

        private Image _background;
        private Image _selectionAccent;
        private TextMeshProUGUI _nameLabel;
        private UIKeyCapStrip _keyboardStrip;
        private UIKeyCapStrip _gamepadStrip;
        private Button _button;

        private Action<UIKeyBindingRow> _onSelected;

        /// <summary>이 행이 대표하는 액션. 상세 패널이 이 값으로 내용을 채운다.</summary>
        public string MapName { get; private set; }
        public string ActionName { get; private set; }

        public Selectable Selectable => _button;
        public bool SuppressGlobalFocusIndicator => true;
        public RectTransform GlobalFocusIndicatorTarget => null;

        public void Build()
        {
            _background = UGuiFactory.AddImage(gameObject, RowNormal);
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _background;
            _button.transition = Selectable.Transition.None; // 하이라이트는 직접 칠한다.
            _button.onClick.AddListener(NotifySelected);

            HorizontalLayoutGroup layout = UGuiFactory.AddHLG(gameObject, spacing: 8f, padding: 0);
            layout.padding = new RectOffset(22, 18, 0, 0);

            RectTransform accentRect = UGuiFactory.NewRect("SelectionAccent", transform);
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            var accentLayout = accentRect.gameObject.AddComponent<LayoutElement>();
            accentLayout.ignoreLayout = true;
            _selectionAccent = UGuiFactory.AddImage(accentRect.gameObject, SelectionAccent);
            _selectionAccent.raycastTarget = false;
            _selectionAccent.enabled = false;

            _nameLabel = UGuiFactory.MakeText(transform, string.Empty, 17f, NameText);
            UGuiFactory.SetSize(_nameLabel.gameObject, flexW: 1f);

            _keyboardStrip = MakeStrip("KeyboardStrip", KeyboardColumnWidth);
            _gamepadStrip = MakeStrip("GamepadStrip", GamepadColumnWidth);

            UGuiFactory.SetSize(gameObject, minH: RowHeight, prefH: RowHeight, flexH: 0f);
        }

        private UIKeyCapStrip MakeStrip(string name, float width)
        {
            RectTransform rect = UGuiFactory.NewRect(name, transform);
            var strip = rect.gameObject.AddComponent<UIKeyCapStrip>();
            UGuiFactory.SetSize(rect.gameObject, minW: width, prefW: width);
            return strip;
        }

        public void Configure(
            string mapName,
            string actionName,
            string displayName,
            bool hasAnyBinding,
            Action<UIKeyBindingRow> onSelected)
        {
            MapName = mapName;
            ActionName = actionName;
            _onSelected = onSelected;

            if (_nameLabel != null)
            {
                _nameLabel.text = displayName;
                _nameLabel.color = hasAnyBinding ? NameText : NameTextUnbound;
            }
        }

        public void SetKeyboardParts(IReadOnlyList<GlyphPart> parts, string fallbackDisplay)
        {
            Apply(_keyboardStrip, parts, fallbackDisplay);
        }

        public void SetGamepadParts(IReadOnlyList<GlyphPart> parts, string fallbackDisplay)
        {
            Apply(_gamepadStrip, parts, fallbackDisplay);
        }

        private static void Apply(
            UIKeyCapStrip strip,
            IReadOnlyList<GlyphPart> parts,
            string fallbackDisplay)
        {
            if (strip == null)
                return;

            // 글리프 해석이 되면 스프라이트/브랜드 표기를 살리고, 실패하면 서술자의 사람이 읽는
            // 문자열로 떨어뜨린다. 둘 다 없으면 "-"가 된다.
            if (parts != null && parts.Count > 0)
                strip.SetParts(parts);
            else
                strip.SetText(fallbackDisplay);
        }

        public void SetSelectedVisual(bool selected)
        {
            if (_background != null)
                _background.color = selected ? RowSelected : RowNormal;
            if (_selectionAccent != null)
                _selectionAccent.enabled = selected;
        }

        // 게임패드 내비게이션으로 들어온 선택도 상세 패널에 반영한다.
        public void OnSelect(BaseEventData eventData) => NotifySelected();

        // 마우스로 훑을 때도 상세가 따라오면 목업의 정보 밀도를 유지할 수 있다.
        public void OnPointerEnter(PointerEventData eventData) => NotifySelected();

        private void NotifySelected() => _onSelected?.Invoke(this);

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(NotifySelected);
        }
    }
}
