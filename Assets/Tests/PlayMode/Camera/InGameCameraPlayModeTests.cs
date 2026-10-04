using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem.PlayModeTests
{
    /// <summary>실제 프레임에서 인게임 Modifier 파이프라인과 충돌·연출의 합성을 검증한다.</summary>
    public sealed class InGameCameraPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private CameraSettings _settings;
        private CameraContext _context;
        private InGameCameraBehavior _behavior;
        private readonly CameraResolver _resolver = new CameraResolver();

        private sealed class TestAdapter : CameraRuntimeAdapterBase
        {
            public Transform Target;
            public override bool TryResolveTarget(Component candidate, out CameraTargetInfo target)
            {
                target = new CameraTargetInfo(Target, true, true, true, default);
                return candidate != null && candidate.transform == Target;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<CameraSettings>();
            _settings.defaultOffset = Vector3.zero;
            _settings.combatOffset = Vector3.zero;
            _settings.fovExplore = _settings.fovCombat = _settings.fovLockOn;
            _settings.enableTraversalComposition = false;
            _settings.enableLockOnPitchRecovery = false;
            var camera = CreateObject("카메라", new Vector3(0f, 0f, -5.7f)).AddComponent<Camera>();
            camera.enabled = false;
            camera.aspect = 16f / 9f;
            camera.fieldOfView = _settings.fovLockOn;
            Transform player = CreateObject("플레이어", Vector3.zero).transform;
            Transform target = CreateObject("대상", new Vector3(0f, 0f, 8f)).transform;
            target.gameObject.AddComponent<CapsuleCollider>();
            CameraRuntimeServices.Configure(new TestAdapter { Target = target });
            var state = new CameraState { TargetDistance = 5.7f, CurrentDistance = 5.7f };
            _context = new CameraContext(state)
            {
                MainCamera = camera,
                Target = player,
                CameraPivot = CreateObject("피벗", Vector3.zero).transform,
                Settings = _settings,
                LockOn = new CameraLockOn(_settings, player, camera, 0, 1 << 29),
                Collision = new CameraCollision(_settings, player, 1 << 29, 5.7f),
                DistanceController = new CameraDistanceController(_settings, player, 0, _settings.fovLockOn)
            };
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            _behavior = new InGameCameraBehavior();
            _behavior.OnEnter(_context, default);
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

        [UnityTest]
        public IEnumerator 연속_흔들림_후에도_데드존_기본_구도는_그대로_남는다()
        {
            for (int i = 0; i < 45; i++)
            {
                _context.HasActiveEffects = true;
                Apply(new CameraEffectState { yawDelta = 15f, pitchDelta = -8f, offsetDelta = Vector3.right * 0.2f });
                Assert.That(_context.State.CurrentYaw, Is.EqualTo(0f).Within(0.001f));
                Assert.That(_context.State.CurrentPitch, Is.EqualTo(0f).Within(0.001f));
                yield return null;
            }
            _context.HasActiveEffects = false;
            Apply(default);
            Assert.That(Quaternion.Angle(_context.MainCamera.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
            _context.LockOn.Release();
            Apply(default);
            Assert.That(_context.IsAligning, Is.False);
            Assert.That(_context.State.CurrentYaw, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator 락온_피치는_입력_잠금_후_추적이_재개되면_자동_복귀한다()
        {
            _settings.enableLockOnPitchRecovery = true;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 0f, 1f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            _context.IsInputLocked = true;
            for (int i = 0; i < 60; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.State.CurrentPitch, Is.Zero);
            _context.IsInputLocked = false;
            for (int i = 0; i < 720; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.State.CurrentPitch, Is.EqualTo(_settings.lockOnPreferredPitch).Within(0.01f));
            Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
            Vector3 viewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.y, Is.InRange(0.35f, 0.65f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator 벽_앞에서_데드존보다_충돌_안전을_우선한다()
        {
            GameObject wall = CreateObject("벽", new Vector3(0f, 0f, -2f));
            wall.layer = 29;
            wall.AddComponent<BoxCollider>().size = new Vector3(20f, 20f, 0.5f);
            Physics.SyncTransforms();
            for (int i = 0; i < 45; i++)
            {
                Apply(default);
                Assert.That(_context.MainCamera.transform.position.z, Is.GreaterThan(-1.75f));
                Assert.That(_context.State.CurrentYaw, Is.EqualTo(0f).Within(0.001f));
                yield return null;
            }
            Assert.That(_context.LockOn.IsActive, Is.True);
            Assert.That(_context.State.TargetDistance, Is.EqualTo(5.7f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator 플레이어가_적을_통과해도_락온과_시야를_유지한_뒤_복귀한다()
        {
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(1.5f, 0f, 8f);
            // 포커스 보간이 끝난 실제 파이프라인에서 플레이어만 움직인다.
            for (int i = 0; i < 120; i++)
                ApplyStep(1f / 60f);
            for (int i = 0; i <= 60; i++)
            {
                _context.Target.position = new Vector3(0f, 0f, 10f * i / 60f);
                ApplyStep(1f / 60f);
                Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
                Assert.That(_context.LockOn.CanTrack, Is.True);
                Assert.That(_context.State.CurrentYaw, Is.EqualTo(0f).Within(0.01f));
                yield return null;
            }

            _context.Target.position = new Vector3(0f, 0f, 16f);
            for (int i = 0; i < 360; i++)
            {
                float previousYaw = _context.State.CurrentYaw;
                ApplyStep(1f / 60f);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousYaw, _context.State.CurrentYaw)),
                    Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.x / 60f + 0.001f));
            }
            Vector3 viewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
            Assert.That(_context.LockOn.IsActive, Is.True);
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.x, Is.InRange(0.35f, 0.65f));
        }

        [UnityTest]
        public IEnumerator 높이_차이가_있는_근접_대상도_피치_제한각으로_밀리지_않는다()
        {
            // 궤도 해의 수렴을 검증하므로 근접 피치 하강을 막는 별도 정책은 제외한다.
            _settings.enableLockOnCrossingProtection = false;
            Transform target = _context.LockOn.CurrentTarget;
            _settings.defaultOffset = Vector3.up * 1.4f;
            _settings.combatOffset = _settings.defaultOffset;
            _context.State.CameraOffset = _settings.defaultOffset;
            _context.State.SmoothPosition = _settings.defaultOffset;
            target.position = new Vector3(0f, 0f, -1.5f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Vector3 viewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
            Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.y, Is.InRange(0.35f, 0.65f));
            Assert.That(Mathf.Abs(_context.State.CurrentPitch), Is.LessThan(40f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator 대상_앞의_벽은_연속_가림_지연_후에만_락온을_해제한다()
        {
            _settings.lockOnOcclusionGraceTime = 1.5f;
            Transform target = _context.LockOn.CurrentTarget;
            GameObject wall = CreateObject("시야를 가리는 벽", new Vector3(0f, 0f, 2f));
            wall.layer = 29;
            wall.AddComponent<BoxCollider>().size = new Vector3(20f, 20f, 0.5f);
            Physics.SyncTransforms();
            for (int i = 0; i < 84; i++)
            {
                ApplyStep(1f / 60f);
                Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
                Assert.That(_context.LockOn.CanTrack, Is.True);
            }
            wall.SetActive(false);
            Physics.SyncTransforms();
            ApplyStep(1f / 60f);
            Assert.That(_context.LockOn.CanTrack, Is.True);
            wall.SetActive(true);
            Physics.SyncTransforms();
            for (int i = 0; i < 84; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
            for (int i = 0; i < 12; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.LockOn.IsActive, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 가림_유예_중에도_각도_보정이_가시_상태와_동일하게_이어진다()
        {
            Transform target = _context.LockOn.CurrentTarget;
            GameObject wall = CreateObject("시야를 가리는 벽", new Vector3(0f, 0f, 2f));
            wall.layer = 29;
            wall.AddComponent<BoxCollider>().size = new Vector3(40f, 40f, 0.5f);
            _settings.enableLockOnPitchRecovery = true;
            var visibleAngles = new Vector2[60];
            const float deltaTime = 1f / 60f;

            // 같은 이동을 가시/가림으로 재생하여 포커스 동결과 추적 관성 초기화를 함께 검출한다.
            for (int pass = 0; pass < 2; pass++)
            {
                wall.SetActive(false);
                target.position = new Vector3(0f, 0f, 8f);
                Physics.SyncTransforms();
                Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
                _context.State.CurrentYaw = 0f;
                _context.State.CurrentPitch = 0f;
                _behavior.OnEnter(_context, default);
                ApplyStep(0f);
                wall.SetActive(pass == 1);

                for (int i = 0; i < visibleAngles.Length; i++)
                {
                    target.position = new Vector3(4f + i * 0.05f, 0f, 8f);
                    Physics.SyncTransforms();
                    float previousYaw = _context.State.CurrentYaw;
                    float previousPitch = _context.State.CurrentPitch;
                    ApplyStep(deltaTime);
                    Vector2 angles = new Vector2(_context.State.CurrentYaw, _context.State.CurrentPitch);
                    Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
                    Assert.That(_context.LockOn.CanTrack, Is.True);
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousYaw, angles.x)),
                        Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.x * deltaTime + 0.001f));
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousPitch, angles.y)),
                        Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.y * deltaTime + 0.001f));
                    if (pass == 0)
                        visibleAngles[i] = angles;
                    else
                        Assert.That(Vector2.Distance(angles, visibleAngles[i]), Is.LessThan(0.001f));
                }
                Assert.That(_context.State.CurrentYaw, Is.GreaterThan(1f));
                Assert.That(_context.State.CurrentPitch, Is.GreaterThan(1f));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 바닥에_접촉한_카메라도_락온_피치가_한번에_복귀한다()
        {
            _settings.enableLockOnPitchRecovery = true;
            _settings.defaultOffset = Vector3.up * 1.3f;
            _settings.combatOffset = _settings.defaultOffset;
            _context.State.CameraOffset = _settings.defaultOffset;
            _context.State.SmoothPosition = _settings.defaultOffset;
            _context.State.CurrentPitch = -30f;
            _context.State.TargetDistance = 4.2f;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 1f, 1.5f);
            GameObject floor = CreateObject("락온 검증 바닥", new Vector3(0f, -0.5f, 0f));
            floor.layer = 29;
            floor.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);

            Quaternion initialRotation = Quaternion.Euler(-30f, 0f, 0f);
            float blockedDistance = _context.Collision.Evaluate(_settings.defaultOffset,
                initialRotation * Vector3.back, _context.State.TargetDistance);
            Assert.That(blockedDistance, Is.LessThan(2.2f));
            _context.MainCamera.transform.SetPositionAndRotation(
                _settings.defaultOffset + initialRotation * Vector3.back * blockedDistance, initialRotation);

            bool hasCrossedBoundary = false;
            float elapsed = 0f;
            while (elapsed < 3f)
            {
                yield return null;
                float deltaTime = Time.deltaTime;
                if (deltaTime <= 0f)
                    continue;
                elapsed += deltaTime;
                float previousPitch = _context.State.CurrentPitch;
                Vector3 previousViewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
                ApplyStep(deltaTime);
                Vector3 viewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
                Assert.That(_context.LockOn.CanTrack, Is.True);
                Assert.That(_context.MainCamera.transform.position.y, Is.GreaterThanOrEqualTo(_settings.cameraRadius));
                Assert.That(_context.State.CurrentPitch, Is.GreaterThanOrEqualTo(previousPitch - 0.001f));
                float boundary = _settings.lockOnDeadZone.yMin + _settings.lockOnDeadZoneHysteresis;
                if (!hasCrossedBoundary && previousViewport.y < boundary && viewport.y >= boundary)
                {
                    hasCrossedBoundary = true;
                    Assert.That((_context.State.CurrentPitch - previousPitch) / deltaTime, Is.GreaterThan(20f));
                }
            }
            Assert.That(hasCrossedBoundary, Is.True);
            Assert.That(_context.State.CurrentPitch, Is.EqualTo(_settings.lockOnPreferredPitch).Within(0.5f));
            Assert.That(_context.Collision.CurrentDistance, Is.EqualTo(4.2f).Within(0.05f));
        }

        [TestCase(4f / 3f, 30)]
        [TestCase(16f / 9f, 60)]
        [TestCase(21f / 9f, 120)]
        public void 공중_대상은_피치를_낮추지_않고_발밑과_포커스를_함께_담는다(float aspect, int frameRate)
        {
            PrepareElevatedTarget();
            _context.MainCamera.aspect = aspect;
            float initialFov = _context.MainCamera.fieldOfView;
            for (int i = 0; i < frameRate * 6; i++)
            {
                ApplyStep(1f / frameRate);
                Assert.That(_context.State.CurrentPitch, Is.EqualTo(25f).Within(0.001f));
                Assert.That(_context.State.TargetDistance, Is.EqualTo(4.2f).Within(0.001f));
            }

            Assert.That(_context.LockOn.IsActive, Is.True);
            Assert.That(_context.MainCamera.fieldOfView, Is.EqualTo(initialFov).Within(0.001f));
            Assert.That(_context.CameraPivot.position.y, Is.GreaterThan(1.4f));
            _context.LockOn.TryGetTargetFramingPoints(_settings.lockOnFitTopPadding, out Vector3 focus, out Vector3 top);
            AssertInsideFraming(_context.Target.position);
            AssertInsideFraming(focus);
            AssertPlayerScale();
        }

        [Test]
        public void 공중_프레이밍_해제는_줌_원본을_보존하고_피벗과_거리를_복구한다()
        {
            PrepareElevatedTarget();
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.Collision.CurrentDistance, Is.GreaterThan(4.2f));
            _context.LockOn.Release();
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.State.TargetDistance, Is.EqualTo(4.2f).Within(0.001f));
            Assert.That(_context.Collision.CurrentDistance, Is.EqualTo(4.2f).Within(0.01f));
            Assert.That(_context.CameraPivot.position.y, Is.EqualTo(1.3f).Within(0.01f));
        }

        [TestCase(0f, true, 25f)]
        [TestCase(-12f, true, 25f)]
        [TestCase(10f, false, 10f)]
        public void 머리_위_대상은_프레이밍과_피치_복귀가_서로_밀어내지_않는다(
            float initialPitch, bool canRecoverPitch, float expectedPitch)
        {
            PrepareElevatedTarget();
            _context.State.CurrentPitch = initialPitch;
            _settings.enableLockOnPitchRecovery = canRecoverPitch;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 5f, 0f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < 360; i++)
            {
                ApplyStep(1f / 60f);
                Assert.That(_context.State.CurrentPitch, Is.GreaterThanOrEqualTo(initialPitch - 0.001f));
                Assert.That(_context.State.CurrentYaw, Is.EqualTo(0f).Within(0.001f));
            }
            Assert.That(_context.State.CurrentPitch, Is.EqualTo(expectedPitch).Within(0.01f));
            AssertInsideFraming(_context.Target.position);
            AssertInsideFraming(_context.LockOn.FocusPosition);
            AssertPlayerScale();
        }

        [Test]
        public void 이미_화면에_담긴_높은_대상은_피벗과_거리를_바꾸지_않는다()
        {
            PrepareElevatedTarget();
            _context.State.TargetDistance = 5.7f;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 2.4f, 12f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.CameraPivot.position.y, Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(_context.Collision.CurrentDistance, Is.EqualTo(5.7f).Within(0.001f));
            AssertInsideFraming(_context.LockOn.FocusPosition);
        }

        [TestCase(52f, 9f)]
        [TestCase(52f, 2f)]
        [TestCase(58f, 9f)]
        [TestCase(58f, 2f)]
        public void 탐색_FOV로_공중_시험_표적을_락온해도_포커스와_플레이어를_유지한다(float fov, float distance)
        {
            PrepareElevatedTarget();
            _context.MainCamera.fieldOfView = fov;
            _context.DistanceController = new CameraDistanceController(_settings, _context.Target, 0, fov);
            _context.Target.position = Vector3.up * 0.1f;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 4.2f, distance);
            CapsuleCollider capsule = target.GetComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.72f;
            capsule.center = Vector3.up * 0.95f;
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Vector3 focus = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
            Vector3 feet = _context.MainCamera.WorldToViewportPoint(_context.Target.position);
            Assert.That(focus.z, Is.GreaterThan(0f));
            Assert.That(focus.y, Is.InRange(0.04f, 0.96f));
            Assert.That(feet.y, Is.InRange(0.02f, 0.98f));
            AssertPlayerScale();
        }

        [Test]
        public void 대형_적의_상단이_높아져도_같은_락온_지점에서_더_후퇴하지_않는다()
        {
            PrepareElevatedTarget();
            _context.LockOn.CurrentTarget.GetComponent<CapsuleCollider>().height = 6f;
            Physics.SyncTransforms();
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Vector3 before = _context.MainCamera.transform.position;
            _context.LockOn.CurrentTarget.GetComponent<CapsuleCollider>().height = 20f;
            Physics.SyncTransforms();
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Assert.That(Vector3.Distance(before, _context.MainCamera.transform.position), Is.LessThan(0.001f));
            AssertPlayerScale();
        }

        [TestCase(3f)]
        [TestCase(4.2f)]
        [TestCase(8.5f)]
        public void 극단적인_고저차도_사용자_줌_대비_플레이어_크기_제한을_지킨다(float distance)
        {
            PrepareElevatedTarget();
            _context.State.TargetDistance = distance;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 9f, 0f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < 360; i++)
            {
                ApplyStep(1f / 60f);
                AssertPlayerScale();
                Assert.That(_context.CameraPivot.position.y,
                    Is.LessThanOrEqualTo(1.3f + _settings.lockOnHeightFramingMaxLift + 0.001f));
                Assert.That(_context.State.CurrentPitch, Is.EqualTo(25f).Within(0.001f));
            }
        }

        private void AssertPlayerScale()
        {
            Vector3 playerFocus = _context.Target.position + _context.State.CameraOffset;
            float actualDepth = _context.MainCamera.WorldToViewportPoint(playerFocus).z;
            float relativeScale = _context.State.TargetDistance / actualDepth;
            Assert.That(relativeScale, Is.GreaterThanOrEqualTo(_settings.lockOnFramingMinPlayerScale - 0.001f));
        }

        [Test]
        public void 공중에서_지상_대상으로_바뀌면_프레이밍_확장이_누적되지_않는다()
        {
            PrepareElevatedTarget();
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 0f, 8f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < 360; i++)
                ApplyStep(1f / 60f);
            Assert.That(_context.State.TargetDistance, Is.EqualTo(4.2f).Within(0.001f));
            Assert.That(_context.Collision.CurrentDistance, Is.EqualTo(4.2f).Within(0.01f));
            Assert.That(_context.CameraPivot.position.y, Is.EqualTo(1.3f).Within(0.01f));
            Vector3 viewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
            Assert.That(viewport.y, Is.InRange(0.35f, 0.65f));
        }

        [Test]
        public void 공중_프레이밍_중에도_천장과_거리_상한을_지킨다()
        {
            PrepareElevatedTarget();
            _settings.lockOnRequireLineOfSight = false;
            _settings.lockOnFitMaxDistance = 7f;
            GameObject ceiling = CreateObject("프레이밍 천장", new Vector3(0f, 2.5f, 0f));
            ceiling.layer = 29;
            ceiling.AddComponent<BoxCollider>().size = new Vector3(30f, 0.5f, 30f);
            Physics.SyncTransforms();
            for (int i = 0; i < 360; i++)
            {
                ApplyStep(1f / 60f);
                Assert.That(_context.State.CurrentPitch, Is.EqualTo(25f).Within(0.001f));
                Assert.That(_context.CameraPivot.position.y, Is.LessThan(2.25f));
                Assert.That(_context.MainCamera.transform.position.y, Is.LessThan(2.25f));
                Assert.That(_context.Collision.CurrentDistance, Is.LessThanOrEqualTo(7.001f));
            }
        }

        [Test]
        public void 공중_프레이밍은_시간이_멈추면_보간을_진행하지_않는다()
        {
            PrepareElevatedTarget();
            for (int i = 0; i < 30; i++)
                ApplyStep(1f / 60f);
            CameraPose before = _behavior.EvaluatePose(_context, 0f, default);
            for (int i = 0; i < 10; i++)
            {
                CameraPose after = _behavior.EvaluatePose(_context, 0f, default);
                Assert.That(after.PivotPosition, Is.EqualTo(before.PivotPosition));
                Assert.That(after.Distance, Is.EqualTo(before.Distance));
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 락온한_적_옆을_지나가도_불필요한_피치_왕복이_없다(int fps)
        {
            _settings.enableLockOnPitchRecovery = true;
            _context.State.CurrentPitch = _settings.lockOnPreferredPitch;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(1.5f, -0.3f, 2f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            for (int i = 0; i < fps * 10; i++)
            {
                _context.Target.position = Vector3.forward
                    * Mathf.Lerp(0f, 6f, Mathf.Clamp01((i - fps) / (fps * 3f)));
                Physics.SyncTransforms();
                ApplyStep(1f / fps);
                Assert.That(_context.State.CurrentPitch,
                    Is.EqualTo(_settings.lockOnPreferredPitch).Within(0.001f));
                Assert.That(_context.LockOn.IsActive, Is.True);
            }
            Assert.That(_context.State.CurrentYaw, Is.GreaterThan(90f));
            Vector3 viewport = _context.MainCamera.WorldToViewportPoint(_context.LockOn.FocusPosition);
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.x, Is.InRange(0.35f, 0.65f));
            Assert.That(viewport.y, Is.InRange(0.35f, 0.65f));
        }

        [TestCase(30, 90f, false)]
        [TestCase(60, 90f, false)]
        [TestCase(120, 90f, false)]
        [TestCase(60, -90f, false)]
        [TestCase(60, 90f, true)]
        [TestCase(60, -90f, true)]
        [TestCase(60, 90f, false, 65f)]
        public void 좁은_통로에서는_벽을_벗어난_궤도로_두_대상을_함께_담는다(
            int fps, float initialYaw, bool hasEndWall, float initialPitch = 25f)
        {
            PrepareElevatedTarget();
            _context.State.CurrentYaw = initialYaw;
            _context.State.CurrentPitch = initialPitch;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 0f, 2f);
            target.GetComponent<CapsuleCollider>().center = Vector3.up * 1.1f;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject wall = CreateObject("통로 벽", new Vector3(side * 1.8f, 2f, 0f));
                wall.layer = 29;
                wall.AddComponent<BoxCollider>().size = new Vector3(0.4f, 8f, 30f);
            }
            if (hasEndWall)
            {
                GameObject wall = CreateObject("통로 끝 벽", new Vector3(0f, 2f, -1f));
                wall.layer = 29;
                wall.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 0.4f);
            }
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            float deltaTime = 1f / fps;
            for (int i = 0; i < fps * 4; i++)
            {
                float previousYaw = _context.State.CurrentYaw;
                ApplyStep(deltaTime);
                Assert.That(Physics.CheckSphere(_context.MainCamera.transform.position,
                    _settings.cameraRadius, 1 << 29, QueryTriggerInteraction.Ignore), Is.False);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousYaw, _context.State.CurrentYaw)),
                    Is.LessThanOrEqualTo(_settings.lockOnMaxAngularSpeed.x * deltaTime + 0.001f));
                Assert.That(_context.State.CurrentPitch,
                    Is.InRange(0f, Mathf.Max(initialPitch, _settings.lockOnObstructionMaxPitch)));
            }
            Assert.That(_context.LockOn.CurrentTarget, Is.EqualTo(target));
            Assert.That(_context.Collision.CurrentDistance, Is.GreaterThanOrEqualTo(3f));
            Assert.That(_context.State.CurrentPitch, Is.LessThanOrEqualTo(_settings.lockOnObstructionMaxPitch));
            AssertInsideFraming(_context.Target.position);
            AssertInsideFraming(_context.Target.position + _settings.defaultOffset);
            AssertInsideFraming(_context.LockOn.FocusPosition);
            Assert.That(_context.State.TargetDistance, Is.EqualTo(4.2f));
            float settledYaw = _context.State.CurrentYaw;
            for (int i = 0; i < fps * 2; i++)
                ApplyStep(deltaTime);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(settledYaw, _context.State.CurrentYaw)), Is.LessThan(0.1f));
            foreach (GameObject item in _objects)
            {
                if (item.layer == 29)
                    item.SetActive(false);
            }
            Physics.SyncTransforms();
            for (int i = 0; i < fps * 2; i++)
                ApplyStep(deltaTime);
            Assert.That(_context.Collision.CurrentDistance, Is.EqualTo(4.2f).Within(0.01f));
            Assert.That(_context.State.CurrentPitch, Is.EqualTo(_settings.lockOnPreferredPitch).Within(0.5f));
        }

        [Test]
        public void 충돌_후보_조회는_복귀_상태를_진행시키지_않는다()
        {
            float initial = _context.Collision.CurrentDistance;
            GameObject wall = CreateObject("조회 검증 벽", new Vector3(0f, 0f, -2f));
            wall.layer = 29;
            wall.AddComponent<BoxCollider>().size = new Vector3(20f, 20f, 0.5f);
            Physics.SyncTransforms();
            float available = _context.Collision.GetAvailableDistance(Vector3.zero, Vector3.back, initial);
            Assert.That(available, Is.LessThan(2f));
            Assert.That(_context.Collision.CurrentDistance, Is.EqualTo(initial));
            Assert.That(_context.Collision.Evaluate(Vector3.zero, Vector3.back, initial), Is.LessThanOrEqualTo(available));
            wall.transform.position += Vector3.forward * (_settings.collisionDistanceDeadZone * 0.5f);
            Physics.SyncTransforms();
            available = _context.Collision.GetAvailableDistance(Vector3.zero, Vector3.back, initial);
            Assert.That(_context.Collision.Evaluate(Vector3.zero, Vector3.back, initial), Is.LessThanOrEqualTo(available));
        }

        private void PrepareElevatedTarget()
        {
            // 고정 간격 시뮬레이션과 실제 Time.deltaTime을 쓰는 충돌 복귀 보간을 분리한다.
            _settings.collisionReturnSpeed = 0f;
            _settings.collisionMaxDistanceChangeSpeed = 0f;
            _settings.enableLockOnPitchRecovery = true;
            _settings.defaultOffset = Vector3.up * 1.3f;
            _settings.combatOffset = _settings.defaultOffset;
            _context.State.CameraOffset = _settings.defaultOffset;
            _context.State.SmoothPosition = _settings.defaultOffset;
            _context.State.CurrentPitch = 25f;
            _context.State.TargetDistance = 4.2f;
            Transform target = _context.LockOn.CurrentTarget;
            target.position = new Vector3(0f, 5f, 3f);
            Physics.SyncTransforms();
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
        }

        private void AssertInsideFraming(Vector3 point)
        {
            Vector3 viewport = _context.MainCamera.WorldToViewportPoint(point);
            float margin = (1f - _settings.lockOnFitSafeFraction) * 0.5f - 0.01f;
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.x, Is.InRange(margin, 1f - margin));
            Assert.That(viewport.y, Is.InRange(margin, 1f - margin));
        }

        private void ApplyStep(float deltaTime)
        {
            CameraPose pose = _behavior.EvaluatePose(_context, deltaTime, default);
            _resolver.Apply(pose, _context.MainCamera, _context.CameraPivot);
        }

        private void Apply(CameraEffectState effects)
        {
            CameraPose pose = _behavior.EvaluatePose(_context, Time.deltaTime, effects);
            _resolver.Apply(pose, _context.MainCamera, _context.CameraPivot);
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.position = position;
            _objects.Add(item);
            return item;
        }
    }
}
