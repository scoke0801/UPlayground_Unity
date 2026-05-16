using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Component
{
    /// <summary>
    /// BT 노드가 참조하는 적 AI Facade.
    /// EnemyAIController 의사결정 로직을 BT로 이전하는 동안 BT 노드와 EnemyAIController을 분리하는 추상 계층.
    /// Phase 7 완료 시 EnemyAIController 클래스는 제거되고 본 클래스가 책임을 흡수한다.
    /// </summary>
    public abstract class EnemyAIContext : MonoBehaviour
    {
        public abstract EnemyBehaviorSO BehaviorData { get; }
        public abstract BehaviorPhase CurrentPhase { get; }
        public abstract Vector3 SpawnPosition { get; }
        public abstract bool EnablePatrol { get; }
        public abstract bool HasGuardMotion { get; }
        public abstract float HealthPercent { get; }
        public abstract float PatrolRadius { get; }
        public abstract float PatrolWaitTime { get; }
        public abstract float OptimalCombatDistance { get; }
        public abstract float MinCombatDistance { get; }
        public abstract float PersonalSpaceDistance { get; }
        public abstract float ChaseStopDistance { get; }
        public abstract float ChaseSpeedMultiplier { get; }
        public abstract float RetreatDistance { get; }
        public abstract float CircleDuration { get; }
        public abstract float GuardDuration { get; }

        public abstract bool CanUseSkill();
        public abstract bool TryRequestAttackSlot();
        public abstract void NotifyBTAttackStarted();
        public abstract void UpdatePhase(float hpPercent);
        public abstract void DecidePostAttack(bool attackHit);
        public abstract Vector3 GetRandomPatrolPoint();
        public abstract void ReleaseGroupSlot();
    }
}
