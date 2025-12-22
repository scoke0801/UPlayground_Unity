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
    
    UI_VirtualCursor _uiVirtualCursor;
    
    private bool _isGamepadActive = false;
    #region IManager 구현
    
    public void Init()
    {
        Debug.Log("[InputManager] 초기화 시작");
        
        Texture2D cursorTexture = Resources.Load<Texture2D>("Cursor/cursor_default");;
        
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        // SwipeDetector 초기화
        InitializeSwipeDetector();

        // Actions 초기화
        InitInputAction();
        
        // 이벤트 구독
        SubscribeToEvents();
        
        // 게임플레이 모드로 시작
        _currentMode = InputMode.None;
        SwitchToGameplay(true);
        
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
        InputSystem.onActionChange += OnActionChange;
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
        InputSystem.onActionChange -= OnActionChange;
        
        gameplayActionMap?.Disable();
        uiActionMap?.Disable();
        
        Debug.Log("[InputManager] 정리 완료");
    }
    
    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    
    #endregion
    
    #region
    
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
    
    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionStarted || change == InputActionChange.ActionPerformed)
        {
            var action = (InputAction)obj;
            var control = action.activeControl;

            // 1. 센서 데이터 무시 (자이로, 가속도계 등)
            if (control.device is Sensor)
            {
                return;
            }

            // 2. 미세한 입력 무시 (EvaluateMagnitude 사용)
            // Magnitude가 임계값(예: 0.1)보다 작으면 장치 전환을 수행하지 않음
            if (control.EvaluateMagnitude() < 0.1f)
            {
                return;
            }
            var inputAction = obj as InputAction;
            var device = inputAction?.activeControl?.device;

            bool current = device is Gamepad;
            if (_isGamepadActive != current)
            {
                _isGamepadActive = current;
                RefreshCursorState();
            }
        }
    }
    /// <summary>
    /// 게임패드 연결 처리
    /// </summary>
    private void HandleGamepadConnected(Gamepad gamepad)
    {
       // RefreshCursorState();
        Debug.Log($"[게임패드 연결] {gamepad.displayName} (ID: {gamepad.deviceId})");
        
    }
    /// <summary>
    /// 게임패드 해제 처리
    /// </summary>
    private void HandleGamepadDisconnected(Gamepad gamepad)
    {
       // RefreshCursorState();
        Debug.Log($"[게임패드 해제] {gamepad.displayName} (ID: {gamepad.deviceId})");
    }
    
    public void SwitchToGameplay(bool isForce)
    {
        if (_currentMode == InputMode.Gameplay) return;
        
        uiActionMap?.Disable();
        gameplayActionMap?.Enable();
        
        _currentMode = InputMode.Gameplay;
        OnInputModeChanged?.Invoke(_currentMode);
        
        ShowCursor(false, isForce);
        
        Debug.Log("[InputManager] 게임플레이 모드로 전환");
    }
    
    public void SwitchToUI(bool isForce)
    {
        if (_currentMode == InputMode.UI) return;
        
        gameplayActionMap?.Disable();
        uiActionMap?.Enable();
        
        _currentMode = InputMode.UI;
        OnInputModeChanged?.Invoke(_currentMode);
        
        ShowCursor(true, isForce);
        
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
        
        RefreshCursorState();
        
        Debug.Log($"ShowCursor: {Cursor.visible}, stackCount: {_cursorVisibleStack}");
    }

    private void RefreshCursorState()
    {
        // 가상 커서 세팅
        if (_uiVirtualCursor == null)
        {
            GameObject go = UIManager.Instance.ShowUI("Cursor");
            if (go != null)
            {
                _uiVirtualCursor = go.GetComponent<UI_VirtualCursor>();
            }
        }
        
        Debug.Log($"CursorStack: {_cursorVisibleStack}, gamePadConnected: {_isGamepadActive}");
        bool finalVisibility = _cursorVisibleStack > 0;

        if (finalVisibility)
        {
            if (_isGamepadActive)
            {
                if(_uiVirtualCursor)
                    _uiVirtualCursor.Show();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if(_uiVirtualCursor)
                    _uiVirtualCursor.Hide();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                Debug.Log("[InputManager] Cursor Show");
            }
        }
        else
        {
            if(_uiVirtualCursor)
                _uiVirtualCursor.Hide();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
        SwitchToUI(true);

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
        SwitchToGameplay(false);

        // 게임 재개
        // Time.timeScale = 1f;
        
        Debug.Log("[InputManager] 일시정지 메뉴 닫힘");
    }
    private void OpenInventory()
    {
        // UI 모드로 전환
        SwitchToUI(true);

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
        SwitchToGameplay(false);

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