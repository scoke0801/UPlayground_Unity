namespace UPlayGround.AI.BehaviorTree
{
    public abstract class BTDecoratorNode : BTNode
    {
        protected BTNode Child => Children.Count > 0 ? Children[0] : null;
    }
}
