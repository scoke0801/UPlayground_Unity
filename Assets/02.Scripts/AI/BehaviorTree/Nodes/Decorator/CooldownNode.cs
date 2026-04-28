using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class CooldownNode : BTDecoratorNode
    {
        [SerializeField] private float _cooldown = 1f;

        private float _lastSuccessTime = -999f;

        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            if (Time.time - _lastSuccessTime < _cooldown)
                return BTStatus.Failure;

            var status = Child.Tick();
            if (status == BTStatus.Success)
                _lastSuccessTime = Time.time;

            return status;
        }
    }
}
