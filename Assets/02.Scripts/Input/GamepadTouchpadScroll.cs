using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class GamepadTouchpadScroll : MonoBehaviour
{
    [Header("Target UI")]
    public ScrollRect scrollRect;

    [Header("Settings")]
    public float scrollSpeed = 0.5f;
    public bool invertY = true;

    private Vector2 _previousTouchPos;
    private bool _isTouching = false;

    void Update()
    {
        if (Gamepad.current == null) return;

        var ps4Controller = Gamepad.current as DualShockGamepad;
        if (ps4Controller == null) return;

        return;
        // 1. 컨트롤 가져오기
        var touch0 = ps4Controller.GetChildControl<TouchControl>("touch0");
        if (touch0 == null) return;

        // [수정된 부분] isInContact 대신 press.isPressed 사용
        // press는 물리적인 클릭이 아니라 '터치 인식 중' 상태를 의미합니다.
        bool isContact = touch0.press.isPressed;

        if (isContact)
        {
            // 2. 위치값 읽기
            Vector2 currentTouchPos = touch0.position.ReadValue();

            if (!_isTouching)
            {
                // 터치 시작 순간
                _previousTouchPos = currentTouchPos;
                _isTouching = true;
            }
            else
            {
                // 터치 중 (드래그)
                Vector2 delta = currentTouchPos - _previousTouchPos;
                ApplyScroll(delta);
                _previousTouchPos = currentTouchPos;
            }
        }
        else
        {
            // 터치 종료
            _isTouching = false;
        }
    }

    private void ApplyScroll(Vector2 delta)
    {
        if (scrollRect != null)
        {
            float yMove = delta.y * scrollSpeed * Time.deltaTime;
            
            // 필요시 X축 스크롤도 추가 가능: delta.x
            
            if (invertY) yMove = -yMove;
            scrollRect.verticalNormalizedPosition += yMove;
        }
    }
}