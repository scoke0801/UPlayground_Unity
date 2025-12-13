using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 시스템 관리 매니저
/// SwipeGestureDetector 통합 버전
/// </summary>
public class InputManager : BaseManager<InputManager>, IManager
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Swipe Detector")]
    [SerializeField] private SwipeGestureDetector swipeDetector;
    
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
    public InputAction InventoryAction { get; private set; }
    public InputAction UiInventoryAction { get; private set; }
    
    // Skill Actions
    public InputAction Skill1Action { get; private set; }
    public InputAction Skill2Action { get; private set; }
    public InputAction Skill3Action { get; private set; }
    public InputAction Skill4Action { get; private set; }
    
    // UI Actions
    public InputAction NavigateAction { get; private set; }
    public InputAction SubmitAction { get; private set; }
    public InputAction CancelAction { get; private set; }
    public InputAction PointAction { get; private set; }
    
    // Test Actions
    public InputAction HoldAction { get; private set; }
    public InputAction SwipeAction { get; private set; }
    public InputAction TouchPadAction { get; private set; }
    
    // SwipeDetector 접근자 (InputAction 스타일로 사용 가능)
    public SwipeGestureDetector.SwipeEvent SwipeStarted => swipeDetector?.started;
    public SwipeGestureDetector.SwipeEvent SwipePerformed => swipeDetector?.performed;
    public SwipeGestureDetector.SwipeEvent SwipeCanceled => swipeDetector?.canceled;
    
    private InputMode currentMode = InputMode.None;
    public System.Action<InputMode> OnInputModeChanged;
    
    [SerializeField] private float swipeThreshold = 50.0f;
    
    #region IManager 구현
    
    public void Init()
    {
        Debug.Log("[InputManager] 초기화 시작");
        
        // SwipeDetector 초기화
        InitializeSwipeDetector();
        
        // Input Actions Asset 로드
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Input/PlayerInputActions");
            if (inputActions == null)
            {
                Debug.LogError("[InputManager] PlayerInputActions를 찾을 수 없습니다!");
                return;
            }
        }
        
        // Action Maps 가져오기
        gameplayActionMap = inputActions.FindActionMap("Gameplay");
        uiActionMap = inputActions.FindActionMap("UI");
        
        if (gameplayActionMap == null || uiActionMap == null)
        {
            Debug.LogError("[InputManager] ActionMap을 찾을 수 없습니다!");
            return;
        }
        
        // Actions 초기화
        InitializeActions();
        
        // 이벤트 구독
        SubscribeToEvents();
        
        // 게임플레이 모드로 시작
        currentMode = InputMode.None;
        SwitchToGameplay();
        
        Debug.Log("[InputManager] 초기화 완료");
    }
    
    private void InitializeSwipeDetector()
    {
        // SwipeDetector가 없으면 생성
        if (swipeDetector == null)
        {
            GameObject detectorObj = new GameObject("SwipeGestureDetector");
            swipeDetector = detectorObj.AddComponent<SwipeGestureDetector>();
            DontDestroyOnLoad(detectorObj);
        }
        
        // InputAction 스타일로 이벤트 구독
        // 방법 1: += 연산자 사용
        swipeDetector.started += OnSwipeStarted;
        swipeDetector.performed += OnSwipePerformed;
        swipeDetector.canceled += OnSwipeCanceled;
    }
    
    private void InitializeActions()
    {
        // Gameplay Actions
        MoveAction = gameplayActionMap.FindAction("Move");
        LookAction = gameplayActionMap.FindAction("Look");
        JumpAction = gameplayActionMap.FindAction("Jump");
        RunAction = gameplayActionMap.FindAction("Run");
        RollAction = gameplayActionMap.FindAction("Roll");
        AttackAction = gameplayActionMap.FindAction("Attack");
        HeavyAttackAction = gameplayActionMap.FindAction("HeavyAttack");
        InteractAction = gameplayActionMap.FindAction("Interact");
        PauseAction = gameplayActionMap.FindAction("Pause");
        InventoryAction = gameplayActionMap.FindAction("Inventory");
        UiInventoryAction = uiActionMap.FindAction("Inventory");
        
        // Skill Actions
        Skill1Action = gameplayActionMap.FindAction("Skill1");
        Skill2Action = gameplayActionMap.FindAction("Skill2");
        Skill3Action = gameplayActionMap.FindAction("Skill3");
        Skill4Action = gameplayActionMap.FindAction("Skill4");
        
        // UI Actions
        NavigateAction = uiActionMap.FindAction("Navigate");
        SubmitAction = uiActionMap.FindAction("Submit");
        CancelAction = uiActionMap.FindAction("Cancel");
        PointAction = uiActionMap.FindAction("Point");
        
        // Test Actions
        HoldAction = gameplayActionMap.FindAction("HoldTest");
        SwipeAction = gameplayActionMap.FindAction("SwipeTest");
        TouchPadAction = gameplayActionMap.FindAction("TouchPadTest");
    }
    
    private void SubscribeToEvents()
    {
        if (PauseAction != null)
            PauseAction.performed += OnPausePerformed;
        
        if (CancelAction != null)
            CancelAction.performed += OnCancelPerformed;
        
        if (InventoryAction != null)
            InventoryAction.performed += OnInventoryPerformed;
        
        if (UiInventoryAction != null)
            UiInventoryAction.performed += OnInventoryPerformed;
        
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    #region 스와이프 이벤트 핸들러
    
    private void OnSwipeStarted(SwipeGestureDetector.SwipeEventArgs args)
    {
        Debug.Log($"[InputManager] 스와이프 시작: {args.StartPosition}");
        // 스와이프 시작 처리
    }
    
    private void OnSwipePerformed(SwipeGestureDetector.SwipeEventArgs args)
    {
        Debug.Log($"[InputManager] 스와이프 감지: {args.Direction}, 속도: {args.Speed:F2}");
        
        // 방향별 처리 예시
        switch (args.Direction)
        {
            case SwipeGestureDetector.SwipeDirection.Up:
                HandleSwipeUp(args);
                break;
            case SwipeGestureDetector.SwipeDirection.Down:
                HandleSwipeDown(args);
                break;
            case SwipeGestureDetector.SwipeDirection.Left:
                HandleSwipeLeft(args);
                break;
            case SwipeGestureDetector.SwipeDirection.Right:
                HandleSwipeRight(args);
                break;
        }
    }
    
    private void OnSwipeCanceled(SwipeGestureDetector.SwipeEventArgs args)
    {
        Debug.Log("[InputManager] 스와이프 취소됨");
    }
    
    // 방향별 처리 메서드들
    private void HandleSwipeUp(SwipeGestureDetector.SwipeEventArgs args)
    {
        // 위 스와이프 처리 (예: 스킬 사용)
        Debug.Log("위쪽 스와이프 - 스킬 실행");
    }
    
    private void HandleSwipeDown(SwipeGestureDetector.SwipeEventArgs args)
    {
        // 아래 스와이프 처리 (예: 회피)
        Debug.Log("아래쪽 스와이프 - 회피");
    }
    
    private void HandleSwipeLeft(SwipeGestureDetector.SwipeEventArgs args)
    {
        // 왼쪽 스와이프 처리 (예: 무기 전환)
        Debug.Log("왼쪽 스와이프 - 무기 전환");
    }
    
    private void HandleSwipeRight(SwipeGestureDetector.SwipeEventArgs args)
    {
        // 오른쪽 스와이프 처리 (예: 아이템 사용)
        Debug.Log("오른쪽 스와이프 - 아이템 사용");
    }
    
    #endregion

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
        if (currentMode == InputMode.Gameplay) return;
        
        uiActionMap?.Disable();
        gameplayActionMap?.Enable();
        
        currentMode = InputMode.Gameplay;
        OnInputModeChanged?.Invoke(currentMode);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[InputManager] 게임플레이 모드로 전환");
    }
    
    public void SwitchToUI()
    {
        if (currentMode == InputMode.UI) return;
        
        gameplayActionMap?.Disable();
        uiActionMap?.Enable();
        
        currentMode = InputMode.UI;
        OnInputModeChanged?.Invoke(currentMode);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[InputManager] UI 모드로 전환");
    }
    
    public InputMode CurrentMode => currentMode;
    
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

public enum InputMode
{
    None,
    Gameplay,
    UI
}