using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.Group;

namespace UPlayGround.Components
{
    public interface IEnemyAIController
    {
        MonsterGroupController Group { get; }
        bool HasAggroTarget { get; }

        void SetGroup(MonsterGroupController group, MemberPriority priority);
        void UpdatePhase(float hpPercent);
        void OnParried();
        void Freeze();
        void Unfreeze();
    }

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
        public abstract GroupIntentBias CurrentGroupIntentBias { get; }

        /// <summary> 정지형 액터가 대기 중에도 타겟을 바라보도록 회전할지 여부. </summary>
        public virtual bool FaceTargetWhileIdle => false;
        /// <summary> 대기 중 타겟 조준 회전 속도. </summary>
        public virtual float IdleFaceTargetSharpness => 6f;

        public abstract MonsterGroupMemory CurrentGroupMemory { get; }

        public abstract bool CanUseSkill();
        public abstract bool TryRequestAttackSlot();
        public abstract bool TryGetFormationSlotPosition(float radius, out Vector3 position);

        /// <summary>
        /// 추격 상태가 전투권에 진입했을 때 사용할 그룹 진형 목적지.
        /// 그룹 비소속이거나 진형 추격이 꺼져 있으면 false를 반환한다.
        /// </summary>
        public virtual bool TryGetChaseFormationPosition(
            float targetDistance,
            out Vector3 position,
            out float arrivalTolerance)
        {
            position = default;
            arrivalTolerance = 0f;
            return false;
        }

        /// <summary>
        /// 근접 그룹 동료로부터 밀려나는 분리(separation) 벡터. 그룹 비소속이면 Vector3.zero.
        /// 여러 마리가 겹쳐 서로 막혀 멈추는 현상을 이동 상태에서 완화하는 데 쓴다.
        /// </summary>
        public virtual Vector3 GetGroupSeparation(float radius) => Vector3.zero;
        public abstract void NotifyBTAttackStarted();
        public abstract void UpdatePhase(float hpPercent);
        public abstract void DecidePostAttack(bool attackHit);
        public abstract Vector3 GetRandomPatrolPoint();
        public abstract void ReleaseGroupSlot();
        public abstract void ReleaseFormationSlot();
    }
}
