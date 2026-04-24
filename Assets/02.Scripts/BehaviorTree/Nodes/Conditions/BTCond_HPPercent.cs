using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    public enum HPCheck { LessThan, GreaterThan }

    [CreateAssetMenu(menuName = "BehaviorTree/Condition/HPPercent", fileName = "BTCond_HPPercent")]
    public class BTCond_HPPercentSO : BTNodeSO
    {
        public HPCheck check = HPCheck.LessThan;
        [Range(0f, 1f)]
        [Tooltip("HP 비율 기준값 (0=0%, 1=100%)")]
        public float threshold = 0.5f;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            var   c = check;
            float t = threshold;
            return new BTLeaf(nodeName, b =>
            {
                float hp   = b.GetFloat(BBKey.SelfHPPercent, 1f);
                bool  pass = c == HPCheck.LessThan ? hp < t : hp > t;
                return pass ? NodeStatus.Success : NodeStatus.Failure;
            });
        }
    }
}
