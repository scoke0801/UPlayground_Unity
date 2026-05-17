using UPlayGround.MovementController;

namespace UPlayGround.AI.BehaviorTree
{
    public class KeepCurrentStateNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            return IsBlockedEnemyStateNode.IsBlockedState(controller?.CurrentState?.StateName)
                ? BTStatus.Running
                : BTStatus.Failure;
        }
    }
}
