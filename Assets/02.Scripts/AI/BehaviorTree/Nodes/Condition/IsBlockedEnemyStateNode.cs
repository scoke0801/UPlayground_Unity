using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsBlockedEnemyStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            return IsBlockedState(controller?.CurrentState) ? BTStatus.Success : BTStatus.Failure;
        }

        public static bool IsBlockedState(GameActorState state)
        {
            return state?.BlocksBehaviorTree == true;
        }
    }
}
