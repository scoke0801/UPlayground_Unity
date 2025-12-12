using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using System.Collections.Generic;
using System.Text;

namespace Game.Input
{
    /// <summary>
    /// Input System 디바이스 실시간 모니터링 윈도우
    /// 게임 화면에 모든 입력 디바이스 정보를 표시
    /// </summary>
    public class InputDeviceDebugWindow : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private bool showWindow = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private float refreshRate = 0.5f;
        
        private float lastRefresh;
        private string deviceInfo = "";
        private Vector2 scrollPosition;
        
        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                showWindow = !showWindow;
            }
            
            if (Time.time - lastRefresh > refreshRate)
            {
                RefreshDeviceInfo();
                lastRefresh = Time.time;
            }
        }
        
        void RefreshDeviceInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== INPUT SYSTEM 디바이스 정보 ===\n");
            
            var devices = InputSystem.devices;
            sb.AppendLine($"총 {devices.Count}개 디바이스 연결\n");
            
            if (devices.Count == 0)
            {
                sb.AppendLine("❌ 연결된 디바이스 없음");
            }
            else
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    var device = devices[i];
                    sb.AppendLine($"[디바이스 #{i + 1}]");
                    sb.AppendLine($"  이름: {device.name}");
                    sb.AppendLine($"  표시 이름: {device.displayName}");
                    sb.AppendLine($"  타입: {device.GetType().FullName}");
                    sb.AppendLine($"  레이아웃: {device.layout}");
                    sb.AppendLine($"  활성화: {device.enabled}");
                    sb.AppendLine($"  설명: {device.description.ToJson()}");
                    
                    // Gamepad 체크
                    if (device is Gamepad gamepad)
                    {
                        sb.AppendLine($"  ✓ Gamepad 확인됨");
                        
                        if (device is DualShockGamepad)
                        {
                            sb.AppendLine($"  ✓ DualShockGamepad 확인됨");
                            
                            if (device is DualSenseGamepadHID)
                            {
                                sb.AppendLine($"  ✓✓ DualSenseGamepadHID 확인됨!");
                            }
                        }
                    }
                    
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("\n=== 특정 타입 검색 결과 ===");
            sb.AppendLine($"Gamepad.current: {(Gamepad.current != null ? Gamepad.current.name : "null")}");
            sb.AppendLine($"DualShockGamepad.current: {(DualShockGamepad.current != null ? DualShockGamepad.current.name : "null")}");
            sb.AppendLine($"DualSenseGamepadHID.current: {(DualSenseGamepadHID.current != null ? DualSenseGamepadHID.current.name : "null")}");
            
            deviceInfo = sb.ToString();
        }
        
        void OnGUI()
        {
            if (!showWindow) return;
            
            float windowWidth = Screen.width * 0.5f;
            float windowHeight = Screen.height * 0.8f;
            float x = Screen.width - windowWidth - 10;
            float y = 10;
            
            GUILayout.BeginArea(new Rect(x, y, windowWidth, windowHeight));
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.wordWrap = true;
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(windowWidth), GUILayout.Height(windowHeight));
            
            GUILayout.Box(deviceInfo, boxStyle, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button($"디바이스 새로고침 ({toggleKey} = 창 토글)"))
            {
                RefreshDeviceInfo();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        void Start()
        {
            RefreshDeviceInfo();
        }
    }
}
