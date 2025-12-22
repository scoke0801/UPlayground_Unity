using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UI_VirtualCursor : UI_Base
{
    [Header("커서 설정")]
    [SerializeField] private RectTransform cursorTransform;
    [SerializeField] private float cursorSpeed = 1000f;
    
    [Header("Input Actions")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction clickAction;
    
    private Vector2 currentCursorPosition;
    private Vector2 moveInput;

    protected override void OnShow()
    {
        // InputAction 이벤트 연결
        InputManager.Instance.CursorMoveAction.performed += OnMove;
        InputManager.Instance.CursorMoveAction.canceled += OnMove;
        InputManager.Instance.CursorClickAction.performed += OnClick;
        
        // 커서 초기 위치 설정 (화면 중앙)
        currentCursorPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        UpdateCursorPosition();
    }

    protected override void OnHide()
    {
        InputManager.Instance.CursorMoveAction.performed -= OnMove;
        InputManager.Instance.CursorMoveAction.canceled -= OnMove;
        InputManager.Instance.CursorClickAction.performed -= OnClick;
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnClick(InputAction.CallbackContext context)
    {
        SimulateClick();
    }
    
    protected override void Update()
    {
        // 커서 위치 업데이트
        currentCursorPosition += cursorSpeed * Time.deltaTime * moveInput;
        
        // 화면 밖으로 나가지 않도록 제한
        currentCursorPosition.x = Mathf.Clamp(currentCursorPosition.x, 0, Screen.width);
        currentCursorPosition.y = Mathf.Clamp(currentCursorPosition.y, 0, Screen.height);
        
        UpdateCursorPosition();
    }
    
    void UpdateCursorPosition()
    {
        if (cursorTransform != null)
        {
            cursorTransform.position = currentCursorPosition;
        }
    }
    
    void SimulateClick()
    {
        // UI 요소와 상호작용
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = currentCursorPosition
        };
        
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        
        if (results.Count > 0)
        {
            GameObject clickedObject = results[0].gameObject;
            ExecuteEvents.Execute(clickedObject, pointerData, ExecuteEvents.pointerClickHandler);
            
            Debug.Log($"클릭: {clickedObject.name}");
        }
    }
}