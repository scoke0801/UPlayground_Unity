using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class SelectorNode : BTCompositeNode
    {
        [SerializeField] private BTAbortType _abortType = BTAbortType.None;

        private int _currentIndex;

        public BTAbortType AbortType
        {
            get => _abortType;
            set => _abortType = value;
        }

        protected override void OnStart()
        {
            _currentIndex = 0;
        }

        protected override BTStatus OnUpdate()
        {
            if (Children.Count == 0)
                return BTStatus.Failure;

            while (_currentIndex < Children.Count)
            {
                var child = Children[_currentIndex];
                if (child == null)
                {
                    _currentIndex++;
                    continue;
                }

                var status = child.Tick();
                if (status == BTStatus.Running)
                    return BTStatus.Running;

                if (status == BTStatus.Success)
                    return BTStatus.Success;

                _currentIndex++;
            }

            return BTStatus.Failure;
        }

        protected override void OnReset()
        {
            _currentIndex = 0;
        }
    }
}
