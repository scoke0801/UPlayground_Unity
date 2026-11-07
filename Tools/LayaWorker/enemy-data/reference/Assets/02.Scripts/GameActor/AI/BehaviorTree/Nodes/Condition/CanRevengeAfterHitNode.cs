using UPlayGround.Components;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class CanRevengeAfterHitNode : BTConditionNode
    {
        [SerializeField] private float _cooldown = 1.5f;

        public float Cooldown
        {
            get => _cooldown;
            set => _cooldown = Mathf.Max(0f, value);
        }

        protected override BTStatus OnUpdate()
        {
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            var poise = Context?.GetComponentCached<PoiseStat>();
            return memory != null && memory.CanRevengeAfterHit(poise, _cooldown)
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
