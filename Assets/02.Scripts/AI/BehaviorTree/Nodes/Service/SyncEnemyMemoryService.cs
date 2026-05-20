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
            var isAttackingFrequently = memory != null && memory.IsPlayerAttackingFrequently();
            var isGuardingFrequently = memory != null && memory.IsPlayerGuardingFrequently();
            var isRecoveringFrequently = memory != null && memory.IsPlayerRecoveringFrequently();
            var dodgeCount = memory?.PlayerDodgeCount ?? 0;
            var guardCount = memory?.PlayerGuardCount ?? 0;
            var attackCount = memory?.PlayerAttackCount ?? 0;
            var recoverCount = memory?.PlayerRecoverCount ?? 0;
            var poise = Context.GetComponentCached<PoiseStat>();
            var wasHitRecently = memory != null && memory.WasHitRecently();
            var recentHitCount = memory?.RecentHitCount ?? 0;
            var lastHitReactionType = memory?.LastHitReactionType.ToString() ?? "";
            var poiseRatio = poise != null ? poise.PoisePercent : 1f;
            var isPoiseBroken = poise != null && poise.IsPoiseBroken;

            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttacking, isAttacking);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuarding, isGuarding);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerStaggered, isStaggered);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerRecovering, isRecovering);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerDodgingFrequently, isDodgingFrequently);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttackingFrequently, isAttackingFrequently);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuardingFrequently, isGuardingFrequently);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPlayerRecoveringFrequently, isRecoveringFrequently);
            Context.Blackboard.SetInt(EnemyBlackboardKeys.PlayerDodgeCount, dodgeCount);
            Context.Blackboard.SetInt(EnemyBlackboardKeys.PlayerGuardCount, guardCount);
            Context.Blackboard.SetInt(EnemyBlackboardKeys.PlayerAttackCount, attackCount);
            Context.Blackboard.SetInt(EnemyBlackboardKeys.PlayerRecoverCount, recoverCount);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.RecentlyHitByPlayer, wasHitRecently);
            Context.Blackboard.SetInt(EnemyBlackboardKeys.RecentHitCount, recentHitCount);
            Context.Blackboard.SetString(EnemyBlackboardKeys.LastHitReactionType, lastHitReactionType);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.PoiseRatio, poiseRatio);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.IsPoiseBroken, isPoiseBroken);

            Context.DebugTrace?.Record(
                this,
                "MemoryWrite",
                BTStatus.Success,
                $"Attacking={isAttacking}, Guarding={isGuarding}, Staggered={isStaggered}, Recovering={isRecovering}, DodgeFreq={isDodgingFrequently}, AttackFreq={isAttackingFrequently}, GuardFreq={isGuardingFrequently}, RecoverFreq={isRecoveringFrequently}, Counts D/G/A/R={dodgeCount}/{guardCount}/{attackCount}/{recoverCount}, HitRecently={wasHitRecently}, PoiseBroken={isPoiseBroken}");
        }
    }
}
