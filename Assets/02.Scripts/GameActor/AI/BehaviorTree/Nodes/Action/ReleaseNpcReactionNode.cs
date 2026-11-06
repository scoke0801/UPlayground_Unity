using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>위협이 사라진 뒤 프로파일의 유지 시간이 지나면 반응을 해제한다. 유지 중에는 Running.</summary>
    public class ReleaseNpcReactionNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var reaction = Context?.GetComponentCached<NpcCombatReaction>();
            if (reaction == null) return BTStatus.Failure;
            return reaction.TryRelease() ? BTStatus.Success : BTStatus.Running;
        }
    }
}
