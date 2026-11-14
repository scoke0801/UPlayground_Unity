using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>실제 가드 파괴 통지의 유지 기간만 읽는다.</summary>
    public sealed class RecentlyGuardBrokenNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
            => Context?.GetComponentCached<EnemyTacticalMemory>()?.WasGuardBrokenRecently() == true
                ? BTStatus.Success : BTStatus.Failure;
    }
}
