using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public static class AbilityCreationPlanner
    {
        public static AbilityCreationPlan Build(AbilityCreationRequest request)
        {
            var plan = new AbilityCreationPlan { Request = request };
            ValidateRequest(request, plan);
            if (request == null || request.Recipe == null)
                return plan;

            string assetName = SanitizeAssetName(request.AssetName);
            string root = NormalizeRoot(request.SaveRoot);
            plan.StableAbilityId = request.AbilityId?.Trim() ?? string.Empty;
            plan.AbilityPath = $"{root}/Abilities/GA_{assetName}.asset";
            plan.PayloadPath = $"{root}/Payloads/AbilityPayload_{assetName}.asset";
            if (request.CreateCommitEffect)
            {
                string effectName = SanitizeAssetName(request.EffectAssetName);
                plan.EffectPath = $"{root}/Effects/GE_{effectName}.asset";
            }

            ValidatePath(plan.AbilityPath, AbilityPlanAssetKind.GameplayAbility, plan);
            ValidatePath(plan.PayloadPath, AbilityPlanAssetKind.MotionPayload, plan);
            if (request.CreateCommitEffect)
                ValidatePath(
                    plan.EffectPath,
                    AbilityPlanAssetKind.GameplayEffect,
                    plan);
            ValidateStableId(plan.StableAbilityId, plan);
            if (request.CreateCommitEffect)
                ValidateEffectId(request.EffectId?.Trim(), plan);

            if (!string.Equals(
                    plan.AbilityPath,
                    plan.PayloadPath,
                    StringComparison.Ordinal))
            {
                plan.AddItem(new AbilityPlanItem(
                    AbilityPlanOperation.Create,
                    AbilityPlanAssetKind.MotionPayload,
                    plan.PayloadPath));
                plan.AddItem(new AbilityPlanItem(
                    AbilityPlanOperation.Create,
                    AbilityPlanAssetKind.GameplayAbility,
                    plan.AbilityPath));
            }
            else
            {
                plan.AddIssue(new AbilityProductionIssue(
                    "PLAN.DUPLICATE_PATH",
                    AbilityProductionSeverity.Error,
                    "Ability와 Payload의 생성 경로가 같습니다."));
            }

            if (request.TargetSet != null)
            {
                plan.AddItem(new AbilityPlanItem(
                    AbilityPlanOperation.Modify,
                    AbilityPlanAssetKind.AbilitySet,
                    AssetDatabase.GetAssetPath(request.TargetSet),
                    request.TargetSet));
            }
            if (request.CreateCommitEffect)
            {
                plan.AddItem(new AbilityPlanItem(
                    AbilityPlanOperation.Create,
                    AbilityPlanAssetKind.GameplayEffect,
                    plan.EffectPath));
            }

            return plan;
        }

        private static void ValidateRequest(
            AbilityCreationRequest request,
            AbilityCreationPlan plan)
        {
            if (request == null)
            {
                plan.AddIssue(new AbilityProductionIssue(
                    "REQUEST.NULL",
                    AbilityProductionSeverity.Error,
                    "생성 요청이 없습니다."));
                return;
            }
            if (request.Recipe == null)
                AddRequired(plan, "REQUEST.RECIPE", "레시피를 선택해야 합니다.");
            if (request.TargetSet == null)
                AddRequired(plan, "REQUEST.TARGET_SET", "대상 AbilitySet이 필요합니다.");
            if (request.MotionReference == null)
                AddRequired(plan, "REQUEST.MOTION_REFERENCE", "MotionReference가 필요합니다.");
            else if (!request.MotionReference.HasAnyMotion)
                AddRequired(
                    plan,
                    "REQUEST.EMPTY_MOTION_REFERENCE",
                    "MotionReference에 실행 가능한 Motion이 없습니다.",
                    request.MotionReference);
            if (string.IsNullOrWhiteSpace(request.DisplayName))
                AddRequired(plan, "REQUEST.DISPLAY_NAME", "표시 이름이 필요합니다.");
            if (string.IsNullOrWhiteSpace(request.AbilityId))
                AddRequired(plan, "REQUEST.ABILITY_ID", "abilityId가 필요합니다.");
            if (string.IsNullOrWhiteSpace(request.AssetName))
                AddRequired(plan, "REQUEST.ASSET_NAME", "에셋 이름이 필요합니다.");
            if (string.IsNullOrWhiteSpace(request.SaveRoot))
                AddRequired(plan, "REQUEST.SAVE_ROOT", "저장 루트가 필요합니다.");
            else if (!NormalizeRoot(request.SaveRoot).StartsWith(
                         "Assets/",
                         StringComparison.Ordinal))
                AddRequired(
                    plan,
                    "REQUEST.INVALID_SAVE_ROOT",
                    "저장 루트는 Assets/ 아래여야 합니다.");
            if (request.RequiredLevel < 1)
                AddRequired(
                    plan,
                    "REQUEST.REQUIRED_LEVEL",
                    "요구 레벨은 1 이상이어야 합니다.");
            if (request.SelectionWeight <= 0f)
                AddRequired(
                    plan,
                    "REQUEST.SELECTION_WEIGHT",
                    "AI 선택 가중치는 0보다 커야 합니다.");
            if (request.MinDistance < 0f || request.MaxDistance < 0f)
                AddRequired(
                    plan,
                    "REQUEST.NEGATIVE_DISTANCE",
                    "사용 거리는 음수일 수 없습니다.");
            if (request.MaxDistance > 0f
                && request.MinDistance > request.MaxDistance)
                AddRequired(
                    plan,
                    "REQUEST.DISTANCE_ORDER",
                    "최소 거리가 최대 거리보다 큽니다.");

            if (request.TaskGraph == null && request.Recipe != null)
            {
                request.TaskGraph = AssetDatabase.LoadAssetAtPath<AbilityTaskGraphSO>(
                    request.Recipe.DefaultTaskGraphPath);
            }
            if (request.TaskGraph?.Root == null)
                AddRequired(
                    plan,
                    "REQUEST.TASK_GRAPH",
                    "실행 가능한 공용 Task Graph를 찾을 수 없습니다.",
                    request.TaskGraph);
            if (request.Recipe?.RequiresEffect == true
                && !request.CreateCommitEffect
                && request.CommitEffect == null
                && request.EndEffect == null)
            {
                AddRequired(
                    plan,
                    "REQUEST.EFFECT",
                    "이 레시피는 Commit 또는 End Effect가 필요합니다.");
            }
            if (request.CreateCommitEffect)
            {
                if (request.CommitEffect != null)
                    AddRequired(
                        plan,
                        "REQUEST.EFFECT_CREATE_OR_REUSE",
                        "Commit Effect 생성과 기존 Effect 공유를 동시에 선택할 수 없습니다.");
                if (string.IsNullOrWhiteSpace(request.EffectId))
                    AddRequired(
                        plan,
                        "REQUEST.EFFECT_ID",
                        "생성할 Effect ID가 필요합니다.");
                if (string.IsNullOrWhiteSpace(request.EffectAssetName))
                    AddRequired(
                        plan,
                        "REQUEST.EFFECT_ASSET_NAME",
                        "생성할 Effect 에셋 이름이 필요합니다.");
                if (request.EffectDurationSeconds < 0f)
                    AddRequired(
                        plan,
                        "REQUEST.EFFECT_DURATION",
                        "Effect 지속시간은 음수일 수 없습니다.");
                if (request.EffectDurationType
                    == GameplayEffectDurationType.Duration
                    && request.EffectDurationSeconds <= 0f)
                {
                    AddRequired(
                        plan,
                        "REQUEST.EFFECT_DURATION_REQUIRED",
                        "Duration Effect는 0보다 큰 지속시간이 필요합니다.");
                }
            }
            if (request.Recipe?.SupportsEffect == false
                && (request.CommitEffect != null || request.EndEffect != null))
            {
                plan.AddIssue(new AbilityProductionIssue(
                    "REQUEST.UNSUPPORTED_EFFECT",
                    AbilityProductionSeverity.Warning,
                    "선택한 레시피의 표준 구성에는 Effect가 포함되지 않습니다. "
                    + "명시한 Effect는 그대로 연결됩니다."));
            }

            ValidateBinding(request, plan);
        }

        private static void ValidateBinding(
            AbilityCreationRequest request,
            AbilityCreationPlan plan)
        {
            if (request.TargetSet == null)
                return;

            if (request.BindingMode == AbilitySetBindingMode.PlayerSkillSlot)
            {
                GameplayAbilitySO occupied =
                    request.TargetSet.GetPlayerAbility(request.PlayerSkillSlot);
                if (occupied != null && !request.ReplaceExistingBinding)
                {
                    AddRequired(
                        plan,
                        "REQUEST.OCCUPIED_PLAYER_SLOT",
                        $"플레이어 슬롯 {request.PlayerSkillSlot}이 이미 사용 중입니다. "
                        + "교체 옵션을 명시하세요.",
                        occupied);
                }
            }
            else if (request.BindingMode
                     == AbilitySetBindingMode.PlayerCombatSequence)
            {
                IReadOnlyList<GameplayAbilitySO> sequence =
                    request.TargetSet.GetCombatSequence(request.PlayerCombatSlot);
                if (sequence.Count > 0 && request.ReplaceExistingBinding)
                {
                    plan.AddIssue(new AbilityProductionIssue(
                        "REQUEST.REPLACE_COMBAT_SEQUENCE",
                        AbilityProductionSeverity.Warning,
                        $"전투 슬롯 {request.PlayerCombatSlot}의 기존 시퀀스를 "
                        + "새 Ability 하나로 교체합니다."));
                }
            }
        }

        private static void ValidateStableId(
            string abilityId,
            AbilityCreationPlan plan)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return;

            string[] guids = AssetDatabase.FindAssets("t:GameplayAbilitySO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is not GameplayAbilitySO ability
                        || !string.Equals(
                            ability.abilityId,
                            abilityId,
                            StringComparison.Ordinal))
                        continue;

                    plan.AddIssue(new AbilityProductionIssue(
                        "PLAN.DUPLICATE_ABILITY_ID",
                        AbilityProductionSeverity.Error,
                        $"abilityId '{abilityId}'가 이미 존재합니다: {path}",
                        ability));
                    return;
                }
            }
        }

        private static void ValidateEffectId(
            string effectId,
            AbilityCreationPlan plan)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return;
            string[] guids = AssetDatabase.FindAssets("t:GameplayEffectSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets =
                    AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is not GameplayEffectSO effect
                        || !string.Equals(
                            effect.effectId,
                            effectId,
                            StringComparison.Ordinal))
                        continue;
                    plan.AddIssue(new AbilityProductionIssue(
                        "PLAN.DUPLICATE_EFFECT_ID",
                        AbilityProductionSeverity.Error,
                        $"effectId '{effectId}'가 이미 존재합니다: {path}",
                        effect));
                    return;
                }
            }
        }

        private static void ValidatePath(
            string path,
            AbilityPlanAssetKind kind,
            AbilityCreationPlan plan)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                return;

            plan.AddIssue(new AbilityProductionIssue(
                "PLAN.PATH_CONFLICT",
                AbilityProductionSeverity.Error,
                $"{kind} 생성 경로가 이미 존재합니다: {path}",
                AssetDatabase.LoadMainAssetAtPath(path)));
        }

        private static string NormalizeRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Assets/10.Datas/Ability/Actor";
            return path.Trim().Replace('\\', '/').TrimEnd('/');
        }

        private static string SanitizeAssetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "NewAbility";

            var invalid = new HashSet<char>(
                System.IO.Path.GetInvalidFileNameChars());
            var characters = name.Trim().ToCharArray();
            for (int i = 0; i < characters.Length; i++)
                if (invalid.Contains(characters[i])
                    || characters[i] == '/'
                    || characters[i] == '\\')
                    characters[i] = '_';
            return new string(characters);
        }

        private static void AddRequired(
            AbilityCreationPlan plan,
            string code,
            string message,
            UnityEngine.Object context = null)
        {
            plan.AddIssue(new AbilityProductionIssue(
                code,
                AbilityProductionSeverity.Error,
                message,
                context));
        }
    }
}
