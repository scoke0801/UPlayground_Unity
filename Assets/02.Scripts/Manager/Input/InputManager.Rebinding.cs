using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UPlayGround.InputDefine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace UPlayGround.Manager
{
    public partial class InputManager
    {
        private const float RebindCaptureTimeout = 10f;
        private const float RebindSecondControlTimeout = 1.25f;
        private const float RebindSingleConfirmDelay = 0.35f;

        private bool _rebindCaptureActive;

        public event Action<InputRebindCaptureState> OnRebindCaptureChanged;

        public async UniTask<InputRebindCaptureResult> CaptureBindingAsync(
            InputBindingTarget target,
            CancellationToken cancellationToken = default)
        {
            if (_rebindCaptureActive)
            {
                return FailedCapture(target, "이미 다른 입력을 캡처하고 있습니다.");
            }

            _rebindCaptureActive = true;
            SetPlayerActionInputSuppressed(true);

            try
            {
                PublishCaptureState(
                    InputRebindCapturePhase.WaitingForNeutral,
                    null,
                    RebindCaptureTimeout,
                    "현재 누른 키를 놓아 주세요.");

                float captureStartedAt = Time.unscaledTime;
                while (!IsDeviceGroupNeutral(target.deviceGroup))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Time.unscaledTime - captureStartedAt >= RebindCaptureTimeout)
                        return TimedOutCapture(target);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                PublishCaptureState(
                    InputRebindCapturePhase.WaitingForFirstControl,
                    null,
                    RebindCaptureTimeout,
                    "새 키를 입력하세요.");

                ButtonControl queuedButton = null;
                using IDisposable subscription = InputSystem.onAnyButtonPress.Call(button =>
                {
                    if (queuedButton != null
                        || button == null
                        || !ControlMatchesDeviceGroup(button, target.deviceGroup))
                    {
                        return;
                    }

                    queuedButton = button as ButtonControl;
                });

                ButtonControl first = null;
                while (first == null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (queuedButton != null)
                    {
                        ButtonControl candidate = queuedButton;
                        queuedButton = null;
                        if (IsCaptureCancel(candidate))
                            return CanceledCapture(target);

                        first = candidate;
                        break;
                    }

                    float elapsed = Time.unscaledTime - captureStartedAt;
                    if (elapsed >= RebindCaptureTimeout)
                        return TimedOutCapture(target);

                    PublishCaptureState(
                        InputRebindCapturePhase.WaitingForFirstControl,
                        null,
                        RebindCaptureTimeout - elapsed,
                        "새 키를 입력하세요.");
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                string firstDisplay = GetControlDisplay(first);
                PublishCaptureState(
                    InputRebindCapturePhase.WaitingForSecondControl,
                    firstDisplay,
                    RebindSecondControlTimeout,
                    "첫 키를 유지한 채 다른 키를 누르면 조합키가 됩니다.");

                float secondStartedAt = Time.unscaledTime;
                float releasedAt = -1f;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (queuedButton != null)
                    {
                        ButtonControl second = queuedButton;
                        queuedButton = null;

                        if (IsCaptureCancel(second))
                            return CanceledCapture(target);
                        if (second == first)
                            continue;

                        var chordResult = new InputRebindCaptureResult(
                            target,
                            InputRebindCapturePhase.Completed,
                            ToBindingPath(first),
                            ToBindingPath(second),
                            $"{firstDisplay} + {GetControlDisplay(second)}");
                        PublishCaptureState(
                            InputRebindCapturePhase.Completed,
                            firstDisplay,
                            0f,
                            chordResult.DisplayString);
                        return chordResult;
                    }

                    if (!first.isPressed)
                    {
                        if (releasedAt < 0f)
                            releasedAt = Time.unscaledTime;

                        if (Time.unscaledTime - releasedAt >= RebindSingleConfirmDelay)
                        {
                            var singleResult = new InputRebindCaptureResult(
                                target,
                                InputRebindCapturePhase.Completed,
                                null,
                                ToBindingPath(first),
                                firstDisplay);
                            PublishCaptureState(
                                InputRebindCapturePhase.Completed,
                                firstDisplay,
                                0f,
                                singleResult.DisplayString);
                            return singleResult;
                        }
                    }
                    else
                    {
                        releasedAt = -1f;
                    }

                    float secondElapsed = Time.unscaledTime - secondStartedAt;
                    if (secondElapsed >= RebindSecondControlTimeout)
                    {
                        var singleResult = new InputRebindCaptureResult(
                            target,
                            InputRebindCapturePhase.Completed,
                            null,
                            ToBindingPath(first),
                            firstDisplay);
                        PublishCaptureState(
                            InputRebindCapturePhase.Completed,
                            firstDisplay,
                            0f,
                            singleResult.DisplayString);
                        return singleResult;
                    }

                    PublishCaptureState(
                        InputRebindCapturePhase.WaitingForSecondControl,
                        firstDisplay,
                        RebindSecondControlTimeout - secondElapsed,
                        "첫 키를 유지한 채 다른 키를 누르면 조합키가 됩니다.");
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return CanceledCapture(target);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return FailedCapture(target, exception.Message);
            }
            finally
            {
                _rebindCaptureActive = false;
                SetPlayerActionInputSuppressed(false);
                SuppressPlayerActionInputBriefly();
            }
        }

        private static bool IsDeviceGroupNeutral(InputBindingDeviceGroup deviceGroup)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!DeviceMatchesGroup(device, deviceGroup))
                    continue;

                foreach (InputControl control in device.allControls)
                {
                    if (control is ButtonControl button && button.isPressed)
                        return false;
                }
            }

            return true;
        }

        private static bool ControlMatchesDeviceGroup(
            InputControl control,
            InputBindingDeviceGroup deviceGroup) =>
            control?.device != null && DeviceMatchesGroup(control.device, deviceGroup);

        private static bool DeviceMatchesGroup(
            InputDevice device,
            InputBindingDeviceGroup deviceGroup)
        {
            return deviceGroup switch
            {
                InputBindingDeviceGroup.Gamepad => device is Gamepad,
                _ => device is Keyboard or Mouse,
            };
        }

        private static bool IsCaptureCancel(ButtonControl button)
        {
            if (button?.device is Keyboard keyboard)
                return button == keyboard.escapeKey;
            if (button?.device is Gamepad gamepad)
                return button == gamepad.buttonEast;
            return false;
        }

        private static string ToBindingPath(InputControl control)
        {
            if (control?.device == null)
                return string.Empty;

            string layout = control.device switch
            {
                Gamepad _ => "Gamepad",
                Keyboard _ => "Keyboard",
                Mouse _ => "Mouse",
                _ => control.device.layout,
            };

            string devicePath = control.device.path?.TrimEnd('/');
            string relative = control.path;
            if (!string.IsNullOrEmpty(devicePath)
                && relative.StartsWith(devicePath, StringComparison.OrdinalIgnoreCase))
            {
                relative = relative.Substring(devicePath.Length).TrimStart('/');
            }
            else
            {
                relative = control.name;
            }

            return $"<{layout}>/{relative}";
        }

        private static string GetControlDisplay(InputControl control)
        {
            if (control == null)
                return "?";

            return string.IsNullOrWhiteSpace(control.displayName)
                ? control.name
                : control.displayName;
        }

        private void PublishCaptureState(
            InputRebindCapturePhase phase,
            string firstControl,
            float remainingSeconds,
            string message)
        {
            OnRebindCaptureChanged?.Invoke(new InputRebindCaptureState(
                phase,
                firstControl,
                Mathf.Max(0f, remainingSeconds),
                message));
        }

        private InputRebindCaptureResult CanceledCapture(InputBindingTarget target)
        {
            PublishCaptureState(
                InputRebindCapturePhase.Canceled,
                null,
                0f,
                "입력 변경을 취소했습니다.");
            return new InputRebindCaptureResult(
                target,
                InputRebindCapturePhase.Canceled,
                null,
                null,
                null);
        }

        private InputRebindCaptureResult TimedOutCapture(InputBindingTarget target)
        {
            PublishCaptureState(
                InputRebindCapturePhase.TimedOut,
                null,
                0f,
                "입력 대기 시간이 초과되었습니다.");
            return new InputRebindCaptureResult(
                target,
                InputRebindCapturePhase.TimedOut,
                null,
                null,
                null);
        }

        private InputRebindCaptureResult FailedCapture(
            InputBindingTarget target,
            string message)
        {
            PublishCaptureState(
                InputRebindCapturePhase.Failed,
                null,
                0f,
                message);
            return new InputRebindCaptureResult(
                target,
                InputRebindCapturePhase.Failed,
                null,
                null,
                message);
        }
    }
}
