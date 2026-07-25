using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public static class AbilityTaskGraphValidator
    {
        public static List<AbilityProductionIssue> Validate(
            AbilityTaskGraphSO graph)
        {
            var issues = new List<AbilityProductionIssue>();
            if (graph?.Root == null)
            {
                issues.Add(new AbilityProductionIssue(
                    "TASK_GRAPH.ROOT",
                    AbilityProductionSeverity.Error,
                    "Task Graph Root가 없습니다.",
                    graph));
                return issues;
            }

            var visiting = new HashSet<AbilityTaskDefinitionSO>();
            var visited = new HashSet<AbilityTaskDefinitionSO>();
            Visit(graph.Root, graph, visiting, visited, issues);
            return issues;
        }

        private static void Visit(
            AbilityTaskDefinitionSO node,
            AbilityTaskGraphSO graph,
            HashSet<AbilityTaskDefinitionSO> visiting,
            HashSet<AbilityTaskDefinitionSO> visited,
            List<AbilityProductionIssue> issues)
        {
            if (node == null)
            {
                issues.Add(new AbilityProductionIssue(
                    "TASK_GRAPH.NULL_CHILD",
                    AbilityProductionSeverity.Error,
                    "Task Graph에 null 자식 노드가 있습니다.",
                    graph));
                return;
            }
            if (visited.Contains(node))
                return;
            if (!visiting.Add(node))
            {
                issues.Add(new AbilityProductionIssue(
                    "TASK_GRAPH.CYCLE",
                    AbilityProductionSeverity.Error,
                    $"Task 참조 순환을 발견했습니다: {node.name}",
                    node));
                return;
            }

            IReadOnlyList<AbilityTaskDefinitionSO> children =
                node switch
                {
                    SequenceAbilityTaskDefinitionSO sequence =>
                        sequence.Children,
                    ParallelAbilityTaskDefinitionSO parallel =>
                        parallel.Children,
                    _ => null,
                };
            if (children != null)
            {
                if (children.Count == 0)
                {
                    issues.Add(new AbilityProductionIssue(
                        "TASK_GRAPH.EMPTY_COMPOSITE",
                        AbilityProductionSeverity.Warning,
                        $"복합 Task에 자식이 없습니다: {node.name}",
                        node));
                }
                for (int i = 0; i < children.Count; i++)
                    Visit(children[i], graph, visiting, visited, issues);
            }
            if (node is WaitGameplayEventTaskDefinitionSO wait
                && string.IsNullOrWhiteSpace(wait.eventTag))
            {
                issues.Add(new AbilityProductionIssue(
                    "TASK_GRAPH.EMPTY_EVENT_TAG",
                    AbilityProductionSeverity.Error,
                    "GameplayEvent 대기 Task의 eventTag가 비어 있습니다.",
                    node));
            }

            visiting.Remove(node);
            visited.Add(node);
        }
    }

    public sealed class AbilityStaticBalanceSummary
    {
        public string AbilityId;
        public int HitPhaseCount;
        public float TotalDamage;
        public float TotalPoiseDamage;
        public float TotalBreakDamage;
        public float MotionDuration;
        public float ExpectedDamage;
        public float ExpectedDuration;
        public float Cooldown;
        public float CycleDuration;
        public float DamagePerSecond;
    }

    public sealed class AbilityMeasuredResult
    {
        public string AbilityId;
        public float AverageDamage;
        public float AverageDuration;
        public float AverageHitCount;
        public float CooldownReadySeconds;
        public int RemainingTaskCount;
        public int RemainingEffectCount;
        public int RemainingTagCount;
    }

    public sealed class AbilityBalanceComparison
    {
        public AbilityStaticBalanceSummary Static;
        public AbilityMeasuredResult Measured;
        public readonly List<string> Findings = new();
    }

    public static class AbilityBalanceAnalyzer
    {
        public static AbilityStaticBalanceSummary Summarize(
            GameplayAbilitySO ability)
        {
            UPlayGroundMotionAbilityPayloadSO payload = FindPayload(ability);
            var summary = new AbilityStaticBalanceSummary
            {
                AbilityId = ability?.abilityId ?? string.Empty,
                ExpectedDamage = ability?.balance?.expectedDamage ?? 0f,
                ExpectedDuration = ability?.balance?.expectedDuration ?? 0f,
                Cooldown = ability?.cooldown?.durationSeconds ?? 0f,
            };
            List<HitPhaseData> phases =
                payload?.attackInfo?.baseInfo?.hitPhases;
            if (phases != null)
            {
                summary.HitPhaseCount = phases.Count;
                for (int i = 0; i < phases.Count; i++)
                {
                    HitPhaseData phase = phases[i];
                    if (phase == null)
                        continue;
                    summary.TotalDamage += phase.damage;
                    summary.TotalPoiseDamage += phase.poiseDamage;
                    summary.TotalBreakDamage += phase.breakDamage;
                }
            }
            AbilityMotionReport motion = AbilityMotionAnalyzer.Analyze(payload);
            summary.MotionDuration = motion.Duration;
            float executionDuration = summary.ExpectedDuration > 0f
                ? summary.ExpectedDuration
                : summary.MotionDuration;
            summary.CycleDuration = Mathf.Max(
                executionDuration,
                summary.Cooldown);
            if (summary.CycleDuration > 0f)
                summary.DamagePerSecond =
                    summary.TotalDamage / summary.CycleDuration;
            return summary;
        }

        public static AbilityBalanceComparison Compare(
            GameplayAbilitySO ability,
            AbilityMeasuredResult measured)
        {
            var result = new AbilityBalanceComparison
            {
                Static = Summarize(ability),
                Measured = measured,
            };
            if (measured == null)
            {
                result.Findings.Add("측정 결과가 없습니다.");
                return result;
            }
            if (!string.Equals(
                    result.Static.AbilityId,
                    measured.AbilityId,
                    StringComparison.Ordinal))
                result.Findings.Add("Ability ID가 일치하지 않습니다.");
            AddDelta(
                result.Findings,
                "피해",
                result.Static.ExpectedDamage > 0f
                    ? result.Static.ExpectedDamage
                    : result.Static.TotalDamage,
                measured.AverageDamage);
            AddDelta(
                result.Findings,
                "지속시간",
                result.Static.ExpectedDuration > 0f
                    ? result.Static.ExpectedDuration
                    : result.Static.MotionDuration,
                measured.AverageDuration);
            AddDelta(
                result.Findings,
                "적중 수",
                result.Static.HitPhaseCount,
                measured.AverageHitCount);
            if (measured.RemainingTaskCount > 0
                || measured.RemainingEffectCount > 0
                || measured.RemainingTagCount > 0)
            {
                result.Findings.Add(
                    "종료 후 런타임 잔류 상태가 있습니다. Task/Effect/Tag를 확인하세요.");
            }
            return result;
        }

        private static void AddDelta(
            List<string> findings,
            string label,
            float expected,
            float actual)
        {
            float delta = actual - expected;
            float ratio = Mathf.Abs(expected) > 0.0001f
                ? delta / Mathf.Abs(expected) * 100f
                : 0f;
            findings.Add(
                $"{label}: 예상 {expected:0.###}, 실측 {actual:0.###}, "
                + $"차이 {delta:+0.###;-0.###;0} ({ratio:+0.#;-0.#;0}%)");
        }

        private static UPlayGroundMotionAbilityPayloadSO FindPayload(
            GameplayAbilitySO ability)
        {
            if (ability?.variants == null)
                return null;
            for (int i = 0; i < ability.variants.Count; i++)
                if (ability.variants[i]?.executionPayload
                    is UPlayGroundMotionAbilityPayloadSO payload)
                    return payload;
            return null;
        }
    }

    [Serializable]
    public sealed class AbilityReplayData
    {
        public string actorId;
        public float startTime;
        public float endTime;
        public List<AbilityReplayFrame> frames = new();
        public List<AbilityReplayEvent> events = new();
    }

    [Serializable]
    public sealed class AbilityReplayFrame
    {
        public float t;
        public int selectedIntent;
        public float distance;
        public bool hasAttackSlot;
        public string resolverFailureReason;
    }

    [Serializable]
    public sealed class AbilityReplayEvent
    {
        public float t;
        public string eventType;
        public string detail;
    }

    public sealed class AbilityReplayComparison
    {
        public int FrameCount;
        public int AttackCandidateFrames;
        public float AttackCandidateRatio;
        public float AverageDistance;
        public int ActivationFailureCount;
        public float StaticSelectionWeight;
        public float MinDistance;
        public float MaxDistance;
        public readonly List<string> Findings = new();
    }

    public static class BalanceReplayComparator
    {
        public static AbilityReplayComparison Compare(
            GameplayAbilitySO ability,
            UPlayGroundMotionAbilityPayloadSO payload,
            AbilityReplayData replay)
        {
            var result = new AbilityReplayComparison
            {
                StaticSelectionWeight =
                    payload?.attackInfo?.selectionWeight ?? 0f,
                MinDistance = ability?.activation?.minDistance ?? 0f,
                MaxDistance = ability?.activation?.maxDistance ?? 0f,
            };
            if (replay?.frames == null || replay.frames.Count == 0)
            {
                result.Findings.Add("Replay 프레임이 없습니다.");
                return result;
            }
            float distanceTotal = 0f;
            for (int i = 0; i < replay.frames.Count; i++)
            {
                AbilityReplayFrame frame = replay.frames[i];
                result.FrameCount++;
                distanceTotal += frame.distance;
                if (frame.hasAttackSlot)
                    result.AttackCandidateFrames++;
                if (!string.IsNullOrWhiteSpace(frame.resolverFailureReason))
                    result.ActivationFailureCount++;
            }
            result.AverageDistance = distanceTotal / result.FrameCount;
            result.AttackCandidateRatio =
                (float)result.AttackCandidateFrames / result.FrameCount;
            if (result.AverageDistance < result.MinDistance
                || (result.MaxDistance > 0f
                    && result.AverageDistance > result.MaxDistance))
            {
                result.Findings.Add(
                    "평균 교전 거리가 Ability 활성화 거리 밖입니다. activation의 "
                    + "minDistance/maxDistance와 AI 선호 거리를 함께 검토하세요.");
            }
            if (result.StaticSelectionWeight > 0f
                && result.AttackCandidateRatio < 0.05f)
            {
                result.Findings.Add(
                    "정적 선택 가중치가 있지만 실제 공격 후보 프레임 비율이 낮습니다.");
            }
            if (result.ActivationFailureCount > 0)
            {
                result.Findings.Add(
                    $"Resolver/활성화 실패 프레임 {result.ActivationFailureCount}개를 "
                    + "원인별로 확인하세요.");
            }
            return result;
        }

        public static AbilityReplayData LoadJson(string path) =>
            JsonUtility.FromJson<AbilityReplayData>(File.ReadAllText(path));

        public static string ToCsv(AbilityReplayComparison result)
        {
            if (result == null)
                return string.Empty;
            return string.Join(
                Environment.NewLine,
                "frameCount,attackCandidateFrames,attackCandidateRatio,"
                + "averageDistance,activationFailureCount,selectionWeight,"
                + "minDistance,maxDistance",
                string.Join(
                    ",",
                    result.FrameCount,
                    result.AttackCandidateFrames,
                    result.AttackCandidateRatio.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture),
                    result.AverageDistance.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture),
                    result.ActivationFailureCount,
                    result.StaticSelectionWeight.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture),
                    result.MinDistance.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture),
                    result.MaxDistance.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture)));
        }
    }

    [Serializable]
    public sealed class AbilityProductionBalanceSnapshot
    {
        public string createdAt;
        public List<AbilityProductionBalanceEntry> entries = new();
    }

    [Serializable]
    public sealed class AbilityProductionBalanceEntry
    {
        public string abilityId;
        public string assetPath;
        public float totalDamage;
        public float motionDuration;
        public float cooldown;
    }

    public static class AbilityProductionSnapshotService
    {
        public static AbilityProductionBalanceSnapshot Capture()
        {
            var snapshot = new AbilityProductionBalanceSnapshot
            {
                createdAt = DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),
            };
            string[] guids = AssetDatabase.FindAssets("t:GameplayAbilitySO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets =
                    AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is not GameplayAbilitySO ability)
                        continue;
                    AbilityStaticBalanceSummary summary =
                        AbilityBalanceAnalyzer.Summarize(ability);
                    snapshot.entries.Add(
                        new AbilityProductionBalanceEntry
                        {
                            abilityId = ability.abilityId,
                            assetPath = path,
                            totalDamage = summary.TotalDamage,
                            motionDuration = summary.MotionDuration,
                            cooldown = summary.Cooldown,
                        });
                }
            }
            snapshot.entries.Sort((left, right) =>
                string.CompareOrdinal(left.abilityId, right.abilityId));
            return snapshot;
        }

        public static List<string> Compare(
            AbilityProductionBalanceSnapshot baseline,
            AbilityProductionBalanceSnapshot current,
            float epsilon = 0.0001f)
        {
            var findings = new List<string>();
            var before = new Dictionary<string, AbilityProductionBalanceEntry>(
                StringComparer.Ordinal);
            if (baseline?.entries != null)
                for (int i = 0; i < baseline.entries.Count; i++)
                    if (!string.IsNullOrWhiteSpace(
                            baseline.entries[i]?.abilityId))
                        before[baseline.entries[i].abilityId] =
                            baseline.entries[i];
            if (current?.entries != null)
            {
                for (int i = 0; i < current.entries.Count; i++)
                {
                    AbilityProductionBalanceEntry after =
                        current.entries[i];
                    if (after == null
                        || string.IsNullOrWhiteSpace(after.abilityId))
                        continue;
                    if (!before.Remove(
                            after.abilityId,
                            out AbilityProductionBalanceEntry old))
                    {
                        findings.Add($"추가: {after.abilityId}");
                        continue;
                    }
                    AddChanged(
                        findings,
                        after.abilityId,
                        "totalDamage",
                        old.totalDamage,
                        after.totalDamage,
                        epsilon);
                    AddChanged(
                        findings,
                        after.abilityId,
                        "motionDuration",
                        old.motionDuration,
                        after.motionDuration,
                        epsilon);
                    AddChanged(
                        findings,
                        after.abilityId,
                        "cooldown",
                        old.cooldown,
                        after.cooldown,
                        epsilon);
                }
            }
            foreach (string removed in before.Keys)
                findings.Add($"제거: {removed}");
            return findings;
        }

        public static void Save(
            string path,
            AbilityProductionBalanceSnapshot snapshot) =>
            File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));

        public static AbilityProductionBalanceSnapshot Load(string path) =>
            JsonUtility.FromJson<AbilityProductionBalanceSnapshot>(
                File.ReadAllText(path));

        private static void AddChanged(
            List<string> findings,
            string abilityId,
            string field,
            float before,
            float after,
            float epsilon)
        {
            if (Mathf.Abs(before - after) <= epsilon)
                return;
            findings.Add(
                $"{abilityId} · {field}: {before:0.###} → {after:0.###}");
        }
    }
}
