using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 쿨다운과 비용을 무시하고 현재 거리를 커버하는 AI 공격 정의가 있는지 검사한다.
    /// 접근 여부를 판단할 때 사용하며, 실제 공격 직전에는 CanUseEnemySkillNode를 별도로 검사해야 한다.
    /// </summary>
    public sealed class HasEnemyAttackInRangeNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            EnemyCombat combat = Context?.GetComponentCached<EnemyCombat>();
            EnemyDetection detection = Context?.GetComponentCached<EnemyDetection>();
            EnemyAIContext aiContext = Context?.GetComponentCached<EnemyAIContext>();
            if (combat?.AbilitySet == null || detection == null || !detection.HasTarget)
                return BTStatus.Failure;

            return EnemyAttackRangePolicy.HasAttackInRange(
                combat.AbilitySet,
                detection.DistanceToTarget,
                combat.CurrentLevel,
                useMeleeApproachRange: true,
                personalSpaceDistance: aiContext?.PersonalSpaceDistance
                                       ?? EnemyAttackRangePolicy.DefaultPersonalSpaceDistance)
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
