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
    public InputAction InteractAction { get; private set; }
    public InputAction PauseAction { get; private set; }
    
    // UI Actions
    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }
    public InputAction PointAction { get; private set; }
    
    // 현재 모드
    private InputMode currentMode = InputMode.Gameplay;
    
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
        
        // Pause 이벤트 구독
        if (PauseAction != null)
        {
            PauseAction.performed += OnPausePerformed;
        }
        
        // 게임플레이 모드로 시작
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
    
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (currentMode == InputMode.Gameplay)
        {
            // UI 모드로 전환하고 일시정지 메뉴 표시
            SwitchToUI();
            
            // UIManager가 있으면 일시정지 메뉴 표시
            if (UIManager.Instance != null)
            {
                GameObject pauseMenuPrefab = Resources.Load<GameObject>("UI/PauseMenu");
                if (pauseMenuPrefab != null)
                {
                    UIManager.Instance.ShowUI(pauseMenuPrefab, CanvasLayer.Popup, "PauseMenu");
                }
                else
                {
                    Debug.LogWarning("[InputManager] PauseMenu 프리팹을 찾을 수 없습니다.");
                }
            }
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
    Gameplay,   // 게임플레이
    UI          // UI
}
