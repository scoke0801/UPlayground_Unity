using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 액터 하위의 부착형 HitBox를 그룹별로 수집하고 Collision Window 수명을 관리한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHitboxSet : MonoBehaviour
    {
        private readonly Dictionary<string, List<CombatHitbox>> _groups =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly List<CombatHitbox> _activeHitboxes = new(8);
        private readonly Collider[] _overlapBuffer = new Collider[128];
        private readonly HashSet<IDamageable> _frameDamageables = new();

        public string ActiveGroupId { get; private set; }
        public bool IsActive => _activeHitboxes.Count > 0;
        public IReadOnlyList<CombatHitbox> ActiveHitboxes => _activeHitboxes;

        private void Awake()
        {
            Refresh();
        }

        public void Refresh()
        {
            string activeGroup = ActiveGroupId;
            _groups.Clear();
            _activeHitboxes.Clear();

            CombatHitbox[] hitboxes = GetComponentsInChildren<CombatHitbox>(true);
            foreach (CombatHitbox hitbox in hitboxes)
            {
                if (hitbox == null || !hitbox.IsSupported)
                    continue;

                if (!_groups.TryGetValue(hitbox.GroupId, out List<CombatHitbox> group))
                {
                    group = new List<CombatHitbox>(2);
                    _groups.Add(hitbox.GroupId, group);
                }
                group.Add(hitbox);
            }

            ActiveGroupId = null;
            if (!string.IsNullOrWhiteSpace(activeGroup))
                BeginGroup(activeGroup);
        }

        public bool HasGroup(string groupId)
        {
            string resolved = ResolveGroupId(groupId);
            return _groups.TryGetValue(resolved, out List<CombatHitbox> group) && group.Count > 0;
        }

        public bool BeginGroup(string groupId)
        {
            string resolved = ResolveGroupId(groupId);
            EndGroup();
            if (!_groups.TryGetValue(resolved, out List<CombatHitbox> group) || group.Count == 0)
            {
                // 장비가 런타임에 교체됐을 수 있으므로 한 번 재수집한다.
                RefreshWithoutRestore();
                if (!_groups.TryGetValue(resolved, out group) || group.Count == 0)
                    return false;
            }

            ActiveGroupId = resolved;
            foreach (CombatHitbox hitbox in group)
            {
                if (hitbox == null || !hitbox.gameObject.activeInHierarchy)
                    continue;
                _activeHitboxes.Add(hitbox);
                hitbox.BeginSampling();
            }
            return _activeHitboxes.Count > 0;
        }

        public void EndGroup()
        {
            foreach (CombatHitbox hitbox in _activeHitboxes)
                hitbox?.ClearSampling();
            _activeHitboxes.Clear();
            ActiveGroupId = null;
        }

        public int DetectActiveGroup(
            Transform ownerRoot,
            LayerMask targetLayer,
            ISet<IDamageable> ignoredTargets,
            List<CombatHit> results,
            bool includeInvincibleTargets)
        {
            _frameDamageables.Clear();
            return CombatHitDetector.DetectAttachedHits(
                ownerRoot,
                _activeHitboxes,
                targetLayer,
                _overlapBuffer,
                ignoredTargets,
                _frameDamageables,
                results,
                includeInvincibleTargets);
        }

        private void RefreshWithoutRestore()
        {
            _groups.Clear();
            CombatHitbox[] hitboxes = GetComponentsInChildren<CombatHitbox>(true);
            foreach (CombatHitbox hitbox in hitboxes)
            {
                if (hitbox == null || !hitbox.IsSupported)
                    continue;
                if (!_groups.TryGetValue(hitbox.GroupId, out List<CombatHitbox> group))
                {
                    group = new List<CombatHitbox>(2);
                    _groups.Add(hitbox.GroupId, group);
                }
                group.Add(hitbox);
            }
        }

        private static string ResolveGroupId(string groupId)
            => string.IsNullOrWhiteSpace(groupId) ? CombatHitbox.DefaultGroupId : groupId.Trim();
    }
}
