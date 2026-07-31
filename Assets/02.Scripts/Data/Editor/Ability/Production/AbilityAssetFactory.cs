using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public static class AbilityAssetFactory
    {
        /// <summary>단일 Variant로 생성하는 Ability의 기본 variantId.
        /// motionKey의 variantId와 반드시 같아야 검증을 통과한다.</summary>
        public const string DefaultVariantId = "Default";

        public static AbilityProductionResult Apply(AbilityCreationPlan plan) =>
            ApplyInternal(plan, null);

        internal static AbilityProductionResult ApplyForTests(
            AbilityCreationPlan plan,
            AbilityProductionStage failAfterStage) =>
            ApplyInternal(plan, failAfterStage);

        private static AbilityProductionResult ApplyInternal(
            AbilityCreationPlan plan,
            AbilityProductionStage? failAfterStage)
        {
            if (plan == null)
                return Failure("생성 계획이 없습니다.");
            if (!plan.CanApply)
                return Failure("오류가 있는 생성 계획은 적용할 수 없습니다.");
            AbilityCreationPlan latest =
                AbilityCreationPlanner.Build(plan.Request);
            if (!latest.CanApply)
                return Failure(
                    "Preview 이후 생성 조건이 변경되었습니다. 계획을 다시 확인하세요.");
            if (!string.Equals(
                    latest.AbilityPath,
                    plan.AbilityPath,
                    StringComparison.Ordinal)
                || !string.Equals(
                    latest.PayloadPath,
                    plan.PayloadPath,
                    StringComparison.Ordinal)
                || !string.Equals(
                    latest.StableAbilityId,
                    plan.StableAbilityId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    latest.EffectPath,
                    plan.EffectPath,
                    StringComparison.Ordinal))
            {
                return Failure(
                    "Preview 이후 ID 또는 저장 경로가 변경되었습니다. "
                    + "계획을 다시 생성하세요.");
            }

            AbilityCreationRequest request = plan.Request;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Ability 생성: {request.DisplayName}");

            var createdPaths = new List<string>();
            var createdFolders = new List<string>();
            GameplayAbilitySO ability = null;
            UPlayGroundMotionAbilityPayloadSO payload = null;
            GameplayEffectSO effect = null;

            try
            {
                EnsureParentFolder(plan.PayloadPath, createdFolders);
                EnsureParentFolder(plan.AbilityPath, createdFolders);

                payload = ScriptableObject.CreateInstance<
                    UPlayGroundMotionAbilityPayloadSO>();
                payload.name = System.IO.Path.GetFileNameWithoutExtension(
                    plan.PayloadPath);
                ConfigurePayload(payload, request);
                Undo.RegisterCreatedObjectUndo(payload, "Ability Payload 생성");
                AssetDatabase.CreateAsset(payload, plan.PayloadPath);
                createdPaths.Add(plan.PayloadPath);
                ThrowIfRequested(
                    failAfterStage,
                    AbilityProductionStage.PayloadCreated);

                if (request.CreateCommitEffect)
                {
                    EnsureParentFolder(plan.EffectPath, createdFolders);
                    effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
                    effect.name = System.IO.Path.GetFileNameWithoutExtension(
                        plan.EffectPath);
                    ConfigureEffect(effect, request);
                    Undo.RegisterCreatedObjectUndo(
                        effect,
                        "Gameplay Effect 생성");
                    AssetDatabase.CreateAsset(effect, plan.EffectPath);
                    createdPaths.Add(plan.EffectPath);
                    ThrowIfRequested(
                        failAfterStage,
                        AbilityProductionStage.EffectCreated);
                }

                ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
                ability.name = System.IO.Path.GetFileNameWithoutExtension(
                    plan.AbilityPath);
                ConfigureAbility(ability, payload, effect, request);
                Undo.RegisterCreatedObjectUndo(ability, "Gameplay Ability 생성");
                AssetDatabase.CreateAsset(ability, plan.AbilityPath);
                createdPaths.Add(plan.AbilityPath);
                ThrowIfRequested(
                    failAfterStage,
                    AbilityProductionStage.AbilityCreated);

                BindMotion(request, payload);

                Undo.RecordObject(request.TargetSet, "AbilitySet 연결");
                if (request.TargetSet.Contains(ability))
                    throw new InvalidOperationException(
                        "대상 AbilitySet에 같은 Ability가 이미 연결되어 있습니다.");
                BindAbility(request, ability);
                EditorUtility.SetDirty(request.TargetSet);
                ThrowIfRequested(
                    failAfterStage,
                    AbilityProductionStage.SetBound);

                EditorUtility.SetDirty(payload);
                EditorUtility.SetDirty(ability);
                AssetDatabase.SaveAssets();

                List<AbilityValidationIssue> issues =
                    AbilityDataValidator.Validate(ability);
                for (int i = 0; i < issues.Count; i++)
                {
                    if (issues[i].Severity == AbilityValidationSeverity.Error)
                        throw new InvalidOperationException(
                            $"생성 후 검증 실패: {issues[i].Message}");
                }

                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeObject = ability;
                EditorGUIUtility.PingObject(ability);
                return new AbilityProductionResult
                {
                    Success = true,
                    Message = "Ability, Payload, 액터 모션 매핑 및 AbilitySet 연결을 완료했습니다.",
                    Ability = ability,
                    Payload = payload,
                    Effect = effect,
                };
            }
            catch (Exception exception)
            {
                Rollback(undoGroup, createdPaths, createdFolders);
                return Failure($"Ability 생성 실패: {exception.Message}");
            }
        }

        private static void ConfigureAbility(
            GameplayAbilitySO ability,
            UPlayGroundMotionAbilityPayloadSO payload,
            GameplayEffectSO createdEffect,
            AbilityCreationRequest request)
        {
            AbilityRecipeDefinition recipe = request.Recipe;
            ability.abilityId = request.AbilityId.Trim();
            ability.presentation = new AbilityPresentationDefinition
            {
                displayName = request.DisplayName.Trim(),
                category = recipe.Category,
            };
            ability.activation = new AbilityActivationRules
            {
                groundCondition = recipe.GroundCondition,
                targetPolicy = recipe.TargetPolicy,
                targetRelation = recipe.TargetRelation,
                minDistance = request.MinDistance,
                maxDistance = request.MaxDistance,
            };
            ability.cost = new AbilityCostDefinition();
            ability.cooldown = new AbilityCooldownDefinition();
            ability.concurrency = recipe.Concurrency;
            ability.taskGraph = request.TaskGraph;
            ability.variants = new List<AbilityVariantDefinition>
            {
                new()
                {
                    variantId = DefaultVariantId,
                    priority = 0,
                    condition = new AbilityVariantCondition(),
                    executionPayload = payload,
                },
            };
            GameplayEffectSO commitEffect =
                createdEffect != null ? createdEffect : request.CommitEffect;
            ability.commitEffects = commitEffect == null
                ? new List<GameplayEffectSO>()
                : new List<GameplayEffectSO> { commitEffect };
            ability.endEffects = request.EndEffect == null
                ? new List<GameplayEffectSO>()
                : new List<GameplayEffectSO> { request.EndEffect };
            ability.persistence = new AbilityPersistencePolicy();
            ability.balance = new AbilityBalanceMetadata();
        }

        private static void ConfigureEffect(
            GameplayEffectSO effect,
            AbilityCreationRequest request)
        {
            effect.effectId = request.EffectId.Trim();
            effect.polarity = request.EffectPolarity;
            effect.presentation = new GameplayEffectPresentationDefinition();
            effect.durationType = request.EffectDurationType;
            effect.durationSeconds = request.EffectDurationSeconds;
            effect.modifiers = new List<GameplayEffectModifierDefinition>();
            if (!string.IsNullOrWhiteSpace(request.EffectAttributeId))
            {
                effect.modifiers.Add(new GameplayEffectModifierDefinition
                {
                    attributeId = request.EffectAttributeId.Trim(),
                    modifierType = request.EffectModifierType,
                    value = request.EffectModifierValue,
                });
            }
            effect.grantedTagIds = new List<
                UPlayGround.Gameplay.Tag.GameplayTag>();
        }

        private static void BindAbility(
            AbilityCreationRequest request,
            GameplayAbilitySO ability)
        {
            AbilitySetSO set = request.TargetSet;
            switch (request.BindingMode)
            {
                case AbilitySetBindingMode.AdditionalAbilities:
                    set.additionalAbilities ??= new List<GameplayAbilitySO>();
                    set.additionalAbilities.Add(ability);
                    break;
                case AbilitySetBindingMode.PlayerSkillSlot:
                    set.playerSlots ??= new List<AbilitySetSO.PlayerSlotEntry>();
                    AbilitySetSO.PlayerSlotEntry playerEntry =
                        set.playerSlots.Find(x =>
                            x != null && x.slot == request.PlayerSkillSlot);
                    if (playerEntry == null)
                    {
                        playerEntry = new AbilitySetSO.PlayerSlotEntry
                        {
                            slot = request.PlayerSkillSlot,
                        };
                        set.playerSlots.Add(playerEntry);
                    }
                    playerEntry.ability = ability;
                    break;
                case AbilitySetBindingMode.PlayerCombatSequence:
                    set.combatBindings ??=
                        new List<PlayerCombatAbilityBinding>();
                    PlayerCombatAbilityBinding combatEntry =
                        set.combatBindings.Find(x =>
                            x != null && x.slot == request.PlayerCombatSlot);
                    if (combatEntry == null)
                    {
                        combatEntry = new PlayerCombatAbilityBinding
                        {
                            slot = request.PlayerCombatSlot,
                        };
                        set.combatBindings.Add(combatEntry);
                    }
                    combatEntry.abilities ??= new List<GameplayAbilitySO>();
                    if (request.ReplaceExistingBinding)
                        combatEntry.abilities.Clear();
                    combatEntry.abilities.Add(ability);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ConfigurePayload(
            UPlayGroundMotionAbilityPayloadSO payload,
            AbilityCreationRequest request)
        {
            AbilityRecipeDefinition recipe = request.Recipe;
            payload.attackInfo = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase
                {
                    motionKey = new AbilityMotionKey(
                        request.AbilityId,
                        DefaultVariantId),
                    attackType = recipe.AttackType,
                    hitPhases = new List<HitPhaseData> { new() },
                },
                aiSelectable = recipe.AiSelectable,
                attackCategory = recipe.AttackCategory,
                requiredLevel = request.RequiredLevel,
                selectionWeight = request.SelectionWeight,
            };
            payload.attackInfo.baseInfo.hitPhases[0].targetingRange =
                request.MaxDistance;
        }

        private static void BindMotion(
            AbilityCreationRequest request,
            UPlayGroundMotionAbilityPayloadSO payload)
        {
            if (request.MotionOwner == null || request.Motion == null)
                throw new InvalidOperationException(
                    "Actor MotionSet 또는 Motion Asset이 없습니다.");

            AbilityMotionKey key = payload.attackInfo.baseInfo.motionKey;
            Undo.RecordObject(request.MotionOwner, "Ability Motion 연결");
            request.MotionOwner.abilityMotions ??=
                new SerializedDictionary<
                    AbilityMotionKey,
                    MotionSetAsset>();
            if (request.MotionOwner.abilityMotions.TryGetValue(
                    key,
                    out MotionSetAsset existing)
                && existing != request.Motion)
            {
                throw new InvalidOperationException(
                    $"Actor MotionSet에 같은 Key가 다른 모션으로 연결되어 있습니다: {key}");
            }

            request.MotionOwner.abilityMotions[key] = request.Motion;
            EditorUtility.SetDirty(request.MotionOwner);
        }

        private static void EnsureParentFolder(
            string assetPath,
            List<string> createdFolders)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath)
                ?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folder))
                throw new InvalidOperationException(
                    $"에셋 부모 경로를 해석할 수 없습니다: {assetPath}");

            EnsureFolder(folder, createdFolders);
        }

        private static void EnsureFolder(
            string folder,
            List<string> createdFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = System.IO.Path.GetDirectoryName(folder)
                ?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(parent)
                || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(
                    $"폴더 경로가 올바르지 않습니다: {folder}");

            EnsureFolder(parent, createdFolders);
            string guid = AssetDatabase.CreateFolder(parent, name);
            if (string.IsNullOrWhiteSpace(guid))
                throw new InvalidOperationException(
                    $"폴더를 생성하지 못했습니다: {folder}");
            createdFolders.Add(folder);
        }

        private static void Rollback(
            int undoGroup,
            List<string> createdPaths,
            List<string> createdFolders)
        {
            try
            {
                Undo.RevertAllDownToGroup(undoGroup);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            for (int i = createdPaths.Count - 1; i >= 0; i--)
            {
                if (AssetDatabase.LoadMainAssetAtPath(createdPaths[i]) != null)
                    AssetDatabase.DeleteAsset(createdPaths[i]);
            }
            for (int i = createdFolders.Count - 1; i >= 0; i--)
            {
                if (!AssetDatabase.IsValidFolder(createdFolders[i]))
                    continue;
                string[] guids = AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { createdFolders[i] });
                if (guids.Length == 0)
                    AssetDatabase.DeleteAsset(createdFolders[i]);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static AbilityProductionResult Failure(string message) =>
            new()
            {
                Success = false,
                Message = message,
            };

        private static void ThrowIfRequested(
            AbilityProductionStage? failAfterStage,
            AbilityProductionStage currentStage)
        {
            if (failAfterStage == currentStage)
                throw new InvalidOperationException(
                    $"테스트 실패 주입: {currentStage}");
        }
    }
}
