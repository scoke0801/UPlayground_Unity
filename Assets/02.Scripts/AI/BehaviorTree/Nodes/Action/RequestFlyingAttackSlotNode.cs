using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 공격 슬롯 요청. EnemyFlyingAIContext의 그룹 슬롯 정책에 위임.
    /// </summary>
    public class RequestFlyingAttackSlotNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Success;

            var result = context.TryRequestAttackSlot();
            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, result);
            return result ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
