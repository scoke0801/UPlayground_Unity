using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/Descend", fileName = "BTAction_Descend")]
    public class BTAction_DescendSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerDescend();
                return NodeStatus.Success;
            });
        }
    }
}
