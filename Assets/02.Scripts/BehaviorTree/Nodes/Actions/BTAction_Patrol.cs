using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Patrol", fileName = "BTAction_Patrol")]
    public class BTAction_PatrolSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.CurrentStateName == "Patrol") return NodeStatus.Success;
                b.Runner.TriggerPatrol();
                return NodeStatus.Success;
            });
    }
}
