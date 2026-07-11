using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 가중치 기반 dive 스킬을 골라 Combat.CurrentSkill에 설정한다.
    /// 이어지는 TransitionFlyingEnemyStateNode(Dive)에서 해당 스킬을 사용한다.
    /// 선택 가능한 스킬이 없으면 Failure를 반환하므로 Sequence가 차단되어 Land 분기로 떨어진다.
    /// </summary>
    public class SelectFlyingDiveSkillNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.SelectAndSetDiveSkill() ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
