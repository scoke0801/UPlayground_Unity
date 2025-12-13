using UnityEngine;
using Steamworks;

public class SteamInputAdvancedSwipe : MonoBehaviour
{
    public enum SwipeDirection { None, Up, Down, Left, Right }
    public enum GestureType { Tap, Swipe, Hold, TwoFingerSwipe }
    
    [Header("제스처 설정")]
    [SerializeField] private float minSwipeDistance = 0.15f;
    [SerializeField] private float maxSwipeTime = 0.5f;
    [SerializeField] private float holdThreshold = 0.5f;
    [SerializeField] private float tapMaxTime = 0.2f;
    
    private InputHandle_t[] controllers;
    private InputActionSetHandle_t gameplaySet;
    private InputAnalogActionHandle_t touchpadPosAction;
    private InputDigitalActionHandle_t touchpadActiveAction;
    
    private bool wasTouching = false;
    private Vector2 touchStartPos;
    private float touchStartTime;
    
    public event System.Action<SwipeDirection, float, float> OnSwipe;
    public event System.Action<Vector2> OnTap;
    public event System.Action<Vector2, float> OnHold;
    
    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam이 초기화되지 않았습니다!");
            enabled = false;
            return;
        }
        
        gameplaySet = SteamInput.GetActionSetHandle("gameplay");
        touchpadPosAction = SteamInput.GetAnalogActionHandle("TouchpadPosition");
        touchpadActiveAction = SteamInput.GetDigitalActionHandle("TouchpadActive");
    }
    
    void Update()
    {
        if (!SteamManager.Initialized) return;
        
        controllers = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
        int count = SteamInput.GetConnectedControllers(controllers);
        if (count == 0) return;
        
        InputHandle_t controller = controllers[0];
        SteamInput.ActivateActionSet(controller, gameplaySet);
        
        // 터치패드 데이터 읽기
        InputAnalogActionData_t posData = SteamInput.GetAnalogActionData(controller, touchpadPosAction);
        InputDigitalActionData_t activeData = SteamInput.GetDigitalActionData(controller, touchpadActiveAction);
        
        bool isTouching = activeData.bState != 0;  // byte를 bool로 변환
        Vector2 currentPos = new Vector2(posData.x, posData.y);
        
        if (isTouching && !wasTouching)
        {
            // 터치 시작
            OnTouchStart(currentPos);
        }
        else if (isTouching && wasTouching)
        {
            // 터치 유지
            OnTouchUpdate(currentPos);
        }
        else if (!isTouching && wasTouching)
        {
            // 터치 종료
            OnTouchEnd(currentPos);
        }
        
        wasTouching = isTouching;
    }
    
    private void OnTouchStart(Vector2 pos)
    {
        touchStartPos = pos;
        touchStartTime = Time.time;
    }
    
    private void OnTouchUpdate(Vector2 currentPos)
    {
        float duration = Time.time - touchStartTime;
        
        // 홀드 제스처 감지
        if (duration >= holdThreshold)
        {
            Vector2 delta = currentPos - touchStartPos;
            if (delta.magnitude < 0.05f) // 거의 움직이지 않음
            {
                OnHold?.Invoke(touchStartPos, duration);
            }
        }
    }
    
    private void OnTouchEnd(Vector2 currentPos)
    {
        float duration = Time.time - touchStartTime;
        Vector2 swipeVector = currentPos - touchStartPos;
        float distance = swipeVector.magnitude;
        
        // 탭 제스처
        if (duration <= tapMaxTime && distance < 0.05f)
        {
            OnTap?.Invoke(touchStartPos);
            Debug.Log($"탭 감지: {touchStartPos}");
        }
        // 스와이프 제스처
        else if (duration <= maxSwipeTime && distance >= minSwipeDistance)
        {
            float speed = distance / duration;
            SwipeDirection direction = GetSwipeDirection(swipeVector);
            
            OnSwipe?.Invoke(direction, speed, distance);
            Debug.Log($"스와이프: {direction}, 속도: {speed:F2}");
        }
    }
    
    private SwipeDirection GetSwipeDirection(Vector2 swipe)
    {
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            return swipe.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        else
            return swipe.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
    }
    
    // 디버그용 - 컨트롤러 정보 표시
    public string GetControllerInfo()
    {
        if (!SteamManager.Initialized || controllers == null || controllers.Length == 0)
            return "컨트롤러 없음";
        
        var controller = controllers[0];
        var inputType = SteamInput.GetInputTypeForHandle(controller);
        
        return $"컨트롤러 타입: {inputType}";
    }
}