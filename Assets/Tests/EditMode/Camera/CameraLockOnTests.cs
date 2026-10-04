using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem.Tests
{
    /// <summary>락온 선택·가림 유예·소실 사유별 정책을 검증한다.</summary>
    public sealed class CameraLockOnTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private CameraSettings _settings;
        private Camera _camera;
        private Transform _player;
        private TestAdapter _adapter;
        private CameraLockOn _lockOn;

        private sealed class TestAdapter : CameraRuntimeAdapterBase
        {
            public readonly Dictionary<Transform, bool> Targets = new Dictionary<Transform, bool>();

            public override bool TryResolveTarget(Component candidate, out CameraTargetInfo target)
            {
                if (candidate != null && Targets.TryGetValue(candidate.transform, out bool alive))
                {
                    target = new CameraTargetInfo(candidate.transform, alive, true, true, default);
                    return true;
                }
                target = default;
                return false;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<CameraSettings>();
            _settings.targetSwitchCooldown = 0f;
            _camera = CreateObject("카메라", new Vector3(0f, 0f, -5.7f)).AddComponent<Camera>();
            _camera.fieldOfView = 58f;
            _camera.aspect = 16f / 9f;
            _player = CreateObject("플레이어", Vector3.zero).transform;
            _adapter = new TestAdapter();
            CameraRuntimeServices.Configure(_adapter);
            _lockOn = new CameraLockOn(_settings, _player, _camera, 1 << 30, 1 << 29);
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
        public void 최초_선택은_뒤쪽_적보다_화면_중앙_적을_선택한다()
        {
            AddTarget(new Vector3(0f, 0f, -8f));
            Transform center = AddTarget(new Vector3(0f, 0f, 8f));
            AddTarget(new Vector3(5f, 0f, 3f));
            Physics.SyncTransforms();
            Assert.That(_lockOn.TryActivate(), Is.True);
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(center));
        }

        [Test]
        public void 화면_뒤에만_대상이_있으면_획득하지_않는다()
        {
            AddTarget(new Vector3(0f, 0f, -8f));
            Physics.SyncTransforms();
            Assert.That(_lockOn.TryActivate(), Is.False);
        }

        [Test]
        public void 벽_뒤의_적은_새로_획득하지_않는다()
        {
            AddTarget(new Vector3(0f, 0f, 8f));
            AddWall();
            Physics.SyncTransforms();
            Assert.That(_lockOn.TryActivate(), Is.False);
        }

        [Test]
        public void 가림_유예_동안_움직이는_대상을_추적하고_만료되면_해제한다()
        {
            _settings.lockOnOcclusionGraceTime = 1.5f;
            Transform target = AddTarget(new Vector3(0f, 0f, 8f));
            Assert.That(_lockOn.TryRestoreTarget(target), Is.True);
            AddWall();
            target.position += Vector3.right;
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(0.1f, false);
            Assert.That(_lockOn.IsActive, Is.True);
            Assert.That(_lockOn.CanTrack, Is.True);
            Assert.That(_lockOn.FocusPosition.x, Is.GreaterThan(0f));
            _lockOn.UpdateTarget(1.3f, false);
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(target));
            _lockOn.UpdateTarget(0.11f, false);
            Assert.That(_lockOn.IsActive, Is.False);
            Assert.That(_lockOn.CanTrack, Is.False);
        }

        [Test]
        public void 다시_보이면_이전_가림_시간을_누적하지_않는다()
        {
            _settings.lockOnOcclusionGraceTime = 1.5f;
            Transform target = AddTarget(new Vector3(0f, 0f, 8f));
            Assert.That(_lockOn.TryRestoreTarget(target), Is.True);
            GameObject wall = AddWall();
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(1.4f, false);
            wall.SetActive(false);
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(0.1f, false);
            Assert.That(_lockOn.CanTrack, Is.True);
            wall.SetActive(true);
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(1.4f, false);
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(target));
            Assert.That(_lockOn.CanTrack, Is.True);
            _lockOn.UpdateTarget(0.11f, false);
            Assert.That(_lockOn.IsActive, Is.False);
        }

        [Test]
        public void 가림_유예_중_추적_중단과_히트스톱은_해제_시간을_진행하지_않는다()
        {
            _settings.lockOnOcclusionGraceTime = 1.5f;
            Transform target = AddTarget(new Vector3(0f, 0f, 8f));
            Assert.That(_lockOn.TryRestoreTarget(target), Is.True);
            AddWall();
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(1f, false);
            Vector3 focus = _lockOn.FocusPosition;
            target.position += Vector3.right;
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(3f, true);
            Assert.That(_lockOn.CanTrack, Is.False);
            Assert.That(_lockOn.FocusPosition, Is.EqualTo(focus));
            _lockOn.UpdateTarget(0f, false);
            Assert.That(_lockOn.FocusPosition, Is.EqualTo(focus));
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(target));
            _lockOn.UpdateTarget(0.51f, false);
            Assert.That(_lockOn.IsActive, Is.False);
        }

        [Test]
        public void 가림_유예를_0으로_설정하면_즉시_해제한다()
        {
            _settings.lockOnOcclusionGraceTime = 0f;
            Assert.That(_lockOn.TryRestoreTarget(AddTarget(new Vector3(0f, 0f, 8f))), Is.True);
            AddWall();
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(0.01f, false);
            Assert.That(_lockOn.IsActive, Is.False);
        }

        [Test]
        public void 거리_초과는_다른_적으로_전환하지_않고_해제한다()
        {
            Transform target = AddTarget(new Vector3(0f, 0f, 8f));
            Assert.That(_lockOn.TryRestoreTarget(target), Is.True);
            AddTarget(new Vector3(2f, 0f, 5f));
            target.position = new Vector3(0f, 0f, 25f);
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(0.36f, false);
            Assert.That(_lockOn.CurrentTarget, Is.Null);
            Assert.That(_lockOn.IsActive, Is.False);
        }

        [Test]
        public void 대상_사망은_화면_안의_다음_적으로_전환한다()
        {
            Transform target = AddTarget(new Vector3(0f, 0f, 8f));
            Transform next = AddTarget(new Vector3(2f, 0f, 5f));
            Assert.That(_lockOn.TryRestoreTarget(target), Is.True);
            _adapter.Targets[target] = false;
            Physics.SyncTransforms();
            _lockOn.UpdateTarget(0.01f, false);
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(next));
        }

        [Test]
        public void 수동_전환은_화면_방향을_따르고_반대편으로_순환하지_않는다()
        {
            Transform center = AddTarget(new Vector3(0f, 0f, 8f));
            Transform upper = AddTarget(new Vector3(0f, 3f, 8f));
            Transform right = AddTarget(new Vector3(4f, 0f, 8f));
            Assert.That(_lockOn.TryRestoreTarget(center), Is.True);
            Physics.SyncTransforms();
            _lockOn.SwitchTarget(Vector2.up);
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(upper));
            _lockOn.TryRestoreTarget(right);
            _lockOn.SwitchTarget(Vector2.right);
            Assert.That(_lockOn.CurrentTarget, Is.EqualTo(right));
        }

        [Test]
        public void 파괴된_대상은_활성_락온으로_남지_않는다()
        {
            Transform target = AddTarget(new Vector3(0f, 0f, 8f));
            Assert.That(_lockOn.TryRestoreTarget(target), Is.True);
            _objects.Remove(target.gameObject);
            UnityEngine.Object.DestroyImmediate(target.gameObject);
            _lockOn.UpdateTarget(0.01f, false);
            Assert.That(_lockOn.IsActive, Is.False);
        }

        private Transform AddTarget(Vector3 position)
        {
            GameObject target = CreateObject("대상", position);
            target.layer = 30;
            target.AddComponent<CapsuleCollider>();
            _adapter.Targets.Add(target.transform, true);
            return target.transform;
        }

        private GameObject AddWall()
        {
            GameObject wall = CreateObject("벽", new Vector3(0f, 0f, 2f));
            wall.layer = 29;
            wall.AddComponent<BoxCollider>().size = new Vector3(6f, 6f, 0.5f);
            return wall;
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
