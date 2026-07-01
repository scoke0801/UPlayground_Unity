#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Combat
{
    public static class CombatHitboxSetupValidator
    {
        public static List<string> Validate(GameObject root)
        {
            var issues = new List<string>();
            if (root == null)
            {
                issues.Add("대상이 null입니다.");
                return issues;
            }

            CombatHitbox[] hitboxes = root.GetComponentsInChildren<CombatHitbox>(true);
            if (hitboxes.Length == 0)
            {
                issues.Add("Warning: CombatHitbox가 없습니다.");
                return issues;
            }

            var groups = new Dictionary<string, string>();
            var groupCounts = new Dictionary<string, int>();
            foreach (CombatHitbox hitbox in hitboxes)
            {
                if (hitbox.ShapeCollider == null)
                {
                    issues.Add($"Error: {GetPath(root.transform, hitbox.transform)}의 Collider가 비어 있습니다.");
                    continue;
                }

                if (!hitbox.IsSupported)
                    issues.Add($"Error: {hitbox.name}은 Box/Capsule Collider가 아닙니다.");
                if (!hitbox.ShapeCollider.isTrigger)
                    issues.Add($"Warning: {hitbox.name} Collider의 isTrigger가 꺼져 있습니다.");
                if (hitbox.ShapeCollider.enabled)
                    issues.Add($"Warning: {hitbox.name} Collider가 활성화되어 있습니다.");
                // 런타임 BeginGroup은 비활성 계층의 HitBox를 수집에서 제외한다. 꺼둔 채로 두면 판정이 빠진다.
                if (!hitbox.gameObject.activeInHierarchy)
                    issues.Add($"Warning: {hitbox.name} GameObject가 비활성 상태입니다. 런타임 그룹 수집에서 제외됩니다.");

                string lower = hitbox.GroupId.ToLowerInvariant();
                if (groups.TryGetValue(lower, out string original) && original != hitbox.GroupId)
                    issues.Add($"Error: 그룹 ID 대소문자가 혼용됩니다. '{original}', '{hitbox.GroupId}'");
                else
                    groups[lower] = hitbox.GroupId;
                groupCounts.TryGetValue(lower, out int count);
                groupCounts[lower] = count + 1;

                if (!hitbox.TryGetWorldShape(out CombatHitboxShape shape))
                    issues.Add($"Warning: {hitbox.name} Collider 크기가 0이거나 유효하지 않습니다.");
                else if (shape.Type == CombatHitboxShapeType.Box
                         && Mathf.Min(shape.HalfExtents.x, shape.HalfExtents.y, shape.HalfExtents.z) < 0.005f)
                    issues.Add($"Warning: {hitbox.name} Box 두께가 지나치게 작습니다.");
            }

            // 그룹 목록 추가 전까지 누적된 항목은 모두 Error/Warning이다(여기까지 오면 hitboxes.Length>0).
            bool hasProblem = issues.Count > 0;

            // 검증 결과(정상/문제)를 먼저 출력한다.
            issues.Insert(0, hasProblem
                ? $"문제 {issues.Count}건 발견 — HitBox {hitboxes.Length}개, 그룹 {groups.Count}개"
                : $"정상: HitBox {hitboxes.Length}개, 그룹 {groups.Count}개");

            // 그룹 목록은 항상 출력한다. 애니메이션 BeginCollisionEvent의 hitboxGroupId에 정확히 입력할
            // 그룹 ID를 저작자가 바로 확인할 수 있게 한다.
            if (groups.Count > 0)
            {
                issues.Add($"그룹 목록 ({groups.Count}개) — BeginCollisionEvent.hitboxGroupId에 사용:");
                foreach (KeyValuePair<string, string> group in groups)
                    issues.Add($"  • \"{group.Value}\"  (HitBox {groupCounts[group.Key]}개)");
            }

            return issues;
        }

        public static List<string> Validate(GameObject root, IReadOnlyList<UnityEngine.Object> assets)
        {
            List<string> issues = Validate(root);
            if (root == null)
                return issues;

            List<UnityEngine.Object> validAssets = assets?
                .Where(asset => asset != null)
                .Distinct()
                .ToList() ?? new List<UnityEngine.Object>();
            if (validAssets.Count == 0)
            {
                issues.Add("통합 검증: 자동 수집된 AttackDataSO/MotionSet 에셋이 없습니다.");
                return issues;
            }

            CombatHitbox[] hitboxes = root.GetComponentsInChildren<CombatHitbox>(true);
            var hitboxGroups = new HashSet<string>(
                hitboxes.Select(hitbox => hitbox.GroupId),
                System.StringComparer.OrdinalIgnoreCase);

            issues.Add($"통합 검증: 대상 에셋 {validAssets.Count}개 (AttackData/MotionSet)");
            AppendGroupUsageIssues(root, validAssets, issues);
            AppendAttackDataIssues(validAssets, hitboxGroups, issues);
            AppendMotionSetIssues(validAssets, hitboxGroups, issues);
            return issues;
        }

        private static void AppendGroupUsageIssues(
            GameObject root,
            IReadOnlyList<UnityEngine.Object> assets,
            List<string> issues)
        {
            List<CombatHitboxGroupSyncUtility.GroupUsage> usages =
                CombatHitboxGroupSyncUtility.Collect(root, assets);
            int missing = 0;
            int unused = 0;

            foreach (CombatHitboxGroupSyncUtility.GroupUsage usage in usages)
            {
                bool requiredByAsset = usage.DataPhaseCount > 0 || usage.EventCount > 0;
                if (!string.IsNullOrWhiteSpace(usage.GroupId)
                    && usage.HitboxCount == 0
                    && requiredByAsset)
                {
                    missing++;
                    issues.Add(
                        $"Error: 데이터/이벤트가 그룹 '{usage.GroupId}'을 요구하지만 HitBox에 없습니다. "
                        + $"Data {usage.DataPhaseCount}, Event {usage.EventCount}");
                }

                if (usage.HitboxCount > 0 && !requiredByAsset)
                {
                    unused++;
                    issues.Add($"Warning: HitBox 그룹 '{usage.GroupId}'은 현재 수집된 공격 데이터/이벤트에서 참조되지 않습니다.");
                }
            }

            if (missing == 0 && unused == 0)
                issues.Add("통합 검증: HitBox 그룹과 데이터/이벤트 참조가 일치합니다.");
        }

        private static void AppendAttackDataIssues(
            IReadOnlyList<UnityEngine.Object> assets,
            HashSet<string> hitboxGroups,
            List<string> issues)
        {
            foreach (AttackDataSO attackData in assets.OfType<AttackDataSO>())
            {
                int attackCount = 0;
                foreach (AnimKey key in System.Enum.GetValues(typeof(AnimKey)))
                {
                    if (key == AnimKey.None)
                        continue;

                    foreach (CombatTimelineUtility.ResolvedAttack attack in CombatTimelineUtility.ResolveAttacks(attackData, key))
                    {
                        attackCount++;
                        ValidateAttackPhases(attackData, attack, hitboxGroups, issues);
                    }
                }

                if (attackCount == 0)
                    issues.Add($"Warning: {attackData.name}에서 AnimKey가 지정된 공격 데이터를 찾지 못했습니다.");
            }
        }

        private static void ValidateAttackPhases(
            AttackDataSO owner,
            CombatTimelineUtility.ResolvedAttack attack,
            HashSet<string> hitboxGroups,
            List<string> issues)
        {
            if (attack.HitPhases == null || attack.HitPhases.Count == 0)
            {
                issues.Add($"Error: {owner.name}/{attack.SourceName}({attack.AnimKey})의 HitPhase가 비어 있습니다.");
                return;
            }

            for (int i = 0; i < attack.HitPhases.Count; i++)
            {
                HitPhaseData phase = attack.HitPhases[i];
                if (phase == null)
                {
                    issues.Add($"Error: {owner.name}/{attack.SourceName} P{i}가 null입니다.");
                    continue;
                }

                string groupId = string.IsNullOrWhiteSpace(phase.hitboxGroupId)
                    ? CombatHitbox.DefaultGroupId
                    : phase.hitboxGroupId.Trim();
                if (!hitboxGroups.Contains(groupId))
                {
                    issues.Add(
                        $"Warning: {owner.name}/{attack.SourceName} P{i} 기본 그룹 '{groupId}'이 HitBox에 없습니다. "
                        + "BeginCollisionEvent에서 그룹을 덮어쓰지 않으면 판정이 실패합니다.");
                }
            }
        }

        private static void AppendMotionSetIssues(
            IReadOnlyList<UnityEngine.Object> assets,
            HashSet<string> hitboxGroups,
            List<string> issues)
        {
            foreach (MotionSetAsset asset in assets.OfType<MotionSetAsset>())
            {
                MotionSet set = asset.motionSet;
                if (set == null)
                {
                    issues.Add($"Error: {asset.name}의 MotionSet이 null입니다.");
                    continue;
                }

                List<CombatTimelineUtility.TimedSpan> collisions =
                    CombatTimelineUtility.CollectCollisionSpans(set);
                if (collisions.Count == 0)
                    continue;

                int maxPhase = -1;
                foreach (CombatTimelineUtility.TimedSpan span in collisions)
                {
                    maxPhase = Mathf.Max(maxPhase, span.PhaseIndex);
                    if (span.End <= span.Start)
                    {
                        issues.Add(
                            $"Warning: {asset.name} P{span.PhaseIndex} Collision 구간 길이가 0입니다. "
                            + $"start {span.Start:0.###}, end {span.End:0.###}");
                    }

                    if (!string.IsNullOrWhiteSpace(span.HitboxGroupId)
                        && !hitboxGroups.Contains(span.HitboxGroupId.Trim()))
                    {
                        issues.Add($"Error: {asset.name} Collision 이벤트 그룹 '{span.HitboxGroupId}'이 HitBox에 없습니다.");
                    }

                    if (span.AdditionalHitboxGroupIds == null)
                        continue;

                    foreach (string additionalGroupId in span.AdditionalHitboxGroupIds)
                    {
                        if (!string.IsNullOrWhiteSpace(additionalGroupId)
                            && !hitboxGroups.Contains(additionalGroupId.Trim()))
                        {
                            issues.Add($"Error: {asset.name} Collision 이벤트 추가 그룹 '{additionalGroupId}'이 HitBox에 없습니다.");
                        }
                    }
                }

                issues.Add($"통합 검증: {asset.name} Collision {collisions.Count}개, 최대 Phase P{maxPhase}");
            }
        }

        private static string GetPath(Transform root, Transform target)
        {
            if (target == root)
                return root.name;
            var names = new Stack<string>();
            while (target != null && target != root)
            {
                names.Push(target.name);
                target = target.parent;
            }
            return $"{root.name}/{string.Join("/", names)}";
        }
    }
}
#endif
