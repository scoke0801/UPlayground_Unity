using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>캐릭터 프리뷰의 마우스 드래그와 게임패드 회전 입력을 처리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UICharacterPreviewInput : MonoBehaviour, IDragHandler
    {
        [SerializeField] private UICharacterPreviewRenderer _renderer;
        [SerializeField, Min(0f)] private float _pointerDegreesPerPixel = 0.35f;
        [SerializeField, Min(0f)] private float _gamepadDegreesPerSecond = 110f;
        [SerializeField, Range(0f, 0.95f)] private float _gamepadDeadZone = 0.2f;

        private IInputService _inputService;
        private InputAction _rotateAction;

        /// <summary>동적으로 생성된 프리뷰 입력 영역에 렌더러를 연결한다.</summary>
        public void Configure(UICharacterPreviewRenderer renderer)
        {
            _renderer = renderer;
        }

        private void OnEnable()
        {
            _inputService = Svc.Input;
            _rotateAction = _inputService?.GetAction(
                InputMapNames.UI,
                UIAction.VirtualCursorMove);
        }

        private void OnDisable()
        {
            _rotateAction = null;
            _inputService = null;
        }

        private void Update()
        {
            if (_renderer == null
                || !_renderer.IsPreviewVisible
                || _inputService?.ActiveDevice != ActiveInputDevice.Gamepad
                || _rotateAction == null)
            {
                return;
            }

            float horizontal = _rotateAction.ReadValue<Vector2>().x;
            if (Mathf.Abs(horizontal) <= _gamepadDeadZone)
                return;

            _renderer.RotateCharacter(
                -horizontal * _gamepadDegreesPerSecond * Time.unscaledDeltaTime);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_renderer == null
                || !_renderer.IsPreviewVisible
                || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _renderer.RotateCharacter(
                -eventData.delta.x * _pointerDegreesPerPixel);
        }
    }
}
