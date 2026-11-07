using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 공중 공격 횟수가 한도에 도달했는지 판정. AirCircle 루프 종료 신호로 사용된다.
    /// </summary>
    public class IsAirAttackLimitReachedNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.AirAttackCount >= context.AirAttackLimit
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
