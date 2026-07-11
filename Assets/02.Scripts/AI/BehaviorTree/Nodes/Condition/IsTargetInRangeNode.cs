using UPlayGround.Components;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class IsTargetInRangeNode : BTConditionNode
    {
        [SerializeField] private FloatComparisonType _comparison = FloatComparisonType.LessOrEqual;
        [SerializeField] private float _minDistance;
        [SerializeField] private float _maxDistance = 3f;

        public FloatComparisonType Comparison
        {
            get => _comparison;
            set => _comparison = value;
        }

        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }

        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        protected override BTStatus OnUpdate()
        {
            var detection = Context?.GetComponentCached<EnemyDetection>();
            if (detection == null || !detection.HasTarget)
                return BTStatus.Failure;

            var distance = detection.DistanceToTarget;
            var result = _comparison switch
            {
                FloatComparisonType.LessOrEqual => distance <= _maxDistance,
                FloatComparisonType.GreaterOrEqual => distance >= _minDistance,
                FloatComparisonType.Between => distance >= _minDistance && distance <= _maxDistance,
                _ => false
            };

            return result ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
