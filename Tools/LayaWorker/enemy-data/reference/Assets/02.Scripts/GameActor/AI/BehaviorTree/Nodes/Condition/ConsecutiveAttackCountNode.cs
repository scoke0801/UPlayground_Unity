using UPlayGround.Components;
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
            // 기억이 없으면 "아직 아무것도 때리지 않은 상태"로 본다. 여기서 Failure를 돌려주면
            // 이 조건을 낀 공격 규칙이 전부 영구 실패해 AI가 조용히 멈춰버린다.
            // 연속 공격 억제(GreaterOrEqual)는 0으로 평가되어 자연히 발동하지 않는다.
            var memory = Context?.GetComponentCached<EnemyTacticalMemory>();
            var count = memory != null ? memory.ConsecutiveAttackCount : 0;
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
