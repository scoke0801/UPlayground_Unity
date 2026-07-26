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
        private const float RebindCommandHoldDuration = 0.75f;

        private enum CaptureButtonDisposition
        {
            Binding,
            Cancel,
            Remove,
        }

        private bool _rebindCaptureActive;
        public bool IsRebindCaptureActive => _rebindCaptureActive;

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
            InputActionMap uiActionMap =
                inputActions?.FindActionMap(InputMapNames.UI, false);
            bool restoreUiActionMap = uiActionMap?.enabled == true;
            if (restoreUiActionMap)
                uiActionMap.Disable();

            try
            {
                if (!IsCaptureDeviceAvailable(target.deviceGroup))
                {
                    return FailedCapture(
                        target,
                        target.deviceGroup == InputBindingDeviceGroup.Gamepad
                            ? "연결된 게임패드가 없습니다."
                            : "사용할 수 있는 키보드 또는 마우스가 없습니다.");
                }

                PublishCaptureState(
                    InputRebindCapturePhase.WaitingForNeutral,
                    null,
                    RebindCaptureTimeout,
                    "현재 누른 키를 놓아 주세요.");

                float captureStartedAt = Time.unscaledTime;
                while (!IsDeviceGroupNeutral(target.deviceGroup))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsCaptureDeviceAvailable(target.deviceGroup))
                        return CaptureDeviceDisconnected(target);
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
                        || button == null)
                    {
                        return;
                    }

                    ButtonControl candidate = button as ButtonControl;
                    if (candidate == null)
                        return;

                    // 취소/삭제 명령 후보는 캡처 대상 장치와 무관하게 먼저 큐에 넣는다.
                    // 짧게 놓으면 일반 바인딩으로 해석하고, 길게 누른 경우에만 명령으로
                    // 확정한다. 따라서 Esc/East/Backspace/Delete도 실제 키로 할당할 수 있다.
                    if (IsCaptureCancel(candidate)
                        || IsCaptureRemove(candidate)
                        || ControlMatchesDeviceGroup(candidate, target.deviceGroup))
                    {
                        queuedButton = candidate;
                    }
                });

                ButtonControl first = null;
                while (first == null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsCaptureDeviceAvailable(target.deviceGroup))
                        return CaptureDeviceDisconnected(target);

                    if (queuedButton != null)
                    {
                        ButtonControl candidate = queuedButton;
                        queuedButton = null;

                        CaptureButtonDisposition disposition =
                            await ResolveCaptureButtonDispositionAsync(
                                candidate,
                                InputRebindCapturePhase.WaitingForFirstControl,
                                null,
                                cancellationToken);
                        if (disposition == CaptureButtonDisposition.Cancel)
                            return CanceledCapture(target);
                        if (disposition == CaptureButtonDisposition.Remove)
                            return RemovedCapture(target);
                        if (!ControlMatchesDeviceGroup(candidate, target.deviceGroup))
                            continue;

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
                    if (!IsCaptureDeviceAvailable(target.deviceGroup))
                        return CaptureDeviceDisconnected(target);

                    if (queuedButton != null)
                    {
                        ButtonControl second = queuedButton;
                        queuedButton = null;

                        CaptureButtonDisposition disposition =
                            await ResolveCaptureButtonDispositionAsync(
                                second,
                                InputRebindCapturePhase.WaitingForSecondControl,
                                firstDisplay,
                                cancellationToken);
                        if (disposition == CaptureButtonDisposition.Cancel)
                            return CanceledCapture(target);
                        if (disposition == CaptureButtonDisposition.Remove)
                            return RemovedCapture(target);
                        if (!ControlMatchesDeviceGroup(second, target.deviceGroup))
                            continue;
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
                while (!AreUiInputButtonsNeutral())
                    await UniTask.Yield(PlayerLoopTiming.Update);
                if (restoreUiActionMap && uiActionMap != null)
                    uiActionMap.Enable();
                _rebindCaptureActive = false;
                SetPlayerActionInputSuppressed(false);
                SuppressPlayerActionInputBriefly();
            }
        }

        private static bool AreUiInputButtonsNeutral() =>
            IsDeviceGroupNeutral(InputBindingDeviceGroup.KeyboardMouse)
            && IsDeviceGroupNeutral(InputBindingDeviceGroup.Gamepad);

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

        private static bool IsCaptureDeviceAvailable(InputBindingDeviceGroup deviceGroup)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (DeviceMatchesGroup(device, deviceGroup))
                    return true;
            }

            return false;
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

        private static bool IsCaptureRemove(ButtonControl button)
        {
            if (button?.device is not Keyboard keyboard)
                return false;

            return button == keyboard.backspaceKey || button == keyboard.deleteKey;
        }

        /// <summary>
        /// 예약 버튼을 짧게 누르면 바인딩으로, 일정 시간 유지하면 캡처 명령으로 해석한다.
        /// 모든 물리 버튼을 재할당할 수 있게 하면서도 마우스 없이 캡처를 빠져나갈 경로를
        /// 보장한다.
        /// </summary>
        private async UniTask<CaptureButtonDisposition> ResolveCaptureButtonDispositionAsync(
            ButtonControl button,
            InputRebindCapturePhase phase,
            string firstControlDisplay,
            CancellationToken cancellationToken)
        {
            bool cancelCandidate = IsCaptureCancel(button);
            bool removeCandidate = IsCaptureRemove(button);
            if (!cancelCandidate && !removeCandidate)
                return CaptureButtonDisposition.Binding;

            string display = GetControlDisplay(button);
            string command = cancelCandidate ? "취소" : "바인딩 제거";
            float holdStartedAt = Time.unscaledTime;

            while (button.isPressed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float elapsed = Time.unscaledTime - holdStartedAt;
                if (elapsed >= RebindCommandHoldDuration)
                {
                    return cancelCandidate
                        ? CaptureButtonDisposition.Cancel
                        : CaptureButtonDisposition.Remove;
                }

                PublishCaptureState(
                    phase,
                    firstControlDisplay,
                    RebindCommandHoldDuration - elapsed,
                    $"{display}: 짧게 놓으면 할당, 계속 누르면 {command}");
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            return CaptureButtonDisposition.Binding;
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

        private InputRebindCaptureResult CaptureDeviceDisconnected(InputBindingTarget target)
        {
            const string message = "캡처 대상 장치의 연결이 끊어졌습니다.";
            PublishCaptureState(
                InputRebindCapturePhase.Canceled,
                null,
                0f,
                message);
            return new InputRebindCaptureResult(
                target,
                InputRebindCapturePhase.Canceled,
                null,
                null,
                message);
        }

        private InputRebindCaptureResult RemovedCapture(InputBindingTarget target)
        {
            const string message = "바인딩 제거를 요청했습니다.";
            PublishCaptureState(
                InputRebindCapturePhase.Completed,
                null,
                0f,
                message);
            return new InputRebindCaptureResult(
                target,
                InputRebindCapturePhase.Completed,
                null,
                null,
                message,
                removalRequested: true);
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
