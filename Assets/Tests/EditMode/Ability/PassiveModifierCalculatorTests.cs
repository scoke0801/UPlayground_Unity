using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Ability.Tests
{
    public sealed class PassiveModifierCalculatorTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
                Object.DestroyImmediate(_created[i]);
            _created.Clear();
        }

        [Test]
        public void 상시_퍼센트와_곱연산을_결합한다()
        {
            CharacterPassiveSetSO set = CreateSet(
                CreatePassive(
                    PassiveScope.ActiveCharacter,
                    PassiveModifierType.LightAttackDamage,
                    ModifierType.Percent,
                    0.2f),
                CreatePassive(
                    PassiveScope.OwnerCharacter,
                    PassiveModifierType.LightAttackDamage,
                    ModifierType.Multiply,
                    1.5f));

            float result = PassiveModifierCalculator.CalculateMultiplier(
                set,
                PassiveModifierType.LightAttackDamage,
                null,
                PassiveScope.ActiveCharacter,
                PassiveScope.OwnerCharacter);

            Assert.That(result, Is.EqualTo(1.8f).Within(0.0001f));
        }

        [Test]
        public void 쿨다운_슬롯_필터를_구분한다()
        {
            PassiveAbilitySO passive = CreatePassive(
                PassiveScope.ActiveCharacter,
                PassiveModifierType.SkillCooldownDuration,
                ModifierType.Percent,
                -0.2f);
            passive.modifiers[0].abilitySlotFilter =
                PassiveAbilitySlotFilter.Ability;
            CharacterPassiveSetSO set = CreateSet(passive);

            float ability = PassiveModifierCalculator.CalculateMultiplier(
                set,
                PassiveModifierType.SkillCooldownDuration,
                PlayerSkillSlot.Ability);
            float ultimate = PassiveModifierCalculator.CalculateMultiplier(
                set,
                PassiveModifierType.SkillCooldownDuration,
                PlayerSkillSlot.Ultimate);

            Assert.That(ability, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(ultimate, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void 제작_비용은_최종_합계에서_올림한다()
        {
            Assert.That(
                PassiveModifierCalculator.CalculateIngredientCost(3, 2, 0.8f),
                Is.EqualTo(5));
        }

        [Test]
        public void 유리효과는_늘고_불리효과는_회복속도만큼_짧아진다()
        {
            Assert.That(
                PassiveModifierCalculator.CalculateEffectDuration(
                    10f, GameplayEffectPolarity.Beneficial, 1.2f),
                Is.EqualTo(12f).Within(0.0001f));
            Assert.That(
                PassiveModifierCalculator.CalculateEffectDuration(
                    10f, GameplayEffectPolarity.Harmful, 2f),
                Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void 대표_패시브는_보유목록_중_중복없이_두개만_노출한다()
        {
            PassiveAbilitySO first = CreatePassive(
                PassiveScope.ActiveCharacter,
                PassiveModifierType.ExperienceGain,
                ModifierType.Percent,
                0.1f);
            PassiveAbilitySO second = CreatePassive(
                PassiveScope.ActiveCharacter,
                PassiveModifierType.ExperienceGain,
                ModifierType.Percent,
                0.2f);
            PassiveAbilitySO notOwned = CreatePassive(
                PassiveScope.ActiveCharacter,
                PassiveModifierType.ExperienceGain,
                ModifierType.Percent,
                0.3f);
            CharacterPassiveSetSO set = CreateSet(first, second);
            set.characterSelectRepresentatives =
                new List<PassiveAbilitySO> { first, first, notOwned, second };

            PassiveAbilitySO[] result =
                set.EnumerateCharacterSelectRepresentatives().ToArray();

            CollectionAssert.AreEqual(
                new[] { first, second },
                result);
        }

        private PassiveAbilitySO CreatePassive(
            PassiveScope scope,
            PassiveModifierType type,
            ModifierType operation,
            float value)
        {
            var passive = ScriptableObject.CreateInstance<PassiveAbilitySO>();
            _created.Add(passive);
            passive.activationType = PassiveActivationType.Always;
            passive.scope = scope;
            passive.modifiers = new List<PassiveModifierDefinition>
            {
                new()
                {
                    modifierType = type,
                    operation = operation,
                    value = value,
                },
            };
            return passive;
        }

        private CharacterPassiveSetSO CreateSet(params PassiveAbilitySO[] passives)
        {
            var set = ScriptableObject.CreateInstance<CharacterPassiveSetSO>();
            _created.Add(set);
            set.characterType = CharacterActorType.Bokusei;
            set.passives = passives.ToList();
            return set;
        }
    }
}
