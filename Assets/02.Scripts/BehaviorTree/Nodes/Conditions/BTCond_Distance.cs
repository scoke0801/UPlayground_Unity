using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    public enum DistanceCheck { LessThan, GreaterThan, Between }

    [CreateAssetMenu(menuName = "BehaviorTree/Condition/Distance", fileName = "BTCond_Distance")]
    public class BTCond_DistanceSO : BTNodeSO
    {
        public DistanceCheck check = DistanceCheck.LessThan;
        [Min(0f)] public float minDistance = 0f;
        [Min(0f)] public float maxDistance = 3f;

        protected override BTNode CreateRuntimeNode(EnemyBlackboard bb)
        {
            float min = minDistance;
            float max = maxDistance;
            var   c   = check;
            return new BTLeaf(nodeName, b =>
            {
                float d = b.DistanceToTarget;
                bool pass = c switch
                {
                    DistanceCheck.LessThan    => d < max,
                    DistanceCheck.GreaterThan => d > min,
                    DistanceCheck.Between     => d >= min && d <= max,
                    _                         => false
                };
                return pass ? NodeStatus.Success : NodeStatus.Failure;
            });
        }
    }
}
