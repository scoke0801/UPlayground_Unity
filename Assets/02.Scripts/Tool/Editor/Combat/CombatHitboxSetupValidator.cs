#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;

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
