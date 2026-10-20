using NUnit.Framework;
using UPlayGround.Group;

namespace UPlayGround.AI.Tests
{
    public sealed class EncounterGroupPriorityTests
    {
        [Test]
        public void ElitePriorityPreservesSerializedValues()
        {
            Assert.AreEqual(0, (int)MemberPriority.Summon);
            Assert.AreEqual(1, (int)MemberPriority.Normal);
            Assert.AreEqual(2, (int)MemberPriority.Summoner);
            Assert.Greater((int)MemberPriority.Elite, (int)MemberPriority.Summoner);
        }

        [TestCase(-1f, true, true)]
        [TestCase(0f, true, true)]
        [TestCase(1f, true, false)]
        [TestCase(-1f, false, false)]
        public void LongComboResponderMustBeVisibleAndFlanking(float dot, bool visible, bool expected)
            => Assert.AreEqual(expected, MonsterGroupController.CanRespondToLongCombo(dot, visible));
    }
}
