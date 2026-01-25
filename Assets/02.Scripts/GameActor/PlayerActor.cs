using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;

namespace UPlayGround.GameActor
{
    /// <summary>
    /// 
    /// </summary>
    public class PlayerActor : Base.GameActor
    {
        private Vector2 _moveInput;
        
        #region Mono
        private void Awake()
        {
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
        private void Update()
        {
            // 매 프레임 저장된 입력값으로 이동 처리
            if (_moveInput.sqrMagnitude > 0)
            {
                Move(_moveInput);
            }
        }
        #endregion
        
        private void Move(Vector2 direction)
        {
            // 실제 이동 로직 구현
            transform.Translate(Time.deltaTime * 5f * direction );
        }
        
        #region InputCallback
        private void OnMoveInput(InputAction.CallbackContext obj)
        {
            Vector2 inputMove = obj.ReadValue<Vector2>();
            
            Debug.Log($"OnMoveInput : {inputMove}");
        }
        
        private void OnMoveCanceled()
        {
            Debug.Log("OnMoveCanceled");
        }
        #endregion
    }

}