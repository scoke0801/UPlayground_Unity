using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Ability.Core;

namespace UPlayGround.Ability.Tests
{
    public sealed class GameplayEffectSpecTests
    {
        [Test]
        public void InstantEffect_MissingRequiredSetByCaller_ReturnsExplicitFailure()
        {
            var clock = new FakeClock();
            using AbilitySystemRuntime target = CreateRuntime(1, clock);
            target.Attributes.Register(new GameplayAttributeDefinition("Vital.Health", 100f));
            var definition = new GameplayEffectDefinition(
                "GE_Damage",
                GameplayEffectDurationPolicy.Instant,
                modifiers: new[]
                {
                    new GameplayEffectModifierSpecDefinition(
                        "Vital.Health",
                        AttributeModifierOperation.Add,
                        new SetByCallerMagnitudeCalculation("Data.Damage")),
                });
            GameplayEffectSpec spec = target.EffectSpecs.Create(
                definition, 1f,
                new GameplayEffectContext(target.Handle, target.Handle, target.Handle),
                target);

            GameplayEffectApplyOutcome outcome = target.Effects.Apply(spec, target);

            Assert.That(outcome.Result, Is.EqualTo(GameplayEffectApplyResult.MissingSetByCaller));
            Assert.That(target.Attributes.GetBase("Vital.Health"), Is.EqualTo(100f));
        }

        [Test]
        public void AttributeCapture_SnapshotOnCreate_DoesNotChangeBeforeApply()
        {
            var clock = new FakeClock();
            using AbilitySystemRuntime source = CreateRuntime(1, clock);
            using AbilitySystemRuntime target = CreateRuntime(2, clock);
            source.Attributes.Register(new GameplayAttributeDefinition("Combat.Attack", 10f));
            target.Attributes.Register(new GameplayAttributeDefinition("Vital.Health", 100f));
            var capture = new GameplayAttributeCaptureDefinition(
                "Combat.Attack",
                GameplayEffectCaptureSource.Source,
                GameplayEffectCapturePolicy.SnapshotOnCreate);
            var definition = new GameplayEffectDefinition(
                "GE_Captured",
                GameplayEffectDurationPolicy.Instant,
                modifiers: new[]
                {
                    new GameplayEffectModifierSpecDefinition(
                        "Vital.Health",
                        AttributeModifierOperation.Add,
                        new AttributeBasedMagnitudeCalculation(capture, coefficient: -1f)),
                });
            GameplayEffectSpec spec = source.EffectSpecs.Create(
                definition, 1f,
                new GameplayEffectContext(source.Handle, source.Handle, target.Handle),
                source);
            source.Attributes.SetBase("Combat.Attack", 20f);

            GameplayEffectApplyOutcome outcome = target.Effects.Apply(spec, source);

            Assert.That(outcome.Succeeded, Is.True);
            Assert.That(target.Attributes.GetBase("Vital.Health"), Is.EqualTo(90f));
        }

        [Test]
        public void DurationEffect_StacksRefreshesAndExpiresWithoutLeavingModifierOrTag()
        {
            var clock = new FakeClock();
            using AbilitySystemRuntime target = CreateRuntime(1, clock);
            target.Attributes.Register(new GameplayAttributeDefinition("Combat.Attack", 10f));
            var definition = new GameplayEffectDefinition(
                "GE_Buff",
                GameplayEffectDurationPolicy.Duration,
                modifiers: new[]
                {
                    new GameplayEffectModifierSpecDefinition(
                        "Combat.Attack", AttributeModifierOperation.Add,
                        new FixedMagnitudeCalculation(5f)),
                },
                duration: new FixedMagnitudeCalculation(2f),
                stackPolicy: AbilityEffectStackPolicy.AddStackAndRefresh,
                maxStackCount: 2,
                grantedTags: new[] { new AbilityTagId("Buff.Attack") });

            GameplayEffectSpec first = target.EffectSpecs.Create(
                definition, 1f,
                new GameplayEffectContext(target.Handle, target.Handle, target.Handle), target);
            GameplayEffectSpec second = target.EffectSpecs.Create(
                definition, 1f,
                new GameplayEffectContext(target.Handle, target.Handle, target.Handle), target);
            target.Effects.Apply(first, target);
            target.Effects.Apply(second, target);

            Assert.That(target.Attributes.GetCurrent("Combat.Attack"), Is.EqualTo(20f));
            Assert.That(target.Tags.HasExact("Buff.Attack"), Is.True);
            Assert.That(target.Effects.Count, Is.EqualTo(1));

            clock.Time = 3f;
            target.Effects.Tick();

            Assert.That(target.Effects.Count, Is.Zero);
            Assert.That(target.Attributes.GetCurrent("Combat.Attack"), Is.EqualTo(10f));
            Assert.That(target.Tags.HasExact("Buff.Attack"), Is.False);
        }

        [Test]
        public void PeriodicEffect_DoesNotExecutePastDurationAfterLongFrame()
        {
            var clock = new FakeClock();
            using AbilitySystemRuntime target = CreateRuntime(1, clock);
            var execution = new CountingExecution();
            var definition = new GameplayEffectDefinition(
                "GE_Periodic",
                GameplayEffectDurationPolicy.Duration,
                executions: new[] { execution },
                duration: new FixedMagnitudeCalculation(1f),
                period: new FixedMagnitudeCalculation(0.25f));
            GameplayEffectSpec spec = target.EffectSpecs.Create(
                definition,
                1f,
                new GameplayEffectContext(target.Handle, target.Handle, target.Handle),
                target);
            target.Effects.Apply(spec, target);

            clock.Time = 2f;
            target.Effects.Tick();

            Assert.That(execution.Count, Is.EqualTo(4));
            Assert.That(target.Effects.Count, Is.Zero);
        }

        [Test]
        public void DurationEffect_Tick중_관리힙을할당하지않는다()
        {
            var clock = new FakeClock();
            using var target = new AbilitySystemRuntime(
                new AbilitySystemHandle(1),
                "Owner",
                clock);
            var definition = new GameplayEffectDefinition(
                "GE.LongDuration",
                GameplayEffectDurationPolicy.Duration,
                duration: new FixedMagnitudeCalculation(1000f));
            GameplayEffectSpec spec = target.EffectSpecs.Create(
                definition,
                1f,
                new GameplayEffectContext(
                    target.Handle,
                    target.Handle,
                    target.Handle),
                target);
            target.Effects.Apply(spec, target);
            clock.Time = 1f;
            target.Effects.Tick();
            _ = System.GC.GetAllocatedBytesForCurrentThread();

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 32; i++)
            {
                clock.Time += 1f;
                target.Effects.Tick();
            }
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero);
        }

        private sealed class CountingExecution : IGameplayEffectExecution
        {
            public int Count { get; private set; }

            public bool Execute(
                in GameplayEffectExecutionInput input,
                GameplayEffectExecutionOutput output,
                out string error)
            {
                Count++;
                error = string.Empty;
                return true;
            }
        }

        private static AbilitySystemRuntime CreateRuntime(ulong id, FakeClock clock) =>
            new(new AbilitySystemHandle(id), $"Owner{id}", clock, enableDebug: true);

        private sealed class FakeClock : IAbilityClock
        {
            public float Time { get; set; }
            public int Frame { get; set; }
        }
    }
}
