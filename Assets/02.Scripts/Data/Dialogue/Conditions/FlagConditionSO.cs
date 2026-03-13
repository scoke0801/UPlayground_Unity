using UnityEngine;

namespace Dialogue
{
    [CreateAssetMenu(menuName = "Dialogue/Condition/Flag", fileName = "Cond_Flag_")]
    public class FlagConditionSO : ConditionSO
    {
        public string flagKey;
        public bool expectedValue = true;

        public override bool Evaluate()
        {
            return GlobalFlagManager.Instance.GetFlag(flagKey) == expectedValue;
        }
    }
}
