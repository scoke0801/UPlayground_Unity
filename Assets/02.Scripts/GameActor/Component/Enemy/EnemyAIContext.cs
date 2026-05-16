using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// BT 노드가 참조하는 적 AI Facade.
    /// EnemyBrain 의사결정 로직을 BT로 이전하는 동안 BT 노드와 EnemyBrain을 분리하는 추상 계층.
    /// Phase 7 완료 시 EnemyBrain 클래스는 제거되고 본 클래스가 책임을 흡수한다.
    /// </summary>
    public abstract class EnemyAIContext : MonoBehaviour
    {
        public abstract bool EnablePatrol { get; }
        public abstract bool HasGuardMotion { get; }
        public abstract float RetreatDistance { get; }
        public abstract float CircleDuration { get; }
        public abstract float GuardDuration { get; }

        public abstract bool CanUseSkill();
        public abstract bool TryRequestAttackSlot();
        public abstract void NotifyBTAttackStarted();
        public abstract void ReleaseGroupSlot();
    }
}
