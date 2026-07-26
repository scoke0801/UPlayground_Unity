using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Ability
{
    public enum AbilityOwnerType
    {
        Actor,
    }

    public enum AbilityCategory
    {
        Attack,
        Defense,
        Movement,
        Support,
        Ultimate,
        Passive,
    }

    public enum AbilityResourceType
    {
        None,
        UltimateEnergy,
        Forte,
        Concerto,
        SkillCharge,
        Health,
    }

    public enum AbilityCostPolicy
    {
        None,
        Fixed,
        All,
        PercentOfMax,
    }

    public enum AbilityGroundCondition
    {
        Any,
        Grounded,
        Airborne,
    }

    public enum AbilityTargetPolicy
    {
        None,
        Optional,
        Required,
    }

    public enum AbilityTargetRelation
    {
        Self,
        Ally,
        Enemy,
    }

    public enum AbilityConcurrencyPolicy
    {
        Allow,
        CancelExisting,
        RejectNew,
    }

    public enum AbilitySwapPolicy
    {
        CancelOnSwap,
        PersistPerCharacter,
        PersistOnPlayerActor,
    }

    public enum GameplayEffectDurationType
    {
        Instant,
        Duration,
        Infinite,
    }

    public enum GameplayEffectPolarity
    {
        Neutral,
        Beneficial,
        Harmful,
    }

    public enum GameplayEffectHudVisibility
    {
        UseDefinition,
        ForceShow,
        ForceHide,
    }

    public enum GameplayEffectStackPolicy
    {
        RejectNew,
        RefreshDuration,
        AddStackAndRefresh,
        ReplaceExisting,
    }

    public enum GameplayEffectRemovalPolicy
    {
        RemoveOnSwap,
        PersistPerCharacter,
        PersistOnPlayerActor,
    }

    public enum GameplayEffectSavePolicy
    {
        DoNotSave,
        SaveRemainingDuration,
    }

    [Serializable]
    public sealed class AbilityPresentationDefinition
    {
        public string displayName = "새 Ability";
        public string description;
        public string nameLocalizationKey;
        public string descriptionLocalizationKey;
        public Sprite icon;
        public Color hudColor = new(0.29f, 0.62f, 1f, 1f);
        public AbilityCategory category = AbilityCategory.Attack;
        public AbilityOwnerType ownerType = AbilityOwnerType.Actor;
    }

    [Serializable]
    public sealed class GameplayEffectPresentationDefinition
    {
        public string displayName = "새 Effect";
        [TextArea] public string description;
        public string nameLocalizationKey;
        public string descriptionLocalizationKey;
        public Sprite icon;
        [Tooltip("Duration/Infinite Effect를 HUD에 표시합니다.")]
        public bool showInHud = true;
        [Tooltip("값이 클수록 제한된 HUD 슬롯에서 먼저 표시됩니다.")]
        public int hudPriority;
        public bool showRemainingTime = true;
        public bool showStackCount = true;
    }

    public readonly struct GameplayEffectApplicationOptions
    {
        public readonly GameplayEffectHudVisibility HudVisibility;

        public GameplayEffectApplicationOptions(
            GameplayEffectHudVisibility hudVisibility)
        {
            HudVisibility = hudVisibility;
        }
    }

    [Serializable]
    public sealed class AbilityActivationRules
    {
        public List<GameplayTag> requiredTagIds = new();
        public List<GameplayTag> blockedTagIds = new();
        public List<GameplayTag> executionGrantedTagIds = new();
        public AbilityGroundCondition groundCondition = AbilityGroundCondition.Any;
        public AbilityTargetPolicy targetPolicy = AbilityTargetPolicy.None;
        public AbilityTargetRelation targetRelation = AbilityTargetRelation.Enemy;
        [Min(0f)] public float minDistance;
        [Min(0f)] public float maxDistance;
    }

    [Serializable]
    public sealed class AbilityCostDefinition
    {
        public AbilityResourceType resourceType = AbilityResourceType.None;
        public AbilityCostPolicy policy = AbilityCostPolicy.None;
        [Min(0f)] public float value;
    }

    [Serializable]
    public sealed class AbilityCooldownDefinition
    {
        [Min(0f)] public float durationSeconds;
        public string cooldownGroupId;

        public string ResolveGroupId(string abilityId) =>
            string.IsNullOrWhiteSpace(cooldownGroupId) ? abilityId : cooldownGroupId.Trim();
    }

    [Serializable]
    public sealed class AbilityVariantCondition
    {
        public AbilityGroundCondition groundCondition = AbilityGroundCondition.Any;
        [Min(0f)] public float minResource;
        public bool requiresFullResource;
        public List<GameplayTag> requiredTagIds = new();
        public List<GameplayTag> blockedTagIds = new();
    }

    [Serializable]
    public sealed class AbilityVariantDefinition
    {
        public string variantId = "Default";
        public int priority;
        public AbilityVariantCondition condition = new();
        [Tooltip("프로젝트별 실행 데이터. UPlayGround에서는 Motion Ability Payload를 사용합니다.")]
        public AbilityExecutionPayloadSO executionPayload;
        public List<GameplayEffectSO> targetEffects = new();
        public List<GameplayEffectSO> ownerEffects = new();

        /// <summary>
        /// Payload 참조 유무만 본다. 실제 실행 가능 여부는 Payload 내용까지 확인해야 하므로
        /// 프로젝트 어댑터의 <c>UPlayGroundAbilityPayloadResolver.IsExecutable</c>을 사용한다.
        /// </summary>
        public bool HasPayload => executionPayload != null;
    }

    [Serializable]
    public sealed class AbilityPersistencePolicy
    {
        public AbilitySwapPolicy swapPolicy = AbilitySwapPolicy.CancelOnSwap;
        public bool saveCooldown = true;
    }

    [Serializable]
    public sealed class AbilityBalanceMetadata
    {
        [Min(0f)] public float expectedDamage;
        [Min(0f)] public float expectedDuration;
        public List<string> roleTags = new();
        public string designerNotes;
    }

    [Serializable]
    public sealed class GameplayEffectModifierDefinition
    {
        [Tooltip("런타임 권위 Attribute ID.")]
        [AttributeIdSelector]
        public string attributeId;
        public ModifierType modifierType = ModifierType.Percent;
        public float value;

        public AttributeId AttributeId => new(attributeId);
    }

}
