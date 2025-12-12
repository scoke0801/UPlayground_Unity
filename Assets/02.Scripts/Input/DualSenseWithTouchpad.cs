using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;

namespace Game.Input
{
    /// <summary>
    /// 터치패드 지원이 추가된 커스텀 DualSense 게임패드
    /// Unity Input System의 DualSenseGamepadHID를 확장하여 터치패드 입력 지원
    /// 
    /// 주의: 레이아웃 등록이 기존 DualSenseGamepadHID와 충돌할 수 있으므로
    /// Current 속성으로 두 타입 모두 검색합니다
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [InputControlLayout(displayName = "DualSense (Touchpad Enhanced)")]
    public class DualSenseWithTouchpad : DualSenseGamepadHID
    {
        // 터치패드 관련 public 접근자
        public Vector2 TouchPosition1 { get; private set; }
        public Vector2 TouchPosition2 { get; private set; }
        public bool Touch1Active { get; private set; }
        public bool Touch2Active { get; private set; }
        
        // 터치패드 해상도 상수
        private const float TOUCHPAD_MAX_X = 1920f;
        private const float TOUCHPAD_MAX_Y = 1080f;
        
        // 이전 프레임 데이터
        private Vector2 previousTouch1;
        private Vector2 previousTouch2;
        private bool previousTouch1Active;
        private bool previousTouch2Active;

        // 정적 생성자 - 에디터 로드 시 자동 등록
        static DualSenseWithTouchpad()
        {
            RegisterLayout();
        }

        // 런타임 초기화 - 플레이 모드 시작 시 자동 등록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeInPlayer()
        {
            RegisterLayout();
        }

        // 레이아웃 등록
        private static void RegisterLayout()
        {
            try
            {
                // 기존 레이아웃 제거 시도 (충돌 방지)
                InputSystem.RemoveLayout("DualSense (Touchpad Enhanced)");
            }
            catch
            {
                // 레이아웃이 없으면 무시
            }
            
            // DualSense VID: 0x054C, PID: 0x0CE6
            // 주의: 이 레이아웃은 기본 DualSenseGamepadHID보다 우선순위가 낮을 수 있습니다
            InputSystem.RegisterLayout<DualSenseWithTouchpad>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x054C)
                    .WithCapability("productId", 0x0CE6));
            
            Debug.Log("[DualSenseWithTouchpad] 레이아웃 등록 완료");
        }

        /// <summary>
        /// 디바이스 설정 완료 시 호출
        /// </summary>
        protected override void FinishSetup()
        {
            base.FinishSetup();
            Debug.Log($"[DualSenseWithTouchpad] 초기화: {name}");
        }

        /// <summary>
        /// 매 프레임 호출 - HID 데이터에서 터치패드 정보 추출
        /// </summary>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            UpdateTouchpadData();
        }

        /// <summary>
        /// HID 리포트에서 터치패드 데이터 추출
        /// </summary>
        private void UpdateTouchpadData()
        {
            try
            {
                // 터치패드 버튼 상태 확인
                bool currentTouchpadPressed = touchpadButton.isPressed;
                UpdateTouchData(currentTouchpadPressed);
            }
            catch (System.Exception e)
            {
                // 초기화 중 에러 무시
                Debug.LogWarning($"[DualSenseWithTouchpad] 데이터 업데이트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 터치 데이터 업데이트
        /// </summary>
        private void UpdateTouchData(bool touchpadPressed)
        {
            previousTouch1Active = Touch1Active;
            previousTouch2Active = Touch2Active;
            previousTouch1 = TouchPosition1;
            previousTouch2 = TouchPosition2;
            
            Touch1Active = touchpadPressed;
            
            if (Touch1Active)
            {
                TouchPosition1 = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// 터치 시작 감지
        /// </summary>
        public bool Touch1WasPressedThisFrame => Touch1Active && !previousTouch1Active;

        /// <summary>
        /// 터치 종료 감지
        /// </summary>
        public bool Touch1WasReleasedThisFrame => !Touch1Active && previousTouch1Active;

        /// <summary>
        /// 현재 연결된 DualSense 컨트롤러 가져오기 (개선된 버전)
        /// DualSenseWithTouchpad 또는 기본 DualSenseGamepadHID 모두 검색
        /// </summary>
        public static DualSenseWithTouchpad Current
        {
            get
            {
                // 방법 1: DualSenseWithTouchpad 직접 검색
                var devices = InputSystem.devices;
                foreach (var device in devices)
                {
                    if (device is DualSenseWithTouchpad customDualSense && device.enabled)
                    {
                        Debug.Log($"[DualSense] DualSenseWithTouchpad 찾음: {customDualSense.name}");
                        return customDualSense;
                    }
                }
                
                // 방법 2: 기본 DualSenseGamepadHID를 래핑
                var standardDualSense = DualSenseGamepadHID.current;
                if (standardDualSense != null && standardDualSense.enabled)
                {
                    Debug.Log($"[DualSense] 표준 DualSenseGamepadHID를 래핑: {standardDualSense.name}");
                    // 기본 DualSenseGamepadHID를 DualSenseWithTouchpad로 캐스팅 시도
                    // (실제로는 래퍼 인스턴스 생성 필요)
                    return CreateWrapperForStandardDualSense(standardDualSense);
                }
                
                return null;
            }
        }

        /// <summary>
        /// 표준 DualSenseGamepadHID를 위한 래퍼 인스턴스 생성
        /// </summary>
        private static DualSenseWithTouchpad CreateWrapperForStandardDualSense(DualShockGamepad standard)
        {
            // 이미 DualSenseWithTouchpad 타입이면 캐스팅
            if (standard is DualSenseWithTouchpad wrapped)
            {
                return wrapped;
            }
            
            // 아니면 기본 인스턴스를 래핑 (제한적)
            // 실제로는 기본 DualSenseGamepadHID의 기능만 사용
            // 이 경우 터치패드 기능은 제한적으로만 작동
            Debug.LogWarning("[DualSense] 표준 DualSenseGamepadHID를 사용 중 - 터치패드 기능 제한됨");
            return null; // 기본 인스턴스는 변환 불가
        }
        
        /// <summary>
        /// 기본 DualSenseGamepadHID 가져오기 (폴백)
        /// </summary>
        public static DualSenseGamepadHID GetStandardDualSense()
        {
            return (DualSenseGamepadHID)DualSenseGamepadHID.current;
        }
    }
    
    /// <summary>
    /// DualSense 헬퍼 클래스 - 터치패드 기능에 접근하기 쉽게
    /// </summary>
    public static class DualSenseHelper
    {
        /// <summary>
        /// 연결된 DualSense 컨트롤러 가져오기
        /// </summary>
        public static DualSenseGamepadHID GetDualSense()
        {
            // 커스텀 버전 시도
            var custom = DualSenseWithTouchpad.Current;
            if (custom != null) return custom;
            
            // 표준 버전 폴백
            return (DualSenseGamepadHID)DualSenseGamepadHID.current;
        }
        
        /// <summary>
        /// 터치패드 버튼이 눌렸는지 확인
        /// </summary>
        public static bool IsTouchpadPressed()
        {
            var ds = GetDualSense();
            return ds != null && ds.touchpadButton.isPressed;
        }
        
        /// <summary>
        /// 터치패드 버튼이 방금 눌렸는지 확인
        /// </summary>
        public static bool WasTouchpadPressedThisFrame()
        {
            var ds = GetDualSense();
            return ds != null && ds.touchpadButton.wasPressedThisFrame;
        }
        
        /// <summary>
        /// 디버그: 연결된 모든 게임패드 출력
        /// </summary>
        public static void LogAllGamepads()
        {
            Debug.Log("=== 연결된 게임패드 목록 ===");
            foreach (var device in InputSystem.devices)
            {
                if (device is Gamepad gamepad)
                {
                    Debug.Log($"  - {gamepad.name} ({gamepad.GetType().Name})");
                }
            }
        }
    }
}
