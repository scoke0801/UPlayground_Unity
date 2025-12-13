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
        
        if (HoldAction != null)
        {
            HoldAction.started += OnHoldStarted;
            HoldAction.performed += OnHoldPerformed;
            HoldAction.canceled += OnHoldCanceled;
        }
        
        if (TouchPadAction != null)
            TouchPadAction.performed += OnTouchPadPerformed;
        
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
    
    #region 기존 메서드들 (생략 - 원본 유지)
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change) { }
    private void HandleGamepadConnected(Gamepad gamepad) { }
    private void HandleGamepadDisconnected(Gamepad gamepad) { }
    
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
    
    private void OnPausePerformed(InputAction.CallbackContext context) { }
    private void OnCancelPerformed(InputAction.CallbackContext context) { }
    private void OnInventoryPerformed(InputAction.CallbackContext obj) { }
    private void OnHoldStarted(InputAction.CallbackContext obj) { }
    private void OnHoldPerformed(InputAction.CallbackContext obj) { }
    private void OnHoldCanceled(InputAction.CallbackContext obj) { }
    private void OnTouchPadPerformed(InputAction.CallbackContext obj) { }
    
    #endregion
}

public enum InputMode
{
    None,
    Gameplay,
    UI
}