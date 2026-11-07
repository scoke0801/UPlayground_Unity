using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 CanUseSkill. Ability 활성화 조건과 사거리 충족 여부.
    /// </summary>
    public class FlyingCanUseSkillNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            if (!context.CanUseSkill())
                return BTStatus.Failure;

            var combat = context.Combat;
            if (combat?.AbilitySet == null)
                return BTStatus.Failure;

            var detection = context.Detection;
            var distance = detection != null && detection.HasTarget
                ? detection.DistanceToTarget
                : float.MaxValue;

            return combat.HasAvailableSkillAtDistance(distance) ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
