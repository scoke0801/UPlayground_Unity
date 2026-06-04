using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 입력 시스템 관리 매니저 - GameInputAction 관리
    /// </summary>
    public partial class InputManager : BaseManager<InputManager>, IManager
    {
        [Header("Input Actions")] [SerializeField]
        private InputActionAsset inputActions;

        private Dictionary<(string /*ActionMap*/, string /*Action*/), InputAction> actionCache
            = new Dictionary<(string, string), InputAction>();

        private Dictionary<string, InputActionMap> actionMapCache = new Dictionary<string, InputActionMap>();

        public void InitInputAction()
        {
            // Input Actions Asset 로드
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>("Input/PlayerInputActions");
                if (inputActions == null)
                {
                    Debug.LogError("[InputManager] PlayerInputActions를 찾을 수 없습니다!");
                    return;
                }
            }

            // Actions 초기화
            InitializeActions();

            foreach (var inputActionMap in actionMapCache.Values)
            {
                inputActionMap.Enable();
            }
        }

        private void InitializeActions()
        {
            // 모든 액션 맵 순회
            foreach (var map in inputActions.actionMaps)
            {
                actionMapCache.Add(map.name, map);

                // 각 맵의 모든 액션 순회
                foreach (var action in map.actions)
                {
                    var key = (map.name, action.name);
                    actionCache[key] = action;
                    actionCache.TryAdd(key, action);

                    action.started += OnInputEventStarted;
                    action.performed += OnInputEventPerformed;
                    action.canceled += OnInputEventCanceled;
                }
            }

            Debug.Log($"총 {actionCache.Count}개 액션 캐싱 완료");
        }

        /// <summary>
        /// 특정 액션 활성화/비활성화
        /// </summary>
        public void SetActionEnabled(string actionMapName, string actionName, bool inEnabled)
        {
            InputAction action = GetAction(actionMapName, actionName);
            if (action == null)
            {
                return;
            }

            if (inEnabled)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }

        public InputAction GetAction(string mapName, string actionName)
        {
            var key = (mapName, actionName);
            return actionCache.TryGetValue(key, out var action) ? action : null;
        }

        public bool GetAction(string mapName, string actionName, out InputAction action)
        {
            var key = (mapName, actionName);
            if (actionCache.TryGetValue(key, out action))
            {
                return true;
            }

            return false;
        }
    }
}