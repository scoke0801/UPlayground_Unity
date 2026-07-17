using UPlayGround.MovementController;

namespace UPlayGround.AI.BehaviorTree
{
    public class KeepCurrentStateNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            return IsBlockedEnemyStateNode.IsBlockedState(controller?.CurrentState)
                ? BTStatus.Running
                : BTStatus.Failure;
        }
    }
}
