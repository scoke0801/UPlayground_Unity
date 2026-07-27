using System.Collections.Generic;
using System.Linq;
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

        [Header("ScrollRect 자동 추적")]
        [Tooltip("게임패드 내비게이션으로 선택이 이동하면 항목이 보이도록 스크롤을 따라간다.")]
        [SerializeField] private bool _autoScrollToSelection = true;

        [Tooltip("뷰포트 가장자리에서 확보할 여백(px).")]
        [SerializeField] private float _scrollPadding = 16f;

        [Tooltip("0이면 즉시 이동. 값이 클수록 빠르게 따라간다.")]
        [SerializeField] private float _scrollLerpSpeed = 16f;

        private GameObject _selectionBeforeShow;
        private GameObject _lastSelection;
        private GameObject _lastTrackedSelection;
        private ScrollRect _trackedScrollRect;
        private Vector2 _scrollTargetNormalized;
        private Vector2 _lastAppliedScrollNormalized;
        private bool _hasScrollTarget;
        private IInputService _inputService;
        private CanvasGroup _scopeCanvasGroup;
        private bool _inputLocked;
        private bool _interactableBeforeLock;
        private bool _blocksRaycastsBeforeLock;
        private bool _virtualPointerActive;

        public bool IsTopmost =>
            ActiveScopes.Count > 0 && ActiveScopes[^1] == this;

        public void SetDefaultSelectable(Selectable selectable, bool ensureSelection = false)
        {
            _defaultSelectable = selectable;
            if (ensureSelection)
                EnsureSelection();
        }

        /// <summary>
        /// 가상 커서가 공간 UI를 조작하는 동안 선택 자동 복원을 중지한다.
        /// UINavigation으로 돌아오면 마지막/기본 선택을 즉시 복원한다.
        /// </summary>
        public void SetVirtualPointerActive(bool active)
        {
            _virtualPointerActive = active;
            if (!active)
                EnsureSelection();
        }

        public void ActivateScope()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                _selectionBeforeShow = eventSystem.currentSelectedGameObject;

            ActiveScopes.Remove(this);
            ActiveScopes.Add(this);
            RefreshScopeLocks();
            ResetScrollTracking();

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
            ResetScrollTracking();
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
            if (_virtualPointerActive)
                return;

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
            if (_virtualPointerActive)
                return;

            EnsureSelection();

            TrackSelectionIntoView();
        }

        /// <summary>
        /// 스펙 §15.3: 동적 리스트를 게임패드로 훑을 때 선택 항목이 뷰포트 밖으로 나가지 않게
        /// 선택을 감싸는 ScrollRect를 따라 움직인다.
        /// 포인터 드래그와 싸우지 않도록 선택이 실제로 바뀐 뒤에만 추적한다.
        /// </summary>
        private void TrackSelectionIntoView()
        {
            if (!_autoScrollToSelection)
            {
                ResetScrollTracking();
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject selection = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selection == null || !IsSelectionInside(selection))
            {
                ResetScrollTracking();
                return;
            }

            bool selectionChanged = selection != _lastTrackedSelection;
            if (selectionChanged)
            {
                _lastTrackedSelection = selection;
                ClearScrollTarget();

                ScrollRect scrollRect = selection.GetComponentInParent<ScrollRect>();
                var target = selection.transform as RectTransform;
                if (scrollRect == null
                    || scrollRect.content == null
                    || scrollRect.viewport == null
                    || target == null)
                {
                    return;
                }

                Vector2 normalized = CalculateNormalizedPositionFor(
                    scrollRect,
                    target,
                    _scrollPadding);
                if (_scrollLerpSpeed <= 0f)
                {
                    ApplyNormalizedPosition(scrollRect, normalized);
                    return;
                }

                Vector2 current = scrollRect.normalizedPosition;
                if ((current - normalized).sqrMagnitude <= 0.000001f)
                    return;

                _trackedScrollRect = scrollRect;
                _scrollTargetNormalized = normalized;
                _lastAppliedScrollNormalized = current;
                _hasScrollTarget = true;
            }

            if (!_hasScrollTarget || _trackedScrollRect == null)
                return;

            Vector2 currentPosition = _trackedScrollRect.normalizedPosition;
            // 자동 보간 중 사용자가 휠/드래그로 위치를 바꾸면 사용자 입력을 우선한다.
            if ((currentPosition - _lastAppliedScrollNormalized).sqrMagnitude > 0.000004f)
            {
                ClearScrollTarget();
                return;
            }

            float t = Mathf.Clamp01(Time.unscaledDeltaTime * _scrollLerpSpeed);
            Vector2 next = Vector2.Lerp(currentPosition, _scrollTargetNormalized, t);
            if ((next - _scrollTargetNormalized).sqrMagnitude <= 0.000001f)
            {
                next = _scrollTargetNormalized;
                ApplyNormalizedPosition(_trackedScrollRect, next);
                ClearScrollTarget();
                return;
            }

            ApplyNormalizedPosition(_trackedScrollRect, next);
            _lastAppliedScrollNormalized = next;
        }

        private void ResetScrollTracking()
        {
            _lastTrackedSelection = null;
            ClearScrollTarget();
        }

        private void ClearScrollTarget()
        {
            _trackedScrollRect = null;
            _hasScrollTarget = false;
        }

        private static void ApplyNormalizedPosition(ScrollRect scrollRect, Vector2 normalized)
        {
            if (scrollRect.horizontal)
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(normalized.x);
            if (scrollRect.vertical)
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized.y);
        }

        /// <summary>
        /// 항목이 뷰포트 안(여백 포함)에 들어오는 최소 이동량만큼만 스크롤 목표를 계산한다.
        /// 이미 보이는 항목은 현재 위치를 그대로 돌려준다.
        /// </summary>
        public static Vector2 CalculateNormalizedPositionFor(
            ScrollRect scrollRect,
            RectTransform target,
            float padding)
        {
            RectTransform viewport = scrollRect.viewport;
            RectTransform content = scrollRect.content;
            Vector2 result = scrollRect.normalizedPosition;

            Rect viewRect = viewport.rect;
            Bounds targetBounds = TransformBoundsTo(viewport, target);

            float scrollableX = content.rect.width - viewRect.width;
            float scrollableY = content.rect.height - viewRect.height;

            if (scrollRect.horizontal && scrollableX > 0.001f)
            {
                float left = targetBounds.min.x - (viewRect.xMin + padding);
                float right = targetBounds.max.x - (viewRect.xMax - padding);
                float deltaX = 0f;
                if (left < 0f) deltaX = left;
                else if (right > 0f) deltaX = right;

                // content가 왼쪽으로 밀리면 normalizedPosition.x는 커진다.
                result.x = Mathf.Clamp01(result.x + deltaX / scrollableX);
            }

            if (scrollRect.vertical && scrollableY > 0.001f)
            {
                float bottom = targetBounds.min.y - (viewRect.yMin + padding);
                float top = targetBounds.max.y - (viewRect.yMax - padding);
                float deltaY = 0f;
                if (bottom < 0f) deltaY = bottom;
                else if (top > 0f) deltaY = top;

                // 항목이 위로 벗어나면 normalizedPosition을 올려 Content를 아래로,
                // 아래로 벗어나면 내려 Content를 위로 이동한다.
                result.y = Mathf.Clamp01(result.y + deltaY / scrollableY);
            }

            return result;
        }

        // 매 프레임 호출되므로 코너 버퍼는 재사용한다.
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        private static Bounds TransformBoundsTo(RectTransform space, RectTransform target)
        {
            target.GetWorldCorners(CornerBuffer);

            var bounds = new Bounds(space.InverseTransformPoint(CornerBuffer[0]), Vector3.zero);
            for (int i = 1; i < 4; i++)
                bounds.Encapsulate(space.InverseTransformPoint(CornerBuffer[i]));

            return bounds;
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
                || _virtualPointerActive
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

    /// <summary>
    /// 화면 구조가 명확한 메뉴에서 Unity Automatic Navigation 대신
    /// 유효한 Selectable만 연결하는 공통 explicit navigation 유틸리티.
    /// 동적 목록은 재구축 직후 다시 호출한다.
    /// </summary>
    public static class UIFocusNavigation
    {
        public static void ConfigureVertical(IEnumerable<Selectable> source, bool wrap = false) =>
            ConfigureLinear(source, vertical: true, wrap);

        public static void ConfigureHorizontal(IEnumerable<Selectable> source, bool wrap = false) =>
            ConfigureLinear(source, vertical: false, wrap);

        public static void ConfigureGrid(IEnumerable<Selectable> source, int columns)
        {
            if (source == null)
                return;

            List<Selectable> items = source
                .Where(IsNavigable)
                .Distinct()
                .ToList();
            int safeColumns = Mathf.Max(1, columns);

            for (int i = 0; i < items.Count; i++)
            {
                int column = i % safeColumns;
                Navigation navigation = items[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = column > 0 ? items[i - 1] : null;
                navigation.selectOnRight = column + 1 < safeColumns && i + 1 < items.Count
                    ? items[i + 1]
                    : null;
                navigation.selectOnUp = i - safeColumns >= 0
                    ? items[i - safeColumns]
                    : null;
                navigation.selectOnDown = i + safeColumns < items.Count
                    ? items[i + safeColumns]
                    : null;
                items[i].navigation = navigation;
            }
        }

        private static void ConfigureLinear(
            IEnumerable<Selectable> source,
            bool vertical,
            bool wrap)
        {
            if (source == null)
                return;

            List<Selectable> items = source
                .Where(IsNavigable)
                .Distinct()
                .ToList();

            for (int i = 0; i < items.Count; i++)
            {
                Selectable previous = i > 0
                    ? items[i - 1]
                    : wrap && items.Count > 1 ? items[^1] : null;
                Selectable next = i + 1 < items.Count
                    ? items[i + 1]
                    : wrap && items.Count > 1 ? items[0] : null;

                Navigation navigation = items[i].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = vertical ? previous : null;
                navigation.selectOnDown = vertical ? next : null;
                navigation.selectOnLeft = vertical ? null : previous;
                navigation.selectOnRight = vertical ? null : next;
                items[i].navigation = navigation;
            }
        }

        public static Selectable FirstNavigable(params Selectable[] items)
        {
            if (items == null)
                return null;

            for (int i = 0; i < items.Length; i++)
            {
                if (IsNavigable(items[i]))
                    return items[i];
            }

            return null;
        }

        public static bool IsNavigable(Selectable selectable) =>
            selectable != null
            && selectable.gameObject.activeInHierarchy
            && selectable.IsActive()
            && selectable.IsInteractable();

        /// <summary>
        /// 동적 ContentSizeFitter가 높이를 갱신한 뒤에도 목록 진입 시작점을 최상단으로 고정한다.
        /// 관성 속도까지 제거해 이전 프레임의 드래그/포커스 추적이 위치를 다시 밀지 않게 한다.
        /// </summary>
        public static void ResetScrollToTop(ScrollRect scrollRect)
        {
            if (scrollRect == null || !scrollRect.vertical)
                return;

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
