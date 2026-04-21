using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Condition/HasTarget", fileName = "BTCond_HasTarget")]
    public class BTCond_HasTargetSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b => b.GetBool(BBKey.HasTarget) ? NodeStatus.Success : NodeStatus.Failure);
    }
}
