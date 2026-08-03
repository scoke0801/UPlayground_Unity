using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Stat;

namespace UPlayGround.Ability.Tests
{
    public sealed class AttributeRegistryTests
    {
        [Test]
        public void 등록된_Attribute만_Resolve할_수_있다()
        {
            Assert.That(
                AttributeRegistry.TryResolve(
                    "Combat.AttackPower",
                    out AttributeReference registered),
                Is.True);
            Assert.That(registered.IsValid(), Is.True);
            Assert.That(
                registered.AttributeId,
                Is.EqualTo("Combat.AttackPower"));

            Assert.That(
                AttributeRegistry.TryResolve(
                    "Combat.AttckPower",
                    out AttributeReference unregistered),
                Is.False);
            Assert.That(unregistered.IsValid(), Is.False);
        }

        [Test]
        public void 코드_표준_Attribute는_Registry에_등록되어_있다()
        {
            AssertCodeDefinedContainer(typeof(Attributes));
        }

        private static void AssertCodeDefinedContainer(Type container)
        {
            foreach (FieldInfo field in container.GetFields(
                         BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(AttributeReference))
                    continue;
                var reference =
                    (AttributeReference)field.GetValue(null);
                if (string.IsNullOrEmpty(reference.AttributeId))
                    continue;
                Assert.That(
                    AttributeRegistry.IsRegistered(reference.AttributeId),
                    Is.True,
                    $"{container.Name}.{field.Name}의 "
                    + $"{reference.AttributeId}이 Registry에 없습니다.");
            }
            foreach (Type nested in container.GetNestedTypes(
                         BindingFlags.Public))
                AssertCodeDefinedContainer(nested);
        }

        [Test]
        public void Registry_메타데이터는_기존_표시와_기본값을_보존한다()
        {
            var expected = new Dictionary<string, (float, string, string)>
            {
                ["Vital.MaxHealth"] = (100f, "최대 체력", "100"),
                ["Combat.AttackPower"] = (1f, "공격력", "1"),
                ["Combat.Defense"] = (0f, "방어력", "25%"),
                ["Combat.CritRate"] = (0f, "치명타 확률", "25%"),
                ["Combat.CritMultiplier"] = (1.5f, "치명타 피해", "25%"),
                ["Vital.PoiseRecoveryRate"] = (40f, "강인도 회복", "40"),
                ["Life.GatheringPower"] = (1f, "채집력", "1"),
            };

            foreach (KeyValuePair<string, (float, string, string)> pair
                     in expected)
            {
                var id = new AttributeId(pair.Key);
                Assert.That(
                    UPlayGroundAttributeDefaults.Get(id),
                    Is.EqualTo(pair.Value.Item1));
                Assert.That(
                    StatDisplayFormatter.GetDisplayName(id),
                    Is.EqualTo(pair.Value.Item2));
                float sample = pair.Key.StartsWith(
                    "Combat.",
                    StringComparison.Ordinal)
                    && pair.Key != "Combat.AttackPower"
                    ? 0.25f
                    : pair.Value.Item1;
                Assert.That(
                    StatDisplayFormatter.FormatValue(id, sample),
                    Is.EqualTo(pair.Value.Item3));
            }
        }

        [Test]
        public void Profile_필수값은_런타임_전용_Attribute를_포함하지_않는다()
        {
            AttributeId[] profileAttributes =
                UPlayGroundAttributeDefaults.ProfileAttributes;

            Assert.That(profileAttributes, Has.Length.EqualTo(15));
            CollectionAssert.Contains(
                profileAttributes,
                (AttributeId)Attributes.Vital.MaxHealth);
            CollectionAssert.DoesNotContain(
                profileAttributes,
                (AttributeId)Attributes.Vital.Health);
            CollectionAssert.DoesNotContain(
                profileAttributes,
                (AttributeId)Attributes.Resource.UltimateEnergy);
            CollectionAssert.DoesNotContain(
                profileAttributes,
                (AttributeId)Attributes.Meta.IncomingDamage);
        }

        [Test]
        public void InternTable은_alias를_같은_핸들로_해석한다()
        {
            var current = new AttributeRegistryEntry
            {
                attributeId = "Vital.Health",
                stableId = "stable-health",
                aliases = new List<string> { "Vital.HP" },
            };
            var table = new AttributeInternTable(
                new[] { current });

            Assert.That(
                table.TryResolve("Vital.Health", out AttributeHandle canonical),
                Is.True);
            Assert.That(
                table.TryResolve("Vital.HP", out AttributeHandle alias),
                Is.True);
            Assert.That(alias, Is.EqualTo(canonical));
        }

        [Test]
        public void Resolver가_주입된_Runtime은_미등록_Attribute를_거부한다()
        {
            var runtime = new AttributeSetRuntime(
                AttributeRegistry.Resolver);
            Assert.That(
                runtime.Register(
                    AttributeRegistry.CreateRuntimeDefinition(
                        new AttributeId("Vital.Health"))),
                Is.True);
            Assert.That(
                runtime.Register(
                    new GameplayAttributeDefinition(
                        new AttributeId("Vital.Unknown"),
                        1f)),
                Is.False);
        }

        [Test]
        public void 직렬화된_미등록_문자열은_유효하지_않다()
        {
            AttributeReference reference =
                JsonUtility.FromJson<AttributeReference>(
                    "{\"_attributeId\":\"Unknown.Attribute\"}");
            Assert.That(
                reference.AttributeId,
                Is.EqualTo("Unknown.Attribute"));
            Assert.That(reference.IsValid(), Is.False);
        }
    }
}
