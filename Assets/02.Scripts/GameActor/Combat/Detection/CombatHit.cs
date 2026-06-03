using UnityEngine;

namespace UPlayGround.Combat
{
    public readonly struct CombatHit
    {
        public readonly IDamageable Damageable;
        public readonly Collider Collider;
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;

        public GameObject HitObject => Collider != null ? Collider.gameObject : null;

        public CombatHit(
            IDamageable damageable,
            Collider collider,
            Vector3 hitPoint,
            Vector3 attackDirection)
        {
            Damageable = damageable;
            Collider = collider;
            HitPoint = hitPoint;
            AttackDirection = attackDirection;
        }
    }
}
