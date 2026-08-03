#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilitySetCompositionTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void 파생Set은_BaseAbility를_교체_제거하고_로컬Ability를_추가한다()
        {
            GameplayAbilitySO commonAttack = CreateAbility("Common.Attack");
            GameplayAbilitySO commonBuff = CreateAbility("Common.Buff");
            GameplayAbilitySO eliteAttack = CreateAbility("Elite.Attack");
            GameplayAbilitySO eliteSpecial = CreateAbility("Elite.Special");
            AbilitySetSO common = Create<AbilitySetSO>();
            common.additionalAbilities.Add(commonAttack);
            common.additionalAbilities.Add(commonBuff);
            AbilitySetSO derived = Create<AbilitySetSO>();
            derived.baseSet = common;
            derived.abilityOverrides.Add(Replace(commonAttack, eliteAttack));
            derived.abilityOverrides.Add(Remove(commonBuff));
            derived.additionalAbilities.Add(eliteSpecial);
            derived.RebuildRuntimeIndex();

            GameplayAbilitySO[] effective =
                derived.EnumerateAll().ToArray();

            Assert.That(
                effective,
                Is.EquivalentTo(new[] { eliteAttack, eliteSpecial }));
            Assert.That(derived.Contains(commonAttack), Is.False);
            Assert.That(derived.Contains(commonBuff), Is.False);
            Assert.That(derived.Contains(eliteAttack), Is.True);
        }

        [Test]
        public void 슬롯_전투_차지_콤보는_Base참조에_AbilityOverride를_적용한다()
        {
            GameplayAbilitySO commonAttack = CreateAbility("Common.Attack");
            GameplayAbilitySO eliteAttack = CreateAbility("Elite.Attack");
            AbilitySetSO common = Create<AbilitySetSO>();
            common.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
            {
                slot = PlayerSkillSlot.Ability,
                ability = commonAttack,
            });
            common.combatBindings.Add(new PlayerCombatAbilityBinding
            {
                slot = PlayerCombatAbilitySlot.LightCombo,
                abilities = new List<GameplayAbilitySO> { commonAttack },
            });
            common.charge.stages.Add(commonAttack);
            common.comboRoutes.Add(new AbilityComboRouteDefinition
            {
                routeId = "CommonRoute",
                ability = commonAttack,
            });
            AbilitySetSO derived = Create<AbilitySetSO>();
            derived.baseSet = common;
            derived.abilityOverrides.Add(Replace(commonAttack, eliteAttack));
            derived.RebuildRuntimeIndex();

            Assert.That(
                derived.GetPlayerAbility(PlayerSkillSlot.Ability),
                Is.SameAs(eliteAttack));
            Assert.That(
                derived.GetCombatAbility(PlayerCombatAbilitySlot.LightCombo),
                Is.SameAs(eliteAttack));
            Assert.That(
                derived.ResolveEffectiveChargeAbility(
                    derived.GetEffectiveCharge().stages[0]),
                Is.SameAs(eliteAttack));
            Assert.That(
                derived.ResolveEffectiveComboRouteAbility(
                    derived.GetEffectiveComboRoutes()[0].ability),
                Is.SameAs(eliteAttack));
            Assert.That(
                derived.TryGetPlayerSlot(
                    eliteAttack,
                    out PlayerSkillSlot slot),
                Is.True);
            Assert.That(slot, Is.EqualTo(PlayerSkillSlot.Ability));
        }

        [Test]
        public void 다단계_파생Set은_Override를_순서대로_합성한다()
        {
            GameplayAbilitySO common = CreateAbility("Common");
            GameplayAbilitySO elite = CreateAbility("Elite");
            GameplayAbilitySO boss = CreateAbility("Boss");
            AbilitySetSO baseSet = Create<AbilitySetSO>();
            baseSet.additionalAbilities.Add(common);
            AbilitySetSO eliteSet = Create<AbilitySetSO>();
            eliteSet.baseSet = baseSet;
            eliteSet.abilityOverrides.Add(Replace(common, elite));
            AbilitySetSO bossSet = Create<AbilitySetSO>();
            bossSet.baseSet = eliteSet;
            bossSet.abilityOverrides.Add(Replace(elite, boss));

            Assert.That(
                bossSet.EnumerateAll().Single(),
                Is.SameAs(boss));
        }

        [Test]
        public void BaseSet순환은_감지되고_열거가_무한재귀하지않는다()
        {
            AbilitySetSO first = Create<AbilitySetSO>();
            AbilitySetSO second = Create<AbilitySetSO>();
            first.baseSet = second;
            second.baseSet = first;

            Assert.That(first.HasInheritanceCycle(), Is.True);
            Assert.DoesNotThrow(() => first.EnumerateAll().ToArray());
        }

        [Test]
        public void ActorDefinition은_Profile공용Set에서_파생된Set만_특수Override로사용한다()
        {
            AbilitySetSO shared = Create<AbilitySetSO>();
            AbilitySetSO unrelatedLegacy = Create<AbilitySetSO>();
            AbilitySetSO derived = Create<AbilitySetSO>();
            derived.baseSet = shared;
            MonsterActorProfileSO profile = Create<MonsterActorProfileSO>();
            profile.abilitySet = shared;
            ActorDefinitionSO definition = Create<ActorDefinitionSO>();
            definition.monsterProfile = profile;
            definition.abilitySet = unrelatedLegacy;

            Assert.That(definition.EffectiveAbilitySet, Is.SameAs(shared));

            definition.abilitySet = derived;

            Assert.That(definition.EffectiveAbilitySet, Is.SameAs(derived));
        }

        private AbilitySetSO.AbilityOverrideEntry Replace(
            GameplayAbilitySO source,
            GameplayAbilitySO replacement) =>
            new()
            {
                sourceAbility = source,
                operation = AbilitySetOverrideOperation.Replace,
                replacementAbility = replacement,
            };

        private AbilitySetSO.AbilityOverrideEntry Remove(
            GameplayAbilitySO source) =>
            new()
            {
                sourceAbility = source,
                operation = AbilitySetOverrideOperation.Remove,
            };

        private GameplayAbilitySO CreateAbility(string id)
        {
            GameplayAbilitySO ability = Create<GameplayAbilitySO>();
            ability.abilityId = id;
            return ability;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            _objects.Add(value);
            return value;
        }
    }
}
#endif
