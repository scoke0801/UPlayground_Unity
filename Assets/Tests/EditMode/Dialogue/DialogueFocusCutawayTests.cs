using NUnit.Framework;

namespace UPlayGround.Dialogue.Tests
{
    /// <summary>
    /// 주목 컷의 시간 진행 계약을 검증한다.
    /// 카메라 push 없이 "언제 대상으로 넘어가고 언제 라인 구도로 돌아오는지"만 확인한다.
    /// </summary>
    public sealed class DialogueFocusCutawayTests
    {
        [Test]
        public void 대기시간이_0이면_시작과_동시에_대상으로_넘어간다()
        {
            var cutaway = new DialogueFocusCutaway();

            DialogueFocusStep step = cutaway.Begin(delaySeconds: 0f, holdSeconds: 2f);

            Assert.AreEqual(DialogueFocusStep.EnterFocus, step);
            Assert.IsTrue(cutaway.IsFocused);
        }

        [Test]
        public void 대기시간이_지나야_대상으로_넘어간다()
        {
            var cutaway = new DialogueFocusCutaway();

            Assert.AreEqual(DialogueFocusStep.None, cutaway.Begin(delaySeconds: 0.5f, holdSeconds: 2f));
            Assert.IsTrue(cutaway.IsActive);
            Assert.IsFalse(cutaway.IsFocused);

            Assert.AreEqual(DialogueFocusStep.None, cutaway.Tick(0.3f));
            Assert.AreEqual(DialogueFocusStep.EnterFocus, cutaway.Tick(0.3f));
            Assert.IsTrue(cutaway.IsFocused);
        }

        [Test]
        public void 유지시간이_지나면_라인_구도로_복귀하고_끝난다()
        {
            var cutaway = new DialogueFocusCutaway();
            cutaway.Begin(delaySeconds: 0f, holdSeconds: 1f);

            Assert.AreEqual(DialogueFocusStep.None, cutaway.Tick(0.6f));
            Assert.AreEqual(DialogueFocusStep.ReturnToLine, cutaway.Tick(0.6f));
            Assert.IsFalse(cutaway.IsActive);

            // 복귀 뒤에는 더 이상 아무 전환도 내보내지 않는다 — 복귀 push가 두 번 일어나면 안 된다.
            Assert.AreEqual(DialogueFocusStep.None, cutaway.Tick(1f));
        }

        [Test]
        public void 대기_구간에서_넘친_시간은_유지_구간에서_차감된다()
        {
            var cutaway = new DialogueFocusCutaway();
            cutaway.Begin(delaySeconds: 0.5f, holdSeconds: 1f);

            // 0.9초 프레임 하나로 대기(0.5)를 넘기면 유지 구간에는 0.6초만 남아야 한다.
            Assert.AreEqual(DialogueFocusStep.EnterFocus, cutaway.Tick(0.9f));
            Assert.AreEqual(DialogueFocusStep.None, cutaway.Tick(0.5f));
            Assert.AreEqual(DialogueFocusStep.ReturnToLine, cutaway.Tick(0.2f));
        }

        [Test]
        public void 유지시간이_0이면_주목_컷을_쓰지_않는다()
        {
            var cutaway = new DialogueFocusCutaway();

            Assert.AreEqual(DialogueFocusStep.None, cutaway.Begin(delaySeconds: 0f, holdSeconds: 0f));
            Assert.IsFalse(cutaway.IsActive);
            Assert.AreEqual(DialogueFocusStep.None, cutaway.Tick(5f));
        }

        [Test]
        public void 취소하면_복귀_전환_없이_끝난다()
        {
            var cutaway = new DialogueFocusCutaway();
            cutaway.Begin(delaySeconds: 0f, holdSeconds: 2f);

            cutaway.Reset();

            Assert.IsFalse(cutaway.IsActive);
            Assert.AreEqual(DialogueFocusStep.None, cutaway.Tick(5f));
        }
    }
}
