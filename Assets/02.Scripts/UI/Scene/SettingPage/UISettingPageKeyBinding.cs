using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UPlayGround.Data.Config;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    public class UISettingPageKeyBinding : UISettingPageBase
    {
        [Header("Device")]
        [SerializeField] private Button _keyboardMouseButton;
        [SerializeField] private Button _gamepadButton;
        [SerializeField] private TMP_Dropdown _categoryDropdown;

        [Header("Binding List")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private UIKeyBindingRow _rowTemplate;

        [Header("Reset")]
        [SerializeField] private Button _resetDeviceButton;

        [Header("Capture Overlay")]
        [SerializeField] private GameObject _captureOverlay;
        [SerializeField] private TextMeshProUGUI _captureTitle;
        [SerializeField] private TextMeshProUGUI _captureMessage;

        [Header("Conflict Overlay")]
        [SerializeField] private GameObject _conflictOverlay;
        [SerializeField] private TextMeshProUGUI _conflictMessage;
        [SerializeField] private Button _replaceButton;
        [SerializeField] private Button _conflictCancelButton;

        private readonly List<UIKeyBindingRow> _rows = new();
        private IInputService _inputService;
        private InputBindingDeviceGroup _deviceGroup = InputBindingDeviceGroup.KeyboardMouse;
        private InputBindingCategory? _category;
        private InputRebindCaptureResult _pendingConflictCapture;
        private GameObject _focusBeforeOverlay;
        private bool _controlsBound;

        protected override void BindControls(SettingsData settingsData)
        {
            if (_controlsBound)
                return;
            _controlsBound = true;

            _keyboardMouseButton?.onClick.AddListener(ShowKeyboardMouse);
            _gamepadButton?.onClick.AddListener(ShowGamepad);
            _categoryDropdown?.onValueChanged.AddListener(OnCategoryChanged);
            _resetDeviceButton?.onClick.AddListener(ResetCurrentDevice);
            _replaceButton?.onClick.AddListener(ReplaceConflict);
            _conflictCancelButton?.onClick.AddListener(CancelConflict);

            if (_categoryDropdown != null)
            {
                _categoryDropdown.ClearOptions();
                _categoryDropdown.AddOptions(new List<string>
                {
                    "전체", "이동", "전투", "상호작용", "카메라", "UI",
                });
                _categoryDropdown.SetValueWithoutNotify(0);
            }

            if (_rowTemplate != null)
                _rowTemplate.gameObject.SetActive(false);
            if (_captureOverlay != null)
                _captureOverlay.SetActive(false);
            if (_conflictOverlay != null)
                _conflictOverlay.SetActive(false);

            BindInputService();
        }

        public override void SyncUIFromData(SettingsData settingsData)
        {
            BindInputService();
            RefreshRows();
        }

        public bool TryHandleBack()
        {
            if (_conflictOverlay != null && _conflictOverlay.activeSelf)
            {
                CancelConflict();
                return true;
            }

            // 실제 캡처 중 Cancel은 InputManager가 raw Escape/B 입력을 소비한다.
            return _captureOverlay != null && _captureOverlay.activeSelf;
        }

        private void BindInputService()
        {
            if (_inputService == Svc.Input)
                return;

            UnbindInputService();
            _inputService = Svc.Input;
            if (_inputService == null)
                return;

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

        private void ShowKeyboardMouse()
        {
            _deviceGroup = InputBindingDeviceGroup.KeyboardMouse;
            RefreshRows();
        }

        private void ShowGamepad()
        {
            _deviceGroup = InputBindingDeviceGroup.Gamepad;
            RefreshRows();
        }

        private void OnCategoryChanged(int index)
        {
            _category = index <= 0
                ? null
                : (InputBindingCategory)(index - 1);
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (_inputService == null || _content == null || _rowTemplate == null)
                return;

            IReadOnlyList<InputBindingDescriptor> descriptors =
                _inputService.GetBindingDescriptors(_deviceGroup);
            var primaryItems = descriptors
                .Where(item => item.Target.slot == InputBindingSlot.Primary)
                .Where(item => !_category.HasValue || item.Category == _category.Value)
                .ToList();

            EnsureRows(primaryItems.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < primaryItems.Count;
                _rows[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                InputBindingDescriptor primary = primaryItems[i];
                InputBindingDescriptor secondary = descriptors.First(item =>
                    item.Target.mapName == primary.Target.mapName
                    && item.Target.actionName == primary.Target.actionName
                    && item.Target.slot == InputBindingSlot.Secondary);
                _rows[i].Configure(primary, secondary, BeginCapture, ResetAction);
            }
        }

        private void EnsureRows(int count)
        {
            while (_rows.Count < count)
            {
                UIKeyBindingRow row = Instantiate(_rowTemplate, _content);
                row.gameObject.name = $"BindingRow_{_rows.Count:00}";
                _rows.Add(row);
            }
        }

        private void BeginCapture(InputBindingTarget target)
        {
            CaptureAsync(target).Forget();
        }

        private async UniTaskVoid CaptureAsync(InputBindingTarget target)
        {
            if (_inputService == null)
                return;

            _focusBeforeOverlay = EventSystem.current?.currentSelectedGameObject;
            if (_captureOverlay != null)
                _captureOverlay.SetActive(true);
            if (_captureTitle != null)
                _captureTitle.text = $"{target.actionName} 입력 변경";

            InputRebindCaptureResult result = await _inputService.CaptureBindingAsync(
                target,
                this.GetCancellationTokenOnDestroy());

            if (_captureOverlay != null)
                _captureOverlay.SetActive(false);

            if (!result.IsCompleted)
            {
                RestoreOverlayFocus();
                return;
            }

            if (_inputService.TryApplyBinding(result, false, out InputBindingConflictInfo conflict))
            {
                RefreshRows();
                RestoreOverlayFocus();
                return;
            }

            if (!conflict.HasConflict)
            {
                RestoreOverlayFocus();
                return;
            }

            _pendingConflictCapture = result;
            ShowConflict(conflict);
        }

        private void OnCaptureStateChanged(InputRebindCaptureState state)
        {
            if (_captureMessage == null)
                return;

            string first = string.IsNullOrWhiteSpace(state.FirstControlDisplay)
                ? string.Empty
                : $"\n[{state.FirstControlDisplay}] + …";
            _captureMessage.text = $"{state.Message}{first}";
        }

        private void ShowConflict(InputBindingConflictInfo conflict)
        {
            if (_conflictOverlay != null)
                _conflictOverlay.SetActive(true);
            if (_conflictMessage != null)
            {
                string subset = conflict.IsChordSubset
                    ? "\n단일키와 조합키 구성 요소가 겹칩니다."
                    : string.Empty;
                _conflictMessage.text =
                    $"이미 “{conflict.ExistingDisplayName}”에서 사용 중인 입력입니다.{subset}";
            }
            if (_replaceButton != null)
                _replaceButton.interactable = !conflict.IsRequired;

            if (EventSystem.current != null && _replaceButton != null && _replaceButton.interactable)
                EventSystem.current.SetSelectedGameObject(_replaceButton.gameObject);
            else if (EventSystem.current != null && _conflictCancelButton != null)
                EventSystem.current.SetSelectedGameObject(_conflictCancelButton.gameObject);
        }

        private void ReplaceConflict()
        {
            if (_inputService == null)
                return;

            _inputService.TryApplyBinding(
                _pendingConflictCapture,
                true,
                out _);
            CloseConflict();
            RefreshRows();
        }

        private void CancelConflict()
        {
            CloseConflict();
            RestoreOverlayFocus();
        }

        private void CloseConflict()
        {
            if (_conflictOverlay != null)
                _conflictOverlay.SetActive(false);
            _pendingConflictCapture = default;
        }

        private void ResetAction(InputBindingTarget target)
        {
            _inputService?.ResetBinding(target);
        }

        private void ResetCurrentDevice()
        {
            _inputService?.ResetBindings(_deviceGroup);
        }

        private void RestoreOverlayFocus()
        {
            if (EventSystem.current == null)
                return;

            EventSystem.current.SetSelectedGameObject(
                _focusBeforeOverlay != null && _focusBeforeOverlay.activeInHierarchy
                    ? _focusBeforeOverlay
                    : _keyboardMouseButton?.gameObject);
            _focusBeforeOverlay = null;
        }

        private void OnDestroy()
        {
            UnbindInputService();

            _keyboardMouseButton?.onClick.RemoveListener(ShowKeyboardMouse);
            _gamepadButton?.onClick.RemoveListener(ShowGamepad);
            _categoryDropdown?.onValueChanged.RemoveListener(OnCategoryChanged);
            _resetDeviceButton?.onClick.RemoveListener(ResetCurrentDevice);
            _replaceButton?.onClick.RemoveListener(ReplaceConflict);
            _conflictCancelButton?.onClick.RemoveListener(CancelConflict);
        }
    }
}
