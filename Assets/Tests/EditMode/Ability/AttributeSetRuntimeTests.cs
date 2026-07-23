using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Ability.Core;

namespace UPlayGround.Ability.Tests
{
    public sealed class AttributeSetRuntimeTests
    {
        [Test]
        public void ModifierOrder_MatchesLegacyFormula_AndOverrideWins()
        {
            var runtime = new AttributeSetRuntime();
            runtime.Register(new GameplayAttributeDefinition(AttributeIds.Combat.AttackPower, 100f));

            runtime.AddModifier(AttributeIds.Combat.AttackPower, AttributeModifierOperation.Add, 20f, "Test", 1);
            runtime.AddModifier(AttributeIds.Combat.AttackPower, AttributeModifierOperation.Percent, 0.5f, "Test", 2);
            runtime.AddModifier(AttributeIds.Combat.AttackPower, AttributeModifierOperation.Multiply, 2f, "Test", 3);

            Assert.That(runtime.GetCurrent(AttributeIds.Combat.AttackPower), Is.EqualTo(360f));

            runtime.AddModifier(AttributeIds.Combat.AttackPower, AttributeModifierOperation.Override, 7f, "Test", 4);
            AttributeModifierHandle higher = runtime.AddModifier(
                AttributeIds.Combat.AttackPower,
                AttributeModifierOperation.Override,
                9f,
                "Test",
                5,
                priority: 1);
            Assert.That(runtime.GetCurrent(AttributeIds.Combat.AttackPower), Is.EqualTo(9f));

            runtime.RemoveModifier(higher);
            Assert.That(runtime.GetCurrent(AttributeIds.Combat.AttackPower), Is.EqualTo(7f));
        }

        [Test]
        public void Transaction_CommitsAllChangesBeforePublishingEvents()
        {
            var runtime = new AttributeSetRuntime();
            var a = new AttributeId("Test.A");
            var b = new AttributeId("Test.B");
            runtime.Register(new GameplayAttributeDefinition(a, 1f));
            runtime.Register(new GameplayAttributeDefinition(b, 2f));
            var observed = new List<(float A, float B)>();
            runtime.AttributeChanged += _ => observed.Add((runtime.GetBase(a), runtime.GetBase(b)));

            using (AttributeSetRuntime.Transaction transaction = runtime.BeginTransaction(77))
            {
                Assert.That(transaction.SetBase(a, 10f), Is.True);
                Assert.That(transaction.SetBase(b, 20f), Is.True);
                Assert.That(transaction.Commit(), Is.True);
            }

            Assert.That(observed, Has.Count.EqualTo(2));
            Assert.That(observed[0], Is.EqualTo((10f, 20f)));
            Assert.That(observed[1], Is.EqualTo((10f, 20f)));
        }

        [Test]
        public void MaximumChange_PreserveRatio_AdjustsResourceBase()
        {
            var runtime = new AttributeSetRuntime();
            runtime.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.MaxHealth,
                100f,
                dependentResourceId: AttributeIds.Vital.Health,
                maxChangePolicy: AttributeMaxChangePolicy.PreserveRatio));
            runtime.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.Health,
                50f,
                AttributeClampPolicy.AttributeRange,
                minimumAttributeId: default,
                maximumAttributeId: AttributeIds.Vital.MaxHealth,
                saveBaseValue: true));

            runtime.SetBase(AttributeIds.Vital.MaxHealth, 200f);

            Assert.That(runtime.GetBase(AttributeIds.Vital.Health), Is.EqualTo(100f));
            Assert.That(runtime.GetCurrent(AttributeIds.Vital.Health), Is.EqualTo(100f));
        }

        [Test]
        public void AttributeRange_UsesFixedBoundsWithDynamicMaximum()
        {
            var runtime = new AttributeSetRuntime();
            runtime.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.MaxHealth,
                100f));
            runtime.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.Health,
                50f,
                AttributeClampPolicy.AttributeRange,
                fixedMinimum: 0f,
                maximumAttributeId: AttributeIds.Vital.MaxHealth));

            runtime.SetBase(AttributeIds.Vital.Health, -25f);
            Assert.That(runtime.GetBase(AttributeIds.Vital.Health), Is.EqualTo(0f));
            Assert.That(runtime.GetCurrent(AttributeIds.Vital.Health), Is.EqualTo(0f));

            runtime.SetBase(AttributeIds.Vital.Health, 125f);
            Assert.That(runtime.GetBase(AttributeIds.Vital.Health), Is.EqualTo(100f));
            Assert.That(runtime.GetCurrent(AttributeIds.Vital.Health), Is.EqualTo(100f));
        }

        [Test]
        public void ChangeRequestedFromCallback_IsDeferredUntilCurrentPublishEnds()
        {
            var runtime = new AttributeSetRuntime();
            var a = new AttributeId("Test.A");
            var b = new AttributeId("Test.B");
            runtime.Register(new GameplayAttributeDefinition(a, 0f));
            runtime.Register(new GameplayAttributeDefinition(b, 0f));
            int eventCount = 0;
            runtime.AttributeChanged += change =>
            {
                eventCount++;
                if (change.AttributeId == a)
                    runtime.SetBase(b, 2f);
            };

            runtime.SetBase(a, 1f);

            Assert.That(runtime.GetBase(b), Is.EqualTo(2f));
            Assert.That(eventCount, Is.EqualTo(2));
        }
    }
}
