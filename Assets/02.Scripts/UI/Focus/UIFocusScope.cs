using System.Collections.Generic;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 전체 화면 UI/팝업의 EventSystem 선택을 스택으로 보존하고 복원한다.
    /// 프리팹에 명시적으로 추가하면 기본 선택을 지정할 수 있으며, 없으면 UI_Base가
    /// 런타임에 붙이고 첫 번째 유효 Selectable을 기본값으로 사용한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIFocusScope : MonoBehaviour
    {
        private static readonly List<UIFocusScope> ActiveScopes = new();

        [SerializeField] private Selectable _defaultSelectable;
        [SerializeField] private bool _rememberLastSelection = true;
        [SerializeField] private bool _autoFocusWhenGamepadActivated = true;

        private GameObject _selectionBeforeShow;
        private GameObject _lastSelection;
        private IInputService _inputService;
        private CanvasGroup _scopeCanvasGroup;
        private bool _inputLocked;
        private bool _interactableBeforeLock;
        private bool _blocksRaycastsBeforeLock;

        public bool IsTopmost =>
            ActiveScopes.Count > 0 && ActiveScopes[^1] == this;

        public void ActivateScope()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                _selectionBeforeShow = eventSystem.currentSelectedGameObject;

            ActiveScopes.Remove(this);
            ActiveScopes.Add(this);
            RefreshScopeLocks();

            BindInputService();
            EnsureSelection();
        }

        public void DeactivateScope()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null
                && IsSelectionInside(eventSystem.currentSelectedGameObject))
            {
                _lastSelection = eventSystem.currentSelectedGameObject;
            }

            ActiveScopes.Remove(this);
            UnbindInputService();
            SetInputLocked(false);
            RefreshScopeLocks();

            if (eventSystem == null)
                return;

            UIFocusScope topScope = GetTopScope();
            GameObject restore = topScope != null
                                 && IsValidSelection(_selectionBeforeShow)
                                 && topScope.IsSelectionInside(_selectionBeforeShow)
                ? _selectionBeforeShow
                : FindTopScopeSelection();
            eventSystem.SetSelectedGameObject(restore);
            topScope?.EnsureSelection();
        }

        public void EnsureSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || !isActiveAndEnabled || !IsTopmost)
                return;

            GameObject current = eventSystem.currentSelectedGameObject;
            if (IsValidSelection(current) && IsSelectionInside(current))
                return;

            GameObject target = _rememberLastSelection && IsValidSelection(_lastSelection)
                ? _lastSelection
                : ResolveDefaultSelection();
            if (target == null)
                return;

            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }

        private void LateUpdate()
        {
            if (!IsTopmost)
                return;

            RefreshScopeLocks();

            // 외부 코드나 포인터가 하위 UI를 선택하더라도 같은 프레임의 마지막에
            // 최상위 스코프로 복귀시켜 다음 Submit/Navigate가 하위 UI로 전달되지 않게 한다.
            EnsureSelection();
        }

        private void BindInputService()
        {
            UnbindInputService();
            _inputService = Svc.Input;
            if (_inputService != null)
                _inputService.OnActiveDeviceChanged += OnActiveDeviceChanged;
        }

        private void UnbindInputService()
        {
            if (_inputService != null)
                _inputService.OnActiveDeviceChanged -= OnActiveDeviceChanged;
            _inputService = null;
        }

        private void OnActiveDeviceChanged(ActiveInputDevice device)
        {
            if (device != ActiveInputDevice.Gamepad
                || !_autoFocusWhenGamepadActivated
                || !IsTopmost)
            {
                return;
            }

            EnsureSelection();
        }

        private GameObject ResolveDefaultSelection()
        {
            if (IsSelectableValid(_defaultSelectable))
                return _defaultSelectable.gameObject;

            Selectable[] selectables = GetComponentsInChildren<Selectable>(true);
            foreach (Selectable selectable in selectables)
            {
                if (IsSelectableValid(selectable))
                    return selectable.gameObject;
            }

            return null;
        }

        private static UIFocusScope GetTopScope()
        {
            RemoveInvalidScopes();
            return ActiveScopes.Count > 0 ? ActiveScopes[^1] : null;
        }

        private static void RefreshScopeLocks()
        {
            RemoveInvalidScopes();
            int topIndex = ActiveScopes.Count - 1;
            for (int i = 0; i < ActiveScopes.Count; i++)
                ActiveScopes[i].SetInputLocked(i != topIndex);
        }

        private static void RemoveInvalidScopes()
        {
            for (int i = ActiveScopes.Count - 1; i >= 0; i--)
            {
                if (ActiveScopes[i] == null)
                    ActiveScopes.RemoveAt(i);
            }
        }

        private void SetInputLocked(bool locked)
        {
            if (_inputLocked == locked)
            {
                if (locked && _scopeCanvasGroup != null)
                {
                    _scopeCanvasGroup.interactable = false;
                    _scopeCanvasGroup.blocksRaycasts = false;
                }
                return;
            }

            _scopeCanvasGroup ??= GetComponent<CanvasGroup>();
            if (_scopeCanvasGroup == null)
                _scopeCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (locked)
            {
                _interactableBeforeLock = _scopeCanvasGroup.interactable;
                _blocksRaycastsBeforeLock = _scopeCanvasGroup.blocksRaycasts;
                _scopeCanvasGroup.interactable = false;
                _scopeCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                _scopeCanvasGroup.interactable = _interactableBeforeLock;
                _scopeCanvasGroup.blocksRaycasts = _blocksRaycastsBeforeLock;
            }

            _inputLocked = locked;
        }

        private bool IsSelectionInside(GameObject selection) =>
            selection != null
            && (selection == gameObject || selection.transform.IsChildOf(transform));

        private static GameObject FindTopScopeSelection()
        {
            for (int i = ActiveScopes.Count - 1; i >= 0; i--)
            {
                UIFocusScope scope = ActiveScopes[i];
                if (scope == null || !scope.isActiveAndEnabled)
                    continue;

                if (IsValidSelection(scope._lastSelection))
                    return scope._lastSelection;

                GameObject fallback = scope.ResolveDefaultSelection();
                if (fallback != null)
                    return fallback;
            }

            return null;
        }

        private static bool IsValidSelection(GameObject selection)
        {
            if (selection == null || !selection.activeInHierarchy)
                return false;

            Selectable selectable = selection.GetComponent<Selectable>();
            return selectable != null && IsSelectableValid(selectable);
        }

        private static bool IsSelectableValid(Selectable selectable) =>
            selectable != null
            && selectable.gameObject.activeInHierarchy
            && selectable.IsActive()
            && selectable.IsInteractable();

        private void OnDestroy()
        {
            ActiveScopes.Remove(this);
            SetInputLocked(false);
            RefreshScopeLocks();
            UnbindInputService();
        }
    }
}
