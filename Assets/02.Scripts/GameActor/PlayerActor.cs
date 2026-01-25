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
        private void Awake()
        {
            if (InputManager.Instance)
            {
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    null, OnMovePerformed, null, null, null, InputLayer.Level_0);
            }

        }

        private void OnDestroy()
        {
            if (InputManager.Instance)
            {
                InputManager.Instance.UnRegisterInputEvent(
                    InputMapNames.PlayerAction,
                    PlayerAction.Move,
                    null, OnMovePerformed, null);
            }
        }
        private void OnMovePerformed(InputAction.CallbackContext obj)
        {
            Vector2 inputMove = obj.ReadValue<Vector2>();
            
            Debug.Log($"OnMovePerformed : {inputMove}");
        }
    }

}