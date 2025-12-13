using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// DualSense 터치패드 스와이프 감지기
/// InputManager와 통합하여 사용
/// </summary>
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
    
    // InputAction 스타일 이벤트
    public class SwipeEvent
    {
        private event Action<SwipeEventArgs> _callback;
        
        public void AddListener(Action<SwipeEventArgs> callback) => _callback += callback;
        public void RemoveListener(Action<SwipeEventArgs> callback) => _callback -= callback;
        public void Invoke(SwipeEventArgs args) => _callback?.Invoke(args);
        
        // InputAction 스타일 연산자
        public static SwipeEvent operator +(SwipeEvent evt, Action<SwipeEventArgs> callback)
        {
            evt?.AddListener(callback);
            return evt;
        }
        
        public static SwipeEvent operator -(SwipeEvent evt, Action<SwipeEventArgs> callback)
        {
            evt?.RemoveListener(callback);
            return evt;
        }
    }
    
    public struct SwipeEventArgs
    {
        public SwipeDirection Direction;
        public float Speed;
        public float Distance;
        public Vector2 StartPosition;
        public Vector2 EndPosition;
    }
    
    [Header("스와이프 설정")]
    [SerializeField] private float minSwipeDistance = 0.15f;
    [SerializeField] private float maxSwipeTime = 0.5f;
    
    // InputAction 스타일 이벤트들
    public SwipeEvent started = new SwipeEvent();
    public SwipeEvent performed = new SwipeEvent();
    public SwipeEvent canceled = new SwipeEvent();
    
    private int deviceId = 0;
    private bool wasTouching = false;
    private Vector2 touchStartPos;
    private float touchStartTime;
    private const int JS_TYPE_DUALSENSE = 5;
    
    void Start()
    {
        InitializeDevice();
    }
    
    private void InitializeDevice()
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
        
        if (isTouching && !wasTouching)
        {
            OnTouchStarted(currentPos);
        }
        else if (!isTouching && wasTouching)
        {
            OnTouchEnded(currentPos);
        }
        
        wasTouching = isTouching;
    }
    
    private void OnTouchStarted(Vector2 position)
    {
        touchStartPos = position;
        touchStartTime = Time.time;
        
        var args = new SwipeEventArgs
        {
            Direction = SwipeDirection.None,
            Speed = 0,
            Distance = 0,
            StartPosition = position,
            EndPosition = position
        };
        
        started.Invoke(args);
    }
    
    private void OnTouchEnded(Vector2 position)
    {
        float duration = Time.time - touchStartTime;
        
        if (duration <= maxSwipeTime)
        {
            Vector2 swipeVector = position - touchStartPos;
            float distance = swipeVector.magnitude;
            
            if (distance >= minSwipeDistance)
            {
                float speed = distance / duration;
                SwipeDirection direction = GetSwipeDirection(swipeVector);
                
                var args = new SwipeEventArgs
                {
                    Direction = direction,
                    Speed = speed,
                    Distance = distance,
                    StartPosition = touchStartPos,
                    EndPosition = position
                };
                
                performed.Invoke(args);
                Debug.Log($"스와이프: {direction}, 속도: {speed:F2}, 거리: {distance:F2}");
            }
            else
            {
                OnSwipeCanceled();
            }
        }
        else
        {
            OnSwipeCanceled();
        }
    }
    
    private void OnSwipeCanceled()
    {
        var args = new SwipeEventArgs
        {
            Direction = SwipeDirection.None,
            Speed = 0,
            Distance = 0,
            StartPosition = touchStartPos,
            EndPosition = touchStartPos
        };
        
        canceled.Invoke(args);
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
        JslDisconnectAndDisposeAll();
    }
}