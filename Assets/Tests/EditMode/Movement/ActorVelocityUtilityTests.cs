using NUnit.Framework;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    public class ActorVelocityUtilityTests
    {
        [Test]
        public void ReplacePlanarPreserveVertical_루트Y를버리고_권위수직속도를보존한다()
        {
            Vector3 desired = new Vector3(4f, 30f, 2f);
            Vector3 authoritative = new Vector3(1f, -3f, 1f);

            Vector3 result = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                desired,
                authoritative,
                Vector3.up);

            Assert.That(result, Is.EqualTo(new Vector3(4f, -3f, 2f)));
        }

        [Test]
        public void ReplacePlanarPreserveVertical_기울어진CharacterUp도_축기준으로분리한다()
        {
            Vector3 up = new Vector3(0f, 1f, 1f).normalized;
            Vector3 desired = up * 20f + Vector3.right * 5f;
            Vector3 authoritative = up * -2f;

            Vector3 result = ActorVelocityUtility.ReplacePlanarPreserveVertical(
                desired,
                authoritative,
                up);

            Assert.That(Vector3.Dot(result, up), Is.EqualTo(-2f).Within(0.0001f));
            Assert.That(Vector3.Dot(result, Vector3.right), Is.EqualTo(5f).Within(0.0001f));
        }
    }
}
