using UPlayGround.MovementController;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsCurrentActorStateNode : BTConditionNode
    {
        [SerializeField] private string _stateName = "Idle";
        [SerializeField] private bool _expectedValue = true;

        public string StateName
        {
            get => _stateName;
            set => _stateName = value;
        }

        public bool ExpectedValue
        {
            get => _expectedValue;
            set => _expectedValue = value;
        }

        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            if (controller?.CurrentState == null)
                return BTStatus.Failure;

            var result = controller.CurrentState.StateName == _stateName;
            return result == _expectedValue ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
