using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Ability.Core;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilitySystemRuntimeTests
    {
        [Test]
        public void TagAggregator_PreservesOtherSourceOwnership()
        {
            var tags = new GameplayTagAggregator();
            GameplayTagSourceHandle first = tags.Add("State.Combat.Attacking", "Ability", 1);
            GameplayTagSourceHandle second = tags.Add("State.Combat.Attacking", "Effect", 2);

            Assert.That(tags.Has("State.Combat"), Is.True);
            tags.Remove(first);
            Assert.That(tags.HasExact("State.Combat.Attacking"), Is.True);
            tags.Remove(second);
            Assert.That(tags.HasExact("State.Combat.Attacking"), Is.False);
        }

        [Test]
        public void EventRouter_DisposeUnsubscribesImmediately()
        {
            var router = new GameplayEventRouter();
            int calls = 0;
            System.IDisposable subscription = router.Subscribe("Event.Combat", _ => calls++, true);
            router.Send(new GameplayEventData("Event.Combat.Hit"));
            subscription.Dispose();
            router.Send(new GameplayEventData("Event.Combat.Hit"));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(router.SubscriptionCount, Is.Zero);
        }

        [Test]
        public void DebugSnapshot_IsDetachedFromRuntimeCollections_AndRingIsBounded()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Owner", clock, enableDebug: true, debugCapacity: 2);
            runtime.Attributes.Register(new GameplayAttributeDefinition("Test.Value", 0f));
            runtime.Attributes.SetBase("Test.Value", 1f);
            runtime.Tags.Add("State.One", "Test", 1);
            runtime.Events.Send(new GameplayEventData("Event.Test"));

            AbilitySystemDebugSnapshot snapshot = runtime.CaptureDebugSnapshot(
                AbilityDebugCaptureOptions.All);
            runtime.Attributes.SetBase("Test.Value", 2f);

            Assert.That(snapshot.Attributes[new AttributeId("Test.Value")].BaseValue, Is.EqualTo(1f));
            Assert.That(snapshot.Tags, Has.Count.EqualTo(1));
            Assert.That(snapshot.Events, Has.Count.EqualTo(2));
            Assert.That(runtime.Debug.Count, Is.EqualTo(2));
        }

        [Test]
        public void RestoreSaveData_RestoresActiveEffectRemainingSeconds()
        {
            var sourceClock = new FakeClock();
            using var source = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Source", sourceClock);
            source.Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Combat.AttackPower, 10f));
            GameplayEffectDefinition definition = CreateSavedDurationEffect();
            GameplayEffectSpec sourceSpec = source.EffectSpecs.Create(
                definition,
                1f,
                new GameplayEffectContext(source.Handle, source.Handle, source.Handle),
                source);
            source.Effects.Apply(sourceSpec, source);
            GameplayEffectSpec stackedSpec = source.EffectSpecs.Create(
                definition,
                1f,
                new GameplayEffectContext(source.Handle, source.Handle, source.Handle),
                source);
            source.Effects.Apply(stackedSpec, source);
            sourceClock.Time = 4f;
            source.Effects.Tick();
            AbilitySystemSaveData saveData = source.CaptureSaveData();

            var targetClock = new FakeClock();
            using var target = new AbilitySystemRuntime(
                new AbilitySystemHandle(2), "Target", targetClock);
            target.Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Combat.AttackPower, 10f));
            GameplayEffectSpec staleSpec = target.EffectSpecs.Create(
                definition,
                1f,
                new GameplayEffectContext(target.Handle, target.Handle, target.Handle),
                target);
            target.Effects.Apply(staleSpec, target);
            target.RestoreSaveData(
                saveData,
                effectId => effectId == definition.EffectId ? definition : null);
            var active = new List<ActiveGameplayEffect>();
            target.Effects.CopyActive(active);

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].RemainingSeconds, Is.EqualTo(6f));
            Assert.That(active[0].StackCount, Is.EqualTo(2));
            Assert.That(target.Attributes.GetCurrent(AttributeIds.Combat.AttackPower), Is.EqualTo(20f));
        }

        private static GameplayEffectDefinition CreateSavedDurationEffect() =>
            new(
                "GE.SavedDuration",
                GameplayEffectDurationPolicy.Duration,
                modifiers: new[]
                {
                    new GameplayEffectModifierSpecDefinition(
                        AttributeIds.Combat.AttackPower,
                        AttributeModifierOperation.Add,
                        new FixedMagnitudeCalculation(5f)),
                },
                duration: new FixedMagnitudeCalculation(10f),
                stackPolicy: AbilityEffectStackPolicy.AddStackAndRefresh,
                maxStackCount: 2,
                saveActiveEffect: true);

        private sealed class FakeClock : IAbilityClock
        {
            public float Time { get; set; }
            public int Frame { get; set; }
        }
    }
}
