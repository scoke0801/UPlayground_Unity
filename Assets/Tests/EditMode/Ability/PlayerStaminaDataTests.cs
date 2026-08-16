using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Components;
using UPlayGround.State;

namespace UPlayGround.Ability.Tests
{
    public sealed class PlayerStaminaDataTests
    {
        private const string PlayerAbilityRoot =
            "Assets/10.Datas/Ability/Migrated";

        [Test]
        public void 스태미나_Attribute와_이동정책이_유효하다()
        {
            Assert.That(
                AttributeRegistry.TryGetDefinition(
                    Attributes.Resource.Stamina,
                    out AttributeRegistryEntry current),
                Is.True);
            Assert.That(current.maximumAttributeId,
                Is.EqualTo(Attributes.Resource.MaxStamina.AttributeId));
            Assert.That(current.saveBaseValue, Is.True);

            Assert.That(
                AttributeRegistry.TryGetDefinition(
                    Attributes.Resource.MaxStamina,
                    out AttributeRegistryEntry maximum),
                Is.True);
            Assert.That(maximum.defaultBaseValue, Is.EqualTo(100f));
            Assert.That(maximum.maxChangePolicy,
                Is.EqualTo(AttributeMaxChangePolicy.FillOnIncrease));

            PlayerStaminaSettingsSO settings = PlayerStaminaSettingsSO.Load();
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.dashCost, Is.GreaterThan(0f));
            Assert.That(settings.dodgeCost, Is.GreaterThan(0f));
            Assert.That(settings.sprintCostPerSecond, Is.GreaterThan(0f));
            Assert.That(settings.recoveryPerSecond, Is.GreaterThan(0f));
        }

        [Test]
        public void 모든_플레이어_강공격은_스태미나_GAS비용을_사용한다()
        {
            HashSet<GameplayAbilitySO> abilities = LoadHeavyAbilities();
            Assert.That(abilities, Is.Not.Empty);

            foreach (GameplayAbilitySO ability in abilities)
            {
                Assert.That(ability.cost, Is.Not.Null, ability.name);
                Assert.That(ability.cost.resourceType,
                    Is.EqualTo(AbilityResourceType.Stamina), ability.name);
                Assert.That(ability.cost.policy,
                    Is.EqualTo(AbilityCostPolicy.Fixed), ability.name);
                Assert.That(ability.cost.value,
                    Is.GreaterThan(0f), ability.name);
            }

            List<AbilityValidationIssue> issues =
                AbilityDataValidator.ValidateAll();
            for (int i = 0; i < issues.Count; i++)
            {
                AbilityValidationIssue issue = issues[i];
                if (issue.Severity != AbilityValidationSeverity.Error
                    || issue.Context is not GameplayAbilitySO ability
                    || !abilities.Contains(ability))
                    continue;
                Assert.Fail($"{ability.name}: {issue.Message}");
            }
        }

        [Test]
        public void 모든_파티원은_최대스태미나를_강화할수있다()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:CharacterSkillTreeSO",
                new[] { "Assets/10.Datas/Party/SkillTree" });
            Assert.That(guids, Is.Not.Empty);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterSkillTreeSO tree =
                    AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(path);
                SkillNodeDefinition node = tree.FindNode(
                    $"Stat.{GrowthAttributeCatalog.StaminaId}");
                Assert.That(node, Is.Not.Null, path);
                Assert.That(node.maxRank, Is.GreaterThan(0), path);

                StatDeltaEffect staminaEffect = null;
                for (int effectIndex = 0; effectIndex < node.effects.Count; effectIndex++)
                {
                    if (node.effects[effectIndex] is StatDeltaEffect effect
                        && effect.AttributeId == GrowthAttributeCatalog.Stamina)
                    {
                        staminaEffect = effect;
                        break;
                    }
                }
                Assert.That(staminaEffect, Is.Not.Null, path);
                Assert.That(staminaEffect.valuePerRank, Is.GreaterThan(0f), path);
            }
        }

        [Test]
        public void 스태미나_소모행동중에는_회복을_차단한다()
        {
            ActorStateId[] blockedStates =
            {
                ActorStateId.Attack,
                ActorStateId.Charge,
                ActorStateId.Dash,
                ActorStateId.DashAttack,
                ActorStateId.Dodge,
                ActorStateId.FinishAttack,
                ActorStateId.JumpAttack,
                ActorStateId.JumpDashAttack,
                ActorStateId.SpecialBreakAttack,
                ActorStateId.Ultimate,
            };
            for (int i = 0; i < blockedStates.Length; i++)
                Assert.That(
                    PlayerStaminaRuntime.IsRecoveryBlockedState(
                        blockedStates[i]),
                    Is.True,
                    blockedStates[i].ToString());

            Assert.That(
                PlayerStaminaRuntime.IsRecoveryBlockedState(
                    ActorStateId.Idle),
                Is.False);
            Assert.That(
                PlayerStaminaRuntime.IsRecoveryBlockedState(
                    ActorStateId.GroundMove),
                Is.False);
        }

        private static HashSet<GameplayAbilitySO> LoadHeavyAbilities()
        {
            string[] setGuids = AssetDatabase.FindAssets(
                "t:AbilitySetSO",
                new[] { PlayerAbilityRoot });
            var abilities = new HashSet<GameplayAbilitySO>();
            for (int i = 0; i < setGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(setGuids[i]);
                if (!Path.GetFileNameWithoutExtension(path)
                        .StartsWith("AbilitySet_Player", StringComparison.Ordinal))
                    continue;

                AbilitySetSO set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(path);
                IReadOnlyList<GameplayAbilitySO> heavySequence =
                    set.GetCombatSequence(PlayerCombatAbilitySlot.HeavyCombo);
                for (int j = 0; j < heavySequence.Count; j++)
                    if (heavySequence[j] != null)
                        abilities.Add(heavySequence[j]);

                IReadOnlyList<GameplayAbilitySO> jumpSequence =
                    set.GetCombatSequence(PlayerCombatAbilitySlot.JumpCombo);
                if (jumpSequence.Count > 0
                    && jumpSequence[jumpSequence.Count - 1] != null)
                    abilities.Add(jumpSequence[jumpSequence.Count - 1]);

                PlayerChargeAbilitySettings charge = set.GetEffectiveCharge();
                if (charge != null)
                {
                    for (int j = 0; j < charge.stages.Count; j++)
                    {
                        GameplayAbilitySO ability =
                            set.ResolveEffectiveChargeAbility(charge.stages[j]);
                        if (ability != null) abilities.Add(ability);
                    }
                }

                IReadOnlyList<AbilityComboRouteDefinition> routes =
                    set.GetEffectiveComboRoutes();
                for (int j = 0; j < routes.Count; j++)
                {
                    AbilityComboRouteDefinition route = routes[j];
                    if (route?.inputPattern == null
                        || route.inputPattern.Count == 0)
                        continue;
                    ComboInputToken last =
                        route.inputPattern[route.inputPattern.Count - 1];
                    if (last is not ComboInputToken.HeavyAttack
                        and not ComboInputToken.Charge)
                        continue;
                    GameplayAbilitySO routeAbility =
                        set.ResolveEffectiveComboRouteAbility(route.ability);
                    GameplayAbilitySO enhancedAbility =
                        set.ResolveEffectiveComboRouteAbility(
                            route.enhancedAbility);
                    if (routeAbility != null) abilities.Add(routeAbility);
                    if (enhancedAbility != null) abilities.Add(enhancedAbility);
                }
            }
            return abilities;
        }
    }
}
