#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;
using MotionData = UPlayGround.Animation.Motion;

namespace UPlayGround.Tool.Editor.AI
{
    /// <summary>
    /// 몬스터 프리팹의 실제 공격 포즈와 부착형 HitBox를 샘플링해
    /// AI 근접 Ability의 안전한 시작 거리를 산출한다.
    /// </summary>
    public static class MonsterMeleeRangeBakeTool
    {
        private const string RequestPath = "Library/MonsterMeleeRangeBake.request";
        private static readonly TimeSpan RequestExpiry = TimeSpan.FromMinutes(10);
        private const string ReportPath = "Library/MonsterMeleeRangeBakeReport.json";
        private const float SampleRate = 120f;
        private const float DistanceStep = 0.05f;
        private const float MinimumTestDistance = 0.1f;
        private const float MaximumTestDistance = 8f;
        private const float MaximumRangeSafetyMargin = 0.15f;
        private const float MinimumRangeSafetyMargin = 0.05f;
        private const float TargetCapsuleRadius = 0.5f;
        private const float TargetCapsuleHeight = 1.6f;
        private const float MinimumUsableRange = 0.45f;
        private const float BehaviorCoverageThreshold = 0.75f;

        [Serializable]
        private sealed class BakeReport
        {
            public string generatedAt;
            public bool applied;
            public int actorDefinitionCount;
            public int uniqueAbilityCount;
            public int measuredCount;
            public int warpMeasuredCount;
            public int warpPreservedCount;
            public int projectileSkippedCount;
            public int aerialSkippedCount;
            public int blockedCount;
            public int changedAbilityCount;
            public int changedBehaviorCount;
            public List<BakeEntry> entries = new();
            public List<BehaviorEntry> behaviors = new();
            public List<string> messages = new();
        }

        [Serializable]
        private sealed class BakeEntry
        {
            public string actorDefinition;
            public string ability;
            public string abilityId;
            public string motionKey;
            public string motionAsset;
            public string status;
            public string message;
            public float currentMinDistance;
            public float currentMaxDistance;
            public float measuredMinDistance;
            public float measuredMaxDistance;
            public float recommendedMinDistance;
            public float recommendedMaxDistance;
            public float warpReachDistance;
            public float warpGateMaxDistance;
            public int warpWindowCount;
            public int usableBakedWarpCount;
            public int hitSampleCount;
        }

        [Serializable]
        private sealed class BehaviorEntry
        {
            public string behavior;
            public string behaviorPath;
            public float currentOptimal;
            public float currentMin;
            public float currentChaseStop;
            public float currentPersonalSpace;
            public float recommendedOptimal;
            public float recommendedMin;
            public float recommendedChaseStop;
            public float recommendedPersonalSpace;
            public int contributingAbilityCount;
            public string status;
            public string message;
        }

        private sealed class AbilityAggregate
        {
            public GameplayAbilitySO Ability;
            public readonly List<BakeEntry> Entries = new();
            public bool HasBlocker;
            public bool HasMeasured;
            public float RecommendedMin;
            public float RecommendedMax = float.MaxValue;
            public float Weight;
        }

        private sealed class AnalysisContext
        {
            public BakeReport Report;
            public readonly Dictionary<GameplayAbilitySO, AbilityAggregate> Abilities = new();
            public readonly Dictionary<EnemyBehaviorSO, HashSet<GameplayAbilitySO>> BehaviorAbilities = new();
            public readonly Dictionary<EnemyBehaviorSO, float> BehaviorPersonalSpaces = new();
        }

        private readonly struct CollisionWindow
        {
            public readonly float Start;
            public readonly float End;
            public readonly HashSet<string> Groups;

            public CollisionWindow(float start, float end, HashSet<string> groups)
            {
                Start = start;
                End = end;
                Groups = groups;
            }
        }

        private readonly struct WarpWindow
        {
            public readonly float Start;
            public readonly float End;
            public readonly float MaxDistance;
            public readonly float MaxSpeed;
            public readonly float TranslationWeight;
            public readonly float HorizontalOffset;
            public readonly bool RequiresBakedRoot;
            public readonly bool HasUsableBakedRoot;

            public WarpWindow(
                float start,
                float end,
                float maxDistance,
                float maxSpeed,
                float translationWeight,
                float horizontalOffset,
                bool requiresBakedRoot,
                bool hasUsableBakedRoot)
            {
                Start = start;
                End = end;
                MaxDistance = maxDistance;
                MaxSpeed = maxSpeed;
                TranslationWeight = translationWeight;
                HorizontalOffset = horizontalOffset;
                RequiresBakedRoot = requiresBakedRoot;
                HasUsableBakedRoot = hasUsableBakedRoot;
            }
        }

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedRun()
        {
            EditorApplication.delayCall += TryRunRequestedCommand;
        }

        [global::UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/AI/근접 공격 거리/전체 분석")]
        public static void AnalyzeAll()
        {
            Run(apply: false);
        }

        [global::UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/AI/근접 공격 거리/전체 베이크")]
        public static void BakeAll()
        {
            Run(apply: true);
        }

        [global::UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/AI/근접 공격 거리/최근 보고서 열기")]
        private static void OpenLastReport()
        {
            string fullPath = Path.GetFullPath(ReportPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[MonsterMeleeRangeBake] 이전 리포트가 없습니다.");
                return;
            }
            EditorUtility.RevealInFinder(fullPath);
        }

        private static void TryRunRequestedCommand()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(RequestPath))
                return;

            string command;
            DateTime requestedAtUtc;
            try
            {
                command = File.ReadAllText(RequestPath).Trim();
                requestedAtUtc = File.GetLastWriteTimeUtc(RequestPath);
                File.Delete(RequestPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MonsterMeleeRangeBake] 요청 파일 처리 실패: {exception}");
                return;
            }

            bool apply = string.Equals(command, "apply", StringComparison.OrdinalIgnoreCase);

            // apply는 프로젝트 전역 Ability/BehaviorSO를 덮어쓴다. 남아 있던 오래된 요청 파일이
            // 한참 뒤의 도메인 리로드에서 조용히 실행되는 것을 막기 위해 신선도를 확인한다.
            TimeSpan age = DateTime.UtcNow - requestedAtUtc;
            if (apply && age > RequestExpiry)
            {
                Debug.LogWarning(
                    $"[MonsterMeleeRangeBake] {age.TotalMinutes:F1}분 지난 apply 요청을 무시했습니다. " +
                    "필요하면 요청 파일을 다시 작성하거나 도구 메뉴에서 직접 실행하세요.");
                return;
            }

            if (apply)
                Debug.LogWarning("[MonsterMeleeRangeBake] 요청 파일에 의해 Bake All(에셋 변경)을 실행합니다.");

            Run(apply);
        }

        private static void Run(bool apply)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[MonsterMeleeRangeBake] Play Mode에서는 실행할 수 없습니다.");
                return;
            }

            try
            {
                Debug.Log($"[MonsterMeleeRangeBake] {(apply ? "Bake All" : "Analyze All")} 시작");
                AnalysisContext context = AnalyzeProject();
                if (apply)
                    Apply(context);
                context.Report.applied = apply;
                WriteReport(context.Report);
                Debug.Log(
                    $"[MonsterMeleeRangeBake] 완료. measured={context.Report.measuredCount}, " +
                    $"warp-preserved={context.Report.warpPreservedCount}, " +
                    $"projectile={context.Report.projectileSkippedCount}, " +
                    $"blocked={context.Report.blockedCount}, " +
                    $"ability-changed={context.Report.changedAbilityCount}, " +
                    $"behavior-changed={context.Report.changedBehaviorCount}, report={ReportPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                var failed = new BakeReport
                {
                    generatedAt = DateTime.Now.ToString("O"),
                    applied = false,
                    messages = new List<string> { exception.ToString() },
                };
                WriteReport(failed);
            }
        }

        private static AnalysisContext AnalyzeProject()
        {
            var context = new AnalysisContext
            {
                Report = new BakeReport
                {
                    generatedAt = DateTime.Now.ToString("O"),
                },
            };

            ActorDefinitionSO[] definitions = AssetDatabase
                .FindAssets($"t:{nameof(ActorDefinitionSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(definition => definition != null
                                     && definition.monsterProfile != null
                                     && definition.EffectiveAbilitySet != null
                                     && definition.prefab != null)
                .OrderBy(definition => definition.name, StringComparer.Ordinal)
                .ToArray();

            context.Report.actorDefinitionCount = definitions.Length;
            foreach (ActorDefinitionSO definition in definitions)
                AnalyzeDefinition(definition, context);

            FinalizeAbilityAggregates(context);
            BuildBehaviorRecommendations(context, definitions);
            context.Report.uniqueAbilityCount = context.Abilities.Count;
            return context;
        }

        private static void AnalyzeDefinition(ActorDefinitionSO definition, AnalysisContext context)
        {
            string prefabPath = AssetDatabase.GetAssetPath(definition.prefab);
            if (string.IsNullOrWhiteSpace(prefabPath)
                || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                AddDefinitionBlockers(definition, context, "유효한 Prefab 에셋 경로가 아닙니다.");
                return;
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                ActorAnimator animator = root.GetComponentInChildren<ActorAnimator>(true);
                if (animator == null || animator.MotionSet == null)
                {
                    AddDefinitionBlockers(definition, context, "ActorAnimator 또는 ActorAnimationMotionSet이 없습니다.");
                    return;
                }

                CombatHitbox[] hitboxes = root.GetComponentsInChildren<CombatHitbox>(true)
                    .Where(hitbox => hitbox != null
                                     && hitbox.IsSupported)
                    .ToArray();
                float personalSpace = ResolvePersonalSpace(root);
                EnemyBehaviorSO behavior = definition.EffectiveBehaviorData;
                if (behavior != null)
                {
                    if (!context.BehaviorPersonalSpaces.TryGetValue(behavior, out float current)
                        || personalSpace > current)
                        context.BehaviorPersonalSpaces[behavior] = personalSpace;
                    if (!context.BehaviorAbilities.TryGetValue(behavior, out HashSet<GameplayAbilitySO> set))
                    {
                        set = new HashSet<GameplayAbilitySO>();
                        context.BehaviorAbilities.Add(behavior, set);
                    }
                    foreach (GameplayAbilitySO ability in definition.EffectiveAbilitySet.EnumerateAll())
                        if (ability != null)
                            set.Add(ability);
                }

                foreach (GameplayAbilitySO ability in definition.EffectiveAbilitySet
                             .EnumerateAll()
                             .Where(ability => ability != null)
                             .Distinct())
                {
                    AnalyzeAbility(definition, root, animator, hitboxes, ability, context);
                }
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AnalyzeAbility(
            ActorDefinitionSO definition,
            GameObject root,
            ActorAnimator animator,
            CombatHitbox[] hitboxes,
            GameplayAbilitySO ability,
            AnalysisContext context)
        {
            if (ability.variants == null)
                return;

            foreach (AbilityVariantDefinition variant in ability.variants)
            {
                if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(variant, out AbilityAttackInfo attackInfo)
                    || attackInfo?.aiSelectable != true
                    || attackInfo.baseInfo?.HasHitPhases != true)
                    continue;

                AbilityAggregate aggregate = GetAggregate(context, ability);
                aggregate.Weight = Mathf.Max(aggregate.Weight, attackInfo.selectionWeight);
                var entry = new BakeEntry
                {
                    actorDefinition = definition.name,
                    ability = ability.name,
                    abilityId = ability.abilityId,
                    motionKey = attackInfo.motionKey.ToString(),
                    currentMinDistance = ability.activation?.minDistance ?? 0f,
                    currentMaxDistance = ability.activation?.maxDistance ?? 0f,
                };
                aggregate.Entries.Add(entry);
                context.Report.entries.Add(entry);

                if (attackInfo.isAerialSkill || attackInfo.isDiveAttack)
                {
                    entry.status = "AerialSkipped";
                    entry.message = "공중/다이브 공격은 지상 근접 사거리 베이크에서 제외합니다.";
                    context.Report.aerialSkippedCount++;
                    continue;
                }

                MotionSetAsset motionAsset = animator.MotionSet.GetAbilityMotionAsset(attackInfo.motionKey);
                entry.motionAsset = motionAsset != null ? motionAsset.name : string.Empty;
                if (motionAsset?.motionSet == null)
                {
                    Block(entry, aggregate, context, "이 액터의 MotionSet에서 Motion Key를 해석할 수 없습니다.");
                    continue;
                }

                bool hasCollision = HasEvent<BeginCollisionEvent>(motionAsset.motionSet);
                bool hasProjectile = HasEvent<SpawnProjectileEvent>(motionAsset.motionSet);
                bool hasWarp = HasEvent<MotionEvent_MotionWarp>(motionAsset.motionSet);
                if (!hasCollision)
                {
                    if (hasProjectile)
                    {
                        entry.status = "ProjectileSkipped";
                        entry.message = "SpawnProjectileEvent 기반 공격입니다.";
                        context.Report.projectileSkippedCount++;
                    }
                    else
                    {
                        Block(entry, aggregate, context, "Collision/Projectile 이벤트가 없어 도달 거리를 측정할 수 없습니다.");
                    }
                    continue;
                }

                if (hitboxes.Length == 0)
                {
                    Block(entry, aggregate, context, "프리팹에 지원되는 CombatHitbox가 없습니다.");
                    continue;
                }

                MeasureAttachedHitboxRange(root, animator, hitboxes, motionAsset.motionSet, attackInfo, entry);
                if (hasWarp)
                {
                    if (entry.hitSampleCount == 0 || entry.recommendedMaxDistance < MinimumUsableRange)
                    {
                        Block(entry, aggregate, context, "MotionWarp 공격의 Collision Window에서 HitBox 포즈를 찾지 못했습니다.");
                        continue;
                    }

                    ApplyConservativeWarpReach(root, motionAsset.motionSet, attackInfo, entry);
                    entry.status = "Measured";
                    entry.message = entry.usableBakedWarpCount > 0
                        ? "베이크된 DeltaWarp와 런타임 속도/게이트 상한을 보수적으로 합산했습니다."
                        : "유효한 DeltaWarp 루트 베이크가 없어 원본 HitBox 포즈 범위만 사용했습니다.";
                    aggregate.HasMeasured = true;
                    aggregate.RecommendedMin = Mathf.Max(aggregate.RecommendedMin, entry.recommendedMinDistance);
                    aggregate.RecommendedMax = Mathf.Min(aggregate.RecommendedMax, entry.recommendedMaxDistance);
                    context.Report.measuredCount++;
                    context.Report.warpMeasuredCount++;
                    continue;
                }

                if (entry.hitSampleCount == 0 || entry.recommendedMaxDistance < MinimumUsableRange)
                {
                    Block(entry, aggregate, context, "Collision Window에서 대상 캡슐과 겹치는 HitBox 포즈를 찾지 못했습니다.");
                    continue;
                }

                if (entry.currentMaxDistance > 0f)
                    entry.recommendedMaxDistance = Mathf.Min(
                        entry.recommendedMaxDistance,
                        entry.currentMaxDistance);

                entry.status = "Measured";
                aggregate.HasMeasured = true;
                aggregate.RecommendedMin = Mathf.Max(aggregate.RecommendedMin, entry.recommendedMinDistance);
                aggregate.RecommendedMax = Mathf.Min(aggregate.RecommendedMax, entry.recommendedMaxDistance);
                context.Report.measuredCount++;
            }
        }

        private static void MeasureAttachedHitboxRange(
            GameObject root,
            ActorAnimator actorAnimator,
            CombatHitbox[] hitboxes,
            MotionSet motionSet,
            AbilityAttackInfo attackInfo,
            BakeEntry entry)
        {
            List<CollisionWindow> windows = CollectCollisionWindows(motionSet, attackInfo);
            if (windows.Count == 0)
                return;

            Animator animator = actorAnimator.GetComponent<Animator>()
                                ?? actorAnimator.GetComponentInChildren<Animator>(true);
            if (animator == null)
                return;

            ActorAnimator subActorAnimator = actorAnimator.SubAnimator;
            Animator subAnimator = subActorAnimator != null
                ? subActorAnimator.GetComponent<Animator>()
                  ?? subActorAnimator.GetComponentInChildren<Animator>(true)
                : null;

            Transform distanceOrigin = root.GetComponentInChildren<GameActor>(true)?.transform
                                       ?? root.transform;
            Vector3 initialPosition = distanceOrigin.position;
            Quaternion initialRotation = distanceOrigin.rotation;
            int distanceCount = Mathf.FloorToInt(
                (MaximumTestDistance - MinimumTestDistance) / DistanceStep) + 1;
            var hitDistances = new bool[distanceCount];

            var requiredGroups = new HashSet<string>(
                windows.SelectMany(window => window.Groups),
                StringComparer.OrdinalIgnoreCase);
            var activationStates = new Dictionary<GameObject, bool>();
            foreach (CombatHitbox hitbox in hitboxes)
            {
                if (hitbox == null || !requiredGroups.Contains(hitbox.GroupId))
                    continue;
                Transform current = hitbox.transform;
                while (current != null && current != root.transform)
                {
                    if (!activationStates.ContainsKey(current.gameObject))
                        activationStates.Add(current.gameObject, current.gameObject.activeSelf);
                    current = current.parent;
                }
            }

            bool startedAnimationMode = false;
            try
            {
                // activeSelf는 오브젝트별 독립 값이라 활성화/복원 순서는 결과에 영향이 없다.
                // Dictionary 열거 순서에 의미를 부여하지 않는다.
                foreach (GameObject gameObject in activationStates.Keys)
                    gameObject.SetActive(true);
                if (!AnimationMode.InAnimationMode())
                {
                    AnimationMode.StartAnimationMode();
                    startedAnimationMode = true;
                }

                foreach (CollisionWindow window in windows)
                {
                    float start = Mathf.Max(0f, window.Start);
                    float end = Mathf.Min(motionSet.TotalDuration, Mathf.Max(window.Start, window.End));
                    int sampleCount = Mathf.Max(1, Mathf.CeilToInt((end - start) * SampleRate));
                    for (int sample = 0; sample <= sampleCount; sample++)
                    {
                        float t = Mathf.Lerp(start, end, sample / (float)sampleCount);
                        if (!motionSet.GetMotionAtTime(t, out int motionIndex, out float localTime)
                            || motionIndex < 0
                            || motionIndex >= motionSet.motions.Count)
                            continue;

                        MotionData motion = motionSet.motions[motionIndex];
                        if (motion?.motionClip == null)
                            continue;
                        float clipTime = motion.ClipStartTime
                                         + localTime * Mathf.Max(0.0001f, motion.playbackSpeed);

                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(animator.gameObject, motion.motionClip, clipTime);
                        // 채찍처럼 별도 Animator를 쓰는 무기도 런타임 ActorAnimator.PlayMotion과
                        // 동일한 시간의 같은 클립으로 구동해야 실제 HitBox 포즈를 측정할 수 있다.
                        if (subAnimator != null && subAnimator != animator)
                            AnimationMode.SampleAnimationClip(
                                subAnimator.gameObject,
                                motion.motionClip,
                                clipTime);
                        AnimationMode.EndSampling();
                        Physics.SyncTransforms();

                        for (int distanceIndex = 0; distanceIndex < distanceCount; distanceIndex++)
                        {
                            if (hitDistances[distanceIndex])
                                continue;
                            float distance = MinimumTestDistance + distanceIndex * DistanceStep;
                            Vector3 targetBase = initialPosition
                                                 + initialRotation * Vector3.forward * distance;
                            if (OverlapsAnyHitbox(targetBase, initialRotation, hitboxes, window.Groups))
                            {
                                hitDistances[distanceIndex] = true;
                                entry.hitSampleCount++;
                            }
                        }
                    }
                }
            }
            finally
            {
                foreach ((GameObject gameObject, bool wasActive) in activationStates)
                    if (gameObject != null)
                        gameObject.SetActive(wasActive);
                if (startedAnimationMode && AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
            }

            if (!TryFindBestContiguousRange(hitDistances, out int first, out int last))
                return;

            float measuredMin = MinimumTestDistance + first * DistanceStep;
            float measuredMax = MinimumTestDistance + last * DistanceStep;
            entry.measuredMinDistance = RoundToStep(measuredMin);
            entry.measuredMaxDistance = RoundToStep(measuredMax);
            entry.recommendedMinDistance = first == 0
                ? 0f
                : RoundUpToStep(measuredMin + MinimumRangeSafetyMargin);
            entry.recommendedMaxDistance = RoundDownToStep(
                Mathf.Max(0f, measuredMax - MaximumRangeSafetyMargin));
        }

        private static bool OverlapsAnyHitbox(
            Vector3 targetBase,
            Quaternion targetRotation,
            CombatHitbox[] hitboxes,
            HashSet<string> activeGroups)
        {
            Vector3 targetUp = targetRotation * Vector3.up;
            float halfSegment = Mathf.Max(0f, TargetCapsuleHeight * 0.5f - TargetCapsuleRadius);
            Vector3 targetCenter = targetBase + targetUp * (TargetCapsuleHeight * 0.5f);
            Vector3 targetPoint0 = targetCenter - targetUp * halfSegment;
            Vector3 targetPoint1 = targetCenter + targetUp * halfSegment;

            foreach (CombatHitbox hitbox in hitboxes)
            {
                if (hitbox?.ShapeCollider == null
                    || !activeGroups.Contains(hitbox.GroupId)
                    || !hitbox.TryGetWorldShape(out CombatHitboxShape shape))
                    continue;

                if (shape.Type == CombatHitboxShapeType.Capsule)
                {
                    float radius = shape.Radius + TargetCapsuleRadius;
                    if (SegmentSegmentSqrDistance(
                            shape.Point0,
                            shape.Point1,
                            targetPoint0,
                            targetPoint1) <= radius * radius)
                        return true;
                }
                else if (SegmentObbSqrDistance(
                             targetPoint0,
                             targetPoint1,
                             shape.Center,
                             shape.Rotation,
                             shape.HalfExtents) <= TargetCapsuleRadius * TargetCapsuleRadius)
                {
                    return true;
                }
            }
            return false;
        }

        private static float SegmentObbSqrDistance(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            Vector3 boxCenter,
            Quaternion boxRotation,
            Vector3 halfExtents)
        {
            Quaternion inverse = Quaternion.Inverse(boxRotation);
            Vector3 start = inverse * (segmentStart - boxCenter);
            Vector3 end = inverse * (segmentEnd - boxCenter);
            const int subdivisions = 16;
            float best = float.MaxValue;
            for (int i = 0; i <= subdivisions; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, i / (float)subdivisions);
                float dx = Mathf.Max(Mathf.Abs(point.x) - halfExtents.x, 0f);
                float dy = Mathf.Max(Mathf.Abs(point.y) - halfExtents.y, 0f);
                float dz = Mathf.Max(Mathf.Abs(point.z) - halfExtents.z, 0f);
                best = Mathf.Min(best, dx * dx + dy * dy + dz * dz);
            }
            return best;
        }

        private static float SegmentSegmentSqrDistance(
            Vector3 p1,
            Vector3 q1,
            Vector3 p2,
            Vector3 q2)
        {
            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);
            float s;
            float t;

            if (a <= 1e-8f && e <= 1e-8f)
                return (p1 - p2).sqrMagnitude;
            if (a <= 1e-8f)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= 1e-8f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denominator = a * e - b * b;
                    s = denominator > 1e-8f
                        ? Mathf.Clamp01((b * f - c * e) / denominator)
                        : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            Vector3 c1 = p1 + d1 * s;
            Vector3 c2 = p2 + d2 * t;
            return (c1 - c2).sqrMagnitude;
        }

        private static List<CollisionWindow> CollectCollisionWindows(
            MotionSet motionSet,
            AbilityAttackInfo attackInfo)
        {
            var result = new List<CollisionWindow>();
            if (motionSet.globalEvents != null)
            {
                foreach (MotionEventBase motionEvent in motionSet.globalEvents)
                    if (motionEvent is BeginCollisionEvent collision)
                        result.Add(CreateWindow(collision, 0f, attackInfo));
            }

            float offset = 0f;
            if (motionSet.motions != null)
            {
                foreach (MotionData motion in motionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase motionEvent in motion.events)
                            if (motionEvent is BeginCollisionEvent collision)
                                result.Add(CreateWindow(collision, offset, attackInfo));
                    }
                    offset += motion?.Duration ?? 0f;
                }
            }
            return result;
        }

        private static void ApplyConservativeWarpReach(
            GameObject root,
            MotionSet motionSet,
            AbilityAttackInfo attackInfo,
            BakeEntry entry)
        {
            List<CollisionWindow> collisions = CollectCollisionWindows(motionSet, attackInfo);
            List<WarpWindow> warps = CollectWarpWindows(motionSet, root);
            entry.warpWindowCount = warps.Count;
            entry.usableBakedWarpCount = warps.Count(warp => warp.HasUsableBakedRoot);

            float baseReach = entry.recommendedMaxDistance;
            float bestReach = baseReach;
            float bestTranslation = 0f;
            float bestGate = 0f;
            foreach (CollisionWindow collision in collisions)
            {
                foreach (WarpWindow warp in warps)
                {
                    if (warp.Start > collision.End + 0.0001f)
                        continue;

                    float available = Mathf.Min(warp.End, collision.End) - warp.Start;
                    if (available <= 0f)
                        continue;

                    bool canUseTargetCorrection = !warp.RequiresBakedRoot || warp.HasUsableBakedRoot;
                    float integratedBlendTime = canUseTargetCorrection
                        ? IntegrateWarpBlend(available)
                        : 0f;
                    float translation = Mathf.Max(
                        0f,
                        warp.MaxSpeed
                        * integratedBlendTime
                        * Mathf.Clamp01(warp.TranslationWeight)
                        - warp.HorizontalOffset);
                    float candidate = Mathf.Min(warp.MaxDistance, baseReach + translation);
                    if (candidate > bestReach)
                    {
                        bestReach = candidate;
                        bestTranslation = translation;
                        bestGate = warp.MaxDistance;
                    }
                }
            }

            // 이번 작업은 허공 공격 제거가 목적이므로 기존 activation보다 사거리를 넓히지 않는다.
            if (entry.currentMaxDistance > 0f)
                bestReach = Mathf.Min(bestReach, entry.currentMaxDistance);
            entry.warpReachDistance = RoundDownToStep(bestTranslation);
            entry.warpGateMaxDistance = RoundDownToStep(bestGate);
            entry.recommendedMaxDistance = RoundDownToStep(bestReach);
        }

        private static float IntegrateWarpBlend(float duration)
        {
            // MotionWarpController의 blendWeight = MoveTowards(..., deltaTime * 15) 적분.
            const float rampRate = 15f;
            float rampDuration = 1f / rampRate;
            return duration <= rampDuration
                ? 0.5f * rampRate * duration * duration
                : duration - 0.5f * rampDuration;
        }

        private static List<WarpWindow> CollectWarpWindows(MotionSet motionSet, GameObject root)
        {
            var result = new List<WarpWindow>();
            EnemyCombat combat = root.GetComponentInChildren<EnemyCombat>(true);
            if (motionSet.globalEvents != null)
            {
                foreach (MotionEventBase motionEvent in motionSet.globalEvents)
                    if (motionEvent is MotionEvent_MotionWarp warp)
                        result.Add(CreateWarpWindow(warp, 0f, combat));
            }

            float offset = 0f;
            if (motionSet.motions != null)
            {
                foreach (MotionData motion in motionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase motionEvent in motion.events)
                            if (motionEvent is MotionEvent_MotionWarp warp)
                                result.Add(CreateWarpWindow(warp, offset, combat));
                    }
                    offset += motion?.Duration ?? 0f;
                }
            }
            return result;
        }

        private static WarpWindow CreateWarpWindow(
            MotionEvent_MotionWarp warp,
            float offset,
            EnemyCombat combat)
        {
            float minDistance = warp.minDistance;
            float maxDistance = warp.maxDistance;
            float maxSpeed = warp.maxSpeed;
            bool overrideDistance = warp.overrideDistance;
            MotionWarpModifierType modifier = warp.modifierType;
            float translationWeight = warp.translationWeight;

            switch (warp.preset)
            {
                case MotionWarpPreset.LightAttack:
                    modifier = MotionWarpModifierType.DeltaWarp;
                    overrideDistance = true;
                    minDistance = 0.25f;
                    maxDistance = 7f;
                    maxSpeed = 22f;
                    translationWeight = 1f;
                    break;
                case MotionWarpPreset.HeavyAttack:
                    modifier = MotionWarpModifierType.DeltaWarp;
                    overrideDistance = true;
                    minDistance = 0.35f;
                    maxDistance = 8f;
                    maxSpeed = 20f;
                    translationWeight = 1f;
                    break;
                case MotionWarpPreset.FinishAttack:
                    modifier = MotionWarpModifierType.DeltaWarp;
                    overrideDistance = true;
                    minDistance = 0.1f;
                    maxDistance = 5f;
                    maxSpeed = 16f;
                    translationWeight = 1f;
                    break;
                case MotionWarpPreset.Grab:
                    modifier = MotionWarpModifierType.DeltaWarp;
                    overrideDistance = true;
                    minDistance = 0.05f;
                    maxDistance = 3f;
                    maxSpeed = 12f;
                    translationWeight = 1f;
                    break;
            }

            if (!overrideDistance)
            {
                minDistance = combat != null ? combat.WarpMinDistance : 0.3f;
                maxDistance = combat != null ? combat.WarpMaxDistance : 6f;
                maxSpeed = combat != null ? combat.WarpMaxSpeed : 18f;
            }

            bool bakedUsable = warp.bakedValid
                               && Mathf.Approximately(warp.bakedStartTime, warp.startTime)
                               && Mathf.Approximately(warp.bakedEndTime, warp.endTime)
                               && warp.bakedPathLen > 0.0001f;
            Vector3 horizontalOffset = new(warp.targetOffset.x, 0f, warp.targetOffset.z);
            return new WarpWindow(
                offset + warp.startTime,
                offset + Mathf.Max(warp.startTime, warp.endTime),
                Mathf.Max(minDistance, maxDistance),
                Mathf.Max(0f, maxSpeed),
                translationWeight,
                horizontalOffset.magnitude,
                modifier == MotionWarpModifierType.DeltaWarp,
                bakedUsable);
        }

        private static CollisionWindow CreateWindow(
            BeginCollisionEvent collision,
            float offset,
            AbilityAttackInfo attackInfo)
        {
            string primary = collision.hitboxGroupId;
            if (string.IsNullOrWhiteSpace(primary))
                primary = attackInfo.baseInfo.GetHitPhase(collision.hitPhaseIndex).hitboxGroupId;
            if (string.IsNullOrWhiteSpace(primary))
                primary = CombatHitbox.DefaultGroupId;

            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                primary.Trim(),
            };
            if (collision.additionalHitboxGroupIds != null)
            {
                foreach (string group in collision.additionalHitboxGroupIds)
                    if (!string.IsNullOrWhiteSpace(group))
                        groups.Add(group.Trim());
            }
            return new CollisionWindow(
                offset + collision.startTime,
                offset + Mathf.Max(collision.startTime, collision.endTime),
                groups);
        }

        private static bool HasEvent<T>(MotionSet motionSet) where T : MotionEventBase
        {
            if (motionSet?.globalEvents?.Any(motionEvent => motionEvent is T) == true)
                return true;
            return motionSet?.motions?.Any(
                motion => motion?.events?.Any(motionEvent => motionEvent is T) == true) == true;
        }

        private static void FinalizeAbilityAggregates(AnalysisContext context)
        {
            foreach (AbilityAggregate aggregate in context.Abilities.Values)
            {
                if (!aggregate.HasMeasured || aggregate.RecommendedMax == float.MaxValue)
                    continue;
                if (aggregate.RecommendedMin >= aggregate.RecommendedMax)
                {
                    aggregate.HasBlocker = true;
                    foreach (BakeEntry entry in aggregate.Entries.Where(entry => entry.status == "Measured"))
                    {
                        entry.status = "Blocked";
                        entry.message = "공유 소비자/Variant의 안전 거리 교집합이 없습니다.";
                    }
                    context.Report.blockedCount++;
                }
            }
        }

        private static void BuildBehaviorRecommendations(
            AnalysisContext context,
            ActorDefinitionSO[] definitions)
        {
            foreach ((EnemyBehaviorSO behavior, HashSet<GameplayAbilitySO> abilities) in context.BehaviorAbilities)
            {
                var contributors = new List<(float min, float max, float weight)>();
                bool hasBlockedMeleeAbility = false;
                foreach (GameplayAbilitySO ability in abilities)
                {
                    if (!context.Abilities.TryGetValue(ability, out AbilityAggregate aggregate))
                        continue;
                    if (aggregate.HasBlocker
                        && aggregate.Entries.Any(abilityEntry => abilityEntry.status == "Blocked"))
                    {
                        hasBlockedMeleeAbility = true;
                    }
                    if (aggregate.HasMeasured && !aggregate.HasBlocker)
                    {
                        contributors.Add((
                            aggregate.RecommendedMin,
                            aggregate.RecommendedMax,
                            Mathf.Max(0.01f, aggregate.Weight)));
                    }
                    else if (aggregate.Entries.Any(entry => entry.status == "WarpPreserved")
                             && ability.activation != null
                             && ability.activation.maxDistance > 0f)
                    {
                        contributors.Add((
                            Mathf.Max(0f, ability.activation.minDistance),
                            ability.activation.maxDistance,
                            Mathf.Max(0.01f, aggregate.Weight)));
                    }
                }

                var entry = new BehaviorEntry
                {
                    behavior = behavior.name,
                    behaviorPath = AssetDatabase.GetAssetPath(behavior),
                    currentOptimal = behavior.optimalCombatDistance,
                    currentMin = behavior.minCombatDistance,
                    currentChaseStop = behavior.chaseStopDistance,
                    currentPersonalSpace = behavior.personalSpaceDistance,
                    contributingAbilityCount = contributors.Count,
                };
                context.Report.behaviors.Add(entry);
                if (contributors.Count == 0 || hasBlockedMeleeAbility)
                {
                    entry.status = "Blocked";
                    entry.message = contributors.Count == 0
                        ? "검증 가능한 근접 Ability가 없습니다."
                        : "같은 BehaviorSO에 측정 차단된 근접 Ability가 포함되어 자동 전투 거리 변경을 보류합니다.";
                    continue;
                }

                float preferred = ResolvePreferredDistance(contributors);
                float personal = context.BehaviorPersonalSpaces.TryGetValue(behavior, out float measuredPersonal)
                    ? measuredPersonal
                    : Mathf.Max(0.4f, behavior.personalSpaceDistance);
                personal = RoundToStep(Mathf.Clamp(personal, 0.4f, preferred));
                float chase = RoundDownToStep(Mathf.Max(personal, preferred - 0.2f));
                float min = RoundToStep(Mathf.Clamp(personal + 0.2f, personal, chase));

                entry.recommendedOptimal = RoundDownToStep(preferred);
                entry.recommendedPersonalSpace = personal;
                entry.recommendedChaseStop = chase;
                entry.recommendedMin = min;
                entry.status = "Ready";
            }
        }

        private static float ResolvePreferredDistance(
            List<(float min, float max, float weight)> contributors)
        {
            float totalWeight = contributors.Sum(item => item.weight);
            float furthest = contributors.Max(item => item.max);

            // 임계 커버리지를 만족하는 가장 먼 거리를 우선 채택하고,
            // 하나도 없을 때만 "커버리지가 가장 높은(동률이면 더 먼) 거리" 폴백을 쓴다.
            // 두 후보를 한 변수로 겹쳐 쓰면 폴백이 첫 개선 지점에서 잠긴다.
            float thresholdDistance = -1f;
            float fallbackDistance = MinimumTestDistance;
            float fallbackCoverage = -1f;
            for (float distance = MinimumTestDistance; distance <= furthest + 0.001f; distance += DistanceStep)
            {
                float covered = contributors
                    .Where(item => distance >= item.min && distance <= item.max)
                    .Sum(item => item.weight);
                float coverage = totalWeight > 0f ? covered / totalWeight : 0f;
                if (coverage >= BehaviorCoverageThreshold)
                {
                    thresholdDistance = distance;
                    continue;
                }

                if (coverage > fallbackCoverage
                    || (Mathf.Approximately(coverage, fallbackCoverage) && distance > fallbackDistance))
                {
                    fallbackCoverage = coverage;
                    fallbackDistance = distance;
                }
            }

            float bestDistance = thresholdDistance >= 0f ? thresholdDistance : fallbackDistance;
            return Mathf.Max(MinimumUsableRange, bestDistance);
        }

        private static float ResolvePersonalSpace(GameObject root)
        {
            CapsuleCollider capsule = root.GetComponentInChildren<CapsuleCollider>(true);
            float selfRadius = capsule != null ? GetHorizontalRadius(capsule) : 0.35f;
            return selfRadius + TargetCapsuleRadius + 0.05f;
        }

        private static float GetHorizontalRadius(CapsuleCollider capsule)
        {
            Vector3 scale = capsule.transform.lossyScale;
            return capsule.direction switch
            {
                0 => capsule.radius * Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)),
                1 => capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)),
                _ => capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)),
            };
        }

        private static void Apply(AnalysisContext context)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("몬스터 근접 사거리 Bake All");
            try
            {
                foreach (AbilityAggregate aggregate in context.Abilities.Values)
                {
                    if (!aggregate.HasMeasured || aggregate.HasBlocker
                        || aggregate.RecommendedMax == float.MaxValue)
                        continue;
                    GameplayAbilitySO ability = aggregate.Ability;
                    float currentMinDistance = ability.activation?.minDistance ?? 0f;
                    float currentMaxDistance = ability.activation?.maxDistance ?? 0f;
                    if (Mathf.Approximately(currentMinDistance, aggregate.RecommendedMin)
                        && Mathf.Approximately(currentMaxDistance, aggregate.RecommendedMax))
                        continue;
                    Undo.RecordObject(ability, "몬스터 근접 Ability 사거리 베이크");
                    ability.activation ??= new AbilityActivationRules();
                    ability.activation.minDistance = aggregate.RecommendedMin;
                    ability.activation.maxDistance = aggregate.RecommendedMax;
                    EditorUtility.SetDirty(ability);
                    context.Report.changedAbilityCount++;
                }

                foreach (BehaviorEntry entry in context.Report.behaviors.Where(entry => entry.status == "Ready"))
                {
                    EnemyBehaviorSO behavior = AssetDatabase.LoadAssetAtPath<EnemyBehaviorSO>(entry.behaviorPath);
                    if (behavior == null)
                        throw new InvalidOperationException($"BehaviorSO 경로를 다시 불러올 수 없습니다: {entry.behaviorPath}");
                    if (Mathf.Approximately(behavior.optimalCombatDistance, entry.recommendedOptimal)
                        && Mathf.Approximately(behavior.minCombatDistance, entry.recommendedMin)
                        && Mathf.Approximately(behavior.chaseStopDistance, entry.recommendedChaseStop)
                        && Mathf.Approximately(behavior.personalSpaceDistance, entry.recommendedPersonalSpace))
                        continue;
                    Undo.RecordObject(behavior, "몬스터 전투 거리 베이크");
                    behavior.optimalCombatDistance = entry.recommendedOptimal;
                    behavior.minCombatDistance = entry.recommendedMin;
                    behavior.chaseStopDistance = entry.recommendedChaseStop;
                    behavior.personalSpaceDistance = entry.recommendedPersonalSpace;
                    EditorUtility.SetDirty(behavior);
                    context.Report.changedBehaviorCount++;
                }

                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static AbilityAggregate GetAggregate(
            AnalysisContext context,
            GameplayAbilitySO ability)
        {
            if (context.Abilities.TryGetValue(ability, out AbilityAggregate aggregate))
                return aggregate;
            aggregate = new AbilityAggregate { Ability = ability };
            context.Abilities.Add(ability, aggregate);
            return aggregate;
        }

        private static void AddDefinitionBlockers(
            ActorDefinitionSO definition,
            AnalysisContext context,
            string message)
        {
            foreach (GameplayAbilitySO ability in definition.EffectiveAbilitySet
                         .EnumerateAll()
                         .Where(ability => ability != null)
                         .Distinct())
            {
                if (ability.variants == null)
                    continue;
                foreach (AbilityVariantDefinition variant in ability.variants)
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(variant, out AbilityAttackInfo attackInfo)
                        || attackInfo?.aiSelectable != true)
                        continue;
                    AbilityAggregate aggregate = GetAggregate(context, ability);
                    var entry = new BakeEntry
                    {
                        actorDefinition = definition.name,
                        ability = ability.name,
                        abilityId = ability.abilityId,
                        motionKey = attackInfo.motionKey.ToString(),
                        status = "Blocked",
                        message = message,
                        currentMinDistance = ability.activation?.minDistance ?? 0f,
                        currentMaxDistance = ability.activation?.maxDistance ?? 0f,
                    };
                    aggregate.Entries.Add(entry);
                    aggregate.HasBlocker = true;
                    context.Report.entries.Add(entry);
                    context.Report.blockedCount++;
                }
            }
        }

        private static void Block(
            BakeEntry entry,
            AbilityAggregate aggregate,
            AnalysisContext context,
            string message)
        {
            entry.status = "Blocked";
            entry.message = message;
            aggregate.HasBlocker = true;
            context.Report.blockedCount++;
        }

        private static bool TryFindBestContiguousRange(
            bool[] values,
            out int bestStart,
            out int bestEnd)
        {
            bestStart = -1;
            bestEnd = -1;
            int currentStart = -1;
            for (int i = 0; i <= values.Length; i++)
            {
                bool hit = i < values.Length && values[i];
                if (hit && currentStart < 0)
                    currentStart = i;
                if (hit || currentStart < 0)
                    continue;
                int end = i - 1;
                if (bestStart < 0 || end - currentStart > bestEnd - bestStart)
                {
                    bestStart = currentStart;
                    bestEnd = end;
                }
                currentStart = -1;
            }
            return bestStart >= 0;
        }

        private static float RoundToStep(float value) =>
            Mathf.Round(value / DistanceStep) * DistanceStep;

        private static float RoundDownToStep(float value) =>
            Mathf.Floor(value / DistanceStep + 0.0001f) * DistanceStep;

        private static float RoundUpToStep(float value) =>
            Mathf.Ceil(value / DistanceStep - 0.0001f) * DistanceStep;

        private static void WriteReport(BakeReport report)
        {
            string directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }
}
#endif
