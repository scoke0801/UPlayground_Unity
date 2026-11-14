using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Combat;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>실제 장비 사다리·평균 롤 기여도·런타임 범위 제한의 일치를 검증한다.</summary>
    public sealed class ItemEconomyBalanceVerificationTests
    {
        [Test]
        public void ActualData_HasCompleteIncreasingChainsAndPreservedReferences()
        {
            Assert.That(ItemEconomyBalanceVerification.ValidateData(), Is.Empty);
        }

        [TestCase(1, 1)]
        [TestCase(1, 5)]
        [TestCase(2, 10)]
        [TestCase(3, 15)]
        [TestCase(3, 20)]
        [TestCase(4, 25)]
        [TestCase(4, 30)]
        [TestCase(5, 40)]
        [TestCase(5, 50)]
        public void AllCharacters_AverageFixedRolls_BothAxesMeetTarget(int band, int level)
        {
            var characters = AssetDatabase.FindAssets("t:PartyMemberGrowthSO", new[] { "Assets/10.Datas/Party/Growth" })
                .Select(g => AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.That(characters.Length, Is.GreaterThan(0));
            foreach (var character in characters)
            {
                var result = ItemEconomyBalanceVerification.Analyze(band, level, 0, character.characterType);
                Assert.That(result.attack, Is.InRange(.25, .35), $"{character.characterType} 공격 기여도");
                Assert.That(result.survival, Is.InRange(.25, .35), $"{character.characterType} 생존 기여도");
            }
        }

        [TestCase(1, 1)] [TestCase(1, 5)] [TestCase(2, 10)] [TestCase(3, 15)] [TestCase(3, 20)]
        [TestCase(4, 25)] [TestCase(4, 30)] [TestCase(5, 40)] [TestCase(5, 50)]
        public void AllGrades_TimeToKill_MeetsUnchangedPolicy(int band, int level)
        {
            var result = ItemEconomyBalanceVerification.Analyze(band, level);
            var scaling = AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(ItemEconomyCombatCalibration.ScalingPath);
            var targets = Resources.Load<CombatBalanceTargetsSO>(CombatBalanceTargetsSO.ResourceKey);
            foreach (MonsterActorGrade grade in System.Enum.GetValues(typeof(MonsterActorGrade)))
            {
                var stats = MonsterStatCalculator.Calculate(scaling, grade, level);
                double ttk = stats[GrowthAttributeCatalog.Health] / ((1 - stats[GrowthAttributeCatalog.Defense]) * result.expectedDps);
                double target = targets.GetTargetSeconds(grade);
                Assert.That(ttk, Is.InRange(target * .8, target * 1.2), grade.ToString());
            }
        }

        [Test]
        public void BakedProfiles_UseCurrentSourceHealth()
        {
            var source = AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(ItemEconomyCombatCalibration.ScalingPath);
            var actors = AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(g => AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null && (a.actorType & ActorType.Monster) != 0 && a.EffectiveMonsterScaling == source).ToArray();
            Assert.That(actors.Length, Is.GreaterThan(0));
            foreach (var actor in actors)
            {
                Assert.That(actor.attributeProfile.TryGetBaseValue(GrowthAttributeCatalog.Health, out float health), Is.True, actor.actorId);
                Assert.That(health, Is.EqualTo(MonsterStatCalculator.Calculate(source, actor)[GrowthAttributeCatalog.Health]).Within(.001), actor.actorId);
            }
        }

        [Test]
        public void MonsterCalculator_UsesEffectiveProfileGradeAndLevel()
        {
            var actor = ScriptableObject.CreateInstance<ActorDefinitionSO>();
            var profile = ScriptableObject.CreateInstance<MonsterActorProfileSO>();
            var scaling = ScriptableObject.CreateInstance<MonsterScalingSO>();
            try
            {
                scaling.FillDefaults();
                actor.grade = MonsterActorGrade.Normal; actor.level = 1;
                profile.grade = MonsterActorGrade.Boss; profile.level = 20; actor.monsterProfile = profile;
                var actual = MonsterStatCalculator.Calculate(scaling, actor);
                var expected = MonsterStatCalculator.Calculate(scaling, MonsterActorGrade.Boss, 20);
                Assert.That(actual[GrowthAttributeCatalog.Health], Is.EqualTo(expected[GrowthAttributeCatalog.Health]));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(profile); UnityEngine.Object.DestroyImmediate(scaling);
            }
        }

        [Test]
        public void DefenseLimit_AfterAllModifiers_MatchesRuntimeAndPureCalculator()
        {
            var defense = GrowthAttributeCatalog.Defense;
            var registry = AttributeRegistry.CreateRuntimeDefinition(defense);
            var runtime = new AttributeSetRuntime(AttributeRegistry.Resolver);
            runtime.Register(registry, .2f);
            runtime.AddModifier(defense, AttributeModifierOperation.Add, .5f, "장비", 1);
            runtime.AddModifier(defense, AttributeModifierOperation.Percent, .3f, "스킬", 2);
            runtime.AddModifier(defense, AttributeModifierOperation.Multiply, 2f, "롤", 3);
            var skills = new[]
            {
                new SkillStatModifierEntry(defense, AttributeModifierOperation.Add, .5f),
                new SkillStatModifierEntry(defense, AttributeModifierOperation.Percent, .3f),
                new SkillStatModifierEntry(defense, AttributeModifierOperation.Multiply, 2f),
            };
            var effective = CharacterEffectiveStatCalculator.Calculate(new Dictionary<AttributeId, float> { [defense] = .2f }, null, skills);
            Assert.That(registry.FixedMaximum, Is.EqualTo(.6f));
            Assert.That(runtime.GetCurrent(defense), Is.EqualTo(.6f));
            Assert.That(effective[defense], Is.EqualTo(runtime.GetCurrent(defense)));
        }

        [Test]
        public void EquipmentScale_PreservesStoredRollAndMissingFieldDefault()
        {
            var equipment = ScriptableObject.CreateInstance<EquipmentSO>();
            try
            {
                JsonUtility.FromJsonOverwrite("{}", equipment);
                Assert.That(equipment.rankValueScale, Is.EqualTo(1f));
                equipment.rankValueScale = .2f;
                var item = new ItemInstance { data = equipment };
                item.growthAttributeRolls.Add(new EquipmentGrowthAttributeRoll { attributeId = GrowthAttributeCatalog.HealthId, rank = 3 });
                var modifiers = new List<AttributeModifierValue>();
                CharacterEffectiveStatCalculator.AddEquipmentModifiers(item, modifiers);
                Assert.That(modifiers[0].Value, Is.EqualTo(12f));
                Assert.That(item.growthAttributeRolls[0].rank, Is.EqualTo(3));
            }
            finally { UnityEngine.Object.DestroyImmediate(equipment); }
        }

        [Test]
        public void OneBossSession_UsesCorrectEquipmentAndMaterialExpectations()
        {
            Assert.That(35 * .15 + 4 * .4 + 1.35, Is.EqualTo(8.2).Within(.00001));
            Assert.That(35 * 1.5 + 4 * 3.5 + 7, Is.EqualTo(73.5).Within(.00001));
            Assert.That(100 * (1 - .15) / .15, Is.EqualTo(566.6666667).Within(.0001));
        }
    }
}
