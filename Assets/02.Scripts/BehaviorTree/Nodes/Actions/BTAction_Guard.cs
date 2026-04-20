using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Guard", fileName = "BTAction_Guard")]
    public class BTAction_GuardSO : BTNodeSO
    {
        [Min(0.1f)] public float minDuration = 0.8f;
        [Min(0.1f)] public float maxDuration = 1.5f;

        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
        {
            float min = minDuration;
            float max = maxDuration;
            return new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.CurrentStateName == "Guard") return NodeStatus.Success;
                b.Runner.TriggerGuard(Random.Range(min, max));
                return NodeStatus.Success;
            });
        }
    }
}
