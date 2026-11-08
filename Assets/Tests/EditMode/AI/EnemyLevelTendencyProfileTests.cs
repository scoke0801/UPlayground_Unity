using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.Enemy;
using UnityEngine;

namespace UPlayGround.AI.Tests
{
    public sealed class EnemyLevelTendencyProfileTests
    {
        private EnemyLevelTendencyProfileSO _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<EnemyLevelTendencyProfileSO>();
            // 저작 순서와 무관하게 해석되는지 보기 위해 일부러 섞어 둔다.
            _profile.bands = new List<EnemyLevelTendencyProfileSO.LevelBand>
            {
                new() { minLevel = 30, reactionScale = 1.3f },
                new() { minLevel = 1, reactionScale = 0.6f },
                new() { minLevel = 10, reactionScale = 1f },
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
        }

        [TestCase(1, 0.6f)]
        [TestCase(9, 0.6f)]
        [TestCase(10, 1f)]
        [TestCase(29, 1f)]
        [TestCase(30, 1.3f)]
        [TestCase(99, 1.3f)]
        public void 레벨_이하의_가장_높은_구간을_적용한다(int level, float expectedReaction)
        {
            var band = _profile.ResolveBand(level);

            Assert.That(band, Is.Not.Null);
            Assert.That(band.reactionScale, Is.EqualTo(expectedReaction));
        }

        [Test]
        public void 해당_구간이_없으면_null을_반환한다()
        {
            _profile.bands = new List<EnemyLevelTendencyProfileSO.LevelBand>
            {
                new() { minLevel = 20 },
            };

            Assert.That(_profile.ResolveBand(5), Is.Null);
        }
    }
}
