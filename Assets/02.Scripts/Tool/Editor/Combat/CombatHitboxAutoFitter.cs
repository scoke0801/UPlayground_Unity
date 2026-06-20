#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Component;

namespace UPlayGround.Tool.Editor.Combat
{
    public enum CombatHitboxSetupMode
    {
        WeaponAutoFit,
        HumanoidBodySetup,
        GenericBodySetup,
        ValidateOnly,
        RefitExisting,
        RemoveGenerated,
    }

    public readonly struct CombatHitboxSetupResult
    {
        public readonly string Target;
        public readonly int Created;
        public readonly int Updated;
        public readonly int Skipped;
        public readonly IReadOnlyList<string> Messages;

        public CombatHitboxSetupResult(
            string target,
            int created,
            int updated,
            int skipped,
            IReadOnlyList<string> messages)
        {
            Target = target;
            Created = created;
            Updated = updated;
            Skipped = skipped;
            Messages = messages;
        }
    }

    public readonly struct CombatHitboxTargetAnalysis
    {
        public readonly CombatHitboxSetupMode SuggestedMode;
        public readonly int RendererCount;
        public readonly int ExistingHitboxCount;
        public readonly bool HasAnimator;
        public readonly bool IsHumanoid;
        public readonly bool IsCombatActor;
        public readonly string Summary;

        public CombatHitboxTargetAnalysis(
            CombatHitboxSetupMode suggestedMode,
            int rendererCount,
            int existingHitboxCount,
            bool hasAnimator,
            bool isHumanoid,
            bool isCombatActor,
            string summary)
        {
            SuggestedMode = suggestedMode;
            RendererCount = rendererCount;
            ExistingHitboxCount = existingHitboxCount;
            HasAnimator = hasAnimator;
            IsHumanoid = isHumanoid;
            IsCombatActor = isCombatActor;
            Summary = summary;
        }
    }

    public static class CombatHitboxAutoFitter
    {
        public const int GeneratorVersion = 1;

        public static CombatHitboxTargetAnalysis Analyze(GameObject root)
        {
            if (root == null)
            {
                return new CombatHitboxTargetAnalysis(
                    CombatHitboxSetupMode.WeaponAutoFit,
                    0,
                    0,
                    false,
                    false,
                    false,
                    "대상을 선택하세요.");
            }

            Animator animator = root.GetComponentInChildren<Animator>(true);
            bool isHumanoid = animator != null && animator.isHuman;
            bool isCombatActor = root.GetComponentInChildren<GameActor>(true) != null
                                 || root.GetComponentInChildren<CharacterModelData>(true) != null;
            int rendererCount = root.GetComponentsInChildren<Renderer>(true)
                .Count(renderer => (renderer is MeshRenderer or SkinnedMeshRenderer)
                                   && renderer.GetComponentInParent<ParticleSystem>() == null);
            int hitboxCount = root.GetComponentsInChildren<CombatHitbox>(true).Length;

            CombatHitboxSetupMode mode;
            string targetType;
            if (isHumanoid)
            {
                mode = CombatHitboxSetupMode.HumanoidBodySetup;
                targetType = "Humanoid 신체";
            }
            else if (isCombatActor || animator != null)
            {
                mode = CombatHitboxSetupMode.GenericBodySetup;
                targetType = "Generic 신체";
            }
            else
            {
                mode = CombatHitboxSetupMode.WeaponAutoFit;
                targetType = "무기/소품";
            }

            string summary =
                $"{targetType}로 판별 · 하위 Renderer {rendererCount}개 · 기존 HitBox {hitboxCount}개";
            return new CombatHitboxTargetAnalysis(
                mode,
                rendererCount,
                hitboxCount,
                animator != null,
                isHumanoid,
                isCombatActor,
                summary);
        }

        public static CombatHitboxSetupResult Apply(
            GameObject root,
            CombatHitboxSetupMode mode,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit)
        {
            if (root == null)
                return new CombatHitboxSetupResult("(null)", 0, 0, 1, new[] { "대상이 null입니다." });

            return mode switch
            {
                CombatHitboxSetupMode.WeaponAutoFit => FitWeapon(root, profile, forceRefit),
                CombatHitboxSetupMode.HumanoidBodySetup => FitHumanoid(root, profile, forceRefit),
                CombatHitboxSetupMode.GenericBodySetup => FitGeneric(root, profile, forceRefit),
                CombatHitboxSetupMode.ValidateOnly => new CombatHitboxSetupResult(
                    root.name, 0, 0, 0, CombatHitboxSetupValidator.Validate(root)),
                CombatHitboxSetupMode.RefitExisting => RefitExisting(root, profile, forceRefit),
                CombatHitboxSetupMode.RemoveGenerated => RemoveGenerated(root),
                _ => new CombatHitboxSetupResult(root.name, 0, 0, 1, new[] { "지원하지 않는 모드입니다." }),
            };
        }

        private static CombatHitboxSetupResult FitWeapon(
            GameObject root,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit)
        {
            List<Renderer> renderers = CollectRenderers(root, profile);
            if (renderers.Count == 0)
                return Result(root, 0, 0, 1, "유효한 Renderer를 찾지 못했습니다.");

            if (!TryCalculateLocalBounds(root.transform, renderers, out Bounds bounds))
                return Result(root, 0, 0, 1, "Renderer Bounds 계산에 실패했습니다.");

            string groupId = profile != null ? profile.DefaultGroupId : "MainWeapon";
            return CreateOrRefit(root, root.transform, groupId, bounds, profile, forceRefit, "RendererBounds");
        }

        private static CombatHitboxSetupResult FitHumanoid(
            GameObject root,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman)
                return Result(root, 0, 0, 1, "Humanoid Animator를 찾지 못했습니다.");

            IReadOnlyList<CombatHitboxBoneRule> rules = profile?.BoneRules;
            List<CombatHitboxBoneRule> fallbackRules = null;
            if (rules == null || rules.Count == 0)
            {
                fallbackRules = CreateDefaultHumanoidRules();
                rules = fallbackRules;
            }

            int created = 0;
            int updated = 0;
            int skipped = 0;
            var messages = new List<string>();
            foreach (CombatHitboxBoneRule rule in rules)
            {
                Transform bone = animator.GetBoneTransform(rule.humanoidBone);
                if (bone == null)
                {
                    skipped++;
                    messages.Add($"{rule.groupId}: {rule.humanoidBone} 본 없음");
                    continue;
                }

                Bounds bounds = new(rule.center, MaxComponents(rule.size, Vector3.one * ResolveMinimumThickness(profile)));
                CombatHitboxSetupResult result = CreateOrRefit(
                    root,
                    bone,
                    rule.groupId,
                    bounds,
                    profile,
                    forceRefit,
                    GetPath(root.transform, bone),
                    rule.shape);
                created += result.Created;
                updated += result.Updated;
                skipped += result.Skipped;
                messages.AddRange(result.Messages);
            }

            return new CombatHitboxSetupResult(root.name, created, updated, skipped, messages);
        }

        private static CombatHitboxSetupResult FitGeneric(
            GameObject root,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit)
        {
            IReadOnlyList<string> patterns = profile?.IncludeNamePatterns;
            if (patterns == null || patterns.Count == 0)
                patterns = new[] { "hand", "foot", "claw", "tail", "wing", "head", "weapon", "sword", "axe", "horn" };

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            var candidates = transforms
                .Where(t => t != root.transform && MatchesAny(t.name, patterns))
                .Take(16)
                .ToList();
            if (candidates.Count == 0)
                return Result(root, 0, 0, 1, "이름 규칙과 일치하는 Generic 본을 찾지 못했습니다.");

            int created = 0;
            int updated = 0;
            int skipped = 0;
            var messages = new List<string>();
            foreach (Transform candidate in candidates)
            {
                Renderer[] childRenderers = candidate.GetComponentsInChildren<Renderer>(true);
                Bounds bounds;
                if (!TryCalculateLocalBounds(candidate, childRenderers, out bounds))
                    bounds = new Bounds(Vector3.zero, Vector3.one * 0.2f);

                string group = string.IsNullOrWhiteSpace(profile?.DefaultGroupId)
                    ? CombatHitbox.DefaultGroupId
                    : profile.DefaultGroupId;
                CombatHitboxSetupResult result = CreateOrRefit(
                    root,
                    candidate,
                    group,
                    bounds,
                    profile,
                    forceRefit,
                    GetPath(root.transform, candidate));
                created += result.Created;
                updated += result.Updated;
                skipped += result.Skipped;
                messages.AddRange(result.Messages);
            }

            return new CombatHitboxSetupResult(root.name, created, updated, skipped, messages);
        }

        private static CombatHitboxSetupResult RefitExisting(
            GameObject root,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit)
        {
            int updated = 0;
            int skipped = 0;
            var messages = new List<string>();
            foreach (CombatHitbox hitbox in root.GetComponentsInChildren<CombatHitbox>(true))
            {
                CombatHitboxGeneratedMarker marker = hitbox.GetComponent<CombatHitboxGeneratedMarker>();
                if (marker == null || marker.ManuallyModified && !forceRefit)
                {
                    skipped++;
                    continue;
                }

                Transform source = FindByPath(root.transform, marker.SourcePath);
                Renderer[] renderers = source != null
                    ? source.GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();
                if (!TryCalculateLocalBounds(hitbox.transform, renderers, out Bounds bounds))
                {
                    skipped++;
                    continue;
                }

                ApplyShape(hitbox.gameObject, bounds, profile, CombatHitboxPreferredShape.Auto);
                EditorUtility.SetDirty(hitbox);
                updated++;
            }
            messages.Add($"기존 자동 생성 HitBox {updated}개 Refit, {skipped}개 건너뜀");
            return new CombatHitboxSetupResult(root.name, 0, updated, skipped, messages);
        }

        private static CombatHitboxSetupResult RemoveGenerated(GameObject root)
        {
            CombatHitboxGeneratedMarker[] markers =
                root.GetComponentsInChildren<CombatHitboxGeneratedMarker>(true);
            int removed = 0;
            foreach (CombatHitboxGeneratedMarker marker in markers)
            {
                if (marker == null)
                    continue;
                Undo.DestroyObjectImmediate(marker.gameObject);
                removed++;
            }
            return Result(root, 0, removed, 0, $"자동 생성 HitBox {removed}개 제거");
        }

        private static CombatHitboxSetupResult CreateOrRefit(
            GameObject root,
            Transform parent,
            string groupId,
            Bounds bounds,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit,
            string sourcePath,
            CombatHitboxPreferredShape shapeOverride = CombatHitboxPreferredShape.Auto)
        {
            CombatHitboxGeneratedMarker existing = root
                .GetComponentsInChildren<CombatHitboxGeneratedMarker>(true)
                .FirstOrDefault(m => m.SourcePath == sourcePath
                                     && m.GetComponent<CombatHitbox>()?.GroupId == groupId);
            if (existing != null && existing.ManuallyModified && !forceRefit)
                return Result(root, 0, 0, 1, $"{groupId}: 수동 수정 항목 보존");

            GameObject hitboxObject;
            bool created = existing == null;
            if (created)
            {
                hitboxObject = new GameObject($"[HitBox] {groupId}");
                Undo.RegisterCreatedObjectUndo(hitboxObject, "Combat HitBox 생성");
                hitboxObject.transform.SetParent(parent, false);
            }
            else
            {
                hitboxObject = existing.gameObject;
                Undo.RecordObject(hitboxObject, "Combat HitBox Refit");
            }

            CombatHitboxGeneratedMarker marker =
                hitboxObject.GetComponent<CombatHitboxGeneratedMarker>()
                ?? Undo.AddComponent<CombatHitboxGeneratedMarker>(hitboxObject);
            CombatHitbox hitbox =
                hitboxObject.GetComponent<CombatHitbox>()
                ?? Undo.AddComponent<CombatHitbox>(hitboxObject);

            CombatHitboxPreferredShape resolvedShape = shapeOverride != CombatHitboxPreferredShape.Auto
                ? shapeOverride
                : profile?.PreferredShape ?? CombatHitboxPreferredShape.Auto;
            Collider collider = ApplyShape(hitboxObject, PrepareBounds(bounds, profile), profile, resolvedShape);
            hitbox.Configure(
                groupId,
                collider,
                profile == null || profile.UseSweep,
                profile?.SweepStepDistance ?? 0.15f,
                profile?.MaxSweepSteps ?? 8);

            string profilePath = profile != null ? AssetDatabase.GetAssetPath(profile) : null;
            string profileGuid = string.IsNullOrEmpty(profilePath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(profilePath);
            marker.Configure(
                GeneratorVersion,
                profileGuid,
                sourcePath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            EditorUtility.SetDirty(hitboxObject);

            return Result(
                root,
                created ? 1 : 0,
                created ? 0 : 1,
                0,
                $"{groupId}: {(created ? "생성" : "갱신")} ({collider.GetType().Name})");
        }

        private static Collider ApplyShape(
            GameObject target,
            Bounds bounds,
            CombatHitboxSetupProfileSO profile,
            CombatHitboxPreferredShape preferred)
        {
            Vector3 size = MaxComponents(bounds.size, Vector3.one * ResolveMinimumThickness(profile));
            int longestAxis = LongestAxis(size);
            float longest = size[longestAxis];
            float second = Mathf.Max(size[(longestAxis + 1) % 3], size[(longestAxis + 2) % 3]);
            bool useCapsule = preferred == CombatHitboxPreferredShape.Capsule
                              || preferred == CombatHitboxPreferredShape.Auto && longest > second * 1.8f;

            BoxCollider oldBox = target.GetComponent<BoxCollider>();
            CapsuleCollider oldCapsule = target.GetComponent<CapsuleCollider>();
            if (useCapsule)
            {
                if (oldBox != null)
                    Undo.DestroyObjectImmediate(oldBox);
                CapsuleCollider capsule = oldCapsule ?? Undo.AddComponent<CapsuleCollider>(target);
                capsule.center = bounds.center;
                capsule.direction = longestAxis;
                capsule.radius = Mathf.Max(ResolveMinimumThickness(profile) * 0.5f, second * 0.5f);
                capsule.height = Mathf.Max(longest, capsule.radius * 2f);
                capsule.isTrigger = true;
                capsule.enabled = false;
                return capsule;
            }

            if (oldCapsule != null)
                Undo.DestroyObjectImmediate(oldCapsule);
            BoxCollider box = oldBox ?? Undo.AddComponent<BoxCollider>(target);
            box.center = bounds.center;
            box.size = size;
            box.isTrigger = true;
            box.enabled = false;
            return box;
        }

        private static Bounds PrepareBounds(Bounds bounds, CombatHitboxSetupProfileSO profile)
        {
            float padding = profile?.Padding ?? 0.02f;
            Vector3 size = bounds.size + Vector3.one * padding * 2f;
            int axis = LongestAxis(size);
            float length = size[axis];
            float trimStart = length * (profile?.AxisTrimStart ?? 0f);
            float trimEnd = length * (profile?.AxisTrimEnd ?? 0f);
            size[axis] = Mathf.Max(ResolveMinimumThickness(profile), length - trimStart - trimEnd);
            Vector3 center = bounds.center;
            center[axis] += (trimStart - trimEnd) * 0.5f;
            return new Bounds(center, size);
        }

        private static List<Renderer> CollectRenderers(
            GameObject root,
            CombatHitboxSetupProfileSO profile)
        {
            IReadOnlyList<string> excludes = profile?.ExcludeNamePatterns;
            return root.GetComponentsInChildren<Renderer>(true)
                .Where(r => r is MeshRenderer or SkinnedMeshRenderer)
                .Where(r => r.GetComponentInParent<ParticleSystem>() == null)
                .Where(r => excludes == null || !MatchesAny(GetPath(root.transform, r.transform), excludes))
                .ToList();
        }

        private static bool TryCalculateLocalBounds(
            Transform localRoot,
            IEnumerable<Renderer> renderers,
            out Bounds bounds)
        {
            bool initialized = false;
            bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;
                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);
                    Vector3 local = localRoot.InverseTransformPoint(corner);
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }
            return initialized;
        }

        private static bool MatchesAny(string value, IReadOnlyList<string> patterns)
        {
            if (patterns == null)
                return false;
            for (int i = 0; i < patterns.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(patterns[i])
                    && value.IndexOf(patterns[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static List<CombatHitboxBoneRule> CreateDefaultHumanoidRules()
        {
            return new List<CombatHitboxBoneRule>
            {
                Rule("RightFist", HumanBodyBones.RightHand, CombatHitboxPreferredShape.Box, new Vector3(0.2f, 0.18f, 0.28f)),
                Rule("LeftFist", HumanBodyBones.LeftHand, CombatHitboxPreferredShape.Box, new Vector3(0.2f, 0.18f, 0.28f)),
                Rule("RightFoot", HumanBodyBones.RightFoot, CombatHitboxPreferredShape.Capsule, new Vector3(0.18f, 0.18f, 0.4f)),
                Rule("LeftFoot", HumanBodyBones.LeftFoot, CombatHitboxPreferredShape.Capsule, new Vector3(0.18f, 0.18f, 0.4f)),
                Rule("Head", HumanBodyBones.Head, CombatHitboxPreferredShape.Box, new Vector3(0.3f, 0.35f, 0.3f)),
                Rule("BodyCharge", HumanBodyBones.Chest, CombatHitboxPreferredShape.Capsule, new Vector3(0.55f, 0.8f, 0.4f)),
            };
        }

        private static CombatHitboxBoneRule Rule(
            string group,
            HumanBodyBones bone,
            CombatHitboxPreferredShape shape,
            Vector3 size)
            => new() { groupId = group, humanoidBone = bone, shape = shape, size = size };

        private static string GetPath(Transform root, Transform target)
        {
            if (target == root)
                return string.Empty;
            var names = new Stack<string>();
            while (target != null && target != root)
            {
                names.Push(target.name);
                target = target.parent;
            }
            return string.Join("/", names);
        }

        private static Transform FindByPath(Transform root, string path)
            => string.IsNullOrWhiteSpace(path) ? root : root.Find(path);

        private static int LongestAxis(Vector3 size)
            => size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;

        private static float ResolveMinimumThickness(CombatHitboxSetupProfileSO profile)
            => profile?.MinimumThickness ?? 0.04f;

        private static Vector3 MaxComponents(Vector3 a, Vector3 b)
            => new(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));

        private static CombatHitboxSetupResult Result(
            GameObject root,
            int created,
            int updated,
            int skipped,
            string message)
            => new(root != null ? root.name : "(null)", created, updated, skipped, new[] { message });
    }
}
#endif
