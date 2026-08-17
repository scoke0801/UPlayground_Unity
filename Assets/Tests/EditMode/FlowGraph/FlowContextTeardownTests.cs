using System;
using System.Reflection;
using NUnit.Framework;
using UPlayGround.FlowGraph;

namespace UPlayGround.FlowGraph.Tests
{
    public sealed class FlowContextTeardownTests
    {
        [Test]
        public void DisposeTeardowns_DisposesInReverseOrderExactlyOnce()
        {
            var context = new FlowContext(null, null);
            int sequence = 0;
            int firstOrder = 0;
            int secondOrder = 0;
            context.RegisterTeardown(new Probe(() => firstOrder = ++sequence));
            context.RegisterTeardown(new Probe(() => secondOrder = ++sequence));

            MethodInfo dispose = typeof(FlowContext).GetMethod(
                "DisposeTeardowns",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dispose, Is.Not.Null);
            dispose.Invoke(context, null);
            dispose.Invoke(context, null);

            Assert.That(secondOrder, Is.EqualTo(1));
            Assert.That(firstOrder, Is.EqualTo(2));
            Assert.That(sequence, Is.EqualTo(2));
        }

        [Test]
        public void RegisterAfterCompletion_DisposesImmediately()
        {
            var context = new FlowContext(null, null);
            MethodInfo dispose = typeof(FlowContext).GetMethod(
                "DisposeTeardowns",
                BindingFlags.Instance | BindingFlags.NonPublic);
            dispose.Invoke(context, null);
            int disposed = 0;

            context.RegisterTeardown(new Probe(() => disposed++));

            Assert.That(disposed, Is.EqualTo(1));
        }

        private sealed class Probe : IDisposable
        {
            private Action _onDispose;

            public Probe(Action onDispose) => _onDispose = onDispose;

            public void Dispose()
            {
                Action callback = _onDispose;
                _onDispose = null;
                callback?.Invoke();
            }
        }
    }
}
