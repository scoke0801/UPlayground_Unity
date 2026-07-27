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
    }
}
