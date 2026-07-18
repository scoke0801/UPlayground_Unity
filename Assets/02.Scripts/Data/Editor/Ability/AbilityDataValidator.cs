using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Editor.Ability
{
    public enum AbilityValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct AbilityValidationIssue
    {
        public readonly AbilityValidationSeverity Severity;
        public readonly UnityEngine.Object Context;
        public readonly string Message;

        public AbilityValidationIssue(
            AbilityValidationSeverity severity,
            UnityEngine.Object context,
            string message)
        {
            Severity = severity;
            Context = context;
            Message = message;
        }
    }

    public static class AbilityDataValidator
    {
        public static List<AbilityValidationIssue> ValidateAll()
        {
            var issues = new List<AbilityValidationIssue>();
            var ids = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            string[] abilityGuids = AssetDatabase.FindAssets("t:GameplayAbilitySO");
            string[] effectGuids = AssetDatabase.FindAssets("t:GameplayEffectSO");
            string[] setGuids = AssetDatabase.FindAssets("t:AbilitySetSO");

            for (int i = 0; i < abilityGuids.Length; i++)
            {
                var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(
                    AssetDatabase.GUIDToAssetPath(abilityGuids[i]));
                ValidateAbility(ability, issues);
                ValidateUniqueId(ability?.abilityId, ability, "Ability", ids, issues);
            }

            for (int i = 0; i < effectGuids.Length; i++)
            {
                var effect = AssetDatabase.LoadAssetAtPath<GameplayEffectSO>(
                    AssetDatabase.GUIDToAssetPath(effectGuids[i]));
                ValidateEffect(effect, issues);
                ValidateUniqueId(effect?.effectId, effect, "Effect", ids, issues);
            }

            for (int i = 0; i < setGuids.Length; i++)
            {
                var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(
                    AssetDatabase.GUIDToAssetPath(setGuids[i]));
                ValidateSet(set, issues);
            }

            ValidateActorAbilityBindings(issues);

            return issues;
        }

        private static void ValidateActorAbilityBindings(
            List<AbilityValidationIssue> issues)
        {
            string[] profileGuids = AssetDatabase.FindAssets(
                $"t:{nameof(MonsterActorProfileSO)}");
            for (int i = 0; i < profileGuids.Length; i++)
            {
                MonsterActorProfileSO profile =
                    AssetDatabase.LoadAssetAtPath<MonsterActorProfileSO>(
                        AssetDatabase.GUIDToAssetPath(profileGuids[i]));
                if (profile.abilitySet == null)
                {
                    Error(profile, "공용 AbilitySet이 없습니다.", issues);
                    continue;
                }

                bool hasAiAttack = false;
                foreach (GameplayAbilitySO ability in profile.abilitySet.EnumerateAll())
                {
                    if (ability?.variants == null) continue;
                    for (int j = 0; j < ability.variants.Count; j++)
                    {
                        if (!UPlayGroundAbilityPayloadResolver.TryResolve(
                                ability.variants[j],
                                out _,
                                out AbilityAttackInfo attackInfo)
                            || !attackInfo.aiSelectable)
                            continue;
                        hasAiAttack = true;
                        break;
                    }
                }

                if (!hasAiAttack)
                    Error(profile, "BT가 선택할 공격 Ability가 없습니다.", issues);
            }
        }

        public static List<AbilityValidationIssue> Validate(UnityEngine.Object target)
        {
            var issues = new List<AbilityValidationIssue>();
            switch (target)
            {
                case GameplayAbilitySO ability:
                    ValidateAbility(ability, issues);
                    break;
                case GameplayEffectSO effect:
                    ValidateEffect(effect, issues);
                    break;
                case AbilitySetSO set:
                    ValidateSet(set, issues);
                    break;
            }
            return issues;
        }

        private static void ValidateAbility(
            GameplayAbilitySO ability,
            List<AbilityValidationIssue> issues)
        {
            if (ability == null) return;
            if (string.IsNullOrWhiteSpace(ability.abilityId))
                Error(ability, "abilityId가 비어 있습니다.", issues);
            if (ability.schemaVersion < 1)
                Error(ability, "schemaVersion은 1 이상이어야 합니다.", issues);
            if (ability.cost != null && ability.cost.value < 0f)
                Error(ability, "비용은 음수일 수 없습니다.", issues);
            if (ability.cooldown != null && ability.cooldown.durationSeconds < 0f)
                Error(ability, "쿨다운은 음수일 수 없습니다.", issues);
            if (ability.activation != null)
            {
                if (ability.activation.maxDistance > 0f
                    && ability.activation.minDistance > ability.activation.maxDistance)
                    Error(ability, "최소 대상 거리가 최대 대상 거리보다 큽니다.", issues);
                if (ability.activation.targetPolicy == AbilityTargetPolicy.None
                    && (ability.activation.minDistance > 0f
                        || ability.activation.maxDistance > 0f))
                    Warning(ability, "대상 정책이 None이지만 거리 조건이 설정되어 있습니다.", issues);
                if (ability.activation.targetRelation == AbilityTargetRelation.Self
                    && ability.activation.minDistance > 0f)
                    Error(ability, "Self 대상 Ability의 최소 거리는 0이어야 합니다.", issues);
            }
            if (ability.cost != null)
            {
                bool consumes = ability.cost.policy != AbilityCostPolicy.None;
                if (consumes && ability.cost.resourceType == AbilityResourceType.None)
                    Error(ability, "비용 정책이 있지만 자원 종류가 None입니다.", issues);
                if (!consumes && ability.cost.value > 0f)
                    Warning(ability, "비용 정책이 None이므로 입력한 비용 값은 사용되지 않습니다.", issues);
            }
            if (ability.variants == null || ability.variants.Count == 0)
            {
                Error(ability, "실행 가능한 Variant가 없습니다.", issues);
                return;
            }

            int executableCount = 0;
            for (int i = 0; i < ability.variants.Count; i++)
            {
                AbilityVariantDefinition variant = ability.variants[i];
                if (variant == null)
                {
                    Error(ability, $"Variant {i}가 null입니다.", issues);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(variant.variantId))
                    Error(ability, $"Variant {i}의 ID가 비어 있습니다.", issues);
                bool executable = UPlayGroundAbilityPayloadResolver.TryResolveAnimKey(
                    variant, out AnimKey animKey);
                if (animKey == AnimKey.None)
                    Error(ability, $"Variant '{variant.variantId}'의 AnimKey가 None입니다.", issues);
                if (!executable)
                    Error(ability, $"Variant '{variant.variantId}'의 실행 Payload가 없습니다.", issues);
                if (executable) executableCount++;
                ValidateEffectReferences(
                    variant.ownerEffects,
                    ability,
                    $"Variant '{variant.variantId}' owner Effect",
                    issues);
                ValidateEffectReferences(
                    variant.targetEffects,
                    ability,
                    $"Variant '{variant.variantId}' target Effect",
                    issues);

                for (int j = i + 1; j < ability.variants.Count; j++)
                {
                    AbilityVariantDefinition other = ability.variants[j];
                    if (other != null
                        && variant.priority == other.priority
                        && ConditionsEqual(variant.condition, other.condition))
                    {
                        Warning(ability,
                            $"Variant '{variant.variantId}'와 '{other.variantId}'의 조건/우선순위가 같습니다.",
                            issues);
                    }
                }
            }

            if (executableCount == 0)
                Error(ability, "실행 가능한 Variant가 하나도 없습니다.", issues);
            if (ability.presentation?.icon == null)
                Warning(ability, "표시 아이콘이 없습니다.", issues);
            if (string.IsNullOrWhiteSpace(ability.presentation?.nameLocalizationKey))
                Info(ability, "이름 로컬라이즈 키가 없습니다.", issues);
            ValidateTagList(ability.activation?.requiredTagIds, ability, "Required", issues);
            ValidateTagList(ability.activation?.blockedTagIds, ability, "Blocked", issues);
            ValidateTagList(ability.activation?.executionGrantedTagIds, ability, "Granted", issues);
            ValidateEffectReferences(
                ability.commitEffects, ability, "Commit Effect", issues);
            ValidateEffectReferences(
                ability.endEffects, ability, "End Effect", issues);
        }

        private static void ValidateEffect(
            GameplayEffectSO effect,
            List<AbilityValidationIssue> issues)
        {
            if (effect == null) return;
            if (string.IsNullOrWhiteSpace(effect.effectId))
                Error(effect, "effectId가 비어 있습니다.", issues);
            if (effect.schemaVersion < 1)
                Error(effect, "schemaVersion은 1 이상이어야 합니다.", issues);
            if (effect.durationSeconds < 0f || effect.periodSeconds < 0f)
                Error(effect, "지속/주기 시간은 음수일 수 없습니다.", issues);
            if (effect.durationType == GameplayEffectDurationType.Duration
                && effect.durationSeconds <= 0f)
                Error(effect, "Duration Effect의 지속 시간은 0보다 커야 합니다.", issues);
            if (effect.IsPeriodic && effect.periodSeconds <= 0f)
                Error(effect, "주기 Effect의 periodSeconds는 0보다 커야 합니다.", issues);
            if (effect.maxStackCount < 1)
                Error(effect, "maxStackCount는 1 이상이어야 합니다.", issues);
            if (effect.stackPolicy != GameplayEffectStackPolicy.RejectNew
                && string.IsNullOrWhiteSpace(effect.stackingKey))
                Error(effect, "중첩 정책을 사용하는 Effect에는 stackingKey가 필요합니다.", issues);
            if (effect.durationType == GameplayEffectDurationType.Duration
                && effect.removalPolicy == GameplayEffectRemovalPolicy.RemoveOnSwap)
                Info(effect, "교체 시 제거되는 Duration Effect입니다.", issues);
            if (effect.resourceOperations != null)
            {
                for (int i = 0; i < effect.resourceOperations.Count; i++)
                {
                    GameplayResourceOperation operation = effect.resourceOperations[i];
                    if (operation == null) continue;
                    if (operation.resourceType == AbilityResourceType.Health
                        && operation.magnitude < 0f)
                    {
                        Error(
                            effect,
                            $"Health 자원 연산 {i}번의 음수 값은 사용할 수 없습니다. "
                            + "피해는 방어·사망·텔레메트리를 보존하는 Combat Damage Effect로 정의해야 합니다.",
                            issues);
                    }
                }
            }
            if (effect.modifiers != null)
            {
                for (int i = 0; i < effect.modifiers.Count; i++)
                    if (effect.modifiers[i] == null)
                        Error(effect, $"Modifier {i}번이 null입니다.", issues);
            }
            if (effect.resourceOperations != null)
            {
                for (int i = 0; i < effect.resourceOperations.Count; i++)
                    if (effect.resourceOperations[i] == null)
                        Error(effect, $"자원 연산 {i}번이 null입니다.", issues);
            }
            ValidateTagList(effect.grantedTagIds, effect, "Granted", issues);
        }

        private static void ValidateSet(
            AbilitySetSO set,
            List<AbilityValidationIssue> issues)
        {
            if (set == null) return;
            var seenSlots = new HashSet<Data.Combat.PlayerSkillSlot>();
            for (int i = 0; i < (set.playerSlots?.Count ?? 0); i++)
            {
                AbilitySetSO.PlayerSlotEntry entry = set.playerSlots[i];
                if (entry == null || entry.ability == null)
                    Error(set, $"플레이어 슬롯 {i}의 Ability 참조가 없습니다.", issues);
                else if (!seenSlots.Add(entry.slot))
                    Error(set, $"'{entry.slot}' 슬롯이 중복되었습니다.", issues);
            }
            for (int i = 0; i < (set.additionalAbilities?.Count ?? 0); i++)
                if (set.additionalAbilities[i] == null)
                    Error(set, $"추가 Ability {i}번 참조가 없습니다.", issues);

            var seenCombatSlots = new HashSet<PlayerCombatAbilitySlot>();
            for (int i = 0; i < (set.combatBindings?.Count ?? 0); i++)
            {
                PlayerCombatAbilityBinding binding = set.combatBindings[i];
                if (binding == null)
                {
                    Error(set, $"전투 슬롯 {i}가 null입니다.", issues);
                    continue;
                }
                if (!seenCombatSlots.Add(binding.slot))
                    Error(set, $"전투 슬롯 '{binding.slot}'이 중복되었습니다.", issues);
                if (binding.abilities == null || binding.abilities.Count == 0)
                    Error(set, $"전투 슬롯 '{binding.slot}'에 Ability가 없습니다.", issues);
                else
                    for (int j = 0; j < binding.abilities.Count; j++)
                        if (binding.abilities[j] == null)
                            Error(set, $"전투 슬롯 '{binding.slot}' {j}번 참조가 없습니다.", issues);
            }

            int stageCount = set.charge?.stages?.Count ?? 0;
            int thresholdCount = set.charge?.stageThresholds?.Count ?? 0;
            if (thresholdCount > 0 && thresholdCount != Mathf.Max(0, stageCount - 1))
                Error(
                    set,
                    $"차지 임계값 수({thresholdCount})는 단계 수 - 1({Mathf.Max(0, stageCount - 1)})이어야 합니다.",
                    issues);
            for (int i = 0; i < stageCount; i++)
                if (set.charge.stages[i] == null)
                    Error(set, $"차지 단계 {i}의 Ability 참조가 없습니다.", issues);

            for (int i = 0; i < (set.comboRoutes?.Count ?? 0); i++)
            {
                AbilityComboRouteDefinition route = set.comboRoutes[i];
                if (route == null)
                    Error(set, $"연계 라우트 {i}가 null입니다.", issues);
                else if (route.ability == null)
                    Error(set, $"연계 라우트 '{route.DisplayLabel}'의 Ability 참조가 없습니다.", issues);
                else if (route.IsEmpty)
                    Warning(set, $"연계 라우트 '{route.DisplayLabel}'의 입력 패턴이 비어 있습니다.", issues);
            }
        }

        private static void ValidateEffectReferences(
            List<GameplayEffectSO> effects,
            UnityEngine.Object context,
            string label,
            List<AbilityValidationIssue> issues)
        {
            if (effects == null)
            {
                Error(context, $"{label} 목록이 null입니다.", issues);
                return;
            }
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] == null)
                    Error(context, $"{label} {i}번 참조가 없습니다.", issues);
        }

        private static void ValidateTagList(
            List<GameplayTagId> tags,
            UnityEngine.Object context,
            string label,
            List<AbilityValidationIssue> issues)
        {
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
                if (tags[i] == GameplayTagId.None)
                    Warning(context, $"{label} 태그 목록 {i}번이 None입니다.", issues);
        }

        private static void ValidateUniqueId(
            string id,
            UnityEngine.Object context,
            string label,
            Dictionary<string, UnityEngine.Object> ids,
            List<AbilityValidationIssue> issues)
        {
            if (context == null || string.IsNullOrWhiteSpace(id)) return;
            if (ids.TryGetValue(id.Trim(), out UnityEngine.Object existing))
            {
                Error(context, $"{label} ID '{id}'가 '{existing.name}'과 중복됩니다.", issues);
                Error(existing, $"{label} ID '{id}'가 '{context.name}'과 중복됩니다.", issues);
            }
            else
            {
                ids.Add(id.Trim(), context);
            }
        }

        private static bool ConditionsEqual(
            AbilityVariantCondition a,
            AbilityVariantCondition b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            return a.groundCondition == b.groundCondition
                   && UnityEngine.Mathf.Approximately(a.minResource, b.minResource)
                   && a.requiresFullResource == b.requiresFullResource
                   && ListsEqual(a.requiredTagIds, b.requiredTagIds)
                   && ListsEqual(a.blockedTagIds, b.blockedTagIds);
        }

        private static bool ListsEqual<T>(List<T> a, List<T> b)
        {
            int ac = a?.Count ?? 0;
            int bc = b?.Count ?? 0;
            if (ac != bc) return false;
            for (int i = 0; i < ac; i++)
                if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) return false;
            return true;
        }

        private static void Error(
            UnityEngine.Object context,
            string message,
            List<AbilityValidationIssue> issues) =>
            issues.Add(new AbilityValidationIssue(
                AbilityValidationSeverity.Error, context, message));

        private static void Warning(
            UnityEngine.Object context,
            string message,
            List<AbilityValidationIssue> issues) =>
            issues.Add(new AbilityValidationIssue(
                AbilityValidationSeverity.Warning, context, message));

        private static void Info(
            UnityEngine.Object context,
            string message,
            List<AbilityValidationIssue> issues) =>
            issues.Add(new AbilityValidationIssue(
                AbilityValidationSeverity.Info, context, message));
    }
}
