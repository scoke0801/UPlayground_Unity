using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;

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
            var issues = new List<string>();
            foreach (AbilitySetSO set in sets)
            {
                foreach (GameplayAbilitySO ability in set.EnumerateAll().Distinct())
                {
                    if (ability?.variants == null)
                        continue;

                    foreach (var variant in ability.variants)
                    {
                        if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                                variant,
                                out var attackInfo)
                            || !attackInfo.aiSelectable)
                            continue;

                        if (variant.executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                        {
                            issues.Add($"{ability.name}: UPlayGround 모션 Payload가 아닙니다.");
                            continue;
                        }

                        if (payload.attackInfo?.baseInfo?.motionRef == null)
                            issues.Add($"{ability.name}: MotionReference 누락");
                        else if (!payload.attackInfo.baseInfo.motionRef.HasAnyMotion)
                            issues.Add($"{ability.name}: 실행 가능한 MotionSetAsset 누락");
                        if (attackInfo.baseInfo?.hitPhases == null
                            || attackInfo.baseInfo.hitPhases.Count == 0)
                        {
                            issues.Add($"{ability.name}: HitPhase 누락");
                        }
                        validated++;
                    }
                }
            }

            Assert.That(validated, Is.GreaterThan(0));
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
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
                    UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                        variant,
                        out var attackInfo)
                    && attackInfo.aiSelectable);
        }
    }
}
