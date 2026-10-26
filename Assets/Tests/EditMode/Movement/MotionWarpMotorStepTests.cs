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

        [TestCase(15f)]
        [TestCase(30f)]
        public void 경사면_투영으로_줄어든_수평_이동은_충돌_차단으로_보지_않는다(float slopeAngle)
        {
            var owner = new GameObject("워프 경사 검사");
            try
            {
                var warp = owner.AddComponent<MotionWarpController>();
                warp.BeginWarpWindow(MotionWarpWindowSettings.Default(.3f));
                // KCC는 지면 위에서 속도를 경사 접선으로 돌려 크기를 유지하므로 수평 진행이 cos만큼 줄어든다.
                float step = 6f / 60f;
                Vector3 applied = Quaternion.AngleAxis(-slopeAngle, Vector3.right) * Vector3.forward * step;
                Vector3 position = Vector3.zero;
                for (int frame = 0; frame < 60; frame++)
                {
                    warp.BeginMotorStep(position);
                    warp.RecordRequestedMotorVelocity(Vector3.forward * 6f, 1f / 60f);
                    position += applied;
                    warp.CompleteMotorStep(position, Vector3.up);
                }
                Assert.That(warp.ConstraintFlags.HasFlag(MotionWarpConstraintFlags.KccBlocked), Is.False);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void 요청_방향으로_나아가지_못하고_옆으로_밀리면_차단으로_본다()
        {
            var owner = new GameObject("워프 측면 밀림 검사");
            try
            {
                var warp = owner.AddComponent<MotionWarpController>();
                warp.BeginWarpWindow(MotionWarpWindowSettings.Default(.3f));
                Vector3 position = Vector3.zero;
                for (int frame = 0; frame < 6; frame++)
                {
                    warp.BeginMotorStep(position);
                    warp.RecordRequestedMotorVelocity(Vector3.forward * 3f, 1f / 60f);
                    position += Vector3.right * (3f / 60f);
                    warp.CompleteMotorStep(position, Vector3.up);
                }
                Assert.That(warp.ConstraintFlags.HasFlag(MotionWarpConstraintFlags.KccBlocked), Is.True);
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
