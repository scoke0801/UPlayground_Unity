using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/ConsecutiveDefense", fileName = "BTCond_ConsecutiveDefense")]
    public class BTCond_ConsecutiveDefenseSO : BTNodeSO
    {
        [Tooltip("연속 방어 횟수 한계. 이 값 이상이면 Success.")]
        [Min(1)] public int maxStreak = 2;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            int limit = maxStreak;
            return new BTLeaf(nodeName, b =>
                b.GetInt(BBKey.ConsecutiveDefensiveCount) >= limit
                    ? NodeStatus.Success
                    : NodeStatus.Failure);
        }
    }
}
