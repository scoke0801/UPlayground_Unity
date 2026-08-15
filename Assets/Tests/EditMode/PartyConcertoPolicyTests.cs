using NUnit.Framework;
using UPlayGround.Data.Party;

namespace UPlayGround.Core.Tests
{
    public class PartyConcertoPolicyTests
    {
        [Test]
        public void 최대치에_도달해야_교체_특수공격이_준비된다()
        {
            Assert.That(PartyConcertoPolicy.IsReady(99.99f, 100f), Is.False);
            Assert.That(PartyConcertoPolicy.IsReady(100f, 100f), Is.True);
        }

        [Test]
        public void 저작된_어빌리티가_없으면_게이지가_차도_발동하지_않는다()
        {
            Assert.That(PartyConcertoPolicy.CanTriggerSwapSpecial(
                100f, 100f, false, false), Is.False);
        }

        [Test]
        public void 스왑회피와_어시스트가_교체_특수공격보다_우선한다()
        {
            Assert.That(PartyConcertoPolicy.CanTriggerSwapSpecial(
                100f, 100f, true, true), Is.False);
            Assert.That(PartyConcertoPolicy.CanTriggerSwapSpecial(
                100f, 100f, true, false), Is.True);
        }
    }
}
