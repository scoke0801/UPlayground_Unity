using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Editor;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Ability.Tests
{
    public sealed class MotionSetSectionMigrationTests
    {
        readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
                if (created != null)
                    Object.DestroyImmediate(created);
            _created.Clear();
        }

        [TestCase("Motion.Locomotion.Idle")]
        [TestCase("Motion.Locomotion.Run.B")]
        [TestCase("Motion.Air.Fall")]
        [TestCase("Motion.Crouch.Walk")]
        [TestCase("Motion.Fly.Idle")]
        [TestCase("Motion.Interaction.Fishing.Catch.Loop")]
        [TestCase("Motion.Reaction.Guard")]
        public void IsContinuousTag_지속_슬롯을_판별한다(string tag)
        {
            Assert.That(MotionSetSectionMigration.IsContinuousTag(tag), Is.True);
        }

        [TestCase("Motion.Action.Dodge")]
        [TestCase("Motion.Air.Jump")]
        [TestCase("Motion.Crouch.Enter")]
        [TestCase("Motion.Reaction.Hit.F")]
        [TestCase("Motion.Stop.Running.F")]
        public void IsContinuousTag_단발_슬롯을_제외한다(string tag)
        {
            Assert.That(MotionSetSectionMigration.IsContinuousTag(tag), Is.False);
        }

        [Test]
        public void Analyze_지속_슬롯_참조는_LoopSelf다()
        {
            MotionSetAsset asset = CreateAsset();
            var references = new Dictionary<MotionSetAsset, HashSet<string>>
            {
                [asset] = new() { "Motion.Locomotion.Run" },
            };

            MotionSetSectionMigrationEntry result = MotionSetSectionMigration.Analyze(
                asset, "Assets/Test.asset", "test", references);

            Assert.That(result.decision, Is.EqualTo(MotionSetSectionMigrationDecision.LoopSelf));
        }

        [Test]
        public void Analyze_단발_슬롯_참조는_Stop이다()
        {
            MotionSetAsset asset = CreateAsset();
            var references = new Dictionary<MotionSetAsset, HashSet<string>>
            {
                [asset] = new() { "Motion.Action.Dodge" },
            };

            MotionSetSectionMigrationEntry result = MotionSetSectionMigration.Analyze(
                asset, "Assets/Test.asset", "test", references);

            Assert.That(result.decision, Is.EqualTo(MotionSetSectionMigrationDecision.Stop));
        }

        [Test]
        public void Analyze_지속과_단발_공유는_Review다()
        {
            MotionSetAsset asset = CreateAsset();
            var references = new Dictionary<MotionSetAsset, HashSet<string>>
            {
                [asset] = new() { "Motion.Locomotion.Run", "Motion.Action.Dodge" },
            };

            MotionSetSectionMigrationEntry result = MotionSetSectionMigration.Analyze(
                asset, "Assets/Test.asset", "test", references);

            Assert.That(result.decision, Is.EqualTo(MotionSetSectionMigrationDecision.Review));
        }

        [Test]
        public void Analyze_근거가_없는_에셋은_Review다()
        {
            MotionSetAsset asset = CreateAsset();

            MotionSetSectionMigrationEntry result = MotionSetSectionMigration.Analyze(
                asset,
                "Assets/Test.asset",
                "test",
                new Dictionary<MotionSetAsset, HashSet<string>>());

            Assert.That(result.decision, Is.EqualTo(MotionSetSectionMigrationDecision.Review));
        }

        [Test]
        public void Analyze_이미_Section이_있으면_건너뛴다()
        {
            MotionSetAsset asset = CreateAsset();
            asset.motionSet.sections.Add(new MotionSection
            {
                id = "section",
                startTime = 0f,
                endPolicy = MotionSectionEndPolicy.Stop,
            });

            MotionSetSectionMigrationEntry result = MotionSetSectionMigration.Analyze(
                asset,
                "Assets/Test.asset",
                "test",
                new Dictionary<MotionSetAsset, HashSet<string>>());

            Assert.That(result.decision, Is.EqualTo(MotionSetSectionMigrationDecision.Existing));
        }

        MotionSetAsset CreateAsset()
        {
            var clip = new AnimationClip();
            clip.SetCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, 1f, 1f));
            var asset = ScriptableObject.CreateInstance<MotionSetAsset>();
            asset.motionSet = new MotionSet
            {
                motions = new List<Motion>
                {
                    new() { motionClip = clip },
                },
            };
            _created.Add(clip);
            _created.Add(asset);
            return asset;
        }
    }
}
