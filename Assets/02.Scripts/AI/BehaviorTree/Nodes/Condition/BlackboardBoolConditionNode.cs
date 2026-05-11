using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BlackboardBoolConditionNode : BTConditionNode
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _expectedValue = true;

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null || !Context.Blackboard.TryGetBool(_key, out var value))
                return BTStatus.Failure;

            var status = value == _expectedValue ? BTStatus.Success : BTStatus.Failure;
            Context.DebugTrace?.Record(this, "BlackboardRead", status, $"{_key}={value}, expected={_expectedValue}");
            return status;
        }
    }
}
