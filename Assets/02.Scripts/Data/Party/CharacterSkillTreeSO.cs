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
        [Min(0)] public int milestoneInterval = 5;
        [Min(0)] public int milestoneBonus = 1;

        public int TotalPointsAtLevel(int level)
        {
            int completedLevelUps = Mathf.Max(0, level - 1);
            int milestones = milestoneInterval > 0
                ? Mathf.Max(0, level) / milestoneInterval
                : 0;
            return completedLevelUps * Mathf.Max(0, perLevel)
                   + milestones * Mathf.Max(0, milestoneBonus);
        }
    }

    public enum SkillNodeBlockReason
    {
        None,
        InsufficientPoints,
        MissingPrerequisite,
        LevelTooLow,
        MaxRank,
        NotInSafeZone,
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
            $"{attributeId} {operation} {valuePerRank * Mathf.Max(0, rank):0.###}";
    }

    [Serializable]
    public sealed class AbilityScalarEffect : SkillNodeEffect
    {
        public string abilityId;
        public AbilityScalarKind kind;
        public ModifierType operation = ModifierType.Percent;
        public float valuePerRank;

        public override string Describe(int rank) =>
            $"{abilityId} {kind} {operation} {valuePerRank * Mathf.Max(0, rank):0.###}";
    }

    [Serializable]
    public sealed class AbilityUnlockEffect : SkillNodeEffect
    {
        public string abilityId;

        public override string Describe(int rank) =>
            rank > 0 ? $"{abilityId} 해금" : $"{abilityId} 잠김";
    }

    [Serializable]
    public sealed class PassiveGrantEffect : SkillNodeEffect
    {
        public PassiveAbilitySO passive;

        public override string Describe(int rank) =>
            rank > 0 && passive != null ? $"{passive.name} 부여" : "패시브 미지정";
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
