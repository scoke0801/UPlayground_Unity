using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;

namespace UPlayGround.Combat.Tests
{
    /// <summary>플레이어 충돌에 대한 NPC 반응의 단계·부위·방향 판정 규칙을 검사한다.</summary>
    public sealed class NpcBumpResolverTests
    {
        private NpcBumpProfileSO _profile;

        [SetUp]
        public void SetUp() => _profile = ScriptableObject.CreateInstance<NpcBumpProfileSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_profile);

        [TestCase(1f, NpcBumpTier.None)]
        [TestCase(2f, NpcBumpTier.Lean)]
        [TestCase(5f, NpcBumpTier.Stagger)]
        [TestCase(9f, NpcBumpTier.Shove)]
        public void 속도에_맞는_단계를_고른다(float speed, NpcBumpTier expected)
        {
            Assert.AreEqual(expected, NpcBumpResolver.ResolveTier(_profile, speed, NpcBumpContext.Free));
        }

        [Test]
        public void 작업_중이면_상체_기울기까지만_허용한다()
        {
            Assert.AreEqual(NpcBumpTier.Lean, NpcBumpResolver.ResolveTier(_profile, 9f, NpcBumpContext.Working));
            Assert.AreEqual(NpcBumpTier.None, NpcBumpResolver.ResolveTier(_profile, 1f, NpcBumpContext.Working));
        }

        [Test]
        public void 대화나_전투_반응_중이면_반응하지_않는다()
        {
            Assert.AreEqual(NpcBumpTier.None, NpcBumpResolver.ResolveTier(_profile, 9f, NpcBumpContext.Locked));
        }

        [Test]
        public void 프로필이_없으면_반응하지_않는다()
        {
            Assert.AreEqual(NpcBumpTier.None, NpcBumpResolver.ResolveTier(null, 9f, NpcBumpContext.Free));
        }

        [TestCase(0f, 0.3f, 0.2f, NpcBumpPart.Hip)]
        [TestCase(0f, 1.3f, 0.2f, NpcBumpPart.Chest)]
        [TestCase(0f, 1.3f, -0.2f, NpcBumpPart.Back)]
        [TestCase(-0.3f, 1.4f, 0f, NpcBumpPart.LeftShoulder)]
        [TestCase(0.3f, 1.4f, 0f, NpcBumpPart.RightShoulder)]
        public void 로컬_접촉점으로_부위를_고른다(float x, float y, float z, NpcBumpPart expected)
        {
            Assert.AreEqual(expected, NpcBumpResolver.ResolvePart(_profile, new Vector3(x, y, z), 1.7f));
        }

        [Test]
        public void 높이를_모르면_골반으로_판정하지_않는다()
        {
            Assert.AreEqual(NpcBumpPart.Chest, NpcBumpResolver.ResolvePart(_profile, new Vector3(0f, 0.3f, 0.1f), 0f));
        }

        [Test]
        public void 단계가_높을수록_멀리_밀린다()
        {
            Assert.AreEqual(0f, NpcBumpResolver.GetPushDistance(_profile, NpcBumpTier.Lean));
            Assert.Less(NpcBumpResolver.GetPushDistance(_profile, NpcBumpTier.Stagger),
                NpcBumpResolver.GetPushDistance(_profile, NpcBumpTier.Shove));
        }

        [Test]
        public void 밀림_방향은_수평으로_투영한_플레이어_진행_방향이다()
        {
            Vector3 direction = NpcBumpResolver.ResolvePushDirection(new Vector3(0f, -3f, 4f), Vector3.back);
            Assert.AreEqual(0f, direction.y, 0.0001f);
            Assert.AreEqual(1f, direction.z, 0.0001f);
        }

        [Test]
        public void 속도가_없으면_접촉_법선의_반대로_민다()
        {
            Vector3 direction = NpcBumpResolver.ResolvePushDirection(Vector3.zero, new Vector3(1f, 0f, 0f));
            Assert.AreEqual(-1f, direction.x, 0.0001f);
        }

        [Test]
        public void 푸시_속도는_감쇠로_나누면_목표_밀림_거리가_된다()
        {
            float speed = NpcBumpResolver.GetPushSpeed(_profile, NpcBumpTier.Shove);
            Assert.AreEqual(_profile.shoveDistance, speed / _profile.pushDrag, 0.0001f);
            Assert.AreEqual(0f, NpcBumpResolver.GetPushSpeed(_profile, NpcBumpTier.Lean));
        }

        [Test]
        public void 반응_곡선은_빠르게_오르고_회복_시간에_0으로_돌아온다()
        {
            Assert.AreEqual(0f, NpcBumpResolver.EvaluateEnvelope(_profile, 0f));
            Assert.AreEqual(1f, NpcBumpResolver.EvaluateEnvelope(_profile, _profile.attackSeconds), 0.0001f);
            float middle = NpcBumpResolver.EvaluateEnvelope(_profile, (_profile.attackSeconds + _profile.recoverSeconds) * 0.5f);
            Assert.That(middle, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.AreEqual(0f, NpcBumpResolver.EvaluateEnvelope(_profile, _profile.recoverSeconds));
        }

        [Test]
        public void 어깨를_맞으면_가슴보다_더_비틀어진다()
        {
            var chest = NpcBumpResolver.ResolveUpperBodyAngles(_profile, NpcBumpTier.Stagger, NpcBumpPart.Chest);
            var shoulder = NpcBumpResolver.ResolveUpperBodyAngles(_profile, NpcBumpTier.Stagger, NpcBumpPart.LeftShoulder);
            Assert.Greater(shoulder.twist, chest.twist);
            Assert.Greater(chest.lean, shoulder.lean);
        }

        [TestCase(NpcBumpPart.Chest, NpcBumpMotionDirection.Front)]
        [TestCase(NpcBumpPart.Hip, NpcBumpMotionDirection.Front)]
        [TestCase(NpcBumpPart.Back, NpcBumpMotionDirection.Back)]
        [TestCase(NpcBumpPart.LeftShoulder, NpcBumpMotionDirection.Left)]
        [TestCase(NpcBumpPart.RightShoulder, NpcBumpMotionDirection.Right)]
        public void 닿은_부위가_피격_모션_방향이_된다(NpcBumpPart part, NpcBumpMotionDirection expected)
        {
            Assert.AreEqual(expected, NpcBumpResolver.ResolveMotionDirection(part));
        }

        [Test]
        public void 휘청과_밀침은_서로_다른_모션_슬롯을_쓴다()
        {
            foreach (NpcBumpMotionDirection direction in System.Enum.GetValues(typeof(NpcBumpMotionDirection)))
            {
                var stagger = NpcBumpResolver.ResolveMotionTag(NpcBumpTier.Stagger, direction);
                var shove = NpcBumpResolver.ResolveMotionTag(NpcBumpTier.Shove, direction);
                Assert.AreNotEqual(stagger.TagName, shove.TagName);
                StringAssert.Contains("Stagger", stagger.TagName);
                StringAssert.Contains("Shove", shove.TagName);
            }
        }

        [Test]
        public void 상체_기울기_단계는_전용_모션_슬롯이_없다()
        {
            Assert.IsFalse(NpcBumpResolver.ResolveMotionTag(NpcBumpTier.Lean, NpcBumpMotionDirection.Front).IsValid());
            Assert.IsFalse(NpcBumpResolver.ResolveMotionTag(NpcBumpTier.None, NpcBumpMotionDirection.Back).IsValid());
        }

        [Test]
        public void 단계가_높을수록_상체가_더_기운다()
        {
            var lean = NpcBumpResolver.ResolveUpperBodyAngles(_profile, NpcBumpTier.Lean, NpcBumpPart.Chest);
            var shove = NpcBumpResolver.ResolveUpperBodyAngles(_profile, NpcBumpTier.Shove, NpcBumpPart.Chest);
            Assert.Greater(shove.lean, lean.lean);
        }
    }
}
