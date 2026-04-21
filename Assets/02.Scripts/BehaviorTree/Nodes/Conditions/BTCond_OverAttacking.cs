using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/OverAttacking", fileName = "BTCond_OverAttacking")]
    public class BTCond_OverAttackingSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Memory == null) return NodeStatus.Failure;
                return b.Memory.IsOverAttacking(b.GetInt(BBKey.PhaseMaxConsecutiveAttacks))
                    ? NodeStatus.Success
                    : NodeStatus.Failure;
            });
    }
}
