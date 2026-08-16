using System;
using System.Collections.Generic;
using UPlayGround.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Diagnostics;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 입력 시스템 관리 매니저 - GameInputAction 관리
    /// </summary>
    public partial class InputManager : BaseManager<InputManager>, IManager
    {
        
        private readonly struct InputCallbackKey : IEquatable<InputCallbackKey>
        {
            public readonly string ActionMapName;
            public readonly string ActionName;

            public InputCallbackKey(string mapName, string actionName)
            {
                ActionMapName = mapName;
                ActionName = actionName;
            }

            public bool Equals(InputCallbackKey other) =>
                ActionMapName == other.ActionMapName && ActionName == other.ActionName;

            public override int GetHashCode() => HashCode.Combine(ActionMapName, ActionName);
        }

        // 콜백 정보를 담는 클래스
        private class InputCallbackData
        {
            public Action<InputAction.CallbackContext> Callback;
            public Func<bool> CheckFunc;
            public Action CancelCallback;
            public InputLayer Layer;
            public bool IsRegistered = true;

            public InputCallbackData(Action<InputAction.CallbackContext> callback, Func<bool> checkFunc,
                Action cancelCallback, InputLayer layer)
            {
                Callback = callback;
                CheckFunc = checkFunc;
                CancelCallback = cancelCallback;
                Layer = layer;
            }
        }

        private Dictionary<InputCallbackKey, List<InputCallbackData>> startCallbackDict = new();
        private Dictionary<InputCallbackKey, List<InputCallbackData>> performCallbackDict = new();
        private Dictionary<InputCallbackKey, List<InputCallbackData>> cancelCallbackDict = new();
        private readonly List<InputCallbackKey> _emptyCallbackKeyScratch = new();
        private int _callbackDispatchDepth;
        private bool _hasPendingCallbackCleanup;

        public void RegisterInputEvent(string mapName, string actionName,
            Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled,
            Func<bool> checkFunc, Action cancelCallback, InputLayer inputLayer)
        {
            var key = new InputCallbackKey(mapName, actionName);

            if (started != null)
            {
                if (!startCallbackDict.ContainsKey(key))
                {
                    startCallbackDict[key] = new List<InputCallbackData>();
                }

                startCallbackDict[key].Add(new InputCallbackData(started, checkFunc, cancelCallback, inputLayer));
            }

            if (performed != null)
            {
                if (!performCallbackDict.ContainsKey(key))
                {
                    performCallbackDict[key] = new List<InputCallbackData>();
                }

                performCallbackDict[key].Add(new InputCallbackData(performed, checkFunc, cancelCallback, inputLayer));
            }

            if (canceled != null)
            {
                if (!cancelCallbackDict.ContainsKey(key))
                {
                    cancelCallbackDict[key] = new List<InputCallbackData>();
                }

                cancelCallbackDict[key].Add(new InputCallbackData(canceled, checkFunc, cancelCallback, inputLayer));
            }
        }

        public void UnRegisterInputEvent(string mapName, string actionName,
            Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled)
        {
            var key = new InputCallbackKey(mapName, actionName);

            if (started != null) RemoveFromDict(startCallbackDict, key, started);
            if (performed != null) RemoveFromDict(performCallbackDict, key, performed);
            if (canceled != null) RemoveFromDict(cancelCallbackDict, key, canceled);
        }

        // 세 진입점 모두 게이트를 통과한 뒤에는 조합 중재기(InputManager.Chord.cs)를 거친다.
        // 콜백 디스패치와 전투 버퍼 적재는 중재 결과가 확정된 시점에만 일어난다.
        private void OnInputEventStarted(InputAction.CallbackContext context)
        {
            if (!PassesInputGates(context))
                return;

            SubmitToChordArbiter(context, InputArbiterPhase.Started);
        }

        private void OnInputEventPerformed(InputAction.CallbackContext context)
        {
            if (!PassesInputGates(context))
                return;

            SubmitToChordArbiter(context, InputArbiterPhase.Performed);
        }

        private bool PassesInputGates(
            InputAction.CallbackContext context,
            bool applyPointerGate = true,
            bool isRelease = false)
        {
            // started/performed를 차단했더라도 이미 전달된 hold의 release는 반드시 통과시킨다.
            // canceled까지 막으면 가드·차지·커서 표시 같은 상태가 영구히 남을 수 있다.
            if (!isRelease && _rebindCaptureActive)
            {
                return false;
            }

            if (!isRelease && ShouldSuppressPlayerActionInput(context))
            {
                return false;
            }

            if (applyPointerGate && ShouldBlockPointerPlayerActionOverUI(context))
            {
                return false;
            }

            return true;
        }

        private PointerEventData _uiPointerEventData;
        private readonly List<RaycastResult> _uiRaycastResults = new(16);

        private bool ShouldBlockPointerPlayerActionOverUI(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == null) return false;
            if (CurrentLayer != InputLayer.Level_0) return false;
            if (action.actionMap?.name != InputMapNames.PlayerAction) return false;

            // TPS 조작 중 잠기거나 숨겨진 포인터는 화면 중앙 HUD/조준점 위에 머물 수 있다.
            // 실제 UI 포인터를 표시한 상태에서만 UI 레이캐스트로 게임플레이 입력을 차단한다.
            if (_cursorVisibleStack <= 0
                || !Cursor.visible
                || Cursor.lockState == CursorLockMode.Locked)
                return false;

            if (!IsPointerLikeInput(context)) return false;

            return IsPointerOverUI(context);
        }

        private static bool IsPointerLikeInput(InputAction.CallbackContext context)
        {
            var device = context.control?.device;
            return device is Mouse || device is Touchscreen || device is Pen;
        }

        // 입력 콜백에서 EventSystem.IsPointerOverGameObject()를 호출하지 않기 위해
        // 현재 포인터 좌표로 직접 UI 레이캐스트한다.
        private bool IsPointerOverUI(InputAction.CallbackContext context)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (!TryGetPointerPosition(context, out Vector2 position))
                return false;

            _uiPointerEventData ??= new PointerEventData(eventSystem);
            _uiPointerEventData.Reset();
            _uiPointerEventData.position = position;

            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(_uiPointerEventData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        private static bool TryGetPointerPosition(InputAction.CallbackContext context, out Vector2 position)
        {
            switch (context.control?.device)
            {
                case Mouse mouse:
                    position = mouse.position.ReadValue();
                    return true;
                case Pen pen:
                    position = pen.position.ReadValue();
                    return true;
                case Touchscreen touchscreen:
                    position = touchscreen.primaryTouch.position.ReadValue();
                    return true;
                default:
                    position = default;
                    return false;
            }
        }

        /// <summary>
        /// HUD 클릭을 실제 PlayerAction의 started/performed 흐름과 동일하게 전달한다.
        /// release는 다음 프레임 LateUpdate에 보내 PlayerActor가 performed 상태를 최소 한 번 소비하게 한다.
        /// 포인터 UI 게이트는 명시적인 HUD 클릭이므로 적용하지 않는다.
        /// </summary>
        public bool TryPerformPlayerAction(string actionName)
        {
            if (_rebindCaptureActive
                || CurrentLayer != InputLayer.Level_0
                || IsPlayerActionCurrentlySuppressed()
                || !IsHudPlayerAction(actionName))
            {
                return false;
            }

            // 같은 액션을 연속 프레임에 다시 요청하면 pending 프레임만 뒤로 밀려
            // started/performed만 반복되고 canceled가 영구히 발화되지 않는다(차지 고착·버퍼 소실).
            // 새 started 앞에서 직전 합성 입력을 먼저 강제 릴리스해 started/canceled 대칭을 보장한다.
            ForceReleaseSyntheticPlayerAction(actionName);

            var context = default(InputAction.CallbackContext);
            ExecuteCallbacksForAction(
                context,
                startCallbackDict,
                InputMapNames.PlayerAction,
                actionName,
                InputArbiterPhase.Started);

            _inputBuffer?.AddInput(
                actionName,
                bufferTime: PlayerInputBufferPolicy.GetDuration(actionName),
                replaceExisting: true);

            ExecuteCallbacksForAction(
                context,
                performCallbackDict,
                InputMapNames.PlayerAction,
                actionName,
                InputArbiterPhase.Performed);

            _pendingSyntheticPlayerActionReleases[actionName] = Time.frameCount + 1;
            return true;
        }

        /// <summary>
        /// 보류 중인 합성 입력의 릴리스(canceled)를 발화한다.
        /// force가 true면 프레임 조건과 무관하게 전부 즉시 해제한다(Dispose 경로).
        /// 릴리스 콜백이 다시 TryPerformPlayerAction을 호출해 딕셔너리를 변경할 수 있으므로,
        /// 대상 키를 먼저 수집·제거한 뒤에 콜백을 발화한다(순회 중 컬렉션 변경 방지).
        /// </summary>
        private void ReleaseSyntheticPlayerActions(bool force = false)
        {
            if (_pendingSyntheticPlayerActionReleases.Count == 0)
                return;

            _syntheticReleaseScratch.Clear();
            foreach (var pair in _pendingSyntheticPlayerActionReleases)
            {
                if (!force && Time.frameCount < pair.Value)
                    continue;

                _syntheticReleaseScratch.Add(pair.Key);
            }

            if (_syntheticReleaseScratch.Count == 0)
                return;

            // 콜백 발화 전에 먼저 제거해야 재진입 시 딕셔너리 변경이 안전하다.
            for (int i = 0; i < _syntheticReleaseScratch.Count; i++)
                _pendingSyntheticPlayerActionReleases.Remove(_syntheticReleaseScratch[i]);

            for (int i = 0; i < _syntheticReleaseScratch.Count; i++)
                InvokeSyntheticRelease(_syntheticReleaseScratch[i], suppressExceptions: force);
        }

        /// <summary>
        /// 특정 액션의 보류 릴리스를 즉시 발화한다. 보류 중이 아니면 아무것도 하지 않는다.
        /// 재진입 안전을 위해 공용 스크래치 리스트를 사용하지 않는다.
        /// </summary>
        private void ForceReleaseSyntheticPlayerAction(string actionName)
        {
            if (!_pendingSyntheticPlayerActionReleases.Remove(actionName))
                return;

            InvokeSyntheticRelease(actionName, suppressExceptions: false);
        }

        /// <summary>
        /// 합성 입력의 cancel 콜백을 발화한다.
        /// UI가 같은 프레임에 모달을 열었더라도 hold 상태는 반드시 해제해야 하므로
        /// 레이어 게이트와 레이어 변경 break를 모두 우회한다(일반 물리 입력 경로는 그대로 유지).
        /// </summary>
        private void InvokeSyntheticRelease(string actionName, bool suppressExceptions)
        {
            var context = default(InputAction.CallbackContext);

            if (!suppressExceptions)
            {
                ExecuteCallbacksForAction(
                    context,
                    cancelCallbackDict,
                    InputMapNames.PlayerAction,
                    actionName,
                    InputArbiterPhase.Canceled,
                    ignoreLayer: true,
                    ignoreLayerChangeBreak: true);
                return;
            }

            // Dispose 시점에는 소비자가 이미 파괴됐을 수 있다. 예외가 정리 흐름을 끊지 않게 막는다.
            try
            {
                ExecuteCallbacksForAction(
                    context,
                    cancelCallbackDict,
                    InputMapNames.PlayerAction,
                    actionName,
                    InputArbiterPhase.Canceled,
                    ignoreLayer: true,
                    ignoreLayerChangeBreak: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[InputManager] 합성 입력 릴리스 중 예외 무시 ({actionName}): {e}");
            }
        }

        private bool IsPlayerActionCurrentlySuppressed()
        {
            return _isPlayerActionInputSuppressed
                   || Time.frameCount <= _playerActionSuppressedUntilFrame
                   || Time.unscaledTime <= _playerActionSuppressedUntilTime;
        }

        private static bool IsHudPlayerAction(string actionName)
        {
            return actionName == PlayerAction.Attack
                   || actionName == PlayerAction.HeavyAttack
                   || actionName == PlayerAction.Dodge
                   || actionName == PlayerAction.Jump
                   || actionName == PlayerAction.Dash
                   || actionName == PlayerAction.SkillAbility
                   || actionName == PlayerAction.SkillUltimate
                   || actionName == PlayerAction.ElementBuff;
        }

        // Canceled는 포인터-오버-UI 게이트를 적용하지 않는다.
        // 눌러둔 채 커서가 UI 위로 올라간 상태에서 떼면 release가 유실돼 hold가 영구히 남는다.
        private void OnInputEventCanceled(InputAction.CallbackContext context)
        {
            if (!PassesInputGates(context, applyPointerGate: false, isRelease: true))
                return;

            SubmitToChordArbiter(context, InputArbiterPhase.Canceled);
        }

        private static InputControl FindControlOnDevice(InputDevice device, string bindingPath)
        {
            if (device == null || string.IsNullOrWhiteSpace(bindingPath))
                return null;

            string relative = ToRelativeControlPath(bindingPath);
            foreach (InputControl control in device.allControls)
            {
                if (string.Equals(
                        ToRelativeControlPath(control.path),
                        relative,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return control;
                }
            }

            return null;
        }

        private static string ToRelativeControlPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            int layoutEnd = path.IndexOf(">/", StringComparison.Ordinal);
            if (layoutEnd >= 0)
                return path.Substring(layoutEnd + 2).Trim('/').ToLowerInvariant();

            string normalized = path.Trim('/').ToLowerInvariant();
            int slash = normalized.IndexOf('/');
            return slash >= 0 ? normalized.Substring(slash + 1) : normalized;
        }

        private void ExecuteCallbacksForAction(
            InputAction.CallbackContext context,
            Dictionary<InputCallbackKey, List<InputCallbackData>> dict,
            string mapName,
            string actionName,
            InputArbiterPhase phase,
            bool ignoreLayer = false,
            bool ignoreLayerChangeBreak = false)
        {
            var key = new InputCallbackKey(mapName, actionName);
            if (!dict.TryGetValue(key, out List<InputCallbackData> callbackList))
                return;

            _callbackDispatchDepth++;
            try
            {
                bool traced = false;
                int callbackCount = callbackList.Count;
                for (int i = 0; i < callbackCount; i++)
                {
                    InputCallbackData data = callbackList[i];
                    if (!data.IsRegistered)
                        continue;

                    // 레이어 검사: 등록된 레이어가 현재 활성화된 레이어보다 낮으면 실행하지 않음
                    if (!ignoreLayer && data.Layer != InputLayer.None && data.Layer < CurrentLayer)
                        continue;

                    // 조건 함수 검사: checkFunc가 등록되어 있다면 실행 결과 확인
                    if (data.CheckFunc != null && !data.CheckFunc.Invoke())
                        continue;

                    // 실행 전 현재 레이어 캐싱
                    InputLayer cachedLayer = CurrentLayer;

                    if (!traced)
                    {
                        TraceInputDispatch(context, mapName, actionName, phase);
                        traced = true;
                    }

                    data.Callback?.Invoke(context);

                    // 실행 결과로 레이어가 바뀌거나 PlayerAction 억제가 시작됐다면 후속 콜백을 막는다.
                    // canceled와 합성 릴리스는 hold 해제를 모두 전달해야 하므로 중단 조건에서 제외한다.
                    if (!ignoreLayerChangeBreak
                        && (cachedLayer != CurrentLayer
                            || (phase != InputArbiterPhase.Canceled
                                && mapName == InputMapNames.PlayerAction
                                && IsPlayerActionCurrentlySuppressed())))
                    {
                        break;
                    }
                }
            }
            finally
            {
                EndCallbackDispatch();
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void TraceInputDispatch(
            InputAction.CallbackContext context,
            string mapName,
            string actionName,
            InputArbiterPhase phase)
        {
            if (!RuntimeLog.IsEnabled(RuntimeLogCategory.Input)) return;

            InputAction action = context.action;

            // Move/Look 같은 연속 값은 performed가 매 입력 업데이트마다 발생한다.
            // 시작/해제만 남기고, Button의 performed와 합성 입력은 모두 기록한다.
            if (phase == InputArbiterPhase.Performed
                && action != null
                && action.type != InputActionType.Button)
            {
                return;
            }

            string source = action == null ? "Synthetic" : "Physical";
            RuntimeLog.Trace(
                RuntimeLogCategory.Input,
                $"[Input] {mapName}/{actionName} ({phase}, {source})");
        }

        // PlayerAction 액션맵에 한해 차단. UI/메뉴 등 다른 맵은 통과시켜 모션 툴 사용 중에도 메뉴 조작이 가능해야 한다.
        // actionMap.name 비교는 Unity Input System 액션맵 이름이 InputMapNames.PlayerAction 상수와 동일하게 유지되어야 안전.
        // Look(카메라 회전)은 _allowLookDuringSuppression이 켜져 있으면 통과 — 모션 프리뷰 중 시점 회전용.
        private bool ShouldSuppressPlayerActionInput(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == null) return false;
            if (action.actionMap?.name != InputMapNames.PlayerAction) return false;

            if (!_isPlayerActionInputSuppressed
                && Time.frameCount > _playerActionSuppressedUntilFrame
                && Time.unscaledTime > _playerActionSuppressedUntilTime)
            {
                return false;
            }

            // 참조 비교: InitInputAction에서 캐시된 동일 인스턴스이므로 문자열 비교보다 저렴.
            if (_allowLookDuringSuppression && action == _cachedLookAction) return false;
            return true;
        }

        // 다른 시스템에서 InputBuffer에 접근할 수 있도록 하는 프로퍼티
        public InputBuffer InputBuffer => _inputBuffer;

        private void RemoveFromDict(Dictionary<InputCallbackKey, List<InputCallbackData>> dict,
            InputCallbackKey key, Action<InputAction.CallbackContext> callback)
        {
            if (dict.TryGetValue(key, out var list))
            {
                // 리스트를 뒤에서부터 순회하며 일치하는 콜백 제거
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].Callback == callback)
                    {
                        if (_callbackDispatchDepth > 0)
                        {
                            list[i].IsRegistered = false;
                            _hasPendingCallbackCleanup = true;
                        }
                        else
                        {
                            list.RemoveAt(i);
                        }
                    }
                }

                // 리스트가 비었다면 메모리 관리를 위해 키 삭제
                if (_callbackDispatchDepth == 0 && list.Count == 0)
                {
                    dict.Remove(key);
                }
            }
        }

        private void EndCallbackDispatch()
        {
            _callbackDispatchDepth--;
            if (_callbackDispatchDepth == 0 && _hasPendingCallbackCleanup)
                CleanupInactiveCallbacks();
        }

        private void CleanupInactiveCallbacks()
        {
            CleanupInactiveCallbacks(startCallbackDict);
            CleanupInactiveCallbacks(performCallbackDict);
            CleanupInactiveCallbacks(cancelCallbackDict);
            _hasPendingCallbackCleanup = false;
        }

        private void CleanupInactiveCallbacks(
            Dictionary<InputCallbackKey, List<InputCallbackData>> dict)
        {
            _emptyCallbackKeyScratch.Clear();
            foreach (KeyValuePair<InputCallbackKey, List<InputCallbackData>> pair in dict)
            {
                List<InputCallbackData> callbacks = pair.Value;
                for (int i = callbacks.Count - 1; i >= 0; i--)
                {
                    if (!callbacks[i].IsRegistered)
                        callbacks.RemoveAt(i);
                }

                if (callbacks.Count == 0)
                    _emptyCallbackKeyScratch.Add(pair.Key);
            }

            for (int i = 0; i < _emptyCallbackKeyScratch.Count; i++)
                dict.Remove(_emptyCallbackKeyScratch[i]);
        }

        /// <summary>
        /// Layer 변경 시점에 호출되어 입력이 진행중이던 이벤트에 대하여 Cancel처리가 필요함을 노티
        /// </summary>
        private void InvokeCancelEvents(InputLayer newLayer, string mapName = null)
        {
            // 한 번의 레이어 변경에 대해 중복 실행을 방지하기 위한 집합
            HashSet<Action> executedCancels = new HashSet<Action>();
            List<InputCallbackKey> callbackKeySnapshot = new List<InputCallbackKey>();

            _callbackDispatchDepth++;
            try
            {
                InvokeCancelEvents(startCallbackDict, newLayer, mapName, executedCancels, callbackKeySnapshot);
                InvokeCancelEvents(performCallbackDict, newLayer, mapName, executedCancels, callbackKeySnapshot);
                InvokeCancelEvents(cancelCallbackDict, newLayer, mapName, executedCancels, callbackKeySnapshot);
            }
            finally
            {
                EndCallbackDispatch();
            }
        }

        private static void InvokeCancelEvents(
            Dictionary<InputCallbackKey, List<InputCallbackData>> dict,
            InputLayer newLayer,
            string mapName,
            HashSet<Action> executedCancels,
            List<InputCallbackKey> callbackKeySnapshot)
        {
            // CancelCallback이 새 입력을 등록해도 현재 순회의 Dictionary 열거자가 깨지거나
            // 같은 취소 턴에 새 콜백까지 실행되지 않도록 시작 시점의 키만 순회한다.
            callbackKeySnapshot.Clear();
            foreach (InputCallbackKey key in dict.Keys)
                callbackKeySnapshot.Add(key);

            for (int keyIndex = 0; keyIndex < callbackKeySnapshot.Count; keyIndex++)
            {
                InputCallbackKey key = callbackKeySnapshot[keyIndex];
                if (mapName != null && key.ActionMapName != mapName)
                    continue;
                if (!dict.TryGetValue(key, out List<InputCallbackData> callbacks))
                    continue;

                int callbackCount = callbacks.Count;
                for (int i = 0; i < callbackCount; i++)
                {
                    InputCallbackData data = callbacks[i];
                    bool layerCondition = data.IsRegistered
                                          && data.Layer != InputLayer.None
                                          && data.Layer < newLayer;
                    if (layerCondition
                        && data.CancelCallback != null
                        && executedCancels.Add(data.CancelCallback))
                    {
                        try
                        {
                            data.CancelCallback.Invoke();
                        }
                        catch (Exception exception)
                        {
                            // 한 소비자의 정리 실패가 나머지 hold 해제와 레이어 전환을 막으면
                            // 입력 고착 범위가 확대되므로 오류를 보고하고 다음 콜백을 계속 처리한다.
                            Debug.LogException(exception);
                        }
                    }
                }
            }
        }
    }
}
