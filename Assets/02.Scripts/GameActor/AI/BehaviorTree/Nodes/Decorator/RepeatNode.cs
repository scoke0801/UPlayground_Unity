using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class RepeatNode : BTDecoratorNode
    {
        [SerializeField] private int _repeatCount = 1;

        private int _currentCount;

        protected override void OnStart()
        {
            _currentCount = 0;
        }

        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            while (_repeatCount <= 0 || _currentCount < _repeatCount)
            {
                var status = Child.Tick();
                if (status == BTStatus.Running)
                    return BTStatus.Running;

                _currentCount++;
                Child.ResetNode();

                if (_repeatCount <= 0)
                    return BTStatus.Running;
            }

            return BTStatus.Success;
        }
    }
}
