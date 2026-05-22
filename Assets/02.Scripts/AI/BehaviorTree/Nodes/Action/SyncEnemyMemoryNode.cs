using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// SyncEnemyMemoryService의 1회 Tick 버전 Action. JSON으로 만든 BT가 Service 부착 없이도
    /// Memory.Player.* / Memory.Hit.* / Self.PoiseRatio / Self.IsPoiseBroken 키를 Blackboard에 채울 수 있도록 한다.
    /// 전투 분기 진입 전에 Sequence 맨 앞에 두는 패턴을 권장.
    /// </summary>
    public class SyncEnemyMemoryNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Failure;

            var memory = Context.GetComponentCached<EnemyTacticalMemory>();
            var poise = Context.GetComponentCached<PoiseStat>();
            var snapshot = EnemyMemoryBlackboardSnapshot.From(memory, poise);
            snapshot.WriteTo(Context.Blackboard);

            Context.DebugTrace?.Record(
                this,
                "MemoryWrite",
                BTStatus.Success,
                snapshot.ToDebugString());
            return BTStatus.Success;
        }
    }
}
