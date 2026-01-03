// IsFallingConditionSO.cs (신규 TransitionConditionSO)
using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Cond_IsFalling", menuName = "UP/FSM/Conditions/Is Falling")]
    public class IsFallingConditionSO : TransitionConditionSO
    {
        public override bool CheckCondition(CharacterBrain brain)
        {
            // 땅에 닿지 않았고 (점프/낙하 중), 하강 중일 때 (수직 속도가 음수)
            return !brain.IsGrounded() && brain.Motor.Velocity.y < 0; 
        }
    }
}