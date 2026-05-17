using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 카운터(지상 체류/공격 횟수, 공중 공격 횟수) 전체 초기화.
    /// EnemyFlyingAIController.ResetAllCounters 등가.
    /// </summary>
    public class ResetFlyingCountersNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            context.ResetAllCounters();
            return BTStatus.Success;
        }
    }
}
