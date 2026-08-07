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

    public enum AbilityResourceTrigger
    {
        AbilityCommitted,
        AttackHit,
        GameplayEvent,
        EffectExpired,
    }

    [Serializable]
    public sealed class AbilityResourceRule
    {
        public AbilityResourceTrigger trigger;
        public AbilityResourceType resourceType;
        public GameplayTag requiredTag;
        public float delta;
    }

    [CreateAssetMenu(
        fileName = "AbilityResourceRules_",
        menuName = "UPlayGround/Ability/Resource Rules")]
    public sealed class AbilityResourceRuleSO : ScriptableObject
    {
        public List<AbilityResourceRule> rules = new();
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

    public enum AbilityTagMatchMode
    {
        /// <summary>하위 계층 태그도 조건을 만족시킨다.</summary>
        Hierarchy,
        /// <summary>태그 문자열이 정확히 일치해야 한다.</summary>
        Exact,
    }

    public enum AbilityTriggerSource
    {
        /// <summary>소유 태그가 새로 추가되는 순간 한 번 활성화를 시도한다.</summary>
        OwnedTagAdded,
        /// <summary>태그가 존재하는 동안 활성 상태를 유지하고 소실 시 취소한다.</summary>
        OwnedTagPresent,
        /// <summary>Gameplay Event가 전달될 때 활성화를 시도한다.</summary>
        GameplayEvent,
    }

    public enum AbilityTriggerActivationMode
    {
        /// <summary>Ability System이 Prepare와 Commit을 직접 수행한다.</summary>
        Immediate,
        /// <summary>전투 계층에 활성화 요청을 전달한다.</summary>
        Request,
    }

    [Serializable]
    public sealed class AbilityTriggerDefinition
    {
        public GameplayTag triggerTag;
        public AbilityTriggerSource source = AbilityTriggerSource.OwnedTagAdded;
        public AbilityTriggerActivationMode mode = AbilityTriggerActivationMode.Immediate;
        public AbilityTagMatchMode matchMode = AbilityTagMatchMode.Exact;
        [Tooltip("같은 프레임에 여러 트리거가 걸리면 높은 값이 먼저 처리됩니다.")]
        public int priority;
        [Min(0f)]
        [Tooltip("트리거로 재활성화되기까지의 최소 간격입니다. OwnedTagPresent에는 적용되지 않습니다.")]
        public float retriggerIntervalSeconds;
        [Tooltip("Request 트리거가 현재 주 실행 Ability를 선점할 수 있는지 여부입니다. 기본값은 선점 금지입니다.")]
        public bool allowPreemption;
    }

    [Serializable]
    public sealed class AbilityTagRequirement
    {
        [Tooltip("전부 보유해야 활성화됩니다. (AND)")]
        public List<GameplayTag> requireAll = new();
        [Tooltip("하나라도 보유하면 활성화됩니다. 비어 있으면 검사하지 않습니다. (OR)")]
        public List<GameplayTag> requireAny = new();
        [Tooltip("하나라도 보유하면 활성화를 차단합니다. (NONE)")]
        public List<GameplayTag> blockAny = new();
        public AbilityTagMatchMode matchMode = AbilityTagMatchMode.Hierarchy;
        [Tooltip("위 평면 조건으로 표현할 수 없는 중첩 조건입니다. 평면 조건과 AND로 결합됩니다.")]
        [SerializeReference] public AbilityTagExpression expression;

        public bool IsEmpty =>
            (requireAll?.Count ?? 0) == 0
            && (requireAny?.Count ?? 0) == 0
            && (blockAny?.Count ?? 0) == 0
            && AbilityTagExpressionUtility.IsEffectivelyEmpty(expression);
    }

    public static class AbilityTagRequirementEvaluator
    {
        [ThreadStatic]
        private static GameplayTagReaderQuerySource _tagQuerySource;
        [ThreadStatic]
        private static bool _isEvaluatingExpression;

        public static bool Matches(
            AbilityTagRequirement requirement,
            IGameplayTagReader tags)
        {
            if (requirement == null || requirement.IsEmpty)
                return true;
            if (tags == null)
                return false;
            if (!MatchesFlat(requirement, tags))
                return false;
            return requirement.expression == null
                   || EvaluateExpression(requirement.expression, tags);
        }

        /// <summary>
        /// 표현식 평가마다 어댑터를 새로 만들지 않으려는 호출자를 위한 오버로드.
        /// <paramref name="cachedSource"/>는 호출자가 소유·재사용한다.
        /// </summary>
        public static bool Matches(
            AbilityTagRequirement requirement,
            IGameplayTagReader tags,
            GameplayTagReaderQuerySource cachedSource)
        {
            if (requirement == null || requirement.IsEmpty)
                return true;
            if (tags == null)
                return false;
            if (!MatchesFlat(requirement, tags))
                return false;
            if (requirement.expression == null)
                return true;
            return requirement.expression.Evaluate(
                cachedSource != null
                    ? cachedSource.Bind(tags)
                    : new GameplayTagReaderQuerySource(tags));
        }

        private static bool EvaluateExpression(
            AbilityTagExpression expression,
            IGameplayTagReader tags)
        {
            // HasTag 구현이 다시 조건 평가를 호출하는 재진입 경로에서는 공유 어댑터의
            // Bind 대상이 바뀌지 않도록 일회성 어댑터로 격리한다.
            if (_isEvaluatingExpression)
                return expression.Evaluate(new GameplayTagReaderQuerySource(tags));

            _tagQuerySource ??= new GameplayTagReaderQuerySource();
            _isEvaluatingExpression = true;
            try
            {
                return expression.Evaluate(_tagQuerySource.Bind(tags));
            }
            finally
            {
                _isEvaluatingExpression = false;
            }
        }

        private static bool MatchesFlat(
            AbilityTagRequirement requirement,
            IGameplayTagReader tags)
        {
            bool hierarchy = requirement.matchMode == AbilityTagMatchMode.Hierarchy;
            for (int i = 0; i < (requirement.requireAll?.Count ?? 0); i++)
                if (requirement.requireAll[i].IsValid()
                    && !tags.HasTag(requirement.requireAll[i], hierarchy))
                    return false;
            bool hasAnyRequirement = false;
            bool hasAny = false;
            for (int i = 0; i < (requirement.requireAny?.Count ?? 0); i++)
            {
                if (!requirement.requireAny[i].IsValid()) continue;
                hasAnyRequirement = true;
                if (tags.HasTag(requirement.requireAny[i], hierarchy))
                    hasAny = true;
            }
            if (hasAnyRequirement && !hasAny)
                return false;
            for (int i = 0; i < (requirement.blockAny?.Count ?? 0); i++)
                if (requirement.blockAny[i].IsValid()
                    && tags.HasTag(requirement.blockAny[i], hierarchy))
                    return false;
            return true;
        }
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
        CancelExisting = 1,
        RejectNew = 2,
        Background = 3,
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
        public readonly IReadOnlyDictionary<string, float> SetByCallerMagnitudes;

        public GameplayEffectApplicationOptions(
            GameplayEffectHudVisibility hudVisibility,
            IReadOnlyDictionary<string, float> setByCallerMagnitudes = null)
        {
            HudVisibility = hudVisibility;
            SetByCallerMagnitudes = setByCallerMagnitudes;
        }
    }

    [Serializable]
    public sealed class AbilityActivationRules
    {
        public List<GameplayTag> requiredTagIds = new();
        public List<GameplayTag> blockedTagIds = new();
        public List<GameplayTag> executionGrantedTagIds = new();
        public AbilityTagRequirement ownerTagRequirement = new();
        public AbilityTagRequirement sourceTagRequirement = new();
        public AbilityTagRequirement targetTagRequirement = new();
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
        [Min(1)] public int maxCharges = 1;
        [Min(0f)] public float globalLockSeconds;

        public string ResolveGroupId(string abilityId) =>
            string.IsNullOrWhiteSpace(cooldownGroupId) ? abilityId : cooldownGroupId.Trim();
    }

    public enum AbilityTargetingMode
    {
        None,
        AutoTarget,
        GroundIndicator,
        Aimed,
    }

    [Serializable]
    public sealed class AbilityTargetingDefinition
    {
        public AbilityTargetingMode mode;
        [Min(0f)] public float indicatorRadius;
        [Min(0f)] public float maximumAimDistance;
        public bool clampToGround = true;
    }

    [Serializable]
    public sealed class AbilityVariantCondition
    {
        public AbilityGroundCondition groundCondition = AbilityGroundCondition.Any;
        [Min(0f)] public float minResource;
        public bool requiresFullResource;
        public List<GameplayTag> requiredTagIds = new();
        public List<GameplayTag> blockedTagIds = new();
        public AbilityTagRequirement ownerTagRequirement = new();
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
        [Tooltip("Background 실행의 안전 종료 시간. 0이면 Background로 실행할 수 없습니다.")]
        [Min(0f)] public float backgroundMaxDurationSeconds;
    }

    [Serializable]
    public sealed class AbilityBalanceMetadata
    {
        [Min(0f)] public float expectedDamage;
        [Min(0f)] public float expectedDuration;
        public List<string> roleTags = new();
        public string designerNotes;
    }

    /// <summary>
    /// Modifier 크기를 어디서 얻는지 결정한다. 기본값 <see cref="Fixed"/>는 기존 저작과 같으므로
    /// 이미 저장된 Effect 에셋의 동작이 바뀌지 않는다.
    /// </summary>
    public enum GameplayEffectMagnitudeSource
    {
        /// <summary>고정값. <c>value</c>를 그대로 사용한다.</summary>
        Fixed = 0,
        /// <summary>Source/Target Attribute를 캡처해 계수를 적용한다.</summary>
        AttributeBased = 1,
        /// <summary>실행 시점에 코드가 넣어준 SetByCaller 값을 사용한다.</summary>
        SetByCaller = 2,
        /// <summary>Spec Level에 비례해 <c>value + (Level - 1) * perLevel</c>로 계산한다.</summary>
        ScalableByLevel = 3,
    }

    [Serializable]
    public sealed class GameplayEffectModifierDefinition
    {
        [Tooltip("런타임 권위 Attribute ID.")]
        [AttributeIdSelector]
        public string attributeId;
        public ModifierType modifierType = ModifierType.Percent;
        [Tooltip("Fixed의 고정값이자 ScalableByLevel의 기준값입니다.")]
        public float value;

        [Header("크기 계산")]
        public GameplayEffectMagnitudeSource magnitudeSource =
            GameplayEffectMagnitudeSource.Fixed;

        [Header("AttributeBased")]
        [Tooltip("캡처할 Attribute ID입니다.")]
        [AttributeIdSelector]
        public string sourceAttributeId;
        [Tooltip("Source는 시전자, Target은 피적용자 Attribute를 캡처합니다.")]
        public GameplayEffectCaptureSource captureSource =
            GameplayEffectCaptureSource.Source;
        public GameplayEffectCapturePolicy capturePolicy =
            GameplayEffectCapturePolicy.SnapshotOnApply;
        [Tooltip("(캡처값 + preAdd) * coefficient + postAdd")]
        public float coefficient = 1f;
        public float preAdd;
        public float postAdd;

        [Header("SetByCaller")]
        [Tooltip("실행 코드가 채우는 키입니다. 예: Data.Damage")]
        public string setByCallerKey;
        [Tooltip("키가 없으면 실패시키지 않고 defaultValue를 사용합니다.")]
        public bool allowMissingSetByCaller;
        public float setByCallerDefaultValue;

        [Header("ScalableByLevel")]
        public float perLevel;

        public AttributeId AttributeId => new(attributeId);
        public AttributeId SourceAttributeId => new(sourceAttributeId);
    }

}
