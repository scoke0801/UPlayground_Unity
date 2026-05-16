using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 현재 State가 비행 공중 루프(Flying_AirCircle / Flying_TakeOff / Flying_Dive)인지 판정.
    /// </summary>
    public class IsFlyingAirStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            var state = controller?.CurrentState?.StateName;
            return state is "Flying_AirCircle" or "Flying_TakeOff" or "Flying_Dive"
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }

    /// <summary>
    /// 현재 State가 비행 지상 전투(Flying_Chase/GroundAttack/Circle/Retreat)인지 판정.
    /// </summary>
    public class IsFlyingGroundCombatStateNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var controller = Context?.GetComponentCached<ActorMovementController>();
            var state = controller?.CurrentState?.StateName;
            return state is "Flying_Chase" or "Flying_GroundAttack" or "Flying_Circle" or "Flying_Retreat"
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }

    /// <summary>
    /// 공중 공격 횟수가 한도에 도달했는지 판정. AirCircle 루프 종료 신호로 사용된다.
    /// </summary>
    public class IsAirAttackLimitReachedNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.AirAttackCount >= context.AirAttackLimit
                ? BTStatus.Success
                : BTStatus.Failure;
        }
    }

    /// <summary>
    /// 지상 체류 한도/공격 한도에 도달해 이륙해야 하는지 판정.
    /// EnemyFlyingAIController.ShouldTakeOff와 동일한 정의를 Context로 위임.
    /// </summary>
    public class ShouldFlyingTakeOffNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.ShouldTakeOff() ? BTStatus.Success : BTStatus.Failure;
        }
    }

    /// <summary>
    /// dive 스킬(isDiveAttack)이 현재 레벨 기준으로 발사 가능한지 판정.
    /// </summary>
    public class HasDiveSkillAvailableNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return context.HasDiveSkillAvailable() ? BTStatus.Success : BTStatus.Failure;
        }
    }

    /// <summary>
    /// Random.value &lt; Context.DiveChance 1회 굴림. 하강 분기에서 Dive 시도 게이트로 사용된다.
    /// 매 OnUpdate마다 재굴림되므로 Conditional Abort 하위에 두지 말 것 — 관찰자에 의해 매 tick 결과가 흔들린다.
    /// 현재는 IsAirAttackLimitReached가 1회 트리거인 시점에서만 평가되므로 안전.
    /// </summary>
    public class RollDiveChanceNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            return UnityEngine.Random.value < context.DiveChance ? BTStatus.Success : BTStatus.Failure;
        }
    }

    /// <summary>
    /// 비행형 CanUseSkill. 글로벌 쿨다운 + 사거리 보유 스킬 존재 여부.
    /// 지상 <see cref="CanUseEnemySkillNode"/>의 비행 버전.
    /// </summary>
    public class FlyingCanUseSkillNode : BTConditionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyFlyingAIContext>();
            if (context == null)
                return BTStatus.Failure;

            if (!context.CanUseSkill())
                return BTStatus.Failure;

            var combat = context.Combat;
            if (combat?.AttackData == null)
                return BTStatus.Failure;

            var detection = context.Detection;
            var distance = detection != null && detection.HasTarget
                ? detection.DistanceToTarget
                : float.MaxValue;

            return combat.HasAvailableSkillAtDistance(distance) ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
