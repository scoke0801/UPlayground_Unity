using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// dive 스킬(isDiveAttack)이 현재 레벨 기준으로 발사 가능한지 판정.
    /// </summary>
    public class HasDiveSkillAvailableNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.HasDiveSkillAvailable() ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
