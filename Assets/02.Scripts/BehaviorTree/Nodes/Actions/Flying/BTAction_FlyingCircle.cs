using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/Action/Flying/FlyingCircle", fileName = "BTAction_FlyingCircle")]
    public class BTAction_FlyingCircleSO : BTNodeSO
    {
        [Min(0.1f)] public float durationMin = 0.8f;
        [Min(0.1f)] public float durationMax = 2.0f;

        protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
        {
            float dMin = durationMin;
            float dMax = durationMax;
            return new BTLeaf(nodeName, b =>
            {
                b.FlyingRunner?.TriggerFlyingCircle(Random.Range(dMin, dMax));
                return NodeStatus.Success;
            });
        }
    }
}
