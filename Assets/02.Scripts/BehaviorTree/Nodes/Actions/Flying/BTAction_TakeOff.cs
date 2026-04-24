using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/TakeOff", fileName = "BTAction_TakeOff")]
    public class BTAction_TakeOffSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerTakeOff();
                return NodeStatus.Success;
            });
        }
    }
}
