using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 공간 좌표를 직접 가리켜야 하는 UI에서만 활성화하는 게임패드 가상 커서.
    /// 오른쪽 스틱이 처음 움직일 때 UINavigation에서 포인터 모드로 전환하고,
    /// 왼쪽 스틱/D-Pad Navigate 입력이 들어오면 기존 선택 포커스로 복귀한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIVirtualCursorController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private RectTransform _cursorVisual;
        [SerializeField] private RectTransform _movementArea;
        [SerializeField] private Canvas _canvas;

        [Header("이동")]
        [SerializeField, Min(1f)] private float _speed = 900f;
        [SerializeField, Range(0f, 0.95f)] private float _deadZone = 0.2f;
        [SerializeField, Min(0f)] private float _edgeThreshold = 48f;

        private readonly List<RaycastResult> _raycastResults = new(16);
        private IInputService _inputService;
        private InputAction _moveAction;
        private UIFocusScope _focusScope;
        private PointerEventData _pointerEventData;
        private GameObject _hoverTarget;
        private GameObject _navigationSelection;
        private Vector2 _lastScreenPosition;
        private InputLayer _inputLayer;
        private bool _isActive;
        private bool _isPointerMode;

        /// <summary>
        /// 커서가 이동 영역 가장자리에서 바깥 방향으로 밀릴 때 방향 벡터를 발행한다.
        /// 지도처럼 뷰 자체를 패닝하는 호스트가 선택적으로 사용한다.
        /// </summary>
        public event Action<Vector2> OnEdgeMoveRequested;

        public bool IsPointerMode => _isPointerMode;

        /// <summary>Builder가 생성한 참조를 연결한다.</summary>
        public void Configure(RectTransform cursorVisual, RectTransform movementArea, Canvas canvas)
        {
            _cursorVisual = cursorVisual;
            _movementArea = movementArea;
            _canvas = canvas;
            HideVisual();
        }

        public void Activate(InputLayer inputLayer)
        {
            if (_isActive)
                return;

            _inputService = Svc.Input;
            if (_inputService == null || _cursorVisual == null || _movementArea == null)
            {
                HideVisual();
                return;
            }

            _inputLayer = inputLayer;
            _moveAction = _inputService.GetAction(InputMapNames.UI, UIAction.VirtualCursorMove);
            _focusScope = GetComponent<UIFocusScope>();
            _pointerEventData = EventSystem.current != null
                ? new PointerEventData(EventSystem.current) { pointerId = -100 }
                : null;

            _inputService.RegisterInputEvent(
                InputMapNames.UI,
                UIAction.Submit,
                null,
                OnSubmit,
                null,
                CanHandlePointerInput,
                null,
                _inputLayer);
            _inputService.RegisterInputEvent(
                InputMapNames.UI,
                UIAction.Navigate,
                null,
                OnNavigate,
                null,
                null,
                null,
                _inputLayer);

            _isActive = true;
            ExitPointerMode(restoreSelection: false);
            CenterCursor();
        }

        public void Deactivate()
        {
            if (!_isActive)
            {
                HideVisual();
                return;
            }

            _inputService?.UnRegisterInputEvent(
                InputMapNames.UI,
                UIAction.Submit,
                null,
                OnSubmit,
                null);
            _inputService?.UnRegisterInputEvent(
                InputMapNames.UI,
                UIAction.Navigate,
                null,
                OnNavigate,
                null);

            ExitPointerMode(restoreSelection: false);
            _isActive = false;
            _inputService = null;
            _moveAction = null;
            _pointerEventData = null;
            _navigationSelection = null;
        }

        /// <summary>
        /// 호스트 UI가 확인 팝업처럼 UINavigation 전용 오버레이를 열 때 호출한다.
        /// </summary>
        public void ReturnToNavigation()
        {
            if (_isPointerMode)
                ExitPointerMode(restoreSelection: true);
        }

        private void Update()
        {
            if (!_isActive
                || _inputService == null
                || _inputService.ActiveDevice != ActiveInputDevice.Gamepad
                || _moveAction == null)
            {
                if (_isPointerMode)
                    ExitPointerMode(restoreSelection: true);
                return;
            }

            Vector2 input = _moveAction.ReadValue<Vector2>();
            if (input.sqrMagnitude <= _deadZone * _deadZone)
            {
                if (_isPointerMode)
                    UpdateHover();
                return;
            }

            if (!_isPointerMode)
                EnterPointerMode();

            MoveCursor(input);
            RequestEdgeMove(input);
            UpdateHover();
        }

        private void EnterPointerMode()
        {
            if (_isPointerMode)
                return;

            _isPointerMode = true;
            _navigationSelection = EventSystem.current?.currentSelectedGameObject;
            _focusScope ??= GetComponent<UIFocusScope>();
            _focusScope?.SetVirtualPointerActive(true);
            EventSystem.current?.SetSelectedGameObject(null);

            if (_cursorVisual != null)
                _cursorVisual.gameObject.SetActive(true);
            UpdatePointerPosition();
            UpdateHover();
        }

        private void ExitPointerMode(bool restoreSelection)
        {
            if (_hoverTarget != null && _pointerEventData != null)
                ExecuteEvents.Execute(_hoverTarget, _pointerEventData, ExecuteEvents.pointerExitHandler);
            _hoverTarget = null;

            _isPointerMode = false;
            HideVisual();
            _focusScope?.SetVirtualPointerActive(false);

            if (restoreSelection
                && _navigationSelection != null
                && _navigationSelection.activeInHierarchy
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_navigationSelection);
            }
        }

        private void MoveCursor(Vector2 input)
        {
            Rect rect = _movementArea.rect;
            Vector2 halfCursor = _cursorVisual.rect.size * 0.5f;
            Vector2 position = _cursorVisual.anchoredPosition
                               + input * (_speed * Time.unscaledDeltaTime);

            position.x = Mathf.Clamp(
                position.x,
                rect.xMin + halfCursor.x,
                rect.xMax - halfCursor.x);
            position.y = Mathf.Clamp(
                position.y,
                rect.yMin + halfCursor.y,
                rect.yMax - halfCursor.y);

            _cursorVisual.anchoredPosition = position;
            UpdatePointerPosition();
        }

        private void RequestEdgeMove(Vector2 input)
        {
            Rect rect = _movementArea.rect;
            Vector2 position = _cursorVisual.anchoredPosition;
            Vector2 direction = Vector2.zero;

            if (input.x < 0f && position.x <= rect.xMin + _edgeThreshold)
                direction.x = input.x;
            else if (input.x > 0f && position.x >= rect.xMax - _edgeThreshold)
                direction.x = input.x;

            if (input.y < 0f && position.y <= rect.yMin + _edgeThreshold)
                direction.y = input.y;
            else if (input.y > 0f && position.y >= rect.yMax - _edgeThreshold)
                direction.y = input.y;

            if (direction.sqrMagnitude > 0f)
                OnEdgeMoveRequested?.Invoke(Vector2.ClampMagnitude(direction, 1f));
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            if (!CanHandlePointerInput())
                return;

            UpdateHover();
            GameObject clickTarget = FindRaycastHandler<IPointerClickHandler>(out RaycastResult raycast);
            if (clickTarget == null)
                return;

            _pointerEventData.button = PointerEventData.InputButton.Left;
            _pointerEventData.pointerPressRaycast = raycast;
            ExecuteEvents.Execute(clickTarget, _pointerEventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(clickTarget, _pointerEventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(clickTarget, _pointerEventData, ExecuteEvents.pointerClickHandler);
        }

        private void OnNavigate(InputAction.CallbackContext context)
        {
            if (!_isPointerMode)
                return;

            Vector2 navigate = context.ReadValue<Vector2>();
            if (navigate.sqrMagnitude > _deadZone * _deadZone)
                ExitPointerMode(restoreSelection: true);
        }

        private bool CanHandlePointerInput() =>
            _isActive
            && _isPointerMode
            && _inputService?.ActiveDevice == ActiveInputDevice.Gamepad
            && _pointerEventData != null;

        private void UpdateHover()
        {
            if (_pointerEventData == null)
                return;

            UpdatePointerPosition();
            GameObject next = FindRaycastHandler<IPointerEnterHandler>(out _);
            if (next == _hoverTarget)
                return;

            if (_hoverTarget != null)
                ExecuteEvents.Execute(_hoverTarget, _pointerEventData, ExecuteEvents.pointerExitHandler);
            _hoverTarget = next;
            if (_hoverTarget != null)
                ExecuteEvents.Execute(_hoverTarget, _pointerEventData, ExecuteEvents.pointerEnterHandler);
        }

        private GameObject FindRaycastHandler<T>(out RaycastResult selectedRaycast)
            where T : IEventSystemHandler
        {
            selectedRaycast = default;
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || _pointerEventData == null)
                return null;

            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _raycastResults);
            foreach (RaycastResult result in _raycastResults)
            {
                if (result.gameObject == null
                    || !IsInsideMovementArea(result.gameObject.transform))
                {
                    continue;
                }

                GameObject handler = ExecuteEvents.GetEventHandler<T>(result.gameObject);
                if (handler == null || !IsInsideMovementArea(handler.transform))
                    continue;

                selectedRaycast = result;
                return handler;
            }

            return null;
        }

        private bool IsInsideMovementArea(Transform target) =>
            target != null
            && _movementArea != null
            && (target == _movementArea || target.IsChildOf(_movementArea));

        private void UpdatePointerPosition()
        {
            if (_cursorVisual == null || _pointerEventData == null)
                return;

            Camera camera = _canvas != null ? _canvas.worldCamera : null;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, _cursorVisual.position);
            _pointerEventData.delta = screenPosition - _lastScreenPosition;
            _pointerEventData.position = screenPosition;
            _lastScreenPosition = screenPosition;
        }

        private void CenterCursor()
        {
            if (_cursorVisual == null)
                return;

            _cursorVisual.anchorMin = new Vector2(0.5f, 0.5f);
            _cursorVisual.anchorMax = new Vector2(0.5f, 0.5f);
            _cursorVisual.anchoredPosition = Vector2.zero;
            UpdatePointerPosition();
        }

        private void HideVisual()
        {
            if (_cursorVisual != null)
                _cursorVisual.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_isActive)
                Deactivate();
        }
    }
}
