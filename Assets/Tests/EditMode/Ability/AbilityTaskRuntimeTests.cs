using NUnit.Framework;
using UPlayGround.Ability.Core;
using UnityEngine;

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

        [Test]
        public void WaitTag_CompletesAndUnsubscribes()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1),
                "Owner",
                clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            runtime.Tasks.Start(
                parent,
                new WaitTagTask(context, "State.Charged", true));

            GameplayTagSourceHandle handle = runtime.Tags.Add(
                "State.Charged",
                "Test",
                1);

            Assert.That(handle.IsValid, Is.True);
            Assert.That(runtime.Tasks.Count, Is.Zero);
        }

        [Test]
        public void WaitInputRelease_CompletesFromInputPort()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1),
                "Owner",
                clock);
            var input = new FakeInputPort();
            runtime.SetInputPort(input);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            runtime.Tasks.Start(parent, new WaitInputTask(context, 0, true));

            input.State = AbilityInputState.Released;
            runtime.Tasks.Tick();

            Assert.That(runtime.Tasks.Count, Is.Zero);
        }

        [Test]
        public void Loop_RepeatsChildToConfiguredCount()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1),
                "Owner",
                clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            var loop = new LoopAbilityTask(
                context,
                item => new WaitDelayTask(item, 1f),
                2,
                0f);
            runtime.Tasks.Start(parent, loop);

            clock.Time = 1f;
            runtime.Tasks.Tick();
            clock.Time = 2f;
            runtime.Tasks.Tick();

            Assert.That(loop.State, Is.EqualTo(AbilityTaskState.Succeeded));
            Assert.That(runtime.Tasks.Count, Is.Zero);
        }

        [Test]
        public void ParentCompletion_실패상태와_사유를_한번만_전달한다()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            FailingTaskDefinition definition =
                ScriptableObject.CreateInstance<FailingTaskDefinition>();

            runtime.Tasks.Start(parent, definition);

            Assert.That(runtime.Tasks.TryConsumeParentCompletion(
                parent, out AbilityTaskState state, out string reason), Is.True);
            Assert.That(state, Is.EqualTo(AbilityTaskState.Failed));
            Assert.That(reason, Is.EqualTo("ExpectedFailure"));
            Assert.That(runtime.Tasks.TryConsumeParentCompletion(
                parent, out _, out _), Is.False);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ParentCompletion_FailParent비활성화면_성공으로_전달한다()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            FailingTaskDefinition definition =
                ScriptableObject.CreateInstance<FailingTaskDefinition>();
            definition.failParentOnFailure = false;

            runtime.Tasks.Start(parent, definition);

            Assert.That(runtime.Tasks.TryConsumeParentCompletion(
                parent, out AbilityTaskState state, out _), Is.True);
            Assert.That(state, Is.EqualTo(AbilityTaskState.Succeeded));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ParentCompletion_여러최상위Task의_전파실패를_보존한다()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);

            runtime.Tasks.Start(parent, new FailingTask(context));
            runtime.Tasks.Start(parent, new WaitDelayTask(context, 1f));

            Assert.That(runtime.Tasks.TryConsumeParentCompletion(
                parent, out _, out _), Is.False);
            clock.Time = 1f;
            runtime.Tasks.Tick();
            Assert.That(runtime.Tasks.TryConsumeParentCompletion(
                parent, out AbilityTaskState state, out string reason), Is.True);
            Assert.That(state, Is.EqualTo(AbilityTaskState.Failed));
            Assert.That(reason, Is.EqualTo("ExpectedFailure"));
        }

        [Test]
        public void ParentCompletion_비전파실패와_성공은_최종성공이다()
        {
            var clock = new FakeClock();
            using var runtime = new AbilitySystemRuntime(
                new AbilitySystemHandle(1), "Owner", clock);
            var parent = new AbilityExecutionHandle(11);
            var context = new AbilityTaskContext(runtime, parent, clock);
            FailingTaskDefinition definition =
                ScriptableObject.CreateInstance<FailingTaskDefinition>();
            definition.failParentOnFailure = false;

            runtime.Tasks.Start(parent, definition);
            runtime.Tasks.Start(parent, new WaitDelayTask(context, 1f));
            clock.Time = 1f;
            runtime.Tasks.Tick();

            Assert.That(runtime.Tasks.TryConsumeParentCompletion(
                parent, out AbilityTaskState state, out _), Is.True);
            Assert.That(state, Is.EqualTo(AbilityTaskState.Succeeded));
            Object.DestroyImmediate(definition);
        }

        private sealed class FakeClock : IAbilityClock
        {
            public float Time { get; set; }
            public int Frame { get; set; }
        }

        private sealed class FakeInputPort : IAbilityInputPort
        {
            public AbilityInputState State;
            public AbilityInputState GetSlotState(int slot) => State;
        }

        private sealed class FailingTaskDefinition : AbilityTaskDefinitionSO
        {
            public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
                new FailingTask(context);
        }

        private sealed class FailingTask : AbilityTaskInstance
        {
            public FailingTask(AbilityTaskContext context) : base(context) { }
            protected override void OnActivate() => Fail("ExpectedFailure");
        }
    }
}
