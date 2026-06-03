using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    public static class CombatHitDetector
    {
        public static int DetectMeleeHits(
            in MeleeHitShape shape,
            Collider[] overlapBuffer,
            ISet<IDamageable> ignoredDamageables,
            List<CombatHit> results)
        {
            results.Clear();
            if (overlapBuffer == null || overlapBuffer.Length == 0)
                return 0;

            int hitCount = Physics.OverlapSphereNonAlloc(
                shape.Origin,
                shape.Radius,
                overlapBuffer,
                shape.TargetLayer);

            if (hitCount == overlapBuffer.Length)
            {
                Collider[] saturatedHits = Physics.OverlapSphere(
                    shape.Origin,
                    shape.Radius,
                    shape.TargetLayer);
                return CollectMeleeHits(shape, saturatedHits, saturatedHits.Length, ignoredDamageables, results);
            }

            return CollectMeleeHits(shape, overlapBuffer, hitCount, ignoredDamageables, results);
        }

        private static int CollectMeleeHits(
            in MeleeHitShape shape,
            Collider[] hits,
            int hitCount,
            ISet<IDamageable> ignoredDamageables,
            List<CombatHit> results)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;
                if (shape.Owner != null && (hit.transform == shape.Owner || hit.transform.IsChildOf(shape.Owner)))
                    continue;

                Vector3 flatDirection = hit.transform.position - (shape.Owner != null ? shape.Owner.position : shape.Origin);
                flatDirection.y = 0f;
                if (shape.HalfAngle > 0f && flatDirection.sqrMagnitude > 0.001f)
                {
                    if (Vector3.Angle(shape.Forward, flatDirection) > shape.HalfAngle)
                        continue;
                }

                if (shape.HeightRange > 0f)
                {
                    float closestY = hit.ClosestPoint(shape.Origin).y;
                    if (Mathf.Abs(closestY - shape.Origin.y) > shape.HeightRange)
                        continue;
                }

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                      ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage())
                    continue;
                if (ignoredDamageables != null && ignoredDamageables.Contains(damageable))
                    continue;

                Vector3 hitPoint = hit.ClosestPoint(shape.Origin);
                Vector3 attackDirection = shape.Owner != null
                    ? (hit.transform.position - shape.Owner.position).normalized
                    : shape.Forward;

                results.Add(new CombatHit(damageable, hit, hitPoint, attackDirection));
            }

            return results.Count;
        }
    }
}
