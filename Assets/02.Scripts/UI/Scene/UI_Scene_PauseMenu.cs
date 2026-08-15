using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 일시정지 메뉴 UI.
    /// 표시/닫기 트윈(Dim 페이드 + Panel 스케일 팝인/아웃)은 UI_PopupBase가 담당한다.
    /// 인스펙터에서 UI_PopupBase의 _dim(CanvasGroup)/_panel(RectTransform)을 연결해야 트윈이 재생된다.
    /// </summary>
    public class UI_Scene_PauseMenu : UI_PopupBase, IUIFocusPresentation
    {
        private static readonly Color DefaultButtonColor = new Color(0.10f, 0.13f, 0.17f, 1f);
        private static readonly Color DefaultSelectedButtonColor = new Color(0.10f, 0.30f, 0.36f, 1f);
        private static readonly Color DefaultDangerButtonColor = new Color(0.30f, 0.10f, 0.12f, 1f);

        [Header("UI 버튼")]
        [SerializeField] private Button resumeButton;   // 재개
        [SerializeField] private Button saveButton;     // 저장
        [SerializeField] private Button gotoTitleButton;// 타이틀로 이동
        [SerializeField] private Button exitButton;     // 게임 종료

        [Header("표시 (선택)")]
        [SerializeField] private TMPro.TextMeshProUGUI playTimeText;   // "플레이 시간 HH:MM:SS"
        [SerializeField] private TMPro.TextMeshProUGUI pauseStatusText; // "게임이 일시정지되었습니다"

        private Image _resumeButtonImage;
        private Image _saveButtonImage;
        private Image _gotoTitleButtonImage;
        private Image _exitButtonImage;
        private RectTransform _selectionHighlight;
        private Button _focusedButton;
        private Color _normalButtonColor = DefaultButtonColor;
        private Color _selectedButtonColor = DefaultSelectedButtonColor;
        private Color _dangerButtonColor = DefaultDangerButtonColor;

        public bool SuppressGlobalFocusIndicator => true;
        public RectTransform GlobalFocusIndicatorTarget => null;

        protected override void Awake()
        {
            base.Awake();

            if (resumeButton != null)    resumeButton.onClick.AddListener(OnResumeClicked);
            if (saveButton != null)      saveButton.onClick.AddListener(OnSaveClicked);
            if (gotoTitleButton != null) gotoTitleButton.onClick.AddListener(OnGoToTitleClicked);
            if (exitButton != null)      exitButton.onClick.AddListener(OnGameExitClicked);

            EnsureSelectOnPointerEnter(resumeButton);
            EnsureSelectOnPointerEnter(saveButton);
            EnsureSelectOnPointerEnter(gotoTitleButton);
            EnsureSelectOnPointerEnter(exitButton);
            CacheSelectionVisuals();
        }

        private static void EnsureSelectOnPointerEnter(Button button)
        {
            if (button == null)
                return;

            if (button.GetComponent<UISelectOnPointerEnter>() == null)
                button.gameObject.AddComponent<UISelectOnPointerEnter>();
        }

        private void OnSaveClicked()
        {
            // 포즈 메뉴를 유지한 채 슬롯 선택 UI만 위에 띄워 일시정지 상태를 보존한다.
            var go = UISvc.UI.ShowUI(UI_Scene_SaveSlotMenu.UIKey);
            go?.GetComponent<UI_Scene_SaveSlotMenu>()?.SetMode(UI_Scene_SaveSlotMenu.SaveSlotMode.Save);
        }

        protected override void Update()
        {
            base.Update();
            SyncFocusedButtonFromEventSystem();
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();

            Svc.GameTime.SetPause(true);

            if (playTimeText != null)
                playTimeText.text = $"플레이 시간 {Svc.GameTime.FormatPlayTime()}";

            if (pauseStatusText != null)
                pauseStatusText.text = "게임이 일시정지되었습니다";

            SetInitialButtonFocus();
        }

        /// <summary> 일시정지 메뉴를 열 때 키보드/게임패드 네비게이션 시작 버튼을 지정한다. </summary>
        private void SetInitialButtonFocus()
        {
            if (EventSystem.current == null)
                return;

            var initialButton = resumeButton != null && resumeButton.interactable
                ? resumeButton
                : GetFirstInteractableButton();

            if (initialButton != null)
            {
                EventSystem.current.SetSelectedGameObject(initialButton.gameObject);
                ApplyFocusedButton(initialButton);
            }
        }

        private Button GetFirstInteractableButton()
        {
            if (saveButton != null && saveButton.interactable) return saveButton;
            if (gotoTitleButton != null && gotoTitleButton.interactable) return gotoTitleButton;
            if (exitButton != null && exitButton.interactable) return exitButton;
            return null;
        }

        private void CacheSelectionVisuals()
        {
            _resumeButtonImage = GetButtonImage(resumeButton);
            _saveButtonImage = GetButtonImage(saveButton);
            _gotoTitleButtonImage = GetButtonImage(gotoTitleButton);
            _exitButtonImage = GetButtonImage(exitButton);

            if (_saveButtonImage != null)
                _normalButtonColor = _saveButtonImage.color;
            if (_resumeButtonImage != null)
                _selectedButtonColor = _resumeButtonImage.color;
            if (_exitButtonImage != null)
                _dangerButtonColor = _exitButtonImage.color;

            _selectionHighlight = resumeButton != null
                ? resumeButton.transform.Find("Highlight") as RectTransform
                : null;
        }

        private static Image GetButtonImage(Button button)
        {
            return button != null ? button.targetGraphic as Image : null;
        }

        private void SyncFocusedButtonFromEventSystem()
        {
            if (!IsVisible || EventSystem.current == null)
                return;

            var selected = EventSystem.current.currentSelectedGameObject;
            var selectedButton = selected != null ? selected.GetComponent<Button>() : null;

            if (selectedButton == resumeButton
                || selectedButton == saveButton
                || selectedButton == gotoTitleButton
                || selectedButton == exitButton)
            {
                ApplyFocusedButton(selectedButton);
            }
        }

        private void ApplyFocusedButton(Button button)
        {
            if (button == null)
                return;

            bool changed = _focusedButton != button;
            if (changed)
                _focusedButton = button;

            SetButtonColor(_resumeButtonImage, button == resumeButton ? _selectedButtonColor : _normalButtonColor);
            SetButtonColor(_saveButtonImage, button == saveButton ? _selectedButtonColor : _normalButtonColor);
            SetButtonColor(_gotoTitleButtonImage, button == gotoTitleButton ? _selectedButtonColor : _normalButtonColor);
            SetButtonColor(_exitButtonImage, button == exitButton ? _selectedButtonColor : _dangerButtonColor);

            if (changed)
            {
                MoveSelectionChild(_selectionHighlight, button.transform);
            }
        }

        private static void SetButtonColor(Image image, Color color)
        {
            if (image != null)
                image.color = color;
        }

        private static void MoveSelectionChild(RectTransform child, Transform parent)
        {
            if (child == null || parent == null)
                return;

            child.SetParent(parent, false);
            child.gameObject.SetActive(true);

            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = Vector2.zero;
            child.offsetMax = Vector2.zero;
            child.SetAsFirstSibling();
        }

        protected override void OnHide()
        {
            Svc.GameTime?.SetPause(false);
            base.OnHide();
        }

        private void OnResumeClicked()
        {
            UISvc.UI.HideUI(UIKeyType.PauseMenu);
        }

        private void OnGoToTitleClicked()
        {
            // 타이틀로 나가기 전 timeScale 복구는 GameTimeManager.Dispose에서 처리됨
            UISvc.Scene.LoadScene(SceneName.Title);
        }

        private void OnGameExitClicked()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}
