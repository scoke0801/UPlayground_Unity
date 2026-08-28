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

        [Tooltip("저장된 노드 ID를 해석하는 스킬 트리 스키마 버전입니다. 노드 ID를 삭제하거나 의미를 바꿀 때 증가시킵니다.")]
        [Min(1)] public int skillTreeVersion = 1;

        [Tooltip("이전 버전의 노드 ID를 현재 ID로 옮기는 순차 마이그레이션 규칙입니다.")]
        public List<SkillNodeIdMigration> nodeIdMigrations = new();

        [Header("기본 전투 해금")]
        [Min(0)] public int initiallyUnlockedLightComboCount = 6;
        [Min(0)] public int initiallyUnlockedHeavyComboCount = 2;

        public List<SkillNodeDefinition> nodes = new();

        /// <summary>공격 체인별로 캐릭터 획득 즉시 사용할 수 있는 타수를 반환한다.</summary>
        public int GetInitiallyUnlockedComboCount(
            PlayerCombatAbilitySlot slot) =>
            slot switch
            {
                PlayerCombatAbilitySlot.LightCombo =>
                    Mathf.Max(0, initiallyUnlockedLightComboCount),
                PlayerCombatAbilitySlot.HeavyCombo =>
                    Mathf.Max(0, initiallyUnlockedHeavyComboCount),
                _ => 0,
            };

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

        /// <summary>저장 버전 이후의 이름 변경을 순서대로 적용해 현재 노드 ID를 반환한다.</summary>
        public string MigrateNodeId(string nodeId, int savedVersion)
        {
            string migratedId = nodeId?.Trim();
            if (string.IsNullOrEmpty(migratedId))
                return string.Empty;

            int currentVersion = Mathf.Max(1, savedVersion);
            int targetVersion = Mathf.Max(1, skillTreeVersion);
            while (currentVersion < targetVersion)
            {
                for (int i = 0; i < (nodeIdMigrations?.Count ?? 0); i++)
                {
                    SkillNodeIdMigration migration = nodeIdMigrations[i];
                    if (migration == null
                        || migration.fromVersion != currentVersion
                        || !string.Equals(
                            migration.oldNodeId?.Trim(),
                            migratedId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    migratedId = migration.newNodeId?.Trim() ?? string.Empty;
                    break;
                }
                currentVersion++;
            }
            return migratedId;
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

        /// <summary>비용 없이 기본 지급되는 단일 해금 노드인지 반환한다.</summary>
        public bool IsGrantedByDefault =>
            Mathf.Max(1, maxRank) == 1
            && effects?.Count == 1
            && effects[0] is AbilityUnlockEffect
            {
                grantedByDefault: true,
            };
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
        [Tooltip("캐릭터 획득 즉시 비용 없이 해금하고, 이 노드를 후속 노드의 충족된 선행 조건으로 취급합니다.")]
        public bool grantedByDefault;

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
        public List<SkillPointGrantEntry> bonusPointGrants = new();
        public List<SkillNodeRankEntry> takenNodes = new();
    }

    /// <summary>레벨에서 다시 계산할 수 없는 스킬 트리 원인 데이터만 저장한다.</summary>
    [Serializable]
    public sealed class CharacterSkillProgressSaveData
    {
        public int skillTreeVersion;
        public List<SkillPointGrantEntry> bonusPointGrants = new();
        public List<SkillNodeRankEntry> takenNodes = new();
    }

    /// <summary>한 스킬 트리 버전 단계에서 변경된 노드 ID를 다음 버전 ID로 연결한다.</summary>
    [Serializable]
    public sealed class SkillNodeIdMigration
    {
        [Min(1)] public int fromVersion = 1;
        public string oldNodeId;
        public string newNodeId;
    }

    /// <summary>레벨 외 출처에서 지급된 스킬 포인트의 원인을 보존한다.</summary>
    [Serializable]
    public sealed class SkillPointGrantEntry
    {
        public string sourceId;
        public int amount;
    }

    [Serializable]
    public sealed class SkillNodeRankEntry
    {
        public string nodeId;
        public int rank;
    }
}
