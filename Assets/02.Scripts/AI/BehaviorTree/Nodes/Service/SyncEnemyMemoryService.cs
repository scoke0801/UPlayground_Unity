using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// EnemyTacticalMemory의 플레이어 상태 관찰 결과를 Blackboard에 동기화한다.
    /// Phase 4 전술 반응 분기(IsPlayerAttacking 등)에 사용하는 5개 bool 키를 채운다.
    /// Memory 컴포넌트가 없으면 모든 키를 false로 유지한다.
    /// </summary>
    public class SyncEnemyMemoryService : BTServiceNode
    {
        protected override void OnServiceTick()
        {
            if (Context?.Blackboard == null)
                return;

            var memory = Context.GetComponentCached<EnemyTacticalMemory>();
            var isAttacking = memory != null && memory.IsPlayerAttacking();
            var isGuarding = memory != null && memory.IsPlayerGuarding();
            var isStaggered = memory != null && memory.IsPlayerStaggered();
            var isRecovering = memory != null && memory.IsPlayerRecovering();
            var isDodgingFrequently = memory != null && memory.IsPlayerDodgingFrequently();

            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttacking, isAttacking);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuarding, isGuarding);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerStaggered, isStaggered);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerRecovering, isRecovering);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerDodgingFrequently, isDodgingFrequently);

            Context.DebugTrace?.Record(
                this,
                "MemoryWrite",
                BTStatus.Success,
                $"Attacking={isAttacking}, Guarding={isGuarding}, Staggered={isStaggered}, Recovering={isRecovering}, DodgingFreq={isDodgingFrequently}");
        }
    }
}
