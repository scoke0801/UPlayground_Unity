using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.GameActor.MovementController;
using UPlayGround.InputDefine;

namespace UPlayGround.GameActor
{
    /// <summary>
    /// 
    /// </summary>
    public class PlayerActor : Base.GameActor<PlayerMovementController>
    {
        private Vector2 _moveInput;
        
        #region Mono
        protected virtual void Awake()
        {
            base.Awake();
            if (InputManager.Instance)
            {
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    OnMoveInput, OnMoveInput, OnMoveInput, null, OnMoveCanceled, InputLayer.Level_0);
            }
        }

        private void OnDestroy()
        {
            if (InputManager.Instance)
            {
                InputManager.Instance.UnRegisterInputEvent(
                    InputMapNames.PlayerAction,
                    PlayerAction.Move,
                    OnMoveInput, OnMoveInput, OnMoveInput);
            }
        }
        // private void Update()
        // {
        //     // movementController.SetInp
        //     // 매 프레임 저장된 입력값으로 이동 처리
        //     if (_moveInput.sqrMagnitude > 0)
        //     {
        //         Move(_moveInput);
        //     }
        // }
        #endregion
        
        
        #region InputCallback
        private void OnMoveInput(InputAction.CallbackContext obj)
        {
            Vector2 inputMove = obj.ReadValue<Vector2>();
            
            // 컨트롤러에 입력값 전달
            if (movementController != null)
            {
                movementController.SetMoveInput(inputMove);
            }
        }
        
        private void OnMoveCanceled()
        {
            // 레이어 변경 등으로 인한 강제 중지 시 입력값 초기화
            if (movementController != null)
            {
                movementController.SetMoveInput(Vector2.zero);
            }
        }
        #endregion
    }

}