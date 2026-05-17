using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행 공중 공격 카운터만 초기화. TakeOff 직후 새 공중 루프 진입 시점.
    /// </summary>
    public class ResetFlyingAirCountersNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            context.ResetAirCounters();
            return BTStatus.Success;
        }
    }
}
