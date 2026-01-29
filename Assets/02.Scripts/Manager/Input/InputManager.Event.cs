using System;
using System.Collections.Generic;
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
            ExecuteCallbacks(context, startCallbackDict);
        }

        private void OnInputEventPerformed(InputAction.CallbackContext context)
        {
            ExecuteCallbacks(context, performCallbackDict);
        }

        private void OnInputEventCanceled(InputAction.CallbackContext context)
        {
            ExecuteCallbacks(context, cancelCallbackDict);
        }

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
                if (data.Layer < CurrentLayer)
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
                        // 현재 레이어보다 낮은 레이어이면서, 아직 이번 턴에 실행되지 않은 CancelCallback만 실행
                        if (data.Layer < CurrentLayer && data.CancelCallback != null)
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