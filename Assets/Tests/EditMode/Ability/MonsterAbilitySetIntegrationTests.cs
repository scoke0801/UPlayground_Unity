using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Ability.Tests
{
    public sealed class MonsterAbilitySetIntegrationTests
    {
        [Test]
        public void 모든_몬스터_프로필은_AI_공격_AbilitySet을_가진다()
        {
            MonsterActorProfileSO[] profiles = AssetDatabase
                .FindAssets($"t:{nameof(MonsterActorProfileSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonsterActorProfileSO>)
                .Where(x => x != null)
                .ToArray();

            Assert.That(profiles, Is.Not.Empty);
            foreach (MonsterActorProfileSO profile in profiles)
            {
                Assert.That(
                    profile.abilitySet,
                    Is.Not.Null,
                    $"{profile.name}: AbilitySet 누락");
                Assert.That(
                    CountAiAttacks(profile.abilitySet),
                    Is.GreaterThan(0),
                    $"{profile.name}: BT가 선택할 공격 Ability가 없습니다.");
            }
        }

        [Test]
        public void 몬스터_Ability_Payload는_실행_가능한_공격_정보를_가진다()
        {
            AbilitySetSO[] sets = LoadAllAbilitySets()
                .Where(set => CountAiAttacks(set) > 0)
                .ToArray();

            Assert.That(sets, Is.Not.Empty);
            int validated = 0;
            foreach (AbilitySetSO set in sets)
            {
                foreach (GameplayAbilitySO ability in set.EnumerateAll().Distinct())
                {
                    if (ability?.variants == null)
                        continue;

                    foreach (var variant in ability.variants)
                    {
                        if (!UPlayGroundAbilityPayloadResolver.TryResolve(
                                variant,
                                out AnimKey animKey,
                                out var attackInfo)
                            || !attackInfo.aiSelectable)
                            continue;

                        Assert.That(
                            animKey,
                            Is.Not.EqualTo(AnimKey.None),
                            $"{ability.name}: AnimKey 누락");
                        Assert.That(
                            attackInfo.baseInfo?.hitPhases,
                            Is.Not.Null.And.Not.Empty,
                            $"{ability.name}: HitPhase 누락");
                        validated++;
                    }
                }
            }

            Assert.That(validated, Is.GreaterThan(0));
        }

        [Test]
        public void 몬스터_ActorDefinition은_프로필과_같은_AbilitySet을_사용한다()
        {
            ActorDefinitionSO[] definitions = AssetDatabase
                .FindAssets($"t:{nameof(ActorDefinitionSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(x => x != null && x.monsterProfile != null)
                .ToArray();

            Assert.That(definitions, Is.Not.Empty);
            foreach (ActorDefinitionSO definition in definitions)
            {
                Assert.That(
                    definition.EffectiveAbilitySet,
                    Is.SameAs(definition.monsterProfile.abilitySet),
                    $"{definition.name}: 프로필 AbilitySet 연결 불일치");
            }
        }

        private static AbilitySetSO[] LoadAllAbilitySets() =>
            AssetDatabase
                .FindAssets($"t:{nameof(AbilitySetSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AbilitySetSO>)
                .Where(x => x != null)
                .ToArray();

        private static int CountAiAttacks(AbilitySetSO set)
        {
            if (set == null)
                return 0;

            return set.EnumerateAll()
                .Where(x => x != null)
                .Distinct()
                .SelectMany(x => x.variants ?? Enumerable.Empty<AbilityVariantDefinition>())
                .Count(variant =>
                    UPlayGroundAbilityPayloadResolver.TryResolve(
                        variant,
                        out _,
                        out var attackInfo)
                    && attackInfo.aiSelectable);
        }
    }
}
