using NUnit.Framework;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    public class MotionWarpArrivalUtilityTests
    {
        [Test]
        public void ResolveContactShell_양쪽반경과간격만큼_타겟중심앞에배치한다()
        {
            Vector3 result = MotionWarpArrivalUtility.ResolveContactShell(
                Vector3.zero,
                new Vector3(0f, 0f, 5f),
                0.5f,
                0.75f,
                0.25f,
                Vector3.zero,
                Quaternion.identity);

            Assert.That(result, Is.EqualTo(new Vector3(0f, 0f, 3.5f)));
        }

        [Test]
        public void ResolveContactShell_큰타겟일수록_중심이아닌표면기준간격을유지한다()
        {
            Vector3 small = MotionWarpArrivalUtility.ResolveContactShell(
                Vector3.zero, Vector3.forward * 5f, 0.5f, 0.5f, 0.2f, Vector3.zero, Quaternion.identity);
            Vector3 large = MotionWarpArrivalUtility.ResolveContactShell(
                Vector3.zero, Vector3.forward * 5f, 0.5f, 1.5f, 0.2f, Vector3.zero, Quaternion.identity);

            Assert.That(small.z, Is.EqualTo(3.8f).Within(0.0001f));
            Assert.That(large.z, Is.EqualTo(2.8f).Within(0.0001f));
        }

        [Test]
        public void CanTranslate_도착오차가DeadZone안이면_false다()
        {
            bool result = MotionWarpArrivalUtility.CanTranslate(
                0.05f, 0.08f, Vector3.forward, Vector3.forward, 45f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CanTranslate_타겟중심이근거리여도_도착오차가남으면_true다()
        {
            bool result = MotionWarpArrivalUtility.CanTranslate(
                0.3f, 0.08f, Vector3.forward, Vector3.forward, 45f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void CanTranslate_허용각도밖이면_false다()
        {
            bool result = MotionWarpArrivalUtility.CanTranslate(
                3f, 1.5f, Vector3.forward, Vector3.right, 45f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void LimitCorrection_절대보정상한에서클램프한다()
        {
            Vector3 result = MotionWarpArrivalUtility.LimitCorrection(
                Vector3.right * 2f, 10f, 0.5f, 1f);

            Assert.That(result, Is.EqualTo(Vector3.right * 0.5f));
        }

        [Test]
        public void LimitCorrection_원본이동량대비비율상한에서클램프한다()
        {
            Vector3 result = MotionWarpArrivalUtility.LimitCorrection(
                Vector3.forward * 2f, 1f, 10f, 0.3f);

            Assert.That(result, Is.EqualTo(Vector3.forward * 0.3f));
        }

        [Test]
        public void ResolveCorrectionReferenceDistance_작은원본보다도착오차가크면_도착오차를사용한다()
        {
            float result = MotionWarpArrivalUtility.ResolveCorrectionReferenceDistance(
                0.09f,
                0.7f);

            Assert.That(result, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void LimitCorrection_인플레이스여도_도착오차기준예산을허용한다()
        {
            float referenceDistance =
                MotionWarpArrivalUtility.ResolveCorrectionReferenceDistance(0f, 0.7f);
            Vector3 result = MotionWarpArrivalUtility.LimitCorrection(
                Vector3.forward * 0.7f,
                referenceDistance,
                0.5f,
                0.3f);

            Assert.That(result.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(result.z, Is.EqualTo(0.21f).Within(0.0001f));
        }

        [Test]
        public void LimitAccumulatedCorrection_여러프레임에도_전체예산을초과하지않는다()
        {
            Vector3 accumulated = Vector3.zero;
            const float budget = 0.5f;

            for (int frame = 0; frame < 10; frame++)
            {
                Vector3 allowed =
                    MotionWarpArrivalUtility.LimitAccumulatedCorrection(
                        accumulated,
                        Vector3.forward * 0.2f,
                        budget);
                accumulated += allowed;
            }

            Assert.That(accumulated.magnitude, Is.EqualTo(budget).Within(0.0001f));
        }

        [Test]
        public void LimitAccumulatedCorrection_방향이바뀌어도_누적벡터는예산안이다()
        {
            Vector3 accumulated = Vector3.forward * 0.4f;
            Vector3 allowed =
                MotionWarpArrivalUtility.LimitAccumulatedCorrection(
                    accumulated,
                    Vector3.right * 0.4f,
                    0.5f);

            accumulated += allowed;

            Assert.That(accumulated.magnitude, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ResolveCorrectionStepScale_속도클램프로방향이바뀌어도_예산원을벗어나지않는다()
        {
            Vector3 accumulated = Vector3.right;
            Vector3 candidateStep =
                new Vector3(-0.000102f, 0f, 0.020201f);
            float scale =
                MotionWarpArrivalUtility.ResolveCorrectionStepScale(
                    accumulated,
                    candidateStep,
                    1f);

            Vector3 result = accumulated + candidateStep * scale;

            Assert.That(scale, Is.GreaterThanOrEqualTo(0f));
            Assert.That(scale, Is.LessThanOrEqualTo(1f));
            Assert.That(result.magnitude, Is.LessThanOrEqualTo(1.000001f));
        }

        [Test]
        public void ResolveVerticalVelocity_Translation차단이면_MatchTargetY도원본Y를유지한다()
        {
            float result = MotionWarpArrivalUtility.ResolveVerticalVelocity(
                -3f,
                12f,
                0f,
                WarpYPolicy.MatchTargetY,
                1f);

            Assert.That(result, Is.EqualTo(-3f).Within(0.0001f));
        }

        [Test]
        public void ResolveVerticalVelocity_MatchTargetY는Translation가중치를반영한다()
        {
            float result = MotionWarpArrivalUtility.ResolveVerticalVelocity(
                0f,
                10f,
                0.25f,
                WarpYPolicy.MatchTargetY,
                1f);

            Assert.That(result, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void ResolveForwardTimeDelta_포즈시간이되감기면_음수시간을소모하지않는다()
        {
            float result = MotionWarpArrivalUtility.ResolveForwardTimeDelta(
                0.8f,
                0.2f);

            Assert.That(result, Is.EqualTo(0f));
        }

        [TestCase(0.9f, 1f / 0.9f)]
        [TestCase(1.1f, 1f / 1.1f)]
        public void ResolvePhysicalRemainingTime_포즈속도를실제남은시간으로환산한다(
            float authoredRate,
            float expected)
        {
            float result =
                MotionWarpArrivalUtility.ResolvePhysicalRemainingTime(
                    1f,
                    authoredRate);

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ResolveFallbackVelocity_미베이크인플레이스도_유한한보정속도를만든다()
        {
            Vector3 result = MotionWarpArrivalUtility.ResolveFallbackVelocity(
                Vector3.zero,
                Vector3.forward * 0.7f,
                0.2f,
                1f / 60f,
                0.5f,
                0.3f);

            Assert.That(float.IsNaN(result.z) || float.IsInfinity(result.z), Is.False);
            Assert.That(result.z, Is.EqualTo(1.05f).Within(0.0001f));
        }

        [Test]
        public void ResolveAuthoredWarpPoint_공격자접촉점을_타겟접촉점에맞춘다()
        {
            Vector3 result = MotionWarpArrivalUtility.ResolveAuthoredWarpPoint(
                new Vector3(0f, 1f, 4f),
                new Vector3(0f, 1f, 0.5f),
                Quaternion.identity,
                Vector3.zero,
                Quaternion.identity);

            Assert.That(result, Is.EqualTo(new Vector3(0f, 0f, 3.5f)));
        }
    }
}
