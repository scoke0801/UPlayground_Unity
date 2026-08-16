using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    public readonly struct PartyCombatPowerResult
    {
        public CharacterActorType CharacterType { get; }
        public int Level { get; }
        public long CombatPower { get; }
        public IReadOnlyDictionary<AttributeId, float> Stats { get; }

        public PartyCombatPowerResult(
            CharacterActorType characterType,
            int level,
            long combatPower,
            IReadOnlyDictionary<AttributeId, float> stats)
        {
            CharacterType = characterType;
            Level = level;
            CombatPower = combatPower;
            Stats = stats;
        }
    }

    /// <summary>
    /// 파티 기본 Attribute와 전투력을 계산하는 순수 계산기.
    /// 런타임 버프, 장비, 일시 수정자는 포함하지 않는다.
    /// </summary>
    public static class PartyPowerCalculator
    {
        public static Dictionary<AttributeId, float> CalculateBaseStats(
            PartyMemberGrowthSO growthData)
        {
            var attributes = new Dictionary<AttributeId, float>();

            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
            {
                float baseValue = growthData?.baseProfile != null
                                  && growthData.baseProfile.TryGetBaseValue(
                                      attributeId, out float profileValue)
                    ? profileValue
                    : UPlayGroundAttributeDefaults.Get(attributeId);

                attributes[attributeId] = baseValue;
            }

            return attributes;
        }

        public static long CalculateCombatPower(
            IReadOnlyDictionary<AttributeId, float> attributes)
        {
            if (attributes == null) return 0L;

            float maxHealth = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            float attackPower = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower));
            float defense = Mathf.Clamp01(
                Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.Defense));
            float critRate = Mathf.Clamp01(
                Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.CritRate));
            float critMultiplier = Mathf.Max(
                1f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.CritMultiplier));
            float attackSpeed = Mathf.Max(
                0.1f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Combat.AttackSpeed));
            float maxPoise = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise));
            float generation = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Resource.GenerationMultiplier));
            float moveSpeed = Mathf.Max(
                0f, Get(attributes, global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed));

            float effectiveAttack = attackPower
                                    * (1f + critRate * Mathf.Max(0f, critMultiplier - 1f))
                                    * attackSpeed
                                    * generation;
            float effectiveHealth = maxHealth / Mathf.Max(0.1f, 1f - defense);
            float utility = maxPoise * 0.25f
                            + Mathf.Max(0f, moveSpeed - 1f) * 100f;
            float combatPower = effectiveHealth * 0.35f
                                + effectiveAttack * 100f * 0.55f
                                + utility * 0.10f;

            return Math.Max(
                0L,
                (long)Math.Round(
                    combatPower, MidpointRounding.AwayFromZero));
        }

        public static PartyCombatPowerResult Calculate(
            CharacterActorType characterType,
            PartyMemberGrowthSO growthData,
            int level)
        {
            Dictionary<AttributeId, float> attributes =
                CalculateBaseStats(growthData);
            return new PartyCombatPowerResult(
                characterType,
                Math.Max(1, level),
                CalculateCombatPower(attributes),
                attributes);
        }

        private static float Get(
            IReadOnlyDictionary<AttributeId, float> attributes,
            AttributeId attributeId) =>
            attributes.TryGetValue(attributeId, out float value)
                ? value
                : UPlayGroundAttributeDefaults.Get(attributeId);
    }
}
