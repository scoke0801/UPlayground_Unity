using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BlackboardStringConditionNode : BTConditionNode
    {
        [SerializeField] private string _key;
        [SerializeField] private string _expectedValue;
        [SerializeField] private bool _ignoreCase = true;

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public string ExpectedValue
        {
            get => _expectedValue;
            set => _expectedValue = value;
        }

        public bool IgnoreCase
        {
            get => _ignoreCase;
            set => _ignoreCase = value;
        }

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null || !Context.Blackboard.TryGetString(_key, out var value))
                return BTStatus.Failure;

            var comparison = _ignoreCase
                ? System.StringComparison.OrdinalIgnoreCase
                : System.StringComparison.Ordinal;
            var success = string.Equals(value, _expectedValue, comparison);
            var status = success ? BTStatus.Success : BTStatus.Failure;
            Context.DebugTrace?.Record(this, "BlackboardRead", status, $"{_key}={value}, expected={_expectedValue}");
            return status;
        }
    }
}
