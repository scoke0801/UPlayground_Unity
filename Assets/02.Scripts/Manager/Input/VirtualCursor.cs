using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class VirtualCursor : UI_Base
{
    [Header("커서 설정")]
    [SerializeField] private RectTransform cursorTransform;
    [SerializeField] private float cursorSpeed = 1000f;
    [SerializeField] private Canvas canvas;
    
    [Header("게임패드 입력")]
    [SerializeField] private float deadzone = 0.1f;
    
    private Vector2 currentCursorPosition;
    private Gamepad gamepad;
    
    void Start()
    {
        // 게임패드 연결 확인
        gamepad = Gamepad.current;
        
        // 커서 초기 위치 설정 (화면 중앙)
        currentCursorPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        UpdateCursorPosition();
        
        // 마우스 커서 숨기기 (선택사항)
        Cursor.visible = false;
    }
    
    void Update()
    {
        if (gamepad == null)
        {
            gamepad = Gamepad.current;
            return;
        }
        
        // 왼쪽 스틱으로 커서 이동
        Vector2 stickInput = gamepad.leftStick.ReadValue();
        
        // 데드존 적용
        if (stickInput.magnitude < deadzone)
            stickInput = Vector2.zero;
        
        // 커서 위치 업데이트
        currentCursorPosition += stickInput * cursorSpeed * Time.deltaTime;
        
        // 화면 밖으로 나가지 않도록 제한
        currentCursorPosition.x = Mathf.Clamp(currentCursorPosition.x, 0, Screen.width);
        currentCursorPosition.y = Mathf.Clamp(currentCursorPosition.y, 0, Screen.height);
        
        UpdateCursorPosition();
        
        // A 버튼으로 클릭
        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            SimulateClick();
        }
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