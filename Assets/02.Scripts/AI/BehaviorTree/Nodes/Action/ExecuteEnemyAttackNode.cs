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
            var brain = Context.GetComponentCached<EnemyBrain>();
            var detection = Context.GetComponentCached<EnemyDetection>();
            if (combat == null || brain == null || detection == null || !detection.HasTarget)
                return BTStatus.Failure;

            if (!combat.HasAvailableSkillAtDistance(detection.DistanceToTarget))
                return BTStatus.Failure;

            controller.TransitionToState(new EnemyAttackState(controller, combat, brain, detection));
            return BTStatus.Running;
        }
    }
}
