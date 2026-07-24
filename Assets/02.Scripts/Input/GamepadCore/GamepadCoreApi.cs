using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GamepadCore.Unity
{
    [Flags]
    public enum GamepadButtons : uint
    {
        Cross = 1u << 0,
        Square = 1u << 1,
        Triangle = 1u << 2,
        Circle = 1u << 3,
        DpadUp = 1u << 4,
        DpadDown = 1u << 5,
        DpadLeft = 1u << 6,
        DpadRight = 1u << 7,
        L1 = 1u << 8,
        R1 = 1u << 9,
        L2 = 1u << 10,
        R2 = 1u << 11,
        L3 = 1u << 12,
        R3 = 1u << 13,
        PS = 1u << 14,
        Share = 1u << 15,
        Options = 1u << 16,
        Touchpad = 1u << 17,
        Mute = 1u << 18,
        Fn1 = 1u << 19,
        Fn2 = 1u << 20,
        PaddleLeft = 1u << 21,
        PaddleRight = 1u << 22
    }

    public enum GamepadHand : byte
    {
        Left = 0,
        Right = 1,
        Both = 2
    }

    public enum GamepadDeviceType
    {
        DualSense = 0,
        DualSenseEdge = 1,
        DualShock4 = 2,
        NotFound = 3
    }

    public readonly struct GamepadState
    {
        public readonly Vector2 LeftStick;
        public readonly Vector2 RightStick;
        public readonly float LeftTrigger;
        public readonly float RightTrigger;
        public readonly Vector3 Gyroscope;
        public readonly Vector3 Accelerometer;
        public readonly Vector3 Gravity;
        public readonly Vector3 Tilt;
        public readonly Vector2 TouchPosition;
        public readonly Vector2 TouchRelative;
        public readonly int TouchId;
        public readonly int TouchFingerCount;
        public readonly GamepadButtons Buttons;
        public readonly float BatteryLevel;
        public readonly bool IsTouching;

        internal GamepadState(NativeInputState state)
        {
            LeftStick = state.leftStick.ToVector2();
            RightStick = state.rightStick.ToVector2();
            LeftTrigger = state.leftTrigger;
            RightTrigger = state.rightTrigger;
            Gyroscope = state.gyroscope.ToVector3();
            Accelerometer = state.accelerometer.ToVector3();
            Gravity = state.gravity.ToVector3();
            Tilt = state.tilt.ToVector3();
            TouchPosition = state.touchPosition.ToVector2();
            TouchRelative = state.touchRelative.ToVector2();
            TouchId = state.touchId;
            TouchFingerCount = state.touchFingerCount;
            Buttons = (GamepadButtons)state.buttons;
            BatteryLevel = state.batteryLevel;
            IsTouching = state.isTouching != 0;
        }

        public bool IsPressed(GamepadButtons button)
        {
            return (Buttons & button) == button;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeVector2
    {
        public float x;
        public float y;
        public Vector2 ToVector2() => new Vector2(x, y);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeVector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeInputState
    {
        public NativeVector2 leftStick;
        public NativeVector2 rightStick;
        public float leftTrigger;
        public float rightTrigger;
        public NativeVector3 gyroscope;
        public NativeVector3 accelerometer;
        public NativeVector3 gravity;
        public NativeVector3 tilt;
        public NativeVector2 touchPosition;
        public NativeVector2 touchRelative;
        public int touchId;
        public int touchFingerCount;
        public uint buttons;
        public float batteryLevel;
        public byte isTouching;
        public byte reserved0;
        public byte reserved1;
        public byte reserved2;
    }

    internal static class Native
    {
        private const string Library = "GamepadCore";
        private const CallingConvention Convention = CallingConvention.Cdecl;

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_Initialize();

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_Shutdown();

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_Update(float deltaTime);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_UpdateInput(int index, float deltaTime);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern int GC_GetConnectedGamepadCount();

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern int GC_GetDeviceType(int index);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern float GC_GetBatteryLevel(int index);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_IsGamepadConnected(int index);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_IsGamepadWireless(int index);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_GetInputState(
            int index, out NativeInputState state);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetVibration(int index, byte left, byte right);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_DualSenseSettings(
            int index,
            byte micEnabled,
            byte headsetEnabled,
            byte speakerEnabled,
            byte micVolume,
            byte audioVolume,
            byte rumbleMode,
            byte rumbleReduction,
            byte triggerReduction);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_UpdateOutput(int index);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetLightbar(
            int index, byte red, byte green, byte blue);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetLightbarFlash(
            int index, byte red, byte green, byte blue,
            float brightTime, float darkTime);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_ResetLights(int index);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetPlayerLed(
            int index, byte ledMask, byte brightness);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetMicrophoneLed(int index, byte mode);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_EnableTouch(
            int index, [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_EnableGesture(
            int index, [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_EnableMotionSensor(
            int index, [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_ResetGyroOrientation(int index);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_ResetGamepad(int index);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_ResetAllGamepads();

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_StopTrigger(int index, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerResistance(
            int index, byte startZone, byte strength, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_SetTriggerCustom(
            int index, byte hand, [In] byte[] bytes, int byteCount);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerGameCube(int index, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerBow(
            int index, byte startZone, byte snapBack, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerGalloping(
            int index, byte startPosition, byte endPosition,
            byte firstFoot, byte secondFoot, byte frequency, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerWeapon(
            int index, byte startZone, byte amplitude,
            byte behavior, byte trigger, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerMachineGun(
            int index, byte startZone, byte behavior,
            byte amplitude, byte frequency, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_SetTriggerMachine(
            int index, byte startZone, byte behaviorFlag,
            byte force, byte amplitude, byte period, byte frequency, byte hand);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern int GC_TriggerAudioHaptics(
            int index, [In] short[] samples, int sampleCount);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_SendAudioHapticUSB(
            int index, [In] short[] samples, int sampleCount);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_SendAudioHapticBT(
            int index, [In] byte[] packet);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_ProcessAudioStream(
            int index, [In] float[] samples, int frameCount, int channels);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern void GC_StopAudioHaptics(int index);

        [DllImport(Library, CallingConvention = Convention)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GC_IsAudioHapticsProcessing(int index);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern int GC_GetQueuedHapticCount(int index);

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern IntPtr GC_GetVersionString();

        [DllImport(Library, CallingConvention = Convention)]
        internal static extern IntPtr GC_GetLastError();
    }

    public static class GamepadCoreApi
    {
        public const int MaxGamepads = 4;
        public static bool IsInitialized { get; private set; }

        public static string Version =>
            Marshal.PtrToStringAnsi(Native.GC_GetVersionString()) ?? string.Empty;

        public static string LastError =>
            Marshal.PtrToStringAnsi(Native.GC_GetLastError()) ?? string.Empty;

        public static bool Initialize()
        {
            if (IsInitialized) return true;
            IsInitialized = Native.GC_Initialize();
            return IsInitialized;
        }

        public static void Shutdown()
        {
            if (!IsInitialized) return;
            Native.GC_Shutdown();
            IsInitialized = false;
        }

        public static void Update(float deltaTime)
        {
            if (!IsInitialized) return;
            Native.GC_Update(deltaTime);
            for (int index = 0; index < MaxGamepads; ++index)
            {
                if (Native.GC_IsGamepadConnected(index))
                    Native.GC_UpdateInput(index, deltaTime);
            }
        }

        public static int ConnectedCount => Native.GC_GetConnectedGamepadCount();
        public static bool IsConnected(int index) =>
            Native.GC_IsGamepadConnected(index);
        public static bool IsWireless(int index) =>
            Native.GC_IsGamepadWireless(index);

        public static GamepadDeviceType GetDeviceType(int index) =>
            (GamepadDeviceType)Native.GC_GetDeviceType(index);

        public static float GetBatteryLevel(int index)
        {
            float value = Native.GC_GetBatteryLevel(index);
            return value > 1f ? value / 100f : value;
        }

        public static bool TryGetState(int index, out GamepadState state)
        {
            if (Native.GC_GetInputState(index, out NativeInputState native))
            {
                state = new GamepadState(native);
                return true;
            }
            state = default;
            return false;
        }

        public static void SetVibration(int index, float left, float right)
        {
            Native.GC_SetVibration(index, ToByte(left), ToByte(right));
        }

        public static void ConfigureDualSense(
            int index,
            bool microphone,
            bool headset,
            bool speaker,
            byte microphoneVolume,
            byte audioVolume,
            byte rumbleMode = 0xFF,
            byte rumbleReduction = 0,
            byte triggerReduction = 0)
        {
            Native.GC_DualSenseSettings(
                index,
                microphone ? (byte)1 : (byte)0,
                headset ? (byte)1 : (byte)0,
                speaker ? (byte)1 : (byte)0,
                microphoneVolume,
                audioVolume,
                rumbleMode,
                rumbleReduction,
                triggerReduction);
            Native.GC_UpdateOutput(index);
        }

        public static void SetLightbar(int index, Color color)
        {
            Native.GC_SetLightbar(
                index, ToByte(color.r), ToByte(color.g), ToByte(color.b));
        }

        public static void SetLightbarFlash(
            int index, Color color, float brightSeconds, float darkSeconds)
        {
            Native.GC_SetLightbarFlash(
                index, ToByte(color.r), ToByte(color.g), ToByte(color.b),
                brightSeconds, darkSeconds);
        }

        public static void ResetLights(int index) => Native.GC_ResetLights(index);
        public static void SetPlayerLed(int index, byte mask, byte brightness) =>
            Native.GC_SetPlayerLed(index, mask, brightness);
        public static void SetMicrophoneLed(int index, bool enabled) =>
            Native.GC_SetMicrophoneLed(index, enabled ? (byte)1 : (byte)0);

        public static void EnableTouch(int index, bool enabled, bool gesture = false)
        {
            Native.GC_EnableTouch(index, enabled);
            Native.GC_EnableGesture(index, gesture);
        }

        public static void EnableMotion(int index, bool enabled) =>
            Native.GC_EnableMotionSensor(index, enabled);
        public static void ResetMotionOrientation(int index) =>
            Native.GC_ResetGyroOrientation(index);
        public static bool ResetGamepad(int index) => Native.GC_ResetGamepad(index);
        public static void ResetAllGamepads() => Native.GC_ResetAllGamepads();

        public static void StopTrigger(int index, GamepadHand hand)
        {
            if (hand == GamepadHand.Both)
            {
                Native.GC_StopTrigger(index, (byte)GamepadHand.Left);
                Native.GC_StopTrigger(index, (byte)GamepadHand.Right);
                return;
            }
            Native.GC_StopTrigger(index, (byte)hand);
        }

        public static void SetTriggerResistance(
            int index, byte startZone, byte strength, GamepadHand hand)
        {
            if (hand == GamepadHand.Both)
            {
                Native.GC_SetTriggerResistance(
                    index, startZone, strength, (byte)GamepadHand.Left);
                Native.GC_SetTriggerResistance(
                    index, startZone, strength, (byte)GamepadHand.Right);
                return;
            }
            Native.GC_SetTriggerResistance(index, startZone, strength, (byte)hand);
        }

        public static bool SetTriggerCustom(
            int index, GamepadHand hand, byte[] tenByteReport)
        {
            if (tenByteReport == null || tenByteReport.Length != 10)
                throw new ArgumentException(
                    "Adaptive trigger reports must contain exactly 10 bytes.",
                    nameof(tenByteReport));
            return Native.GC_SetTriggerCustom(
                index, (byte)hand, tenByteReport, tenByteReport.Length);
        }

        public static void SetTriggerGameCube(int index, GamepadHand hand) =>
            Native.GC_SetTriggerGameCube(index, (byte)hand);

        public static void SetTriggerBow(
            int index, byte startZone, byte snapBack, GamepadHand hand) =>
            Native.GC_SetTriggerBow(index, startZone, snapBack, (byte)hand);

        public static void SetTriggerGalloping(
            int index, byte startPosition, byte endPosition,
            byte firstFoot, byte secondFoot, byte frequency, GamepadHand hand) =>
            Native.GC_SetTriggerGalloping(
                index, startPosition, endPosition, firstFoot, secondFoot,
                frequency, (byte)hand);

        public static void SetTriggerWeapon(
            int index, byte startZone, byte amplitude,
            byte behavior, byte trigger, GamepadHand hand) =>
            Native.GC_SetTriggerWeapon(
                index, startZone, amplitude, behavior, trigger, (byte)hand);

        public static void SetTriggerMachineGun(
            int index, byte startZone, byte behavior,
            byte amplitude, byte frequency, GamepadHand hand) =>
            Native.GC_SetTriggerMachineGun(
                index, startZone, behavior, amplitude, frequency, (byte)hand);

        public static void SetTriggerMachine(
            int index, byte startZone, byte behaviorFlag,
            byte force, byte amplitude, byte period,
            byte frequency, GamepadHand hand) =>
            Native.GC_SetTriggerMachine(
                index, startZone, behaviorFlag, force, amplitude,
                period, frequency, (byte)hand);

        // DualSense USB PCM must be 48 kHz, signed 16-bit, interleaved stereo.
        public static int PlayPcm16(int index, short[] interleavedStereo)
        {
            if (interleavedStereo == null) throw new ArgumentNullException(
                nameof(interleavedStereo));
            return Native.GC_TriggerAudioHaptics(
                index, interleavedStereo, interleavedStereo.Length);
        }

        public static bool QueueUsbPcm16(int index, short[] interleavedStereo)
        {
            if (interleavedStereo == null) throw new ArgumentNullException(
                nameof(interleavedStereo));
            return Native.GC_SendAudioHapticUSB(
                index, interleavedStereo, interleavedStereo.Length);
        }

        // Bluetooth packets use the controller's 64-byte encoded haptic format.
        public static bool QueueBluetoothHapticPacket(int index, byte[] packet)
        {
            if (packet == null || packet.Length != 64)
                throw new ArgumentException(
                    "Bluetooth haptic packets must contain exactly 64 bytes.",
                    nameof(packet));
            return Native.GC_SendAudioHapticBT(index, packet);
        }

        public static void ProcessUnityAudio(
            int index, float[] interleavedStereo, int frameCount)
        {
            if (interleavedStereo == null) throw new ArgumentNullException(
                nameof(interleavedStereo));
            if (interleavedStereo.Length < frameCount * 2)
                throw new ArgumentException(
                    "The buffer does not contain frameCount stereo frames.",
                    nameof(interleavedStereo));
            Native.GC_ProcessAudioStream(
                index, interleavedStereo, frameCount, 2);
        }

        public static void StopAudioHaptics(int index) =>
            Native.GC_StopAudioHaptics(index);
        public static bool IsAudioHapticsProcessing(int index) =>
            Native.GC_IsAudioHapticsProcessing(index);
        public static int QueuedHapticCount(int index) =>
            Native.GC_GetQueuedHapticCount(index);

        private static byte ToByte(float normalized)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(normalized) * 255f);
        }
    }

    [DefaultExecutionOrder(-1000)]
    public sealed class GamepadCoreBehaviour : MonoBehaviour
    {
        private void Awake()
        {
            if (!GamepadCoreApi.Initialize())
                Debug.LogError("GamepadCore initialization failed: " +
                               GamepadCoreApi.LastError);
        }

        private void Update()
        {
            GamepadCoreApi.Update(Time.unscaledDeltaTime);
        }

        private void OnApplicationQuit()
        {
            GamepadCoreApi.Shutdown();
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) GamepadCoreApi.Shutdown();
        }
    }
}
