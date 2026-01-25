using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.GameActor.MovementController;
using UPlayGround.InputDefine;

namespace UPlayGround.GameActor
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PlayerActor : Base.GameActor<PlayerMovementController>
    {
        private Vector2 _currentMoveInput;
        private Camera _camera;
        
        private bool _jumpRequest;
        
        #region Mono
        protected override void Awake()
        {
            base.Awake();
            
            _camera = Camera.main;
            
            if (InputManager.Instance)
            {
                InputLayer layer = InputLayer.Level_0;
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    OnMoveInput, OnMoveInput, OnMoveInput, null, OnMoveCanceled, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,
                    null, OnJumpInput, null, null, null, layer);
            }
        }

        private void OnDestroy()
        {
            if (InputManager.Instance)
            {
                InputManager.Instance.UnRegisterInputEvent(
                    InputMapNames.PlayerAction, PlayerAction.Move,
                    OnMoveInput, OnMoveInput, OnMoveInput);
                
                InputManager.Instance.UnRegisterInputEvent(
                    InputMapNames.PlayerAction, PlayerAction.Jump,
                    null, OnJumpInput, null);
            }
        }
        
        private void Update()
        {
            if (movementController == null) return;

            // CameraManager를 통해 현재 메인 카메라의 회전값을 가져옴
            Quaternion cameraRotation = Quaternion.identity;
            if (_camera != null)
            {
                cameraRotation = _camera.transform.rotation;
            }

            // 이동 입력과 카메라 회전값을 함께 전달
            movementController.SetInputs(_currentMoveInput, cameraRotation, _jumpRequest);
            
            // 전달 후 점프 요청 초기화 (한 프레임만 유효)
            _jumpRequest = false;
        }
        #endregion

    }

    // Input 처리
    public partial class PlayerActor : Base.GameActor<PlayerMovementController>
    {
        #region InputCallback
        private void OnMoveInput(InputAction.CallbackContext obj)
        {
            _currentMoveInput = obj.ReadValue<Vector2>();
        }
        
        private void OnMoveCanceled()
        {
            _currentMoveInput = Vector2.zero;
            movementController.SetInputs(Vector2.zero, Quaternion.identity, _jumpRequest);
        }
        
        private void OnJumpInput(InputAction.CallbackContext obj)
        {
            // 버튼이 눌린 순간(Started/Performed)에 true 설정
            if (obj.performed)
            {
                _jumpRequest = true;
            }
        }
        #endregion
    }
}