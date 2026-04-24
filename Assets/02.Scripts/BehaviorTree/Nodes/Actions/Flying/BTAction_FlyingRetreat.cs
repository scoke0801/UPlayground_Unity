using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/FlyingRetreat", fileName = "BTAction_FlyingRetreat")]
    public class BTAction_FlyingRetreatSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerFlyingRetreat();
                return NodeStatus.Success;
            });
        }
    }
}
