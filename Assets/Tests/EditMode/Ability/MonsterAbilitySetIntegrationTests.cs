using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;

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
            ActorAnimationMotionSet[] motionSets = AssetDatabase
                .FindAssets($"t:{nameof(ActorAnimationMotionSet)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>)
                .Where(x => x != null)
                .ToArray();
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

                        MotionKey motionKey =
                            payload.attackInfo?.baseInfo?.motionKey ?? default;
                        if (!motionKey.IsValid)
                            issues.Add($"{ability.name}: Motion Key 누락");
                        else if (!motionSets.Any(x =>
                                     x.GetAbilityMotionAsset(motionKey) != null))
                        {
                            issues.Add(
                                $"{ability.name}: Motion Key '{motionKey}'를 "
                                + "해석할 Actor MotionSet 매핑 누락");
                        }
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

        /// <summary>
        /// 위 테스트는 "프로젝트 어딘가의 MotionSet"에 매핑만 있으면 통과하므로,
        /// 매핑이 엉뚱한 액터에만 있는 경우를 잡지 못한다. 런타임 EnemyCombat은 자기 액터의
        /// MotionSet으로만 해석하므로, 액터 단위로 실제 해석 가능 여부를 확인한다.
        /// </summary>
        [Test]
        public void 몬스터_AI공격_MotionKey는_그_액터의_MotionSet에서_해석된다()
        {
            ActorDefinitionSO[] definitions = AssetDatabase
                .FindAssets($"t:{nameof(ActorDefinitionSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(x => x != null
                            && x.monsterProfile != null
                            && x.EffectiveAbilitySet != null
                            && x.prefab != null)
                .ToArray();

            Assert.That(definitions, Is.Not.Empty);

            int validated = 0;
            var issues = new List<string>();
            foreach (ActorDefinitionSO definition in definitions)
            {
                ActorAnimator animator =
                    definition.prefab.GetComponentInChildren<ActorAnimator>(true);
                ActorAnimationMotionSet motionSet = animator != null
                    ? animator.MotionSet
                    : null;
                if (motionSet == null)
                {
                    issues.Add(
                        $"{definition.name}: prefab에서 ActorAnimator의 "
                        + "ActorAnimationMotionSet을 찾지 못했습니다.");
                    continue;
                }

                foreach (GameplayAbilitySO ability in
                         definition.EffectiveAbilitySet.EnumerateAll().Distinct())
                {
                    if (ability?.variants == null)
                        continue;

                    foreach (AbilityVariantDefinition variant in ability.variants)
                    {
                        if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                                variant,
                                out var attackInfo)
                            || !attackInfo.aiSelectable)
                            continue;

                        MotionKey motionKey =
                            attackInfo.baseInfo?.motionKey ?? default;
                        if (!motionKey.IsValid)
                            continue;

                        validated++;
                        if (motionSet.GetAbilityMotionAsset(motionKey) == null)
                            issues.Add(
                                $"{definition.name} / {ability.name}: Motion Key "
                                + $"'{motionKey}'를 '{motionSet.name}'에서 해석할 수 "
                                + "없습니다. 다른 액터 MotionSet에만 매핑되어 있으면 "
                                + "런타임에서 이 공격이 선택 후보에서 조용히 빠집니다.");
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
