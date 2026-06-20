using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    public static class CombatHitDetector
    {
        public static int DetectAttachedHits(
            Transform ownerRoot,
            IReadOnlyList<CombatHitbox> hitboxes,
            LayerMask targetLayer,
            Collider[] overlapBuffer,
            ISet<IDamageable> ignoredDamageables,
            ISet<IDamageable> collectedDamageables,
            List<CombatHit> results,
            bool includeInvincibleTargets = false)
        {
            results.Clear();
            if (hitboxes == null || overlapBuffer == null || overlapBuffer.Length == 0)
                return 0;

            if (collectedDamageables == null)
                return 0;

            for (int i = 0; i < hitboxes.Count; i++)
            {
                CombatHitbox hitbox = hitboxes[i];
                if (hitbox == null || !hitbox.TryGetWorldShape(out CombatHitboxShape current))
                    continue;

                int sampleCount = 1;
                Vector3 sweepDirection = Vector3.zero;
                if (hitbox.UseSweep && hitbox.HasPreviousShape)
                {
                    float distance = Vector3.Distance(hitbox.PreviousShape.Center, current.Center);
                    sweepDirection = current.Center - hitbox.PreviousShape.Center;
                    sampleCount = Mathf.Clamp(
                        Mathf.CeilToInt(distance / hitbox.SweepStepDistance),
                        1,
                        hitbox.MaxSweepSteps);
                }

                for (int sample = 1; sample <= sampleCount; sample++)
                {
                    float t = sample / (float)sampleCount;
                    CombatHitboxShape shape = hitbox.HasPreviousShape
                        ? CombatHitboxShape.Lerp(hitbox.PreviousShape, current, t)
                        : current;
                    CollectAttachedShapeHits(
                        ownerRoot,
                        shape,
                        targetLayer,
                        overlapBuffer,
                        ignoredDamageables,
                        collectedDamageables,
                        results,
                        includeInvincibleTargets,
                        sweepDirection);
                }

                hitbox.CommitShape(current);
            }

            return results.Count;
        }

        private static void CollectAttachedShapeHits(
            Transform ownerRoot,
            in CombatHitboxShape shape,
            LayerMask targetLayer,
            Collider[] overlapBuffer,
            ISet<IDamageable> ignoredDamageables,
            ISet<IDamageable> collected,
            List<CombatHit> results,
            bool includeInvincibleTargets,
            Vector3 preferredAttackDirection)
        {
            int hitCount = shape.Type == CombatHitboxShapeType.Box
                ? Physics.OverlapBoxNonAlloc(
                    shape.Center,
                    shape.HalfExtents,
                    overlapBuffer,
                    shape.Rotation,
                    targetLayer,
                    QueryTriggerInteraction.Collide)
                : Physics.OverlapCapsuleNonAlloc(
                    shape.Point0,
                    shape.Point1,
                    shape.Radius,
                    overlapBuffer,
                    targetLayer,
                    QueryTriggerInteraction.Collide);

            Collider[] hits = overlapBuffer;
            if (hitCount == overlapBuffer.Length)
            {
                hits = shape.Type == CombatHitboxShapeType.Box
                    ? Physics.OverlapBox(
                        shape.Center,
                        shape.HalfExtents,
                        shape.Rotation,
                        targetLayer,
                        QueryTriggerInteraction.Collide)
                    : Physics.OverlapCapsule(
                        shape.Point0,
                        shape.Point1,
                        shape.Radius,
                        targetLayer,
                        QueryTriggerInteraction.Collide);
                hitCount = hits.Length;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;
                if (ownerRoot != null && (hit.transform == ownerRoot || hit.transform.IsChildOf(ownerRoot)))
                    continue;

                IDamageable damageable = hit.GetComponent<IDamageable>()
                                      ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || collected.Contains(damageable))
                    continue;

                bool deliverable = includeInvincibleTargets ? damageable.IsAlive() : damageable.CanTakeDamage();
                if (!deliverable || ignoredDamageables != null && ignoredDamageables.Contains(damageable))
                    continue;

                Vector3 hitPoint = hit.ClosestPoint(shape.Center);
                Vector3 attackDirection = preferredAttackDirection;
                if (attackDirection.sqrMagnitude < 0.0001f)
                    attackDirection = hitPoint - shape.Center;
                if (attackDirection.sqrMagnitude < 0.0001f && ownerRoot != null)
                    attackDirection = hit.transform.position - ownerRoot.position;
                if (attackDirection.sqrMagnitude < 0.0001f)
                    attackDirection = ownerRoot != null ? ownerRoot.forward : Vector3.forward;

                collected.Add(damageable);
                results.Add(new CombatHit(
                    damageable,
                    hit,
                    hitPoint,
                    attackDirection.normalized));
            }
        }
    }
}
