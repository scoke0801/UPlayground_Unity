using UPlayGround.Components;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// EnemyTacticalMemory의 플레이어 상태 관찰 결과를 Blackboard에 동기화한다.
    /// Phase 4 전술 반응 분기(Memory.Player.* 등)에 사용하는 bool/Int/String 키를 채운다.
    /// Memory 컴포넌트가 없으면 모든 키를 false/0/빈 문자열로 유지한다.
    /// </summary>
    public class SyncEnemyMemoryService : BTServiceNode
    {
        protected override void OnServiceTick()
        {
            if (Context?.Blackboard == null)
                return;

            var memory = Context.GetComponentCached<EnemyTacticalMemory>();
            var poise = Context.GetComponentCached<PoiseStat>();
            var snapshot = EnemyMemoryBlackboardSnapshot.From(memory, poise);
            snapshot.WriteTo(Context.Blackboard);

            Context.DebugTrace?.Record(
                this,
                "MemoryWrite",
                BTStatus.Success,
                snapshot.ToDebugString());
        }
    }

    internal readonly struct EnemyMemoryBlackboardSnapshot
    {
        private readonly PlayerReadSnapshot _playerRead;
        private readonly HitMemorySnapshot _hitMemory;
        private readonly PoiseSnapshot _poise;

        private EnemyMemoryBlackboardSnapshot(
            PlayerReadSnapshot playerRead,
            HitMemorySnapshot hitMemory,
            PoiseSnapshot poise)
        {
            _playerRead = playerRead;
            _hitMemory = hitMemory;
            _poise = poise;
        }

        public static EnemyMemoryBlackboardSnapshot From(EnemyTacticalMemory memory, PoiseStat poise)
        {
            return new EnemyMemoryBlackboardSnapshot(
                PlayerReadSnapshot.From(memory),
                HitMemorySnapshot.From(memory),
                PoiseSnapshot.From(poise));
        }

        public void WriteTo(Blackboard blackboard)
        {
            _playerRead.WriteTo(blackboard);
            _hitMemory.WriteTo(blackboard);
            _poise.WriteTo(blackboard);
        }

        public string ToDebugString()
        {
            return $"{_playerRead.ToDebugString()}, {_hitMemory.ToDebugString()}, {_poise.ToDebugString()}";
        }
    }

    internal readonly struct PlayerReadSnapshot
    {
        private readonly bool _isAttacking;
        private readonly bool _isGuarding;
        private readonly bool _isStaggered;
        private readonly bool _isRecovering;
        private readonly bool _isDodgingFrequently;
        private readonly bool _isAttackingFrequently;
        private readonly bool _isGuardingFrequently;
        private readonly bool _isRecoveringFrequently;
        private readonly int _dodgeCount;
        private readonly int _guardCount;
        private readonly int _attackCount;
        private readonly int _recoverCount;

        private PlayerReadSnapshot(EnemyTacticalMemory memory)
        {
            _isAttacking = memory != null && memory.IsPlayerAttacking();
            _isGuarding = memory != null && memory.IsPlayerGuarding();
            _isStaggered = memory != null && memory.IsPlayerStaggered();
            _isRecovering = memory != null && memory.IsPlayerRecovering();
            _isDodgingFrequently = memory != null && memory.IsPlayerDodgingFrequently();
            _isAttackingFrequently = memory != null && memory.IsPlayerAttackingFrequently();
            _isGuardingFrequently = memory != null && memory.IsPlayerGuardingFrequently();
            _isRecoveringFrequently = memory != null && memory.IsPlayerRecoveringFrequently();
            _dodgeCount = memory?.PlayerDodgeCount ?? 0;
            _guardCount = memory?.PlayerGuardCount ?? 0;
            _attackCount = memory?.PlayerAttackCount ?? 0;
            _recoverCount = memory?.PlayerRecoverCount ?? 0;
        }

        public static PlayerReadSnapshot From(EnemyTacticalMemory memory)
        {
            return new PlayerReadSnapshot(memory);
        }

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetBool(blackboard, _isAttacking, EnemyBlackboardKeys.MemoryPlayerIsAttacking);
            BlackboardWriteUtility.SetBool(blackboard, _isGuarding, EnemyBlackboardKeys.MemoryPlayerIsGuarding);
            BlackboardWriteUtility.SetBool(blackboard, _isStaggered, EnemyBlackboardKeys.MemoryPlayerIsStaggered);
            BlackboardWriteUtility.SetBool(blackboard, _isRecovering, EnemyBlackboardKeys.MemoryPlayerIsRecovering);
            BlackboardWriteUtility.SetBool(blackboard, _isDodgingFrequently, EnemyBlackboardKeys.MemoryPlayerIsDodgingFrequently);
            BlackboardWriteUtility.SetBool(blackboard, _isAttackingFrequently, EnemyBlackboardKeys.MemoryPlayerIsAttackingFrequently);
            BlackboardWriteUtility.SetBool(blackboard, _isGuardingFrequently, EnemyBlackboardKeys.MemoryPlayerIsGuardingFrequently);
            BlackboardWriteUtility.SetBool(blackboard, _isRecoveringFrequently, EnemyBlackboardKeys.MemoryPlayerIsRecoveringFrequently);
            BlackboardWriteUtility.SetInt(blackboard, _dodgeCount, EnemyBlackboardKeys.MemoryPlayerDodgeCount);
            BlackboardWriteUtility.SetInt(blackboard, _guardCount, EnemyBlackboardKeys.MemoryPlayerGuardCount);
            BlackboardWriteUtility.SetInt(blackboard, _attackCount, EnemyBlackboardKeys.MemoryPlayerAttackCount);
            BlackboardWriteUtility.SetInt(blackboard, _recoverCount, EnemyBlackboardKeys.MemoryPlayerRecoverCount);
        }

        public string ToDebugString()
        {
            return $"PlayerRead A/G/S/R={_isAttacking}/{_isGuarding}/{_isStaggered}/{_isRecovering}, Freq D/A/G/R={_isDodgingFrequently}/{_isAttackingFrequently}/{_isGuardingFrequently}/{_isRecoveringFrequently}, Counts D/G/A/R={_dodgeCount}/{_guardCount}/{_attackCount}/{_recoverCount}";
        }
    }

    internal readonly struct HitMemorySnapshot
    {
        private readonly bool _wasHitRecently;
        private readonly int _recentHitCount;
        private readonly string _lastHitReactionType;

        private HitMemorySnapshot(EnemyTacticalMemory memory)
        {
            _wasHitRecently = memory != null && memory.WasHitRecently();
            _recentHitCount = memory?.RecentHitCount ?? 0;
            _lastHitReactionType = memory?.LastHitReactionType.ToString() ?? "";
        }

        public static HitMemorySnapshot From(EnemyTacticalMemory memory)
        {
            return new HitMemorySnapshot(memory);
        }

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetBool(blackboard, _wasHitRecently, EnemyBlackboardKeys.MemoryHitRecentlyByPlayer);
            BlackboardWriteUtility.SetInt(blackboard, _recentHitCount, EnemyBlackboardKeys.MemoryHitRecentCount);
            BlackboardWriteUtility.SetString(blackboard, _lastHitReactionType, EnemyBlackboardKeys.MemoryHitLastReactionType);
        }

        public string ToDebugString()
        {
            return $"HitMemory Recent={_wasHitRecently}, Count={_recentHitCount}, Last={_lastHitReactionType}";
        }
    }

    internal readonly struct PoiseSnapshot
    {
        private readonly float _poiseRatio;
        private readonly bool _isPoiseBroken;

        private PoiseSnapshot(PoiseStat poise)
        {
            _poiseRatio = poise != null ? poise.PoisePercent : 1f;
            _isPoiseBroken = poise != null && poise.IsPoiseBroken;
        }

        public static PoiseSnapshot From(PoiseStat poise)
        {
            return new PoiseSnapshot(poise);
        }

        public void WriteTo(Blackboard blackboard)
        {
            BlackboardWriteUtility.SetFloat(blackboard, _poiseRatio, EnemyBlackboardKeys.SelfPoiseRatio);
            BlackboardWriteUtility.SetBool(blackboard, _isPoiseBroken, EnemyBlackboardKeys.SelfIsPoiseBroken);
        }

        public string ToDebugString()
        {
            return $"Poise Ratio={_poiseRatio:0.00}, Broken={_isPoiseBroken}";
        }
    }

    internal static class BlackboardWriteUtility
    {
        public static void SetBool(Blackboard blackboard, bool value, string key)
            => blackboard.SetBool(key, value);

        public static void SetInt(Blackboard blackboard, int value, string key)
            => blackboard.SetInt(key, value);

        public static void SetFloat(Blackboard blackboard, float value, string key)
            => blackboard.SetFloat(key, value);

        public static void SetString(Blackboard blackboard, string value, string key)
            => blackboard.SetString(key, value);

        public static void SetObject(Blackboard blackboard, UnityEngine.Object value, string key)
            => blackboard.SetObject(key, value);
    }
}
