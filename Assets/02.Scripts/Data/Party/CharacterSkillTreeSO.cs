using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    [Serializable]
    public sealed class SkillPointRule
    {
        [Min(0)] public int perLevel = 1;

        public int TotalPointsAtLevel(int level)
        {
            int completedLevelUps = Mathf.Max(0, level - 1);
            return completedLevelUps * Mathf.Max(0, perLevel);
        }
    }

    public enum SkillNodeBlockReason
    {
        None,
        InsufficientPoints,
        MissingPrerequisite,
        LevelTooLow,
        MaxRank,
        MissingTree,
        MissingNode,
    }

    public enum AbilityScalarKind
    {
        Damage,
        BreakDamage,
        Cooldown,
        Cost,
    }

    [CreateAssetMenu(
        fileName = "CharacterSkillTree_",
        menuName = "UPlayGround/파티/Character Skill Tree")]
    public sealed class CharacterSkillTreeSO : ScriptableObject
    {
        public CharacterActorType characterType;
        public List<SkillNodeDefinition> nodes = new();

        public SkillNodeDefinition FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return null;
            string normalized = nodeId.Trim();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null
                    && string.Equals(
                        nodes[i].NormalizedId,
                        normalized,
                        StringComparison.Ordinal))
                    return nodes[i];
            return null;
        }
    }

    [Serializable]
    public sealed class SkillNodeDefinition
    {
        public string nodeId;
        public string displayNameKey;
        public string descriptionKey;
        public Sprite icon;
        [Min(1)] public int cost = 1;
        [Min(1)] public int maxRank = 1;
        public List<string> requiredNodeIds = new();
        [Min(0)] public int requiredLevel;
        public Vector2 layoutPosition;
        [SerializeReference] public List<SkillNodeEffect> effects = new();

        public string NormalizedId => nodeId?.Trim();
    }

    [Serializable]
    public abstract class SkillNodeEffect
    {
        public abstract string Describe(int rank);
    }

    [Serializable]
    public sealed class StatDeltaEffect : SkillNodeEffect
    {
        [AttributeIdSelector] public string attributeId;
        public AttributeModifierOperation operation = AttributeModifierOperation.Add;
        public float valuePerRank;

        public AttributeId AttributeId => new(attributeId);

        public override string Describe(int rank) =>
            StatDisplayFormatter.FormatModifier(
                AttributeId,
                operation,
                valuePerRank * Mathf.Max(0, rank));
    }

    [Serializable]
    public sealed class AbilityScalarEffect : SkillNodeEffect
    {
        public string abilityId;
        public AbilityScalarKind kind;
        public ModifierType operation = ModifierType.Percent;
        public float valuePerRank;

        public override string Describe(int rank)
        {
            string label = kind switch
            {
                AbilityScalarKind.Damage => "스킬 피해",
                AbilityScalarKind.BreakDamage => "스킬 브레이크 피해",
                AbilityScalarKind.Cooldown => "스킬 재사용 대기",
                AbilityScalarKind.Cost => "스킬 소모량",
                _ => "스킬 효과",
            };
            float value = valuePerRank * Mathf.Max(0, rank);
            string sign = value >= 0f ? "+" : string.Empty;
            return operation == ModifierType.Percent
                ? $"{label} {sign}{value * 100f:0.#}%"
                : $"{label} {sign}{value:0.###}";
        }
    }

    [Serializable]
    public sealed class AbilityUnlockEffect : SkillNodeEffect
    {
        public string abilityId;
        public string unlockedLabel = "기술";

        public override string Describe(int rank) =>
            rank > 0
                ? $"{ResolveLabel()} 해금"
                : $"{ResolveLabel()} 잠김";

        private string ResolveLabel() =>
            string.IsNullOrWhiteSpace(unlockedLabel)
                ? "기술"
                : unlockedLabel.Trim();
    }

    [Serializable]
    public sealed class DodgeCooldownEffect : SkillNodeEffect
    {
        [Range(0f, 0.8f)] public float reductionPerRank = 0.08f;

        public override string Describe(int rank)
        {
            float reduction = reductionPerRank * Mathf.Max(0, rank);
            return $"회피 재사용 대기 -{reduction * 100f:0.#}%";
        }
    }

    [Serializable]
    public sealed class PassiveGrantEffect : SkillNodeEffect
    {
        public PassiveAbilitySO passive;

        public override string Describe(int rank)
        {
            string displayName = passive?.presentation?.displayName;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = "패시브";
            return rank > 0 && passive != null
                ? $"{displayName.Trim()} 활성"
                : $"{displayName.Trim()} 잠김";
        }
    }

    public readonly struct SkillStatModifierEntry
    {
        public AttributeId AttributeId { get; }
        public AttributeModifierOperation Operation { get; }
        public float Value { get; }

        public SkillStatModifierEntry(
            AttributeId attributeId,
            AttributeModifierOperation operation,
            float value)
        {
            AttributeId = attributeId;
            Operation = operation;
            Value = value;
        }

        public AttributeModifierValue ToRuntimeValue() =>
            new(AttributeId, Operation, Value);
    }

    [Serializable]
    public sealed class CharacterSkillProgressState
    {
        public CharacterActorType characterType;
        public int grantedUpToLevel;
        public int totalPoints;
        public int spentPoints;
        public List<SkillNodeRankEntry> takenNodes = new();
    }

    [Serializable]
    public sealed class SkillNodeRankEntry
    {
        public string nodeId;
        public int rank;
    }
}
