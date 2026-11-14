using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>점유·해제·후보 등록 없이 현재 슬롯 가용성을 조회한다.</summary>
    public sealed class HasAttackSlotNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var ground = Context?.GetComponentCached<EnemyAIContext>();
            bool available = ground != null ? ground.CanRequestAttackSlot()
                : Context?.GetComponentCached<EnemyFlyingAIContext>()?.CanRequestAttackSlot() == true;
            return available ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
