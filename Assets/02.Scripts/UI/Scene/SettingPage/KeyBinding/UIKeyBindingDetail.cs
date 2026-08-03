using System;
using System.Collections.Generic;
using TMPro;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 키 설정 화면 우측 상세 패널.
    /// 선택한 액션의 이름·설명, 두 장치의 현재 바인딩, 그리고 인라인 키 변경 영역을 담는다.
    /// 기존 전체 화면 모달 캡처 오버레이를 대체한다.
    /// </summary>
    public sealed class UIKeyBindingDetail : MonoBehaviour
    {
        private static readonly Color PanelBg = new(0.055f, 0.075f, 0.105f, 0.98f);
        private static readonly Color BoxBg = new(0.075f, 0.105f, 0.15f, 1f);
        private static readonly Color Accent = new(0.32f, 0.58f, 1f, 1f);
        private static readonly Color TextMain = new(0.92f, 0.95f, 1f, 1f);
        private static readonly Color TextSub = new(0.62f, 0.68f, 0.76f, 1f);
        private static readonly Color Divider = new(0.22f, 0.26f, 0.33f, 1f);
        private static readonly Color CaptureIdle = new(0.07f, 0.10f, 0.15f, 1f);
        private static readonly Color CaptureActive = new(0.13f, 0.24f, 0.42f, 1f);

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _description;
        private UIKeyCapStrip _keyboardStrip;
        private UIKeyCapStrip _gamepadStrip;
        private TextMeshProUGUI _keyboardSecondary;
        private TextMeshProUGUI _gamepadSecondary;
        private Image _captureBox;
        private TextMeshProUGUI _captureText;
        private TextMeshProUGUI _captureHint;
        private GameObject _body;

        private GameObject _conflictActions;
        private Button _replaceButton;

        private Action<InputBindingDeviceGroup, InputBindingSlot> _onRequestCapture;
        private Action<bool> _onConflictDecision;

        public void Build(
            Action<InputBindingDeviceGroup, InputBindingSlot> onRequestCapture,
            Action<bool> onConflictDecision)
        {
            _onRequestCapture = onRequestCapture;
            _onConflictDecision = onConflictDecision;

            UGuiFactory.AddImage(gameObject, PanelBg);
            VerticalLayoutGroup root = UGuiFactory.AddVLG(gameObject, spacing: 18f, padding: 32);
            root.childForceExpandHeight = false;

            _title = UGuiFactory.MakeText(transform, "—", 34f, TextMain,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UGuiFactory.SetSize(_title.gameObject, minH: 48f, prefH: 48f, flexH: 0f);

            _description = UGuiFactory.MakeText(transform, string.Empty, 20f, TextSub);
            _description.overflowMode = TextOverflowModes.Overflow;
            _description.enableWordWrapping = true;
            UGuiFactory.SetSize(_description.gameObject, minH: 68f, prefH: 68f, flexH: 0f);

            _body = UGuiFactory.NewRect("Body", transform).gameObject;
            VerticalLayoutGroup bodyLayout = UGuiFactory.AddVLG(_body, spacing: 12f, padding: 0);
            bodyLayout.childForceExpandHeight = false;
            UGuiFactory.SetSize(_body, flexH: 1f);

            BuildCurrentBindingSection(_body.transform);
            UGuiFactory.MakeSeparator(_body.transform, Divider);
            BuildCaptureSection(_body.transform);

            SetSelection(null);
        }

        private void BuildCurrentBindingSection(Transform parent)
        {
            TextMeshProUGUI header = UGuiFactory.MakeText(
                parent, "현재 바인딩", 21f, TextMain,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UGuiFactory.SetSize(header.gameObject, minH: 34f, prefH: 34f, flexH: 0f);

            RectTransform row = UGuiFactory.NewRect("BindingRow", parent);
            HorizontalLayoutGroup layout = UGuiFactory.AddHLG(row.gameObject, spacing: 10f, padding: 0, forceExpandWidth: true);
            layout.childAlignment = TextAnchor.UpperCenter;
            UGuiFactory.SetSize(row.gameObject, minH: 184f, prefH: 184f, flexH: 0f);

            _keyboardStrip = BuildDeviceBox(
                row, "키보드 / 마우스", InputBindingDeviceGroup.KeyboardMouse, out _keyboardSecondary);
            _gamepadStrip = BuildDeviceBox(
                row, "게임패드", InputBindingDeviceGroup.Gamepad, out _gamepadSecondary);
        }

        /// <summary>
        /// 장치 1개 분량의 박스. Primary 칩을 크게 보여주고, 아래 줄에서 Secondary 슬롯을 만진다.
        /// 목업에는 Secondary가 없지만 시스템이 이미 2슬롯을 지원하므로 접근 경로를 남긴다.
        /// </summary>
        private UIKeyCapStrip BuildDeviceBox(
            Transform parent,
            string caption,
            InputBindingDeviceGroup deviceGroup,
            out TextMeshProUGUI secondaryLabel)
        {
            RectTransform box = UGuiFactory.NewRect("DeviceBox_" + deviceGroup, parent);
            UGuiFactory.AddImage(box.gameObject, BoxBg);
            VerticalLayoutGroup layout = UGuiFactory.AddVLG(box.gameObject, spacing: 8f, padding: 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            UGuiFactory.SetSize(
                box.gameObject, flexW: 1f, minH: 184f, prefH: 184f, flexH: 0f);

            TextMeshProUGUI captionLabel = UGuiFactory.MakeText(
                box, caption, 17f, TextSub, TextAlignmentOptions.Center);
            UGuiFactory.SetSize(captionLabel.gameObject, minH: 24f, prefH: 24f, flexH: 0f);

            RectTransform primaryRow = UGuiFactory.NewRect("Primary", box);
            var strip = primaryRow.gameObject.AddComponent<UIKeyCapStrip>();
            UGuiFactory.SetSize(primaryRow.gameObject, minH: 52f, prefH: 52f, flexH: 0f);

            Button primaryButton = UGuiFactory.MakeButton(
                box, "이 장치 키 변경", 16f, CaptureIdle, TextTint(Accent), out _);
            UGuiFactory.SetSize(primaryButton.gameObject, minH: 36f, prefH: 36f, flexH: 0f);
            primaryButton.onClick.AddListener(() =>
                _onRequestCapture?.Invoke(deviceGroup, InputBindingSlot.Primary));

            Button secondaryButton = UGuiFactory.MakeButton(
                box, "보조: -", 15f, CaptureIdle, TextSub, out secondaryLabel);
            UGuiFactory.SetSize(secondaryButton.gameObject, minH: 30f, prefH: 30f, flexH: 0f);
            secondaryButton.onClick.AddListener(() =>
                _onRequestCapture?.Invoke(deviceGroup, InputBindingSlot.Secondary));

            return strip;
        }

        private static Color TextTint(Color color) => color;

        private void BuildCaptureSection(Transform parent)
        {
            TextMeshProUGUI header = UGuiFactory.MakeText(
                parent, "키 변경", 21f, Accent,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UGuiFactory.SetSize(header.gameObject, minH: 34f, prefH: 34f, flexH: 0f);

            _captureText = UGuiFactory.MakeText(
                parent, "변경할 키나 버튼을 입력하세요.", 18f, TextSub);
            UGuiFactory.SetSize(_captureText.gameObject, minH: 28f, prefH: 28f, flexH: 0f);

            RectTransform box = UGuiFactory.NewRect("CaptureBox", parent);
            _captureBox = UGuiFactory.AddImage(box.gameObject, CaptureIdle);
            UGuiFactory.SetSize(box.gameObject, minH: 60f, prefH: 60f, flexH: 0f);

            TextMeshProUGUI boxText = UGuiFactory.MakeText(
                box, string.Empty, 20f, TextMain, TextAlignmentOptions.Center);
            var boxTextRect = (RectTransform)boxText.transform;
            boxTextRect.anchorMin = Vector2.zero;
            boxTextRect.anchorMax = Vector2.one;
            boxTextRect.offsetMin = new Vector2(10f, 0f);
            boxTextRect.offsetMax = new Vector2(-10f, 0f);
            _captureHint = boxText;

            TextMeshProUGUI hints = UGuiFactory.MakeText(
                parent,
                "길게 누르기: Esc / 게임패드 B 취소     Backspace / Delete 제거",
                16f, TextSub);
            UGuiFactory.SetSize(hints.gameObject, minH: 28f, prefH: 28f, flexH: 0f);

            BuildConflictActions(parent);
        }

        // 충돌 시에만 뜨는 대체/취소. 기존 모달 오버레이를 대신한다.
        private void BuildConflictActions(Transform parent)
        {
            RectTransform row = UGuiFactory.NewRect("ConflictActions", parent);
            UGuiFactory.AddHLG(row.gameObject, spacing: 8f, padding: 0, forceExpandWidth: true);
            UGuiFactory.SetSize(row.gameObject, minH: 42f, prefH: 42f, flexH: 0f);
            _conflictActions = row.gameObject;

            _replaceButton = UGuiFactory.MakeButton(
                row, "대체", 17f, CaptureActive, TextMain, out _);
            _replaceButton.onClick.AddListener(() => _onConflictDecision?.Invoke(true));

            Button cancel = UGuiFactory.MakeButton(
                row, "취소", 17f, CaptureIdle, TextSub, out _);
            cancel.onClick.AddListener(() => _onConflictDecision?.Invoke(false));

            _conflictActions.SetActive(false);
        }

        /// <summary>선택된 액션 정보를 채운다. null이면 안내 상태로 되돌린다.</summary>
        public void SetSelection(KeyBindingSelection? selection)
        {
            if (selection == null)
            {
                if (_title != null) _title.text = "액션을 선택하세요";
                if (_description != null) _description.text = "목록에서 항목을 고르면 상세 정보가 표시됩니다.";
                if (_body != null) _body.SetActive(false);
                return;
            }

            KeyBindingSelection value = selection.Value;
            if (_body != null) _body.SetActive(true);
            if (_title != null) _title.text = value.DisplayName;
            if (_description != null) _description.text = value.Description ?? string.Empty;

            ApplyStrip(_keyboardStrip, value.KeyboardParts, value.KeyboardDisplay);
            ApplyStrip(_gamepadStrip, value.GamepadParts, value.GamepadDisplay);

            if (_keyboardSecondary != null)
                _keyboardSecondary.text = $"보조: {Or(value.KeyboardSecondaryDisplay)}";
            if (_gamepadSecondary != null)
                _gamepadSecondary.text = $"보조: {Or(value.GamepadSecondaryDisplay)}";
        }

        private static string Or(string display) =>
            string.IsNullOrWhiteSpace(display) || display == "미지정" ? "-" : display;

        private static void ApplyStrip(
            UIKeyCapStrip strip,
            IReadOnlyList<GlyphPart> parts,
            string fallbackDisplay)
        {
            if (strip == null)
                return;

            if (parts != null && parts.Count > 0)
                strip.SetParts(parts);
            else
                strip.SetText(fallbackDisplay);
        }

        /// <summary>캡처 진행 상태를 인라인으로 반영한다.</summary>
        public void SetCaptureState(bool active, string message, string firstControlDisplay)
        {
            if (_conflictActions != null)
                _conflictActions.SetActive(false);

            if (_captureBox != null)
                _captureBox.color = active ? CaptureActive : CaptureIdle;

            if (_captureText != null)
            {
                _captureText.text = active
                    ? (string.IsNullOrWhiteSpace(message) ? "입력을 기다리는 중…" : message)
                    : (string.IsNullOrWhiteSpace(message)
                        ? "변경할 키나 버튼을 입력하세요."
                        : message);
            }

            if (_captureHint != null)
            {
                _captureHint.text = string.IsNullOrWhiteSpace(firstControlDisplay)
                    ? string.Empty
                    : active
                        ? $"{firstControlDisplay} + …"
                        : firstControlDisplay;
            }
        }

        /// <summary>
        /// 충돌 안내를 캡처 영역에 인라인으로 띄운다.
        /// <paramref name="allowReplace"/>가 false면 필수 키 등으로 대체가 불가능한 경우다.
        /// </summary>
        public void SetConflictMessage(string message, bool allowReplace)
        {
            if (_captureText != null)
                _captureText.text = message;
            if (_captureHint != null)
                _captureHint.text = string.Empty;
            if (_captureBox != null)
                _captureBox.color = CaptureIdle;

            if (_conflictActions != null)
                _conflictActions.SetActive(true);
            if (_replaceButton != null)
                _replaceButton.interactable = allowReplace;
        }
    }

    /// <summary>상세 패널이 그릴 한 액션 분량의 데이터.</summary>
    public readonly struct KeyBindingSelection
    {
        public readonly string DisplayName;
        public readonly string Description;
        public readonly IReadOnlyList<GlyphPart> KeyboardParts;
        public readonly IReadOnlyList<GlyphPart> GamepadParts;
        public readonly string KeyboardDisplay;
        public readonly string GamepadDisplay;
        public readonly string KeyboardSecondaryDisplay;
        public readonly string GamepadSecondaryDisplay;

        public KeyBindingSelection(
            string displayName,
            string description,
            IReadOnlyList<GlyphPart> keyboardParts,
            IReadOnlyList<GlyphPart> gamepadParts,
            string keyboardDisplay,
            string gamepadDisplay,
            string keyboardSecondaryDisplay,
            string gamepadSecondaryDisplay)
        {
            DisplayName = displayName;
            Description = description;
            KeyboardParts = keyboardParts;
            GamepadParts = gamepadParts;
            KeyboardDisplay = keyboardDisplay;
            GamepadDisplay = gamepadDisplay;
            KeyboardSecondaryDisplay = keyboardSecondaryDisplay;
            GamepadSecondaryDisplay = gamepadSecondaryDisplay;
        }
    }
}
