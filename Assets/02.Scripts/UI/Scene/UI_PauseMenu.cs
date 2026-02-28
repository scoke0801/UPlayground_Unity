using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Enum;
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
    
    protected override void Awake()
    {
        base.Awake();
        
        // 버튼 이벤트 연결
        if (gameExitButton != null)
            gameExitButton.onClick.AddListener(OnGameExitClicked);
        
        if (gotoTitleButton != null)
            gotoTitleButton.onClick.AddListener(OnGoToTitleClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }
    
    /// <summary>
    /// 게임 재개 버튼
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[PauseMenu] 게임 재개");
        
        // UI 닫기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideUI("PauseMenu");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 설정 버튼
    /// </summary>
    private void OnSettingsClicked()
    {
        Debug.Log("[PauseMenu] 설정 메뉴 열기");
        
        // TODO: 설정 메뉴 UI 표시
        // UIManager.Instance.ShowUI(settingsMenuPrefab, CanvasLayer.Popup, "SettingsMenu");
    }
    
    /// <summary>
    /// 타이틀 이동 버튼
    /// </summary>
    private void OnGoToTitleClicked()
    {
        Debug.Log("[PauseMenu] 메인 메뉴로 이동");
        
        SceneManager.Instance.LoadScene(SceneName.Title);
    }
    
    /// <summary>
    /// 게임 종료 버튼
    /// </summary>
    private void OnGameExitClicked()
    {
        Debug.Log("[PauseMenu] 게임 종료");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
