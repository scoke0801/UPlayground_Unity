using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>관찰한 플레이어 체력 비율의 판정 결과를 읽는다.</summary>
    public sealed class IsPlayerLowHealthNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
            => Context?.GetComponentCached<EnemyTacticalMemory>()?.IsPlayerLowHealth == true
                ? BTStatus.Success : BTStatus.Failure;
    }
}
