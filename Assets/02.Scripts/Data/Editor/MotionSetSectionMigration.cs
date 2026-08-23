using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using IOPath = System.IO.Path;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Data.Editor
{
    public enum MotionSetSectionMigrationDecision
    {
        Existing,
        LoopSelf,
        Stop,
        Review,
        Invalid,
    }

    public sealed class MotionSetSectionMigrationEntry
    {
        public MotionSetAsset asset;
        public string path;
        public string guid;
        public MotionSetSectionMigrationDecision decision;
        public string reason;
        public readonly List<string> referencedTags = new();
        public readonly List<string> abilityMotionPaths = new();
    }

    /// <summary>
    /// Section이 없는 MotionSet을 명시적인 단일 Section 구조로 변환한다.
    /// 이름 추측은 사용하지 않고 슬롯 의미, 클립 Loop 설정, 기존 LoopEvent만 근거로 사용한다.
    /// </summary>
    [InitializeOnLoad]
    public static class MotionSetSectionMigration
    {
        const string ReportPath = "Temp/MotionSetSectionMigrationReport.txt";
        const string RequestPath = "Temp/MotionSetSectionMigrationRequest.txt";
        const string ReviewedSnapshotHash =
            "3EAEE71B86E8A06AFF013C57801D677801A4579633A947571BB97E312A8EA655";

        static readonly HashSet<string> ReviewedContinuousPaths =
            new(StringComparer.Ordinal)
            {
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Hwarin/DoubleAxe/DoubleAxe_Run.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Chair_Sit_Idle.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Walk.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/Raon_Common_Crouch_Idle.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/Raon_Common_Crouch_Walk.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/Npc_Talk_2.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Fall.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Guard.asset",
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Skeleton/Skeleton_Common_Idle_Bow.asset",
            };

        static MotionSetSectionMigration()
        {
            EditorApplication.update += PollRequest;
        }

        [MenuItem("Tools/UPlayGround/Animation/MotionSet Section Migration/Dry Run")]
        public static void DryRunMenu()
        {
            List<MotionSetSectionMigrationEntry> entries = AnalyzeAll();
            WriteReport(entries, false);
            ShowSummary(entries, "Dry Run");
        }

        [MenuItem("Tools/UPlayGround/Animation/MotionSet Section Migration/Apply")]
        public static void ApplyMenu()
        {
            List<MotionSetSectionMigrationEntry> entries = AnalyzeAll();
            int reviewCount = entries.Count(entry =>
                entry.decision == MotionSetSectionMigrationDecision.Review);
            if (reviewCount > 0)
            {
                WriteReport(entries, false);
                EditorUtility.DisplayDialog(
                    "MotionSet Section Migration",
                    $"판단이 필요한 에셋 {reviewCount}개가 있어 적용을 중단했습니다.\n{ReportPath}",
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "MotionSet Section Migration",
                    "Section이 없는 유효한 MotionSet을 일괄 변환합니다.\n" +
                    "기존 에셋은 완료 시 Base Layer를 유지하며, 지속 모션만 LoopSelf로 설정합니다.",
                    "적용",
                    "취소"))
                return;

            Apply(entries);
        }

        public static List<MotionSetSectionMigrationEntry> AnalyzeAll()
        {
            Dictionary<MotionSetAsset, HashSet<string>> references = BuildReferenceMap();
            Dictionary<MotionSetAsset, HashSet<string>> abilityMotions =
                BuildAbilityMotionMap();
            string[] guids = AssetDatabase.FindAssets("t:MotionSetAsset");
            var entries = new List<MotionSetSectionMigrationEntry>(guids.Length);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                entries.Add(Analyze(asset, path, guid, references, abilityMotions));
            }

            entries.Sort((left, right) =>
                string.Compare(left.path, right.path, StringComparison.Ordinal));
            ResolveReviewedSnapshot(entries);
            return entries;
        }

        public static MotionSetSectionMigrationEntry Analyze(
            MotionSetAsset asset,
            string path,
            string guid,
            IReadOnlyDictionary<MotionSetAsset, HashSet<string>> references,
            IReadOnlyDictionary<MotionSetAsset, HashSet<string>> abilityMotions = null)
        {
            var entry = new MotionSetSectionMigrationEntry
            {
                asset = asset,
                path = path,
                guid = guid,
            };

            if (asset == null || asset.motionSet == null || !asset.motionSet.IsValid() ||
                asset.motionSet.TotalDuration <= 0f)
            {
                entry.decision = MotionSetSectionMigrationDecision.Invalid;
                entry.reason = "유효한 재생 구간이 없음";
                return entry;
            }

            if (asset.motionSet.sections is { Count: > 0 })
            {
                entry.decision = MotionSetSectionMigrationDecision.Existing;
                entry.reason = "이미 Section 저작됨";
                return entry;
            }

            if (references != null && references.TryGetValue(asset, out HashSet<string> tags))
                entry.referencedTags.AddRange(tags.OrderBy(tag => tag, StringComparer.Ordinal));
            if (abilityMotions != null &&
                abilityMotions.TryGetValue(asset, out HashSet<string> referencePaths))
            {
                entry.abilityMotionPaths.AddRange(
                    referencePaths.OrderBy(value => value, StringComparer.Ordinal));
            }

            bool hasContinuousReference = entry.referencedTags.Any(IsContinuousTag);
            bool hasFiniteReference = entry.referencedTags.Any(tag => !IsContinuousTag(tag));
            if (hasContinuousReference && hasFiniteReference)
            {
                entry.decision = MotionSetSectionMigrationDecision.Review;
                entry.reason = "지속 슬롯과 단발 슬롯이 같은 MotionSet을 공유함";
                return entry;
            }

            if (hasContinuousReference)
            {
                entry.decision = MotionSetSectionMigrationDecision.LoopSelf;
                entry.reason = "지속 GameplayTag 슬롯 참조";
                return entry;
            }

            if (hasFiniteReference)
            {
                entry.decision = MotionSetSectionMigrationDecision.Stop;
                entry.reason = "단발 GameplayTag 슬롯 참조";
                return entry;
            }

            if (entry.abilityMotionPaths.Count > 0)
            {
                entry.decision = MotionSetSectionMigrationDecision.Stop;
                entry.reason = HasDirectorLoopEvent(asset.motionSet)
                    ? "Ability Motion Key 매핑, 기존 LoopEvent가 내부 반복 구간 제어"
                    : "Ability Motion Key 매핑";
                return entry;
            }

            if (HasDirectorLoopEvent(asset.motionSet))
            {
                entry.decision = MotionSetSectionMigrationDecision.Review;
                entry.reason = "참조 근거 없이 기존 LoopEvent가 내부 반복 구간을 제어함";
                return entry;
            }

            List<AnimationClip> clips = CollectBaseClips(asset.motionSet);
            entry.decision = MotionSetSectionMigrationDecision.Review;
            entry.reason = clips.Any(clip => clip.isLooping)
                ? "미참조 MotionSet, AnimationClip Loop Time은 보조 근거로만 사용"
                : "ActorAnimationMotionSet 참조를 찾지 못함";
            return entry;
        }

        public static void Apply(List<MotionSetSectionMigrationEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (entries.Any(entry => entry.decision == MotionSetSectionMigrationDecision.Review))
                throw new InvalidOperationException("Review 항목이 남아 있어 마이그레이션할 수 없습니다.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate MotionSet Sections");
            int changed = 0;
            List<MotionSetSectionMigrationEntry> targets = entries
                .Where(entry =>
                    entry.asset != null &&
                    entry.decision is not MotionSetSectionMigrationDecision.Existing and
                        not MotionSetSectionMigrationDecision.Invalid)
                .ToList();
            MotionSetSectionMigrationEntry dirty = targets.FirstOrDefault(entry =>
                EditorUtility.IsDirty(entry.asset));
            if (dirty != null)
            {
                throw new InvalidOperationException(
                    $"저장되지 않은 대상 에셋이 있어 중단합니다: {dirty.path}");
            }

            string backupRoot = BackupTargets(targets);
            try
            {
                foreach (MotionSetSectionMigrationEntry entry in targets)
                {
                    Undo.RegisterCompleteObjectUndo(entry.asset, "Migrate MotionSet Section");
                    MotionSet set = entry.asset.motionSet;
                    set.sections ??= new List<MotionSection>();
                    set.sections.Add(new MotionSection
                    {
                        id = $"section_{entry.guid}",
                        displayName = entry.decision == MotionSetSectionMigrationDecision.LoopSelf
                            ? "Loop"
                            : "Main",
                        startTime = 0f,
                        endPolicy = entry.decision == MotionSetSectionMigrationDecision.LoopSelf
                            ? MotionSectionEndPolicy.LoopSelf
                            : MotionSectionEndPolicy.Stop,
                    });
                    set.schemaVersion = MotionSet.CurrentSchemaVersion;
                    EditorUtility.SetDirty(entry.asset);
                    AssetDatabase.SaveAssetIfDirty(entry.asset);
                    changed++;
                }

                Undo.CollapseUndoOperations(undoGroup);
                WriteReport(entries, true, backupRoot);
                Debug.Log(
                    $"[MotionSetSectionMigration] {changed}개 MotionSet 변환 완료. " +
                    $"보고서: {ReportPath}, 백업: {backupRoot}");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                RestoreTargets(targets, backupRoot);
                throw;
            }
        }

        public static bool IsContinuousTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;
            if (tag.Equals("Motion.Locomotion", StringComparison.Ordinal) ||
                tag.StartsWith("Motion.Locomotion.", StringComparison.Ordinal))
                return true;
            if (tag.EndsWith(".Loop", StringComparison.Ordinal))
                return true;

            return tag is
                "Motion.Air.Fall" or
                "Motion.Crouch.Idle" or
                "Motion.Crouch.Walk" or
                "Motion.Fly.Idle" or
                "Motion.Fly.Move" or
                "Motion.Interaction.Fishing.Idle" or
                "Motion.Interaction.Fishing.Catch.Loop" or
                "Motion.Interaction.GroundWork.Loop" or
                "Motion.Npc.Talk" or
                "Motion.Reaction.Guard" or
                "Motion.Reaction.Grabbed" or
                "Motion.Reaction.Stun";
        }

        static Dictionary<MotionSetAsset, HashSet<string>> BuildReferenceMap()
        {
            var result = new Dictionary<MotionSetAsset, HashSet<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:ActorAnimationMotionSet"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActorAnimationMotionSet owner =
                    AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(path);
                if (owner?.motionSlots == null)
                    continue;

                foreach (var pair in owner.motionSlots)
                {
                    if (pair.Value == null)
                        continue;
                    if (!result.TryGetValue(pair.Value, out HashSet<string> tags))
                    {
                        tags = new HashSet<string>(StringComparer.Ordinal);
                        result.Add(pair.Value, tags);
                    }
                    if (pair.Key.IsValid())
                        tags.Add(pair.Key.TagName);
                }
            }
            return result;
        }

        static Dictionary<MotionSetAsset, HashSet<string>> BuildAbilityMotionMap()
        {
            var result = new Dictionary<MotionSetAsset, HashSet<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:ActorAnimationMotionSet"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActorAnimationMotionSet motionSet =
                    AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(path);
                if (motionSet?.abilityMotions == null)
                    continue;

                foreach (KeyValuePair<MotionKey, MotionSetAsset> pair
                         in motionSet.abilityMotions)
                {
                    AddAbilityMotion(
                        result,
                        pair.Value,
                        $"{path} [{pair.Key}]");
                }
            }
            return result;
        }

        static void AddAbilityMotion(
            Dictionary<MotionSetAsset, HashSet<string>> result,
            MotionSetAsset asset,
            string path)
        {
            if (asset == null)
                return;
            if (!result.TryGetValue(asset, out HashSet<string> paths))
            {
                paths = new HashSet<string>(StringComparer.Ordinal);
                result.Add(asset, paths);
            }
            paths.Add(path);
        }

        static bool HasDirectorLoopEvent(MotionSet set)
        {
            if (ContainsLoopEvent(set.globalEvents))
                return true;
            if (set.motions != null)
                foreach (Motion motion in set.motions)
                    if (ContainsLoopEvent(motion?.events))
                        return true;
            return false;
        }

        static bool ContainsLoopEvent(List<MotionEventBase> events)
        {
            if (events == null)
                return false;
            return events.Any(motionEvent =>
                motionEvent != null &&
                string.Equals(
                    motionEvent.GetType().Name,
                    "LoopEvent",
                    StringComparison.Ordinal));
        }

        static List<AnimationClip> CollectBaseClips(MotionSet set)
        {
            var result = new List<AnimationClip>();
            if (set?.motions == null)
                return result;
            foreach (Motion motion in set.motions)
                if (motion?.motionClip != null && !result.Contains(motion.motionClip))
                    result.Add(motion.motionClip);
            return result;
        }

        static void PollRequest()
        {
            if (EditorApplication.isCompiling || !File.Exists(RequestPath))
                return;

            string request = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            List<MotionSetSectionMigrationEntry> entries = AnalyzeAll();
            if (string.Equals(request, "Apply", StringComparison.OrdinalIgnoreCase))
            {
                if (entries.Any(entry =>
                        entry.decision == MotionSetSectionMigrationDecision.Review))
                {
                    WriteReport(entries, false);
                    Debug.LogError(
                        $"[MotionSetSectionMigration] Review 항목이 남아 적용을 중단했습니다. {ReportPath}");
                    return;
                }
                Apply(entries);
            }
            else
            {
                WriteReport(entries, false);
                ShowSummary(entries, "자동 Dry Run");
            }
        }

        static void WriteReport(
            IReadOnlyList<MotionSetSectionMigrationEntry> entries,
            bool applied,
            string backupRoot = null)
        {
            Directory.CreateDirectory(IOPath.GetDirectoryName(ReportPath) ?? "Temp");
            using var writer = new StreamWriter(ReportPath, false);
            writer.WriteLine($"MotionSet Section Migration ({(applied ? "Applied" : "Dry Run")})");
            writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(backupRoot))
                writer.WriteLine($"Backup: {backupRoot}");
            foreach (MotionSetSectionMigrationDecision decision in
                     Enum.GetValues(typeof(MotionSetSectionMigrationDecision)))
            {
                writer.WriteLine(
                    $"{decision}: {entries.Count(entry => entry.decision == decision)}");
            }
            writer.WriteLine();
            foreach (MotionSetSectionMigrationEntry entry in entries)
            {
                string tags = entry.referencedTags.Count > 0
                    ? string.Join(", ", entry.referencedTags)
                    : "-";
                string abilityMotions = entry.abilityMotionPaths.Count > 0
                    ? string.Join(", ", entry.abilityMotionPaths)
                    : "-";
                writer.WriteLine(
                    $"[{entry.decision}] {entry.path}\n" +
                    $"  Reason: {entry.reason}\n" +
                    $"  Tags: {tags}\n" +
                    $"  AbilityMotions: {abilityMotions}");
            }
        }

        static string BackupTargets(IReadOnlyList<MotionSetSectionMigrationEntry> targets)
        {
            string root = IOPath.Combine(
                "Temp",
                "MotionSetMigration",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(root);
            using var manifest = new StreamWriter(
                IOPath.Combine(root, "manifest.tsv"),
                false,
                Encoding.UTF8);
            manifest.WriteLine("guid\tsha256\tassetPath\tbackupPath\tdecision");

            foreach (MotionSetSectionMigrationEntry entry in targets)
            {
                string backupPath = IOPath.Combine(root, $"{entry.guid}.asset");
                File.Copy(entry.path, backupPath, true);
                manifest.WriteLine(
                    $"{entry.guid}\t{ComputeSha256(entry.path)}\t{entry.path}\t{backupPath}\t{entry.decision}");
            }
            return root;
        }

        static void RestoreTargets(
            IReadOnlyList<MotionSetSectionMigrationEntry> targets,
            string backupRoot)
        {
            foreach (MotionSetSectionMigrationEntry entry in targets)
            {
                string backupPath = IOPath.Combine(backupRoot, $"{entry.guid}.asset");
                if (!File.Exists(backupPath))
                    continue;
                File.Copy(backupPath, entry.path, true);
                AssetDatabase.ImportAsset(entry.path, ImportAssetOptions.ForceUpdate);
            }
        }

        static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        static void ResolveReviewedSnapshot(List<MotionSetSectionMigrationEntry> entries)
        {
            List<MotionSetSectionMigrationEntry> reviewEntries = entries
                .Where(entry => entry.decision == MotionSetSectionMigrationDecision.Review)
                .OrderBy(entry => entry.guid, StringComparer.Ordinal)
                .ToList();
            string snapshot = string.Join("\n", reviewEntries.Select(entry => entry.guid));
            using SHA256 sha = SHA256.Create();
            string hash = BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot)))
                .Replace("-", string.Empty);
            if (!string.Equals(hash, ReviewedSnapshotHash, StringComparison.Ordinal))
                return;

            foreach (MotionSetSectionMigrationEntry entry in reviewEntries)
            {
                if (ReviewedContinuousPaths.Contains(entry.path))
                {
                    entry.decision = MotionSetSectionMigrationDecision.LoopSelf;
                    entry.reason = "Advisor 수동 검토 스냅샷: 지속 재생";
                }
                else
                {
                    entry.decision = MotionSetSectionMigrationDecision.Stop;
                    entry.reason = "Advisor 수동 검토 스냅샷: 단발/내부 LoopEvent 제어";
                }
            }
        }

        static void ShowSummary(
            IReadOnlyList<MotionSetSectionMigrationEntry> entries,
            string title)
        {
            string summary = string.Join(
                "\n",
                Enum.GetValues(typeof(MotionSetSectionMigrationDecision))
                    .Cast<MotionSetSectionMigrationDecision>()
                    .Select(decision =>
                        $"{decision}: {entries.Count(entry => entry.decision == decision)}"));
            Debug.Log($"[MotionSetSectionMigration] {title}\n{summary}\n{ReportPath}");
        }
    }
}
