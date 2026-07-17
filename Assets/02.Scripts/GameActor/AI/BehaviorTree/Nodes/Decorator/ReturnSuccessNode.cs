namespace UPlayGround.AI.BehaviorTree
{
    public class ReturnSuccessNode : BTDecoratorNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            var status = Child.Tick();
            return status == BTStatus.Running ? BTStatus.Running : BTStatus.Success;
        }
    }
}
