using UPlayGround.Component;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class HasTargetNode : BTConditionNode
    {
        [SerializeField] private bool _expectedValue = true;

        public bool ExpectedValue
        {
            get => _expectedValue;
            set => _expectedValue = value;
        }

        protected override BTStatus OnUpdate()
        {
            var detection = Context?.GetComponentCached<EnemyDetection>();
            if (detection == null)
                return BTStatus.Failure;

            return detection.HasTarget == _expectedValue ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
