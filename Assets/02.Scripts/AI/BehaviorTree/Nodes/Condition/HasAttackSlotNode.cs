namespace UPlayGround.AI.BehaviorTree
{
    public class HasAttackSlotNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            return Context?.Blackboard != null
                   && Context.Blackboard.TryGetBool(EnemyBlackboardKeys.HasAttackSlot, out var hasSlot)
                   && hasSlot
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
