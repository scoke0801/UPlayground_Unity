// IsJumpPressedAndGroundedConditionSO.cs
using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Cond_IsJumpAndGrounded", menuName = "FSM/Conditions/Is Jump Pressed And Grounded")]
    public class IsJumpPressedAndGroundedConditionSO : TransitionConditionSO
    {
        public override bool CheckCondition(CharacterBrain brain)
        {
            // 점프 입력 상태와 착지 상태를 모두 확인합니다.
            return brain.IsJumpPressed && brain.IsGrounded();
        }
    }
}