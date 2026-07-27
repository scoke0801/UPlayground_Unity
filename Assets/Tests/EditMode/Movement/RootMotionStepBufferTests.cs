using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.Movement.Tests
{
    public class RootMotionStepBufferTests
    {
        [Test]
        public void BeginStep_여러Animator델타를합산하고_한번만소비한다()
        {
            var buffer = RootMotionStepBuffer.Create();
            buffer.Accumulate(Vector3.right, Quaternion.Euler(0f, 30f, 0f));
            buffer.Accumulate(Vector3.forward * 2f, Quaternion.Euler(0f, 15f, 0f));

            buffer.BeginStep();

            Assert.That(buffer.StepPosition, Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(Quaternion.Angle(buffer.StepRotation, Quaternion.Euler(0f, 45f, 0f)),
                Is.LessThan(0.01f));

            buffer.EndStep();
            buffer.BeginStep();
            Assert.That(buffer.StepPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(buffer.StepRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void ConsumePending_잔류소비자는_같은델타를재사용하지않는다()
        {
            var buffer = RootMotionStepBuffer.Create();
            buffer.Accumulate(Vector3.up * 3f, Quaternion.Euler(10f, 0f, 0f));

            buffer.ConsumePending(out Vector3 firstPosition, out Quaternion firstRotation);
            buffer.ConsumePending(out Vector3 secondPosition, out Quaternion secondRotation);

            Assert.That(firstPosition, Is.EqualTo(Vector3.up * 3f));
            Assert.That(Quaternion.Angle(firstRotation, Quaternion.Euler(10f, 0f, 0f)),
                Is.LessThan(0.01f));
            Assert.That(secondPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(secondRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void Flush_대기중델타와활성스텝을모두제거한다()
        {
            var buffer = RootMotionStepBuffer.Create();
            buffer.Accumulate(Vector3.one, Quaternion.Euler(0f, 90f, 0f));
            buffer.BeginStep();
            buffer.Accumulate(Vector3.right, Quaternion.Euler(0f, 30f, 0f));

            buffer.Flush();

            Assert.That(buffer.PendingPosition, Is.EqualTo(Vector3.zero));
            Assert.That(buffer.StepPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(buffer.PendingRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(buffer.StepRotation, Quaternion.identity), Is.LessThan(0.01f));
        }
    }
}
