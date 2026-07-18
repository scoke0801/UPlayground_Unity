using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Ability.Core;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilityCoreRuntimeTests
    {
        [Test]
        public void Cooldown_CaptureRestore_UsesInjectedClock()
        {
            var clock = new FakeClock();
            var runtime = new AbilityCooldownRuntime(clock);
            runtime.Start("Skill.Primary", 5f);

            clock.TimeValue = 2f;
            var snapshots = new List<AbilityCooldownSnapshot>();
            runtime.Capture(snapshots);

            Assert.That(snapshots, Has.Count.EqualTo(1));
            Assert.That(snapshots[0].RemainingSeconds, Is.EqualTo(3f));

            var restoredClock = new FakeClock { TimeValue = 10f };
            var restored = new AbilityCooldownRuntime(restoredClock);
            restored.Restore(
                snapshots[0].GroupId,
                snapshots[0].RemainingSeconds);
            Assert.That(restored.GetRemaining("Skill.Primary"), Is.EqualTo(3f));
        }

        [Test]
        public void Cooldown_RemoveExpired_RemovesOnlyExpiredGroups()
        {
            var clock = new FakeClock();
            var runtime = new AbilityCooldownRuntime(clock);
            runtime.Start("Short", 1f);
            runtime.Start("Long", 3f);

            clock.TimeValue = 2f;
            Assert.That(runtime.RemoveExpired(), Is.True);
            Assert.That(runtime.GetRemaining("Short"), Is.Zero);
            Assert.That(runtime.GetRemaining("Long"), Is.EqualTo(1f));
        }

        [Test]
        public void EffectStack_AddAndRefresh_ClampsToMaximum()
        {
            AbilityEffectStackResult result = AbilityEffectStackRuntime.Resolve(
                AbilityEffectStackPolicy.AddStackAndRefresh,
                currentStackCount: 3,
                maxStackCount: 3);

            Assert.That(result.Action, Is.EqualTo(AbilityEffectStackAction.RefreshExisting));
            Assert.That(result.StackCount, Is.EqualTo(3));
        }

        private sealed class FakeClock : IAbilityClock
        {
            public float TimeValue;
            public float Time => TimeValue;
            public int Frame => 0;
        }
    }
}
