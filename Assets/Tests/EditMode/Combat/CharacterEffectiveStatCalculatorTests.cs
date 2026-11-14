using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;

namespace UPlayGround.Combat.Tests
{
    /// <summary>장비·스킬 연산 순서와 확정 롤의 공유 경로를 검증한다.</summary>
    public sealed class CharacterEffectiveStatCalculatorTests
    {
        [Test]
        public void Calculate_MixedOperations_FoldsOnceAcrossSources()
        {
            var hp = GrowthAttributeCatalog.Health;
            var equipment = ScriptableObject.CreateInstance<EquipmentSO>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(equipment);
                var entries = serialized.FindProperty("_statModifiers");
                entries.arraySize = 1;
                var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("attributeId").stringValue = hp.Value;
                entry.FindPropertyRelative("modifierType").enumValueIndex = 0;
                entry.FindPropertyRelative("value").floatValue = 10f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var item = new ItemInstance { data = equipment };
                item.growthAttributeRolls.Add(new EquipmentGrowthAttributeRoll
                {
                    attributeId = hp.Value, rank = 2,
                });
                var skills = new[]
                {
                    new SkillStatModifierEntry(hp, AttributeModifierOperation.Add, 5f),
                    new SkillStatModifierEntry(hp, AttributeModifierOperation.Percent, .1f),
                    new SkillStatModifierEntry(hp, AttributeModifierOperation.Percent, .2f),
                    new SkillStatModifierEntry(hp, AttributeModifierOperation.Multiply, 1.5f),
                    new SkillStatModifierEntry(hp, AttributeModifierOperation.Multiply, .8f),
                };
                var bases = new Dictionary<AttributeId, float> { [hp] = 100f };
                var result = CharacterEffectiveStatCalculator.Calculate(bases, new[] { item }, skills);
                Assert.That(result[hp], Is.EqualTo(241.8f).Within(.001f));
                Assert.That(bases[hp], Is.EqualTo(100f));
                Assert.That(item.growthAttributeRolls[0].rank, Is.EqualTo(2));
                Assert.That(CharacterEffectiveStatCalculator.Calculate(bases, new[] { item }, skills)[hp],
                    Is.EqualTo(result[hp]));
            }
            finally { UnityEngine.Object.DestroyImmediate(equipment); }
        }

        [Test]
        public void Calculate_LastOverride_WinsOverOtherOperations()
        {
            var hp = GrowthAttributeCatalog.Health;
            var skills = new[]
            {
                new SkillStatModifierEntry(hp, AttributeModifierOperation.Override, 200f),
                new SkillStatModifierEntry(hp, AttributeModifierOperation.Add, 1000f),
                new SkillStatModifierEntry(hp, AttributeModifierOperation.Multiply, 3f),
                new SkillStatModifierEntry(hp, AttributeModifierOperation.Override, 250f),
            };
            Assert.That(CharacterEffectiveStatCalculator.Calculate(null, null, skills)[hp], Is.EqualTo(250f));
        }

        [Test]
        public void AddEquipmentModifiers_UsesCatalogForEveryRoll()
        {
            var equipment = ScriptableObject.CreateInstance<EquipmentSO>();
            try
            {
                var item = new ItemInstance { data = equipment };
                foreach (var id in GrowthAttributeCatalog.DefaultEquipmentRollIds)
                    item.growthAttributeRolls.Add(new EquipmentGrowthAttributeRoll { attributeId = id.Value, rank = 3 });
                var modifiers = new List<AttributeModifierValue>();
                CharacterEffectiveStatCalculator.AddEquipmentModifiers(item, modifiers);
                Assert.That(modifiers.Count, Is.EqualTo(6));
                for (int i = 0; i < modifiers.Count; i++)
                {
                    Assert.That(GrowthAttributeCatalog.TryGetEquipmentFlatValuePerRank(modifiers[i].AttributeId, out float value), Is.True);
                    Assert.That(modifiers[i].Value, Is.EqualTo(value * 3f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(equipment); }
        }
    }
}
