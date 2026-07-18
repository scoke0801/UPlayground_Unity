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
        Player,
        Enemy,
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

    public enum GameplayResourceOperationType
    {
        Add,
        Set,
        PercentOfMax,
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
        public AbilityOwnerType ownerType = AbilityOwnerType.Player;
    }

    [Serializable]
    public sealed class AbilityActivationRules
    {
        public List<GameplayTagId> requiredTagIds = new();
        public List<GameplayTagId> blockedTagIds = new();
        public List<GameplayTagId> executionGrantedTagIds = new();
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
        public List<GameplayTagId> requiredTagIds = new();
        public List<GameplayTagId> blockedTagIds = new();
    }

    [Serializable]
    public sealed class AbilityVariantDefinition
    {
        public string variantId = "Default";
        public int priority;
        public AbilityVariantCondition condition = new();
        [Tooltip("신규 모듈형 실행 데이터. 설정된 경우 레거시 필드보다 우선합니다.")]
        public AbilityExecutionPayloadSO executionPayload;
        [Tooltip("V1 에셋 호환 필드. 변환 도구 적용 전까지 유지합니다.")]
        public AnimKey animKey = AnimKey.None;
        [Tooltip("V1 에셋 호환 필드. 변환 도구 적용 전까지 유지합니다.")]
        public PlayerAttackInfo playerAttackInfo = new();
        public List<GameplayEffectSO> targetEffects = new();
        public List<GameplayEffectSO> ownerEffects = new();

        public AnimKey ResolveLegacyAnimKey() =>
            animKey != AnimKey.None
                ? animKey
                : playerAttackInfo?.baseInfo?.animKey ?? AnimKey.None;

        public bool HasLegacyExecutionData =>
            playerAttackInfo?.baseInfo != null && ResolveLegacyAnimKey() != AnimKey.None;

        public bool IsExecutable => executionPayload != null || HasLegacyExecutionData;
    }

    [Serializable]
    public sealed class AbilityCueDefinition
    {
        public string startCueId;
        public string failureCueId;
        public string endCueId;
        public string cooldownReadyCueId;
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
        public StatType statType = StatType.AttackPower;
        public ModifierType modifierType = ModifierType.Percent;
        public float value;
    }

    [Serializable]
    public sealed class GameplayResourceOperation
    {
        public AbilityResourceType resourceType = AbilityResourceType.Health;
        public GameplayResourceOperationType operation = GameplayResourceOperationType.Add;
        public float magnitude;
    }

    [Serializable]
    public sealed class AbilityResourceSaveEntry
    {
        public AbilityResourceType resourceType;
        public float currentValue;
    }

    [Serializable]
    public sealed class AbilityCooldownSaveEntry
    {
        public string cooldownGroupId;
        public float remainingSeconds;
    }

    [Serializable]
    public sealed class GameplayEffectSaveEntry
    {
        public string effectId;
        public string sourceActorId;
        public float remainingSeconds;
        public int stackCount;
        public float capturedMagnitude;
    }

    [Serializable]
    public sealed class AbilityRuntimeSaveData
    {
        public int version = 1;
        public List<AbilityResourceSaveEntry> resources = new();
        public List<AbilityCooldownSaveEntry> cooldowns = new();
        public List<GameplayEffectSaveEntry> activeEffects = new();
    }
}
