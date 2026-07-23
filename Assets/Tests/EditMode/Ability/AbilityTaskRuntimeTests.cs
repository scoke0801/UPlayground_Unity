using NUnit.Framework;
using UPlayGround.Ability.Core;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilityTaskRuntimeTests
    {
        [Test]
        public void ParentCancel_EndsAllChildrenAndReleasesEventSubscription()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            runtime.Tasks.Start(parent, new WaitGameplayEventTask(context, "Event.Hit"));
            runtime.Tasks.Start(parent, new WaitDelayTask(context, 10f));

            int cancelled = runtime.Tasks.CancelParent(parent);

            Assert.That(cancelled, Is.EqualTo(2));
            Assert.That(runtime.Tasks.Count, Is.Zero);
            Assert.That(runtime.Events.SubscriptionCount, Is.Zero);
        }

        [Test]
        public void Sequence_CompletesEachChildExactlyOnce()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            var sequence = new SequenceAbilityTask(
                context,
                new System.Func<AbilityTaskContext, AbilityTaskInstance>[]
                {
                    item => new WaitDelayTask(item, 1f),
                    item => new WaitDelayTask(item, 1f),
                });
            runtime.Tasks.Start(parent, sequence);

            clock.Time = 1f;
            runtime.Tasks.Tick();
            Assert.That(runtime.Tasks.Count, Is.EqualTo(1));
            clock.Time = 2f;
            runtime.Tasks.Tick();

            Assert.That(runtime.Tasks.Count, Is.Zero);
            Assert.That(sequence.State, Is.EqualTo(AbilityTaskState.Succeeded));
        }

        [Test]
        public void Parallel_WaitsForAllChildrenWhenFirstChildCompletesSynchronously()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            var parallel = new ParallelAbilityTask(
                context,
                new System.Func<AbilityTaskContext, AbilityTaskInstance>[]
                {
                    item => new WaitDelayTask(item, 0f),
                    item => new WaitDelayTask(item, 1f),
                },
                completeOnAny: false);

            runtime.Tasks.Start(parent, parallel);

            Assert.That(parallel.State, Is.EqualTo(AbilityTaskState.Active));
            Assert.That(runtime.Tasks.Count, Is.EqualTo(1));

            clock.Time = 1f;
            runtime.Tasks.Tick();

            Assert.That(parallel.State, Is.EqualTo(AbilityTaskState.Succeeded));
            Assert.That(runtime.Tasks.Count, Is.Zero);
        }

        private sealed class FakeClock : IAbilityClock
        {
            public float Time { get; set; }
            public int Frame { get; set; }
        }
    }
}
