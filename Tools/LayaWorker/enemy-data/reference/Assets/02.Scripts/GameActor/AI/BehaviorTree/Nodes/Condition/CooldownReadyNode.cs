using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class CooldownReadyNode : BTConditionNode
    {
        [SerializeField] private string _cooldownId;

        public string CooldownId
        {
            get => _cooldownId;
            set => _cooldownId = value;
        }

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null || string.IsNullOrWhiteSpace(_cooldownId))
                return BTStatus.Success;

            var key = EnemyBlackboardKeys.CooldownReadyTime(_cooldownId);
            return !Context.Blackboard.TryGetRuntimeFloat(key, out var readyTime) || Time.time >= readyTime
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
