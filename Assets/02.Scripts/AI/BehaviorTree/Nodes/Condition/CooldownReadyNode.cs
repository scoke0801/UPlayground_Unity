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

            var key = $"Cooldown.{_cooldownId}.ReadyTime";
            return !Context.Blackboard.TryGetFloat(key, out var readyTime) || Time.time >= readyTime
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
