using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Chase", fileName = "BTAction_Chase")]
    public class BTAction_ChaseSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.GetString(BBKey.CurrentStateName) == "Chase") return NodeStatus.Success;
                b.Runner.TriggerChase();
                return NodeStatus.Success;
            });
    }
}
