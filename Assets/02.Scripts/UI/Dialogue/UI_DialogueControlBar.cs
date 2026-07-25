using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround.Dialogue;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 대화 좌상단 재생 컨트롤 바 — 정지 / 자동 / 스킵 / 이력.
    /// 상태는 IUIDialogueService(매니저 소유 재생 컨트롤러)에 있고 이 UI는 구독·명령만 합니다.
    /// </summary>
    public class UI_DialogueControlBar : UI_Base
    {
        [Header("버튼")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button backlogButton;

        [Header("상태 라벨")]
        [SerializeField] private TextMeshProUGUI pauseLabel;
        [SerializeField] private TextMeshProUGUI autoLabel;

        [Header("토글 색상")]
        [SerializeField] private Color inactiveColor = new(0.82f, 0.80f, 0.74f, 1f);
        [SerializeField] private Color activeColor = new(1f, 0.72f, 0.28f, 1f);

        // 컨트롤 바는 대화 위에 얹히는 보조 UI이므로 하위 입력을 추가로 막지 않는다.
        // (입력 독점은 이미 UI_Dialogue/UI_MonologueDialogue가 담당한다.)
        protected override bool BlocksLowerInput => false;

        // 포커스 스코프를 만들지 않기 위해 false로 고정한다.
        // 기본 구현은 Scene 레이어 이상이면 true라, 그대로 두면 UI_Base가 FocusScope를 활성화해
        // 컨트롤 바 버튼 하나가 EventSystem 선택 상태가 된다. 그러면 대화 진행 키(스페이스 등)가
        // 그 버튼의 Submit으로 소비돼 '스페이스를 눌렀는데 이력 창이 열리는' 오작동이 난다.
        // 커서는 대화 본체(UI_Dialogue)가 이미 표시 상태로 push하므로 마우스 클릭에는 영향이 없다.
        protected override bool RequiresCursorVisible => false;

        protected override void Awake()
        {
            base.Awake();

            // 클릭 후에는 선택 상태를 반드시 비운다.
            // 마우스 클릭은 Selectable을 EventSystem 선택 상태로 만들기 때문에, 그대로 두면
            // 이후 Submit 계열 입력이 그 버튼을 다시 눌러 대화 진행 대신 엉뚱한 동작이 일어난다.
            pauseButton?.onClick.AddListener(() => RunCommand(TogglePause));
            autoButton?.onClick.AddListener(() => RunCommand(ToggleAuto));
            skipButton?.onClick.AddListener(() => RunCommand(RequestSkip));
            backlogButton?.onClick.AddListener(() => RunCommand(ToggleBacklog));
        }

        private void RunCommand(System.Action command)
        {
            ClearOwnSelection();
            command();
        }

        // 이 컨트롤 바 안쪽이 선택되어 있을 때만 해제한다(다른 UI의 포커스를 건드리지 않기 위해).
        private void ClearOwnSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;

            if (selected != null && selected.transform.IsChildOf(transform))
                eventSystem.SetSelectedGameObject(null);
        }

        protected override void OnShow()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnPauseChanged += HandlePauseChanged;
                dialogue.OnAutoChanged  += HandleAutoChanged;
            }

            Svc.Input?.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueSkip,
                null, OnInputSkip, null, null, null, InputLayer.Level_1);
            Svc.Input?.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueToggleAuto,
                null, OnInputToggleAuto, null, null, null, InputLayer.Level_1);
            Svc.Input?.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueBacklog,
                null, OnInputBacklog, null, null, null, InputLayer.Level_1);

            RefreshAll();
        }

        protected override void OnHide()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue != null)
            {
                dialogue.OnPauseChanged -= HandlePauseChanged;
                dialogue.OnAutoChanged  -= HandleAutoChanged;
            }

            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueSkip,
                null, OnInputSkip, null);
            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueToggleAuto,
                null, OnInputToggleAuto, null);
            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueBacklog,
                null, OnInputBacklog, null);
        }

        // ── 명령 ────────────────────────────────────────────────────────

        private void TogglePause()
        {
            var dialogue = UISvc.Dialogue;
            dialogue?.SetPaused(!dialogue.IsPaused);
        }

        private void ToggleAuto()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue == null)
                return;

            bool next = !dialogue.IsAuto;
            dialogue.SetAuto(next);

            // 자동을 켜면 정지와 모순되므로 정지를 해제한다.
            if (next && dialogue.IsPaused)
                dialogue.SetPaused(false);
        }

        private void RequestSkip()
        {
            var dialogue = UISvc.Dialogue;
            if (dialogue == null)
                return;

            // 스킵은 정지 상태와 모순되므로 먼저 정지를 푼다.
            if (dialogue.IsPaused)
                dialogue.SetPaused(false);

            dialogue.RequestSkip(ResolveActiveChannel());
        }

        private void ToggleBacklog()
        {
            var uiManager = UIManager.Instance;
            if (uiManager == null)
                return;

            if (uiManager.IsUIActive(DialogueUIKeys.DialogueBacklog))
                uiManager.HideUI(DialogueUIKeys.DialogueBacklog);
            else
                uiManager.ShowUI(DialogueUIKeys.DialogueBacklog);
        }

        // 컨트롤 바는 Main/Monologue 두 채널 위에 공유되므로 열려 있는 대화 UI로 채널을 판정한다.
        private static DialogueChannel ResolveActiveChannel()
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null && uiManager.IsUIActive(DialogueUIKeys.MonologueDialogue))
                return DialogueChannel.Monologue;

            return DialogueChannel.Main;
        }

        // ── 입력 ────────────────────────────────────────────────────────

        private void OnInputSkip(InputAction.CallbackContext ctx) => RequestSkip();
        private void OnInputToggleAuto(InputAction.CallbackContext ctx) => ToggleAuto();
        private void OnInputBacklog(InputAction.CallbackContext ctx) => ToggleBacklog();

        // ── 표시 갱신 ────────────────────────────────────────────────────

        private void HandlePauseChanged(bool paused) => ApplyPauseVisual(paused);
        private void HandleAutoChanged(bool auto) => ApplyAutoVisual(auto);

        private void RefreshAll()
        {
            var dialogue = UISvc.Dialogue;
            ApplyPauseVisual(dialogue != null && dialogue.IsPaused);
            ApplyAutoVisual(dialogue != null && dialogue.IsAuto);
        }

        private void ApplyPauseVisual(bool paused)
        {
            if (pauseLabel == null)
                return;

            // 버튼은 '누르면 일어날 동작'을 표시한다. 아이콘 글리프는 한글 폰트에 없어 □로 깨지므로 텍스트를 쓴다.
            pauseLabel.text = paused ? "재개" : "정지";
            pauseLabel.color = paused ? activeColor : inactiveColor;
        }

        private void ApplyAutoVisual(bool auto)
        {
            if (autoLabel == null)
                return;

            autoLabel.text = "AUTO";
            autoLabel.color = auto ? activeColor : inactiveColor;
        }
    }
}
