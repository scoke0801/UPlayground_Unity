using UnityEngine;

namespace Game.Input
{
    /// <summary>
    /// DualSense 터치패드 제스처 감지 및 처리
    /// 스와이프, 탭, 핀치 등의 제스처를 인식합니다
    /// </summary>
    public class TouchpadGestureDetector : MonoBehaviour
    {
        [Header("제스처 설정")]
        [SerializeField] private float swipeThreshold = 0.15f;  // 스와이프로 인식하는 최소 거리
        [SerializeField] private float tapMaxDuration = 0.3f;   // 탭으로 인식하는 최대 시간
        [SerializeField] private float tapMaxMovement = 0.05f;  // 탭으로 인식하는 최대 이동 거리
        
        [Header("디버그")]
        [SerializeField] private bool showDebugLog = true;
        [SerializeField] private bool showGizmos = false;
        
        // DualSense 컨트롤러 참조
        private DualSenseWithTouchpad dualSense;
        
        // 터치 추적 변수
        private Vector2 touch1StartPos;
        private Vector2 touch1CurrentPos;
        private float touch1StartTime;
        private bool touch1WasActive;
        
        private Vector2 touch2StartPos;
        private Vector2 touch2CurrentPos;
        private float touch2StartTime;
        private bool touch2WasActive;
        
        // 멀티터치 추적
        private bool isTwoFingerGesture;
        private float initialTwoFingerDistance;

        void Start()
        {
            // DualSense 컨트롤러 찾기
            FindDualSense();
        }

        void Update()
        {
            if (dualSense == null)
            {
                // 주기적으로 컨트롤러 재검색
                if (Time.frameCount % 60 == 0)
                {
                    FindDualSense();
                }
                return;
            }

            ProcessTouchInput();
        }

        /// <summary>
        /// DualSense 컨트롤러 찾기
        /// </summary>
        private void FindDualSense()
        {
            dualSense = DualSenseWithTouchpad.Current;
            
            if (dualSense != null)
            {
                Log($"[DualSense] 터치패드 지원 컨트롤러 연결: {dualSense.name}");
            }
        }

        /// <summary>
        /// 터치 입력 처리
        /// </summary>
        private void ProcessTouchInput()
        {
            ProcessSingleTouch();
            ProcessMultiTouch();
        }

        /// <summary>
        /// 단일 터치 처리
        /// </summary>
        private void ProcessSingleTouch()
        {
            bool isTouch1Active = dualSense.Touch1Active;
            
            // 터치 시작
            if (isTouch1Active && !touch1WasActive)
            {
                OnTouchStart();
            }
            
            // 터치 중
            if (isTouch1Active)
            {
                OnTouchMove();
            }
            
            // 터치 종료
            if (!isTouch1Active && touch1WasActive)
            {
                OnTouchEnd();
            }
            
            touch1WasActive = isTouch1Active;
        }

        /// <summary>
        /// 멀티터치 처리 (현재 버전에서는 제한적)
        /// </summary>
        private void ProcessMultiTouch()
        {
            bool isTouch1Active = dualSense.Touch1Active;
            bool isTouch2Active = dualSense.Touch2Active;
            
            // 두 손가락 제스처 시작
            if (isTouch1Active && isTouch2Active && !isTwoFingerGesture)
            {
                isTwoFingerGesture = true;
                touch1CurrentPos = dualSense.TouchPosition1;
                touch2CurrentPos = dualSense.TouchPosition2;
                initialTwoFingerDistance = Vector2.Distance(touch1CurrentPos, touch2CurrentPos);
                
                Log($"[Gesture] 두 손가락 제스처 시작 (거리: {initialTwoFingerDistance:F3})");
            }
            
            // 두 손가락 제스처 진행 중
            if (isTouch1Active && isTouch2Active && isTwoFingerGesture)
            {
                Vector2 newTouch1 = dualSense.TouchPosition1;
                Vector2 newTouch2 = dualSense.TouchPosition2;
                float currentDistance = Vector2.Distance(newTouch1, newTouch2);
                
                // 핀치 제스처 감지
                float distanceDelta = currentDistance - initialTwoFingerDistance;
                if (Mathf.Abs(distanceDelta) > 0.1f)
                {
                    if (distanceDelta > 0)
                    {
                        OnPinchOut(distanceDelta);
                    }
                    else
                    {
                        OnPinchIn(Mathf.Abs(distanceDelta));
                    }
                }
            }
            
            // 두 손가락 제스처 종료
            if ((!isTouch1Active || !isTouch2Active) && isTwoFingerGesture)
            {
                isTwoFingerGesture = false;
                Log("[Gesture] 두 손가락 제스처 종료");
            }
        }

        /// <summary>
        /// 터치 시작 이벤트
        /// </summary>
        private void OnTouchStart()
        {
            touch1StartPos = dualSense.TouchPosition1;
            touch1CurrentPos = touch1StartPos;
            touch1StartTime = Time.time;
            
            Log($"[Touch] 시작: {touch1StartPos}");
        }

        /// <summary>
        /// 터치 이동 이벤트
        /// </summary>
        private void OnTouchMove()
        {
            Vector2 newPos = dualSense.TouchPosition1;
            Vector2 delta = newPos - touch1CurrentPos;
            
            if (delta.magnitude > 0.01f)
            {
                Log($"[Touch] 이동: {newPos} (델타: {delta})");
            }
            
            touch1CurrentPos = newPos;
        }

        /// <summary>
        /// 터치 종료 이벤트 및 제스처 인식
        /// </summary>
        private void OnTouchEnd()
        {
            float duration = Time.time - touch1StartTime;
            Vector2 totalSwipe = touch1CurrentPos - touch1StartPos;
            float distance = totalSwipe.magnitude;
            
            Log($"[Touch] 종료: 시간={duration:F2}s, 거리={distance:F3}");
            
            // 탭 제스처
            if (duration < tapMaxDuration && distance < tapMaxMovement)
            {
                OnTap(touch1CurrentPos);
            }
            // 스와이프 제스처
            else if (distance > swipeThreshold)
            {
                OnSwipe(totalSwipe);
            }
        }

        /// <summary>
        /// 탭 제스처 처리
        /// </summary>
        private void OnTap(Vector2 position)
        {
            Log($"[Gesture] 탭 - 위치: {position}");
            
            // 탭 제스처 처리 코드 작성
            // 예: UI 버튼 클릭, 선택 등
        }

        /// <summary>
        /// 스와이프 제스처 처리
        /// </summary>
        private void OnSwipe(Vector2 swipeVector)
        {
            float absX = Mathf.Abs(swipeVector.x);
            float absY = Mathf.Abs(swipeVector.y);
            
            string direction = "";
            
            if (absX > absY)
            {
                direction = swipeVector.x > 0 ? "→ 오른쪽" : "← 왼쪽";
            }
            else
            {
                direction = swipeVector.y > 0 ? "↑ 위" : "↓ 아래";
            }
            
            Log($"[Gesture] 스와이프 {direction} (거리: {swipeVector.magnitude:F3})");
            
            // 스와이프 제스처 처리 코드 작성
            // 예: 페이지 전환, 스크롤, 카메라 회전 등
        }

        /// <summary>
        /// 핀치 아웃 제스처 (확대)
        /// </summary>
        private void OnPinchOut(float delta)
        {
            Log($"[Gesture] 핀치 아웃 (확대) - 델타: {delta:F3}");
            
            // 핀치 아웃 제스처 처리 코드 작성
            // 예: 줌 인, 확대 등
        }

        /// <summary>
        /// 핀치 인 제스처 (축소)
        /// </summary>
        private void OnPinchIn(float delta)
        {
            Log($"[Gesture] 핀치 인 (축소) - 델타: {delta:F3}");
            
            // 핀치 인 제스처 처리 코드 작성
            // 예: 줌 아웃, 축소 등
        }

        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLog)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// 씬 뷰에서 터치 위치 시각화
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showGizmos || dualSense == null || Camera.main == null) return;
            
            // 터치 1 시각화
            if (dualSense.Touch1Active)
            {
                Vector3 worldPos1 = Camera.main.ViewportToWorldPoint(
                    new Vector3(touch1CurrentPos.x, touch1CurrentPos.y, 10f));
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(worldPos1, 0.5f);
            }
            
            // 터치 2 시각화
            if (dualSense.Touch2Active)
            {
                Vector3 worldPos2 = Camera.main.ViewportToWorldPoint(
                    new Vector3(touch2CurrentPos.x, touch2CurrentPos.y, 10f));
                
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(worldPos2, 0.5f);
            }
        }
    }
}
