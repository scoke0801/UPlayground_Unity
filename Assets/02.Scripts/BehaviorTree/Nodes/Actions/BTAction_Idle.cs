using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Idle", fileName = "BTAction_Idle")]
    public class BTAction_IdleSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.GetString(BBKey.CurrentStateName) == "Idle") return NodeStatus.Success;
                b.Runner.TriggerIdle();
                return NodeStatus.Success;
            });
    }
}
