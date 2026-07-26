using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 입력 시스템 관리 매니저 - 활성 디바이스(키보드+마우스 ↔ 게임패드) 및 게임패드 브랜드 감지
    /// </summary>
    public partial class InputManager : BaseManager<InputManager>, IManager
    {
        private ActiveInputDevice _activeDevice = ActiveInputDevice.KeyboardMouse;
        private GamepadBrand _gamepadBrand = GamepadBrand.Generic;

        /// <summary>현재 플레이어가 마지막으로 사용한 입력 디바이스 분류.</summary>
        public ActiveInputDevice ActiveDevice => _activeDevice;

        /// <summary>마지막으로 사용한 게임패드의 브랜드. 키보드/마우스 사용 중에도 직전 값을 유지한다.</summary>
        public GamepadBrand GamepadBrand => _gamepadBrand;

        /// <summary>
        /// 활성 디바이스 또는 게임패드 브랜드가 바뀌면 발화. 키 프롬프트 UI 등이 구독하는 단일 소스.
        /// </summary>
        public event Action<ActiveInputDevice> OnActiveDeviceChanged;

        // 스틱 드리프트·마우스 미세 이동·진동 노이즈로 디바이스 상태가 떨리는 것을 막는 액추에이션 임계값.
        // 이 값 이상으로 실제 눌림/이동한 컨트롤이 하나라도 있을 때만 전환한다.
        // 감지 신뢰성의 핵심 노브 — 플레이테스트로 튜닝할 수 있도록 인스펙터에 노출.
        [SerializeField] private float _deviceSwitchActuation = 0.5f;

        private bool _deviceDetectionInitialized;

        private void InitDeviceDetection()
        {
            if (_deviceDetectionInitialized) return;

            InputSystem.onEvent += OnInputSystemEvent;
            InputSystem.onDeviceChange += OnInputDeviceChange;
            _deviceDetectionInitialized = true;
        }

        private void DisposeDeviceDetection()
        {
            if (!_deviceDetectionInitialized) return;

            InputSystem.onEvent -= OnInputSystemEvent;
            InputSystem.onDeviceChange -= OnInputDeviceChange;
            OnActiveDeviceChanged = null;
            _deviceDetectionInitialized = false;
        }

        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            // 장치 구성이 바뀌면 같은 컨트롤 경로라도 사람이 읽는 이름이 달라질 수 있다.
            // 표시 문자열 캐시는 여기서만 비운다(바인딩 변경은 경로만 바꾸므로 무효화 불필요).
            ClearBindingDisplayCache();

            if (device is not Gamepad disconnected
                || _activeDevice != ActiveInputDevice.Gamepad
                || change is not (InputDeviceChange.Disconnected or InputDeviceChange.Removed))
            {
                return;
            }

            Gamepad fallback = null;
            foreach (Gamepad gamepad in Gamepad.all)
            {
                if (gamepad != null && gamepad != disconnected && gamepad.added)
                {
                    fallback = gamepad;
                    break;
                }
            }

            if (fallback != null)
            {
                _gamepadBrand = DetectBrand(fallback);
                OnActiveDeviceChanged?.Invoke(ActiveInputDevice.Gamepad);
                return;
            }

            _activeDevice = ActiveInputDevice.KeyboardMouse;
            _isGamepadActive = false;
            RefreshCursorState();
            OnActiveDeviceChanged?.Invoke(_activeDevice);
        }

        // InputSystem.onEvent: 모든 입력 이벤트를 본다. 연결/해제(onDeviceChange)와 달리
        // "지금 어떤 디바이스를 쓰는가"를 알 수 있는 정석 경로.
        private void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
        {
            ActiveInputDevice candidate;
            GamepadBrand candidateBrand = _gamepadBrand; // 게임패드가 아니면 직전 브랜드 유지

            switch (device)
            {
                case Gamepad gamepad:
                    candidate = ActiveInputDevice.Gamepad;
                    candidateBrand = DetectBrand(gamepad);
                    break;
                case Keyboard _:
                case Mouse _:
                    candidate = ActiveInputDevice.KeyboardMouse;
                    break;
                default:
                    return; // 그 외 디바이스(터치/펜 등)는 무시
            }

            // 디바이스 클래스 또는 (게임패드 유지 중) 브랜드가 바뀌었을 때만 갱신한다.
            // 브랜드 변경을 클래스 변경 뒤에 게이트하면 패드↔패드 브랜드 전환이 누락되므로 둘을 OR로 본다.
            bool classChanged = candidate != _activeDevice;
            bool brandChanged = candidate == ActiveInputDevice.Gamepad && candidateBrand != _gamepadBrand;
            if (!classChanged && !brandChanged)
                return;

            // EnumerateChangedControls는 StateEvent/DeltaStateEvent에 대해서만 동작하며,
            // 임계값 이상으로 액추에이트된 컨트롤만 돌려준다. 노이즈로 인한 깜빡임 차단.
            bool actuated = false;
            foreach (var _ in eventPtr.EnumerateChangedControls(device: device,
                         magnitudeThreshold: _deviceSwitchActuation))
            {
                actuated = true;
                break;
            }

            if (!actuated)
                return;

            // 이벤트 발화 전에 상태를 먼저 확정한다 — 구독자가 Refresh에서 GamepadBrand를 다시 읽을 때 최신값을 보도록.
            _activeDevice = candidate;
            if (candidate == ActiveInputDevice.Gamepad)
                _gamepadBrand = candidateBrand;
            _isGamepadActive = candidate == ActiveInputDevice.Gamepad; // 기존 커서 로직과 동기화
            RefreshCursorState();

            OnActiveDeviceChanged?.Invoke(candidate);
        }

        private static GamepadBrand DetectBrand(Gamepad gamepad)
        {
            switch (gamepad)
            {
                case DualShockGamepad _:           // PS4 DualShock / PS5 DualSense(파생)
                    return GamepadBrand.PlayStation;
                case SwitchProControllerHID _:     // Switch Pro 컨트롤러
                    return GamepadBrand.Switch;
                case XInputController _:            // Xbox(XInput) — Windows 기본 경로
                    return GamepadBrand.Xbox;
                default:
                    return GamepadBrand.Generic;
            }
        }
    }
}
