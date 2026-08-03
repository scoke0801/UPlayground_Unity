#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
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
                         UPlayGroundAttributeDefaults.All)
                    if (!values.ContainsKey(attributeId))
                        failures.Add($"{path}: 필수 Attribute 누락 {attributeId}");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void 모든_GameplayAbility는_실행가능한_TaskGraph를_가진다()
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
                else if (ability.taskGraph == null)
                    failures.Add($"{path}: TaskGraph 누락");
                else if (ability.taskGraph.Root == null)
                    failures.Add($"{path}: TaskGraph Root 누락");
                else if (ability.taskGraph.Root is not WaitMotionSetEndAbilityTask)
                    failures.Add($"{path}: 미지원 Root {ability.taskGraph.Root.GetType().Name}");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [TestCase(
            "Assets/10.Datas/Ability/Migrated/PlayerKatanaAttackData/GA_PlayerKatanaAttackData_Ultimate.asset",
            CharacterActorType.Bokusei,
            1)]
        [TestCase(
            "Assets/10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/GA_PlayerDoubleAxeAttackData_Ultimate.asset",
            CharacterActorType.Honoka,
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
    }
}
#endif
