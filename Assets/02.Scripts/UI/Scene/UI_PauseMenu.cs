using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Enum;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

/// <summary>
/// 일시정지 메뉴 UI
/// </summary>
public class UI_PauseMenu : UI_Base
{
    [Header("UI 버튼")]
    [SerializeField] private Button gameExitButton;
    [SerializeField] private Button gotoTitleButton;
    [SerializeField] private Button quitButton;

    [Header("플레이 시간 표시 (선택)")]
    [SerializeField] private TMPro.TextMeshProUGUI playTimeText;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (gameExitButton != null) gameExitButton.onClick.AddListener(OnGameExitClicked);
        if (gotoTitleButton != null) gotoTitleButton.onClick.AddListener(OnGoToTitleClicked);
        if (quitButton != null)      quitButton.onClick.AddListener(OnResumeClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();

        GameTimeManager.Instance.SetPause(true);
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());

        if (playTimeText != null)
            playTimeText.text = GameTimeManager.Instance.FormatPlayTime();
    }

    protected override void OnHide()
    {
        GameTimeManager.Instance.SetPause(false);
        InputManager.Instance.SetInputLayer(InputLayer.None);
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
