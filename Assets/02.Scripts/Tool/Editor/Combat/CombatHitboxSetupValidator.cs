#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.EditorTools;

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
                issues.Add("통합 검증: 자동 수집된 Ability Payload/MotionSet 에셋이 없습니다.");
                return issues;
            }

            CombatHitbox[] hitboxes = root.GetComponentsInChildren<CombatHitbox>(true);
            var hitboxGroups = new HashSet<string>(
                hitboxes.Select(hitbox => hitbox.GroupId),
                System.StringComparer.OrdinalIgnoreCase);

            issues.Add($"통합 검증: 대상 에셋 {validAssets.Count}개 (Ability Payload/MotionSet)");
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
            // 키마다 프로젝트를 훑지 않도록 이 검증 1회 분량의 인덱스만 만든다.
            var motionIndex = new AbilityMotionIndex();
            foreach (AbilitySetSO attackData in assets.OfType<AbilitySetSO>())
            {
                int attackCount = 0;
                var seen = new HashSet<AbilityAttackInfo>();
                foreach (var entry in AbilityAttackEditorUtility.Collect(attackData, true))
                {
                    AbilityAttackInfo info = entry.AttackInfo;
                    if (info?.baseInfo == null || !seen.Add(info))
                        continue;

                    attackCount++;
                    var attack = new CombatTimelineUtility.ResolvedAttack
                    {
                        SourceName = entry.Ability != null
                            ? entry.Ability.name
                            : entry.Payload != null ? entry.Payload.name : "알 수 없는 Ability",
                        // 여기서 모션은 진단 메시지 표시용이다. 같은 키가 무기별로 다른
                        // 모션을 가리키는 것이 정상이므로 모호해도 대표 하나를 쓴다.
                        MotionAsset = motionIndex.ResolveRepresentative(
                            info.motionKey),
                        HitPhases = info.baseInfo.hitPhases,
                        InterruptActions = info.interruptActions,
                        AttackInfo = info,
                        Owner = entry.Payload,
                    };
                    ValidateAttackPhases(attackData, attack, hitboxGroups, issues);
                }

                if (attackCount == 0)
                    issues.Add($"Warning: {attackData.name}에서 모션이 지정된 공격 데이터를 찾지 못했습니다.");
            }
        }

        private static void ValidateAttackPhases(
            AbilitySetSO owner,
            CombatTimelineUtility.ResolvedAttack attack,
            HashSet<string> hitboxGroups,
            List<string> issues)
        {
            if (attack.HitPhases == null || attack.HitPhases.Count == 0)
            {
                issues.Add($"Error: {owner.name}/{attack.SourceName}({attack.MotionAsset?.name ?? "모션 없음"})의 HitPhase가 비어 있습니다.");
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

                List<CombatTimelineUtility.TimedSpan> disabledCollisions =
                    CombatTimelineUtility.CollectSpans<DisableCollisionEvent>(set);

                int maxPhase = -1;
                foreach (CombatTimelineUtility.TimedSpan span in collisions)
                {
                    maxPhase = Mathf.Max(maxPhase, span.PhaseIndex);

                    if (span.IsExplicitCollision)
                    {
                        AppendExplicitShapeIssues(asset.name, span, issues);
                        continue;
                    }

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

                AppendExplicitDisableOverlapIssues(
                    asset.name,
                    collisions,
                    disabledCollisions,
                    issues);

                issues.Add($"통합 검증: {asset.name} Collision {collisions.Count}개, 최대 Phase P{maxPhase}");
            }
        }

        /// <summary>
        /// DisableCollisionEvent는 부착형 그룹만 재개할 수 있으므로 Explicit Window와 겹치면
        /// 종료 뒤 판정 소스가 바뀐다. 완료 스펙 14.3의 저작 금지 조합을 검증 오류로 승격한다.
        /// </summary>
        private static void AppendExplicitDisableOverlapIssues(
            string assetName,
            IReadOnlyList<CombatTimelineUtility.TimedSpan> collisions,
            IReadOnlyList<CombatTimelineUtility.TimedSpan> disabledCollisions,
            List<string> issues)
        {
            if (disabledCollisions == null || disabledCollisions.Count == 0)
                return;

            foreach (CombatTimelineUtility.TimedSpan collision in collisions)
            {
                if (!collision.IsExplicitCollision
                    || collision.ExplicitShape?.evaluation != CollisionEvaluationType.Window)
                {
                    continue;
                }

                foreach (CombatTimelineUtility.TimedSpan disabled in disabledCollisions)
                {
                    if (disabled.Start >= collision.End || disabled.End <= collision.Start)
                        continue;

                    issues.Add(
                        $"Error: {assetName} P{collision.PhaseIndex} Explicit Window "
                        + $"({collision.Start:0.###}~{collision.End:0.###})와 DisableCollision "
                        + $"({disabled.Start:0.###}~{disabled.End:0.###})이 겹칩니다. "
                        + "DisableCollision은 Explicit Shape를 재개할 수 없으므로 구간을 분리하세요.");
                }
            }
        }

        /// <summary>
        /// 명시적 범위 판정 이벤트의 데이터 검증(스펙 11.3).
        /// 부착형 그룹 존재 검증은 적용하지 않는다 — Explicit Shape는 HitBox 그룹을 사용하지 않는다.
        /// </summary>
        private static void AppendExplicitShapeIssues(
            string assetName,
            CombatTimelineUtility.TimedSpan span,
            List<string> issues)
        {
            ExplicitCollisionShapeData shape = span.ExplicitShape;
            if (shape == null)
            {
                issues.Add(
                    $"Error: {assetName} P{span.PhaseIndex} Collision 판정 소스가 Explicit Shape인데 "
                    + "Shape 데이터가 없습니다.");
                return;
            }

            if (!shape.Validate(out string error))
                issues.Add($"Error: {assetName} P{span.PhaseIndex} Explicit Shape — {error}");

            if (shape.evaluation == CollisionEvaluationType.Window && span.End <= span.Start)
            {
                issues.Add(
                    $"Error: {assetName} P{span.PhaseIndex} Explicit Shape가 Window 평가인데 duration이 0입니다. "
                    + "duration을 주거나 OnceOnBegin으로 전환하세요.");
            }

            if (shape.anchor == CollisionAnchorType.PrimaryTarget)
            {
                issues.Add(
                    $"Info: {assetName} P{span.PhaseIndex} Explicit Shape가 PrimaryTarget Anchor를 사용합니다. "
                    + "런타임에 주 대상이 없으면 판정이 중단됩니다.");
            }

            if (shape.anchor == CollisionAnchorType.WorldPosition
                && shape.worldPosition == Vector3.zero)
            {
                issues.Add(
                    $"Warning: {assetName} P{span.PhaseIndex} Explicit Shape가 WorldPosition Anchor인데 "
                    + "좌표가 원점입니다. 런타임 Context가 좌표를 제공하지 않으면 월드 원점에서 판정합니다.");
            }

            issues.Add($"Info: {assetName} P{span.PhaseIndex} Explicit Shape — {shape.Describe()}");
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
