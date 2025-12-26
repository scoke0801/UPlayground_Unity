// IsDodgePressedConditionSO.cs
using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Cond_IsDodgePressed", menuName = "UP/FSM/Conditions/Is Dodge Pressed")]
    public class IsDodgePressedConditionSO : TransitionConditionSO
    {
        public override bool CheckCondition(CharacterBrain brain)
        {
            // CharacterBrain의 공개된 속성(InputDirection, IsDodgePressed 등)을 확인합니다.
            return brain.IsDodgePressed; 
        }
    }
}