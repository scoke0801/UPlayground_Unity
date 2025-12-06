using UnityEngine;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "Cond_IsInteracting", menuName = "FSM/Conditions/Is Interacting")]
    public class InIteractingConditionSO : TransitionConditionSO
    {
        public override bool CheckCondition(CharacterBrain brain)
        {
            if (brain.IsInteractionPressed == false)
            {
                return false;
            }

            if (GameObjectManager.Instance.IsInteractionTargetExist() == false)
            {
                return false;
            }
            
            return true;
        }
    }
}