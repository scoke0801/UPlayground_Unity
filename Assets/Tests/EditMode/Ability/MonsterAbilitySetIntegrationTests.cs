using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Ability.Tests
{
    public sealed class MonsterAbilitySetIntegrationTests
    {
        [Test]
        public void 지상_몬스터_프리팹의_AIController는_활성화되어_있다()
        {
            ActorDefinitionSO[] definitions = AssetDatabase
                .FindAssets($"t:{nameof(ActorDefinitionSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(x => x != null && x.monsterProfile != null && x.prefab != null)
                .ToArray();

            Assert.That(definitions, Is.Not.Empty);

            var issues = new List<string>();
            foreach (ActorDefinitionSO definition in definitions)
            {
                EnemyAIController controller = definition.prefab.GetComponent<EnemyAIController>();
                if (controller != null && !controller.enabled)
                {
                    issues.Add(
                        $"{definition.name}: EnemyAIController가 비활성화되어 "
                        + "ManagedTick과 공격 쿨다운 진행이 멈춥니다.");
                }
            }

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

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
            var pendingAbilities = new List<string>();
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
                            payload.attackInfo?.motionKey ?? default;
                        bool isMotionResolved = motionKey.IsValid
                            && motionSets.Any(x => x.GetAbilityMotionAsset(motionKey) != null);
                        if (attackInfo.isMotionContentPending)
                        {
                            if (isMotionResolved)
                                issues.Add(DescribeStalePendingMark(ability.name, motionKey));
                            else
                                pendingAbilities.Add($"{ability.name} ('{motionKey}')");
                            continue;
                        }

                        if (!motionKey.IsValid)
                            issues.Add($"{ability.name}: Motion Key 누락");
                        else if (!isMotionResolved)
                        {
                            issues.Add(
                                $"{ability.name}: Motion Key '{motionKey}'를 "
                                + "해석할 Actor MotionSet 매핑 누락. 모션 확정 전이면 "
                                + "Payload의 모션 콘텐츠 보류를 켜세요.");
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

            if (pendingAbilities.Count > 0)
            {
                TestContext.WriteLine(
                    $"모션 콘텐츠 보류 공격 {pendingAbilities.Count}건 (AI 선택 제외):\n"
                    + string.Join("\n", pendingAbilities));
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

                        MotionKey motionKey = attackInfo.motionKey;
                        if (!motionKey.IsValid)
                            continue;

                        bool isMotionResolved =
                            motionSet.GetAbilityMotionAsset(motionKey) != null;
                        if (attackInfo.isMotionContentPending)
                        {
                            if (isMotionResolved)
                                issues.Add(DescribeStalePendingMark(
                                    $"{definition.name} / {ability.name}",
                                    motionKey));
                            continue;
                        }

                        validated++;
                        if (!isMotionResolved)
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
        public void Lian_몬스터_공격_MotionKey는_채찍_전용_Motion으로_해석된다()
        {
            const string prefabPath =
                "Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Lian_Whip.prefab";
            string[] abilitySetPaths =
            {
                "Assets/10.Datas/Ability/Actor/Humanoid_WhipAttackData/AbilitySet_Humanoid_WhipAttackData.asset",
                "Assets/10.Datas/Ability/Actor/Monster_Lian/AbilitySet_Monster_Lian.asset",
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, $"Lian 몬스터 프리팹 누락: {prefabPath}");

            ActorAnimator actorAnimator = prefab.GetComponent<ActorAnimator>();
            Assert.That(actorAnimator, Is.Not.Null, "Lian 몬스터의 ActorAnimator 누락");
            Assert.That(actorAnimator.SubAnimator, Is.Not.Null, "Lian 채찍 SubAnimator 연결 누락");
            Assert.That(actorAnimator.MotionSet, Is.Not.Null, "Lian 캐릭터 MotionSet 연결 누락");
            Assert.That(actorAnimator.SubAnimator.MotionSet, Is.Not.Null, "Lian 채찍 MotionSet 연결 누락");
            Assert.That(actorAnimator.SubAnimator.MotionSet.name, Is.EqualTo("WhipMotionSet"));

            int validated = 0;
            foreach (string abilitySetPath in abilitySetPaths)
            {
                AbilitySetSO abilitySet =
                    AssetDatabase.LoadAssetAtPath<AbilitySetSO>(abilitySetPath);
                Assert.That(abilitySet, Is.Not.Null, $"AbilitySet 누락: {abilitySetPath}");

                foreach (GameplayAbilitySO ability in abilitySet.EnumerateAll().Distinct())
                {
                    foreach (AbilityVariantDefinition variant in
                             ability?.variants
                             ?? Enumerable.Empty<AbilityVariantDefinition>())
                    {
                        if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                                variant,
                                out var attackInfo)
                            || !attackInfo.aiSelectable)
                            continue;

                        MotionKey motionKey = attackInfo.motionKey;
                        MotionSetAsset actorMotion =
                            actorAnimator.MotionSet.GetAbilityMotionAsset(motionKey);
                        MotionSetAsset weaponMotion =
                            actorAnimator.SubAnimator.MotionSet.GetAbilityMotionAsset(motionKey);

                        Assert.That(
                            actorMotion,
                            Is.Not.Null,
                            $"{ability.name}: 캐릭터 Motion Key '{motionKey}' 해석 실패");
                        Assert.That(
                            weaponMotion,
                            Is.Not.Null,
                            $"{ability.name}: 채찍 Motion Key '{motionKey}' 해석 실패");
                        Assert.That(
                            weaponMotion,
                            Is.Not.SameAs(actorMotion),
                            $"{ability.name}: 캐릭터용 MotionSetAsset이 채찍에 연결되어 있습니다.");
                        Assert.That(
                            weaponMotion.name,
                            Does.StartWith("Whip_"),
                            $"{ability.name}: 채찍 전용 MotionSetAsset이 아닙니다.");
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

        [Test]
        public void 모든_AI선택_공격은_명시적_카테고리를_가진다()
        {
            GameplayAbilitySO[] abilities = AssetDatabase
                .FindAssets($"t:{nameof(GameplayAbilitySO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>)
                .Where(x => x != null)
                .ToArray();

            var issues = new List<string>();
            foreach (GameplayAbilitySO ability in abilities)
            {
                foreach (AbilityVariantDefinition variant in
                         ability.variants
                         ?? Enumerable.Empty<AbilityVariantDefinition>())
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                            variant,
                            out var attackInfo)
                        || !attackInfo.aiSelectable
                        || attackInfo.attackCategory
                            != AbilityAttackCategory.None)
                        continue;

                    issues.Add(
                        $"{ability.name} / {variant?.variantId}: "
                        + "aiSelectable 공격의 attackCategory가 None입니다.");
                }
            }

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void Counter_AI공격은_Counter_역할을_가진다()
        {
            GameplayAbilitySO[] abilities = AssetDatabase
                .FindAssets($"t:{nameof(GameplayAbilitySO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>)
                .Where(x => x != null
                            && x.abilityId?.Contains(
                                ".Counter.",
                                System.StringComparison.Ordinal) == true)
                .ToArray();

            var issues = new List<string>();
            foreach (GameplayAbilitySO ability in abilities)
            {
                foreach (AbilityVariantDefinition variant in
                         ability.variants
                         ?? Enumerable.Empty<AbilityVariantDefinition>())
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                            variant,
                            out var attackInfo)
                        || !attackInfo.aiSelectable
                        || (attackInfo.aiRoles & AbilityAIRole.Counter) != 0)
                        continue;

                    issues.Add(
                        $"{ability.name} / {variant?.variantId}: "
                        + "Counter 역할이 없습니다.");
                }
            }

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        /// <summary>
        /// 모션을 연결하고 보류 표시를 끄지 않으면 런타임 AI가 그 공격을 계속 제외한다.
        /// 보류 표시가 콘텐츠 확정 뒤까지 남지 않도록 해석되는 보류 공격은 실패로 보고한다.
        /// </summary>
        private static string DescribeStalePendingMark(string owner, MotionKey motionKey) =>
            $"{owner}: Motion Key '{motionKey}'가 해석되는데 모션 콘텐츠 보류 표시가 "
            + "남아 있습니다. 보류를 끄지 않으면 AI가 이 공격을 선택하지 않습니다.";

        private static AbilitySetSO[] LoadAllAbilitySets() =>
            AssetDatabase
                .FindAssets($"t:{nameof(AbilitySetSO)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AbilitySetSO>)
                .Where(x => x != null)
                .ToArray();

        /// <summary>
        /// 모션 콘텐츠 보류 공격도 선언된 AI 공격으로 센다. 보류는 Payload에 명시되고 Motion
        /// 연결 테스트가 따로 추적하므로, 여기서는 프로필의 AbilitySet 연결 자체를 검증한다.
        /// </summary>
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
