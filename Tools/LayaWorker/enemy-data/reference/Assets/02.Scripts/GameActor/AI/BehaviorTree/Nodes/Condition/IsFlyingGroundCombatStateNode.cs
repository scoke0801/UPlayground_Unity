using UPlayGround.MovementController;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 현재 State가 비행 지상 전투(Flying_Chase/GroundAttack/Circle/Retreat)인지 판정.
    /// </summary>
    public class IsFlyingGroundCombatStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            var state = controller?.CurrentState?.StateName;
            return state is "Flying_Chase" or "Flying_GroundAttack" or "Flying_Circle" or "Flying_Retreat"
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
