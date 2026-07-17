using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsPoiseBrokenNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var poise = Context?.GetComponentCached<PoiseStat>();
            return poise != null && poise.IsPoiseBroken ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
