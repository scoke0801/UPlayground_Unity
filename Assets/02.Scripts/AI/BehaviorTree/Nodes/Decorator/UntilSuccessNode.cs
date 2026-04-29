namespace UPlayGround.AI.BehaviorTree
{
    public class UntilSuccessNode : BTDecoratorNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            var status = Child.Tick();
            if (status == BTStatus.Success)
                return BTStatus.Success;

            if (status == BTStatus.Failure)
                ResetChild();

            return BTStatus.Running;
        }
    }
}
