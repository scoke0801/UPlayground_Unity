using UPlayGround.Component;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public enum IntComparisonType
    {
        LessThan,
        GreaterOrEqual
    }

    public class ConsecutiveAttackCountNode : BTConditionNode
    {
        [SerializeField] private IntComparisonType _comparison = IntComparisonType.LessThan;
        [SerializeField] private int _threshold = 2;

        public IntComparisonType Comparison
        {
            get => _comparison;
            set => _comparison = value;
        }

        public int Threshold
        {
            get => _threshold;
            set => _threshold = Mathf.Max(0, value);
        }

        protected override BTStatus OnUpdate()
        {
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            if (memory == null)
                return BTStatus.Failure;

            var count = memory.ConsecutiveAttackCount;
            var passed = _comparison switch
            {
                IntComparisonType.LessThan => count < _threshold,
                IntComparisonType.GreaterOrEqual => count >= _threshold,
                _ => false
            };

            return passed ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
