#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.Editor;
using UPlayGround.MovementController;

namespace UPlayGround.EditorTools
{
    /// <summary>플레이어 Katana 일반 검격의 워핑을 원본 이동량 내 보정으로 저작한다.</summary>
    public static class KatanaMotionWarpAuthoring
    {
        private const string OwnerPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_KatanaAnimationSet.asset";
        private const string MotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/";
        private const string ReportPath = "Logs/KatanaWarp/authoring.txt";

        /// <summary>실제 경·강공격 매핑의 변경 대상과 베이크 상태를 저장 없이 확인한다.</summary>
        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/Katana 일반 공격 분석")]
        public static void Analyze() => Run(false);

        /// <summary>검격 보정값과 누락 베이크를 적용하고 실패하면 전체를 복구한다.</summary>
        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/Katana 일반 공격 적용")]
        public static void Apply() => Run(true);

        private static void Run(bool apply)
        {
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed || EditorApplication.isPlaying)
                throw new InvalidOperationException("컴파일 오류가 없는 Edit Mode에서 실행해야 합니다.");

            ActorAnimationMotionSet owner = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(OwnerPath);
            if (owner == null || owner.abilityMotions == null)
                throw new InvalidOperationException("플레이어 Katana 모션 매핑을 찾지 못했습니다.");

            var targets = new Dictionary<MotionSetAsset, bool>();
            foreach (var binding in owner.abilityMotions)
            {
                string key = binding.Key.value;
                bool heavy = key.StartsWith("Katana.Heavy.", StringComparison.Ordinal);
                if (!heavy && !key.StartsWith("Katana.Light.", StringComparison.Ordinal))
                    continue;
                if (binding.Value == null)
                    throw new InvalidOperationException($"MotionKey 매핑 누락: {key}");
                if (targets.TryGetValue(binding.Value, out bool previous) && previous != heavy)
                    throw new InvalidOperationException($"경·강공격이 같은 모션을 공유합니다: {key}");
                targets[binding.Value] = heavy;
            }
            if (targets.Count == 0)
                throw new InvalidOperationException("Katana 일반 공격 매핑이 없습니다.");

            var warps = new Dictionary<MotionSetAsset, MotionEvent_MotionWarp>();
            foreach (MotionSetAsset asset in targets.Keys)
            {
                ValidateAsset(asset);
                MotionEvent_MotionWarp warp = asset.motionSet.motions[0].events
                    .OfType<MotionEvent_MotionWarp>().SingleOrDefault();
                if (warp != null)
                    warps.Add(asset, warp);
            }
            HashSet<MotionSetAsset> shared = FindSharedMotions(owner, warps.Keys);

            var report = new StringBuilder();
            report.AppendLine($"Katana 일반 공격: 매핑 모션 {targets.Count}, 워핑 모션 {warps.Count}, 공유 모션 {shared.Count}, 적용={apply}");
            var backups = new Dictionary<UnityEngine.Object, byte[]>();
            var createdPaths = new List<string>();
            int group = -1;
            try
            {
                if (apply)
                {
                    Undo.IncrementCurrentGroup();
                    group = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Katana 일반 검격 워핑 제한");
                    if (shared.Count > 0)
                    {
                        backups.Add(owner, File.ReadAllBytes(OwnerPath));
                        Undo.RegisterCompleteObjectUndo(owner, "Katana 플레이어 모션 분리");
                        ForkSharedMotions(owner, targets, warps, shared, createdPaths);
                        EditorUtility.SetDirty(owner);
                    }
                }
                foreach (var entry in warps)
                {
                    MotionSetAsset asset = entry.Key;
                    MotionEvent_MotionWarp warp = entry.Value;
                    MotionWarpWindowSettings before = warp.settingsVersion < 1
                        ? warp.ResolveLegacySettings() : warp.ResolveSettings();
                    report.AppendLine($"{AssetDatabase.GetAssetPath(asset)} | {before.modifierType}/{before.arrivalMode}" +
                        $" | window={warp.startTime:R}~{warp.endTime:R} | path={warp.bakedPathLen:R} | profiles={warp.bakedProfiles.Count}");
                    if (!apply)
                        continue;
                    if (!createdPaths.Contains(AssetDatabase.GetAssetPath(asset)))
                        backups.Add(asset, File.ReadAllBytes(AssetDatabase.GetAssetPath(asset)));
                    Undo.RegisterCompleteObjectUndo(asset, "Katana 일반 검격 워핑 제한");
                    ApplyProfile(warp, before, targets[asset]);
                    EditorUtility.SetDirty(asset);
                }
                if (apply)
                {
                    // 신규 DeltaWarp도 출시 첫 시전부터 동일하게 동작하도록 실제 소유 리그를 베이크한다.
                    WarpRootMotionBatchBakeWindow.BakeMissingProfiles(warps.Keys);
                    foreach (var entry in warps)
                    {
                        MotionEvent_MotionWarp warp = entry.Value;
                        if (warp.bakedProfiles.Count == 0 || warp.bakedProfiles.Any(profile =>
                                profile == null || !warp.TryGetBakedProfile(profile.avatar, profile.animatorScale, out _)))
                            throw new InvalidOperationException($"현재 리그 궤적 베이크 누락: {entry.Key.name}");
                        AssetDatabase.SaveAssetIfDirty(entry.Key);
                        report.AppendLine($"APPLIED | {entry.Key.name} | path={warp.bakedPathLen:R}" +
                            $" | maxCorrection={warp.maxCorrectionDistance:R} | ratio={warp.maxCorrectionRatio:R}");
                    }
                    AssetDatabase.SaveAssetIfDirty(owner);
                    Undo.CollapseUndoOperations(group);
                }
            }
            catch
            {
                if (group >= 0)
                {
                    Undo.RevertAllDownToGroup(group);
                    foreach (var backup in backups)
                    {
                        string path = AssetDatabase.GetAssetPath(backup.Key);
                        File.WriteAllBytes(path, backup.Value);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    foreach (string path in createdPaths)
                        AssetDatabase.DeleteAsset(path);
                }
                throw;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log(report.ToString());
        }

        private static void ValidateAsset(MotionSetAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!path.StartsWith(MotionRoot, StringComparison.Ordinal)
                || SerializationUtility.HasManagedReferencesWithMissingTypes(asset))
                throw new InvalidOperationException($"공유 범위 또는 managed reference를 확인해야 합니다: {path}");
            MotionSet set = asset.motionSet;
            if (set == null || set.baseLayerIndex != 0 || set.motions?.Count != 1
                || set.layers?.Count > 0 || set.globalEvents?.Count > 0)
                throw new InvalidOperationException($"단일 전신 타임라인만 자동 적용합니다: {path}");
            var motion = set.motions[0];
            if (motion?.motionClip == null || motion.events == null || motion.events.Any(evt => evt == null))
                throw new InvalidOperationException($"클립 또는 이벤트 누락: {path}");
            foreach (MotionEvent_MotionWarp warp in motion.events.OfType<MotionEvent_MotionWarp>())
            {
                if (warp.timeLink.enabled || warp.startTime < 0f || warp.endTime <= warp.startTime
                    || warp.endTime > motion.Duration || warp.amplifyEnabled)
                    throw new InvalidOperationException($"시간 링크·증폭·워프 구간을 먼저 검토해야 합니다: {path}");
                float firstHit = motion.events.OfType<BeginCollisionEvent>()
                    .Select(hit => hit.startTime).DefaultIfEmpty(float.PositiveInfinity).Min();
                if (float.IsPositiveInfinity(firstHit) || firstHit <= warp.startTime)
                    throw new InvalidOperationException($"워프 이후 타격 근거가 없습니다: {path}");
            }
        }

        private static HashSet<MotionSetAsset> FindSharedMotions(ActorAnimationMotionSet owner, IEnumerable<MotionSetAsset> assets)
        {
            var scope = new HashSet<MotionSetAsset>(assets);
            var shared = new HashSet<MotionSetAsset>();
            foreach (string guid in AssetDatabase.FindAssets("t:ActorAnimationMotionSet"))
            {
                var other = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == null || other == owner || other.abilityMotions == null)
                    continue;
                foreach (MotionSetAsset asset in other.abilityMotions.Values)
                    if (asset != null && scope.Contains(asset)) shared.Add(asset);
                if (other.motionSlots != null)
                    foreach (MotionSetAsset asset in other.motionSlots.Values)
                        if (asset != null && scope.Contains(asset)) shared.Add(asset);
            }
            return shared;
        }

        private static void ForkSharedMotions(
            ActorAnimationMotionSet owner, Dictionary<MotionSetAsset, bool> targets,
            Dictionary<MotionSetAsset, MotionEvent_MotionWarp> warps,
            HashSet<MotionSetAsset> shared, List<string> createdPaths)
        {
            foreach (MotionSetAsset source in shared)
            {
                string destination = MotionRoot + source.name + "_PlayerGrounded.asset";
                if (File.Exists(destination) || !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), destination))
                    throw new InvalidOperationException($"기존 파일을 덮어쓰지 않습니다: {destination}");
                createdPaths.Add(destination);
                var clone = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(destination);
                ValidateAsset(clone);
                MotionEvent_MotionWarp warp = clone.motionSet.motions[0].events.OfType<MotionEvent_MotionWarp>().Single();
                if (ReferenceEquals(warp, warps[source]))
                    throw new InvalidOperationException("복제된 managed reference가 원본과 공유됩니다.");
                // 에셋 GUID가 바뀌면 출처 지문도 달라지므로 원본의 베이크를 재사용하지 않는다.
                warp.bakedProfiles.Clear();
                warp.bakedValid = false;
                warp.bakedFormatVersion = 0;
                targets[clone] = targets[source];
                targets.Remove(source);
                warps.Remove(source);
                warps.Add(clone, warp);
                foreach (MotionKey key in owner.abilityMotions.Keys.ToArray())
                    if (owner.abilityMotions[key] == source) owner.abilityMotions[key] = clone;
                if (owner.motionSlots != null)
                    foreach (var slot in owner.motionSlots.Keys.ToArray())
                        if (owner.motionSlots[slot] == source) owner.motionSlots[slot] = clone;
                Undo.RegisterCreatedObjectUndo(clone, "Katana 플레이어 모션 분리");
            }
        }

        private static void ApplyProfile(MotionEvent_MotionWarp warp, MotionWarpWindowSettings before, bool heavy)
        {
            // 구형 에셋의 실행값을 먼저 해석한 뒤 새 보정 계약을 명시적으로 저장한다.
            warp.preset = MotionWarpPreset.Custom;
            warp.modifierType = MotionWarpModifierType.EndpointCorrection;
            warp.settingsVersion = 1;
            warp.targetPolicy = MotionWarpTargetPolicy.Snapshot;
            warp.translationWeight = before.translationWeight;
            warp.rotationWeight = before.rotationWeight;
            warp.rotationCurve = before.rotationCurve;
            warp.ignoreY = true;
            warp.yPolicy = WarpYPolicy.IgnoreY;
            warp.arrivalMode = WarpArrivalMode.ContactShell;
            warp.playbackRateWarpPolicy = PlaybackRateWarpPolicy.Disabled;
            warp.usePlaybackRateWarp = false;
            warp.overrideDistance = true;
            warp.minDistance = heavy ? 0.35f : 0.25f;
            warp.maxDistance = heavy ? 3f : 2.5f;
            warp.maxSpeed = heavy ? 16f : 18f;
            warp.desiredStandOff = heavy ? 0.24f : 0.18f;
            warp.noTranslationWithinReach = heavy ? 0.15f : 0.12f;
            warp.maxCorrectionDistance = heavy ? 0.8f : 0.5f;
            warp.maxCorrectionRatio = heavy ? 0.4f : 0.3f;
            warp.maxWarpAngle = heavy ? 35f : 45f;
            warp.translationEndLeadTime = heavy ? 0.1f : 0.06f;
        }
    }
}
#endif
