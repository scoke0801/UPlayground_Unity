using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;

namespace UPlayGround.Combat.Tests
{
    /// <summary>NPC 전투 반응 단계 판정의 거리 경계와 깜빡임 방지(히스테리시스)를 검사한다.</summary>
    public sealed class NpcReactionResolverTests
    {
        private NpcCombatReactionProfileSO _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<NpcCombatReactionProfileSO>();
            _profile.watchRadius = 25f;
            _profile.cowerRadius = 15f;
            _profile.fleeRadius = 8f;
            _profile.releaseRadiusScale = 1.25f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_profile);

        [TestCase(5f, NpcReactionLevel.Flee)]
        [TestCase(12f, NpcReactionLevel.Cower)]
        [TestCase(20f, NpcReactionLevel.Watch)]
        [TestCase(40f, NpcReactionLevel.None)]
        public void 거리에_맞는_가장_높은_단계를_고른다(float distance, NpcReactionLevel expected)
        {
            Assert.AreEqual(expected, NpcReactionResolver.Resolve(_profile, NpcReactionLevel.None, distance));
        }

        [Test]
        public void 이미_해당_단계면_반경을_넓혀_경계에서_유지한다()
        {
            Assert.AreEqual(NpcReactionLevel.Cower, NpcReactionResolver.Resolve(_profile, NpcReactionLevel.None, 8.5f));
            Assert.AreEqual(NpcReactionLevel.Flee, NpcReactionResolver.Resolve(_profile, NpcReactionLevel.Flee, 9.5f));
            Assert.AreEqual(NpcReactionLevel.Cower, NpcReactionResolver.Resolve(_profile, NpcReactionLevel.Flee, 10.5f));
        }

        [Test]
        public void 프로파일이_없으면_반응하지_않는다()
        {
            Assert.AreEqual(NpcReactionLevel.None, NpcReactionResolver.Resolve(null, NpcReactionLevel.None, 1f));
            Assert.IsFalse(NpcReactionResolver.IsWithinLevel(null, NpcReactionLevel.Watch, NpcReactionLevel.None, 1f));
        }
    }
}
