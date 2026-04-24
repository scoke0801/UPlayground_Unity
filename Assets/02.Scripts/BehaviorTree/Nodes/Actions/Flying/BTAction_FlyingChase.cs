using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/FlyingChase", fileName = "BTAction_FlyingChase")]
    public class BTAction_FlyingChaseSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerFlyingChase();
                return NodeStatus.Success;
            });
        }
    }
}
