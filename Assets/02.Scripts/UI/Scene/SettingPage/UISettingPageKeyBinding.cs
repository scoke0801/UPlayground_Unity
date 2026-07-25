using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UPlayGround.Data.Config;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 키 설정 페이지. 좌측 카테고리 레일 · 중앙 그룹 목록(두 장치 동시 표시) · 우측 상세 패널의
    /// 3분할 구성이다.
    ///
    /// 계층을 코드로 조립한다. 행 수가 액션 수에 따라 달라지고 키캡 칩 개수도 바인딩 형태
    /// (단일 / 조합)에 따라 변하므로 프리팹으로 고정 저작할 수 없는 구조다. 프로젝트에는
    /// <c>UI_DevCheatPanel</c>이 같은 방식을 쓰고 있고, 공용 헬퍼는 <see cref="UGuiFactory"/>다.
    ///
    /// 데이터·정책은 기존 것을 그대로 쓴다: <c>GetBindingDescriptors</c>(장치별로 2번 호출해
    /// 액션 기준으로 머지), <c>CaptureBindingAsync</c>, 충돌 정책, 프로필 스냅샷.
    /// </summary>
    public class UISettingPageKeyBinding : UISettingPageBase
    {
        [Header("Glyph")]
        [Tooltip("Assets/10.Datas/UI/Input/InputGlyphData.asset. 비어 있으면 텍스트 키캡으로 표시된다.")]
        [SerializeField] private InputGlyphDataSO _glyphData;

        private static readonly Color ListBg = new(0.055f, 0.075f, 0.105f, 0.98f);
        private static readonly Color RailBg = new(0.04f, 0.06f, 0.085f, 0.98f);
        private static readonly Color RailItemOn = new(0.13f, 0.24f, 0.42f, 1f);
        private static readonly Color RailItemOff = new(1f, 1f, 1f, 0f);
        private static readonly Color RailAccent = new(0.32f, 0.58f, 1f, 1f);
        private static readonly Color SectionText = new(0.92f, 0.95f, 1f, 1f);
        private static readonly Color HeaderText = new(0.66f, 0.72f, 0.82f, 1f);
        private static readonly Color TextMain = new(0.85f, 0.89f, 0.95f, 1f);
        private static readonly Color Divider = new(0.16f, 0.21f, 0.29f, 1f);
        private static readonly Color ResetBg = new(0.08f, 0.11f, 0.16f, 1f);

        private const float RailWidth = 280f;
        private const float DetailWidth = 440f;

        /// <summary>액션 1개에 대한 두 장치·두 슬롯 서술자 묶음.</summary>
        private readonly struct MergedBinding
        {
            public readonly InputBindingDescriptor KeyboardPrimary;
            public readonly InputBindingDescriptor GamepadPrimary;
            public readonly InputBindingDescriptor KeyboardSecondary;
            public readonly InputBindingDescriptor GamepadSecondary;

            public MergedBinding(
                InputBindingDescriptor keyboardPrimary,
                InputBindingDescriptor gamepadPrimary,
                InputBindingDescriptor keyboardSecondary,
                InputBindingDescriptor gamepadSecondary)
            {
                KeyboardPrimary = keyboardPrimary;
                GamepadPrimary = gamepadPrimary;
                KeyboardSecondary = keyboardSecondary;
                GamepadSecondary = gamepadSecondary;
            }

            public string MapName => KeyboardPrimary.Target.mapName;
            public string ActionName => KeyboardPrimary.Target.actionName;
            public string DisplayName => KeyboardPrimary.DisplayName;
            public string Description => KeyboardPrimary.Description;
            public InputBindingCategory Category => KeyboardPrimary.Category;

            public bool HasAnyBinding =>
                KeyboardPrimary.HasBinding || GamepadPrimary.HasBinding;
        }

        private IInputService _inputService;
        private InputBindingCategory? _category;
        private bool _built;

        private RectTransform _railContent;
        private RectTransform _listContent;
        private UIKeyBindingDetail _detail;
        private ScrollRect _listScroll;

        private readonly List<Button> _railButtons = new();
        private readonly List<Image> _railBackgrounds = new();
        private readonly List<Image> _railAccents = new();
        private readonly List<TextMeshProUGUI> _railLabels = new();
        private readonly List<InputBindingCategory?> _railCategories = new();
        private readonly List<UIKeyBindingRow> _rows = new();
        private readonly List<MergedBinding> _merged = new();
        private readonly Dictionary<InputBindingTarget, InputRebindCaptureResult> _pendingCaptures = new();
        private readonly HashSet<InputBindingTarget> _pendingClears = new();
        private readonly HashSet<string> _pendingActionResets = new();
        private readonly HashSet<InputBindingTarget> _replaceApprovedTargets = new();

        private UIKeyBindingRow _selectedRow;
        private string _selectedMap;
        private string _selectedAction;
        private bool _capturing;
        private bool _resetAllPending;
        private InputRebindCaptureResult _pendingConflictCapture;

        protected override void BindControls(SettingsData settingsData)
        {
            BuildLayout();
            BindInputService();
        }

        public override void SyncUIFromData(SettingsData settingsData)
        {
            BuildLayout();
            BindInputService();
            RefreshRows();
        }

        /// <summary>설정 메뉴를 열 때 이전 편집 대기열을 비운다.</summary>
        public void BeginEditSession()
        {
            DiscardPendingChanges();
            if (_built)
                RefreshRows();
        }

        /// <summary>
        /// 대기 중인 입력 변경을 한 번에 반영한다. 실패하면 입력 프로필 전체를 롤백한다.
        /// 충돌이 있으면 상세 패널에서 대체 여부를 정한 뒤 하단 적용을 다시 눌러야 한다.
        /// </summary>
        public bool ApplyPendingChanges()
        {
            if (_inputService == null)
                return false;

            if (!_resetAllPending
                && _pendingActionResets.Count == 0
                && _pendingCaptures.Count == 0
                && _pendingClears.Count == 0)
            {
                return true;
            }

            string snapshot = _inputService.CaptureBindingProfileSnapshot();

            if (_resetAllPending)
                _inputService.ResetBindings();
            else
            {
                foreach (string actionKey in _pendingActionResets)
                {
                    SplitActionKey(actionKey, out string mapName, out string actionName);
                    _inputService.ResetBindingsForAction(mapName, actionName);
                }
            }

            foreach (InputBindingTarget target in _pendingClears)
            {
                if (_inputService.ClearBinding(target))
                    continue;

                _inputService.RestoreBindingProfileSnapshot(snapshot);
                _detail?.SetConflictMessage(
                    "필수 액션의 기본 바인딩은 제거할 수 없습니다.",
                    allowReplace: false);
                return false;
            }

            foreach (KeyValuePair<InputBindingTarget, InputRebindCaptureResult> pair in _pendingCaptures)
            {
                bool replace = _replaceApprovedTargets.Contains(pair.Key);
                if (_inputService.TryApplyBinding(pair.Value, replace, out InputBindingConflictInfo conflict))
                    continue;

                _inputService.RestoreBindingProfileSnapshot(snapshot);
                _pendingConflictCapture = pair.Value;
                ShowConflict(conflict);
                return false;
            }

            DiscardPendingChanges();
            return true;
        }

        public void DiscardPendingChanges()
        {
            _pendingCaptures.Clear();
            _pendingClears.Clear();
            _pendingActionResets.Clear();
            _replaceApprovedTargets.Clear();
            _pendingConflictCapture = default;
            _resetAllPending = false;
        }

        public void StageResetAll()
        {
            DiscardPendingChanges();
            _resetAllPending = true;
            _detail?.SetCaptureState(
                false,
                "전체 키 설정 초기화가 대기 중입니다. 하단 적용을 눌러 확정하세요.",
                null);
        }

        /// <summary>
        /// 캡처·충돌 중의 뒤로 가기를 소비한다. true면 상위 설정 메뉴가 닫히지 않는다.
        /// </summary>
        public bool TryHandleBack()
        {
            if (_pendingConflictCapture.IsCompleted)
            {
                CancelConflict();
                return true;
            }

            // 캡처 중 Escape/B는 InputManager가 raw 입력으로 소비한다.
            return _capturing;
        }

        #region 레이아웃 구성

        private void BuildLayout()
        {
            if (_built)
                return;
            _built = true;

            // 이 페이지는 이전에 프리팹으로 저작돼 있었다. 남아 있는 자식(장치 탭, 카테고리
            // 드롭다운, 빈 목록 박스, 모달 오버레이)은 새 구성과 겹치므로 비활성화한다.
            // 파괴하지 않는 이유: 프리팹 인스턴스를 런타임에 부수면 되돌릴 수 없고,
            // Unity에서 실제 정리는 사람이 확인하고 지우는 편이 안전하다.
            for (int i = transform.childCount - 1; i >= 0; i--)
                transform.GetChild(i).gameObject.SetActive(false);

            // 프리팹 루트(Panel_Keys)에는 이미 VerticalLayoutGroup이 붙어 있다.
            // 같은 오브젝트에 레이아웃 그룹을 하나 더 얹으면 Unity가 충돌로 처리하므로
            // 루트의 그룹을 끄고, 3분할은 스트레치된 자식에서 구성한다.
            foreach (LayoutGroup existing in GetComponents<LayoutGroup>())
                existing.enabled = false;

            RectTransform host = UGuiFactory.NewStretched("KeyBindingLayout", transform);
            HorizontalLayoutGroup root = UGuiFactory.AddHLG(host.gameObject, spacing: 16f, padding: 0);
            root.childForceExpandWidth = false;
            root.childAlignment = TextAnchor.UpperLeft;

            BuildRail(host);
            BuildList(host);
            BuildDetail(host);
        }

        private void BuildRail(Transform parent)
        {
            RectTransform rail = UGuiFactory.NewRect("CategoryRail", parent);
            UGuiFactory.AddImage(rail.gameObject, RailBg);
            UGuiFactory.SetSize(rail.gameObject, minW: RailWidth, prefW: RailWidth, flexH: 1f);

            _railContent = UGuiFactory.NewStretched("Items", rail);
            VerticalLayoutGroup layout = UGuiFactory.AddVLG(_railContent.gameObject, spacing: 4f, padding: 12);
            layout.childForceExpandHeight = false;

            TextMeshProUGUI title = UGuiFactory.MakeText(
                _railContent, "카테고리", 15f, HeaderText,
                TextAlignmentOptions.Left, FontStyles.Bold);
            UGuiFactory.SetSize(title.gameObject, minH: 38f, prefH: 38f, flexH: 0f);

            AddRailItem("모든", null);
            foreach (InputBindingCategory category in Enum.GetValues(typeof(InputBindingCategory)))
                AddRailItem(InputBindingCategoryNames.ToKorean(category), category);
        }

        private void AddRailItem(string label, InputBindingCategory? category)
        {
            Button button = UGuiFactory.MakeButton(
                _railContent, label, 17f, RailItemOff, TextMain, out TextMeshProUGUI text);
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            button.transition = Selectable.Transition.None;
            UGuiFactory.SetSize(button.gameObject, minH: 54f, prefH: 54f, flexH: 0f);

            RectTransform accentRect = UGuiFactory.NewRect("SelectedAccent", button.transform);
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            Image accent = UGuiFactory.AddImage(accentRect.gameObject, RailAccent);
            accent.raycastTarget = false;

            InputBindingCategory? captured = category;
            button.onClick.AddListener(() => SelectCategory(captured));

            _railButtons.Add(button);
            _railBackgrounds.Add(button.targetGraphic as Image);
            _railAccents.Add(accent);
            _railLabels.Add(text);
            _railCategories.Add(category);
            RefreshRailVisual();
        }

        private void BuildList(Transform parent)
        {
            RectTransform panel = UGuiFactory.NewRect("BindingList", parent);
            UGuiFactory.AddImage(panel.gameObject, ListBg);
            UGuiFactory.SetSize(panel.gameObject, minW: 760f, flexW: 1f, flexH: 1f);

            VerticalLayoutGroup layout = UGuiFactory.AddVLG(panel.gameObject, spacing: 0f, padding: 0);
            layout.childForceExpandHeight = false;

            BuildColumnHeader(panel);
            UGuiFactory.MakeSeparator(panel, Divider);

            RectTransform scrollHost = UGuiFactory.NewRect("ScrollHost", panel);
            UGuiFactory.SetSize(scrollHost.gameObject, flexH: 1f);
            _listContent = UGuiFactory.MakeVerticalScroll(scrollHost, out _listScroll, spacing: 2f, padding: 0);
        }

        // 컬럼 폭은 UIKeyBindingRow의 상수를 공유해야 행과 어긋나지 않는다.
        private static void BuildColumnHeader(Transform parent)
        {
            RectTransform header = UGuiFactory.NewRect("ColumnHeader", parent);
            HorizontalLayoutGroup layout = UGuiFactory.AddHLG(header.gameObject, spacing: 8f, padding: 0);
            layout.padding = new RectOffset(18, 18, 0, 0);
            UGuiFactory.SetSize(header.gameObject, minH: 50f, prefH: 50f, flexH: 0f);

            TextMeshProUGUI spacer = UGuiFactory.MakeText(header, string.Empty, 14f, HeaderText);
            UGuiFactory.SetSize(spacer.gameObject, flexW: 1f);

            TextMeshProUGUI keyboard = UGuiFactory.MakeText(
                header, "키보드 / 마우스", 14f, HeaderText, TextAlignmentOptions.Center);
            UGuiFactory.SetSize(keyboard.gameObject,
                minW: UIKeyBindingRow.KeyboardColumnWidth, prefW: UIKeyBindingRow.KeyboardColumnWidth);

            TextMeshProUGUI gamepad = UGuiFactory.MakeText(
                header, "게임패드", 14f, HeaderText, TextAlignmentOptions.Center);
            UGuiFactory.SetSize(gamepad.gameObject,
                minW: UIKeyBindingRow.GamepadColumnWidth, prefW: UIKeyBindingRow.GamepadColumnWidth);
        }

        private void BuildDetail(Transform parent)
        {
            RectTransform panel = UGuiFactory.NewRect("Detail", parent);
            UGuiFactory.SetSize(panel.gameObject, minW: DetailWidth, prefW: DetailWidth, flexH: 1f);

            VerticalLayoutGroup layout = UGuiFactory.AddVLG(panel.gameObject, spacing: 10f, padding: 0);
            layout.childForceExpandHeight = false;

            RectTransform detailHost = UGuiFactory.NewRect("DetailBody", panel);
            UGuiFactory.SetSize(detailHost.gameObject, flexH: 1f);
            _detail = detailHost.gameObject.AddComponent<UIKeyBindingDetail>();
            _detail.Build(RequestCapture, OnConflictDecision);

            Button resetDevice = UGuiFactory.MakeButton(
                panel, "선택 액션 기본값 복원", 14f, ResetBg, HeaderText, out _);
            UGuiFactory.SetSize(resetDevice.gameObject, minH: 42f, prefH: 42f, flexH: 0f);
            resetDevice.onClick.AddListener(ResetSelectedAction);
        }

        #endregion

        #region 서비스 바인딩

        private void BindInputService()
        {
            IInputService current = Svc.Input;
            if (current == null)
            {
                UnbindInputService();
                WarnOnce("Svc.Input이 아직 준비되지 않아 키 목록을 채울 수 없습니다.");
                return;
            }

            if (_inputService == current)
                return;

            UnbindInputService();
            _inputService = current;
            _inputService.OnBindingsChanged += RefreshRows;
            _inputService.OnRebindCaptureChanged += OnCaptureStateChanged;
        }

        private void UnbindInputService()
        {
            if (_inputService != null)
            {
                _inputService.OnBindingsChanged -= RefreshRows;
                _inputService.OnRebindCaptureChanged -= OnCaptureStateChanged;
            }
            _inputService = null;
        }

        #endregion

        #region 목록 갱신

        private void SelectCategory(InputBindingCategory? category)
        {
            _category = category;
            RefreshRailVisual();
            RefreshRows();
            if (_listScroll != null)
                _listScroll.verticalNormalizedPosition = 1f;
        }

        private void RefreshRailVisual()
        {
            for (int i = 0; i < _railBackgrounds.Count; i++)
            {
                if (_railBackgrounds[i] == null)
                    continue;

                bool selected = _railCategories[i].Equals(_category);
                _railBackgrounds[i].color = selected ? RailItemOn : RailItemOff;
                _railAccents[i].enabled = selected;
                _railLabels[i].color = selected ? SectionText : TextMain;
            }
        }

        private void RefreshRows()
        {
            if (_inputService == null)
            {
                WarnOnce("IInputService가 바인딩되지 않아 키 목록을 채울 수 없습니다.");
                return;
            }
            if (_listContent == null)
                return;

            BuildMergedBindings();

            // 섹션 헤더 + 행을 매번 다시 만든다. 34개 규모라 재구축 비용이 무시할 만하고,
            // 카테고리 필터에 따라 섹션 구성 자체가 바뀌어 재사용이 오히려 복잡하다.
            ClearListContent();
            _rows.Clear();

            InputBindingCategory? currentSection = null;
            bool anyRow = false;

            for (int i = 0; i < _merged.Count; i++)
            {
                MergedBinding item = _merged[i];
                if (_category.HasValue && item.Category != _category.Value)
                    continue;

                if (currentSection == null || currentSection.Value != item.Category)
                {
                    currentSection = item.Category;
                    AddSectionHeader(InputBindingCategoryNames.ToKorean(item.Category));
                }

                AddRow(item);
                anyRow = true;
            }

            if (!anyRow)
            {
                WarnOnce($"표시할 바인딩이 없습니다. category=" +
                         $"{(_category.HasValue ? _category.Value.ToString() : "전체")}, " +
                         $"merged={_merged.Count}. merged가 0이면 액션 맵/액션 이름이 " +
                         "InputDefine 상수와 일치하지 않는 것이다.");
            }

            RestoreSelection();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);
        }

        /// <summary>
        /// 두 장치의 서술자를 액션 기준으로 합친다.
        /// <c>GetBindingDescriptors</c>는 장치별로 같은 순서·같은 액션 집합을 돌려주므로
        /// 서비스 계약을 바꾸지 않고 인덱스 대신 (map, action) 키로 맞춘다.
        /// </summary>
        private void BuildMergedBindings()
        {
            _merged.Clear();

            IReadOnlyList<InputBindingDescriptor> keyboard =
                _inputService.GetBindingDescriptors(InputBindingDeviceGroup.KeyboardMouse);
            IReadOnlyList<InputBindingDescriptor> gamepad =
                _inputService.GetBindingDescriptors(InputBindingDeviceGroup.Gamepad);

            for (int i = 0; i < keyboard.Count; i++)
            {
                InputBindingDescriptor kbPrimary = keyboard[i];
                if (kbPrimary.Target.slot != InputBindingSlot.Primary)
                    continue;

                _merged.Add(new MergedBinding(
                    kbPrimary,
                    Find(gamepad, kbPrimary, InputBindingSlot.Primary, InputBindingDeviceGroup.Gamepad),
                    Find(keyboard, kbPrimary, InputBindingSlot.Secondary, InputBindingDeviceGroup.KeyboardMouse),
                    Find(gamepad, kbPrimary, InputBindingSlot.Secondary, InputBindingDeviceGroup.Gamepad)));
            }
        }

        private static InputBindingDescriptor Find(
            IReadOnlyList<InputBindingDescriptor> source,
            InputBindingDescriptor reference,
            InputBindingSlot slot,
            InputBindingDeviceGroup deviceGroup)
        {
            for (int i = 0; i < source.Count; i++)
            {
                InputBindingDescriptor item = source[i];
                if (item.Target.slot == slot
                    && item.Target.mapName == reference.Target.mapName
                    && item.Target.actionName == reference.Target.actionName)
                {
                    return item;
                }
            }

            // 없으면 빈 서술자를 합성한다. 예외를 던지면 목록 전체가 죽는다.
            return new InputBindingDescriptor(
                new InputBindingTarget(
                    reference.Target.mapName, reference.Target.actionName, deviceGroup, slot),
                reference.DisplayName,
                reference.Description,
                reference.Category,
                "미지정",
                isComposite: false,
                isRequired: reference.IsRequired,
                isCustomized: false);
        }

        private void ClearListContent()
        {
            for (int i = _listContent.childCount - 1; i >= 0; i--)
            {
                Transform child = _listContent.GetChild(i);

                // Destroy는 프레임 끝에 처리되므로 그대로 두면 새 행과 한 프레임 공존해
                // 레이아웃이 두 배로 계산된다. 먼저 부모에서 떼어내 레이아웃에서 제외한다.
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void AddSectionHeader(string title)
        {
            RectTransform host = UGuiFactory.NewRect("Section_" + title, _listContent);
            UGuiFactory.SetSize(host.gameObject, minH: 46f, prefH: 46f, flexH: 0f);

            TextMeshProUGUI label = UGuiFactory.MakeText(
                host, title, 18f, SectionText, TextAlignmentOptions.Left, FontStyles.Bold);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 0f);
            rect.offsetMax = new Vector2(-18f, 0f);
        }

        private void AddRow(MergedBinding item)
        {
            RectTransform host = UGuiFactory.NewRect($"Row_{item.ActionName}", _listContent);
            var row = host.gameObject.AddComponent<UIKeyBindingRow>();
            row.Build();
            row.Configure(
                item.MapName,
                item.ActionName,
                item.DisplayName,
                item.HasAnyBinding,
                OnRowSelected);

            row.SetKeyboardParts(
                ResolveParts(
                    item.MapName,
                    item.ActionName,
                    ActiveInputDevice.KeyboardMouse,
                    InputBindingSlot.Primary),
                ResolveDisplay(item.KeyboardPrimary));
            row.SetGamepadParts(
                ResolveParts(
                    item.MapName,
                    item.ActionName,
                    ActiveInputDevice.Gamepad,
                    InputBindingSlot.Primary),
                ResolveDisplay(item.GamepadPrimary));

            _rows.Add(row);
        }

        /// <summary>
        /// 글리프 파트를 해석한다. 실패하면 null을 돌려주고 호출자가 텍스트 표시로 떨어뜨린다.
        /// </summary>
        private IReadOnlyList<GlyphPart> ResolveParts(
            string mapName,
            string actionName,
            ActiveInputDevice device,
            InputBindingSlot slot)
        {
            if (_glyphData == null)
                return null;

            GamepadBrand brand = device == ActiveInputDevice.Gamepad && _inputService != null
                ? _inputService.GamepadBrand
                : GamepadBrand.Generic;

            InputBindingDeviceGroup deviceGroup = device == ActiveInputDevice.Gamepad
                ? InputBindingDeviceGroup.Gamepad
                : InputBindingDeviceGroup.KeyboardMouse;
            var target = new InputBindingTarget(mapName, actionName, deviceGroup, slot);

            if (_pendingClears.Contains(target))
                return Array.Empty<GlyphPart>();

            if (_pendingCaptures.TryGetValue(target, out InputRebindCaptureResult capture))
            {
                InputGlyphResult pending = InputGlyphResolver.ResolvePaths(
                    capture.ModifierPath,
                    capture.ControlPath,
                    capture.DisplayString,
                    device,
                    brand,
                    _glyphData);
                return pending.IsValid ? pending.Parts : null;
            }

            InputGlyphResult result =
                InputGlyphResolver.Resolve(mapName, actionName, device, brand, _glyphData);
            return result.IsValid ? result.Parts : null;
        }

        private string ResolveDisplay(InputBindingDescriptor descriptor)
        {
            if (_pendingClears.Contains(descriptor.Target))
                return "-";
            return _pendingCaptures.TryGetValue(
                descriptor.Target,
                out InputRebindCaptureResult capture)
                ? capture.DisplayString
                : descriptor.BindingDisplay;
        }

        #endregion

        #region 선택과 상세

        private void OnRowSelected(UIKeyBindingRow row)
        {
            if (row == null)
                return;

            if (_selectedRow != null && _selectedRow != row)
                _selectedRow.SetSelectedVisual(false);

            _selectedRow = row;
            _selectedMap = row.MapName;
            _selectedAction = row.ActionName;
            row.SetSelectedVisual(true);

            PushSelectionToDetail();
        }

        private void RestoreSelection()
        {
            // 재구축으로 행 인스턴스가 바뀌므로 (map, action)으로 다시 찾는다.
            _selectedRow = null;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].MapName == _selectedMap && _rows[i].ActionName == _selectedAction)
                {
                    _selectedRow = _rows[i];
                    break;
                }
            }

            if (_selectedRow == null && _rows.Count > 0)
                _selectedRow = _rows[0];

            if (_selectedRow == null)
            {
                _selectedMap = null;
                _selectedAction = null;
                _detail?.SetSelection(null);
                return;
            }

            _selectedMap = _selectedRow.MapName;
            _selectedAction = _selectedRow.ActionName;
            _selectedRow.SetSelectedVisual(true);
            PushSelectionToDetail();
        }

        private void PushSelectionToDetail()
        {
            if (_detail == null)
                return;

            if (!TryGetSelected(out MergedBinding item))
            {
                _detail.SetSelection(null);
                return;
            }

            _detail.SetSelection(new KeyBindingSelection(
                item.DisplayName,
                item.Description,
                ResolveParts(
                    item.MapName,
                    item.ActionName,
                    ActiveInputDevice.KeyboardMouse,
                    InputBindingSlot.Primary),
                ResolveParts(
                    item.MapName,
                    item.ActionName,
                    ActiveInputDevice.Gamepad,
                    InputBindingSlot.Primary),
                ResolveDisplay(item.KeyboardPrimary),
                ResolveDisplay(item.GamepadPrimary),
                ResolveDisplay(item.KeyboardSecondary),
                ResolveDisplay(item.GamepadSecondary)));
        }

        private bool TryGetSelected(out MergedBinding result)
        {
            for (int i = 0; i < _merged.Count; i++)
            {
                if (_merged[i].MapName == _selectedMap && _merged[i].ActionName == _selectedAction)
                {
                    result = _merged[i];
                    return true;
                }
            }

            result = default;
            return false;
        }

        #endregion

        #region 캡처와 충돌

        private void RequestCapture(InputBindingDeviceGroup deviceGroup, InputBindingSlot slot)
        {
            if (_capturing || _inputService == null || string.IsNullOrEmpty(_selectedAction))
                return;

            CaptureAsync(new InputBindingTarget(
                _selectedMap, _selectedAction, deviceGroup, slot)).Forget();
        }

        private async UniTaskVoid CaptureAsync(InputBindingTarget target)
        {
            _capturing = true;
            _detail?.SetCaptureState(true, "새 키를 입력하세요.", null);

            try
            {
                InputRebindCaptureResult result = await _inputService.CaptureBindingAsync(
                    target,
                    this.GetCancellationTokenOnDestroy());

                if (result.IsRemovalRequested)
                {
                    _pendingCaptures.Remove(target);
                    _replaceApprovedTargets.Remove(target);
                    _pendingClears.Add(target);
                    _detail?.SetCaptureState(
                        false,
                        "바인딩 제거가 대기 중입니다. 하단 적용을 눌러 확정하세요.",
                        null);
                    RefreshRows();
                    return;
                }

                if (!result.IsCompleted)
                {
                    _detail?.SetCaptureState(false, null, null);
                    return;
                }

                _pendingClears.Remove(target);
                _replaceApprovedTargets.Remove(target);
                _pendingCaptures[target] = result;
                _detail?.SetCaptureState(
                    false,
                    "변경사항이 대기 중입니다. 하단 적용을 눌러 확정하세요.",
                    result.DisplayString);
                RefreshRows();
            }
            finally
            {
                _capturing = false;
            }
        }

        private void OnCaptureStateChanged(InputRebindCaptureState state)
        {
            _detail?.SetCaptureState(
                _capturing,
                state.Message,
                state.FirstControlDisplay);
        }

        private void ShowConflict(InputBindingConflictInfo conflict)
        {
            string subset = conflict.IsChordSubset
                ? " 단일키와 조합키 구성 요소가 겹칩니다."
                : string.Empty;

            if (conflict.IsRequired)
            {
                // 필수 키는 대체 자체가 막혀 있다. 안내만 하고 보류 캡처를 버린다.
                _pendingConflictCapture = default;
                _detail?.SetConflictMessage(
                    $"“{conflict.ExistingDisplayName}”은 필수 키라 대체할 수 없습니다.{subset}",
                    allowReplace: false);
                return;
            }

            _detail?.SetConflictMessage(
                $"이미 “{conflict.ExistingDisplayName}”에서 사용 중입니다.{subset} 대체할까요?",
                allowReplace: true);
        }

        /// <summary>상세 패널의 대체/취소 선택을 처리한다.</summary>
        private void OnConflictDecision(bool replace)
        {
            if (!replace || _inputService == null || !_pendingConflictCapture.IsCompleted)
            {
                CancelConflict();
                return;
            }

            _replaceApprovedTargets.Add(_pendingConflictCapture.Target);
            _pendingConflictCapture = default;
            _detail?.SetCaptureState(
                false,
                "충돌 대체가 승인되었습니다. 하단 적용을 다시 눌러 확정하세요.",
                null);
            RefreshRows();
        }

        private void CancelConflict()
        {
            if (_pendingConflictCapture.IsCompleted)
            {
                _pendingCaptures.Remove(_pendingConflictCapture.Target);
                _replaceApprovedTargets.Remove(_pendingConflictCapture.Target);
            }
            _pendingConflictCapture = default;
            _detail?.SetCaptureState(false, null, null);
            RefreshRows();
        }

        private void ResetSelectedAction()
        {
            if (_inputService == null || string.IsNullOrEmpty(_selectedAction))
                return;

            string actionKey = MakeActionKey(_selectedMap, _selectedAction);
            _pendingActionResets.Add(actionKey);

            RemovePendingForAction(_selectedMap, _selectedAction);
            _detail?.SetCaptureState(
                false,
                "선택 액션 초기화가 대기 중입니다. 하단 적용을 눌러 확정하세요.",
                null);
        }

        #endregion

        private static string MakeActionKey(string mapName, string actionName) =>
            $"{mapName}\n{actionName}";

        private static void SplitActionKey(
            string key,
            out string mapName,
            out string actionName)
        {
            int separator = key?.IndexOf('\n') ?? -1;
            mapName = separator >= 0 ? key.Substring(0, separator) : key ?? string.Empty;
            actionName = separator >= 0 ? key.Substring(separator + 1) : string.Empty;
        }

        private void RemovePendingForAction(string mapName, string actionName)
        {
            var targets = new List<InputBindingTarget>();
            foreach (InputBindingTarget target in _pendingCaptures.Keys)
            {
                if (target.mapName == mapName && target.actionName == actionName)
                    targets.Add(target);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                _pendingCaptures.Remove(targets[i]);
                _replaceApprovedTargets.Remove(targets[i]);
            }

            _pendingClears.RemoveWhere(target =>
                target.mapName == mapName && target.actionName == actionName);
        }

        private static readonly HashSet<string> WarnedMessages = new();

        private static void WarnOnce(string message)
        {
            if (WarnedMessages.Add(message))
                Debug.LogWarning($"[UISettingPageKeyBinding] {message}");
        }

        private void OnDestroy()
        {
            UnbindInputService();
        }
    }
}
