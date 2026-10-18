using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Components
{
    /// <summary>공격 보정 타깃과 주변 적 검색 책임.</summary>
    public sealed class PlayerTargetingController
    {
        private readonly Transform _owner;
        private readonly GameActor _ownerActor;
        private readonly Collider _ownerCollider;
        private readonly Collider[] _overlapBuffer = new Collider[128];
        public string LastSelectionReason { get; private set; } = "미선정";

        public PlayerTargetingController(Transform owner)
        {
            _owner = owner;
            _ownerActor = owner != null ? owner.GetComponent<GameActor>() : null;
            _ownerCollider = owner != null ? owner.GetComponent<Collider>() : null;
        }

        /// <summary>공격 의도 방향의 허용 범위 안에서 가려지지 않은 가장 가까운 적을 선택한다.</summary>
        public Transform FindAttackTarget(float searchRange, float searchAngle, Vector3 attackDirection,
            LayerMask targetLayer, LayerMask obstacleLayers, float maxHeightDifference = 0.6f)
        {
            if (_owner == null || searchRange <= 0f) return null;
            Vector3 origin = _owner.position;
            attackDirection.y = 0f;
            Vector3 forward = attackDirection.sqrMagnitude > 0f ? attackDirection.normalized : _owner.forward;

            float clampedAngle = Mathf.Clamp(searchAngle, 0f, 180f);
            int count = Physics.OverlapSphereNonAlloc(origin, searchRange, _overlapBuffer, targetLayer);
            if (count == _overlapBuffer.Length)
            {
                Collider[] saturatedHits = Physics.OverlapSphere(origin, searchRange, targetLayer);
                return FindBestTarget(saturatedHits, saturatedHits.Length, origin, forward, clampedAngle, searchRange, obstacleLayers, maxHeightDifference);
            }

            return FindBestTarget(_overlapBuffer, count, origin, forward, clampedAngle, searchRange, obstacleLayers, maxHeightDifference);
        }

        private Transform FindBestTarget(
            Collider[] hits,
            int count,
            Vector3 origin,
            Vector3 forward,
            float searchAngle,
            float searchRange,
            LayerMask obstacleLayers,
            float maxHeightDifference)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            float bestAngle = float.MaxValue;
            int heightRejected = 0, rangeRejected = 0, angleRejected = 0, obstacleRejected = 0;
            Vector3 sightOrigin = _ownerCollider != null ? _ownerCollider.bounds.center : origin;
            LastSelectionReason = "입력 방향 안에 유효한 대상 없음";

            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.transform == _owner || hit.transform.IsChildOf(_owner)) continue;
                IDamageable damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;
                GameActor targetActor = hit.GetComponentInParent<GameActor>();
                if (!IsHostile(targetActor)) continue;
                Vector3 direction = targetActor.transform.position - origin;
                if (Mathf.Abs(direction.y) > maxHeightDifference) { heightRejected++; continue; }
                // OverlapSphere는 범위 밖 액터의 큰 콜라이더도 반환하므로 액터 위치로 상한을 다시 검사한다.
                direction.y = 0f;
                if (direction.sqrMagnitude > searchRange * searchRange) { rangeRejected++; continue; }
                float distance = direction.sqrMagnitude;
                float angle = Vector3.Angle(forward, direction);
                if (angle > searchAngle) { angleRejected++; continue; }
                if (distance > bestDistance) continue;
                if (Mathf.Approximately(distance, bestDistance))
                {
                    if (angle > bestAngle) continue;
                    if (Mathf.Approximately(angle, bestAngle) && best != null
                        && targetActor.GetInstanceID() >= best.GetComponent<GameActor>().GetInstanceID()) continue;
                }
                if (Physics.Linecast(sightOrigin, hit.bounds.center, obstacleLayers, QueryTriggerInteraction.Ignore))
                { obstacleRejected++; continue; }
                bestAngle = angle;
                bestDistance = distance;
                best = targetActor.transform;
            }

            if (best != null) LastSelectionReason = "입력 방향 내 최근접 / 동률 각도·인스턴스 ID";
            else LastSelectionReason = $"유효 대상 없음; 후보={count}; 높이={heightRejected}; 거리={rangeRejected}; 각도={angleRejected}; 장애물={obstacleRejected}";
            return best;
        }

        /// <summary>명시적 락온도 사거리·높이·생존·시야 조건을 통과해야 공격에 바인딩한다.</summary>
        public bool CanSelectTarget(Transform target, float range, float maxHeightDifference, LayerMask obstacles)
        {
            if (_owner == null || target == null) { LastSelectionReason = "대상 없음"; return false; }
            GameActor actor = target.GetComponentInParent<GameActor>();
            IDamageable damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
            if (!IsHostile(actor) || damageable == null || !damageable.CanTakeDamage())
            { LastSelectionReason = "적대·생존·피해 가능 조건 거절"; return false; }
            Vector3 offset = actor.transform.position - _owner.position;
            if (Mathf.Abs(offset.y) > maxHeightDifference)
            { LastSelectionReason = "높이 차 거절"; return false; }
            offset.y = 0f;
            if (range <= 0f || offset.sqrMagnitude > range * range)
            { LastSelectionReason = "공격 탐색 거리 거절"; return false; }
            Collider targetCollider = actor.GetComponent<Collider>();
            Vector3 start = _ownerCollider != null ? _ownerCollider.bounds.center : _owner.position;
            Vector3 end = targetCollider != null ? targetCollider.bounds.center : actor.transform.position;
            if (Physics.Linecast(start, end, obstacles, QueryTriggerInteraction.Ignore))
            { LastSelectionReason = "장애물 거절"; return false; }
            LastSelectionReason = "선정 대상 거리·높이·시야 통과";
            return true;
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

        private void CollectEnemyControllers(
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
                if (controller != null && IsHostile(monster) && !result.Contains(controller))
                    result.Add(controller);
            }
        }

        private bool IsHostile(GameActor target)
        {
            return _ownerActor != null
                   && target != null
                   && CombatRelationUtility.CanTarget(_ownerActor, target);
        }
    }
}
