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
            if (state == null)
                return false;

            return (state.StateTags & ActorStateTag.InterruptLocked) == ActorStateTag.InterruptLocked
                   || state.BlocksBehaviorTree;
        }
    }

}
