using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class HasEnemyActionDelayElapsedNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Success;

            return Context.Blackboard.TryGetFloat(EnemyBlackboardKeys.NextActionAllowedTime, out var nextAllowedTime)
                && Time.time < nextAllowedTime
                    ? BTStatus.Failure
                    : BTStatus.Success;
        }
    }
}
