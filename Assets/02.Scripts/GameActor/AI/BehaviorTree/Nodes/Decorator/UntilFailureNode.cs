namespace UPlayGround.AI.BehaviorTree
{
    public class UntilFailureNode : BTDecoratorNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            var status = Child.Tick();
            if (status == BTStatus.Failure)
                return BTStatus.Success;

            if (status == BTStatus.Success)
                ResetChild();

            return BTStatus.Running;
        }
    }
}
