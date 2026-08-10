using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 적의 루프 카운터를 Blackboard에 동기화한다.
    ///
    /// <see cref="SyncEnemyBlackboardService"/>는 Detection/State만 다루므로 비행 루프
    /// 카운터는 어디에서도 Blackboard에 올라가지 않았다. BlackboardCompare로 "공중 공격을
    /// 몇 발 했나" 같은 수치 분기를 짜려면 이 서비스가 필요하다.
    ///
    /// ■ 여기에 판정(predicate)을 추가하지 않는다
    /// 이륙 필요·급강하 가능·하강 요청 같은 술어는 이미 전용 조건 노드가 제공한다
    /// (ShouldFlyingTakeOffNode / HasDiveSkillAvailableNode / IsFlyingDescendRequestedNode).
    /// 조건 노드는 해당 분기에 도달할 때만 평가되지만 서비스는 매 틱 돈다.
    /// 특히 급강하 가능 판정은 AbilitySet 전체를 순회하며 Ability마다 활성화 평가를
    /// 수행하므로, 그걸 서비스로 옮기면 아무도 읽지 않을 수 있는 값을 위해 매 틱
    /// 전수 스캔을 돌리게 된다. 술어는 조건 노드에, 수치만 여기에 둔다.
    /// </summary>
    public class SyncFlyingEnemyBlackboardService : BTServiceNode
    {
        protected override void OnServiceTick()
        {
            if (Context?.Blackboard == null)
                return;

            var flying = Context.GetComponentCached<EnemyFlyingAIContext>();
            if (flying == null)
                return;

            var snapshot = FlyingCounterBlackboardSnapshot.From(flying);
            snapshot.WriteTo(Context.Blackboard);

            Context.DebugTrace?.Record(
                this,
                "FlyingCounterWrite",
                BTStatus.Success,
                snapshot.ToDebugString());
        }
    }

    internal readonly struct FlyingCounterBlackboardSnapshot
    {
        private readonly int _airAttackCount;
        private readonly int _airAttackLimit;
        private readonly float _groundTimer;
        private readonly int _groundAttackCount;

        private FlyingCounterBlackboardSnapshot(
            int airAttackCount,
            int airAttackLimit,
            float groundTimer,
            int groundAttackCount)
        {
            _airAttackCount = airAttackCount;
            _airAttackLimit = airAttackLimit;
            _groundTimer = groundTimer;
            _groundAttackCount = groundAttackCount;
        }

        /// <summary>전부 필드 읽기다. 여기에 계산이 필요한 값을 넣지 않는다.</summary>
        public static FlyingCounterBlackboardSnapshot From(EnemyFlyingAIContext flying)
            => new(
                flying.AirAttackCount,
                flying.AirAttackLimit,
                flying.GroundTimer,
                flying.GroundAttackCount);

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetInt(
                blackboard, _airAttackCount, EnemyBlackboardKeys.AirAttackCount);
            BlackboardWriteUtility.SetInt(
                blackboard, _airAttackLimit, EnemyBlackboardKeys.AirAttackLimit);
            BlackboardWriteUtility.SetFloat(
                blackboard, _groundTimer, EnemyBlackboardKeys.GroundTimer);
            BlackboardWriteUtility.SetInt(
                blackboard, _groundAttackCount, EnemyBlackboardKeys.GroundAttackCount);
        }

        public string ToDebugString()
            => $"{EnemyBlackboardKeys.AirAttackCount}={_airAttackCount}/"
               + $"{_airAttackLimit}, "
               + $"{EnemyBlackboardKeys.GroundTimer}={_groundTimer:0.00}, "
               + $"{EnemyBlackboardKeys.GroundAttackCount}={_groundAttackCount}";
    }
}
