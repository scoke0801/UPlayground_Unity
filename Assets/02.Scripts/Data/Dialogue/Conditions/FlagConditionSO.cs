using UnityEngine;

namespace UPlayGround.Dialogue
{
    [CreateAssetMenu(menuName = "UPlayGround/대화/조건/Flag", fileName = "Cond_Flag_")]
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
