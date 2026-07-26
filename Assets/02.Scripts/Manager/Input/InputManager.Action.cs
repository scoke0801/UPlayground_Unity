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

            EnsureStandardUiActions();

            // Actions 초기화
            InitializeActions();

            // 사용자 바인딩 슬롯은 Enable 전에 전부 만들어 둔다.
            // 이후 리바인딩은 override만 쓰므로 런타임에 에셋 구조가 바뀌지 않는다.
            // (구조가 바뀌면 UI 액션을 참조하는 InputSystemUIInputModule의 캐시가 깨진다)
            EnsureAllUserBindingSlots();

            // 사용자 바인딩은 Action Map을 Enable하기 전에 적용한다.
            LoadInputBindingProfile();

            // 조합 카탈로그는 effective binding이 확정된 뒤에 만든다.
            InitChordArbiter();

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

        private void EnsureStandardUiActions()
        {
            InputActionMap uiMap = inputActions.FindActionMap(InputMapNames.UI, false);
            if (uiMap == null)
            {
                Debug.LogError("[InputManager] UI Action Map이 없습니다.");
                return;
            }

            InputAction navigate = uiMap.FindAction(UIAction.Navigate, false)
                                   ?? uiMap.AddAction(UIAction.Navigate, InputActionType.PassThrough);
            if (!HasBinding(navigate, "<Gamepad>/leftStick"))
                navigate.AddBinding("<Gamepad>/leftStick", groups: "Gamepad");
            if (!HasCompositePart(navigate, "<Gamepad>/dpad/up"))
            {
                var dpadComposite = navigate.AddCompositeBinding("2DVector")
                    .With("Up", "<Gamepad>/dpad/up", groups: "Gamepad")
                    .With("Down", "<Gamepad>/dpad/down", groups: "Gamepad")
                    .With("Left", "<Gamepad>/dpad/left", groups: "Gamepad")
                    .With("Right", "<Gamepad>/dpad/right", groups: "Gamepad");
                navigate.ChangeBinding(dpadComposite.bindingIndex).WithGroup("Gamepad");
            }
            if (!HasCompositePart(navigate, "<Keyboard>/upArrow"))
            {
                var keyboardComposite = navigate.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow", groups: "Keyboard&Mouse")
                    .With("Down", "<Keyboard>/downArrow", groups: "Keyboard&Mouse")
                    .With("Left", "<Keyboard>/leftArrow", groups: "Keyboard&Mouse")
                    .With("Right", "<Keyboard>/rightArrow", groups: "Keyboard&Mouse");
                navigate.ChangeBinding(keyboardComposite.bindingIndex).WithGroup("Keyboard&Mouse");
            }

            InputAction submit = uiMap.FindAction(UIAction.Submit, false)
                                 ?? uiMap.AddAction(UIAction.Submit, InputActionType.Button);
            if (!HasBinding(submit, "<Gamepad>/buttonSouth"))
                submit.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
            if (!HasBinding(submit, "<Keyboard>/enter"))
                submit.AddBinding("<Keyboard>/enter", groups: "Keyboard&Mouse");
            if (!HasBinding(submit, "<Keyboard>/space"))
                submit.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");

            InputAction cancel = uiMap.FindAction(UIAction.Cancel, false)
                                 ?? uiMap.AddAction(UIAction.Cancel, InputActionType.Button);
            if (!HasBinding(cancel, "<Gamepad>/buttonEast"))
                cancel.AddBinding("<Gamepad>/buttonEast", groups: "Gamepad");
            if (!HasBinding(cancel, "<Keyboard>/escape"))
                cancel.AddBinding("<Keyboard>/escape", groups: "Keyboard&Mouse");

            EnsureUiAction(uiMap, UIAction.Point, InputActionType.PassThrough, "<Mouse>/position", "Keyboard&Mouse");
            EnsureUiAction(uiMap, UIAction.Click, InputActionType.PassThrough, "<Mouse>/leftButton", "Keyboard&Mouse");
            EnsureUiAction(uiMap, UIAction.RightClick, InputActionType.PassThrough, "<Mouse>/rightButton", "Keyboard&Mouse");
            EnsureUiAction(uiMap, UIAction.MiddleClick, InputActionType.PassThrough, "<Mouse>/middleButton", "Keyboard&Mouse");
            EnsureUiAction(uiMap, UIAction.ScrollWheel, InputActionType.PassThrough, "<Mouse>/scroll", "Keyboard&Mouse");
            EnsureUiAction(uiMap, UIAction.VirtualCursorMove, InputActionType.PassThrough, "<Gamepad>/rightStick", "Gamepad");
        }

        private static InputAction EnsureUiAction(
            InputActionMap map,
            string actionName,
            InputActionType type,
            string defaultPath,
            string group)
        {
            InputAction action = map.FindAction(actionName, false) ?? map.AddAction(actionName, type);
            if (!HasBinding(action, defaultPath))
                action.AddBinding(defaultPath, groups: group);
            return action;
        }

        private static bool HasBinding(InputAction action, string path)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (string.Equals(binding.path, path, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasCompositePart(InputAction action, string partPath)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (!bindings[i].isComposite)
                    continue;

                for (int p = i + 1; p < bindings.Count && bindings[p].isPartOfComposite; p++)
                {
                    if (string.Equals(
                            bindings[p].path,
                            partPath,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
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
