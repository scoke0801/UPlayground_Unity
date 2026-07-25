using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public sealed class AbilityMotionReport
    {
        public MotionSetAsset Motion;
        public float Duration;
        public int EventCount;
        public int RequiredHitPhaseCount;
        public bool HasProjectileEvent;
        public bool HasTelegraphEvent;
        public readonly List<string> EventTypes = new();
        public readonly List<AbilityProductionIssue> Issues = new();
    }

    public static class AbilityMotionAnalyzer
    {
        public static AbilityMotionReport Analyze(
            UPlayGroundMotionAbilityPayloadSO payload)
        {
            var report = new AbilityMotionReport();
            if (payload?.attackInfo?.baseInfo == null)
            {
                report.Issues.Add(Error(
                    "MOTION.PAYLOAD",
                    "분석할 Motion Payload가 없습니다.",
                    payload));
                return report;
            }

            MotionReferenceSO reference =
                payload.attackInfo.baseInfo.motionRef;
            report.Motion = reference != null ? reference.defaultMotion : null;
            if (report.Motion?.motionSet == null)
            {
                report.Issues.Add(Error(
                    "MOTION.DEFAULT_MISSING",
                    "기본 Motion을 해석할 수 없습니다. 무기 타입을 모르는 상태에서 "
                    + "첫 override를 임의 선택하지 않습니다.",
                    reference));
                return report;
            }

            MotionSet set = report.Motion.motionSet;
            report.Duration = set.TotalDuration;
            List<MotionEventBase> events =
                set.GetEventsInRange(
                    -0.001f,
                    Mathf.Max(set.TotalDuration, 0.001f));
            var uniqueTypes = new HashSet<string>(StringComparer.Ordinal);
            int maxPhase = -1;
            for (int i = 0; i < events.Count; i++)
            {
                MotionEventBase motionEvent = events[i];
                if (motionEvent == null)
                    continue;
                report.EventCount++;
                Type type = motionEvent.GetType();
                string typeName = type.Name;
                if (uniqueTypes.Add(typeName))
                    report.EventTypes.Add(typeName);
                if (string.Equals(
                        typeName,
                        "SpawnProjectileEvent",
                        StringComparison.Ordinal))
                    report.HasProjectileEvent = true;
                if (typeName.IndexOf(
                        "Telegraph",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    report.HasTelegraphEvent = true;

                FieldInfo phaseField = type.GetField(
                    "hitPhaseIndex",
                    BindingFlags.Instance | BindingFlags.Public);
                if (phaseField?.FieldType == typeof(int))
                    maxPhase = Mathf.Max(
                        maxPhase,
                        (int)phaseField.GetValue(motionEvent));
            }
            report.EventTypes.Sort(StringComparer.Ordinal);
            report.RequiredHitPhaseCount = maxPhase + 1;
            int actualPhases =
                payload.attackInfo.baseInfo.hitPhases?.Count ?? 0;
            if (report.RequiredHitPhaseCount > actualPhases)
            {
                report.Issues.Add(Error(
                    "MOTION.HIT_PHASE_SHORTAGE",
                    $"Motion은 {report.RequiredHitPhaseCount}개 HitPhase를 요구하지만 "
                    + $"Payload에는 {actualPhases}개만 있습니다.",
                    payload));
            }
            if (report.EventCount == 0)
            {
                report.Issues.Add(new AbilityProductionIssue(
                    "MOTION.NO_EVENT",
                    AbilityProductionSeverity.Warning,
                    "Motion 타임라인에 이벤트가 없습니다.",
                    report.Motion));
            }
            return report;
        }

        public static bool ExpandHitPhasesToMatch(
            UPlayGroundMotionAbilityPayloadSO payload,
            AbilityMotionReport report)
        {
            if (payload?.attackInfo?.baseInfo == null || report == null)
                return false;
            List<HitPhaseData> phases =
                payload.attackInfo.baseInfo.hitPhases ??= new List<HitPhaseData>();
            if (report.RequiredHitPhaseCount <= phases.Count)
                return false;
            Undo.RecordObject(payload, "Motion 기준 HitPhase 확장");
            while (phases.Count < report.RequiredHitPhaseCount)
                phases.Add(new HitPhaseData());
            EditorUtility.SetDirty(payload);
            return true;
        }

        private static AbilityProductionIssue Error(
            string code,
            string message,
            UnityEngine.Object context) =>
            new(code, AbilityProductionSeverity.Error, message, context);
    }

    public sealed class AbilityDependencyReport
    {
        public UnityEngine.Object Target;
        public readonly List<UnityEngine.Object> Referencers = new();
    }

    public static class AbilityDependencyAnalyzer
    {
        public static AbilityDependencyReport FindReferencers(
            UnityEngine.Object target)
        {
            var report = new AbilityDependencyReport { Target = target };
            string targetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrWhiteSpace(targetPath))
                return report;

            var visited = new HashSet<string>(StringComparer.Ordinal);
            string[] filters =
            {
                "t:GameplayAbilitySO",
                "t:AbilitySetSO",
                "t:UPlayGroundMotionAbilityPayloadSO",
                "t:GameplayEffectSO",
                "t:PassiveAbilitySO",
            };
            for (int filterIndex = 0;
                 filterIndex < filters.Length;
                 filterIndex++)
            {
                string[] guids = AssetDatabase.FindAssets(
                    filters[filterIndex]);
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (path == targetPath || !visited.Add(path))
                        continue;
                    string[] dependencies =
                        AssetDatabase.GetDependencies(path, true);
                    if (Array.IndexOf(dependencies, targetPath) < 0)
                        continue;
                    UnityEngine.Object asset =
                        AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null)
                        report.Referencers.Add(asset);
                }
            }
            return report;
        }
    }

    public sealed class AbilityClonePlan
    {
        public GameplayAbilitySO Source;
        public UPlayGroundMotionAbilityPayloadSO SourcePayload;
        public string AbilityId;
        public string AbilityPath;
        public string PayloadPath;
        public readonly List<AbilityProductionIssue> Issues = new();
        public bool CanApply => Source != null
            && SourcePayload != null
            && Issues.Find(x => x.Severity == AbilityProductionSeverity.Error)
                == null;
    }

    public static class AbilityCloneService
    {
        public static AbilityClonePlan Build(
            GameplayAbilitySO source,
            string abilityId,
            string assetName,
            string saveRoot)
        {
            var plan = new AbilityClonePlan
            {
                Source = source,
                AbilityId = abilityId?.Trim(),
            };
            plan.SourcePayload = FindMotionPayload(source);
            string root = (saveRoot ?? string.Empty)
                .Trim().Replace('\\', '/').TrimEnd('/');
            string safeName = (assetName ?? string.Empty)
                .Trim().Replace('/', '_').Replace('\\', '_');
            plan.AbilityPath = $"{root}/Abilities/GA_{safeName}.asset";
            plan.PayloadPath =
                $"{root}/Payloads/AbilityPayload_{safeName}.asset";
            if (source == null)
                AddError(plan, "CLONE.SOURCE", "복제 원본 Ability가 없습니다.");
            if (plan.SourcePayload == null)
                AddError(
                    plan,
                    "CLONE.PAYLOAD",
                    "원본의 기본 Variant에서 Motion Payload를 찾지 못했습니다.");
            if (string.IsNullOrWhiteSpace(plan.AbilityId))
                AddError(plan, "CLONE.ID", "새 abilityId가 필요합니다.");
            else
                CheckAbilityIdConflict(plan);
            if (string.IsNullOrWhiteSpace(safeName)
                || !root.StartsWith("Assets/", StringComparison.Ordinal))
                AddError(plan, "CLONE.PATH", "Assets/ 아래의 유효한 저장 경로가 필요합니다.");
            CheckConflict(plan, plan.AbilityPath);
            CheckConflict(plan, plan.PayloadPath);
            return plan;
        }

        public static AbilityProductionResult Apply(AbilityClonePlan plan)
        {
            if (plan?.CanApply != true)
                return Failure("오류가 있는 복제 계획은 적용할 수 없습니다.");
            AbilityClonePlan latest = Build(
                plan.Source,
                plan.AbilityId,
                System.IO.Path.GetFileNameWithoutExtension(plan.AbilityPath)
                    .Replace("GA_", string.Empty),
                System.IO.Path.GetDirectoryName(
                        System.IO.Path.GetDirectoryName(plan.AbilityPath))
                    ?.Replace('\\', '/'));
            if (!latest.CanApply)
                return Failure("Preview 이후 경로 또는 원본 상태가 변경되었습니다.");

            try
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(plan.AbilityPath)
                    ?.Replace('\\', '/'));
                EnsureFolder(System.IO.Path.GetDirectoryName(plan.PayloadPath)
                    ?.Replace('\\', '/'));
                UPlayGroundMotionAbilityPayloadSO payload =
                    UnityEngine.Object.Instantiate(plan.SourcePayload);
                payload.name =
                    System.IO.Path.GetFileNameWithoutExtension(plan.PayloadPath);
                AssetDatabase.CreateAsset(payload, plan.PayloadPath);

                GameplayAbilitySO ability =
                    UnityEngine.Object.Instantiate(plan.Source);
                ability.name =
                    System.IO.Path.GetFileNameWithoutExtension(plan.AbilityPath);
                ability.abilityId = plan.AbilityId;
                for (int i = 0; i < ability.variants.Count; i++)
                {
                    if (ability.variants[i]?.executionPayload
                        == plan.SourcePayload)
                        ability.variants[i].executionPayload = payload;
                }
                AssetDatabase.CreateAsset(ability, plan.AbilityPath);
                AssetDatabase.SaveAssets();
                Selection.activeObject = ability;
                return new AbilityProductionResult
                {
                    Success = true,
                    Message = "Ability와 Motion Payload를 안전 복제했습니다. "
                        + "TaskGraph, MotionReference, Effect는 명시적으로 공유됩니다.",
                    Ability = ability,
                    Payload = payload,
                };
            }
            catch (Exception exception)
            {
                if (AssetDatabase.LoadMainAssetAtPath(plan.AbilityPath) != null)
                    AssetDatabase.DeleteAsset(plan.AbilityPath);
                if (AssetDatabase.LoadMainAssetAtPath(plan.PayloadPath) != null)
                    AssetDatabase.DeleteAsset(plan.PayloadPath);
                return Failure($"Ability 복제 실패: {exception.Message}");
            }
        }

        private static UPlayGroundMotionAbilityPayloadSO FindMotionPayload(
            GameplayAbilitySO source)
        {
            if (source?.variants == null)
                return null;
            for (int i = 0; i < source.variants.Count; i++)
                if (source.variants[i]?.executionPayload
                    is UPlayGroundMotionAbilityPayloadSO payload)
                    return payload;
            return null;
        }

        private static void CheckConflict(AbilityClonePlan plan, string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AddError(plan, "CLONE.CONFLICT", $"경로가 이미 존재합니다: {path}");
        }

        private static void CheckAbilityIdConflict(AbilityClonePlan plan)
        {
            string[] guids = AssetDatabase.FindAssets("t:GameplayAbilitySO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets =
                    AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is GameplayAbilitySO ability
                        && string.Equals(
                            ability.abilityId,
                            plan.AbilityId,
                            StringComparison.Ordinal))
                    {
                        AddError(
                            plan,
                            "CLONE.DUPLICATE_ID",
                            $"abilityId가 이미 존재합니다: {path}");
                        return;
                    }
                }
            }
        }

        private static void AddError(
            AbilityClonePlan plan,
            string code,
            string message) =>
            plan.Issues.Add(new AbilityProductionIssue(
                code,
                AbilityProductionSeverity.Error,
                message));

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static AbilityProductionResult Failure(string message) =>
            new() { Success = false, Message = message };
    }
}
