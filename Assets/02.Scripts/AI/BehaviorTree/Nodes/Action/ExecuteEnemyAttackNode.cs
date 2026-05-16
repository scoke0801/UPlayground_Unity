using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.AI.BehaviorTree
{
    public class ExecuteEnemyAttackNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller == null)
                return BTStatus.Failure;

            if (controller.CurrentState?.StateName == "Attack")
                return BTStatus.Running;

            if (controller.CurrentState?.StateName is "Death" or "Hit" or "Grabbed" or "Airborne")
                return BTStatus.Failure;

            var combat = Context.GetComponentCached<EnemyCombat>();
            var context = Context.GetComponentCached<EnemyAIContext>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            if (combat == null || context == null || detection == null || !detection.HasTarget)
                return BTStatus.Failure;

            if (!combat.HasAvailableSkillAtDistance(detection.DistanceToTarget))
                return BTStatus.Failure;

            if (!context.TryRequestAttackSlot())
            {
                Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
                return BTStatus.Failure;
            }

            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, true);
            context.NotifyBTAttackStarted();
            controller.TransitionToState(new EnemyAttackState(controller, combat, context, detection));
            return BTStatus.Running;
        }
    }

    public class RequestEnemyAttackSlotNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context == null)
                return BTStatus.Success;

            var result = context.TryRequestAttackSlot();
            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, result);
            return result ? BTStatus.Success : BTStatus.Failure;
        }
    }

    public class KeepCurrentStateNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            return BTStatus.Running;
        }
    }
}
