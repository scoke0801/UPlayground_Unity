using UnityEngine;
using Steamworks;

public class SteamInputSwipeDetector : MonoBehaviour
{
    public enum SwipeDirection { None, Up, Down, Left, Right }
    
    [Header("스와이프 설정")]
    [SerializeField] private float minSwipeDistance = 0.15f;
    [SerializeField] private float maxSwipeTime = 0.5f;
    
    // Steam Input 핸들
    private InputHandle_t[] controllers;
    private InputActionSetHandle_t gameplaySet;
    private InputAnalogActionHandle_t touchpadPosAction;
    private InputDigitalActionHandle_t touchpadActiveAction;
    
    // 스와이프 추적
    private bool wasTouching = false;
    private Vector2 touchStartPos;
    private float touchStartTime;
    
    public event System.Action<SwipeDirection, float, float> OnSwipeDetected;
    
    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam이 초기화되지 않았습니다!");
            return;
        }
        
        // 액션 핸들 가져오기
        gameplaySet = SteamInput.GetActionSetHandle("gameplay");
        touchpadPosAction = SteamInput.GetAnalogActionHandle("TouchpadPosition");
        touchpadActiveAction = SteamInput.GetDigitalActionHandle("TouchpadActive");
        
        Debug.Log("Steam Input 초기화 완료");
    }
    
    void Update()
    {
        if (!SteamManager.Initialized) return;
        
        // 연결된 컨트롤러 가져오기
        controllers = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
        int count = SteamInput.GetConnectedControllers(controllers);
        
        if (count == 0) return;
        
        // 첫 번째 컨트롤러 사용
        InputHandle_t controller = controllers[0];
        
        // 액션 셋 활성화
        SteamInput.ActivateActionSet(controller, gameplaySet);
        
        // 터치패드 상태 읽기
        InputAnalogActionData_t posData = SteamInput.GetAnalogActionData(controller, touchpadPosAction);
        InputDigitalActionData_t activeData = SteamInput.GetDigitalActionData(controller, touchpadActiveAction);
        
        bool isTouching = activeData.bState != 0;  // byte를 bool로 변환
        Vector2 currentPos = new Vector2(posData.x, posData.y);
        
        // 터치 시작
        if (isTouching && !wasTouching)
        {
            touchStartPos = currentPos;
            touchStartTime = Time.time;
        }
        // 터치 종료 - 스와이프 판정
        else if (!isTouching && wasTouching)
        {
            float duration = Time.time - touchStartTime;
            
            if (duration <= maxSwipeTime)
            {
                Vector2 swipeVector = currentPos - touchStartPos;
                float distance = swipeVector.magnitude;
                
                if (distance >= minSwipeDistance)
                {
                    float speed = distance / duration;
                    SwipeDirection direction = GetSwipeDirection(swipeVector);
                    
                    OnSwipeDetected?.Invoke(direction, speed, distance);
                    Debug.Log($"스와이프: {direction}, 속도: {speed:F2}, 거리: {distance:F2}");
                }
            }
        }
        
        wasTouching = isTouching;
    }
    
    private SwipeDirection GetSwipeDirection(Vector2 swipe)
    {
        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            return swipe.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        else
            return swipe.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
    }
    
    void OnDestroy()
    {
        // Steam Input 정리는 SteamManager가 처리
    }
}