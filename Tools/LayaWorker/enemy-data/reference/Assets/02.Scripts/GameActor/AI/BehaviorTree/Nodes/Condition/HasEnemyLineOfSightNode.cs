using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>현재 타깃까지 EnemyDetection 기준의 장애물 차폐가 없는지 검사한다.</summary>
    public sealed class HasEnemyLineOfSightNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            EnemyDetection detection = Context?.GetComponentCached<EnemyDetection>();
            return detection != null && detection.HasLineOfSightToCurrentTarget()
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
