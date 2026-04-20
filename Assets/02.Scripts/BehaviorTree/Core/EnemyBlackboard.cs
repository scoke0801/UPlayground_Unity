using UnityEngine;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.BehaviorTree
{
    /// <summary>
    /// BT 실행 중 모든 노드가 공유하는 런타임 컨텍스트.
    /// BTRunner에서 매 틱 최신값으로 갱신한다.
    /// </summary>
    public class EnemyBlackboard
    {
        // ── 참조 ─────────────────────────────────────────────────────
        public BTRunner              Runner     { get; set; }
        public EnemyDetection        Detection  { get; set; }
        public EnemyCombat           Combat     { get; set; }
        public EnemyTacticalMemory   Memory     { get; set; }
        public ActorMovementController Movement  { get; set; }

        // ── 매 틱 갱신값 ─────────────────────────────────────────────
        public bool   HasTarget        { get; set; }
        public float  DistanceToTarget { get; set; }
        public string CurrentStateName { get; set; }

        // ── 행동 리듬 제어 ────────────────────────────────────────────
        /// <summary> 마지막으로 공격/행동을 실행한 Time.time </summary>
        public float LastActionTime  { get; set; } = -999f;
        /// <summary> 다음 행동까지 최소 대기 시간 (BTRunner가 롤링) </summary>
        public float NextActionDelay { get; set; } = 0.5f;

        public bool IsActionReady => Time.time - LastActionTime >= NextActionDelay;

        // ── 페이즈 노출 (BTRunner가 protected _currentPhase에서 읽어 씀) ──
        public bool  PhaseAllowCharge { get; set; }
        public bool  PhaseAllowFlank  { get; set; }
        public float PhaseChargeChance { get; set; }
        public float PhaseFlankChance  { get; set; }
        public int   PhaseMaxConsecutiveAttacks { get; set; } = 3;

        // ── 거리 임계값 (BTRunner가 BehaviorSO에서 읽어 씀) ────────────
        public float OptimalCombatDistance { get; set; } = 2.5f;
        public float MaxAttackRange        { get; set; } = 2.5f;
        public float PersonalSpaceDistance { get; set; } = 0.8f;
        public float MinCombatDistance     { get; set; } = 1.5f;
        public float RetreatDistance       { get; set; } = 3f;

        // ── 방어 행동 카운터 ──────────────────────────────────────────────
        public int  ConsecutiveDefensiveCount { get; set; }
        public bool HasGuardMotion            { get; set; }
    }
}
