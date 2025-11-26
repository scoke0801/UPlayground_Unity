using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 시스템 관리 매니저
/// </summary>
public class InputManager : BaseManager<InputManager>, IManager
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    
    // Action Maps
    private InputActionMap gameplayActionMap;
    private InputActionMap uiActionMap;
    
    // Gameplay Actions
    public InputAction MoveAction { get; private set; }
    public InputAction LookAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction RunAction { get; private set; }
    public InputAction RollAction { get; private set; }
    public InputAction AttackAction { get; private set; }
    public InputAction HeavyAttackAction { get; private set; }
    public InputAction InteractAction { get; private set; }
    public InputAction PauseAction { get; private set; }
    
    // UI Actions
    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }
    public InputAction PointAction { get; private set; }
    
    // 현재 모드
    private InputMode currentMode = InputMode.None;
    
    // 이벤트
    public System.Action<InputMode> OnInputModeChanged;
    
    #region IManager 구현
    
    public void Init()
    {
        Debug.Log("[InputManager] 초기화 시작");
        
        // Input Actions Asset 로드
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Input/PlayerInputActions");
            
            if (inputActions == null)
            {
                Debug.LogError("[InputManager] PlayerInputActions를 찾을 수 없습니다! Resources 폴더에 PlayerInputActions.inputactions 파일이 있는지 확인하세요.");
                return;
            }
        }
        
        // Action Maps 가져오기
        gameplayActionMap = inputActions.FindActionMap("Gameplay");
        uiActionMap = inputActions.FindActionMap("UI");
        
        if (gameplayActionMap == null)
        {
            Debug.LogError("[InputManager] 'Gameplay' ActionMap을 찾을 수 없습니다!");
            return;
        }
        
        if (uiActionMap == null)
        {
            Debug.LogError("[InputManager] 'UI' ActionMap을 찾을 수 없습니다!");
            return;
        }
        
        // Gameplay Actions 초기화
        MoveAction = gameplayActionMap.FindAction("Move");
        LookAction = gameplayActionMap.FindAction("Look");
        JumpAction = gameplayActionMap.FindAction("Jump");
        RunAction = gameplayActionMap.FindAction("Run");
        RollAction = gameplayActionMap.FindAction("Roll");
        AttackAction = gameplayActionMap.FindAction("Attack");
        HeavyAttackAction = gameplayActionMap.FindAction("HeavyAttack");
        InteractAction = gameplayActionMap.FindAction("Interact");
        PauseAction = gameplayActionMap.FindAction("Pause");
        
        // UI Actions 초기화
        NavigateAction = uiActionMap.FindAction("Navigate");
        SubmitAction = uiActionMap.FindAction("Submit");
        CancelAction = uiActionMap.FindAction("Cancel");
        PointAction = uiActionMap.FindAction("Point");
        
        // 액션 유효성 검사
        if (MoveAction == null) Debug.LogWarning("[InputManager] Move 액션을 찾을 수 없습니다!");
        if (LookAction == null) Debug.LogWarning("[InputManager] Look 액션을 찾을 수 없습니다!");
        if (JumpAction == null) Debug.LogWarning("[InputManager] Jump 액션을 찾을 수 없습니다!");
        if (PauseAction == null) Debug.LogWarning("[InputManager] Pause 액션을 찾을 수 없습니다!");

        // Pause 이벤트 구독
        if (PauseAction != null)
        {
            PauseAction.performed += OnPausePerformed;
        }

        // UI Cancel 이벤트 구독 (ESC로 메뉴 닫기)
        if (CancelAction != null)
        {
            CancelAction.performed += OnCancelPerformed;
        }
        
        // 게임플레이 모드로 시작
        currentMode = InputMode.None;
        SwitchToGameplay();
        
        Debug.Log("[InputManager] 초기화 완료");
    }
    
    public void Dispose()
    {
        Debug.Log("[InputManager] 정리 시작");
        
        // 이벤트 구독 해제
        if (PauseAction != null)
        {
            PauseAction.performed -= OnPausePerformed;
        }

        if (CancelAction != null)
        {
            CancelAction.performed -= OnCancelPerformed;
        }
        
        // 모든 액션 비활성화
        gameplayActionMap?.Disable();
        uiActionMap?.Disable();
        
        Debug.Log("[InputManager] 정리 완료");
    }
    
    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    
    #endregion
    
    #region 입력 모드 전환
    
    /// <summary>
    /// 게임플레이 모드로 전환
    /// </summary>
    public void SwitchToGameplay()
    {
        if (currentMode == InputMode.Gameplay) return;
        
        uiActionMap?.Disable();
        gameplayActionMap?.Enable();
        
        currentMode = InputMode.Gameplay;
        OnInputModeChanged?.Invoke(currentMode);
        
        // 마우스 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[InputManager] 게임플레이 모드로 전환");
    }
    
    /// <summary>
    /// UI 모드로 전환
    /// </summary>
    public void SwitchToUI()
    {
        if (currentMode == InputMode.UI) return;
        
        gameplayActionMap?.Disable();
        uiActionMap?.Enable();
        
        currentMode = InputMode.UI;
        OnInputModeChanged?.Invoke(currentMode);
        
        // 마우스 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[InputManager] UI 모드로 전환");
    }
    
    /// <summary>
    /// 현재 입력 모드
    /// </summary>
    public InputMode CurrentMode => currentMode;
    
    #endregion
    
    #region 입력 이벤트 처리
    
    /// <summary>
    /// ESC (Pause) 키 입력 처리 - 토글 방식
    /// </summary>
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[InputManager] UIManager가 없어서 일시정지 메뉴를 표시할 수 없습니다.");
            return;
        }

        // 토글 방식: 이미 메뉴가 열려있으면 닫기
        if (UIManager.Instance.IsUIActive("PauseMenu"))
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    /// <summary>
    /// UI 모드에서 Cancel (ESC) 키 입력 처리
    /// </summary>
    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        // UI 모드일 때만 처리
        if (currentMode == InputMode.UI && UIManager.Instance != null)
        {
            if (UIManager.Instance.IsUIActive("PauseMenu"))
            {
                ClosePauseMenu();
            }
        }
    }

    /// <summary>
    /// 일시정지 메뉴 열기
    /// </summary>
    private void OpenPauseMenu()
    {
        // UI 모드로 전환
        SwitchToUI();

        // 방법 1: UIPrefabDatabase 사용 (권장)
        GameObject menuObj = UIManager.Instance.ShowUI("PauseMenu", CanvasLayer.Popup);
        
        // 방법 2: Resources 직접 로드 (Database에 없을 경우 대체)
        if (menuObj == null)
        {
            GameObject pauseMenuPrefab = Resources.Load<GameObject>("UI/PauseMenu");
            if (pauseMenuPrefab != null)
            {
                UIManager.Instance.ShowUI(pauseMenuPrefab, CanvasLayer.Popup, "PauseMenu");
                Debug.Log("[InputManager] Resources에서 PauseMenu를 로드했습니다.");
            }
            else
            {
                Debug.LogError("[InputManager] PauseMenu 프리팹을 찾을 수 없습니다!");
            }
        }

        // 게임 일시정지 (선택사항 - 필요에 따라 주석 해제)
        // Time.timeScale = 0f;
        
        Debug.Log("[InputManager] 일시정지 메뉴 열림");
    }

    /// <summary>
    /// 일시정지 메뉴 닫기
    /// </summary>
    private void ClosePauseMenu()
    {
        UIManager.Instance.HideUI("PauseMenu");
        
        // 게임플레이 모드로 복귀
        SwitchToGameplay();

        // 게임 재개
        // Time.timeScale = 1f;
        
        Debug.Log("[InputManager] 일시정지 메뉴 닫힘");
    }

    /// <summary>
    /// 외부에서 일시정지 메뉴를 닫을 수 있도록 public 메서드 제공
    /// (UI의 Resume 버튼 등에서 호출)
    /// </summary>
    public void ResumeGame()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsUIActive("PauseMenu"))
        {
            ClosePauseMenu();
        }
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 모든 입력 비활성화
    /// </summary>
    public void DisableAllInput()
    {
        gameplayActionMap?.Disable();
        uiActionMap?.Disable();
        Debug.Log("[InputManager] 모든 입력 비활성화");
    }
    
    /// <summary>
    /// 모든 입력 활성화
    /// </summary>
    public void EnableAllInput()
    {
        if (currentMode == InputMode.Gameplay)
        {
            gameplayActionMap?.Enable();
        }
        else
        {
            uiActionMap?.Enable();
        }
        Debug.Log("[InputManager] 입력 활성화");
    }
    
    /// <summary>
    /// 특정 액션 활성화/비활성화
    /// </summary>
    public void SetActionEnabled(string actionName, bool enabled)
    {
        InputAction action = gameplayActionMap?.FindAction(actionName) ?? uiActionMap?.FindAction(actionName);
        
        if (action != null)
        {
            if (enabled)
                action.Enable();
            else
                action.Disable();
        }
    }
    
    #endregion
}

/// <summary>
/// 입력 모드
/// </summary>
public enum InputMode
{
    None,
    Gameplay,   // 게임플레이
    UI          // UI
}