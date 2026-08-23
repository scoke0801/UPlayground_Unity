#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilityDataIntegrityTests
    {
        [Test]
        public void 모든_AttributeProfile은_고유한_AttributeId와_필수값을_가진다()
        {
            string[] guids = AssetDatabase.FindAssets("t:AttributeProfileSO");
            var failures = new List<string>();
            Assert.That(guids, Is.Not.Empty, "Attribute Profile 데이터가 없습니다.");

            for (int assetIndex = 0; assetIndex < guids.Length; assetIndex++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[assetIndex]);
                AttributeProfileSO profile =
                    AssetDatabase.LoadAssetAtPath<AttributeProfileSO>(path);
                if (profile == null)
                {
                    failures.Add($"{path}: 로드 실패");
                    continue;
                }

                var values = new Dictionary<AttributeId, float>();
                if (!profile.TryCopyBaseValues(values, out string error))
                {
                    failures.Add($"{path}: {error}");
                    continue;
                }

                foreach (AttributeId attributeId in
                         UPlayGroundAttributeDefaults.ProfileAttributes)
                    if (!values.ContainsKey(attributeId))
                        failures.Add($"{path}: 필수 Attribute 누락 {attributeId}");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void 모든_GameplayAbility는_TaskGraph_또는_Request트리거를_가진다()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:GameplayAbilitySO", new[] { "Assets/10.Datas/Ability" });
            var failures = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameplayAbilitySO ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                if (ability == null)
                    failures.Add($"{path}: Ability 로드 실패");
                else if (IsRequestDrivenAbility(ability))
                    continue;
                else if (ability.taskGraph == null)
                    failures.Add($"{path}: TaskGraph 누락");
                else if (ability.taskGraph.Root == null)
                    failures.Add($"{path}: TaskGraph Root 누락");
                else if (ability.taskGraph.Root is not WaitMotionSetEndAbilityTask)
                    failures.Add($"{path}: 미지원 Root {ability.taskGraph.Root.GetType().Name}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void 태그_트리거_마이그레이션_데이터는_유효_AbilitySet에_한번씩_연결된다()
        {
            const string monsterSetRoot = "Assets/10.Datas/Ability/Actor";
            const string monsterTriggerRoot =
                "Assets/10.Datas/Ability/Actor/TagTriggers";
            const string playerSetRoot = "Assets/10.Datas/Ability/Migrated";
            const string playerTriggerRoot =
                "Assets/10.Datas/Ability/Migrated/TagTriggers";

            List<GameplayAbilitySO> monsterTriggers = LoadAssets<GameplayAbilitySO>(
                monsterTriggerRoot);
            List<GameplayAbilitySO> playerTriggers = LoadAssets<GameplayAbilitySO>(
                playerTriggerRoot);
            List<AbilitySetSO> monsterSets = LoadAssets<AbilitySetSO>(monsterSetRoot);
            List<AbilitySetSO> playerSets = LoadAssets<AbilitySetSO>(playerSetRoot);

            Assert.That(monsterTriggers, Has.Count.EqualTo(12));
            Assert.That(playerTriggers, Has.Count.EqualTo(9));
            Assert.That(monsterSets, Is.Not.Empty);
            Assert.That(playerSets, Is.Not.Empty);

            AssertRequestTriggerDefinitions(monsterTriggers, "Trigger.Monster.");
            AssertRequestTriggerDefinitions(playerTriggers, "Trigger.Player.Hit.");

            foreach (AbilitySetSO set in monsterSets)
            {
                AssertMigrationOwnerIsCurrent(set, monsterTriggers);
                AssertEffectiveSetContainsTriggersExactlyOnce(set, monsterTriggers);
                Assert.That(
                    EnumerateEffectiveAdditionalWithMultiplicity(set)
                        .Count(playerTriggers.Contains),
                    Is.Zero,
                    set.name);
            }

            foreach (AbilitySetSO set in playerSets)
            {
                AssertMigrationOwnerIsCurrent(set, playerTriggers);
                AssertEffectiveSetContainsTriggersExactlyOnce(set, playerTriggers);
                Assert.That(
                    EnumerateEffectiveAdditionalWithMultiplicity(set)
                        .Count(monsterTriggers.Contains),
                    Is.Zero,
                    set.name);
                Assert.That(
                    set.playerSlots.Any(slot =>
                        slot != null && playerTriggers.Contains(slot.ability)),
                    Is.False,
                    $"{set.name}: Request 트리거 Ability가 입력 슬롯에도 연결됨");
            }
        }

        [TestCase(
            "Assets/10.Datas/Ability/Migrated/PlayerKatanaAttackData/GA_PlayerKatanaAttackData_Ultimate.asset",
            CharacterActorType.Raon,
            1)]
        [TestCase(
            "Assets/10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/GA_PlayerDoubleAxeAttackData_Ultimate.asset",
            CharacterActorType.Hwarin,
            2)]
        public void 시퀀스형_Ultimate는_Variant_Payload가_시퀀스를_소유한다(
            string abilityPath,
            CharacterActorType expectedOwner,
            int expectedEventCount)
        {
            GameplayAbilitySO ability =
                AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(abilityPath);
            UPlayGroundUltimateAbilityPayloadSO payload =
                AssetDatabase.LoadAllAssetsAtPath(abilityPath)
                    .OfType<UPlayGroundUltimateAbilityPayloadSO>()
                    .SingleOrDefault();

            Assert.That(ability, Is.Not.Null, abilityPath);
            Assert.That(payload, Is.Not.Null, $"{abilityPath}: Ultimate Payload 누락");
            Assert.That(payload.sequence, Is.Not.Null, $"{abilityPath}: Sequence 누락");
            Assert.That(payload.sequence.ownerType, Is.EqualTo(expectedOwner));
            Assert.That(
                payload.sequence.events,
                Has.Count.EqualTo(expectedEventCount),
                $"{abilityPath}: Ultimate managed reference 이벤트 유실");
            Assert.That(
                payload.sequence.events,
                Has.None.Null,
                $"{abilityPath}: Ultimate managed reference 타입 복원 실패");
            Assert.That(payload.attackInfo?.motionKey.IsValid, Is.True);

            AbilityVariantDefinition variant = ability.variants.Single();
            Assert.That(variant.executionPayload, Is.SameAs(payload));
            Assert.That(UPlayGroundAbilityPayloadResolver.IsExecutable(variant), Is.True);
            Assert.That(
                UPlayGroundUltimateAbilityPayloadResolver.TryResolve(
                    variant,
                    out UPlayGroundUltimateAbilityPayloadSO resolved),
                Is.True);
            Assert.That(resolved, Is.SameAs(payload));
        }

        [TestCase("Bow.Ability.Skill.4", 3, 155f, 465f, 166f)]
        [TestCase("Bow.Ability.Skill.5", 4, 210f, 630f, 225f)]
        public void Bow_다중투사체는_Damage_Poise_Break_총량을_보존한다(
            string motionKey,
            int expectedPhaseCount,
            float expectedDamage,
            float expectedPoiseDamage,
            float expectedBreakDamage)
        {
            const string path =
                "Assets/10.Datas/Ability/Migrated/PlayerBowAttackData/GA_PlayerBowAttackData_Ability.asset";
            UPlayGroundMotionAbilityPayloadSO payload =
                AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<UPlayGroundMotionAbilityPayloadSO>()
                    .SingleOrDefault(value =>
                        value.attackInfo?.motionKey.value == motionKey);

            Assert.That(payload, Is.Not.Null, $"{motionKey}: Payload 누락");
            List<HitPhaseData> phases = payload.attackInfo.baseInfo.hitPhases;
            Assert.That(phases, Has.Count.EqualTo(expectedPhaseCount));
            Assert.That(phases.Sum(value => value.damage),
                Is.EqualTo(expectedDamage).Within(0.001f));
            Assert.That(phases.Sum(value => value.poiseDamage),
                Is.EqualTo(expectedPoiseDamage).Within(0.001f));
            Assert.That(phases.Sum(value => value.breakDamage),
                Is.EqualTo(expectedBreakDamage).Within(0.001f));
        }

        private static bool IsRequestDrivenAbility(GameplayAbilitySO ability) =>
            ability?.triggers != null
            && ability.triggers.Count > 0
            && ability.triggers.All(trigger =>
                trigger != null
                && trigger.mode == AbilityTriggerActivationMode.Request);

        private static List<T> LoadAssets<T>(string root)
            where T : UnityEngine.Object => AssetDatabase
                .FindAssets($"t:{typeof(T).Name}", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();

        private static void AssertRequestTriggerDefinitions(
            IReadOnlyList<GameplayAbilitySO> abilities,
            string tagPrefix)
        {
            var tags = new HashSet<string>();
            for (int i = 0; i < abilities.Count; i++)
            {
                GameplayAbilitySO ability = abilities[i];
                Assert.That(ability.taskGraph, Is.Null, ability.name);
                Assert.That(ability.triggers, Has.Count.EqualTo(1), ability.name);
                Assert.That(ability.variants, Has.Count.EqualTo(1), ability.name);
                Assert.That(
                    ability.variants[0].executionPayload,
                    Is.Null,
                    ability.name);

                AbilityTriggerDefinition trigger = ability.triggers[0];
                Assert.That(trigger.source, Is.EqualTo(AbilityTriggerSource.GameplayEvent));
                Assert.That(trigger.mode, Is.EqualTo(AbilityTriggerActivationMode.Request));
                Assert.That(trigger.matchMode, Is.EqualTo(AbilityTagMatchMode.Exact));
                bool hitReaction = trigger.triggerTag.TagName.Contains(".Hit.");
                Assert.That(
                    trigger.allowPreemption,
                    Is.EqualTo(hitReaction),
                    $"{ability.name}: 피격 리액션만 명시적 선점을 허용해야 합니다.");
                Assert.That(
                    trigger.triggerTag.TagName.StartsWith(tagPrefix),
                    Is.True,
                    ability.name);
                Assert.That(tags.Add(trigger.triggerTag.TagName), Is.True, ability.name);
            }
        }

        private static void AssertMigrationOwnerIsCurrent(
            AbilitySetSO set,
            IReadOnlyList<GameplayAbilitySO> triggers)
        {
            bool ownsTrigger = set.additionalAbilities != null
                               && set.additionalAbilities.Any(triggers.Contains);
            if (set.baseSet == null || ownsTrigger)
            {
                Assert.That(
                    set.tagTriggerMigrationVersion,
                    Is.EqualTo(1),
                    $"{set.name}: 태그 트리거를 직접 소유하는 Set의 마이그레이션 버전");
            }
        }

        private static void AssertEffectiveSetContainsTriggersExactlyOnce(
            AbilitySetSO set,
            IReadOnlyList<GameplayAbilitySO> triggers)
        {
            List<GameplayAbilitySO> effective =
                EnumerateEffectiveAdditionalWithMultiplicity(set).ToList();
            for (int i = 0; i < triggers.Count; i++)
            {
                GameplayAbilitySO trigger = triggers[i];
                Assert.That(
                    effective.Count(ability => ability == trigger),
                    Is.EqualTo(1),
                    $"{set.name}: {trigger.name} 유효 연결 수");
            }
        }

        /// <summary>
        /// 런타임과 같은 Base → Override → Local 순서로 additionalAbilities를 합성한다.
        /// 중복 검출이 목적이므로 AbilitySetSO.EnumerateAll의 HashSet 중복 제거는 사용하지 않는다.
        /// </summary>
        private static IEnumerable<GameplayAbilitySO>
            EnumerateEffectiveAdditionalWithMultiplicity(AbilitySetSO set)
        {
            return EnumerateEffectiveAdditionalWithMultiplicity(
                set,
                new HashSet<AbilitySetSO>());
        }

        private static IEnumerable<GameplayAbilitySO>
            EnumerateEffectiveAdditionalWithMultiplicity(
                AbilitySetSO set,
                HashSet<AbilitySetSO> visited)
        {
            if (set == null || !visited.Add(set))
                yield break;

            if (set.baseSet != null)
            {
                foreach (GameplayAbilitySO inherited in
                         EnumerateEffectiveAdditionalWithMultiplicity(
                             set.baseSet,
                             visited))
                {
                    GameplayAbilitySO resolved =
                        set.ResolveInheritedAbility(inherited);
                    if (resolved != null)
                        yield return resolved;
                }
            }

            for (int i = 0; i < (set.additionalAbilities?.Count ?? 0); i++)
            {
                GameplayAbilitySO local = set.additionalAbilities[i];
                if (local != null)
                    yield return local;
            }
        }
    }
}
#endif
