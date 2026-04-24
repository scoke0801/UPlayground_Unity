using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/FlyingGroundAttack", fileName = "BTAction_FlyingGroundAttack")]
    public class BTAction_FlyingGroundAttackSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerFlyingGroundAttack();
                return NodeStatus.Success;
            });
        }
    }
}
