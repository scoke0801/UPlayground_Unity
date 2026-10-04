using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Config;

namespace UPlayGround.CameraSystem.Tests
{
    /// <summary>수동 입력 우선권, 다중 명중의 안정성, 화면 경계와 저장 옵션을 검증한다.</summary>
    public sealed class CameraHitAssistTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private CameraSettings _settings;
        private CameraContext _context;
        private CameraPose _pose;
        private TestAdapter _adapter;

        private sealed class TestAdapter : CameraRuntimeAdapterBase
        {
            public CameraUserPreferences Preferences = CameraUserPreferences.Default;
            public readonly HashSet<Transform> DeadTargets = new HashSet<Transform>();
            public override CameraUserPreferences UserPreferences => Preferences;
            public override bool TryResolveTarget(Component candidate, out CameraTargetInfo target)
            {
                target = new CameraTargetInfo(candidate.transform, !DeadTargets.Contains(candidate.transform),
                    true, true, default);
                return true;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<CameraSettings>();
            Camera camera = CreateTarget(new Vector3(0f, 0f, -5.7f)).gameObject.AddComponent<Camera>();
            camera.aspect = 16f / 9f;
            camera.fieldOfView = 58f;
            _context = new CameraContext(new CameraState())
            {
                Settings = _settings,
                MainCamera = camera,
                Target = CreateTarget(Vector3.zero),
                LastManualInputTime = -100f,
                CollisionLayers = 1 << 29,
                RotationTransition = new CameraRotationTransition()
            };
            _pose = new CameraPose
            {
                CameraPosition = camera.transform.position,
                CameraRotation = Quaternion.identity,
                PivotPosition = Vector3.zero,
                Distance = 5.7f,
                FieldOfView = 58f
            };
            _adapter = new TestAdapter();
            CameraRuntimeServices.Configure(_adapter);
        }

        [TearDown]
        public void TearDown()
        {
            CameraRuntimeServices.Reset();
            foreach (GameObject item in _objects)
                UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void 화면_중앙의_적은_명중해도_시점을_돌리지_않는다()
        {
            Submit(CreateTarget(new Vector3(2f, 0f, 8f)));
            Step(0.2f);
            Assert.That(_pose.Yaw, Is.Zero);
            Assert.That(_context.HitAssist.IsActive, Is.False);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void 화면_가장자리_보정은_프레임률과_무관하게_각도와_속도를_제한한다(int frameRate)
        {
            Submit(CreateTarget(new Vector3(9f, 0f, 8f)));
            for (int i = 0; i < frameRate; i++)
            {
                float previous = _pose.Yaw;
                Step(1f / frameRate);
                Assert.That(_pose.Yaw - previous,
                    Is.LessThanOrEqualTo(_settings.hitAssistMaxYawSpeed / frameRate + 0.001f));
            }
            Assert.That(_pose.Yaw, Is.EqualTo(_settings.hitAssistMaxYawPerPulse).Within(0.02f));
            Assert.That(_pose.Pitch, Is.Zero);
            Assert.That(_pose.Distance, Is.EqualTo(5.7f));
        }

        [Test]
        public void 안전_영역_경계까지만_돌고_중앙으로_끌어오지_않는다()
        {
            Transform target = CreateTarget(new Vector3(7f, 0f, 8f));
            Submit(target);
            Step(0.3f);
            CameraDeadZoneTracker.TryProject(_pose, target.position, _context.MainCamera.aspect, out Vector2 viewport);
            Assert.That(viewport.x, Is.EqualTo(_settings.hitAssistHorizontalZone.y).Within(0.002f));
        }

        [Test]
        public void 광역_명중_순서가_바뀌어도_대표_대상은_같다()
        {
            Transform left = CreateTarget(new Vector3(-8.5f, 0f, 8f));
            Transform right = CreateTarget(new Vector3(8f, 0f, 8f));
            Submit(left);
            Submit(right);
            Step(0f);
            Assert.That(_context.HitAssist.CurrentTarget, Is.SameAs(right));
            _context.HitAssist.Reset();
            Submit(right);
            Submit(left);
            Step(0f);
            Assert.That(_context.HitAssist.CurrentTarget, Is.SameAs(right));
        }

        [Test]
        public void 다단_명중이_진행_중인_회전량과_시간을_늘리지_않는다()
        {
            Transform target = CreateTarget(new Vector3(9f, 0f, 8f));
            for (int i = 0; i < 15; i++)
            {
                Submit(target);
                Submit(target);
                Step(1f / 60f);
            }
            Assert.That(_pose.Yaw, Is.EqualTo(6f).Within(0.02f));
            Assert.That(_context.HitAssist.IsActive, Is.False);
        }

        [Test]
        public void 대표_대상_유지_중에는_다른_적_명중으로_좌우_반전하지_않는다()
        {
            Transform right = CreateTarget(new Vector3(8f, 0f, 8f));
            Transform left = CreateTarget(new Vector3(-8f, 0f, 8f));
            Submit(right);
            Step(0.2f);
            float yaw = _pose.Yaw;
            Submit(left);
            Step(0.15f);
            Assert.That(_context.HitAssist.CurrentTarget, Is.SameAs(right));
            Assert.That(_pose.Yaw, Is.EqualTo(yaw));
        }

        [Test]
        public void 수동_입력_유예_중에는_새_명중도_보정하지_않는다()
        {
            _context.LastManualInputTime = Time.unscaledTime;
            Submit(CreateTarget(new Vector3(8f, 0f, 8f)));
            Step(0.1f);
            Assert.That(_pose.Yaw, Is.Zero);
        }

        [Test]
        public void 조준_보조를_꺼도_독립된_타격_보조는_작동한다()
        {
            _adapter.Preferences = new CameraUserPreferences(true, 1, 1, false, true, 1, false, 1, 1,
                hitAssistEnabled: true);
            Submit(CreateTarget(new Vector3(8f, 0f, 8f)));
            Step(0.1f);
            Assert.That(_pose.Yaw, Is.GreaterThan(0f));
        }

        [Test]
        public void 보조를_끄면_진행_중인_보정도_중단한다()
        {
            Submit(CreateTarget(new Vector3(8f, 0f, 8f)));
            Step(0.02f);
            float yaw = _pose.Yaw;
            _adapter.Preferences = new CameraUserPreferences(true, 1, 1, false, true, 1, true, 1, 1,
                hitAssistEnabled: false);
            Step(0.2f);
            Assert.That(_pose.Yaw, Is.EqualTo(yaw));
            Assert.That(_context.HitAssist.IsActive, Is.False);
        }

        [Test]
        public void 벽_뒤나_화면_밖_혹은_죽은_적은_보정하지_않는다()
        {
            Transform target = CreateTarget(new Vector3(8f, 0f, 8f));
            GameObject wall = CreateTarget(new Vector3(4f, 0f, 1f)).gameObject;
            wall.layer = 29;
            wall.AddComponent<BoxCollider>().size = new Vector3(20f, 20f, 0.2f);
            Physics.SyncTransforms();
            Submit(target);
            Step(0.2f);
            Assert.That(_pose.Yaw, Is.Zero);
            wall.SetActive(false);
            _adapter.DeadTargets.Add(target);
            Submit(target);
            Step(0.2f);
            Assert.That(_pose.Yaw, Is.Zero);
            Submit(CreateTarget(new Vector3(8f, 20f, 8f)));
            Step(0.2f);
            Assert.That(_pose.Yaw, Is.Zero);
        }

        [Test]
        public void 모드_이탈은_대기_명중을_복귀_시_재생하지_않는다()
        {
            Submit(CreateTarget(new Vector3(8f, 0f, 8f)));
            new HitAssistCameraModifier().OnExit(_context);
            Step(0.2f);
            Assert.That(_pose.Yaw, Is.Zero);
        }

        [Test]
        public void 이동_정렬은_사용자_옵션을_켠_탐색_중에만_작동한다()
        {
            _adapter.Preferences = new CameraUserPreferences(true, 1, 1, false, true, 1, false, 0, 1,
                movementRecenteringEnabled: true);
            _context.Motion = new CameraMotionContext(true, true, new Vector3(4f, 0f, 0f), Vector3.up, Vector3.up);
            var frame = new CameraFrame { Context = _context, State = _context.State, DeltaTime = 0.1f };
            var align = new AlignCameraModifier();
            _context.CombatStateProvider = () => true;
            align.Apply(ref frame);
            Assert.That(_context.State.CurrentYaw, Is.Zero);
            _context.CombatStateProvider = () => false;
            align.Apply(ref frame);
            Assert.That(_context.State.CurrentYaw, Is.GreaterThan(0f).And.LessThanOrEqualTo(4.5f));
        }

        [Test]
        public void 설정_취소와_초기화는_두_카메라_옵션을_독립적으로_복원한다()
        {
            SettingsData data = ScriptableObject.CreateInstance<SettingsData>();
            try
            {
                data.hitCameraAssist = false;
                data.movementCameraRecentering = true;
                SettingsSnapshot snapshot = SettingsSnapshot.From(data);
                data.ResetToDefault();
                Assert.That(data.hitCameraAssist, Is.True);
                Assert.That(data.movementCameraRecentering, Is.False);
                snapshot.ApplyTo(data);
                Assert.That(data.hitCameraAssist, Is.False);
                Assert.That(data.movementCameraRecentering, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        private Transform CreateTarget(Vector3 position)
        {
            var item = new GameObject("카메라 보정 테스트");
            item.transform.position = position;
            _objects.Add(item);
            return item.transform;
        }

        private void Submit(Transform target) => _context.HitAssist.Submit(new CameraHitAssist.Request(
            target, target.position, 1f, 60f, 0.12f, 0.45f));

        private void Step(float deltaTime) => _context.HitAssist.Apply(ref _pose, _context, deltaTime);
    }
}
