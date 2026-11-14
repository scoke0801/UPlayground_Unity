using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>관찰 주기에 계산된 타겟의 후방 여부를 읽는다.</summary>
    public sealed class IsTargetBehindNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
            => Context?.GetComponentCached<EnemyTacticalMemory>()?.IsTargetBehind == true
                ? BTStatus.Success : BTStatus.Failure;
    }
}
