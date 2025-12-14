using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 시스템 관리 매니저
/// </summary>
public partial class InputManager : BaseManager<InputManager>, IManager
{
    private InputMode _currentMode = InputMode.None;
    public System.Action<InputMode> OnInputModeChanged;

    private int _cursorVisibleStack = 0;
    #region IManager 구현
    
    public void Init()
    {
        Debug.Log("[InputManager] 초기화 시작");
        
        // SwipeDetector 초기화
        InitializeSwipeDetector();

        // Actions 초기화
        InitInputAction();
        
        // 이벤트 구독
        SubscribeToEvents();
        
        // 게임플레이 모드로 시작
        _currentMode = InputMode.None;
        SwitchToGameplay();
        
        Debug.Log("[InputManager] 초기화 완료");
    }
    
    private void SubscribeToEvents()
    {
        if (PauseAction != null)
            PauseAction.performed += OnPausePerformed;
        
        if (CancelAction != null)
            CancelAction.performed += OnCancelPerformed;
        
        if (InventoryAction != null)
            InventoryAction.performed += OnInventoryPerformed;

        if (ShowCursorAction != null)
        {
            ShowCursorAction.started += OnShowCursorStarted;
            ShowCursorAction.canceled += OnShowCursorCanceled;
        }
        
        if (UiInventoryAction != null)
            UiInventoryAction.performed += OnInventoryPerformed;
        
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    public void Dispose()
    {
        Debug.Log("[InputManager] 정리 시작");
        
        // 스와이프 이벤트 구독 해제
        if (swipeDetector != null)
        {
            swipeDetector.started -= OnSwipeStarted;
            swipeDetector.performed -= OnSwipePerformed;
            swipeDetector.canceled -= OnSwipeCanceled;
        }
        
        // 기타 이벤트 구독 해제
        if (PauseAction != null)
            PauseAction.performed -= OnPausePerformed;
        
        if (CancelAction != null)
            CancelAction.performed -= OnCancelPerformed;
        
        if (InventoryAction != null)
            InventoryAction.performed -= OnInventoryPerformed;
        
        if (UiInventoryAction != null)
            UiInventoryAction.performed -= OnInventoryPerformed;
        
        if (ShowCursorAction != null)
        {
            ShowCursorAction.started -= OnShowCursorStarted;
            ShowCursorAction.canceled -= OnShowCursorCanceled;
        }
        
        InputSystem.onDeviceChange -= OnDeviceChange;
        
        gameplayActionMap?.Disable();
        uiActionMap?.Disable();
        
        Debug.Log("[InputManager] 정리 완료");
    }
    
    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    
    #endregion
    
    #region 기존 메서드들
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad gamepad))
            return;
        
        switch (change)
        {
            case InputDeviceChange.Added:
                HandleGamepadConnected(gamepad);
                break;

            case InputDeviceChange.Removed:
                HandleGamepadDisconnected(gamepad);
                break;
        }
    }
    /// <summary>
    /// 게임패드 연결 처리
    /// </summary>
    private void HandleGamepadConnected(Gamepad gamepad)
    {
        Debug.Log($"[게임패드 연결] {gamepad.displayName} (ID: {gamepad.deviceId})");
        
    }
    /// <summary>
    /// 게임패드 해제 처리
    /// </summary>
    private void HandleGamepadDisconnected(Gamepad gamepad)
    {
        Debug.Log($"[게임패드 해제] {gamepad.displayName} (ID: {gamepad.deviceId})");
    }
    
    public void SwitchToGameplay()
    {
        if (_currentMode == InputMode.Gameplay) return;
        
        uiActionMap?.Disable();
        gameplayActionMap?.Enable();
        
        _currentMode = InputMode.Gameplay;
        OnInputModeChanged?.Invoke(_currentMode);
        
        ShowCursor(false, true);
        
        Debug.Log("[InputManager] 게임플레이 모드로 전환");
    }
    
    public void SwitchToUI()
    {
        if (_currentMode == InputMode.UI) return;
        
        gameplayActionMap?.Disable();
        uiActionMap?.Enable();
        
        _currentMode = InputMode.UI;
        OnInputModeChanged?.Invoke(_currentMode);
        
        ShowCursor(true, true);
        
        Debug.Log("[InputManager] UI 모드로 전환");
    }
    
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
        if (_currentMode == InputMode.UI && UIManager.Instance != null)
        {
            if (UIManager.Instance.IsUIActive("PauseMenu"))
            {
                ClosePauseMenu();
            }
        }
    }

    private void OnShowCursorStarted(InputAction.CallbackContext obj)
    {
        ShowCursor(true);
    }

    private void OnShowCursorCanceled(InputAction.CallbackContext obj)
    {
        ShowCursor(false);
    }

    private void ShowCursor(bool isShow, bool isForce = false)
    {
        if (isForce)
        {
            _cursorVisibleStack = isShow ? 1 : 0;
        }
        else
        {
            if (isShow)
            {
                ++_cursorVisibleStack;
            }
            else
            {
                _cursorVisibleStack = math.max(0, _cursorVisibleStack - 1);
            }
        }
        
        bool finalVisibility = _cursorVisibleStack > 0;
        
        Cursor.lockState = finalVisibility ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = finalVisibility;
        
        Debug.Log($"ShowCursor: {Cursor.visible}, stackCount: {_cursorVisibleStack}");
    }
    
    private void OnInventoryPerformed(InputAction.CallbackContext obj)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[InputManager] UIManager가 없어서 일시정지 메뉴를 표시할 수 없습니다.");
            return;
        }

        // 토글 방식: 이미 메뉴가 열려있으면 닫기
        if (UIManager.Instance.IsUIActive("Inventory"))
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    /// <summary>
    /// 일시정지 메뉴 열기
    /// </summary>
    private void OpenPauseMenu()
    {
        // UI 모드로 전환
        SwitchToUI();

        GameObject menuObj = UIManager.Instance.ShowUI("PauseMenu", CanvasLayer.Scene);
        
        if (menuObj == null)
        {
            Debug.LogError("[InputManager] PauseMenu 프리팹을 찾을 수 없습니다!");
            return;
        }

        UI_Base ui = menuObj.GetComponent<UI_Base>();
        if (ui != null)
        {
            ui.AnimationChange("Open");
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
        UI_Base ui = UIManager.Instance.GetUI<UI_Base>("PauseMenu");
        
        if (ui != null)
        {
            ui.AnimationChange("Close");
        }
        
        // 게임플레이 모드로 복귀
        SwitchToGameplay();

        // 게임 재개
        // Time.timeScale = 1f;
        
        Debug.Log("[InputManager] 일시정지 메뉴 닫힘");
    }
    private void OpenInventory()
    {
        // UI 모드로 전환
        SwitchToUI();

        GameObject menuObj = UIManager.Instance.ShowUI("Inventory", CanvasLayer.Scene);
        
        if (menuObj == null)
        {
            Debug.LogError("[InputManager] Inventory 프리팹을 찾을 수 없습니다!");
            return;
        }

        UI_Base ui = menuObj.GetComponent<UI_Base>();
        if (ui != null)
        {
            ui.AnimationChange("Open");
        }
        // 게임 일시정지 (선택사항 - 필요에 따라 주석 해제)
        // Time.timeScale = 0f;
        
        Debug.Log("[InputManager] 인벤토리열림");
    }
    private void CloseInventory()
    {
        UI_Base ui = UIManager.Instance.GetUI<UI_Base>("Inventory");
        
        if (ui != null)
        {
            ui.AnimationChange("Close");
        }
        
        // 게임플레이 모드로 복귀
        SwitchToGameplay();

        // 게임 재개
        // Time.timeScale = 1f;
        
        Debug.Log("[InputManager] 인벤토리 닫힘");
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
        if (_currentMode == InputMode.Gameplay)
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

public enum InputMode
{
    None,
    Gameplay,
    UI
}