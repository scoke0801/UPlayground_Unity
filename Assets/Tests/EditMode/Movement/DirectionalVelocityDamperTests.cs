using NUnit.Framework;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    public class DirectionalVelocityDamperTests
    {
        [Test]
        public void Apply_최초스텝에는_요청속도를감쇠하지않는다()
        {
            var damper = new DirectionalVelocityDamper(Vector3.right * 10f, 8f);
            Vector3 velocity = Vector3.right * 10f;

            damper.Apply(ref velocity, 0.02f);

            Assert.That(velocity.x, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void Apply_충돌로방향성분이사라지면_반대속도를만들지않는다()
        {
            var damper = new DirectionalVelocityDamper(Vector3.right * 10f, 8f);
            Vector3 velocity = Vector3.zero;

            damper.Apply(ref velocity, 0.02f);

            Assert.That(velocity.x, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Apply_지정방향만감쇠하고_수직속도는보존한다()
        {
            var damper = new DirectionalVelocityDamper(Vector3.right * 10f, 8f);
            Vector3 velocity = new Vector3(10f, 5f, 0f);

            damper.Apply(ref velocity, 0.02f);
            damper.Apply(ref velocity, 0.02f);

            Assert.That(velocity.x, Is.LessThan(10f));
            Assert.That(velocity.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(velocity.y, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void Apply_반복호출해도_속도방향이뒤집히지않는다()
        {
            var damper = new DirectionalVelocityDamper(Vector3.forward * 12f, 10f);
            Vector3 velocity = Vector3.forward * 12f;

            for (int i = 0; i < 300; i++)
                damper.Apply(ref velocity, 0.02f);

            Assert.That(velocity.z, Is.GreaterThanOrEqualTo(0f));
            Assert.That(velocity.z, Is.LessThan(0.1f));
        }

        [Test]
        public void Launch_Replace는_기존점프속도에_더하지않는다()
        {
            var launch = new PendingVerticalLaunch();
            launch.Enqueue(12f, VerticalLaunchVelocityPolicy.Replace);

            float resolved = launch.Resolve(10f);

            Assert.That(resolved, Is.EqualTo(12f).Within(0.0001f));
        }

        [Test]
        public void Launch_같은스텝의Replace요청은_가장강한값하나만사용한다()
        {
            var launch = new PendingVerticalLaunch();
            launch.Enqueue(12f, VerticalLaunchVelocityPolicy.Replace);
            launch.Enqueue(8f, VerticalLaunchVelocityPolicy.Replace);
            launch.Enqueue(12f, VerticalLaunchVelocityPolicy.Replace);

            float resolved = launch.Resolve(0f);

            Assert.That(resolved, Is.EqualTo(12f).Within(0.0001f));
        }

        [TestCase(15f, 12f, 15f)]
        [TestCase(5f, 12f, 12f)]
        public void Launch_AtLeast는_더강한기존상승속도를보존한다(
            float currentSpeed,
            float requestedSpeed,
            float expectedSpeed)
        {
            var launch = new PendingVerticalLaunch();
            launch.Enqueue(requestedSpeed, VerticalLaunchVelocityPolicy.AtLeast);

            Assert.That(
                launch.Resolve(currentSpeed),
                Is.EqualTo(expectedSpeed).Within(0.0001f));
        }

        [Test]
        public void MotionEvent_상향속도는_설정한한계로제한한다()
        {
            float resolved = ExternalVelocityPolicy.ClampAuthoredUpwardSpeed(
                32f,
                12f,
                allowsUpwardVelocity: true);

            Assert.That(resolved, Is.EqualTo(12f).Within(0.0001f));
        }

        [Test]
        public void MotionEvent_Dive상태에서는_상향속도를차단한다()
        {
            float resolved = ExternalVelocityPolicy.ClampAuthoredUpwardSpeed(
                32f,
                12f,
                allowsUpwardVelocity: false);

            Assert.That(resolved, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
