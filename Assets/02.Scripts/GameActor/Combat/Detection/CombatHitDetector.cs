using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    public static class CombatHitDetector
    {
        private const int DamageableCacheLimit = 512;

        // Collider→IDamageable 해석 결과 캐시. 매 프레임 GetComponentInParent 계층 탐색을 반복하지 않도록
        // 한다. 피격 대상이 아닌 콜라이더(null)도 캐시해 환경 콜라이더의 반복 탐색을 막는다.
        // 키는 참조 동등성으로 비교되므로(파괴된 콜라이더는 잔존) 상한 도달 시 통째로 비운다.
        // Overlap이 돌려주는 콜라이더는 항상 현재 프레임 살아있는 인스턴스라 조회 자체는 안전하다.
        private static readonly Dictionary<Collider, IDamageable> _damageableCache = new();

        private static bool _bufferOverflowWarned;

        // Enter Play Mode Options(도메인 리로드 비활성)에서도 정적 상태가 새 세션에 누수되지 않도록 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _damageableCache.Clear();
            _bufferOverflowWarned = false;
        }

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                hitbox.BeginDebugDetectionSamples();
#endif
                for (int sample = 1; sample <= sampleCount; sample++)
                {
                    float t = sample / (float)sampleCount;
                    CombatHitboxShape shape = hitbox.HasPreviousShape
                        ? CombatHitboxShape.Lerp(hitbox.PreviousShape, current, t)
                        : current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    hitbox.AddDebugDetectionSample(shape);
#endif
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
                if (!_bufferOverflowWarned)
                {
                    _bufferOverflowWarned = true;
                    Debug.LogWarning(
                        $"[CombatHitDetector] Overlap 버퍼({overlapBuffer.Length})가 가득 차 임시 배열을 할당합니다(GC). " +
                        "버퍼 크기 상향 또는 HitBox 범위 축소를 고려하세요. (이 경고는 1회만 출력)");
                }
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

                IDamageable damageable = ResolveDamageable(hit);
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

        private static IDamageable ResolveDamageable(Collider collider)
        {
            if (_damageableCache.TryGetValue(collider, out IDamageable cached))
                return cached;

            IDamageable resolved = collider.GetComponent<IDamageable>()
                                ?? collider.GetComponentInParent<IDamageable>();
            if (_damageableCache.Count >= DamageableCacheLimit)
                _damageableCache.Clear();
            _damageableCache[collider] = resolved;
            return resolved;
        }
    }
}
