using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    public static class CombatHitDetector
    {
        /// <param name="includeInvincibleTargets">
        /// true면 무적(CanTakeDamage=false)이지만 살아있는 대상도 결과에 포함한다.
        /// 적이 플레이어를 칠 때 사용 — 무적/퍼펙트도지/대시회피 판정을 피격자(TakeDamage)의 방어 레이어가 결정하도록 위임.
        /// (기본 false: 플레이어가 적을 칠 때는 무적 대상을 걸러 가짜 히트 피드백을 막는다.)
        /// </param>
        public static int DetectMeleeHits(
            in MeleeHitShape shape,
            Collider[] overlapBuffer,
            ISet<IDamageable> ignoredDamageables,
            List<CombatHit> results,
            bool includeInvincibleTargets = false)
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
                return CollectMeleeHits(shape, saturatedHits, saturatedHits.Length, ignoredDamageables, results, includeInvincibleTargets);
            }

            return CollectMeleeHits(shape, overlapBuffer, hitCount, ignoredDamageables, results, includeInvincibleTargets);
        }

        private static int CollectMeleeHits(
            in MeleeHitShape shape,
            Collider[] hits,
            int hitCount,
            ISet<IDamageable> ignoredDamageables,
            List<CombatHit> results,
            bool includeInvincibleTargets)
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
                if (damageable == null)
                    continue;
                // 무적 대상 위임 모드(적→플레이어)에서는 살아있기만 하면 전달하고, 무적/회피 판정은 TakeDamage가 맡는다.
                bool deliverable = includeInvincibleTargets ? damageable.IsAlive() : damageable.CanTakeDamage();
                if (!deliverable)
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
