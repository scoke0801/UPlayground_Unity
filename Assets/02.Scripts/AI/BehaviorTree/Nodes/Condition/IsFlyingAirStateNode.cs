using UPlayGround.MovementController;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 현재 State가 비행 공중 루프(Flying_AirCircle / Flying_TakeOff / Flying_Dive)인지 판정.
    /// </summary>
    public class IsFlyingAirStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            var state = controller?.CurrentState?.StateName;
            return state is "Flying_AirCircle" or "Flying_TakeOff" or "Flying_Dive"
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
