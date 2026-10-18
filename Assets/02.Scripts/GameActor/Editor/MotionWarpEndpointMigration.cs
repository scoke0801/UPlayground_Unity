#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.EditorTools;
using UPlayGround.MovementController;

namespace UPlayGround.Editor
{
    /// <summary>기존 실행 설정을 먼저 실체화하고 끝점 보정 계약으로 명시적으로 이관한다.</summary>
    public static class MotionWarpEndpointMigration
    {
        private const string ReportRoot = "Logs/MotionWarpReplacement";

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/끝점 보정 이관 분석")]
        public static void Analyze() => Run(false);

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/끝점 보정 이관 적용")]
        public static void Apply() => Run(true);

        /// <summary>현재 모델별 프로필을 보충하고 측정된 제자리 구간을 회전 전용으로 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/끝점 보정 프로필 준비")]
        public static void PrepareProfiles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 정상인 Edit Mode에서 실행해야 합니다.");
            var assets = AssetDatabase.FindAssets("t:MotionSetAsset", new[] { "Assets/10.Datas" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MotionSetAsset>)
                .Where(asset => asset != null).ToArray();
            WarpRootMotionBatchBakeWindow.BakeMissingProfiles(assets);
            bool valid = WarpRootMotionBatchBakeWindow.TryValidateMappedProfiles(out string report);
            Directory.CreateDirectory(ReportRoot);
            File.WriteAllText(Path.Combine(ReportRoot, "profiles.txt"), report);
            if (!valid) throw new InvalidOperationException(report);
            Debug.Log(report);
        }

        /// <summary>콜리전·VFX·캔슬 이벤트를 보존한 채 구버전 워프만 단일 Undo 트랜잭션으로 바꾼다.</summary>
        private static void Run(bool apply)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 정상인 Edit Mode에서 실행해야 합니다.");
            Directory.CreateDirectory(ReportRoot);
            var assets = AssetDatabase.FindAssets("t:MotionSetAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<MotionSetAsset>).Where(asset => asset != null).ToList();
            var report = new StringBuilder();
            var changed = new List<MotionSetAsset>();
            var backups = new Dictionary<string, byte[]>();
            int windows = 0;
            int candidates = 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Motion Warp 끝점 보정 이관");
            try
            {
                foreach (MotionSetAsset asset in assets)
                {
                    if (UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(asset))
                        throw new InvalidOperationException("managed reference 누락: " + AssetDatabase.GetAssetPath(asset));
                    var warps = Collect(asset.motionSet).OfType<MotionEvent_MotionWarp>()
                        .Where(warp => warp.settingsVersion < 1).ToList();
                    if (warps.Count == 0) continue;
                    candidates++;
                    string path = AssetDatabase.GetAssetPath(asset);
                    string protectedEvents = ProtectedEvents(asset.motionSet);
                    if (apply)
                    {
                        backups.Add(path, File.ReadAllBytes(path));
                        string backupPath = Path.Combine(ReportRoot, "BeforeMigration", path);
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                        if (!File.Exists(backupPath)) File.WriteAllBytes(backupPath, backups[path]);
                        Undo.RegisterCompleteObjectUndo(asset, "Motion Warp 끝점 보정 이관");
                    }
                    foreach (MotionEvent_MotionWarp warp in warps)
                    {
                        MotionWarpWindowSettings before = warp.ResolveLegacySettings();
                        if (before.ResolveYPolicy() != WarpYPolicy.IgnoreY)
                            throw new InvalidOperationException("수직 워프는 공중 이동 프로필 검토 후 이관해야 합니다: " + path);
                        report.AppendLine(path + " | " + warp.startTime + "~" + warp.endTime);
                        report.AppendLine("기존 유효 설정: " + JsonUtility.ToJson(before));
                        windows++;
                        if (!apply) continue;
                        // 프리셋을 없애기 전에 실제 실행하던 필드 값을 모두 보존한다.
                        foreach (FieldInfo field in typeof(MotionWarpWindowSettings).GetFields())
                        {
                            FieldInfo destination = typeof(MotionEvent_MotionWarp).GetField(field.Name);
                            if (destination != null && destination.FieldType == field.FieldType)
                                destination.SetValue(warp, field.GetValue(before));
                        }
                        bool melee = before.preset == MotionWarpPreset.LightAttack || before.preset == MotionWarpPreset.HeavyAttack;
                        if (melee && before.arrivalMode == WarpArrivalMode.TargetCenter)
                            warp.arrivalMode = WarpArrivalMode.ContactShell;
                        warp.preset = MotionWarpPreset.Custom;
                        warp.modifierType = MotionWarpModifierType.EndpointCorrection;
                        warp.resolverPolicy = WarpResolverPolicy.UseExisting;
                        if (warp.targetPolicy == MotionWarpTargetPolicy.Predictive)
                            warp.targetPolicy = MotionWarpTargetPolicy.Live;
                        // 원본 재생 속도와 전진량을 대상 거리로 바꾸지 않는 새 계약이다.
                        warp.playbackRateWarpPolicy = PlaybackRateWarpPolicy.Disabled;
                        warp.usePlaybackRateWarp = false;
                        if (warp.amplifyEnabled)
                            throw new InvalidOperationException("증폭 구간은 원본 스텝 클립 검토가 필요합니다: " + path);
                        warp.settingsVersion = 1;
                        foreach (var profile in warp.bakedProfiles ?? new List<MotionWarpRootMotionBakeProfile>())
                        {
                            if (profile != null && profile.HasTrajectory && profile.sourceFingerprint ==
                                WarpRootMotionBatchBakeWindow.BuildSourceFingerprint(asset, warp, profile.avatar, profile.animatorScale))
                                MotionWarpTrajectory.RecordSource(profile, asset.motionSet);
                        }
                        report.AppendLine("이관 설정: " + JsonUtility.ToJson(warp.ResolveSettings()));
                    }
                    if (apply)
                    {
                        if (protectedEvents != ProtectedEvents(asset.motionSet))
                            throw new InvalidOperationException("비워프 이벤트 변경 감지: " + path);
                        EditorUtility.SetDirty(asset);
                        changed.Add(asset);
                    }
                }
                if (apply)
                {
                    foreach (var asset in changed) AssetDatabase.SaveAssetIfDirty(asset);
                    Undo.CollapseUndoOperations(group);
                }
                report.AppendLine($"apply={apply}, assets={candidates}, windows={windows}");
                string reportPath = Path.Combine(ReportRoot, apply ? "migration.txt" : "dry-run.txt");
                // 재실행의 무변경 결과가 실제 이관 당시의 전후 기록을 지우지 않게 한다.
                if (candidates > 0 || !File.Exists(reportPath))
                    File.WriteAllText(reportPath, report.ToString());
                File.WriteAllText(Path.Combine(ReportRoot, "last-run.txt"), $"apply={apply}, assets={candidates}, windows={windows}");
                Debug.Log($"[MotionWarpMigration] apply={apply}, assets={candidates}, windows={windows}");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var entry in backups) File.WriteAllBytes(entry.Key, entry.Value);
                AssetDatabase.Refresh();
                throw;
            }
        }

        private static string ProtectedEvents(MotionSet set)
            => string.Join("\n", Collect(set).Where(evt => evt is not MotionEvent_MotionWarp)
                .Select(evt => evt.GetType().AssemblyQualifiedName + JsonUtility.ToJson(evt)));

        internal static IEnumerable<MotionEventBase> Collect(MotionSet set)
        {
            if (set == null) yield break;
            if (set.globalEvents != null)
                foreach (var evt in set.globalEvents) if (evt != null) yield return evt;
            float offset = 0f;
            if (set.motions == null) yield break;
            foreach (var motion in set.motions)
            {
                if (motion?.events != null)
                    foreach (var evt in motion.events)
                    {
                        if (evt == null) continue;
                        evt.globalStartTimeOffset = offset;
                        yield return evt;
                    }
                offset += motion?.Duration ?? 0f;
            }
            if (set.layers != null)
                foreach (var layer in set.layers)
                {
                    if (layer?.globalEvents != null)
                        foreach (var evt in layer.globalEvents) if (evt != null) yield return evt;
                    if (layer?.motions == null) continue;
                    foreach (var motion in layer.motions)
                        if (motion?.events != null)
                            foreach (var evt in motion.events) if (evt != null) yield return evt;
                }
        }
    }
}
#endif
