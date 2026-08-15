#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;

namespace UPlayGround.EditorTools
{
    /// <summary>
    /// MotionWarp 신규 도착 의미를 명시적으로 선택한 MotionSet에만 적용한다.
    /// 전체 204개를 자동 변경하지 않으며 Dry Run, Undo, 오류 시 그룹 롤백을 보장한다.
    /// </summary>
    public static class MotionWarpContactShellMigrationTool
    {
        private static readonly string[] RepresentativePaths =
        {
            // 플레이어 8종: 빠른/중형/대형 무기와 Light/Heavy를 고르게 포함한다.
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_1_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Heavy_Attack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/HeavyAttack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_2.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Spear/Humanoid_Spear_Attack_1.asset",

            // 몬스터 공용 Humanoid 4종: 소형 검격부터 중량 무기까지 포함한다.
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_2.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/SwordShield/Humanoid_SwordShield_Attack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_1.asset",
        };

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/대표 12개 Dry Run")]
        public static void DryRunRepresentative() => Run(LoadRepresentativeAssets(), apply: false);

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/대표 12개 ContactShell 적용")]
        public static void ApplyRepresentative()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Motion Warp 대표 12개 변환",
                    "대표 MotionSet 12개의 Light/Heavy 워프만 ContactShell로 변환합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;
            Run(LoadRepresentativeAssets(), apply: true);
        }

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/선택 에셋 Dry Run")]
        public static void DryRunSelection() => Run(LoadSelectedAssets(), apply: false);

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/선택 에셋 ContactShell 적용")]
        public static void ApplySelection()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "선택 MotionSet 변환",
                    "Project 창에서 선택한 MotionSet의 Light/Heavy 워프만 변환합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;
            Run(LoadSelectedAssets(), apply: true);
        }

        private static void Run(IReadOnlyList<MotionSetAsset> assets, bool apply)
        {
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일 중이거나 컴파일 오류가 있어 MotionSet을 저장할 수 없습니다.");
            if (assets == null || assets.Count == 0)
                throw new InvalidOperationException("변환할 MotionSetAsset이 없습니다.");

            var reports = new List<string>();
            var changedAssets = new List<MotionSetAsset>();
            int group = -1;
            try
            {
                if (apply)
                {
                    Undo.IncrementCurrentGroup();
                    group = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Motion Warp ContactShell 변환");
                }

                int eventCount = 0;
                int changeCount = 0;
                foreach (MotionSetAsset asset in assets)
                {
                    if (asset == null || asset.motionSet == null)
                        throw new InvalidOperationException("null MotionSet 또는 motionSet 데이터가 포함되어 있습니다.");

                    List<MotionEvent_MotionWarp> events = CollectWarpEvents(asset.motionSet);
                    eventCount += events.Count;
                    int assetChanges = 0;
                    foreach (MotionEvent_MotionWarp warp in events)
                    {
                        if (warp.preset is not (MotionWarpPreset.LightAttack or MotionWarpPreset.HeavyAttack))
                        {
                            reports.Add($"SKIP | {AssetDatabase.GetAssetPath(asset)} | {warp.preset} | 일반 근접 프리셋 아님");
                            continue;
                        }

                        if (warp.arrivalMode == WarpArrivalMode.ContactShell)
                        {
                            reports.Add($"SKIP | {AssetDatabase.GetAssetPath(asset)} | {warp.preset} | 이미 ContactShell");
                            continue;
                        }

                        reports.Add(
                            $"CHANGE | {AssetDatabase.GetAssetPath(asset)} | {warp.preset} | " +
                            $"{warp.arrivalMode} -> ContactShell");
                        assetChanges++;
                        changeCount++;
                    }

                    if (!apply || assetChanges == 0)
                        continue;

                    Undo.RegisterCompleteObjectUndo(asset, "Motion Warp ContactShell 변환");
                    foreach (MotionEvent_MotionWarp warp in events)
                    {
                        if ((warp.preset is MotionWarpPreset.LightAttack or MotionWarpPreset.HeavyAttack)
                            && warp.arrivalMode != WarpArrivalMode.ContactShell)
                            ApplyContactShellProfile(warp);
                    }
                    EditorUtility.SetDirty(asset);
                    changedAssets.Add(asset);
                }

                Debug.Log(
                    $"[MotionWarpMigration] {(apply ? "적용" : "Dry Run")} 완료 — " +
                    $"에셋 {assets.Count}개, 워프 이벤트 {eventCount}개, 변경 {changeCount}개\n" +
                    string.Join("\n", reports));

                if (!apply)
                    return;

                foreach (MotionSetAsset asset in changedAssets)
                    AssetDatabase.SaveAssetIfDirty(asset);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                if (apply && group >= 0)
                {
                    Undo.RevertAllDownToGroup(group);
                    foreach (MotionSetAsset asset in changedAssets)
                    {
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssetIfDirty(asset);
                    }
                }
                throw;
            }
        }

        private static void ApplyContactShellProfile(MotionEvent_MotionWarp warp)
        {
            warp.arrivalMode = WarpArrivalMode.ContactShell;
            warp.localArrivalOffset = Vector3.zero;
            warp.playbackRateWarpPolicy = PlaybackRateWarpPolicy.Disabled;
            warp.usePlaybackRateWarp = false;
            warp.playbackRateRange = new Vector2(0.95f, 1.05f);
            warp.overrideDistance = true;

            if (warp.preset == MotionWarpPreset.HeavyAttack)
            {
                warp.desiredStandOff = 0.24f;
                warp.noTranslationWithinReach = 0.15f;
                warp.maxCorrectionDistance = 0.8f;
                warp.maxCorrectionRatio = 0.4f;
                warp.maxWarpAngle = 35f;
                warp.translationEndLeadTime = 0.1f;
                warp.maxDistance = 3f;
                warp.maxSpeed = 16f;
                return;
            }

            warp.desiredStandOff = 0.18f;
            warp.noTranslationWithinReach = 0.12f;
            warp.maxCorrectionDistance = 0.5f;
            warp.maxCorrectionRatio = 0.3f;
            warp.maxWarpAngle = 45f;
            warp.translationEndLeadTime = 0.06f;
            warp.maxDistance = 2.5f;
            warp.maxSpeed = 18f;
        }

        private static List<MotionEvent_MotionWarp> CollectWarpEvents(MotionSet set)
        {
            var results = new List<MotionEvent_MotionWarp>();
            AddEvents(set.globalEvents, results);
            AddMotionEvents(set.motions, results);
            if (set.layers != null)
            {
                foreach (MotionLayer layer in set.layers)
                {
                    if (layer == null)
                        continue;
                    AddEvents(layer.globalEvents, results);
                    AddMotionEvents(layer.motions, results);
                }
            }
            return results;
        }

        private static void AddMotionEvents(
            IEnumerable<UPlayGround.Animation.Motion> motions,
            ICollection<MotionEvent_MotionWarp> results)
        {
            if (motions == null)
                return;
            foreach (UPlayGround.Animation.Motion motion in motions)
            {
                if (motion == null)
                    continue;
                AddEvents(motion.events, results);
            }
        }

        private static void AddEvents(
            IEnumerable<MotionEventBase> events,
            ICollection<MotionEvent_MotionWarp> results)
        {
            if (events == null)
                return;
            foreach (MotionEventBase motionEvent in events)
            {
                if (motionEvent == null)
                    throw new InvalidOperationException("Missing managed reference 이벤트를 발견했습니다. 저장을 중단합니다.");
                if (motionEvent is MotionEvent_MotionWarp warp)
                    results.Add(warp);
            }
        }

        private static IReadOnlyList<MotionSetAsset> LoadRepresentativeAssets()
        {
            var assets = new List<MotionSetAsset>(RepresentativePaths.Length);
            foreach (string path in RepresentativePaths)
            {
                MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset == null)
                    throw new InvalidOperationException($"대표 MotionSet을 찾지 못했습니다: {path}");
                assets.Add(asset);
            }
            return assets;
        }

        private static IReadOnlyList<MotionSetAsset> LoadSelectedAssets()
            => Selection.objects
                .OfType<MotionSetAsset>()
                .Distinct()
                .ToArray();
    }
}
#endif
