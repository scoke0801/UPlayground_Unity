using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsEnemyPatrolEnabledNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var brain = Context?.GetComponentCached<EnemyBrain>();
            if (brain == null)
                return BTStatus.Failure;

            return brain.EnablePatrol ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
