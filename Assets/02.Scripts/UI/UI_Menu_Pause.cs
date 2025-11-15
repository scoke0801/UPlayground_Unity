using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 일시정지 메뉴 UI
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI 버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    private InputManager inputManager;
    
    private void Awake()
    {
        inputManager = InputManager.Instance;
        
        // 버튼 이벤트 연결
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }
    
    private void OnEnable()
    {
        if (inputManager != null)
        {
            // UI 모드로 전환
            inputManager.SwitchToUI();
            
            // ESC 키로 메뉴 닫기
            if (inputManager.CancelAction != null)
            {
                inputManager.CancelAction.performed += OnCancelPerformed;
            }
        }
        
        // 게임 시간 정지
        Time.timeScale = 0f;
    }
    
    private void OnDisable()
    {
        if (inputManager != null && inputManager.CancelAction != null)
        {
            inputManager.CancelAction.performed -= OnCancelPerformed;
        }
        
        // 게임 시간 재개
        Time.timeScale = 1f;
    }
    
    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        OnResumeClicked();
    }
    
    /// <summary>
    /// 게임 재개 버튼
    /// </summary>
    private void OnResumeClicked()
    {
        Debug.Log("[PauseMenu] 게임 재개");
        
        // 게임플레이 모드로 전환
        if (inputManager != null)
        {
            inputManager.SwitchToGameplay();
        }
        
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
    /// 메인 메뉴로 버튼
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("[PauseMenu] 메인 메뉴로 이동");
        
        // TODO: 메인 메뉴 씬 로드
        // SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// 게임 종료 버튼
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[PauseMenu] 게임 종료");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
