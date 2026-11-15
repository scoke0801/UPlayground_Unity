using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    public class RequestEnemyAttackSlotNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Context?.GetComponentCached<UPlayGround.MovementController.ActorMovementController>()?.Actor
                is MonsterActor { IsPetDistractionActive: true }) return BTStatus.Failure;
            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context == null)
                return BTStatus.Success;

            var result = context.TryRequestAttackSlot();
            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, result);
            return result ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
