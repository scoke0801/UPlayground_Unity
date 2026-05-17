namespace UPlayGround.AI.BehaviorTree
{
    public class KeepCurrentStateNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            return BTStatus.Running;
        }
    }
}
