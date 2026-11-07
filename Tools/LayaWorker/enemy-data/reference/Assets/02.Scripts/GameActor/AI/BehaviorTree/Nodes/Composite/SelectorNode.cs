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

            if (TryHandleConditionalAbort())
                return BTStatus.Running;

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

        protected override void OnStop()
        {
            AbortRunningChildren();
            _currentIndex = 0;
        }

        private bool TryHandleConditionalAbort()
        {
            if (_currentIndex < 0 || _currentIndex >= Children.Count)
                return false;

            var runningChild = Children[_currentIndex];
            if (runningChild == null || !runningChild.IsStarted)
                return false;

            var selfAbort = _abortType == BTAbortType.Self || _abortType == BTAbortType.Both;
            var lowerPriorityAbort = _abortType == BTAbortType.LowerPriority || _abortType == BTAbortType.Both;

            if (selfAbort && TryEvaluateSelfAbort(_currentIndex, out var selfCondition))
            {
                runningChild.Abort();
                Context?.DebugTrace?.Record(this, "ConditionalAbort", BTStatus.Running, $"Self abort by {selfCondition.DisplayName}");
                _currentIndex = 0;
                return true;
            }

            if (lowerPriorityAbort && TryEvaluateLowerPriorityAbort(_currentIndex, out var priorityCondition))
            {
                runningChild.Abort();
                Context?.DebugTrace?.Record(this, "ConditionalAbort", BTStatus.Running, $"LowerPriority abort by {priorityCondition.DisplayName}");
                _currentIndex = 0;
                return true;
            }

            return false;
        }
    }
}
