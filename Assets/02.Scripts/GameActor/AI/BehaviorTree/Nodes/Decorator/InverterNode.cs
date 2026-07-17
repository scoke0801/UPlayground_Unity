namespace UPlayGround.AI.BehaviorTree
{
    public class InverterNode : BTDecoratorNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Child == null)
                return BTStatus.Failure;

            var status = Child.Tick();
            return status switch
            {
                BTStatus.Success => BTStatus.Failure,
                BTStatus.Failure => BTStatus.Success,
                _ => BTStatus.Running
            };
        }
    }
}
