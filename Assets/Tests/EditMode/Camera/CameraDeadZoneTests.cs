using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem.Tests
{
    /// <summary>화면 구도의 정지·복귀·안정성과 연출 분리를 검증한다.</summary>
    public sealed class CameraDeadZoneTests
    {
        private CameraSettings _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<CameraSettings>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_settings);

        [TestCase(1.333333f, 52f)]
        [TestCase(1.777778f, 58f)]
        [TestCase(2.333333f, 70f)]
        public void 데드존_안의_대상은_중앙으로_끌어오지_않는다(float aspect, float fov)
        {
            CameraPose pose = CreatePose(fov);
            Vector3 focus = PointAtViewport(pose, new Vector2(0.61f, 0.4f), aspect, 12f);
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < 180; i++)
                tracker.Track(ref pose, focus, _settings, aspect, 1f / 60f);
            Assert.That(pose.Yaw, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(pose.Pitch, Is.EqualTo(0f).Within(0.0001f));
        }

        [TestCase(0.9f, 0.5f)]
        [TestCase(0.1f, 0.5f)]
        [TestCase(0.5f, 0.9f)]
        [TestCase(0.5f, 0.1f)]
        public void 경계_밖의_대상은_가까운_경계에서_멈춘다(float x, float y)
        {
            const float aspect = 16f / 9f;
            CameraPose pose = CreatePose();
            Vector3 focus = PointAtViewport(pose, new Vector2(x, y), aspect, 12f);
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < 600; i++)
                tracker.Track(ref pose, focus, _settings, aspect, 1f / 60f);
            Assert.That(CameraDeadZoneTracker.TryProject(pose, focus, aspect, out Vector2 viewport), Is.True);
            float value = x == 0.5f ? viewport.y : viewport.x;
            float expected = (x == 0.5f ? y : x) > 0.5f ? 0.63f : 0.37f;
            Assert.That(value, Is.EqualTo(expected).Within(0.003f));
            Quaternion settled = pose.CameraRotation;
            for (int i = 0; i < 60; i++)
                tracker.Track(ref pose, focus, _settings, aspect, 1f / 60f);
            Assert.That(Quaternion.Angle(settled, pose.CameraRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void 정적_대상_추적은_프레임률에_따라_달라지지_않는다()
        {
            CameraPose baseline = Simulate(120);
            Assert.That(Quaternion.Angle(baseline.CameraRotation, Simulate(30).CameraRotation), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(baseline.CameraRotation, Simulate(60).CameraRotation), Is.LessThan(0.1f));
        }

        [Test]
        public void 뒤쪽_대상도_순간_반전하지_않고_복귀한다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            Vector3 focus = new Vector3(0f, 0f, -12f);
            tracker.Track(ref pose, focus, _settings, 16f / 9f, 1f / 60f);
            Assert.That(Mathf.Abs(pose.Yaw), Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.x / 60f));
            for (int i = 0; i < 600; i++)
                tracker.Track(ref pose, focus, _settings, 16f / 9f, 1f / 60f);
            Assert.That(CameraDeadZoneTracker.TryProject(pose, focus, 16f / 9f, out Vector2 viewport), Is.True);
            Assert.That(viewport.x, Is.InRange(0.35f, 0.65f));
        }

        [Test]
        public void 머리_위를_통과하는_대상은_수평_방향을_뒤집지_않는다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < 120; i++)
                tracker.Track(ref pose, new Vector3(0.001f, 10f, -0.001f), _settings, 16f / 9f, 1f / 60f);
            Assert.That(pose.Yaw, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.Pitch, Is.InRange(-65f, 0f));
        }

        [Test]
        public void 히트스톱_시간에는_자동_추적이_진행되지_않는다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            tracker.Track(ref pose, new Vector3(12f, 5f, 0f), _settings, 16f / 9f, 0f);
            Assert.That(pose.Yaw, Is.Zero);
            Assert.That(pose.Pitch, Is.Zero);
        }

        [Test]
        public void 흔들림은_기본_각도와_피벗_상태에_누적되지_않는다()
        {
            var state = new CameraState { CurrentYaw = 10f, CurrentPitch = 15f, CameraOffset = Vector3.up };
            var modifier = new EffectRotationInjectCameraModifier();
            for (int i = 0; i < 120; i++)
            {
                var frame = new CameraFrame
                {
                    State = state,
                    Pose = CreatePose(),
                    Effects = new CameraEffectState { yawDelta = 3f, pitchDelta = 2f, offsetDelta = Vector3.right }
                };
                modifier.Apply(ref frame);
            }
            Assert.That(state.CurrentYaw, Is.EqualTo(10f));
            Assert.That(state.CurrentPitch, Is.EqualTo(15f));
            Assert.That(state.CameraOffset, Is.EqualTo(Vector3.up));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 적을_가까이_지나칠_때_시야를_유지하고_멀어지면_복귀한다(int fps)
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            float deltaTime = 1f / fps;
            for (int i = 0; i <= fps; i++)
            {
                Vector3 focus = new Vector3(1.5f, 0f, Mathf.Lerp(6f, -2f, (float)i / fps));
                tracker.Track(ref pose, focus, _settings, 16f / 9f, deltaTime);
                Assert.That(pose.Yaw, Is.EqualTo(0f).Within(0.001f));
            }

            Vector3 departedFocus = new Vector3(1.5f, 0f, -8f);
            for (int i = 0; i < fps * 6; i++)
            {
                float previousYaw = pose.Yaw;
                tracker.Track(ref pose, departedFocus, _settings, 16f / 9f, deltaTime);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousYaw, pose.Yaw)),
                    Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.x * deltaTime + 0.001f));
            }
            Assert.That(CameraDeadZoneTracker.TryProject(pose, departedFocus, 16f / 9f, out Vector2 viewport), Is.True);
            Assert.That(viewport.x, Is.InRange(0.35f, 0.65f));
        }

        [Test]
        public void 근접_경계에서_왕복해도_기존_방향을_유지한다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            tracker.Track(ref pose, new Vector3(1.8f, 0f, -0.8f), _settings, 16f / 9f, 1f / 60f);
            for (int i = 0; i < 180; i++)
            {
                float x = i % 2 == 0 ? 1.8f : 2.3f;
                tracker.Track(ref pose, new Vector3(x, 0f, -0.8f), _settings, 16f / 9f, 1f / 60f);
            }
            Assert.That(pose.Yaw, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void 교차_보호를_끄면_기존_데드존으로_추적한다()
        {
            _settings.enableLockOnCrossingProtection = false;
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < 120; i++)
                tracker.Track(ref pose, new Vector3(1.8f, 0f, -0.8f), _settings, 16f / 9f, 1f / 60f);
            Assert.That(pose.Yaw, Is.GreaterThan(5f));
        }

        [Test]
        public void 벽으로_카메라가_당겨지면_근접_대상도_화면으로_복귀한다()
        {
            CameraPose pose = CreatePose();
            pose.Distance = 0.5f;
            pose.CameraPosition = Vector3.back * pose.Distance;
            var tracker = new CameraDeadZoneTracker();
            Vector3 focus = new Vector3(0.2f, 0f, -1.5f);
            for (int i = 0; i < 600; i++)
            {
                float previousYaw = pose.Yaw;
                tracker.Track(ref pose, focus, _settings, 16f / 9f, 1f / 60f);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousYaw, pose.Yaw)),
                    Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.x
                        * _settings.lockOnCrossingYawSpeedScale / 60f + 0.001f));
            }
            Assert.That(CameraDeadZoneTracker.TryProject(pose, focus, 16f / 9f, out Vector2 viewport), Is.True);
            Assert.That(viewport.x, Is.InRange(0.1f, 0.9f));
        }

        [Test]
        public void 한_프레임에_적을_통과해도_구도를_즉시_뒤집지_않는다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            tracker.Track(ref pose, Vector3.forward * 4f, _settings, 16f / 9f, 1f / 60f,
                Vector3.forward * 4f);
            tracker.Track(ref pose, new Vector3(1.8f, 0f, -0.8f), _settings, 16f / 9f, 1f / 60f,
                Vector3.back * 4f);
            Assert.That(pose.Yaw, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void 교차_판정은_보간된_포커스보다_실제_대상_거리를_사용한다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            Vector3 laggedFocus = new Vector3(3f, 0f, 0f);
            for (int i = 0; i < 60; i++)
                tracker.Track(ref pose, laggedFocus, _settings, 16f / 9f, 1f / 60f, Vector3.forward);
            Assert.That(pose.Yaw, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void 후방_대상의_좌우_흔들림에도_회전_방향이_번갈아_바뀌지_않는다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < 10; i++)
            {
                float previousYaw = pose.Yaw;
                float x = i % 2 == 0 ? -0.8f : 0.8f;
                tracker.Track(ref pose, new Vector3(x, 0f, -12f), _settings, 16f / 9f, 1f / 60f);
                Assert.That(Mathf.DeltaAngle(previousYaw, pose.Yaw), Is.LessThan(0f));
            }
        }

        [Test]
        public void 대상_전환_후에는_이전_근접_구도와_회전_방향을_사용하지_않는다()
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            tracker.Track(ref pose, new Vector3(-0.8f, 0f, -12f), _settings, 16f / 9f, 1f / 60f);
            tracker.Track(ref pose, Vector3.forward, _settings, 16f / 9f, 1f / 60f);
            tracker.Reset();
            pose = CreatePose();
            tracker.Track(ref pose, new Vector3(0.8f, 0f, -12f), _settings, 16f / 9f, 1f / 60f);
            Assert.That(pose.Yaw, Is.GreaterThan(0f));
            tracker.Reset();
            pose = CreatePose();
            tracker.Track(ref pose, new Vector3(3f, 0f, 0f), _settings, 16f / 9f, 1f / 60f);
            Assert.That(pose.Yaw, Is.GreaterThan(0f));
        }

        [TestCase(30, -1.4f)]
        [TestCase(60, -1.4f)]
        [TestCase(120, -1.4f)]
        [TestCase(60, 1.4f)]
        public void 피벗과_카메라_사이의_대상은_피치가_발산하지_않고_경계로_복귀한다(int fps, float height)
        {
            CameraPose pose = CreatePose();
            Vector3 focus = new Vector3(0f, height, -1.5f);
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < fps * 6; i++)
            {
                float previousPitch = pose.Pitch;
                tracker.Track(ref pose, focus, _settings, 16f / 9f, 1f / fps);
                Assert.That(Mathf.Abs(pose.Pitch - previousPitch),
                    Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.y / fps + 0.001f));
            }
            Assert.That(CameraDeadZoneTracker.TryProject(pose, focus, 16f / 9f, out Vector2 viewport), Is.True);
            Assert.That(viewport.y, Is.EqualTo(height < 0f ? 0.37f : 0.63f).Within(0.003f));
            Assert.That(Mathf.Abs(pose.Pitch), Is.LessThan(40f));
            float settledPitch = pose.Pitch;
            for (int i = 0; i < fps; i++)
                tracker.Track(ref pose, focus, _settings, 16f / 9f, 1f / fps);
            Assert.That(pose.Pitch, Is.EqualTo(settledPitch).Within(0.01f));
        }

        private CameraPose Simulate(int fps)
        {
            CameraPose pose = CreatePose();
            var tracker = new CameraDeadZoneTracker();
            for (int i = 0; i < fps; i++)
                tracker.Track(ref pose, new Vector3(12f, 3f, 4f), _settings, 16f / 9f, 1f / fps);
            return pose;
        }

        private static CameraPose CreatePose(float fov = 58f) => new CameraPose
        {
            CameraRotation = Quaternion.identity,
            CameraPosition = new Vector3(0f, 0f, -5.7f),
            PivotPosition = Vector3.zero,
            Distance = 5.7f,
            FieldOfView = fov
        };

        private static Vector3 PointAtViewport(CameraPose pose, Vector2 viewport, float aspect, float depth)
        {
            float height = Mathf.Tan(pose.FieldOfView * 0.5f * Mathf.Deg2Rad) * depth;
            return pose.CameraPosition + new Vector3((viewport.x - 0.5f) * 2f * height * aspect,
                (viewport.y - 0.5f) * 2f * height, depth);
        }
    }
}
