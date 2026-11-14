using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.Cinematic.Tests
{
    public sealed class UltimateFaceStripTests
    {
        private sealed class Presenter : IFaceStripPresenter
        {
            public long Token;
            private long _next;
            public long Show(FaceStripRequest request) => Token = ++_next;
            public void Hide(long token) { if (Token == token) Token = 0; }
        }

        [Test]
        public void 실행_교체_후_이전_복구는_새_패널을_지우지_않는다()
        {
            var presenter = new Presenter();
            var first = new UltimateRuntimeContext();
            var second = new UltimateRuntimeContext();
            first.ShowFaceStrip(presenter, default);
            second.ShowFaceStrip(presenter, default);
            long token = presenter.Token;
            first.ReleaseFaceStrip();
            Assert.That(presenter.Token, Is.EqualTo(token));
            second.ReleaseFaceStrip();
            Assert.That(presenter.Token, Is.Zero);
            second.ReleaseFaceStrip();
        }

        [Test]
        public void 이벤트_Complete는_같은_실행의_새_패널을_지우지_않는다()
        {
            var presenter = new Presenter();
            var context = new UltimateRuntimeContext();
            context.ShowFaceStrip(presenter, default);
            context.ShowFaceStrip(presenter, default);
            new UltimateFaceStripEvent().Complete(context);
            Assert.That(presenter.Token, Is.GreaterThan(0));
            context.ReleaseFaceStrip();
            Assert.That(presenter.Token, Is.Zero);
        }

        [Test]
        public void 얼굴_이벤트는_managed_reference로_직렬화된다()
        {
            var asset = ScriptableObject.CreateInstance<UltimateSequenceAsset>();
            UltimateSequenceAsset copy = null;
            try
            {
                asset.events.Add(new UltimateFaceStripEvent { hold = 1.3f, startTime = 0.2f });
                copy = UnityEngine.Object.Instantiate(asset);
                Assert.That(copy.events[0], Is.TypeOf<UltimateFaceStripEvent>());
                Assert.That(((UltimateFaceStripEvent)copy.events[0]).hold, Is.EqualTo(1.3f));
                Assert.That(copy.events[0].startTime, Is.EqualTo(0.2f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
