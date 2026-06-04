// Unity C# Script for Audio Haptics Test
// 사용법: 유니티 프로젝트의 Assets 폴더에 저장 후 빈 GameObject에 추가

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using Random = UnityEngine.Random;

/// <summary>
/// DualSense 오디오 햅틱 테스트 매니저
/// </summary>
public partial class AudioHapticsTest : MonoBehaviour
{
    // ============================================
    // DLL Import
    // ============================================

    [DllImport("GamepadCore")]
    private static extern bool GC_Initialize();

    [DllImport("GamepadCore")]
    private static extern void GC_Shutdown();
    [DllImport("GamepadCore")]
    private static extern void GC_Update(float deltaTime);
    
    [DllImport("GamepadCore")]
    private static extern int GC_GetConnectedGamepadCount();

    [DllImport("GamepadCore")]
    private static extern bool GC_IsGamepadConnected(int index);

    [DllImport("GamepadCore")]
    private static extern bool GC_IsGamepadWireless(int index);
    
    [DllImport("GamepadCore")]
    private static extern int GC_TriggerAudioHaptics(int deviceId, short[] audioData, int dataSize);

    [DllImport("GamepadCore")]
    private static extern bool GC_SendAudioHapticUSB(int index, short[] samples, int sampleCount);

    [DllImport("GamepadCore")]
    private static extern bool GC_SendAudioHapticBT(int index, byte[] packet);

    [DllImport("GamepadCore")]
    private static extern int GC_GetQueuedHapticCount(int index);

    [DllImport("GamepadCore")]
    private static extern void GC_SetVibration(int index, byte leftRumble, byte rightRumble);

    [DllImport("GamepadCore")]
    private static extern void GC_SetLightbar(int index, byte r, byte g, byte b);

    [DllImport("GamepadCore")]
    private static extern float GC_GetBatteryLevel(int index);

    [DllImport("GamepadCore")]
    private static extern int GC_GetDeviceType(int index);

    [DllImport("GamepadCore")]
    private static extern IntPtr GC_GetVersionString();

    [DllImport("GamepadCore")]
    private static extern void GC_DualSenseSettings(int index, byte bIsMic, byte bIsHeadset, byte bIsSpeaker,
                                                     byte micVolume, byte audioVolume, byte rumbleMode,
                                                     byte rumbleReduce, byte triggerReduce);

    [DllImport("GamepadCore")]
    private static extern void GC_SetPlayerLed(int index, byte led, byte brightness);

    [DllImport("GamepadCore")]
    private static extern void GC_StopTrigger(int index, byte hand);

    [DllImport("GamepadCore")]
    private static extern void GC_SetTriggerResistance(int index, byte startZone, byte strength, byte hand);

    [DllImport("GamepadCore")]
    private static extern void GC_EnableTouch(int index, bool enable);

    [DllImport("GamepadCore")]
    private static extern void GC_EnableMotionSensor(int index, bool enable);

    [DllImport("GamepadCore")]
    private static extern void GC_ResetGyroOrientation(int index);

    // ============================================
    // Unity Inspector 설정
    // ============================================

    [Header("게임패드 설정")]
    [Tooltip("사용할 게임패드 인덱스 (0-3)")]
    public int gamepadIndex = 0;

    [Header("오디오 설정")]
    public AudioClip audioClip;

    [Header("테스트 설정")]
    [Tooltip("테스트 진동 활성화")]
    public bool enableTestVibration = false;

    [Header("라이트바 설정")]
    public Color lightbarColor = Color.blue;

    [Header("디버그")]
    public bool showDebugInfo = true;

    // ============================================
    // 내부 변수
    // ============================================

    private bool isInitialized = false;
    private bool isConnected = false;
    private bool isWireless = false;

    // ============================================
    // Unity Lifecycle
    // ============================================

    void Start()
    {
        // GamepadCore 초기화
        if (GC_Initialize())
        {
            isInitialized = true;
            Debug.Log("[AudioHaptics] GamepadCore 초기화 성공");

            // 버전 정보 출력
            IntPtr versionPtr = GC_GetVersionString();
            if (versionPtr != IntPtr.Zero)
            {
                string version = Marshal.PtrToStringAnsi(versionPtr);
                Debug.Log($"[AudioHaptics] {version}");
            }
        }
        else
        {
            Debug.LogError("[AudioHaptics] GamepadCore 초기화 실패!");
            enabled = false;
            return;
        }

        CheckConnection();

        if (InputManager.Instance)
        {
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.L1,
                null,OnPerformedL1, null, null, null, InputLayer.None);
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.R1,
                null,OnPerformedR1, null, null, null, InputLayer.None);
            
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Up,
                null,OnPerformedUp, null, null, null, InputLayer.None);
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Down,
                null,OnPerformedDown, null, null, null, InputLayer.None);
            
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Left,
                null,OnPerformedLeft, null, null, null, InputLayer.None);
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Right,
                null,OnPerformedRight, null, null, null, InputLayer.None);
            
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Select,
                null,OnPerformedSelect, null, null, null, InputLayer.None);
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Start,
                null,OnPerformedStart, null, null, null, InputLayer.None);
            
            InputManager.Instance.RegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Touchpad,
                null,OnPerformedTouchpad, null, null, null, InputLayer.None);
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance)
        {
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.L1,
                null,OnPerformedL1, null);
            
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.R1,
                null,OnPerformedR1, null);
            
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Up,
                null,OnPerformedUp, null);
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Down,
                null,OnPerformedDown, null);
            
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Left,
                null,OnPerformedLeft, null);
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Right,
                null,OnPerformedRight, null);
            
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Select,
                null,OnPerformedSelect, null);
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Start,
                null,OnPerformedStart, null);
            
            InputManager.Instance.UnRegisterInputEvent(InputMapNames.Gamepad, GamepadAction.Touchpad,
                null,OnPerformedTouchpad, null);

        }
    }

    void Update()
    {
        if (!isInitialized) return;

        GC_Update(Time.deltaTime);
        
        // 연결 상태 확인
        CheckConnection();

        if (!isConnected) return;

        // 라이트바 색상 업데이트
        UpdateLightbar();

        // 테스트 진동
        if (enableTestVibration)
        {
            byte vibration = (byte)(Mathf.Sin(Time.time * 2f) * 127 + 128);
            GC_SetVibration(gamepadIndex, vibration, vibration);
        }

        // 디버그 정보 표시
        if (showDebugInfo)
        {
            DisplayDebugInfo();
        }
    }

    void OnDestroy()
    {
        if (isInitialized)
        {
            GC_Shutdown();
            Debug.Log("[AudioHaptics] GamepadCore 종료");
        }
    }

    // ============================================
    // 게임패드 제어
    // ============================================

    /// <summary>
    /// 연결 상태 확인
    /// </summary>
    void CheckConnection()
    {
		int count = GC_GetConnectedGamepadCount();
        isConnected = false;
        isWireless = false;

        if (count <= 0)
        {
            return;
        }
        for (int i = 0; i < 4; ++i)
        {
            isConnected = GC_IsGamepadConnected(i);
            if (isConnected == true)
            {
                gamepadIndex = i;
                isWireless = GC_IsGamepadWireless(i);
                break;
            }
        }
    }

    /// <summary>
    /// 라이트바 색상 업데이트
    /// </summary>
    void UpdateLightbar()
    {
        if (!isConnected) return;

        byte r = (byte)(lightbarColor.r * 255);
        byte g = (byte)(lightbarColor.g * 255);
        byte b = (byte)(lightbarColor.b * 255);

        GC_SetLightbar(gamepadIndex, r, g, b);
    }

    /// <summary>
    /// 디버그 정보 표시
    /// </summary>
    void DisplayDebugInfo()
    {
        int connectedCount = GC_GetConnectedGamepadCount();
        int queuedCount = GC_GetQueuedHapticCount(gamepadIndex);
        float battery = GC_GetBatteryLevel(gamepadIndex);

        Debug.Log($"[AudioHaptics] 연결된 게임패드: {connectedCount} | " +
                  $"큐: {queuedCount} | 배터리: {battery:P0}");
    }

    // ============================================
    // Public API (다른 스크립트에서 호출 가능)
    // ============================================

    /// <summary>
    /// 진동 설정
    /// </summary>
    public void SetVibration(float left, float right)
    {
        if (!isConnected) return;

        byte leftByte = (byte)(Mathf.Clamp01(left) * 255);
        byte rightByte = (byte)(Mathf.Clamp01(right) * 255);

        GC_SetVibration(gamepadIndex, leftByte, rightByte);
    }

    /// <summary>
    /// 라이트바 색상 설정
    /// </summary>
    public void SetLightbar(Color color)
    {
        lightbarColor = color;
        UpdateLightbar();
    }

    /// <summary>
    /// 수동으로 오디오 샘플 전송 (USB)
    /// </summary>
    public void SendAudioSamplesUSB(short[] samples)
    {
        if (!isConnected || isWireless) return;
        GC_SendAudioHapticUSB(gamepadIndex, samples, samples.Length);
    }

    public void PlayHaptics(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClip is null.");

            return;
        }

        if (!GC_IsGamepadConnected(gamepadIndex))
        {
            Debug.LogWarning($"Gamepad {gamepadIndex} is not connected.");
            return;
        }
        
        // Get the raw audio data from the clip as floats
        float[] floatData = new float[clip.samples * clip.channels];
        clip.GetData(floatData, 0);
        
        // Convert the float data to 16-bit PCM (short array)
        // This is a common format for audio haptics on devices like the DualSense.
        short[] shortData = new short[floatData.Length];
        for (int i = 0; i < floatData.Length; i++)
        {
            // The float data is from -1.0 to 1.0. We scale it to the short range.
            shortData[i] = (short)(floatData[i] * short.MaxValue);
        }

        // Send the data to the native library.
        GC_TriggerAudioHaptics(gamepadIndex, shortData, shortData.Length);
    }

    /// <summary>
    /// 수동으로 햅틱 패킷 전송 (Bluetooth)
    /// </summary>
    public void SendAudioPacketBT(byte[] packet)
    {
        if (!isConnected || !isWireless || packet.Length != 64) return;
        GC_SendAudioHapticBT(gamepadIndex, packet);
    }

    /// <summary>
    /// DualSense 고급 설정
    /// </summary>
    public void ConfigureDualSense(
        bool enableMic = false,
        bool enableHeadset = false,
        bool enableSpeaker = false,
        float micVolume = 0.0f,
        float audioVolume = 1.0f,
        byte rumbleMode = 0xFC) // Haptics mode
    {
        if (!isConnected) return;

        byte mic = (byte)(enableMic ? 1 : 0);
        byte headset = (byte)(enableHeadset ? 1 : 0);
        byte speaker = (byte)(enableSpeaker ? 1 : 0);
        byte micVol = (byte)(Mathf.Clamp01(micVolume) * 255);
        byte audioVol = (byte)(Mathf.Clamp01(audioVolume) * 255);

        GC_DualSenseSettings(gamepadIndex, mic, headset, speaker, 
                            micVol, audioVol, rumbleMode, 0, 0);
    }

    /// <summary>
    /// 플레이어 LED 설정 (1-5번 LED)
    /// </summary>
    public void SetPlayerLED(int playerNumber, float brightness = 1.0f)
    {
        if (!isConnected || playerNumber < 1 || playerNumber > 5) return;

        // LED 비트 패턴: 0x01=1번, 0x02=2번, 0x04=3번, 0x08=4번, 0x10=5번
        byte ledPattern = (byte)(1 << (playerNumber - 1));
        byte bright = (byte)(Mathf.Clamp01(brightness) * 255);

        GC_SetPlayerLed(gamepadIndex, ledPattern, bright);
    }

    /// <summary>
    /// 어댑티브 트리거 중지
    /// </summary>
    public void StopTrigger(bool leftTrigger = true, bool rightTrigger = true)
    {
        if (!isConnected) return;

        if (leftTrigger)
            GC_StopTrigger(gamepadIndex, 0);
        if (rightTrigger)
            GC_StopTrigger(gamepadIndex, 1);
    }

    /// <summary>
    /// 트리거 저항 효과 설정
    /// </summary>
    public void SetTriggerResistance(bool isLeftTrigger, float startPosition, float strength)
    {
        if (!isConnected) return;

        byte hand = (byte)(isLeftTrigger ? 0 : 1);
        byte start = (byte)(Mathf.Clamp01(startPosition) * 255);
        byte str = (byte)(Mathf.Clamp01(strength) * 255);

        GC_SetTriggerResistance(gamepadIndex, start, str, hand);
    }

    /// <summary>
    /// 터치패드 활성화/비활성화
    /// </summary>
    public void EnableTouchpad(bool enable)
    {
        if (!isConnected) return;
        GC_EnableTouch(gamepadIndex, enable);
    }

    /// <summary>
    /// 모션 센서 활성화/비활성화
    /// </summary>
    public void EnableMotion(bool enable)
    {
        if (!isConnected) return;
        GC_EnableMotionSensor(gamepadIndex, enable);
    }

    /// <summary>
    /// 자이로스코프 방향 리셋
    /// </summary>
    public void ResetGyroscope()
    {
        if (!isConnected) return;
        GC_ResetGyroOrientation(gamepadIndex);
    }

    /// <summary>
    /// 게임패드 타입 확인
    /// </summary>
    public string GetDeviceTypeName()
    {
        if (!isConnected) return "Not Connected";

        int type = GC_GetDeviceType(gamepadIndex);
        return type switch
        {
            1 => "DualShock 4",
            2 => "DualSense",
            _ => "Unknown"
        };
    }

    // ============================================
    // Unity Editor GUI (Inspector)
    // ============================================

    void OnGUI()
    {
        if (!showDebugInfo) return;

        int connectedCount = GC_GetConnectedGamepadCount();
        int queuedCount = isConnected ? GC_GetQueuedHapticCount(gamepadIndex) : 0;
        float battery = isConnected ? GC_GetBatteryLevel(gamepadIndex) : 0f;
        string connectionType = isWireless ? "Bluetooth" : "USB";
        string deviceType = GetDeviceTypeName();

        GUILayout.BeginArea(new Rect(10, 10, 350, 250));
        GUILayout.Box("DualSense Audio Haptics Test");
        GUILayout.Label($"연결된 게임패드: {connectedCount}");
        
        if (isConnected)
        {
            GUILayout.Label($"인덱스: {gamepadIndex}");
            GUILayout.Label($"장치 타입: {deviceType}");
            GUILayout.Label($"연결 타입: {connectionType}");
            GUILayout.Label($"햅틱 큐: {queuedCount}");
            GUILayout.Label($"배터리: {battery:P0}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("트리거 리셋"))
            {
                StopTrigger(true, true);
            }
            
            if (GUILayout.Button("자이로 리셋"))
            {
                ResetGyroscope();
            }
        }
        else
        {
            GUILayout.Label("게임패드를 연결해주세요...");
        }
        
        GUILayout.EndArea();
    }
}


public partial class AudioHapticsTest : MonoBehaviour
{
    private void OnPerformedL1(InputAction.CallbackContext obj)
    {
        SetVibration(0.1f,0.0f);
    }

    private void OnPerformedR1(InputAction.CallbackContext obj)
    {
        SetVibration(0.0f,0.1f);
    }

    private void OnPerformedTouchpad(InputAction.CallbackContext obj)
    {
        SetVibration(0.0f, 0.0f);
        StopTrigger();
    }

    private void OnPerformedStart(InputAction.CallbackContext obj)
    {
        PlayHaptics(audioClip);
    }

    private void OnPerformedSelect(InputAction.CallbackContext obj)
    {
    }

    private void OnPerformedRight(InputAction.CallbackContext obj)
    {        
        SetTriggerResistance(false, 0.5f, 0.5f);
    }

    private void OnPerformedLeft(InputAction.CallbackContext obj)
    {
        SetTriggerResistance(true, 0.5f, 0.5f);
    }

    private void OnPerformedDown(InputAction.CallbackContext obj)
    {
        SetLightbar(Color.black);
    }

    private void OnPerformedUp(InputAction.CallbackContext obj)
    {
        Color randomColor = Random.ColorHSV(
            0f, 1f,   // Hue
            0.6f, 1f, // Saturation
            0.7f, 1f  // Value (밝기)
        );
        SetLightbar(randomColor);
    }
}
