using UnityEngine;
using UnityEngine.UI;
using UPlayGround.UREnum;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

/// <summary>
/// 일시정지 메뉴 UI
/// </summary>
public class UI_PauseMenu : UI_Base
{
    [Header("UI 버튼")]
    [SerializeField] private Button resumeButton;   // 재개
    [SerializeField] private Button saveButton;     // 저장
    [SerializeField] private Button gotoTitleButton;// 타이틀로 이동
    [SerializeField] private Button exitButton;     // 게임 종료

    [Header("표시 (선택)")]
    [SerializeField] private TMPro.TextMeshProUGUI playTimeText;   // "플레이 시간 HH:MM:SS"
    [SerializeField] private TMPro.TextMeshProUGUI pauseStatusText; // "게임이 일시정지되었습니다"

    protected override void Awake()
    {
        base.Awake();

        if (resumeButton != null)    resumeButton.onClick.AddListener(OnResumeClicked);
        if (saveButton != null)      saveButton.onClick.AddListener(OnSaveClicked);
        if (gotoTitleButton != null) gotoTitleButton.onClick.AddListener(OnGoToTitleClicked);
        if (exitButton != null)      exitButton.onClick.AddListener(OnGameExitClicked);
    }

    private void OnSaveClicked()
    {
        // 슬롯 선택 UI를 저장 모드로 띄운다. 슬롯 선택 시 현재 상태를 저장한다.
        var go = UIManager.Instance.ShowUI(UI_SaveSlotMenu.UIKey);
        go?.GetComponent<UI_SaveSlotMenu>()?.SetMode(UI_SaveSlotMenu.SaveSlotMode.Save);

        Hide();
    }

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        base.OnShow();

        GameTimeManager.Instance.SetPause(true);

        if (playTimeText != null)
            playTimeText.text = $"플레이 시간 {GameTimeManager.Instance.FormatPlayTime()}";

        if (pauseStatusText != null)
            pauseStatusText.text = "게임이 일시정지되었습니다";
    }

    protected override void OnHide()
    {
        GameTimeManager.Instance.SetPause(false);
        base.OnHide();
    }

    private void OnResumeClicked()
    {
        UIManager.Instance.HideUI(UIKeyType.PauseMenu);
    }
    
    private void OnGoToTitleClicked()
    {
        // 타이틀로 나가기 전 timeScale 복구는 GameTimeManager.Dispose에서 처리됨
        SceneManager.Instance.LoadScene(SceneName.Title);
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
