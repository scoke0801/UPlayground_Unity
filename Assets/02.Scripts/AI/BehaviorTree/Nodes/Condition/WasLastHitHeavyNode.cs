using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    public class WasLastHitHeavyNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            return memory != null && memory.WasLastHitHeavy() ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
