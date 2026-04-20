using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Circle", fileName = "BTAction_Circle")]
    public class BTAction_CircleSO : BTNodeSO
    {
        [Min(0.1f)] public float minDuration = 1.0f;
        [Min(0.1f)] public float maxDuration = 2.5f;

        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
        {
            float min = minDuration;
            float max = maxDuration;
            return new BTLeaf(nodeName, b =>
            {
                if (b.Runner == null) return NodeStatus.Failure;
                if (b.CurrentStateName == "Circle") return NodeStatus.Success;
                b.Runner.TriggerCircle(Random.Range(min, max));
                return NodeStatus.Success;
            });
        }
    }
}
