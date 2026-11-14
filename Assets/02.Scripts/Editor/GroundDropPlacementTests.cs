using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Data.Save;
using UPlayGround.Manager;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>드롭의 간격과 좁은 지형·낙차·벽 회피를 실제 물리 바닥에서 검사한다.</summary>
    public sealed class GroundDropPlacementTests
    {
        private const int TestLayer = 30;
        private const string TestMap = "ground-drop-placement-tests";
        private readonly Vector3 _origin = new(20000f, 10000f, 20000f);
        private GameObject _root;
        private GroundDropManager _manager;
        private GroundDropSettingsSO _settings;
        private Dictionary<string, GroundDropRecord> _records;
        private Random.State _randomState;
        private readonly List<GameObject> _surfaces = new();

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _root = new GameObject("드롭 배치 검사");
            _root.SetActive(false);
            _manager = _root.AddComponent<GroundDropManager>();
            _settings = ScriptableObject.CreateInstance<GroundDropSettingsSO>();
            _settings.scatterRadius = 1.2f;
            _settings.minimumDropSpacing = 0.55f;
            _settings.tokenHoverHeight = 0.35f;
            _settings.groundMask = 1 << TestLayer;
            _settings.visibilityMask = 1 << TestLayer;
            typeof(GroundDropManager).GetProperty("Settings").SetValue(_manager, _settings);
            _records = (Dictionary<string, GroundDropRecord>)typeof(GroundDropManager)
                .GetField("_records", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
        }

        [TearDown]
        public void TearDown()
        {
            Random.state = _randomState;
            foreach (var surface in _surfaces) Object.DestroyImmediate(surface);
            _surfaces.Clear();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_settings);
            Physics.SyncTransforms();
        }

        [Test]
        public void 평지에서_기존_미획득품과_새_보상_사이의_간격을_확보한다()
        {
            AddSurface(_origin - Vector3.up * 0.1f, new Vector3(10f, 0.2f, 10f));
            for (int seed = 0; seed < 16; seed++)
            {
                _records.Clear();
                Random.InitState(seed);
                AddRecord(_origin + Vector3.up * _settings.surfaceOffset);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 position = FindPosition(4);
                    foreach (var record in _records.Values)
                    {
                        Vector3 other = WorldCoordinates.ToWorld(TestMap, record.position.ToVector3());
                        Assert.That(Vector2.Distance(new Vector2(position.x, position.z), new Vector2(other.x, other.z)),
                            Is.GreaterThanOrEqualTo(_settings.minimumDropSpacing - 0.002f), "시드 " + seed);
                    }
                    Assert.That(Vector2.Distance(new Vector2(position.x, position.z), new Vector2(_origin.x, _origin.z)),
                        Is.LessThanOrEqualTo(_settings.scatterRadius + 0.002f));
                    AddRecord(position);
                }
            }
        }

        [Test]
        public void 좁은_발판에서_간격보다_바닥을_우선하고_아래층으로_떨어뜨리지_않는다()
        {
            AddSurface(_origin - Vector3.up * 0.1f, new Vector3(0.2f, 0.2f, 0.2f));
            AddSurface(_origin - Vector3.up * 3.1f, new Vector3(10f, 0.2f, 10f));
            AddRecord(_origin + Vector3.up * _settings.surfaceOffset);
            Random.InitState(731114);
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = FindPosition(6);
                Assert.That(position.y, Is.EqualTo(_origin.y + _settings.surfaceOffset).Within(0.003f));
                Assert.That(Mathf.Abs(position.x - _origin.x), Is.LessThanOrEqualTo(0.102f));
                Assert.That(Mathf.Abs(position.z - _origin.z), Is.LessThanOrEqualTo(0.102f));
                AddRecord(position);
            }
        }

        [Test]
        public void 원천과_착지점_사이에_벽이_있으면_후보를_거부한다()
        {
            AddSurface(_origin - Vector3.up * 0.1f, new Vector3(10f, 0.2f, 10f));
            AddSurface(_origin + new Vector3(0.5f, 1f, 0f), new Vector3(0.1f, 2f, 4f));
            Assert.That(TryScatter(_origin + Vector3.up * _settings.surfaceOffset, _origin + Vector3.right), Is.False);
            Assert.That(TryScatter(_origin + Vector3.up * _settings.surfaceOffset, _origin + Vector3.left), Is.True);
        }

        [Test]
        public void 급경사_착지점은_거부한다()
        {
            var slope = AddSurface(_origin - Vector3.up * 0.1f, new Vector3(5f, 0.2f, 5f));
            slope.transform.rotation = Quaternion.Euler(0f, 0f, 60f);
            Physics.SyncTransforms();
            Assert.That(TryScatter(_origin + Vector3.up * _settings.surfaceOffset, _origin), Is.False);
        }

        [Test]
        public void 바닥이_없어도_원천좌표를_보존한다()
        {
            Assert.That(FindPosition(4), Is.EqualTo(_origin));
        }

        [Test]
        public void 획득한_보상과_다른_지역의_보상은_간격_계산에서_제외한다()
        {
            AddRecord(_origin, true);
            AddRecord(_origin, false, "다른 지역");
            float distance = (float)typeof(GroundDropManager)
                .GetMethod("FindNearestDropSqrDistance", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_manager, new object[] { _origin, TestMap });
            Assert.That(float.IsPositiveInfinity(distance), Is.True);
        }

        private Vector3 FindPosition(int count) => (Vector3)typeof(GroundDropManager)
            .GetMethod("FindDropPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_manager, new object[] { _origin, TestMap, count });

        private bool TryScatter(Vector3 center, Vector3 candidate)
        {
            var arguments = new object[] { center, candidate, Vector3.zero };
            return (bool)typeof(GroundDropManager)
                .GetMethod("TryFindScatterGround", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_manager, arguments);
        }

        private void AddRecord(Vector3 position, bool collected = false, string map = TestMap)
        {
            string id = _records.Count.ToString();
            _records.Add(id, new GroundDropRecord
            {
                dropId = id, mapId = map, collected = collected,
                position = new SerializableVector3(WorldCoordinates.ToSource(map, position))
            });
        }

        private GameObject AddSurface(Vector3 position, Vector3 size)
        {
            var surface = new GameObject("검사 바닥", typeof(BoxCollider));
            surface.layer = TestLayer;
            surface.transform.position = position;
            surface.GetComponent<BoxCollider>().size = size;
            _surfaces.Add(surface);
            Physics.SyncTransforms();
            return surface;
        }

    }
}
