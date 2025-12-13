using System.Runtime.InteropServices;
using UnityEngine;

public class SwipeGestureDetector : MonoBehaviour
{
    [DllImport("JoyShockLibrary")]
    private static extern int JslConnectDevices();
    
    [DllImport("JoyShockLibrary")]
    private static extern void JslDisconnectAndDisposeAll();
    
    [DllImport("JoyShockLibrary")]
    private static extern int JslGetConnectedDeviceHandles(int[] handles, int size);
    
    [DllImport("JoyShockLibrary")]
    private static extern int JslGetControllerType(int deviceId);

    
    [DllImport("JoyShockLibrary")]
    private static extern bool JslGetTouchDown(int deviceId, bool secondTouch);
    [DllImport("JoyShockLibrary")]
    private static extern float JslGetTouchX(int deviceId, bool secondTouch);
    [DllImport("JoyShockLibrary")]
    private static extern float JslGetTouchY(int deviceId, bool secondTouch);

    public enum SwipeDirection { None, Up, Down, Left, Right }
    
    [Header("스와이프 설정")]
    [SerializeField] private float minSwipeDistance = 0.15f;  // 정규화된 거리
    [SerializeField] private float maxSwipeTime = 0.5f;       // 최대 스와이프 시간
    
    private int deviceId = 0;  // JslConnectDevices로 얻은 핸들
    private bool wasTouching = false;
    private Vector2 touchStartPos;
    private float touchStartTime;
    private const int JS_TYPE_DUALSENSE = 5;
    public event System.Action<SwipeDirection, float, float> OnSwipeDetected;
    
    void Start()
    {
        int count = JslConnectDevices();
        if (count > 0)
        {
            int[] handles = new int[count];
            JslGetConnectedDeviceHandles(handles, count);
            
            foreach (int handle in handles)
            {
                if (JslGetControllerType(handle) == JS_TYPE_DUALSENSE)
                {
                    deviceId = handle;
                    Debug.Log("DualSense 연결됨");
                    break;
                }
            }
        }
    }
    void Update()
    {
        bool isTouching = JslGetTouchDown(deviceId, false);
        Vector2 currentPos = new Vector2(
            JslGetTouchX(deviceId, false),
            JslGetTouchY(deviceId, false)
        );
        
        // 터치 시작
        if (isTouching && !wasTouching)
        {
            touchStartPos = currentPos;
            touchStartTime = Time.time;
            Debug.Log("Touchpad is touching");
        }
        // 터치 종료 - 스와이프 판정
        else if (!isTouching && wasTouching)
        {
            Debug.Log("Touchpad is touching - swiped");
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
}