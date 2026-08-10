using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// AirCircle이 하강을 요청했는지 판정한다.
    ///
    /// <see cref="IsAirAttackLimitReachedNode"/>는 공격 횟수 소진만 잡는다. AirCircle은
    /// 체류 시간 초과로도 하강을 요청하는데 그 경로는 AirAttackCount를 올리지 않으므로,
    /// 횟수 조건만으로 분기하면 시간 초과 시 BT가 공중에 갇힌다.
    /// 두 경로 모두 이 노드로 합류한다.
    /// </summary>
    public class IsFlyingDescendRequestedNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.IsDescendRequested
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
