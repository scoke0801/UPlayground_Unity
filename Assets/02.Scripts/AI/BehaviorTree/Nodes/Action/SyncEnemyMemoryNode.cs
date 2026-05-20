using UPlayGround.Component;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// SyncEnemyMemoryService의 1회 Tick 버전 Action. JSON으로 만든 BT가 Service 부착 없이도
    /// IsPlayerAttacking / IsPlayerGuarding / IsPlayerStaggered / IsPlayerRecovering /
    /// IsPlayerDodgingFrequently / RecentlyHitByPlayer / RecentHitCount / LastHitReactionType /
    /// PoiseRatio / IsPoiseBroken 키를 Blackboard에 채울 수 있도록 한다.
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
            return BTStatus.Success;
        }
    }
}
