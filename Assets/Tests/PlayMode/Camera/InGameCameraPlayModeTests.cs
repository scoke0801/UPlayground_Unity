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
            Transform target = _context.LockOn.CurrentTarget;
            _settings.defaultOffset = Vector3.up * 1.4f;
            _settings.combatOffset = _settings.defaultOffset;
            _context.State.CameraOffset = _settings.defaultOffset;
            _context.State.SmoothPosition = _settings.defaultOffset;
            target.position = new Vector3(0f, 0f, -1.5f);
            Assert.That(_context.LockOn.TryRestoreTarget(target), Is.True);
            Physics.SyncTransforms();
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
                Assert.That(_context.LockOn.CanTrack, Is.False);
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
