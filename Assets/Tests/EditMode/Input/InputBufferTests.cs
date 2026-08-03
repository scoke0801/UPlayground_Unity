using NUnit.Framework;

namespace UPlayGround.Input.Tests
{
    public sealed class InputBufferTests
    {
        [Test]
        public void ReplaceExisting_동일_액션은_최신_입력_하나만_유지한다()
        {
            var buffer = new InputBuffer(bufferTime: 10f);

            buffer.AddInput("Attack", data: 1, replaceExisting: true);
            buffer.AddInput("Attack", data: 2, replaceExisting: true);
            buffer.AddInput("Attack", data: 3, replaceExisting: true);

            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(3, buffer.PeekInput("Attack")?.Data);
        }

        [Test]
        public void ReplaceExisting_다른_액션의_입력은_보존한다()
        {
            var buffer = new InputBuffer(bufferTime: 10f);

            buffer.AddInput("Attack", replaceExisting: true);
            buffer.AddInput("Dodge", replaceExisting: true);
            buffer.AddInput("Attack", replaceExisting: true);

            Assert.AreEqual(2, buffer.Count);
            Assert.IsNotNull(buffer.PeekInput("Attack"));
            Assert.IsNotNull(buffer.PeekInput("Dodge"));
        }

        [Test]
        public void Sequence_같은_타임스탬프에서도_실제_추가_순서를_보존한다()
        {
            var buffer = new InputBuffer(bufferTime: 10f);

            buffer.AddInput("HeavyAttack", timestamp: 0f);
            buffer.AddInput("Attack", timestamp: 0f);

            BufferedInput heavy = buffer.PeekInput("HeavyAttack");
            BufferedInput light = buffer.PeekInput("Attack");

            Assert.Greater(light.Sequence, heavy.Sequence);
            Assert.AreSame(light, buffer.GetLatestInput());
        }

        [Test]
        public void Sequence_동일_액션을_교체해도_새_순번을_부여한다()
        {
            var buffer = new InputBuffer(bufferTime: 10f);

            buffer.AddInput("Attack", data: 1, timestamp: 0f);
            long firstSequence = buffer.PeekInput("Attack").Sequence;
            buffer.AddInput("Attack", data: 2, timestamp: 0f);

            BufferedInput replaced = buffer.PeekInput("Attack");
            Assert.Greater(replaced.Sequence, firstSequence);
            Assert.AreEqual(2, replaced.Data);
        }
    }
}
