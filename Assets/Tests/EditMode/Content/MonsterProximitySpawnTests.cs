using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UPlayGround.Data.World;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UPlayGround.Content.Tests
{
    /// <summary>인접 스폰의 구역 Bake와 리스폰 생성 소유권 경계를 검증한다.</summary>
    public sealed class MonsterProximitySpawnTests
    {
        private WorldPlacementDataSO _data;
        private Scene _scene;
        private readonly List<GameObject> _objects = new();
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<WorldPlacementDataSO>();
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in _objects) UnityEngine.Object.DestroyImmediate(instance);
            _objects.Clear();
            EditorSceneManager.ClosePreviewScene(_scene);
            UnityEngine.Object.DestroyImmediate(_data);
        }

        [Test]
        public void 기존_배치는_명시적으로_켤_때까지_인접_스폰을_사용하지_않는다()
        {
            Assert.That(_data.spawnMonstersByProximity, Is.False);
            _data.SetRecords(new[] { new WorldPlacementRecord() });
            Assert.That(_data.MonsterSpawnAreas, Is.Empty);
        }

        [Test]
        public void 같은_그룹은_셀_경계를_넘어도_하나의_구역으로_굽는다()
        {
            var a = Record(-100, "group-a");
            var b = Record(100, "group-a");
            _data.SetRecords(new[] { a, new WorldPlacementRecord(), b });
            Assert.That(_data.MonsterSpawnAreas.Count, Is.EqualTo(1));
            Assert.That(_data.MonsterSpawnAreas[0].recordIndices, Is.EqualTo(new[] { 0, 2 }));
            Assert.That(_data.MonsterSpawnAreas[0].bounds.Contains(a.position), Is.True);
            Assert.That(_data.MonsterSpawnAreas[0].bounds.Contains(b.position), Is.True);
        }

        [Test]
        public void 음수_좌표와_원점_셀은_분리하고_삭제_후_인덱스를_다시_굽는다()
        {
            _data.SetRecords(new[] { Record(-0.1f), Record(0f), Record(1f) });
            Assert.That(_data.MonsterSpawnAreas.Count, Is.EqualTo(2));
            _data.SetRecords(new[] { Record(1f) });
            Assert.That(_data.MonsterSpawnAreas.Count, Is.EqualTo(1));
            Assert.That(_data.MonsterSpawnAreas[0].recordIndices, Is.EqualTo(new[] { 0 }));
            _data.SetRecords(null);
            Assert.That(_data.MonsterSpawnAreas, Is.Empty);
        }

        [Test]
        public void 비행_높이는_수평_접근_판정에_영향을_주지_않는다()
        {
            var area = new WorldPlacementSpawnArea { bounds = new Bounds(new Vector3(10, 400, 10), Vector3.zero) };
            Assert.That(area.GetDistanceSquared(new Vector3(13, -400, 14)), Is.EqualTo(25f));
        }

        [Test]
        public void 공간_조회는_먼_셀을_제외하고_순간이동_뒤_후보를_교체한다()
        {
            _data.monsterSpawnDistance = 10;
            _data.SetRecords(new[] { Record(-1000), Record(1000) });
            Type loaderType = RuntimeType("UPlayGround.Manager.World.RuntimePlacementLoader");
            Component loader = AddComponent(loaderType);
            loaderType.GetField("_placementData", PrivateInstance).SetValue(loader, _data);
            loaderType.GetField("_spawnCellSize", PrivateInstance).SetValue(loader, 64f);
            MethodInfo index = loaderType.GetMethod("AddSpawnAreaToCells", PrivateInstance);
            for (int i = 0; i < _data.MonsterSpawnAreas.Count; i++)
                index.Invoke(loader, new object[] { i, _data.MonsterSpawnAreas[i].bounds });
            MethodInfo collect = loaderType.GetMethod("CollectNearbySpawnAreas", PrivateInstance);
            var candidates = (List<int>)loaderType.GetField("_candidateAreas", PrivateInstance).GetValue(loader);
            collect.Invoke(loader, new object[] { new Vector3(-1000, 100, 0) });
            Assert.That(candidates, Is.EqualTo(new[] { 0 }));
            collect.Invoke(loader, new object[] { new Vector3(1000, 100, 0) });
            Assert.That(candidates, Is.EqualTo(new[] { 1 }));
            collect.Invoke(loader, new object[] { Vector3.zero });
            Assert.That(candidates, Is.Empty);
        }

        [Test]
        public void 영구처치와_대기시간을_확인하고_도래한_리스폰만_허용한다()
        {
            Type loaderType = RuntimeType("UPlayGround.Manager.World.RuntimePlacementLoader");
            Component loader = AddComponent(loaderType);
            Component world = AddComponent(RuntimeType("UPlayGround.Manager.WorldStateManager"));
            Component time = AddComponent(RuntimeType("UPlayGround.Manager.GameTimeManager"));
            loaderType.GetField("_proximityWorldState", PrivateInstance).SetValue(loader, world);
            loaderType.GetField("_proximityTime", PrivateInstance).SetValue(loader, time);
            loaderType.GetField("_proximityMapId", PrivateInstance).SetValue(loader, "map");
            Type entryType = loaderType.GetNestedType("SpawnEntry", BindingFlags.NonPublic);
            object entry = Activator.CreateInstance(entryType, true);
            var record = Record(0);
            entryType.GetField("Record").SetValue(entry, record);
            MethodInfo canSpawn = loaderType.GetMethod("CanSpawnEntry", PrivateInstance);
            Assert.That(canSpawn.Invoke(loader, new[] { entry }), Is.True);
            var state = new UPlayGround.Data.Save.MonsterRespawnState
            {
                mapId = "map", guid = record.placementGuid, waitingRespawn = true, nextRespawnGameMinute = float.MaxValue
            };
            world.GetType().GetMethod("SetRespawnState").Invoke(world, new object[] { state });
            Assert.That(canSpawn.Invoke(loader, new[] { entry }), Is.False);
            state.nextRespawnGameMinute = 0;
            Assert.That(canSpawn.Invoke(loader, new[] { entry }), Is.True);
            world.GetType().GetMethod("RecordPermanentKill").Invoke(world, new object[] { "map", record.placementGuid });
            Assert.That(canSpawn.Invoke(loader, new[] { entry }), Is.False);
        }

        [Test]
        public void 로더가_소유한_배치는_시간_리스폰이_직접_생성하지_않는다()
        {
            Type managerType = RuntimeType("UPlayGround.Manager.MonsterRespawnManager");
            Type loaderType = RuntimeType("UPlayGround.Manager.World.RuntimePlacementLoader");
            Component manager = AddComponent(managerType);
            Component first = AddComponent(loaderType);
            Component second = AddComponent(loaderType);
            MethodInfo register = managerType.GetMethod("RegisterProximityPlacement");
            register.Invoke(manager, new object[] { "map", "guid", first });
            Assert.That(managerType.GetMethod("HasProximityOwner", PrivateInstance)
                .Invoke(manager, new object[] { "map", "guid" }), Is.True);
            Assert.That(managerType.GetMethod("HasProximityOwner", PrivateInstance)
                .Invoke(manager, new object[] { "other-map", "guid" }), Is.False);
            var error = Assert.Throws<TargetInvocationException>(() =>
                register.Invoke(manager, new object[] { "map", "guid", second }));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            var state = new UPlayGround.Data.Save.MonsterRespawnState { mapId = "map", guid = "guid" };
            Assert.That(managerType.GetMethod("SpawnFromState", PrivateInstance).Invoke(manager, new object[] { state }), Is.Null);
            managerType.GetMethod("UnregisterProximityPlacements").Invoke(manager, new object[] { first });
            Assert.That(managerType.GetMethod("HasProximityOwner", PrivateInstance)
                .Invoke(manager, new object[] { "map", "guid" }), Is.False);
        }

        private Component AddComponent(Type type)
        {
            var instance = new GameObject("MonsterProximitySpawnTest");
            instance.SetActive(false);
            SceneManager.MoveGameObjectToScene(instance, _scene);
            _objects.Add(instance);
            return instance.AddComponent(type);
        }

        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);

        private static WorldPlacementRecord Record(float x, string groupGuid = null) => new()
        {
            position = new Vector3(x, 0, 0), groupGuid = groupGuid, spawnWhenNearby = true,
            actorId = "test-monster", placementGuid = Guid.NewGuid().ToString("N")
        };
    }
}
