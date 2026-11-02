using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.MovementController;
using UPlayGround.State;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Movement.Tests
{
    /// <summary>프레임률·아날로그 조작·접촉 타이밍의 회귀를 확인한다.</summary>
    public sealed class PlayerGlidingMovementTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 하강은_고도를얻지않고_프레임률과무관하게_목표속도로수렴한다(int frameRate)
        {
            float speed = 8f;
            for (int i = 0; i < frameRate * 2; i++)
            {
                speed = PlayerGlidingMovement.IntegrateDescent(speed, 1.6f, 0.22f, 1f / frameRate);
                Assert.That(speed, Is.LessThanOrEqualTo(0f));
                Assert.That(speed, Is.GreaterThanOrEqualTo(-1.6f));
            }
            Assert.That(speed, Is.EqualTo(-1.6f).Within(0.001f));
        }

        [Test]
        public void 무입력은_방향을유지하고_약한스틱은_선회량을줄인다()
        {
            Assert.That(PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.zero,
                Vector3.up, 100f, 0.1f), Is.EqualTo(Vector3.forward));
            Vector3 weak = PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.right * 0.25f,
                Vector3.up, 100f, 0.1f);
            Vector3 strong = PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.right,
                Vector3.up, 100f, 0.1f);
            Assert.That(Vector3.Angle(Vector3.forward, weak), Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(Vector3.Angle(Vector3.forward, strong), Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void 일시정지중에는_속도와방향이변하지않는다()
        {
            Assert.That(PlayerGlidingMovement.IntegrateDescent(-0.2f, 1.6f, 0.22f, 0f), Is.EqualTo(-0.2f));
            Assert.That(PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.back,
                Vector3.up, 110f, 0f), Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void 접촉마커는_앞클립과_재생속도를반영하고_누락을거부한다()
        {
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 0f));
            try
            {
                var set = MotionSet.CreateAuthored("활공 접촉 검사");
                set.motions.Add(new Motion { motionClip = clip, playbackSpeed = 2f });
                var contact = new Motion { motionClip = clip, playbackSpeed = 0.5f };
                contact.markers.Add(new MotionMarker { id = PlayerSuperJumpGlidingState.ContactMarker, normalizedTime = 0.25f });
                set.motions.Add(contact);
                Assert.That(PlayerSuperJumpGlidingState.TryGetMarkerTime(set,
                    PlayerSuperJumpGlidingState.ContactMarker, out float time), Is.True);
                Assert.That(time, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(PlayerSuperJumpGlidingState.TryGetMarkerTime(set,
                    PlayerSuperJumpGlidingState.LaunchMarker, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
        }
    }
}
