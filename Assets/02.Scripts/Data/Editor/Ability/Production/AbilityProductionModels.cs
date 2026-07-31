using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Ability.Production
{
    public enum AbilityProductionOwnerKind
    {
        Player,
        Monster,
        Boss,
    }

    public enum AbilitySetBindingMode
    {
        AdditionalAbilities,
        PlayerSkillSlot,
        PlayerCombatSequence,
    }

    public enum AbilityPlanOperation
    {
        Create,
        Reuse,
        Modify,
    }

    public enum AbilityPlanAssetKind
    {
        GameplayAbility,
        MotionPayload,
        AbilitySet,
        GameplayEffect,
    }

    public enum AbilityProductionSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal enum AbilityProductionStage
    {
        PayloadCreated,
        EffectCreated,
        AbilityCreated,
        SetBound,
    }

    [Serializable]
    public sealed class AbilityRecipeDefinition
    {
        public string RecipeId;
        public string DisplayName;
        public int Version = 1;
        public AbilityProductionOwnerKind OwnerKind;
        public AbilityCategory Category = AbilityCategory.Attack;
        public AbilityTargetPolicy TargetPolicy = AbilityTargetPolicy.Required;
        public AbilityTargetRelation TargetRelation = AbilityTargetRelation.Enemy;
        public AbilityGroundCondition GroundCondition = AbilityGroundCondition.Any;
        public AbilityConcurrencyPolicy Concurrency = AbilityConcurrencyPolicy.RejectNew;
        public AbilityAttackCategory AttackCategory = AbilityAttackCategory.Basic;
        public AttackType AttackType = AttackType.Melee;
        public bool AiSelectable;
        public float DefaultSelectionWeight = 10f;
        public string DefaultTaskGraphPath;
        public AbilitySetBindingMode BindingMode = AbilitySetBindingMode.AdditionalAbilities;
        public bool SupportsEffect;
        public bool RequiresEffect;
    }

    [Serializable]
    public sealed class AbilityCreationRequest
    {
        public AbilityRecipeDefinition Recipe;
        public string DisplayName;
        public string AbilityId;
        public string AssetName;
        public string SaveRoot;
        public AbilitySetSO TargetSet;
        public ActorAnimationMotionSet MotionOwner;
        public MotionSetAsset Motion;
        public AbilityTaskGraphSO TaskGraph;
        public GameplayEffectSO CommitEffect;
        public GameplayEffectSO EndEffect;
        public bool CreateCommitEffect;
        public string EffectId;
        public string EffectAssetName;
        public GameplayEffectPolarity EffectPolarity;
        public GameplayEffectDurationType EffectDurationType;
        public float EffectDurationSeconds;
        public string EffectAttributeId;
        public ModifierType EffectModifierType = ModifierType.Flat;
        public float EffectModifierValue;
        public AbilitySetBindingMode BindingMode;
        public PlayerSkillSlot PlayerSkillSlot;
        public PlayerCombatAbilitySlot PlayerCombatSlot;
        public bool ReplaceExistingBinding;
        public int RequiredLevel = 1;
        public float SelectionWeight = 10f;
        public float MinDistance;
        public float MaxDistance = 3f;
    }

    public sealed class AbilityProductionIssue
    {
        public AbilityProductionIssue(
            string code,
            AbilityProductionSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            Code = code ?? string.Empty;
            Severity = severity;
            Message = message ?? string.Empty;
            Context = context;
        }

        public string Code { get; }
        public AbilityProductionSeverity Severity { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
    }

    public sealed class AbilityPlanItem
    {
        public AbilityPlanItem(
            AbilityPlanOperation operation,
            AbilityPlanAssetKind assetKind,
            string targetPath,
            UnityEngine.Object targetAsset = null)
        {
            Operation = operation;
            AssetKind = assetKind;
            TargetPath = targetPath ?? string.Empty;
            TargetAsset = targetAsset;
        }

        public AbilityPlanOperation Operation { get; }
        public AbilityPlanAssetKind AssetKind { get; }
        public string TargetPath { get; }
        public UnityEngine.Object TargetAsset { get; }
    }

    public sealed class AbilityCreationPlan
    {
        private readonly List<AbilityPlanItem> _items = new();
        private readonly List<AbilityProductionIssue> _issues = new();

        public AbilityCreationRequest Request { get; internal set; }
        public string AbilityPath { get; internal set; }
        public string PayloadPath { get; internal set; }
        public string EffectPath { get; internal set; }
        public string StableAbilityId { get; internal set; }
        public IReadOnlyList<AbilityPlanItem> Items => _items;
        public IReadOnlyList<AbilityProductionIssue> Issues => _issues;
        public bool CanApply
        {
            get
            {
                for (int i = 0; i < _issues.Count; i++)
                    if (_issues[i].Severity == AbilityProductionSeverity.Error)
                        return false;
                return _items.Count > 0;
            }
        }

        internal void AddItem(AbilityPlanItem item)
        {
            if (item != null)
                _items.Add(item);
        }

        internal void AddIssue(AbilityProductionIssue issue)
        {
            if (issue != null)
                _issues.Add(issue);
        }
    }

    public sealed class AbilityProductionResult
    {
        public bool Success { get; internal set; }
        public string Message { get; internal set; }
        public GameplayAbilitySO Ability { get; internal set; }
        public UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO Payload
        {
            get;
            internal set;
        }
        public GameplayEffectSO Effect { get; internal set; }
    }
}
