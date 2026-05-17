using UPlayGround.MovementController;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsBlockedEnemyStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            return IsBlockedState(controller?.CurrentState?.StateName) ? BTStatus.Success : BTStatus.Failure;
        }

        public static bool IsBlockedState(string stateName)
        {
            return stateName is "Death" or "Hit" or "Grabbed" or "Airborne" or "Attack" or "Counter" or "Dodge"
                or "Land" or "TakeOff" or "Aerial" or "AerialAttack"
                or "Flying_TakeOff" or "Flying_GroundAttack" or "Flying_Dive" or "Flying_Land";
        }
    }
}
