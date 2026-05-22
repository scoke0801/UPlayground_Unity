using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.AI.BehaviorTree
{
    public class SyncEnemyBlackboardNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Failure;

            var detection = Context.GetComponentCached<EnemyDetection>();
            var controller = Context.GetComponentCached<ActorMovementController>();
            var snapshot = TargetStateBlackboardSnapshot.From(detection, controller);
            snapshot.WriteTo(Context.Blackboard);

            Context.DebugTrace?.Record(
                this,
                "BlackboardWrite",
                BTStatus.Success,
                snapshot.ToDebugString());
            return BTStatus.Success;
        }
    }
}
