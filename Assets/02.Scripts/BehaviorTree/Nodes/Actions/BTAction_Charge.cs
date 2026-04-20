using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Charge", fileName = "BTAction_Charge")]
    public class BTAction_ChargeSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.CurrentStateName == "Charge") return NodeStatus.Success;
                b.Runner.TriggerCharge();
                return NodeStatus.Success;
            });
    }
}
