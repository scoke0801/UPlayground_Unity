using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class TimeoutNode : BTDecoratorNode
    {
        [SerializeField] private float _timeout = 1f;

        private float _elapsed;

        protected override void OnStart()
        {
            _elapsed = 0f;
        }

        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            var status = Child.Tick();
            if (status != BTStatus.Running)
                return status;

            _elapsed += Time.deltaTime;
            if (_elapsed < Mathf.Max(0f, _timeout))
                return BTStatus.Running;

            AbortChild();
            return BTStatus.Failure;
        }

        protected override void OnReset()
        {
            _elapsed = 0f;
        }
    }
}
