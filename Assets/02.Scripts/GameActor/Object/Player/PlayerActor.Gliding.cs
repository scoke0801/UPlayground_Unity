using UnityEngine.InputSystem;

namespace UPlayGround
{
    public partial class PlayerActor
    {
        private bool CanInputGlidingRestrictedAction() => PlayerController?.IsTraversing != true;

        private void OnInputPerformedSuperJump(InputAction.CallbackContext context)
        {
            PlayerMovementPlayerController?.TryStartSuperJump();
        }

        /// <summary>이동 벡터를 보존하고 활공 중 금지한 행동의 홀드·선입력을 버린다.</summary>
        public void ClearGlidingActionInputs()
        {
            var move = _currentMoveInput;
            ClearAllInputState();
            _currentMoveInput = move;
            PlayerMovementPlayerController?.ClearInputConditions();
            InputMgr?.InputBuffer?.Clear();
        }
    }
}
