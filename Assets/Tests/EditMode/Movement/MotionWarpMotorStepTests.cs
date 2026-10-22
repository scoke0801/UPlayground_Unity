using NUnit.Framework;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    /// <summary>물리 스텝 분할과 관계없이 누적된 충돌 손실을 판정하고 세션마다 초기화한다.</summary>
    public sealed class MotionWarpMotorStepTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(240)]
        public void 작은_스텝으로_나눠도_벽에_막힌_이동을_감지한다(int rate)
        {
            var owner = new GameObject("워프 충돌 검사");
            try
            {
                var warp = owner.AddComponent<MotionWarpController>();
                var settings = MotionWarpWindowSettings.Default(.3f);
                var handle = warp.BeginWarpWindow(settings);
                int steps = Mathf.RoundToInt(rate * settings.duration);
                for (int step = 0; step < steps; step++)
                {
                    warp.BeginMotorStep(Vector3.zero);
                    warp.RecordRequestedMotorVelocity(Vector3.forward * .4f, 1f / rate);
                    if (step == steps - 1) warp.RequestEndWarpWindow(handle);
                    warp.CompleteMotorStep(Vector3.zero, Vector3.up);
                }

                Assert.That(warp.ConstraintFlags.HasFlag(MotionWarpConstraintFlags.KccBlocked), Is.True);
                Assert.That(warp.LastEndReason, Is.EqualTo(MotionWarpEndReason.Blocked));

                warp.BeginWarpWindow(settings);
                warp.BeginMotorStep(Vector3.zero);
                warp.RecordRequestedMotorVelocity(Vector3.forward * .4f, 1f / rate);
                warp.CompleteMotorStep(Vector3.forward * (.4f / rate), Vector3.up);
                Assert.That(warp.ConstraintFlags.HasFlag(MotionWarpConstraintFlags.KccBlocked), Is.False);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void 충돌_판정_허용오차_이내의_누적_손실은_차단하지_않는다()
        {
            var owner = new GameObject("워프 오차 검사");
            try
            {
                var warp = owner.AddComponent<MotionWarpController>();
                warp.BeginWarpWindow(MotionWarpWindowSettings.Default(.3f));
                for (int step = 0; step < 30; step++)
                {
                    warp.BeginMotorStep(Vector3.zero);
                    warp.RecordRequestedMotorVelocity(Vector3.forward, .01f);
                    warp.CompleteMotorStep(Vector3.forward * .0099f, Vector3.up);
                }
                Assert.That(warp.ConstraintFlags.HasFlag(MotionWarpConstraintFlags.KccBlocked), Is.False);
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
