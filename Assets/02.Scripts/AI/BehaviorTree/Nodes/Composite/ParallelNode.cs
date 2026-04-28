using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class ParallelNode : BTCompositeNode
    {
        [SerializeField] private bool _requireAllSuccess = true;

        protected override BTStatus OnUpdate()
        {
            if (Children.Count == 0)
                return BTStatus.Failure;

            var successCount = 0;
            var runningCount = 0;

            foreach (var child in Children)
            {
                if (child == null)
                    continue;

                var status = child.Tick();
                if (status == BTStatus.Failure && _requireAllSuccess)
                    return BTStatus.Failure;

                if (status == BTStatus.Success)
                    successCount++;
                else if (status == BTStatus.Running)
                    runningCount++;
            }

            if (_requireAllSuccess)
                return successCount == Children.Count ? BTStatus.Success : BTStatus.Running;

            if (successCount > 0)
                return BTStatus.Success;

            return runningCount > 0 ? BTStatus.Running : BTStatus.Failure;
        }
    }
}
