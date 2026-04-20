using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/HasTarget", fileName = "BTCond_HasTarget")]
    public class BTCond_HasTargetSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b => b.HasTarget ? NodeStatus.Success : NodeStatus.Failure);
    }
}
