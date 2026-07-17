namespace UPlayGround.AI.BehaviorTree
{
    public abstract class BTDecoratorNode : BTNode
    {
        protected BTNode Child => Children.Count > 0 ? Children[0] : null;

        protected void AbortChild()
        {
            if (Child != null && Child.IsStarted)
                Child.Abort();
        }

        protected void ResetChild()
        {
            Child?.ResetNode();
        }
    }
}
