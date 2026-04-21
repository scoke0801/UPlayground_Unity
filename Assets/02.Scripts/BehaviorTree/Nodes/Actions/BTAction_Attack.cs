using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Attack", fileName = "BTAction_Attack")]
    public class BTAction_AttackSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                b.Runner.TriggerAttack();
                return NodeStatus.Success;
            });
    }
}
