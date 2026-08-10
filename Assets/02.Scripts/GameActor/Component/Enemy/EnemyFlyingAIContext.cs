using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.Group;

namespace UPlayGround.Components
{
    /// <summary>
    /// 비행 몬스터 BT/State Facade.
    /// EnemyFlyingAIController 의사결정 로직을 BT로 이전하는 동안 비행 State와 EnemyFlyingAIController을 분리하는 추상 계층.
    /// 지상형 <see cref="EnemyAIContext"/>와는 형제 관계. BT 노드는 비행 전용 노드에서 본 Context를 조회한다.
    /// </summary>
    public abstract class EnemyFlyingAIContext : MonoBehaviour
    {
        // ── 참조 ──
        public abstract EnemyDetection Detection { get; }
        public abstract EnemyCombat Combat { get; }
        public abstract EnemyFlyingSettingsSO FlyingSettings { get; }

        // ── 지상 전투 ──
        public abstract float ChaseStopDistance { get; }
        public abstract float ChaseSpeedMultiplier { get; }
        public abstract float OptimalCombatDistance { get; }
        public abstract float MinCombatDistance { get; }
        public abstract float PersonalSpaceDistance { get; }
        public abstract GroupIntentBias CurrentGroupIntentBias { get; }
        public abstract MonsterGroupMemory CurrentGroupMemory { get; }
        public abstract float CircleDuration { get; }
        public abstract float RetreatDistance { get; }

        // ── 순찰 ──
        public abstract bool EnablePatrol { get; }
        public abstract float PatrolRadius { get; }
        public abstract float PatrolWaitTime { get; }
        public abstract Vector3 SpawnPosition { get; }

        // ── 공중 ──
        public abstract float AirCircleRadius { get; }
        public abstract float AirHoverHeight { get; }
        public abstract float AirMoveSpeed { get; }
        public abstract int AirAttackLimit { get; }
        public abstract int AirAttackCount { get; }

        // ── 급강하 ──
        public abstract float DiveSpeed { get; }
        public abstract float DiveImpactRadius { get; }
        public abstract float DiveChance { get; }

        // ── 카운터/타이머 ──
        public abstract float GroundTimer { get; }
        public abstract int GroundAttackCount { get; }

        /// <summary>
        /// AirCircle이 하강을 요청했는지. State는 스스로 전이하지 않고 이 플래그만 세우므로
        /// BT가 이걸 보고 Dive/Land를 결정한다.
        ///
        /// 공중 공격 횟수 소진과 체류 시간 초과가 모두 이 플래그로 합류한다.
        /// 시간 초과 경로는 AirAttackCount를 올리지 않아 IsAirAttackLimitReached로는
        /// 잡히지 않기 때문에, 그 조건만 쓰면 BT가 공중에 갇힌다.
        /// </summary>
        public abstract bool IsDescendRequested { get; }

        // ── 판정 ──
        public abstract bool CanUseSkill();
        public abstract bool ShouldTakeOff();
        public abstract bool TryRequestAttackSlot();
        public abstract void ReleaseGroupSlot();
        public abstract void NotifyBTAttackStarted();

        // ── 패트롤 ──
        public abstract Vector3 GetRandomPatrolPoint();

        // ── 카운터 제어 ──
        public abstract void ResetAllCounters();
        public abstract void ResetAirCounters();

        // ── 하강 분기 (Dive or Land 결정) ──
        // 단일 호출형: BT 미연결 폴백 또는 단일 액션으로 사용.
        public abstract void TransitionToDescend();
        // BT 조립형: HasDiveSkillAvailable → RollDiveChance → SelectAndSetDiveSkill → Transition Dive / Land
        public abstract bool HasDiveSkillAvailable();
        public abstract bool SelectAndSetDiveSkill();

        // ── State 콜백 (BT 이전 전까지 의사결정 유지) ──
        public abstract void EvaluateChase();
        public abstract void OnGroundAttackFinished();
        public abstract void OnAirAttackFinished();
        public abstract void OnAirCircleForceDescend();
        public abstract void OnDiveLanded();
    }
}
