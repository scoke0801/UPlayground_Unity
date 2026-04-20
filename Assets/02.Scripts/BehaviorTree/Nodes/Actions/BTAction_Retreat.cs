using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Retreat", fileName = "BTAction_Retreat")]
    public class BTAction_RetreatSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                b.Runner.TriggerRetreat();
                return NodeStatus.Success;
            });
    }
}
