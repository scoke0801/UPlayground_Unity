using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    public class CanIgnoreLightHitNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            var poise = Context?.GetComponentCached<PoiseStat>();
            return memory != null && memory.CanIgnoreLightHit(poise) ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
