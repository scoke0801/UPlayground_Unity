using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/FlyingPatrol", fileName = "BTAction_FlyingPatrol")]
    public class BTAction_FlyingPatrolSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerFlyingPatrol();
                return NodeStatus.Success;
            });
        }
    }
}
