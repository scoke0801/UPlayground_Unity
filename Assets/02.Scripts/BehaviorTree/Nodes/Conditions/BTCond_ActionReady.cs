using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/ActionReady", fileName = "BTCond_ActionReady")]
    public class BTCond_ActionReadySO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b => b.IsActionReady ? NodeStatus.Success : NodeStatus.Failure);
    }
}
