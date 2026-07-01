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
        ChainAutoFit,
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

        // 단일 자식 체인이 이 길이(루트 포함 노드 수) 이상이면 채찍/세그먼트 무기로 보고 ChainAutoFit을 제안한다.
        // 일반 검(Katana 등)은 2~3노드라 오탐을 피한다.
        private const int ChainDepthThreshold = 5;

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
            else if (CollectChain(root.transform).Count >= ChainDepthThreshold)
            {
                mode = CombatHitboxSetupMode.ChainAutoFit;
                targetType = "무기 체인/채찍";
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
                CombatHitboxSetupMode.ChainAutoFit => FitChain(root, profile, forceRefit),
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

        // 채찍/세그먼트 무기: 단일 자식 체인을 따라 내려가며 각 링크(노드→자식)에 캡슐 HitBox를 만든다.
        // 렌더러가 아니라 transform 위치만 쓰므로 스킨드 메시/세그먼트 메시/MagicaCloth 어느 쪽이든 동작한다.
        private static CombatHitboxSetupResult FitChain(
            GameObject root,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit)
        {
            List<Transform> chain = CollectChain(root.transform);
            if (chain.Count < 2)
            {
                return Result(root, 0, 0, 1,
                    "단일 자식 체인을 찾지 못했습니다(노드 2개 이상 필요). 체인 첫 노드(예: Sword_Blade01)를 선택하세요.");
            }

            int stride = Mathf.Max(1, profile?.ChainSegmentStride ?? 1);
            var sampled = new List<Transform>();
            for (int i = 0; i < chain.Count; i += stride)
                sampled.Add(chain[i]);
            // 말단까지 반드시 덮도록 마지막 노드를 포함시킨다(stride로 건너뛰었을 수 있음).
            if (sampled[sampled.Count - 1] != chain[chain.Count - 1])
                sampled.Add(chain[chain.Count - 1]);

            // 프로필이 없으면 FitWeapon과 동일하게 무기 기본 그룹("MainWeapon")을 쓴다.
            string groupId = profile != null ? profile.DefaultGroupId : "MainWeapon";
            float radius = profile?.ChainRadius ?? 0.08f;

            int created = 0;
            int updated = 0;
            int skipped = 0;
            var messages = new List<string>
            {
                $"체인 {chain.Count}노드 · 캡슐 {sampled.Count - 1}개 (stride {stride}, 반경 {radius:0.###}, 그룹 '{groupId}')",
            };
            // 첫 링크 하나만 스윙 트레일 리더로 삼고 끝 노드(chain 말단)를 트레일 종점으로 준다.
            // 나머지 링크는 트레일 off(상시 형상만) → 트레일 비용이 세그먼트 수와 무관하게 1줄로 고정.
            Transform chainTip = chain[chain.Count - 1];
            for (int i = 0; i < sampled.Count - 1; i++)
            {
                CombatHitboxSetupResult result = CreateOrRefitChainLink(
                    root,
                    sampled[i],
                    sampled[i + 1],
                    groupId,
                    radius,
                    profile,
                    forceRefit,
                    GetPath(root.transform, sampled[i]),
                    i == 0 ? chainTip : null);
                created += result.Created;
                updated += result.Updated;
                skipped += result.Skipped;
                messages.AddRange(result.Messages);
            }

            return new CombatHitboxSetupResult(root.name, created, updated, skipped, messages);
        }

        // 캡슐 반경의 월드 변환 스케일. direction=2(로컬 Z축)일 때 반경은 X/Y 중 큰 값으로 스케일된다.
        // CombatHitbox.TryGetWorldShape의 radialScale 계산과 정확히 일치시켜야 월드 반경이 어긋나지 않는다.
        private static float ComputeRadialScaleXY(Transform t)
        {
            Vector3 s = t.lossyScale;
            return Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)));
        }

        // 루트에서 첫 번째 자식을 따라 내려가며 transform 체인을 수집한다(사이클 가드 포함).
        private static List<Transform> CollectChain(Transform start)
        {
            var chain = new List<Transform>();
            var guard = new HashSet<Transform>();
            Transform current = start;
            while (current != null && guard.Add(current))
            {
                chain.Add(current);
                current = current.childCount > 0 ? current.GetChild(0) : null;
            }
            return chain;
        }

        private static CombatHitboxSetupResult CreateOrRefitChainLink(
            GameObject root,
            Transform node,
            Transform child,
            string groupId,
            float radius,
            CombatHitboxSetupProfileSO profile,
            bool forceRefit,
            string sourcePath,
            Transform trailEndpoint)
        {
            CombatHitboxGeneratedMarker existing = root
                .GetComponentsInChildren<CombatHitboxGeneratedMarker>(true)
                .FirstOrDefault(m => m.SourcePath == sourcePath
                                     && m.GetComponent<CombatHitbox>()?.GroupId == groupId);
            if (existing != null && existing.ManuallyModified && !forceRefit)
                return Result(root, 0, 0, 1, $"{groupId}: 수동 수정 항목 보존 ({node.name})");

            // 세그먼트 방향(노드 로컬 공간)으로 캡슐 축을 정렬한다. 자식 오프셋이 비(非)축이어도 정확히 따라간다.
            Vector3 localDir = node.InverseTransformPoint(child.position);
            float length = localDir.magnitude;
            if (length <= Mathf.Epsilon)
                return Result(root, 0, 0, 1, $"{groupId}: {node.name} 세그먼트 길이가 0이라 건너뜀");

            GameObject hitboxObject;
            bool created = existing == null;
            if (created)
            {
                hitboxObject = new GameObject($"[HitBox] {groupId} ({node.name})");
                Undo.RegisterCreatedObjectUndo(hitboxObject, "Combat HitBox 체인 생성");
                hitboxObject.transform.SetParent(node, false);
            }
            else
            {
                hitboxObject = existing.gameObject;
                Undo.RecordObject(hitboxObject.transform, "Combat HitBox 체인 Refit");
            }

            // 세그먼트가 (노드 로컬) 수직에 가까우면 LookRotation의 up과 평행해져 불안정하므로 up을 교체한다.
            Vector3 dir = localDir.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            hitboxObject.transform.localPosition = Vector3.zero;
            hitboxObject.transform.localRotation = Quaternion.LookRotation(dir, up);
            hitboxObject.transform.localScale = Vector3.one;

            CombatHitboxGeneratedMarker marker =
                hitboxObject.GetComponent<CombatHitboxGeneratedMarker>()
                ?? Undo.AddComponent<CombatHitboxGeneratedMarker>(hitboxObject);
            CombatHitbox hitbox =
                hitboxObject.GetComponent<CombatHitbox>()
                ?? Undo.AddComponent<CombatHitbox>(hitboxObject);

            BoxCollider oldBox = hitboxObject.GetComponent<BoxCollider>();
            if (oldBox != null)
                Undo.DestroyObjectImmediate(oldBox);
            CapsuleCollider capsule = EnsureComponent(hitboxObject, hitboxObject.GetComponent<CapsuleCollider>());
            if (capsule == null)
                return Result(root, 0, 0, 1, $"{groupId}: CapsuleCollider 생성 실패 ({node.name}) — 콘솔 로그 확인");

            // radius/length는 노드 로컬 공간 값이지만 본 lossyScale에 따라 월드 크기가 달라진다.
            // _chainRadius를 "월드 기준" 값으로 해석해 보정한다. 단, 런타임 TryGetWorldShape는
            // 캡슐 반경을 radial scale(direction=2이면 max(x,y))로 월드 변환하므로, 여기서도
            // 같은 radial scale로 나눠야 월드 반경이 정확히 radius가 된다(평균 스케일로 나누면
            // 비균등 본에서 월드 반경이 부풀어 오른다).
            float radialScale = ComputeRadialScaleXY(node);
            float localRadius = Mathf.Max(0.001f, radius / radialScale);
            capsule.direction = 2; // 로컬 Z = 세그먼트 방향
            capsule.center = new Vector3(0f, 0f, length * 0.5f);
            capsule.radius = localRadius;
            capsule.height = Mathf.Max(length, localRadius * 2f);
            capsule.isTrigger = true;
            capsule.enabled = false;

            // 채찍 끝 세그먼트는 프레임당 매우 빠르게 움직인다. 스윕 스텝이 반경보다 크면 샘플 사이에
            // 틈이 생겨 빠른 끝점이 적을 관통(터널링)한다 → 검보다 덜 민감하게 느껴지는 주원인.
            // 스텝을 반경 이하로 조여 연속 오버랩 질의가 겹치게 하고, 빠른 끝점을 위해 캡을 올린다.
            // (월드 기준 radius를 그대로 스윕 거리 단위로 쓴다 — 둘 다 월드 단위)
            float chainSweepStep = Mathf.Clamp(radius * 0.9f, 0.01f, profile?.SweepStepDistance ?? 0.1f);
            int chainMaxSweepSteps = Mathf.Clamp(Mathf.Max(profile?.MaxSweepSteps ?? 0, 24), 1, 32);
            hitbox.Configure(
                groupId,
                capsule,
                profile == null || profile.UseSweep,
                chainSweepStep,
                chainMaxSweepSteps);
            // 세그먼트가 많은 채찍은 per-세그먼트 스윙 트레일이 기즈모 렉의 주범이므로 끄고,
            // 대신 누적 없는 상시 형상 표시를 켠다. 상시 형상의 캡슐은 CombatHitbox에서 와이어 스피어가 아닌
            // 선 윤곽으로 그려지므로(렌더 단계에서 일괄 처리), 세그먼트가 많아도 가볍다.
            hitbox.SetStaticShapeEnabled(true);
            if (trailEndpoint != null)
                // 첫 링크만: '첫 노드→끝 노드' 직선 하나로 스윙 궤적을 보여준다(트레일 비용 일정).
                hitbox.SetChainTrail(trailEndpoint, Mathf.Max(0.02f, radius * 0.5f));
            else
                hitbox.SetSwingTrailEnabled(false);

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
                $"{groupId}: {(created ? "생성" : "갱신")} (Capsule, {node.name}→{child.name})");
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
            if (collider == null)
                return Result(root, 0, 0, 1, $"{groupId}: Collider 생성 실패 — 콘솔 로그를 확인하세요.");
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
                CapsuleCollider capsule = EnsureComponent(target, oldCapsule);
                if (capsule == null)
                {
                    Debug.LogError($"[CombatHitboxAutoFitter] {target.name}에 CapsuleCollider를 추가하지 못했습니다.", target);
                    return null;
                }
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
            BoxCollider box = EnsureComponent(target, oldBox);
            if (box == null)
            {
                Debug.LogError($"[CombatHitboxAutoFitter] {target.name}에 BoxCollider를 추가하지 못했습니다.", target);
                return null;
            }
            box.center = bounds.center;
            box.size = size;
            box.isTrigger = true;
            box.enabled = false;
            return box;
        }

        /// <summary>
        /// 콜라이더를 안전하게 확보한다. Undo.AddComponent는 RegisterCreatedObjectUndo로 만든 새 GameObject에
        /// 같은 프레임 다중 호출 시 일부가 null을 반환하는 에디터 한계가 있어(marker+CombatHitbox 추가 후
        /// Collider 추가가 실패), null이면 일반 AddComponent로 폴백한다. 객체 생성 전체가 이미 하나의 Undo로
        /// 묶이므로 폴백해도 Undo 정합성은 유지된다.
        /// </summary>
        private static T EnsureComponent<T>(GameObject target, T existing) where T : UnityEngine.Component
        {
            if (existing != null)
                return existing;
            T component = target.GetComponent<T>();
            if (component != null)
                return component;
            component = Undo.AddComponent<T>(target);
            if (component == null)
            {
                // Undo.AddComponent 폴백은 Undo 스택에 잡히지 않으므로, refit(기존 GameObject) 경로에서도
                // Ctrl+Z로 잔존 컴포넌트가 남지 않도록 생성 자체를 명시적으로 등록한다.
                component = target.AddComponent<T>();
                if (component != null)
                    Undo.RegisterCreatedObjectUndo(component, "Add Collider");
            }
            return component;
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
