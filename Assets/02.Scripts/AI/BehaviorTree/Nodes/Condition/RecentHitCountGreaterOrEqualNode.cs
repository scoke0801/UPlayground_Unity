using UPlayGround.Components;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class RecentHitCountGreaterOrEqualNode : BTConditionNode
    {
        [SerializeField] private int _threshold = 2;

        public int Threshold
        {
            get => _threshold;
            set => _threshold = Mathf.Max(1, value);
        }

        protected override BTStatus OnUpdate()
        {
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            return memory != null && memory.IsRecentHitCountGreaterOrEqual(_threshold)
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
