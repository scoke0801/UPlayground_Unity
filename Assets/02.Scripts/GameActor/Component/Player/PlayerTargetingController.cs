using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>공격 보정 타깃과 주변 적 검색 책임.</summary>
    public sealed class PlayerTargetingController
    {
        private readonly Transform _owner;
        private readonly Collider[] _overlapBuffer = new Collider[128];

        public PlayerTargetingController(Transform owner) => _owner = owner;

        public Transform FindAttackTarget(float targetingRange, float targetingAngle, float searchRange,
            float searchAngle, LayerMask targetLayer, bool skipIfCovered)
        {
            Vector3 origin = _owner.position;
            Vector3 forward = _owner.forward;
            if (skipIfCovered && HasTarget(origin, forward, targetingRange, targetingAngle, targetLayer))
                return null;

            float clampedAngle = Mathf.Clamp(searchAngle, 0f, 180f);
            int count = Physics.OverlapSphereNonAlloc(origin, searchRange, _overlapBuffer, targetLayer);
            if (count == _overlapBuffer.Length)
            {
                Collider[] saturatedHits = Physics.OverlapSphere(origin, searchRange, targetLayer);
                return FindBestTarget(saturatedHits, saturatedHits.Length, origin, forward, clampedAngle);
            }

            return FindBestTarget(_overlapBuffer, count, origin, forward, clampedAngle);
        }

        private Transform FindBestTarget(
            Collider[] hits,
            int count,
            Vector3 origin,
            Vector3 forward,
            float searchAngle)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.transform == _owner || hit.transform.IsChildOf(_owner)) continue;
                Vector3 direction = hit.transform.position - origin;
                direction.y = 0f;
                if (Vector3.Angle(forward, direction) > searchAngle) continue;
                IDamageable damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;
                if (direction.sqrMagnitude >= bestDistance) continue;
                bestDistance = direction.sqrMagnitude;
                best = hit.transform;
            }

            return best;
        }

        public void FillEnemyControllers(float radius, LayerMask targetLayer, List<IEnemyAIController> result)
        {
            if (result == null) return;
            result.Clear();
            int count = Physics.OverlapSphereNonAlloc(_owner.position, radius, _overlapBuffer, targetLayer);
            if (count == _overlapBuffer.Length)
            {
                Collider[] saturatedHits = Physics.OverlapSphere(_owner.position, radius, targetLayer);
                CollectEnemyControllers(saturatedHits, saturatedHits.Length, result);
                return;
            }

            CollectEnemyControllers(_overlapBuffer, count, result);
        }

        private static void CollectEnemyControllers(
            Collider[] hits,
            int count,
            List<IEnemyAIController> result)
        {
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                if (hit == null) continue;
                MonsterActor monster = hit.GetComponent<MonsterActor>() ?? hit.GetComponentInParent<MonsterActor>();
                IEnemyAIController controller = monster?.AIController;
                if (controller != null && !result.Contains(controller))
                    result.Add(controller);
            }
        }

        private bool HasTarget(Vector3 origin, Vector3 forward, float range, float angle, LayerMask layer)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, range, _overlapBuffer, layer);
            if (count == _overlapBuffer.Length)
            {
                Collider[] saturatedHits = Physics.OverlapSphere(origin, range, layer);
                return ContainsTarget(saturatedHits, saturatedHits.Length, origin, forward, angle);
            }

            return ContainsTarget(_overlapBuffer, count, origin, forward, angle);
        }

        private bool ContainsTarget(
            Collider[] hits,
            int count,
            Vector3 origin,
            Vector3 forward,
            float angle)
        {
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.transform == _owner || hit.transform.IsChildOf(_owner)) continue;
                Vector3 direction = hit.transform.position - origin;
                direction.y = 0f;
                if (Vector3.Angle(forward, direction) > angle) continue;
                IDamageable damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.CanTakeDamage()) return true;
            }
            return false;
        }
    }
}
