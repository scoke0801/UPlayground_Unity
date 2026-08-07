using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
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
        private const string CounterAbilityIdSegment = ".Counter.";

        public static List<AbilityValidationIssue> ValidateAll()
        {
            var issues = new List<AbilityValidationIssue>();
            var ids = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            List<GameplayAbilitySO> abilities =
                LoadAssetsIncludingSubAssets<GameplayAbilitySO>();
            List<GameplayEffectSO> effects =
                LoadAssetsIncludingSubAssets<GameplayEffectSO>();
            List<AbilitySetSO> sets =
                LoadAssetsIncludingSubAssets<AbilitySetSO>();
            List<PassiveAbilitySO> passives =
                LoadAssetsIncludingSubAssets<PassiveAbilitySO>();
            List<CharacterPassiveSetSO> passiveSets =
                LoadAssetsIncludingSubAssets<CharacterPassiveSetSO>();
            List<CharacterPassiveDatabaseSO> passiveDatabases =
                LoadAssetsIncludingSubAssets<CharacterPassiveDatabaseSO>();

            // Motion Key 역인덱스는 프로젝트 전체 스캔이므로 전수 검증 1회당 한 번만 만든다.
            var motionIndex = new AbilityMotionIndex();
            var motionEventCache =
                new Dictionary<MotionSetAsset, IReadOnlyList<MotionEventBase>>();
            for (int i = 0; i < abilities.Count; i++)
            {
                GameplayAbilitySO ability = abilities[i];
                ValidateAbility(ability, issues, motionIndex, motionEventCache);
                ValidateUniqueId(ability?.abilityId, ability, "Ability", ids, issues);
            }

            for (int i = 0; i < effects.Count; i++)
            {
                GameplayEffectSO effect = effects[i];
                ValidateEffect(effect, issues);
                ValidateUniqueId(effect?.effectId, effect, "Effect", ids, issues);
            }

            for (int i = 0; i < sets.Count; i++)
            {
                ValidateSet(sets[i], issues);
            }

            for (int i = 0; i < passives.Count; i++)
            {
                PassiveAbilitySO passive = passives[i];
                ValidatePassive(passive, issues);
                ValidateUniqueId(
                    passive?.passiveId, passive, "Passive", ids, issues);
            }

            for (int i = 0; i < passiveSets.Count; i++)
                ValidatePassiveSet(passiveSets[i], issues);

            for (int i = 0; i < passiveDatabases.Count; i++)
                ValidatePassiveDatabase(passiveDatabases[i], issues);

            ValidateActorAbilityBindings(issues);

            return issues;
        }

        private static List<T> LoadAssetsIncludingSubAssets<T>()
            where T : UnityEngine.Object
        {
            var result = new List<T>();
            var seen = new HashSet<int>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));

            if (typeof(T) == typeof(PassiveAbilitySO)
                || typeof(T) == typeof(CharacterPassiveSetSO)
                || typeof(T) == typeof(GameplayEffectSO))
            {
                string[] databaseGuids =
                    AssetDatabase.FindAssets($"t:{nameof(CharacterPassiveDatabaseSO)}");
                for (int i = 0; i < databaseGuids.Length; i++)
                    paths.Add(AssetDatabase.GUIDToAssetPath(databaseGuids[i]));
            }

            foreach (string path in paths)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is T asset && seen.Add(asset.GetInstanceID()))
                        result.Add(asset);
                }
            }
            return result;
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
                        if (ability.variants[j]?.executionPayload
                            is not UPlayGroundMotionAbilityPayloadSO payload)
                            continue;

                        // EnemyCombat.GetAvailableAbilities와 같은 술어를 쓴다.
                        if (!payload.IsAttackExecutable
                            || payload.attackInfo is not AbilityAttackInfo attackInfo
                            || !attackInfo.aiSelectable
                            || attackInfo.baseInfo?.HasHitPhases != true)
                            continue;
                        hasAiAttack = true;
                    }
                }

                if (!hasAiAttack)
                    Error(profile, "BT가 선택할 공격 Ability가 없습니다.", issues);
            }
        }

        /// <summary>
        /// 히트 페이즈 유무와 Ability 분류가 어긋나는지 본다.
        /// HasHitPhases가 "공격인가"의 권위 술어이므로(런타임 BT 선택도 이 값을 쓴다)
        /// 분류와 불일치하면 실행되지 않거나 의도 없이 피해를 주는 Ability가 된다.
        /// </summary>
        private static void ValidateHitPhaseCategoryConsistency(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            UPlayGroundMotionAbilityPayloadSO payload,
            List<AbilityValidationIssue> issues)
        {
            AttackInfoBase baseInfo = payload.attackInfo?.baseInfo;
            if (baseInfo == null) return;

            AbilityCategory category =
                ability.presentation?.category ?? AbilityCategory.Attack;
            bool hasPhases = baseInfo.HasHitPhases;

            if (!hasPhases
                && category is AbilityCategory.Attack or AbilityCategory.Ultimate)
            {
                Error(ability,
                    $"분류가 {category}인데 Variant '{variant.variantId}'에 HitPhase가 없습니다. "
                    + "히트 판정이 발생하지 않고 몬스터 BT의 공격 선택 대상에서도 제외됩니다. "
                    + "HitPhase를 추가하거나 분류를 Support로 바꾸세요.",
                    issues);
                return;
            }

            if (hasPhases
                && category is AbilityCategory.Support or AbilityCategory.Passive)
            {
                Warning(ability,
                    $"분류가 {category}인데 Variant '{variant.variantId}'에 HitPhase가 "
                    + $"{baseInfo.hitPhases.Count}개 있습니다. 모션에 히트 이벤트가 있으면 피해를 줍니다.",
                    issues);
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
                case PassiveAbilitySO passive:
                    ValidatePassive(passive, issues);
                    break;
                case CharacterPassiveSetSO passiveSet:
                    ValidatePassiveSet(passiveSet, issues);
                    break;
                case CharacterPassiveDatabaseSO database:
                    ValidatePassiveDatabase(database, issues);
                    break;
            }
            return issues;
        }

        /// <summary>
        /// motionIndex는 전수 검증(ValidateAll)에서만 넘긴다. 단일 에셋 검증은 값이 바뀔 때마다
        /// 실행되므로, 프로젝트 전체를 스캔하는 역인덱스를 여기서 만들면 입력마다 스캔이 돈다.
        /// </summary>
        private static void ValidateAbility(
            GameplayAbilitySO ability,
            List<AbilityValidationIssue> issues,
            AbilityMotionIndex motionIndex = null,
            Dictionary<MotionSetAsset, IReadOnlyList<MotionEventBase>>
                motionEventCache = null)
        {
            if (ability == null) return;
            if (string.IsNullOrWhiteSpace(ability.abilityId))
                Error(ability, "abilityId가 비어 있습니다.", issues);
            if (ability.cost != null && ability.cost.value < 0f)
                Error(ability, "비용은 음수일 수 없습니다.", issues);
            if (ability.cooldown != null && ability.cooldown.durationSeconds < 0f)
                Error(ability, "쿨다운은 음수일 수 없습니다.", issues);
            bool requestDriven = IsRequestDrivenAbility(ability);
            if (ability.taskGraph?.Root == null && !requestDriven)
                Error(ability, "실행 Task Graph 또는 Root Task가 없습니다.", issues);
            if (ability.activation != null)
            {
                ValidateTagRequirement(
                    ability.activation.ownerTagRequirement,
                    ability,
                    "Owner 활성화",
                    issues);
                ValidateTagRequirement(
                    ability.activation.sourceTagRequirement,
                    ability,
                    "Source 활성화",
                    issues);
                ValidateTagRequirement(
                    ability.activation.targetTagRequirement,
                    ability,
                    "Target 활성화",
                    issues);
                if (!IsEmpty(ability.activation.requiredTagIds)
                    && ability.activation.ownerTagRequirement?.IsEmpty == false)
                {
                    Warning(
                        ability,
                        "레거시 requiredTagIds와 ownerTagRequirement가 함께 설정되어 있습니다.",
                        issues);
                }
                if (!IsEmpty(ability.activation.blockedTagIds)
                    && ability.activation.ownerTagRequirement?.IsEmpty == false)
                {
                    Warning(
                        ability,
                        "레거시 blockedTagIds와 ownerTagRequirement가 함께 설정되어 있습니다.",
                        issues);
                }
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
            ValidateAbilityTriggers(ability, issues);
            ValidateTagList(ability.abilityTagIds, ability, "Ability", issues);
            ValidateTagList(
                ability.cancelAbilitiesWithTag,
                ability,
                "Cancel Ability",
                issues);
            ValidateTagList(
                ability.blockAbilitiesWithTag,
                ability,
                "Block Ability",
                issues);
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
                ValidateTagRequirement(
                    variant.condition?.ownerTagRequirement,
                    ability,
                    $"Variant '{variant.variantId}' Owner",
                    issues);
                if (variant.condition != null
                    && (!IsEmpty(variant.condition.requiredTagIds)
                        || !IsEmpty(variant.condition.blockedTagIds))
                    && variant.condition.ownerTagRequirement?.IsEmpty == false)
                {
                    Warning(
                        ability,
                        $"Variant '{variant.variantId}'에 레거시 태그 조건과 ownerTagRequirement가 함께 설정되어 있습니다.",
                        issues);
                }
                bool executable = requestDriven
                    || UPlayGroundAbilityPayloadResolver.IsExecutable(variant);
                if (variant.executionPayload is UPlayGroundMotionAbilityPayloadSO payload)
                {
                    if (payload.attackInfo == null)
                        Error(ability,
                            $"Variant '{variant.variantId}'의 실행 정보가 없습니다.", issues);
                    else
                    {
                        // Motion Key는 공격 수치(baseInfo)와 분리된 형제 필드이므로
                        // baseInfo 유무와 무관하게 검증한다.
                        if (payload.attackInfo.baseInfo == null)
                            Error(ability,
                                $"Variant '{variant.variantId}'의 공격 정보가 없습니다.", issues);

                        MotionKey actual =
                            payload.attackInfo.motionKey;
                        if (!actual.IsValid)
                            Error(
                                ability,
                                $"Variant '{variant.variantId}'의 Motion Key가 없습니다.",
                                issues);
                        else if (motionIndex != null
                                 && motionIndex.Candidates(actual).Count == 0)
                            // Key가 Ability에서 독립되면서 오타를 구조적으로 막을 수단이 없어졌다.
                            // 콘텐츠 모션 확정 대기 중인 키가 정상적으로 존재하므로 Error가 아닌
                            // Warning으로 보고한다.
                            Warning(
                                ability,
                                $"Variant '{variant.variantId}'의 Motion Key "
                                + $"'{actual}'가 어떤 ActorAnimationMotionSet에서도 "
                                + "해석되지 않습니다.",
                                issues);

                        ValidateAiAttackDefinition(
                            ability,
                            variant,
                            payload.attackInfo,
                            motionIndex,
                            motionEventCache,
                            issues);
                        ValidateSkillConditionGroup(
                            ability,
                            variant,
                            payload.attackInfo.conditionGroup,
                            issues);
                    }
                    ValidateHitPhaseCategoryConsistency(ability, variant, payload, issues);
                }
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

                // ResolveVariant는 조건을 통과한 후보 중 priority 최댓값만 고른다.
                // 조건이 완전히 같고 priority가 더 높은 Variant가 있으면 이 Variant는 영원히 선택되지 않는다.
                for (int j = 0; j < ability.variants.Count; j++)
                {
                    if (j == i) continue;
                    AbilityVariantDefinition other = ability.variants[j];
                    if (other == null
                        || other.priority <= variant.priority
                        || !ConditionsEqual(variant.condition, other.condition))
                        continue;

                    Warning(ability,
                        $"Variant '{variant.variantId}'(priority {variant.priority})는 "
                        + $"조건이 같고 우선순위가 높은 '{other.variantId}'(priority {other.priority})에 "
                        + "항상 밀려 선택되지 않습니다. 조건을 구분하거나 별도 Ability로 분리하세요.",
                        issues);
                    break;
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
            if (effect.presentation == null)
            {
                Error(effect, "Effect 표시 데이터가 없습니다.", issues);
            }
            else
            {
                bool canRemainActive =
                    effect.durationType != GameplayEffectDurationType.Instant;
                if (canRemainActive
                    && effect.presentation.showInHud
                    && effect.presentation.icon == null)
                {
                    Warning(effect, "HUD 표시 Effect에 아이콘이 없어 fallback 아이콘을 사용합니다.", issues);
                }

                if (canRemainActive
                    && effect.presentation.showInHud
                    && string.IsNullOrWhiteSpace(effect.presentation.displayName)
                    && string.IsNullOrWhiteSpace(effect.presentation.nameLocalizationKey))
                {
                    Warning(effect, "HUD 표시 Effect의 이름과 현지화 키가 모두 비어 있습니다.", issues);
                }

                if (!canRemainActive && effect.presentation.showInHud)
                    Info(effect, "Instant Effect는 HUD 노출 설정과 관계없이 아이콘을 표시하지 않습니다.", issues);
                if (effect.durationType == GameplayEffectDurationType.Infinite
                    && effect.presentation.showRemainingTime)
                {
                    Info(effect, "Infinite Effect의 남은 시간 표시는 무시됩니다.", issues);
                }
                if (effect.maxStackCount == 1 && effect.presentation.showStackCount)
                    Info(effect, "단일 스택 Effect의 스택 수 표시는 사용되지 않습니다.", issues);
            }
            if (effect.durationType == GameplayEffectDurationType.Duration
                && effect.removalPolicy == GameplayEffectRemovalPolicy.RemoveOnSwap)
                Info(effect, "교체 시 제거되는 Duration Effect입니다.", issues);
            if (effect.modifiers != null)
            {
                for (int i = 0; i < effect.modifiers.Count; i++)
                {
                    GameplayEffectModifierDefinition modifier = effect.modifiers[i];
                    if (modifier == null)
                    {
                        Error(effect, $"Modifier {i}번이 null입니다.", issues);
                        continue;
                    }
                    if (!modifier.AttributeId.IsValid)
                    {
                        Error(
                            effect,
                            $"Modifier {i}번 Attribute ID가 비어 있습니다.",
                            issues);
                        continue;
                    }
                    ValidateModifierMagnitude(effect, modifier, i, issues);
                }
                if (effect.modifiers.Count > 0
                    && effect.durationType == GameplayEffectDurationType.Instant)
                {
                    Warning(
                        effect,
                        "Instant Effect의 Modifier는 적용되지 않습니다. "
                        + "즉시 수치 변경은 Execution 경로를 사용하세요.",
                        issues);
                }
            }
            ValidateTagList(effect.grantedTagIds, effect, "Granted", issues);
        }

        private static void ValidateAiAttackDefinition(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            AbilityAttackInfo attackInfo,
            AbilityMotionIndex motionIndex,
            Dictionary<MotionSetAsset, IReadOnlyList<MotionEventBase>>
                motionEventCache,
            List<AbilityValidationIssue> issues)
        {
            if (attackInfo?.aiSelectable != true)
                return;

            if (attackInfo.attackCategory == AbilityAttackCategory.None)
            {
                Error(
                    ability,
                    $"Variant '{variant.variantId}'가 aiSelectable이지만 "
                    + "공격 카테고리가 None입니다. 구체 카테고리 또는 명시적 "
                    + "와일드카드 Any를 지정하세요.",
                    issues);
            }

            if (ability.abilityId?.IndexOf(
                    CounterAbilityIdSegment,
                    StringComparison.Ordinal) >= 0
                && (attackInfo.aiRoles & AbilityAIRole.Counter) == 0)
            {
                Error(
                    ability,
                    $"Counter Ability Variant '{variant.variantId}'에 "
                    + "Counter AI 역할이 없습니다. AbilityAIRole.Counter를 "
                    + "지정하세요.",
                    issues);
            }

            if (IsBossAbilityAsset(ability)
                && attackInfo.aiRoles == AbilityAIRole.None)
            {
                Error(
                    ability,
                    $"보스 Ability Variant '{variant.variantId}'에 AI 역할이 없습니다. "
                    + "Opener, Punish, GapCloser, Counter, Signature, Finisher 중 "
                    + "하나 이상을 지정하세요.",
                    issues);
            }

            if (motionIndex == null
                || motionEventCache == null
                || !attackInfo.motionKey.IsValid)
                return;

            IReadOnlyList<MotionSetAsset> candidates =
                motionIndex.Candidates(attackInfo.motionKey);
            var reported = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < candidates.Count; i++)
            {
                MotionSetAsset motionAsset = candidates[i];
                if (motionAsset?.motionSet == null)
                    continue;

                IReadOnlyList<MotionEventBase> events = GetAllMotionEvents(
                    motionAsset,
                    motionEventCache);
                for (int j = 0; j < events.Count; j++)
                {
                    MotionEventBase motionEvent = events[j];
                    if (motionEvent == null
                        || motionEvent.EnemyExecutionPolicy
                            != MotionEventEnemyExecutionPolicy.Forbidden)
                        continue;

                    string key = $"{motionAsset.GetInstanceID()}:{motionEvent.GetType().FullName}";
                    if (!reported.Add(key))
                        continue;
                    Error(
                        ability,
                        $"Variant '{variant.variantId}'의 Motion "
                        + $"'{motionAsset.name}'에 몬스터 실행이 금지된 "
                        + $"{motionEvent.GetType().Name} 이벤트가 있습니다. "
                        + "적 실행 무시 옵션을 켜거나 보스 공격 풀에서 제외하세요.",
                        issues);
                }
            }
        }

        private static bool IsBossAbilityAsset(GameplayAbilitySO ability)
        {
            string path = AssetDatabase.GetAssetPath(ability);
            return !string.IsNullOrEmpty(path)
                   && path.Replace('\\', '/').IndexOf(
                       "/Ability/Actor/Boss/",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<MotionEventBase> GetAllMotionEvents(
            MotionSetAsset motionAsset,
            Dictionary<MotionSetAsset, IReadOnlyList<MotionEventBase>> cache)
        {
            if (cache.TryGetValue(motionAsset, out IReadOnlyList<MotionEventBase> events))
                return events;

            var result = new List<MotionEventBase>();
            MotionSet motionSet = motionAsset.motionSet;
            AddMotionEvents(motionSet.globalEvents, result);
            AddTimelineMotionEvents(motionSet.motions, result);
            for (int i = 0; i < (motionSet.layers?.Count ?? 0); i++)
            {
                MotionLayer layer = motionSet.layers[i];
                if (layer == null || !layer.enabled)
                    continue;
                AddMotionEvents(layer.globalEvents, result);
                AddTimelineMotionEvents(layer.motions, result);
            }

            cache.Add(motionAsset, result);
            return result;
        }

        private static void AddTimelineMotionEvents(
            List<UPlayGround.Animation.Motion> motions,
            List<MotionEventBase> destination)
        {
            for (int i = 0; i < (motions?.Count ?? 0); i++)
                AddMotionEvents(motions[i]?.events, destination);
        }

        private static void AddMotionEvents(
            List<MotionEventBase> source,
            List<MotionEventBase> destination)
        {
            for (int i = 0; i < (source?.Count ?? 0); i++)
                if (source[i] != null)
                    destination.Add(source[i]);
        }

        private static void ValidateSkillConditionGroup(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant,
            SkillConditionGroup group,
            List<AbilityValidationIssue> issues)
        {
            if (group?.conditions == null)
                return;

            for (int i = 0; i < group.conditions.Count; i++)
            {
                SkillCondition condition = group.conditions[i];
                if (condition == null)
                {
                    Error(
                        ability,
                        $"Variant '{variant.variantId}'의 AI 조건 {i}가 null입니다.",
                        issues);
                    continue;
                }

                // TargetHealthBased는 현재 런타임에서 HasTarget만 검사하고 HP 범위를
                // 소비하지 않으므로, 구현되기 전까지 경계 모순 검사 대상이 아니다.
                if (condition.type is not ConditionType.SelfHealthBased
                    and not ConditionType.InjuredAllyNearby)
                    continue;

                if (condition.minHealthPercent > condition.maxHealthPercent)
                {
                    Error(
                        ability,
                        $"Variant '{variant.variantId}'의 AI 조건 {i}에서 최소 HP가 "
                        + "최대 HP보다 큽니다.",
                        issues);
                }
                else if (Mathf.Approximately(
                             condition.minHealthPercent,
                             condition.maxHealthPercent)
                         && (!condition.includeMinHealth
                             || !condition.includeMaxHealth))
                {
                    Error(
                        ability,
                        $"Variant '{variant.variantId}'의 AI 조건 {i}는 같은 HP 경계에서 "
                        + "한쪽을 제외해 만족 가능한 값이 없습니다.",
                        issues);
                }
            }
        }

        private static void ValidateSet(
            AbilitySetSO set,
            List<AbilityValidationIssue> issues)
        {
            if (set == null) return;
            if (set.HasInheritanceCycle())
                Error(set, "AbilitySet의 Base Set 참조에 순환이 있습니다.", issues);
            if (ReferenceEquals(set.baseSet, set))
                Error(set, "AbilitySet은 자기 자신을 Base Set으로 사용할 수 없습니다.", issues);

            var seenOverrideSources = new HashSet<GameplayAbilitySO>();
            for (int i = 0; i < (set.abilityOverrides?.Count ?? 0); i++)
            {
                AbilitySetSO.AbilityOverrideEntry entry =
                    set.abilityOverrides[i];
                if (entry == null)
                {
                    Error(set, $"Ability Override {i}가 null입니다.", issues);
                    continue;
                }
                if (set.baseSet == null)
                    Error(set, "Base Set이 없지만 Ability Override가 정의되어 있습니다.", issues);
                if (entry.sourceAbility == null)
                {
                    Error(set, $"Ability Override {i}의 원본 Ability가 없습니다.", issues);
                    continue;
                }
                if (!seenOverrideSources.Add(entry.sourceAbility))
                    Error(
                        set,
                        $"'{entry.sourceAbility.name}' 원본에 Override가 중복 선언되었습니다.",
                        issues);
                if (set.baseSet != null
                    && !set.baseSet.Contains(entry.sourceAbility))
                {
                    Error(
                        set,
                        $"Override 원본 '{entry.sourceAbility.name}'은 Base Set의 유효 Ability가 아닙니다.",
                        issues);
                }
                if (entry.operation == AbilitySetOverrideOperation.Replace
                    && entry.replacementAbility == null)
                {
                    Error(
                        set,
                        $"'{entry.sourceAbility.name}' Replace 대상이 없습니다.",
                        issues);
                }
                if (entry.operation == AbilitySetOverrideOperation.Remove
                    && entry.replacementAbility != null)
                {
                    Warning(
                        set,
                        $"'{entry.sourceAbility.name}' Remove의 replacementAbility는 사용되지 않습니다.",
                        issues);
                }
                if (ReferenceEquals(
                        entry.sourceAbility,
                        entry.replacementAbility))
                {
                    Warning(
                        set,
                        $"'{entry.sourceAbility.name}'을 같은 Ability로 교체하고 있습니다.",
                        issues);
                }
            }

            var seenSlots = new HashSet<Data.Combat.PlayerSkillSlot>();
            for (int i = 0; i < (set.playerSlots?.Count ?? 0); i++)
            {
                AbilitySetSO.PlayerSlotEntry entry = set.playerSlots[i];
                if (entry == null || entry.ability == null)
                    Error(set, $"플레이어 슬롯 {i}의 Ability 참조가 없습니다.", issues);
                else if (!seenSlots.Add(entry.slot))
                    Error(set, $"'{entry.slot}' 슬롯이 중복되었습니다.", issues);
                if (entry?.slot == Data.Combat.PlayerSkillSlot.ElementalImbue
                    && (entry.ability?.triggers?.Count ?? 0) > 0)
                {
                    Error(
                        set,
                        $"ElementalImbue 슬롯 Ability '{entry.ability.name}'에는 트리거를 설정할 수 없습니다.",
                        issues);
                }
                else if ((entry?.ability?.triggers?.Count ?? 0) > 0)
                {
                    Error(
                        set,
                        $"입력 슬롯 '{entry.slot}' Ability '{entry.ability.name}'에 트리거가 함께 설정되어 입력 1회가 중복 실행될 수 있습니다.",
                        issues);
                }
            }
            for (int i = 0; i < (set.additionalAbilities?.Count ?? 0); i++)
                if (set.additionalAbilities[i] == null)
                    Error(set, $"추가 Ability {i}번 참조가 없습니다.", issues);

            // Request 전용 라우터 Ability는 실행 데이터 없이 Prepare되는 것이 정상이다.
            // 이런 Ability가 BT 선택 대상(aiSelectable)이나 전투 슬롯에 노출되면
            // 트리거 경로를 우회해 활성화가 시도되고, 런타임은 이를 거부하므로
            // "선택은 되는데 아무 일도 일어나지 않는" 무증상 실패가 된다.
            ValidateRequestDrivenExposure(set, issues);

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

            PlayerChargeAbilitySettings effectiveCharge =
                set.GetEffectiveCharge();
            int stageCount = effectiveCharge?.stages?.Count ?? 0;
            int thresholdCount =
                effectiveCharge?.stageThresholds?.Count ?? 0;
            if (thresholdCount > 0 && thresholdCount != Mathf.Max(0, stageCount - 1))
                Error(
                    set,
                    $"차지 임계값 수({thresholdCount})는 단계 수 - 1({Mathf.Max(0, stageCount - 1)})이어야 합니다.",
                    issues);
            for (int i = 0; i < stageCount; i++)
                if (effectiveCharge.stages[i] == null)
                    Error(set, $"차지 단계 {i}의 Ability 참조가 없습니다.", issues);

            IReadOnlyList<AbilityComboRouteDefinition> effectiveRoutes =
                set.GetEffectiveComboRoutes();
            for (int i = 0; i < effectiveRoutes.Count; i++)
            {
                AbilityComboRouteDefinition route = effectiveRoutes[i];
                if (route == null)
                    Error(set, $"연계 라우트 {i}가 null입니다.", issues);
                else if (route.ability == null)
                    Error(set, $"연계 라우트 '{route.DisplayLabel}'의 Ability 참조가 없습니다.", issues);
                else if (route.IsEmpty)
                    Warning(set, $"연계 라우트 '{route.DisplayLabel}'의 입력 패턴이 비어 있습니다.", issues);
            }

            var triggerCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (GameplayAbilitySO ability in set.EnumerateAll())
            {
                for (int i = 0; i < (ability?.triggers?.Count ?? 0); i++)
                {
                    AbilityTriggerDefinition trigger = ability.triggers[i];
                    if (trigger == null || !trigger.triggerTag.IsValid()) continue;
                    string key = $"{trigger.source}:{trigger.triggerTag.TagName}";
                    triggerCounts.TryGetValue(key, out int count);
                    triggerCounts[key] = count + 1;
                }
            }
            foreach (KeyValuePair<string, int> pair in triggerCounts)
            {
                if (pair.Value >= 3)
                {
                    Warning(
                        set,
                        $"같은 Source/Tag 트리거 '{pair.Key}'가 AbilitySet에 {pair.Value}개 있습니다.",
                        issues);
                }
            }
        }

        private static void ValidateAbilityTriggers(
            GameplayAbilitySO ability,
            List<AbilityValidationIssue> issues)
        {
            for (int i = 0; i < (ability.triggers?.Count ?? 0); i++)
            {
                AbilityTriggerDefinition trigger = ability.triggers[i];
                if (trigger == null)
                {
                    Error(ability, $"Trigger {i}가 null입니다.", issues);
                    continue;
                }
                if (!trigger.triggerTag.IsValid())
                {
                    Error(
                        ability,
                        $"Trigger {i}의 태그가 비어 있거나 Registry에 없습니다.",
                        issues);
                }
                if (trigger.mode == AbilityTriggerActivationMode.Immediate
                    && ability.concurrency != AbilityConcurrencyPolicy.Background)
                {
                    Error(
                        ability,
                        $"Immediate Trigger {i}는 Background Ability에서만 사용할 수 있습니다.",
                        issues);
                }
                if (trigger.mode == AbilityTriggerActivationMode.Immediate
                    && ability.concurrency == AbilityConcurrencyPolicy.Background
                    && (ability.persistence?.backgroundMaxDurationSeconds ?? 0f) <= 0f)
                {
                    Error(
                        ability,
                        $"Immediate Background Trigger {i}에는 0보다 큰 최대 실행 시간이 필요합니다.",
                        issues);
                }
                if (trigger.source == AbilityTriggerSource.OwnedTagPresent
                    && ability.concurrency != AbilityConcurrencyPolicy.Background)
                {
                    Error(
                        ability,
                        $"OwnedTagPresent Trigger {i}는 Background Ability에서만 사용할 수 있습니다.",
                        issues);
                }
                if (ContainsTag(
                        ability.activation?.executionGrantedTagIds,
                        trigger.triggerTag,
                        trigger.matchMode))
                {
                    Error(
                        ability,
                        $"Trigger {i}의 태그가 자신의 실행 부여 태그에도 포함되어 순환 발동할 수 있습니다.",
                        issues);
                }
            }

            for (int i = 0; i < (ability.cancelAbilitiesWithTag?.Count ?? 0); i++)
            {
                GameplayTag cancelTag = ability.cancelAbilitiesWithTag[i];
                if (ContainsTag(
                        ability.abilityTagIds,
                        cancelTag,
                        AbilityTagMatchMode.Hierarchy))
                {
                    Error(
                        ability,
                        $"Cancel Ability 태그 '{cancelTag.TagName}'가 자신의 Ability 태그를 포함합니다.",
                        issues);
                }
            }
        }

        /// <summary>
        /// Request 전용 라우터 Ability가 트리거 이외 활성화 경로에 노출됐는지 검사한다.
        /// </summary>
        private static void ValidateRequestDrivenExposure(
            AbilitySetSO set,
            List<AbilityValidationIssue> issues)
        {
            for (int i = 0; i < (set.additionalAbilities?.Count ?? 0); i++)
            {
                GameplayAbilitySO ability = set.additionalAbilities[i];
                if (ability == null || !IsRequestDrivenAbility(ability))
                    continue;
                for (int j = 0; j < (ability.variants?.Count ?? 0); j++)
                {
                    if (ability.variants[j]?.executionPayload
                            is not UPlayGroundMotionAbilityPayloadSO payload
                        || payload.attackInfo?.aiSelectable != true)
                        continue;
                    Error(
                        set,
                        $"Request 전용 트리거 Ability '{ability.name}'의 Variant {j}가 aiSelectable입니다. "
                        + "BT가 직접 선택하면 트리거 경로를 우회하므로 런타임이 활성화를 거부합니다.",
                        issues);
                    break;
                }
            }

            for (int i = 0; i < (set.combatBindings?.Count ?? 0); i++)
            {
                PlayerCombatAbilityBinding binding = set.combatBindings[i];
                if (binding?.abilities == null)
                    continue;
                for (int j = 0; j < binding.abilities.Count; j++)
                {
                    GameplayAbilitySO ability = binding.abilities[j];
                    if (ability == null || !IsRequestDrivenAbility(ability))
                        continue;
                    Error(
                        set,
                        $"Request 전용 트리거 Ability '{ability.name}'가 전투 슬롯 '{binding.slot}'에 "
                        + "바인딩되어 있습니다. 트리거 경로 외 활성화는 거부됩니다.",
                        issues);
                }
            }
        }

        private static bool IsRequestDrivenAbility(GameplayAbilitySO ability)
        {
            if (ability?.triggers == null || ability.triggers.Count == 0)
                return false;
            for (int i = 0; i < ability.triggers.Count; i++)
                if (ability.triggers[i] == null
                    || ability.triggers[i].mode
                    != AbilityTriggerActivationMode.Request)
                    return false;
            return true;
        }

        private static void ValidateModifierMagnitude(
            GameplayEffectSO effect,
            GameplayEffectModifierDefinition modifier,
            int index,
            List<AbilityValidationIssue> issues)
        {
            switch (modifier.magnitudeSource)
            {
                case GameplayEffectMagnitudeSource.AttributeBased:
                    if (!modifier.SourceAttributeId.IsValid)
                    {
                        Error(
                            effect,
                            $"Modifier {index}번 AttributeBased 크기의 "
                            + "캡처 Attribute ID가 비어 있습니다.",
                            issues);
                    }
                    if (Mathf.Approximately(modifier.coefficient, 0f))
                    {
                        Warning(
                            effect,
                            $"Modifier {index}번 AttributeBased 계수가 0이라 "
                            + "캡처값이 반영되지 않습니다.",
                            issues);
                    }
                    if (modifier.captureSource
                            == GameplayEffectCaptureSource.Target
                        && modifier.capturePolicy
                            == GameplayEffectCapturePolicy.SnapshotOnCreate)
                    {
                        Error(
                            effect,
                            $"Modifier {index}번은 Spec 생성 시점에 적용 대상이 없어 "
                            + "Target Attribute를 SnapshotOnCreate로 캡처할 수 없습니다.",
                            issues);
                    }
                    if (modifier.capturePolicy
                        == GameplayEffectCapturePolicy.SnapshotOnCreate
                        && effect.durationType == GameplayEffectDurationType.Infinite)
                    {
                        Info(
                            effect,
                            $"Modifier {index}번은 Spec 생성 시점 값으로 고정됩니다. "
                            + "Infinite Effect에서 의도한 동작인지 확인하세요.",
                            issues);
                    }
                    break;

                case GameplayEffectMagnitudeSource.SetByCaller:
                    if (string.IsNullOrWhiteSpace(modifier.setByCallerKey))
                    {
                        Error(
                            effect,
                            $"Modifier {index}번 SetByCaller 크기의 키가 비어 있습니다.",
                            issues);
                    }
                    else if (!modifier.allowMissingSetByCaller)
                    {
                        Info(
                            effect,
                            $"Modifier {index}번은 SetByCaller "
                            + $"'{modifier.setByCallerKey}'가 없으면 적용이 실패합니다.",
                            issues);
                    }
                    break;

                case GameplayEffectMagnitudeSource.ScalableByLevel:
                    if (Mathf.Approximately(modifier.perLevel, 0f))
                    {
                        Warning(
                            effect,
                            $"Modifier {index}번 ScalableByLevel의 레벨당 증가량이 0입니다. "
                            + "Fixed와 동일하게 동작합니다.",
                            issues);
                    }
                    break;

                case GameplayEffectMagnitudeSource.Fixed:
                    break;

                default:
                    Error(
                        effect,
                        $"Modifier {index}번의 크기 계산 방식이 올바르지 않습니다: "
                        + modifier.magnitudeSource,
                        issues);
                    break;
            }
        }

        private static void ValidateTagRequirement(
            AbilityTagRequirement requirement,
            UnityEngine.Object context,
            string label,
            List<AbilityValidationIssue> issues)
        {
            if (requirement == null) return;
            ValidateTagExpression(requirement.expression, context, label, issues);
            ValidateTagList(requirement.requireAll, context, $"{label} RequireAll", issues);
            ValidateTagList(requirement.requireAny, context, $"{label} RequireAny", issues);
            ValidateTagList(requirement.blockAny, context, $"{label} BlockAny", issues);
            for (int i = 0; i < (requirement.requireAll?.Count ?? 0); i++)
            {
                GameplayTag required = requirement.requireAll[i];
                if (ContainsTag(requirement.blockAny, required, requirement.matchMode))
                {
                    Warning(
                        context,
                        $"{label} 태그 '{required.TagName}'가 RequireAll과 BlockAny에 함께 있습니다.",
                        issues);
                }
            }
        }

        private static void ValidateTagExpression(
            AbilityTagExpression expression,
            UnityEngine.Object context,
            string label,
            List<AbilityValidationIssue> issues)
        {
            if (expression == null) return;

            int depth = AbilityTagExpressionUtility.MeasureDepth(expression);
            if (!AbilityTagExpressionUtility.IsWithinDepth(depth))
            {
                Error(
                    context,
                    $"{label} 중첩 태그 조건의 깊이가 "
                    + $"{AbilityTagExpression.MaxDepth}를 초과했습니다. "
                    + "런타임에서 조건이 항상 실패합니다.",
                    issues);
                return;
            }

            var tagLists = new List<List<GameplayTag>>();
            AbilityTagExpressionUtility.CollectTagLists(expression, tagLists);
            if (!AbilityTagExpressionUtility.HasCondition(expression))
            {
                Warning(
                    context,
                    $"{label} 중첩 태그 조건에 유효한 태그 노드가 없습니다.",
                    issues);
                return;
            }
            for (int i = 0; i < tagLists.Count; i++)
                ValidateTagList(tagLists[i], context, $"{label} 중첩[{i}]", issues);
        }

        private static bool ContainsTag(
            List<GameplayTag> tags,
            GameplayTag expected,
            AbilityTagMatchMode matchMode)
        {
            if (!expected.IsValid()) return false;
            for (int i = 0; i < (tags?.Count ?? 0); i++)
            {
                GameplayTag candidate = tags[i];
                if (matchMode == AbilityTagMatchMode.Exact
                    ? candidate.Equals(expected)
                    : candidate.IsChildOf(expected))
                    return true;
            }
            return false;
        }

        private static bool IsEmpty<T>(List<T> values) =>
            values == null || values.Count == 0;

        private static void ValidatePassive(
            PassiveAbilitySO passive,
            List<AbilityValidationIssue> issues)
        {
            if (passive == null) return;
            if (string.IsNullOrWhiteSpace(passive.passiveId))
                Error(passive, "passiveId가 비어 있습니다.", issues);
            if (passive.presentation == null
                || passive.presentation.category != AbilityCategory.Passive)
            {
                Error(passive, "표시 카테고리는 Passive여야 합니다.", issues);
            }
            if (string.IsNullOrWhiteSpace(passive.characterSelectDescription))
                Warning(passive, "캐릭터 선택 화면용 수치 없는 요약 설명이 비어 있습니다.", issues);

            if (passive.activationType == PassiveActivationType.Always)
            {
                if (passive.modifiers == null || passive.modifiers.Count == 0)
                    Error(passive, "상시 패시브에는 Modifier가 필요합니다.", issues);
            }
            else if (passive.triggeredEffects == null
                     || passive.triggeredEffects.Count == 0)
            {
                Error(passive, "조건부 패시브에는 발동 Effect가 필요합니다.", issues);
            }

            if (passive.modifiers != null)
                for (int i = 0; i < passive.modifiers.Count; i++)
                    if (passive.modifiers[i] == null)
                        Error(passive, $"Modifier {i}번이 null입니다.", issues);
            ValidateEffectReferences(
                passive.triggeredEffects, passive, "Triggered Effect", issues);
        }

        private static void ValidatePassiveSet(
            CharacterPassiveSetSO set,
            List<AbilityValidationIssue> issues)
        {
            if (set == null) return;
            var owned = new HashSet<PassiveAbilitySO>();
            for (int i = 0; i < (set.passives?.Count ?? 0); i++)
            {
                PassiveAbilitySO passive = set.passives[i];
                if (passive == null)
                    Error(set, $"보유 패시브 {i}번 참조가 없습니다.", issues);
                else if (!owned.Add(passive))
                    Error(set, $"보유 패시브 '{passive.name}'가 중복되었습니다.", issues);
            }

            if ((set.characterSelectRepresentatives?.Count ?? 0)
                > CharacterPassiveSetSO.MaxCharacterSelectRepresentatives)
            {
                Error(set, "캐릭터 선택 대표 패시브는 최대 2개입니다.", issues);
            }

            var representatives = new HashSet<PassiveAbilitySO>();
            for (int i = 0;
                 i < (set.characterSelectRepresentatives?.Count ?? 0);
                 i++)
            {
                PassiveAbilitySO passive = set.characterSelectRepresentatives[i];
                if (passive == null)
                    Error(set, $"대표 패시브 {i}번 참조가 없습니다.", issues);
                else if (!owned.Contains(passive))
                    Error(set, $"대표 패시브 '{passive.name}'는 보유 목록에 없습니다.", issues);
                else if (!representatives.Add(passive))
                    Error(set, $"대표 패시브 '{passive.name}'가 중복되었습니다.", issues);
            }
        }

        private static void ValidatePassiveDatabase(
            CharacterPassiveDatabaseSO database,
            List<AbilityValidationIssue> issues)
        {
            if (database == null) return;
            var types = new HashSet<CharacterActorType>();
            for (int i = 0; i < (database.entries?.Count ?? 0); i++)
            {
                CharacterPassiveSetSO set = database.entries[i];
                if (set == null)
                    Error(database, $"패시브 세트 {i}번 참조가 없습니다.", issues);
                else if (set.characterType == CharacterActorType.None)
                    Error(set, "캐릭터 타입이 None입니다.", issues);
                else if (!types.Add(set.characterType))
                    Error(database, $"'{set.characterType}' 패시브 세트가 중복되었습니다.", issues);
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
            List<GameplayTag> tags,
            UnityEngine.Object context,
            string label,
            List<AbilityValidationIssue> issues)
        {
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
                if (!tags[i].IsValid())
                    Warning(
                        context,
                        $"{label} 태그 목록 {i}번이 비어 있거나 Registry에 없습니다.",
                        issues);
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
