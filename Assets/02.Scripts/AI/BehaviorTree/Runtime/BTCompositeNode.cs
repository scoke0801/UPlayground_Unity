namespace UPlayGround.AI.BehaviorTree
{
    public abstract class BTCompositeNode : BTNode
    {
        protected void AbortRunningChildren(BTNode except = null)
        {
            foreach (var child in Children)
            {
                if (child == null || child == except || !child.IsStarted)
                    continue;

                child.Abort();
            }
        }

        protected bool TryEvaluateSelfAbort(int runningIndex, out BTConditionNode changedCondition)
        {
            changedCondition = null;
            if (runningIndex < 0)
                return false;

            var endIndex = System.Math.Min(runningIndex, Children.Count - 1);
            for (var i = 0; i <= endIndex; i++)
            {
                foreach (var condition in EnumerateConditions(Children[i]))
                {
                    if (condition.Disabled)
                        continue;

                    if (!condition.EvaluateAbortChanged(out _))
                        continue;

                    changedCondition = condition;
                    return true;
                }
            }

            return false;
        }

        protected bool TryEvaluateLowerPriorityAbort(int runningIndex, out BTConditionNode changedCondition)
        {
            changedCondition = null;
            if (runningIndex <= 0)
                return false;

            for (var i = 0; i < runningIndex && i < Children.Count; i++)
            {
                foreach (var condition in EnumerateConditions(Children[i]))
                {
                    if (condition.Disabled)
                        continue;

                    if (!condition.EvaluateAbortChanged(out _))
                        continue;

                    changedCondition = condition;
                    return true;
                }
            }

            return false;
        }

        private static System.Collections.Generic.IEnumerable<BTConditionNode> EnumerateConditions(BTNode node)
        {
            if (node == null)
                yield break;

            if (node is BTConditionNode condition)
                yield return condition;

            foreach (var child in node.Children)
            {
                foreach (var nested in EnumerateConditions(child))
                    yield return nested;
            }
        }
    }
}
