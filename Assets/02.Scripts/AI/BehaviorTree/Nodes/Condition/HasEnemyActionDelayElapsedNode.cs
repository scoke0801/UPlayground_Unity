using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class HasEnemyActionDelayElapsedNode : BTConditionNode
    {
        [SerializeField]
        private BlackboardKeySelector _nextActionAllowedTime =
            new("NextActionAllowedTime", BlackboardValueType.Float);

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Success;

            return Context.Blackboard.TryGetFloat(_nextActionAllowedTime, out var nextAllowedTime)
                && Time.time < nextAllowedTime
                    ? BTStatus.Failure
                    : BTStatus.Success;
        }
    }
}
