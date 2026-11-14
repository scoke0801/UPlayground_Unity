using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>관찰 주기에 계산된 타겟의 차지·시전 상태를 읽는다.</summary>
    public sealed class IsTargetCastingOrChargingNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
            => Context?.GetComponentCached<EnemyTacticalMemory>()?.IsTargetCastingOrCharging == true
                ? BTStatus.Success : BTStatus.Failure;
    }
}
