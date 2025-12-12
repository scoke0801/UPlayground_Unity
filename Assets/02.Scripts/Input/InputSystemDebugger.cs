using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Game.Input
{
    /// <summary>
    /// Input System 디바이스 디버거
    /// 연결된 모든 입력 디바이스 정보를 출력합니다
    /// </summary>
    public class InputSystemDebugger : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private bool logOnStart = true;
        [SerializeField] private bool logEveryFrame = false;
        [SerializeField] private KeyCode refreshKey = KeyCode.F1;
        
        private void Start()
        {
            if (logOnStart)
            {
                LogAllDevices();
            }
        }
        
        private void Update()
        {
            if (logEveryFrame && Time.frameCount % 60 == 0)
            {
                LogAllDevices();
            }
            
            if (UnityEngine.Input.GetKeyDown(refreshKey))
            {
                Debug.Log("=== 수동 디바이스 검색 ===");
                LogAllDevices();
            }
        }
        
        [ContextMenu("Log All Devices")]
        public void LogAllDevices()
        {
            Debug.Log("========================================");
            Debug.Log("=== Input System 디바이스 목록 ===");
            Debug.Log("========================================");
            
            var devices = InputSystem.devices;
            Debug.Log($"총 {devices.Count}개의 디바이스 발견\n");
            
            if (devices.Count == 0)
            {
                Debug.LogWarning("연결된 디바이스가 없습니다!");
                return;
            }
            
            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                LogDeviceInfo(i + 1, device);
            }
            
            Debug.Log("========================================");
            
            // DualSense 전용 검색
            FindDualSenseDevices();
        }
        
        private void LogDeviceInfo(int index, InputDevice device)
        {
            Debug.Log($"[디바이스 #{index}]");
            Debug.Log($"  이름: {device.name}");
            Debug.Log($"  표시 이름: {device.displayName}");
            Debug.Log($"  타입: {device.GetType().Name}");
            Debug.Log($"  설명: {device.description}");
            Debug.Log($"  레이아웃: {device.layout}");
            Debug.Log($"  활성화: {device.enabled}");
            
            // Gamepad 타입 체크
            if (device is Gamepad gamepad)
            {
                Debug.Log($"  → Gamepad 타입 확인됨");
                
                // DualShock 계열 체크
                if (device is DualShockGamepad dualShock)
                {
                    Debug.Log($"  → DualShockGamepad 확인됨");
                    
                    if (device is DualSenseGamepadHID dualSense)
                    {
                        Debug.Log($"  → <color=green>DualSenseGamepadHID 확인됨!</color>");
                    }
                    
                    if (device is DualSenseWithTouchpad touchpad)
                    {
                        Debug.Log($"  → <color=cyan>DualSenseWithTouchpad 확인됨!</color>");
                    }
                }
            }
            
            Debug.Log("");
        }
        
        private void FindDualSenseDevices()
        {
            Debug.Log("=== DualSense 전용 검색 ===");
            
            // 방법 1: DualSenseWithTouchpad
            var customDualSense = DualSenseWithTouchpad.Current;
            if (customDualSense != null)
            {
                Debug.Log($"<color=cyan>✓ DualSenseWithTouchpad 발견: {customDualSense.name}</color>");
            }
            else
            {
                Debug.Log("<color=yellow>✗ DualSenseWithTouchpad 없음</color>");
            }
            
            // 방법 2: DualSenseGamepadHID
            var standardDualSense = DualSenseGamepadHID.current;
            if (standardDualSense != null)
            {
                Debug.Log($"<color=green>✓ DualSenseGamepadHID 발견: {standardDualSense.name}</color>");
            }
            else
            {
                Debug.Log("<color=yellow>✗ DualSenseGamepadHID 없음</color>");
            }
            
            // 방법 3: DualShockGamepad
            var dualShock = DualShockGamepad.current;
            if (dualShock != null)
            {
                Debug.Log($"<color=green>✓ DualShockGamepad 발견: {dualShock.name}</color>");
            }
            else
            {
                Debug.Log("<color=yellow>✗ DualShockGamepad 없음</color>");
            }
            
            // 방법 4: 일반 Gamepad
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Debug.Log($"<color=green>✓ Gamepad 발견: {gamepad.name}</color>");
                Debug.Log($"  실제 타입: {gamepad.GetType().Name}");
            }
            else
            {
                Debug.Log("<color=yellow>✗ Gamepad 없음</color>");
            }
            
            Debug.Log("");
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 100));
            GUILayout.Box("=== Input Debugger ===");
            
            if (GUILayout.Button($"디바이스 검색 ({refreshKey})"))
            {
                LogAllDevices();
            }
            
            var dualSense = DualSenseWithTouchpad.Current;
            if (dualSense != null)
            {
                GUILayout.Label($"<color=green>DualSense: 연결됨</color>");
            }
            else
            {
                GUILayout.Label($"<color=red>DualSense: 미연결</color>");
            }
            
            GUILayout.EndArea();
        }
    }
}
