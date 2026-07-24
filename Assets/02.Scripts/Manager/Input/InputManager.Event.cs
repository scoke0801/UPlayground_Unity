using System;
using System.Collections.Generic;
using UPlayGround.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;

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
        private readonly HashSet<InputAction> _syntheticallyCanceledActions = new();

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

        private void OnInputEventStarted(InputAction.CallbackContext context)
        {
            if (_rebindCaptureActive)
                return;

            if (ShouldSuppressPlayerActionInput(context))
                return;

            if (ShouldSuppressSingleBecauseActiveChord(context))
                return;

            if (ShouldBlockPointerPlayerActionOverUI(context))
                return;

            ExecuteCallbacks(context, startCallbackDict);
        }

        private void OnInputEventPerformed(InputAction.CallbackContext context)
        {
            if (_rebindCaptureActive)
                return;

            if (ShouldSuppressPlayerActionInput(context))
                return;

            if (ShouldSuppressSingleBecauseActiveChord(context))
                return;

            if (ShouldBlockPointerPlayerActionOverUI(context))
                return;

            CancelActiveChordModifierActions(context);

            // 전투 관련 입력은 Level_0(HUD)일 때만 버퍼에 추가
            if (CurrentLayer == InputLayer.Level_0
                && context.action.actionMap?.name == InputMapNames.PlayerAction)
            {
                string actionName = context.action.name;
                switch (actionName)
                {
                    case InputDefine.PlayerAction.Attack:
                    case InputDefine.PlayerAction.HeavyAttack:
                    case InputDefine.PlayerAction.Dodge:
                    case InputDefine.PlayerAction.Jump:
                    case InputDefine.PlayerAction.Dash: 
                    case InputDefine.PlayerAction.SkillAbility:
                    case InputDefine.PlayerAction.SkillUltimate:
                    case InputDefine.PlayerAction.ElementBuff:
                    case InputDefine.PlayerAction.CharacterSwap_1:
                    case InputDefine.PlayerAction.CharacterSwap_2:
                    case InputDefine.PlayerAction.CharacterSwap_3:
                    case InputDefine.PlayerAction.CharacterSwap_4:
                        _inputBuffer.AddInput(actionName, bufferTime: GetPlayerActionBufferTime(actionName));
                        break;
                }
            }

            ExecuteCallbacks(context, performCallbackDict);
        }

        private PointerEventData _uiPointerEventData;
        private readonly List<RaycastResult> _uiRaycastResults = new(16);

        private bool ShouldBlockPointerPlayerActionOverUI(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == null) return false;
            if (CurrentLayer != InputLayer.Level_0) return false;
            if (action.actionMap?.name != InputMapNames.PlayerAction) return false;
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

        public static float GetPlayerActionBufferTime(string actionName)
        {
            return actionName switch
            {
                InputDefine.PlayerAction.Attack => 0.24f,
                InputDefine.PlayerAction.HeavyAttack => 0.24f,
                InputDefine.PlayerAction.Dodge => 0.15f,
                InputDefine.PlayerAction.Jump => 0.12f,
                InputDefine.PlayerAction.Dash => 0.12f,
                InputDefine.PlayerAction.SkillAbility => 0.20f,
                InputDefine.PlayerAction.SkillUltimate => 0.20f,
                InputDefine.PlayerAction.ElementBuff => 0.20f,
                InputDefine.PlayerAction.CharacterSwap_1 => 0.15f,
                InputDefine.PlayerAction.CharacterSwap_2 => 0.15f,
                InputDefine.PlayerAction.CharacterSwap_3 => 0.15f,
                InputDefine.PlayerAction.CharacterSwap_4 => 0.15f,
                _ => 0.15f,
            };
        }

        private void OnInputEventCanceled(InputAction.CallbackContext context)
        {
            // 조합 성립 시 이미 전달한 Modifier cancel은 실제 버튼 release에서 중복 전달하지 않는다.
            if (_syntheticallyCanceledActions.Remove(context.action))
                return;

            if (_rebindCaptureActive)
                return;

            if (ShouldSuppressPlayerActionInput(context))
                return;

            if (ShouldSuppressSingleBecauseActiveChord(context))
                return;

            ExecuteCallbacks(context, cancelCallbackDict);
        }

        /// <summary>
        /// Unity Input System은 OneModifier composite가 성립해도 같은 Trigger에 바인딩된
        /// 단일 액션을 자동 소비하지 않는다. 같은 맵의 더 구체적인 조합이 활성 상태면
        /// 구성 단일 액션을 라우터 진입점에서 차단한다.
        /// </summary>
        private bool ShouldSuppressSingleBecauseActiveChord(InputAction.CallbackContext context)
        {
            InputAction currentAction = context.action;
            InputControl triggerControl = context.control;
            InputActionMap map = currentAction?.actionMap;
            if (map == null || triggerControl == null)
                return false;

            foreach (InputAction candidate in map.actions)
            {
                if (candidate == currentAction || !candidate.enabled)
                    continue;

                if (TryFindActiveChord(
                        candidate,
                        triggerControl,
                        out _,
                        out _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 조합 액션이 성립하면 Modifier를 단일 Hold 액션으로 사용하던 상태를 취소한다.
        /// 예: Guard(LB) 유지 중 LB+D-pad 퀵슬롯이 성립하면 Guard canceled 콜백을 1회 호출.
        /// </summary>
        private void CancelActiveChordModifierActions(InputAction.CallbackContext context)
        {
            InputAction chordAction = context.action;
            InputControl triggerControl = context.control;
            InputActionMap map = chordAction?.actionMap;
            if (map == null || triggerControl == null)
                return;

            if (!TryFindActiveChord(
                    chordAction,
                    triggerControl,
                    out string modifierPath,
                    out InputControl modifierControl))
            {
                return;
            }

            foreach (InputAction candidate in map.actions)
            {
                if (candidate == chordAction)
                    continue;
                if (!ActionHasSimpleBindingForControl(candidate, modifierControl, modifierPath))
                    continue;
                if (!_syntheticallyCanceledActions.Add(candidate))
                    continue;

                ExecuteCallbacksForAction(
                    context,
                    cancelCallbackDict,
                    map.name,
                    candidate.name);
                _inputBuffer?.ConsumeInput(candidate.name);
            }
        }

        private static bool TryFindActiveChord(
            InputAction action,
            InputControl triggerControl,
            out string modifierPath,
            out InputControl modifierControl)
        {
            modifierPath = null;
            modifierControl = null;

            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding root = bindings[i];
                if (!root.isComposite || string.IsNullOrWhiteSpace(root.effectivePath))
                    continue;

                string modifier = null;
                string trigger = null;
                for (int p = i + 1;
                     p < bindings.Count && bindings[p].isPartOfComposite;
                     p++)
                {
                    if (string.Equals(bindings[p].name, "modifier", StringComparison.OrdinalIgnoreCase))
                        modifier = bindings[p].effectivePath;
                    else if (string.Equals(bindings[p].name, "binding", StringComparison.OrdinalIgnoreCase))
                        trigger = bindings[p].effectivePath;
                }

                if (string.IsNullOrWhiteSpace(modifier)
                    || string.IsNullOrWhiteSpace(trigger)
                    || !ControlMatchesBindingPath(triggerControl, trigger))
                {
                    continue;
                }

                InputControl foundModifier = FindControlOnDevice(triggerControl.device, modifier);
                if (foundModifier is not UnityEngine.InputSystem.Controls.ButtonControl button
                    || !button.isPressed)
                {
                    continue;
                }

                modifierPath = modifier;
                modifierControl = foundModifier;
                return true;
            }

            return false;
        }

        private static bool ActionHasSimpleBindingForControl(
            InputAction action,
            InputControl control,
            string expectedPath)
        {
            if (control == null)
                return false;

            foreach (InputBinding binding in action.bindings)
            {
                if (binding.isComposite
                    || binding.isPartOfComposite
                    || string.IsNullOrWhiteSpace(binding.effectivePath))
                {
                    continue;
                }

                if (ControlMatchesBindingPath(control, binding.effectivePath)
                    || string.Equals(
                        NormalizeBindingPath(binding.effectivePath),
                        NormalizeBindingPath(expectedPath),
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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

        private static bool ControlMatchesBindingPath(InputControl control, string bindingPath)
        {
            if (control == null || string.IsNullOrWhiteSpace(bindingPath))
                return false;

            return string.Equals(
                ToRelativeControlPath(control.path),
                ToRelativeControlPath(bindingPath),
                StringComparison.OrdinalIgnoreCase);
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

        private static string NormalizeBindingPath(string path) =>
            string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().ToLowerInvariant();

        private void ExecuteCallbacksForAction(
            InputAction.CallbackContext context,
            Dictionary<InputCallbackKey, List<InputCallbackData>> dict,
            string mapName,
            string actionName)
        {
            var key = new InputCallbackKey(mapName, actionName);
            if (!dict.TryGetValue(key, out List<InputCallbackData> callbackList))
                return;

            for (int i = 0; i < callbackList.Count; i++)
            {
                InputCallbackData data = callbackList[i];
                if (data.Layer != InputLayer.None && data.Layer < CurrentLayer)
                    continue;
                if (data.CheckFunc != null && !data.CheckFunc.Invoke())
                    continue;

                data.Callback?.Invoke(context);
            }
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

        // 공통 실행 로직
        private void ExecuteCallbacks(InputAction.CallbackContext context,
            Dictionary<InputCallbackKey, List<InputCallbackData>> dict)
        {
            var key = new InputCallbackKey(context.action.actionMap.name, context.action.name);
            if (!dict.TryGetValue(key, out var callbackList))
            {
                return;
            }

            for (int i = 0; i < callbackList.Count; ++i)
            {
                var data = callbackList[i];

                // 레이어 검사: 등록된 레이어가 현재 활성화된 레이어보다 낮으면 실행하지 않음
                if (data.Layer != InputLayer.None && data.Layer < CurrentLayer)
                {
                    continue;
                }

                // 조건 함수 검사: checkFunc가 등록되어 있다면 실행 결과 확인
                if (data.CheckFunc != null && !data.CheckFunc.Invoke())
                {
                    continue;
                }

                // 실행 전 현재 레이어 캐싱
                InputLayer cachedLayer = CurrentLayer;

                // 콜백 실행
                data.Callback?.Invoke(context);

                // 실행 결과로 인해 레이어가 변경되었다면 후속 이벤트 중단
                if (cachedLayer != CurrentLayer)
                {
                    break;
                }
            }
        }

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
                        list.RemoveAt(i);
                    }
                }

                // 리스트가 비었다면 메모리 관리를 위해 키 삭제
                if (list.Count == 0)
                {
                    dict.Remove(key);
                }
            }
        }

        /// <summary>
        /// Layer 변경 시점에 호출되어 입력이 진행중이던 이벤트에 대하여 Cancel처리가 필요함을 노티
        /// </summary>
        private void InvokeCancelEvents(InputLayer newLayer)
        {
            // 한 번의 레이어 변경에 대해 중복 실행을 방지하기 위한 집합
            HashSet<Action> executedCancels = new HashSet<Action>();

            var dicts = new[] { startCallbackDict, performCallbackDict, cancelCallbackDict };
            foreach (var dict in dicts)
            {
                foreach (var list in dict.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var data = list[i];
                        bool layerCondition = data.Layer != InputLayer.None && data.Layer < CurrentLayer;
                        // 현재 레이어보다 낮은 레이어이면서, 아직 이번 턴에 실행되지 않은 CancelCallback만 실행
                        if (layerCondition && data.CancelCallback != null)
                        {
                            if (executedCancels.Add(data.CancelCallback))
                            {
                                data.CancelCallback.Invoke();
                            }
                        }
                    }
                }
            }
        }
    }
}
