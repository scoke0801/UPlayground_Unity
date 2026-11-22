using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>재사용되는 줄기 Collider를 현재 배치의 타격 대상으로 연결한다.</summary>
    public sealed class TerrainTreeHitProxy : MonoBehaviour, IDamageable, IEnvironmentHitRedirect
    {
        public IDamageable HitTarget { get; internal set; }
        internal CapsuleCollider Capsule { get; set; }
        public CombatResult ReceiveHit(in HitRequest request) => HitTarget != null ? HitTarget.ReceiveHit(request) : default;
        public bool IsAlive() => HitTarget?.IsAlive() == true;
        public bool CanTakeDamage() => HitTarget?.CanTakeDamage() == true;
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => HitTarget?.GetHealthPercent() ?? 0;
        public float GetCurrentHealth() => HitTarget?.GetCurrentHealth() ?? 0;
        public void ApplyHealingEffect(float healAmount) { }
    }
}
