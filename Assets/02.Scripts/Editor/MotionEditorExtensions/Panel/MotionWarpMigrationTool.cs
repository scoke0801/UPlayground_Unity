using System;
using System.Collections.Generic;
using System.Text;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public static class MotionWarpMigrationTool
    {
        private const string DataRoot = "Assets/10.Datas";

        [MenuItem("Tools/UPlayGround/Motion Warp/전체 감사 (Dry Run)")]
        public static void AuditAll()
        {
            MigrationReport report = ProcessAssets(false, false);
            Debug.Log(report.Build("MotionWarp 전체 감사"));
        }

        [MenuItem("Tools/UPlayGround/Motion Warp/선택 MotionSet 마이그레이션")]
        public static void MigrateSelected()
        {
            MigrationReport report = ProcessAssets(true, true);
            Debug.Log(report.Build("MotionWarp 선택 마이그레이션"));
        }

        [MenuItem("Tools/UPlayGround/Motion Warp/전체 마이그레이션")]
        public static void MigrateAll()
        {
            MigrationReport report = ProcessAssets(true, false);
            Debug.Log(report.Build("MotionWarp 전체 마이그레이션"));
        }

        private static MigrationReport ProcessAssets(bool apply, bool selectedOnly)
        {
            string[] paths = selectedOnly ? GetSelectedPaths() : GetAllPaths();
            var report = new MigrationReport();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("MotionWarp ContactShell 마이그레이션");

            try
            {
                foreach (string path in paths)
                {
                    MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                    if (asset?.motionSet == null)
                        continue;

                    int assetChanges = 0;
                    foreach (MotionEvent_MotionWarp warp in EnumerateWarpEvents(asset.motionSet))
                    {
                        report.Total++;
                        List<string> warnings = Validate(warp, path);
                        report.Warnings.AddRange(warnings);
                        if (!NeedsMigration(warp, path))
                            continue;

                        report.WouldChange++;
                        if (!apply)
                            continue;

                        if (assetChanges == 0)
                            Undo.RecordObject(asset, "MotionWarp 데이터 마이그레이션");
                        ApplyPolicy(warp, path);
                        assetChanges++;
                    }

                    if (assetChanges <= 0)
                        continue;
                    report.Changed += assetChanges;
                    report.ChangedAssets++;
                    EditorUtility.SetDirty(asset);
                }

                if (apply)
                {
                    AssetDatabase.SaveAssets();
                    Undo.CollapseUndoOperations(undoGroup);
                }
            }
            catch (Exception exception)
            {
                if (apply)
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    AssetDatabase.SaveAssets();
                }
                throw new InvalidOperationException(
                    "MotionWarp 마이그레이션을 롤백했습니다.",
                    exception);
            }

            return report;
        }

        internal static void ApplyPolicy(MotionEvent_MotionWarp warp, string assetPath)
        {
            bool finish = warp.preset == MotionWarpPreset.FinishAttack
                       || ContainsAny(assetPath, "Finish", "Grab");
            bool dash = ContainsAny(assetPath, "Dash", "Lunge", "BreakAttack", "CounterAttack");

            warp.modifierType = MotionWarpModifierType.DeltaWarp;
            warp.targetPolicy = finish && ContainsAny(assetPath, "Grab")
                ? MotionWarpTargetPolicy.Predictive
                : MotionWarpTargetPolicy.Snapshot;
            warp.translationWeight = 1f;
            warp.rotationWeight = 1f;
            warp.ignoreY = true;
            warp.yPolicy = WarpYPolicy.IgnoreY;
            warp.overrideDistance = true;
            warp.minDistance = 0f;
            warp.targetOffset = Vector3.zero;
            warp.localArrivalOffset = Vector3.zero;
            warp.desiredStandOff = 0.1f;
            warp.translationCurve = BuildTranslationCurve();

            if (finish)
            {
                warp.arrivalMode = WarpArrivalMode.AuthoredWarpPoint;
                warp.warpPointProvider = ContainsAny(assetPath, "Grab")
                    ? WarpPointProvider.Bone
                    : WarpPointProvider.StaticTransform;
                warp.authoredWarpPointLocal = new Vector3(0f, 0f, 0.5f);
                warp.warpPointBone = HumanBodyBones.RightHand;
                warp.warpPointBoneOffset = Vector3.zero;
                warp.targetTransformPath = string.Empty;
                warp.targetPointOffset = Vector3.zero;
                warp.noTranslationWithinReach = 0f;
                warp.maxCorrectionDistance = 1f;
                warp.maxCorrectionRatio = 0.5f;
                warp.maxWarpAngle = 45f;
                warp.maxDistance = 3f;
                warp.maxSpeed = 14f;
                warp.translationEndLeadTime = 0.05f;
                warp.usePlaybackRateWarp = false;
                warp.playbackRateRange = new Vector2(0.95f, 1.05f);
                return;
            }

            warp.arrivalMode = WarpArrivalMode.ContactShell;
            warp.warpPointProvider = WarpPointProvider.Root;
            warp.authoredWarpPointLocal = Vector3.zero;
            warp.warpPointBoneOffset = Vector3.zero;
            warp.targetTransformPath = string.Empty;
            warp.targetPointOffset = Vector3.zero;

            if (dash)
            {
                warp.noTranslationWithinReach = 0.15f;
                warp.maxCorrectionDistance = 1.2f;
                warp.maxCorrectionRatio = 0.5f;
                warp.maxWarpAngle = 30f;
                warp.maxDistance = 5f;
                warp.maxSpeed = 16f;
                warp.translationEndLeadTime = 0.05f;
                warp.usePlaybackRateWarp = true;
                warp.playbackRateRange = new Vector2(0.85f, 1.15f);
            }
            else if (warp.preset == MotionWarpPreset.HeavyAttack)
            {
                warp.noTranslationWithinReach = 0.12f;
                warp.maxCorrectionDistance = 0.8f;
                warp.maxCorrectionRatio = 0.4f;
                warp.maxWarpAngle = 35f;
                warp.maxDistance = 3f;
                warp.maxSpeed = 12f;
                warp.translationEndLeadTime = 0.1f;
                warp.usePlaybackRateWarp = false;
                warp.playbackRateRange = new Vector2(0.95f, 1.05f);
            }
            else
            {
                warp.noTranslationWithinReach = 0.08f;
                warp.maxCorrectionDistance = 0.5f;
                warp.maxCorrectionRatio = 0.3f;
                warp.maxWarpAngle = 45f;
                warp.maxDistance = warp.preset == MotionWarpPreset.LightAttack ? 2.5f : 3.5f;
                warp.maxSpeed = 12f;
                warp.translationEndLeadTime = 0.07f;
                warp.usePlaybackRateWarp = false;
                warp.playbackRateRange = new Vector2(0.95f, 1.05f);
            }
        }

        private static bool NeedsMigration(MotionEvent_MotionWarp warp, string path)
        {
            if (warp.arrivalMode == WarpArrivalMode.TargetCenter)
                return true;
            if (warp.maxCorrectionDistance <= 0f || warp.maxCorrectionRatio <= 0f)
                return true;
            if (warp.maxDistance > 5f)
                return true;
            float expectedDeadZone = ContainsAny(path, "Finish", "Grab")
                ? 0f
                : ContainsAny(path, "Dash", "Lunge", "BreakAttack", "CounterAttack")
                    ? 0.15f
                    : warp.preset == MotionWarpPreset.HeavyAttack ? 0.12f : 0.08f;
            if (!Mathf.Approximately(warp.noTranslationWithinReach, expectedDeadZone))
                return true;
            if ((warp.preset is MotionWarpPreset.LightAttack or MotionWarpPreset.HeavyAttack)
                && warp.arrivalMode != WarpArrivalMode.ContactShell)
                return true;
            if (ContainsAny(path, "Finish", "Grab")
                && warp.arrivalMode != WarpArrivalMode.AuthoredWarpPoint)
                return true;
            return false;
        }

        private static List<string> Validate(MotionEvent_MotionWarp warp, string path)
        {
            var warnings = new List<string>();
            if ((warp.preset is MotionWarpPreset.LightAttack or MotionWarpPreset.HeavyAttack)
                && warp.arrivalMode == WarpArrivalMode.TargetCenter)
                warnings.Add($"{path}: 일반 공격이 TargetCenter를 사용합니다.");
            if (warp.maxCorrectionDistance <= 0f)
                warnings.Add($"{path}: maxCorrectionDistance가 0 이하입니다.");
            if (warp.maxCorrectionRatio <= 0f)
                warnings.Add($"{path}: maxCorrectionRatio가 0 이하입니다.");
            if (warp.maxDistance > 5f)
                warnings.Add($"{path}: maxDistance가 5m를 초과합니다.");
            if (!warp.bakedValid)
                warnings.Add($"{path}: 베이크가 없으므로 결정적 예측 폴백을 사용합니다.");
            return warnings;
        }

        private static IEnumerable<MotionEvent_MotionWarp> EnumerateWarpEvents(MotionSet set)
        {
            if (set.globalEvents != null)
                foreach (MotionEventBase motionEvent in set.globalEvents)
                    if (motionEvent is MotionEvent_MotionWarp warp)
                        yield return warp;

            if (set.motions != null)
                foreach (Motion motion in set.motions)
                    if (motion?.events != null)
                        foreach (MotionEventBase motionEvent in motion.events)
                            if (motionEvent is MotionEvent_MotionWarp warp)
                                yield return warp;

            if (set.layers == null)
                yield break;
            foreach (MotionLayer layer in set.layers)
            {
                if (layer?.globalEvents != null)
                    foreach (MotionEventBase motionEvent in layer.globalEvents)
                        if (motionEvent is MotionEvent_MotionWarp warp)
                            yield return warp;
                if (layer?.motions == null)
                    continue;
                foreach (Motion motion in layer.motions)
                    if (motion?.events != null)
                        foreach (MotionEventBase motionEvent in motion.events)
                            if (motionEvent is MotionEvent_MotionWarp warp)
                                yield return warp;
            }
        }

        private static string[] GetAllPaths()
        {
            string[] guids = AssetDatabase.FindAssets("t:MotionSetAsset", new[] { DataRoot });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            return paths;
        }

        private static string[] GetSelectedPaths()
        {
            var paths = new List<string>();
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                if (selected is not MotionSetAsset)
                    continue;
                paths.Add(AssetDatabase.GetAssetPath(selected));
            }
            return paths.ToArray();
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            foreach (string candidate in candidates)
                if (value.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static AnimationCurve BuildTranslationCurve() => new(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(0.85f, 0.7f, -2f, -2f),
            new Keyframe(1f, 0f, 0f, 0f));

        private sealed class MigrationReport
        {
            public int Total;
            public int WouldChange;
            public int Changed;
            public int ChangedAssets;
            public readonly List<string> Warnings = new();

            public string Build(string title)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"[{title}]");
                builder.AppendLine(
                    $"전체={Total}, 변경대상={WouldChange}, 적용={Changed}, 에셋={ChangedAssets}, 경고={Warnings.Count}");
                foreach (string warning in Warnings)
                    builder.AppendLine(warning);
                return builder.ToString();
            }
        }
    }
}
