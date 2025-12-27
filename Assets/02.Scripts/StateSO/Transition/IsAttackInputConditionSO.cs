// IsAttackInputConditionSO.cs
using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Cond_IsAttackInput", menuName = "UP/FSM/Conditions/Is Attack Input")]
    public class IsAttackInputConditionSO : TransitionConditionSO
    {
        public AttackInputType RequiredType = AttackInputType.Light;

        public override bool CheckCondition(CharacterBrain brain)
        {
            // 요구되는 공격 타입과 현재 입력된 공격 타입을 비교합니다.
            return brain.AttackInput == RequiredType;
        }
    }
}