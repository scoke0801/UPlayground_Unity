using System;
using NUnit.Framework;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Core.Tests
{
    public sealed class EnvironmentReactionTests
    {
        [Test]
        public void ContinuousWaterRetainsItsCoolingCoefficientAcrossSave()
        {
            var p = new EnvironmentReactionParameters { cooling=0, wetCooling=0, evaporation=0, drying=0,
                waterImpulseCooling=0, waterRateCooling=1 };
            var initial = new EnvironmentReactionState { heat=1, fuel=20, durability=100 };
            var a = new EnvironmentReaction(p,initial);
            a.AddEmitterInput(0,1,0.2f);
            a.GetPendingInputs(out double heat,out double water,out double continuous);
            var b = new EnvironmentReaction(p,a.State);
            b.RestorePendingInputs(heat,water,continuous);
            a.Tick(0.2f,0,1); b.Tick(0.2f,0,1);
            Assert.That(b.State,Is.EqualTo(a.State));
            Assert.That(b.State.heat,Is.EqualTo(0.8f).Within(0.001));
            Assert.That(b.State.wetness,Is.EqualTo(0.2f).Within(0.001));
        }

        [Test]
        public void CombatElementsAndResistanceUseExplicitPayload()
        {
            var p = new EnvironmentReactionParameters { slashMultiplier = 0, bluntMultiplier = 2 };
            var reaction = new EnvironmentReaction(p);
            reaction.TryApplyCombatDamage(20, 1, 0, EnvironmentImpactKind.Slash);
            Assert.That(reaction.State.durability, Is.EqualTo(100));
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Burning));
            float health = reaction.State.durability;
            reaction.TryApplyCombatDamage(20, 0, 1, EnvironmentImpactKind.Blunt);
            Assert.That(reaction.State.durability, Is.EqualTo(health - 40).Within(0.001));
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.combustion, Is.Not.EqualTo(EnvironmentCombustion.Burning));
        }

        [Test]
        public void StoneReceivesScorchWithoutFuelOrStructuralDamage()
        {
            var p = new EnvironmentReactionParameters { canIgnite = false, canBreak = false,
                initialFuel = 0, scorchPerHeat = 0.1f };
            var reaction = new EnvironmentReaction(p);
            reaction.AddEmitterInput(2, 0, 0.2f);
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.charAmount, Is.EqualTo(0.04f).Within(0.001));
            Assert.That(reaction.State.fuel, Is.Zero);
            Assert.That(reaction.State.durability, Is.EqualTo(100));
        }

        [Test]
        public void HeatAndWaterOrderProducesSameResult()
        {
            var a = new EnvironmentReaction(new EnvironmentReactionParameters());
            var b = new EnvironmentReaction(new EnvironmentReactionParameters());
            a.TryApplyImpact(1, 0, 0, 1, 0);
            a.TryApplyImpact(2, 0, 0, 0, 1);
            b.TryApplyImpact(2, 0, 0, 0, 1);
            b.TryApplyImpact(1, 0, 0, 1, 0);
            a.Tick(0.2f, 0, 1); b.Tick(0.2f, 0, 1);
            Assert.That(a.State, Is.EqualTo(b.State));
            Assert.That(a.State.combustion, Is.Not.EqualTo(EnvironmentCombustion.Burning));
            Assert.That(a.State.fuel, Is.EqualTo(20));
        }

        [Test]
        public void MultipleHeatSourcesAreSummedBeforeCoolingAndClamping()
        {
            var reaction = new EnvironmentReaction(new EnvironmentReactionParameters());
            reaction.TryApplyImpact(1, 0, 0, 0.8f, 0);
            reaction.TryApplyImpact(2, 0, 0, 0.8f, 0);
            reaction.TryApplyImpact(3, 0, 0, 0, 0.5f);
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.heat, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateHitIsRejectedButAnotherSequenceIsAccepted()
        {
            var reaction = new EnvironmentReaction(new EnvironmentReactionParameters());
            Assert.That(reaction.TryApplyImpact(5, 0, 20, 0, 0), Is.True);
            Assert.That(reaction.TryApplyImpact(5, 0, 20, 0, 0), Is.False);
            Assert.That(reaction.State.durability, Is.EqualTo(80));
            Assert.That(reaction.TryApplyImpact(5, 1, 20, 0, 0), Is.True);
            Assert.That(reaction.State.durability, Is.EqualTo(60));
        }

        [Test]
        public void WaterExtinguishesWithoutRepairingDamageOrChar()
        {
            var reaction = new EnvironmentReaction(new EnvironmentReactionParameters());
            reaction.TryApplyImpact(1, 0, 20, 1, 0);
            reaction.Tick(0.2f, 0, 1);
            var burned = reaction.State;
            Assert.That(burned.combustion, Is.EqualTo(EnvironmentCombustion.Burning));
            reaction.TryApplyImpact(2, 0, 0, 0, 1);
            reaction.Tick(0.2f, 1, 1);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Cooling));
            Assert.That(reaction.State.charAmount, Is.EqualTo(burned.charAmount));
            Assert.That(reaction.State.durability, Is.EqualTo(burned.durability));
            Assert.That(reaction.State.fuel, Is.EqualTo(burned.fuel));
        }

        [Test]
        public void RoofExposureSeparatesRainInput()
        {
            var sheltered = new EnvironmentReaction(new EnvironmentReactionParameters());
            var exposed = new EnvironmentReaction(new EnvironmentReactionParameters());
            sheltered.Tick(0.2f, 1, 0); exposed.Tick(0.2f, 1, 1);
            Assert.That(sheltered.State.wetness, Is.Zero);
            Assert.That(exposed.State.wetness, Is.GreaterThan(0));
        }

        [Test]
        public void BrokenRemnantKeepsFuelAndContinuesBurning()
        {
            var reaction = new EnvironmentReaction(new EnvironmentReactionParameters());
            reaction.TryApplyImpact(1, 0, 100, 1, 0);
            Assert.That(reaction.State.isBroken, Is.True);
            Assert.That(reaction.State.fuel, Is.EqualTo(20));
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Burning));
            Assert.That(reaction.State.fuel, Is.EqualTo(19.8f).Within(0.001f));
            Assert.That(reaction.State.breakCause, Is.EqualTo(EnvironmentBreakCause.Impact));
        }

        [Test]
        public void FuelExhaustionAppliesLastDamageAndBecomesSpent()
        {
            var p = new EnvironmentReactionParameters { initialFuel = 0.1f, damagePerFuel = 6 };
            var reaction = new EnvironmentReaction(p);
            reaction.TryApplyImpact(1, 0, 0, 1, 0);
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.fuel, Is.Zero);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Spent));
            Assert.That(reaction.State.durability, Is.EqualTo(99.4f).Within(0.001f));
        }

        [Test]
        public void NonCombustibleMaterialDoesNotBecomeSpent()
        {
            var p = new EnvironmentReactionParameters { canIgnite = false, initialFuel = 0 };
            var reaction = new EnvironmentReaction(p);
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.combustion, Is.EqualTo(EnvironmentCombustion.Inactive));
        }

        [Test]
        public void ProtectedStructureCanBurnWithoutBreaking()
        {
            var p = new EnvironmentReactionParameters { canBreak = false };
            var reaction = new EnvironmentReaction(p);
            reaction.TryApplyImpact(1, 0, 1000, 1, 0);
            reaction.Tick(0.2f, 0, 1);
            Assert.That(reaction.State.isBroken, Is.False);
            Assert.That(reaction.State.durability, Is.EqualTo(100));
            Assert.That(reaction.State.fuel, Is.LessThan(20));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        public void InvalidInputsDoNotConsumeDedupKey(float invalid)
        {
            var reaction = new EnvironmentReaction(new EnvironmentReactionParameters());
            Assert.That(reaction.TryApplyImpact(1, 0, invalid, 0, 0), Is.False);
            Assert.That(reaction.TryApplyImpact(1, 0, 20, 0, 0), Is.True);
        }

        [Test]
        public void SaveBeforeTickPreservesPendingInputs()
        {
            var p = new EnvironmentReactionParameters();
            var a = new EnvironmentReaction(p);
            a.TryApplyImpact(1, 0, 10, 1, 0.1f);
            a.GetPendingInputs(out double heat, out double water);
            var b = new EnvironmentReaction(p, a.State);
            b.RestorePendingInputs(heat, water);
            a.Tick(0.2f, 0, 1); b.Tick(0.2f, 0, 1);
            Assert.That(b.State, Is.EqualTo(a.State));
        }

        [Test]
        public void InvalidProfileAndStateAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new EnvironmentReaction(new EnvironmentReactionParameters { extinguishWet = 0.1f }));
            Assert.Throws<ArgumentException>(() => new EnvironmentReaction(new EnvironmentReactionParameters(),
                new EnvironmentReactionState { fuel = 21, durability = 100 }));
        }
    }
}
