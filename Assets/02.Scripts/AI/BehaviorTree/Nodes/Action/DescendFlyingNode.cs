using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 공중 루프 종료 시 Dive 또는 Land로 분기.
    /// EnemyFlyingAIController.TransitionToDescend의 데이터 기반 가중치 결정을 그대로 위임한다.
    /// </summary>
    public class DescendFlyingNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            context.TransitionToDescend();
            return BTStatus.Success;
        }
    }
}
