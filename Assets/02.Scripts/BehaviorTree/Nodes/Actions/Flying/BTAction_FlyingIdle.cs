using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/FlyingIdle", fileName = "BTAction_FlyingIdle")]
    public class BTAction_FlyingIdleSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerFlyingIdle();
                return NodeStatus.Success;
            });
        }
    }
}
