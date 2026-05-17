using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 지상 체류 한도/공격 한도에 도달해 이륙해야 하는지 판정.
    /// EnemyFlyingAIController.ShouldTakeOff와 동일한 정의를 Context로 위임.
    /// </summary>
    public class ShouldFlyingTakeOffNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.ShouldTakeOff() ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
