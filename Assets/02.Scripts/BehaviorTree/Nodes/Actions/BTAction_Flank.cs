using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flank", fileName = "BTAction_Flank")]
    public class BTAction_FlankSO : BTNodeSO
    {
        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
            => new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.GetString(BBKey.CurrentStateName) == "Flank") return NodeStatus.Success;
                b.Runner.TriggerFlank();
                return NodeStatus.Success;
            });
    }
}
