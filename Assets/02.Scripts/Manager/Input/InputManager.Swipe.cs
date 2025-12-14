using UnityEngine;

/// <summary>
/// 입력 시스템 관리 매니저
/// </summary>
public partial class InputManager : BaseManager<InputManager>, IManager
{
    [Header("Swipe Detector")]
    [SerializeField] private SwipeGestureDetector swipeDetector;
    
    [SerializeField] private float swipeThreshold = 50.0f;
    
    // SwipeDetector 접근자 (InputAction 스타일로 사용 가능)
    public SwipeGestureDetector.SwipeEvent SwipeStarted => swipeDetector?.started;
    public SwipeGestureDetector.SwipeEvent SwipePerformed => swipeDetector?.performed;
    public SwipeGestureDetector.SwipeEvent SwipeCanceled => swipeDetector?.canceled;
    
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
}
