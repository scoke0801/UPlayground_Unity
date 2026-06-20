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

                string lower = hitbox.GroupId.ToLowerInvariant();
                if (groups.TryGetValue(lower, out string original) && original != hitbox.GroupId)
                    issues.Add($"Error: 그룹 ID 대소문자가 혼용됩니다. '{original}', '{hitbox.GroupId}'");
                else
                    groups[lower] = hitbox.GroupId;

                if (!hitbox.TryGetWorldShape(out CombatHitboxShape shape))
                    issues.Add($"Warning: {hitbox.name} Collider 크기가 0이거나 유효하지 않습니다.");
                else if (shape.Type == CombatHitboxShapeType.Box
                         && Mathf.Min(shape.HalfExtents.x, shape.HalfExtents.y, shape.HalfExtents.z) < 0.005f)
                    issues.Add($"Warning: {hitbox.name} Box 두께가 지나치게 작습니다.");
            }

            if (issues.Count == 0)
                issues.Add($"정상: HitBox {hitboxes.Length}개, 그룹 {groups.Count}개");
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
