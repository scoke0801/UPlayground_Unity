using UPlayGround.MovementController;
using UPlayGround.State;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class HasStateTagNode : BTConditionNode
    {
        [SerializeField] private ActorStateTag _tag = ActorStateTag.InterruptLocked;
        [SerializeField] private bool _expectedValue = true;

        public ActorStateTag Tag
        {
            get => _tag;
            set => _tag = value;
        }

        public bool ExpectedValue
        {
            get => _expectedValue;
            set => _expectedValue = value;
        }

        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            var state = controller?.CurrentState;
            if (state == null)
                return BTStatus.Failure;

            var hasTag = (state.StateTags & _tag) == _tag;
            var status = hasTag == _expectedValue ? BTStatus.Success : BTStatus.Failure;
            Context.DebugTrace?.Record(this, "StateTag", status, $"{state.StateName}: {state.StateTags}, expected={_tag}:{_expectedValue}");
            return status;
        }
    }
}
