using UPlayGround.Components;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class RequestFormationSlotNode : BTActionNode
    {
        [SerializeField] private float _radius = 3f;

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0.1f, value);
        }

        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context == null)
                return BTStatus.Success;

            return context.TryGetFormationSlotPosition(_radius, out _)
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }
}
