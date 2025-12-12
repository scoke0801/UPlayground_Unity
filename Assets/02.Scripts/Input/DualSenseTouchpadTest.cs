using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Game.Input
{
    /// <summary>
    /// DualSense 터치패드 기능 테스트 스크립트
    /// 모든 가능한 방법으로 DualSense를 찾습니다
    /// </summary>
    public class DualSenseTouchpadTest : MonoBehaviour
    {
        [Header("테스트 설정")]
        [SerializeField] private bool enableTest = true;
        [SerializeField] private float logInterval = 2.0f;
        
        private Gamepad currentGamepad;  // 일반 게임패드로도 사용
        private DualSenseGamepadHID dualSense;
        private DualSenseWithTouchpad customDualSense;
        private float lastLogTime;
        private bool isSearching = false;
        
        void Start()
        {
            Debug.Log("====================================");
            Debug.Log("=== DualSense 터치패드 테스트 시작 ===");
            Debug.Log("====================================\n");
            
            // Input System 디바이스 변경 이벤트 등록
            InputSystem.onDeviceChange += OnDeviceChange;
            
            FindDualSense();
        }
        
        void OnDestroy()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }
        
        /// <summary>
        /// 디바이스 연결/해제 이벤트 핸들러
        /// </summary>
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log($"<color=green>✓ 디바이스 연결됨: {device.name} ({device.GetType().Name})</color>");
                    FindDualSense();
                    break;
                    
                case InputDeviceChange.Removed:
                    Debug.Log($"<color=red>✗ 디바이스 연결 해제됨: {device.name}</color>");
                    if (device == currentGamepad || device == dualSense)
                    {
                        currentGamepad = null;
                        dualSense = null;
                        customDualSense = null;
                    }
                    break;
            }
        }
        
        void Update()
        {
            if (!enableTest) return;
            
            // 컨트롤러가 없으면 재검색
            if (currentGamepad == null && !isSearching)
            {
                if (Time.frameCount % 300 == 0) // 5초마다
                {
                    FindDualSense();
                }
                return;
            }
            
            TestTouchpad();
            TestButtons();
        }
        
        /// <summary>
        /// DualSense 컨트롤러 찾기 - 모든 방법 시도
        /// </summary>
        private void FindDualSense()
        {
            if (isSearching) return;
            isSearching = true;
            
            Debug.Log("\n=== DualSense 검색 시작 ===");
            LogAllDevices();
            
            bool found = false;
            
            // ========================================
            // 방법 1: DualSenseGamepadHID.current (가장 확실)
            // ========================================
            Debug.Log("\n[방법 1] DualSenseGamepadHID.current 검색...");
            var dsHID = DualSenseGamepadHID.current;
            if (dsHID != null)
            {
                dualSense = (DualSenseGamepadHID)dsHID;
                currentGamepad = dsHID;
                customDualSense = dsHID as DualSenseWithTouchpad;
                
                Debug.Log($"<color=green>✓ DualSenseGamepadHID 발견!</color>");
                Debug.Log($"  이름: {dsHID.name}");
                Debug.Log($"  타입: {dsHID.GetType().Name}");
                Debug.Log($"  터치패드 버튼: {dsHID.touchpadButton.name}");
                found = true;
            }
            else
            {
                Debug.Log("<color=yellow>  ✗ DualSenseGamepadHID.current = null</color>");
            }
            
            // ========================================
            // 방법 2: DualShockGamepad.current
            // ========================================
            if (!found)
            {
                Debug.Log("\n[방법 2] DualShockGamepad.current 검색...");
                var ds4 = DualShockGamepad.current;
                if (ds4 != null)
                {
                    currentGamepad = ds4;
                    dualSense = ds4 as DualSenseGamepadHID;
                    customDualSense = ds4 as DualSenseWithTouchpad;
                    
                    Debug.Log($"<color=green>✓ DualShockGamepad 발견!</color>");
                    Debug.Log($"  이름: {ds4.name}");
                    Debug.Log($"  실제 타입: {ds4.GetType().Name}");
                    found = true;
                }
                else
                {
                    Debug.Log("<color=yellow>  ✗ DualShockGamepad.current = null</color>");
                }
            }
            
            // ========================================
            // 방법 3: Gamepad.current (가장 범용적)
            // ========================================
            if (!found)
            {
                Debug.Log("\n[방법 3] Gamepad.current 검색...");
                var gp = Gamepad.current;
                if (gp != null)
                {
                    currentGamepad = gp;
                    dualSense = gp as DualSenseGamepadHID;
                    customDualSense = gp as DualSenseWithTouchpad;
                    
                    Debug.Log($"<color=green>✓ Gamepad 발견!</color>");
                    Debug.Log($"  이름: {gp.name}");
                    Debug.Log($"  실제 타입: {gp.GetType().Name}");
                    Debug.Log($"  DualSense 타입: {(dualSense != null ? "예" : "아니오")}");
                    found = true;
                }
                else
                {
                    Debug.Log("<color=yellow>  ✗ Gamepad.current = null</color>");
                }
            }
            
            // ========================================
            // 방법 4: 모든 InputDevice 순회 (최종 수단)
            // ========================================
            if (!found)
            {
                Debug.Log("\n[방법 4] 전체 디바이스 순회 검색...");
                foreach (var device in InputSystem.devices)
                {
                    Debug.Log($"  검색 중: {device.name} ({device.GetType().Name})");
                    
                    // Gamepad 타입인지 확인
                    if (device is Gamepad gp)
                    {
                        currentGamepad = gp;
                        dualSense = gp as DualSenseGamepadHID;
                        customDualSense = gp as DualSenseWithTouchpad;
                        
                        Debug.Log($"<color=green>✓ Gamepad 발견 (순회)!</color>");
                        Debug.Log($"  이름: {gp.name}");
                        Debug.Log($"  타입: {gp.GetType().Name}");
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    Debug.Log("<color=yellow>  ✗ Gamepad 타입 디바이스 없음</color>");
                }
            }
            
            // ========================================
            // 결과 요약
            // ========================================
            Debug.Log("\n=== 검색 결과 ===");
            if (found)
            {
                Debug.Log($"<color=cyan>✓✓✓ 컨트롤러 연결 성공! ✓✓✓</color>");
                Debug.Log($"  디바이스 이름: {currentGamepad.name}");
                Debug.Log($"  디바이스 타입: {currentGamepad.GetType().Name}");
                Debug.Log($"  DualSense 기능: {(dualSense != null ? "사용 가능" : "제한됨")}");
                Debug.Log($"  터치패드 버튼: {(dualSense != null ? dualSense.touchpadButton.name : "없음")}");
                Debug.Log($"  커스텀 기능: {(customDualSense != null ? "활성화" : "비활성화")}");
            }
            else
            {
                Debug.LogError("<color=red>❌ 컨트롤러를 찾을 수 없습니다!</color>");
                Debug.LogError("\n해결 방법:");
                Debug.LogError("  1. 컨트롤러가 USB/Bluetooth로 연결되어 있는지 확인");
                Debug.LogError("  2. DS4Windows, Steam 등 다른 프로그램 종료");
                Debug.LogError("  3. Unity 재시작 후 다시 시도");
                Debug.LogError("  4. Window > Analysis > Input Debugger 확인");
                Debug.LogError("  5. F12 키로 InputDeviceDebugWindow 열기");
            }
            Debug.Log("====================================\n");
            
            isSearching = false;
        }
        
        /// <summary>
        /// 모든 입력 디바이스 로그 출력
        /// </summary>
        private void LogAllDevices()
        {
            Debug.Log("--- 현재 연결된 모든 디바이스 ---");
            var devices = InputSystem.devices;
            
            if (devices.Count == 0)
            {
                Debug.Log("  (연결된 디바이스 없음)");
            }
            else
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    var device = devices[i];
                    string typeInfo = device.GetType().Name;
                    
                    if (device is Gamepad)
                    {
                        typeInfo += " [Gamepad]";
                    }
                    
                    Debug.Log($"  {i + 1}. {device.name} ({typeInfo})");
                }
            }
        }
        
        /// <summary>
        /// 터치패드 테스트
        /// </summary>
        private void TestTouchpad()
        {
            if (dualSense == null) return;
            
            // 주기적 상태 로그
            if (Time.time - lastLogTime > logInterval)
            {
                bool touchpadPressed = dualSense.touchpadButton.isPressed;
                
                if (touchpadPressed)
                {
                    Debug.Log($"[터치패드] 버튼 눌림 중 (시간: {Time.time:F2})");
                }
                
                if (customDualSense != null)
                {
                    bool touch1 = customDualSense.Touch1Active;
                    if (touch1)
                    {
                        Vector2 pos = customDualSense.TouchPosition1;
                        Debug.Log($"[커스텀] Touch 1 활성: ({pos.x:F3}, {pos.y:F3})");
                    }
                }
                
                lastLogTime = Time.time;
            }
            
            // 터치패드 버튼 이벤트
            if (dualSense.touchpadButton.wasPressedThisFrame)
            {
                Debug.Log("<color=yellow>● 터치패드 버튼 눌림!</color>");
            }
            
            if (dualSense.touchpadButton.wasReleasedThisFrame)
            {
                Debug.Log("<color=yellow>○ 터치패드 버튼 떼짐!</color>");
            }
        }
        
        /// <summary>
        /// 기본 버튼 테스트
        /// </summary>
        private void TestButtons()
        {
            if (currentGamepad == null) return;
            
            // F1: 재검색
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                Debug.Log("\n=== [F1] 수동 재검색 ===");
                currentGamepad = null;
                dualSense = null;
                customDualSense = null;
                FindDualSense();
            }
            
            // F2: 디바이스 목록
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                Debug.Log("\n=== [F2] 디바이스 목록 ===");
                LogAllDevices();
            }
            
            // F3: 상세 정보
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                Debug.Log("\n=== [F3] 컨트롤러 상세 정보 ===");
                if (currentGamepad != null)
                {
                    Debug.Log($"이름: {currentGamepad.name}");
                    Debug.Log($"타입: {currentGamepad.GetType().FullName}");
                    Debug.Log($"레이아웃: {currentGamepad.layout}");
                    Debug.Log($"설명: {currentGamepad.description.ToJson()}");
                }
                else
                {
                    Debug.Log("연결된 컨트롤러 없음");
                }
            }
            
            // 게임패드 버튼 테스트
            if (currentGamepad.buttonSouth.wasPressedThisFrame)
            {
                Debug.Log("✕ (Cross) 버튼 눌림");
            }
        }
        
        void OnGUI()
        {
            if (!enableTest) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 500, 350));
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.fontSize = 14;
            
            GUILayout.Box("=== DualSense 터치패드 테스트 ===", boxStyle);
            
            if (currentGamepad == null)
            {
                GUILayout.Label("<color=red>❌ 컨트롤러를 찾을 수 없습니다</color>");
                GUILayout.Label("");
                GUILayout.Label("확인 사항:");
                GUILayout.Label("• USB/Bluetooth 연결 확인");
                GUILayout.Label("• DS4Windows 종료");
                GUILayout.Label("• Steam 종료");
                GUILayout.Label("");
                if (GUILayout.Button("F1: 재검색"))
                {
                    FindDualSense();
                }
            }
            else
            {
                GUILayout.Label($"<color=green>✓ 연결됨</color>");
                GUILayout.Label($"이름: {currentGamepad.name}");
                GUILayout.Label($"타입: {currentGamepad.GetType().Name}");
                GUILayout.Label("");
                
                if (dualSense != null)
                {
                    GUILayout.Label($"<color=cyan>DualSense 기능: 사용 가능</color>");
                    GUILayout.Label($"터치패드 버튼: {(dualSense.touchpadButton.isPressed ? "눌림" : "안 눌림")}");
                    
                    if (customDualSense != null)
                    {
                        GUILayout.Label($"커스텀 기능: 활성화");
                        GUILayout.Label($"Touch 1: {(customDualSense.Touch1Active ? "활성" : "비활성")}");
                    }
                }
                else
                {
                    GUILayout.Label($"<color=yellow>⚠ 일반 Gamepad 모드</color>");
                    GUILayout.Label($"터치패드 기능 제한됨");
                }
                
                GUILayout.Label("");
                GUILayout.Label("단축키:");
                GUILayout.Label("F1: 재검색 | F2: 디바이스 목록");
                GUILayout.Label("F3: 상세 정보 | F12: 디버그 창");
            }
            
            GUILayout.EndArea();
        }
    }
}
