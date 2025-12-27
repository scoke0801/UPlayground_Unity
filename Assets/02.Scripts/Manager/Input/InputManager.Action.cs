using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 입력 시스템 관리 매니저 - GameInputAction 관리
/// </summary>
public partial class InputManager : BaseManager<InputManager>, IManager
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
    public InputAction SprintAction { get; private set; }
    public InputAction HeavyAttackAction { get; private set; }
    public InputAction InteractAction { get; private set; }
    public InputAction PauseAction { get; private set; }
    public InputAction InventoryAction { get; private set; }
    public InputAction UiInventoryAction { get; private set; }
    public InputAction ShowCursorAction { get; private set; }
    
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
    public InputAction CursorMoveAction { get; private set; }
    public InputAction CursorClickAction { get; private set; }
    
    // Test Actions
    public InputAction HoldAction { get; private set; }
    public InputAction SwipeAction { get; private set; }
    public InputAction TouchPadAction { get; private set; }
    
    public void InitInputAction()
    {
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
        SprintAction = gameplayActionMap.FindAction("Sprint");
        HeavyAttackAction = gameplayActionMap.FindAction("HeavyAttack");
        InteractAction = gameplayActionMap.FindAction("Interact");
        PauseAction = gameplayActionMap.FindAction("Pause");
        InventoryAction = gameplayActionMap.FindAction("Inventory");
        ShowCursorAction = gameplayActionMap.FindAction("ShowCursor");
        
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
        UiInventoryAction = uiActionMap.FindAction("Inventory");
        CursorClickAction = uiActionMap.FindAction("CursorClick");
        CursorMoveAction = uiActionMap.FindAction("CursorMove");
        
        // Test Actions
        HoldAction = gameplayActionMap.FindAction("HoldTest");
        SwipeAction = gameplayActionMap.FindAction("SwipeTest");
        TouchPadAction = gameplayActionMap.FindAction("TouchPadTest");
    }
}
