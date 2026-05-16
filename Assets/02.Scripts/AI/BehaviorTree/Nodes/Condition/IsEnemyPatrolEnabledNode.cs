using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsEnemyPatrolEnabledNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.EnablePatrol ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
