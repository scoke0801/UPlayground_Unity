using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class ParallelNode : BTCompositeNode
    {
        [SerializeField] private bool _requireAllSuccess = true;

        private BTStatus[] _childStatuses;

        protected override void OnStart()
        {
            _childStatuses = new BTStatus[Children.Count];
            for (var i = 0; i < _childStatuses.Length; i++)
                _childStatuses[i] = BTStatus.Running;
        }

        protected override BTStatus OnUpdate()
        {
            if (Children.Count == 0)
                return BTStatus.Failure;

            var successCount = 0;
            var runningCount = 0;
            var validChildCount = 0;

            if (_childStatuses == null || _childStatuses.Length != Children.Count)
                OnStart();

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child == null)
                    continue;

                validChildCount++;

                var status = _childStatuses[i];
                if (status == BTStatus.Running)
                    status = child.Tick();

                if (status == BTStatus.Failure && _requireAllSuccess)
                {
                    _childStatuses[i] = status;
                    AbortRunningChildren(child);
                    return BTStatus.Failure;
                }

                if (status == BTStatus.Success)
                {
                    _childStatuses[i] = status;
                    successCount++;
                    if (!_requireAllSuccess)
                    {
                        AbortRunningChildren(child);
                        return BTStatus.Success;
                    }
                }
                else if (status == BTStatus.Running)
                {
                    _childStatuses[i] = status;
                    runningCount++;
                }
                else
                {
                    _childStatuses[i] = status;
                }
            }

            if (validChildCount == 0)
                return BTStatus.Failure;

            if (_requireAllSuccess)
                return successCount == validChildCount ? BTStatus.Success : BTStatus.Running;

            return runningCount > 0 ? BTStatus.Running : BTStatus.Failure;
        }

        protected override void OnStop()
        {
            AbortRunningChildren();
        }

        protected override void OnReset()
        {
            _childStatuses = null;
        }
    }
}
