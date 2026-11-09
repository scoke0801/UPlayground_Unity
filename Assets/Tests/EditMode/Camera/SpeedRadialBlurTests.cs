using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.Camera.Tests
{
    /// <summary>지상 질주·낙하·연출 전환에서 활공 블러가 오작동하지 않는지 검사한다.</summary>
    public sealed class SpeedRadialBlurTests
    {
        private CameraSettings _settings;
        private SpeedRadialBlur _blur;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<CameraSettings>();
            _blur = new SpeedRadialBlur();
        }

        [TearDown]
        public void TearDown()
        {
            SpeedRadialBlurState.Clear();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        private static CameraMotionContext Glide => new CameraMotionContext(true, false,
            Vector3.forward * 18f, Vector3.up, Vector3.up, true, 1f);

        [Test]
        public void 정면비행은방사형_측면비행은수평_후면비행은역방향으로흐른다()
        {
            var forward = SpeedRadialBlur.CalculateFlow(Quaternion.identity, 60f, 16f / 9f, Vector3.forward);
            var side = SpeedRadialBlur.CalculateFlow(Quaternion.identity, 60f, 16f / 9f, Vector3.right);
            var backward = SpeedRadialBlur.CalculateFlow(Quaternion.identity, 60f, 16f / 9f, Vector3.back);
            Assert.That(forward, Is.EqualTo(Vector3.forward));
            Assert.That(side.x, Is.LessThan(0f));
            Assert.That(side.y, Is.Zero);
            Assert.That(side.z, Is.Zero);
            Assert.That(backward, Is.EqualTo(Vector3.back));
        }

        [Test]
        public void 이동방향을바꾸지않고_카메라를회전해도_화면흐름은갱신된다()
        {
            var flow = SpeedRadialBlur.CalculateFlow(Quaternion.Euler(0f, 90f, 0f), 60f,
                16f / 9f, Vector3.forward * 18f);
            Assert.That(flow.x, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(flow.z), Is.LessThan(0.0001f));
        }

        [Test]
        public void 화면비가넓어져도_측면흐름의픽셀비율은유지된다()
        {
            var standard = SpeedRadialBlur.CalculateFlow(Quaternion.identity, 60f, 16f / 9f, Vector3.right);
            var wide = SpeedRadialBlur.CalculateFlow(Quaternion.identity, 60f, 21f / 9f, Vector3.right);
            Assert.That(standard.x * 16f, Is.EqualTo(wide.x * 21f).Within(0.0001f));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 지연시간을넘는프레임의길이가달라도_같은강도에도달한다(int frameRate)
        {
            _blur.Advance(_settings, Glide, true, 0.6f);
            float expected = _blur.Strength;
            _blur.Reset();
            for (int frame = 0; frame < frameRate * 3 / 5; frame++)
                _blur.Advance(_settings, Glide, true, 1f / frameRate);
            Assert.That(_blur.Strength, Is.EqualTo(expected).Within(0.00001f));
        }

        [Test]
        public void 진입기준근처의속도변동이_유지시간을초기화하지않는다()
        {
            var fast = new CameraMotionContext(true, false, Vector3.forward * 14f,
                Vector3.up, Vector3.up, isGliding: true, speedFeedback: 0.36f);
            var nearThreshold = new CameraMotionContext(true, false, Vector3.forward * 14f,
                Vector3.up, Vector3.up, isGliding: true, speedFeedback: 0.34f);
            _blur.Advance(_settings, fast, true, 0.2f);
            _blur.Advance(_settings, nearThreshold, true, 0.2f);
            Assert.That(_blur.Strength, Is.GreaterThan(0f));
        }

        [Test]
        public void 중간가속에서도_최대강도의절반이상을_보장한다()
        {
            var accelerating = new CameraMotionContext(true, false, Vector3.forward * 14f,
                Vector3.up, Vector3.up, true, 0.4f);
            _blur.Advance(_settings, accelerating, true, 2f);
            Assert.That(_blur.Strength, Is.GreaterThan(_settings.glidingRadialBlurStrength * 0.5f));
        }

        [Test]
        public void 짧은활공가속은_블러를켜지않고_유지할때만_등장한다()
        {
            _blur.Advance(_settings, Glide, true, _settings.speedRadialBlurDelay * 0.5f);
            Assert.That(_blur.Strength, Is.Zero);
            _blur.Advance(_settings, Glide, true, _settings.speedRadialBlurDelay);
            Assert.That(_blur.Strength, Is.GreaterThan(0f).And.LessThanOrEqualTo(_settings.glidingRadialBlurStrength));
        }

        [Test]
        public void 잠깐감속하면_다음고속유지시간을_새로센다()
        {
            _blur.Advance(_settings, Glide, true, 0.2f);
            _blur.Advance(_settings, default, true, 0.016f);
            _blur.Advance(_settings, Glide, true, 0.2f);
            Assert.That(_blur.Strength, Is.Zero);
        }

        [Test]
        public void 고속낙하만으로는_활공블러가켜지지않는다()
        {
            var falling = new CameraMotionContext(true, false,
                new Vector3(0, -50, 18), Vector3.up, Vector3.up);
            _blur.Advance(_settings, falling, true, 1f);
            Assert.That(_blur.Strength, Is.Zero);
        }

        [Test]
        public void 지상질주에는_블러가적용되지않고_활공종료시즉시해제된다()
        {
            var sprint = new CameraMotionContext(true, true, Vector3.forward * 10f,
                Vector3.up, Vector3.up, sprintSpeedFeedback: 1f);
            _blur.Advance(_settings, sprint, true, 2f);
            Assert.That(_blur.Strength, Is.Zero);
            _blur.Advance(_settings, Glide, true, 2f);
            Assert.That(_blur.Strength, Is.GreaterThan(0f));
            _blur.Advance(_settings, sprint, true, 0.016f);
            Assert.That(_blur.Strength, Is.Zero);
        }

        [Test]
        public void 감속은부드럽게복귀하고_연출전환은즉시해제한다()
        {
            _blur.Advance(_settings, Glide, true, 1f);
            float previous = _blur.Strength;
            var stopped = new CameraMotionContext(true, false, Vector3.forward * 10f, Vector3.up, Vector3.up, true, 0f);
            _blur.Advance(_settings, stopped, true, 0.016f);
            Assert.That(_blur.Strength, Is.GreaterThan(0f).And.LessThan(previous));
            _blur.Advance(_settings, Glide, false, 0.016f);
            Assert.That(_blur.Strength, Is.Zero);
        }

        [Test]
        public void 일시정지는_강도와유지시간을_진행하지않는다()
        {
            _blur.Advance(_settings, Glide, true, 1f);
            float previous = _blur.Strength;
            _blur.Advance(_settings, Glide, true, 0f);
            Assert.That(_blur.Strength, Is.EqualTo(previous));
        }

        [Test]
        public void 옵션을끄면_즉시해제한다()
        {
            _blur.Advance(_settings, Glide, true, 1f);
            _settings.enableSpeedRadialBlur = false;
            _blur.Advance(_settings, Glide, true, 0.016f);
            Assert.That(_blur.Strength, Is.Zero);
        }
    }
}
