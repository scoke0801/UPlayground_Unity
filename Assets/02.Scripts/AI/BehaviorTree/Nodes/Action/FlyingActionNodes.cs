using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 카운터(지상 체류/공격 횟수, 공중 공격 횟수) 전체 초기화.
    /// EnemyFlyingAIController.ResetAllCounters 등가.
    /// </summary>
    public class ResetFlyingCountersNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            context.ResetAllCounters();
            return BTStatus.Success;
        }
    }

    /// <summary>
    /// 비행 공중 공격 카운터만 초기화. TakeOff 직후 새 공중 루프 진입 시점.
    /// </summary>
    public class ResetFlyingAirCountersNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            context.ResetAirCounters();
            return BTStatus.Success;
        }
    }

    /// <summary>
    /// 공중 루프 종료 시 Dive 또는 Land로 분기.
    /// EnemyFlyingAIController.TransitionToDescend의 데이터 기반 가중치 결정을 그대로 위임한다.
    /// </summary>
    public class DescendFlyingNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            context.TransitionToDescend();
            return BTStatus.Success;
        }
    }

    /// <summary>
    /// 가중치 기반 dive 스킬을 골라 Combat.CurrentSkill에 설정한다.
    /// 이어지는 <see cref="TransitionFlyingEnemyStateNode"/>(Dive)에서 해당 스킬을 사용한다.
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

    /// <summary>
    /// 비행형 공격 슬롯 요청. EnemyFlyingAIContext의 그룹 슬롯 정책에 위임.
    /// </summary>
    public class RequestFlyingAttackSlotNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Success;

            var result = context.TryRequestAttackSlot();
            Context?.Blackboard?.SetBool(EnemyBlackboardKeys.HasAttackSlot, result);
            return result ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
