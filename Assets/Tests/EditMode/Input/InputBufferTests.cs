using NUnit.Framework;
using UPlayGround.InputDefine;

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

        [TestCase(PlayerAction.Attack, PlayerInputBufferPolicy.AttackDuration)]
        [TestCase(PlayerAction.HeavyAttack, PlayerInputBufferPolicy.AttackDuration)]
        [TestCase(PlayerAction.Dash, PlayerInputBufferPolicy.MovementDuration)]
        [TestCase(PlayerAction.Jump, PlayerInputBufferPolicy.MovementDuration)]
        [TestCase(PlayerAction.SkillAbility, PlayerInputBufferPolicy.SkillDuration)]
        [TestCase(PlayerAction.Interact, PlayerInputBufferPolicy.DefaultDuration)]
        public void PlayerInputBufferPolicy_액션별_유지시간을_일관되게_반환한다(
            string actionName,
            float expected)
        {
            Assert.That(PlayerInputBufferPolicy.GetDuration(actionName), Is.EqualTo(expected));
        }

        [Test]
        public void PlayerInputBufferPolicy_강공격은_릴리스에서만_버퍼를_확정한다()
        {
            Assert.IsFalse(PlayerInputBufferPolicy.ShouldBufferOnPerformed(PlayerAction.HeavyAttack));
            Assert.IsTrue(PlayerInputBufferPolicy.ShouldBufferOnPerformed(PlayerAction.Attack));
        }

        [Test]
        public void 생성자_버퍼크기가_0이하면_즉시_실패한다()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new InputBuffer(maxSize: 0));
        }
    }
}
