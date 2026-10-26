using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UPlayGround.Data.World;

namespace UPlayGround.Content.Tests
{
    /// <summary>다중 씬에서 준비 신호·도착점·배치 그룹의 소유 범위와 실패 처리를 검사한다.</summary>
    public sealed class ScenePreparationTests
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Scene> _scenes = new();
        private readonly List<UnityEngine.Object> _assets = new();
        private readonly List<Component> _arrivals = new();
        private Scene _originalScene;
        private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        private static Type ArrivalType => RuntimeType("UPlayGround.SceneArrivalPoint");
        private static Type RegistryType => RuntimeType("UPlayGround.SceneArrivalRegistry");

        [SetUp]
        public void SetUp() => _originalScene = SceneManager.GetActiveScene();

        [TearDown]
        public void TearDown()
        {
            foreach (Component arrival in _arrivals)
                if (arrival != null) RegistryType.GetMethod("Unregister").Invoke(null, new object[] { arrival });
            _arrivals.Clear();
            foreach (Scene scene in _scenes)
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene);
            _scenes.Clear();
            foreach (UnityEngine.Object asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
            if (_originalScene.IsValid() && _originalScene.isLoaded) SceneManager.SetActiveScene(_originalScene);
        }

        [Test]
        public void 같은_ID가_다른_씬에_있으면_대상_씬의_도착점만_찾는다()
        {
            Scene a = CreateScene();
            Scene b = CreateScene();
            string id = Guid.NewGuid().ToString();
            Component first = AddArrival(a, id);
            Component second = AddArrival(b, id);
            Assert.That(FindArrival(id, a), Is.SameAs(first));
            Assert.That(FindArrival(id, b), Is.SameAs(second));
            object[] global = { id, null };
            Assert.That((bool)RegistryType.GetMethod("TryGet", new[] { typeof(string), ArrivalType.MakeByRefType() })
                .Invoke(null, global), Is.False, "범위 없는 조회는 모호한 ID를 선택하면 안 됩니다.");
        }

        [Test]
        public void 대상_씬_내_ID_중복은_실패하고_해제_뒤에는_복구한다()
        {
            Scene scene = CreateScene();
            string id = Guid.NewGuid().ToString();
            Component first = AddArrival(scene, id);
            Component second = AddArrival(scene, id);
            Assert.That(FindArrival(id, scene), Is.Null);
            RegistryType.GetMethod("Unregister").Invoke(null, new object[] { second });
            UnityEngine.Object.DestroyImmediate(second.gameObject);
            Assert.That(FindArrival(id, scene), Is.SameAs(first));
            Assert.That(FindArrival(id, default), Is.Null);
        }

        [Test]
        public void 언로드한_씬의_도착점은_다른_씬의_ID로_대체하지_않는다()
        {
            Scene a = CreateScene();
            Scene b = CreateScene();
            string id = Guid.NewGuid().ToString();
            AddArrival(a, id);
            AddArrival(b, id);
            EditorSceneManager.ClosePreviewScene(a);
            Assert.That(FindArrival(id, a), Is.Null);
            Assert.That(FindArrival(id, b), Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 이웃_씬의_준비_통지는_현재_맵을_변경하지_않는다(bool loading)
        {
            Scene source = CreateScene();
            Scene neighbor = CreateScene();
            // 활성 씬은 Test Runner 소유로 유지하고 이웃의 준비 통지만 검사한다.
            Component manager = AddComponent(source, RuntimeType("UPlayGround.Manager.SceneManager"), false);
            Type managerType = manager.GetType();
            managerType.GetField("_currentMapID", InstanceFields).SetValue(manager, "source-map");
            managerType.GetField("_isLoading", InstanceFields).SetValue(manager, loading);
            managerType.GetField("_activeLoadSceneName", InstanceFields).SetValue(manager, "expected-target");
            Component context = AddComponent(neighbor, RuntimeType("UPlayGround.SceneContext"));
            context.GetType().GetField("MapID").SetValue(context, "neighbor-map");
            Assert.That((bool)managerType.GetMethod("NotifySceneContextReady").Invoke(manager, new object[] { context }), Is.False);
            Assert.That(managerType.GetProperty("CurrentMapID").GetValue(manager), Is.EqualTo("source-map"));
            Assert.That(managerType.GetField("_readyContext", InstanceFields).GetValue(manager), Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void 만들_수_없는_배치는_준비_완료가_아니며_수정_후_재시도할_수_있다(bool spawnOnStart)
        {
            Component loader = AddComponent(CreateScene(), RuntimeType("UPlayGround.Manager.World.RuntimePlacementLoader"));
            loader.GetType().GetField("_spawnOnStart", InstanceFields).SetValue(loader, spawnOnStart);
            var data = ScriptableObject.CreateInstance<WorldPlacementDataSO>();
            _assets.Add(data);
            data.SetRecords(new[] { new WorldPlacementRecord { sourceKind = WorldPlacementSourceKind.DirectPrefab } });
            loader.GetType().GetField("_placementData", InstanceFields).SetValue(loader, data);
            LogAssert.Expect(LogType.Error, new Regex("배치 1개 생성 실패"));
            loader.GetType().GetMethod("SpawnAll").Invoke(loader, null);
            Assert.That(loader.GetType().GetProperty("HasSpawnFailed").GetValue(loader), Is.True);
            Assert.That(loader.GetType().GetProperty("IsSpawnComplete").GetValue(loader), Is.False);
            Assert.That(loader.GetType().GetProperty("IsSpawning").GetValue(loader), Is.False);
            data.SetRecords(Array.Empty<WorldPlacementRecord>());
            loader.GetType().GetMethod("SpawnAll").Invoke(loader, null);
            Assert.That(loader.GetType().GetProperty("HasSpawnFailed").GetValue(loader), Is.False);
            Assert.That(loader.GetType().GetProperty("IsSpawnComplete").GetValue(loader), Is.True);
        }

        [Test]
        public void 배치_그룹은_다른_씬의_동명_그룹에_연결되지_않는다()
        {
            Scene a = CreateScene();
            Scene b = CreateScene();
            Component loader = AddComponent(a, RuntimeType("UPlayGround.Manager.World.RuntimePlacementLoader"));
            Type groupType = Type.GetType("UPlayGround.Group.MonsterGroupController, UPlayGround.Actor", true);
            Component own = AddComponent(a, groupType, false);
            Component other = AddComponent(b, groupType, false);
            own.name = other.name = "shared-group";
            var record = new WorldPlacementRecord { groupName = "shared-group" };
            object result = loader.GetType().GetMethod("ResolveGroup", InstanceFields).Invoke(loader, new object[] { record });
            Assert.That(result, Is.SameAs(own));
        }

        private Scene CreateScene()
        {
            // Test Runner의 미저장 임시 씬을 저장하거나 교체하지 않고 격리된 씬을 만든다.
            Scene scene = EditorSceneManager.NewPreviewScene();
            _scenes.Add(scene);
            return scene;
        }

        [Test]
        public void 저장_소유권은_현재_씬이_아닌_개체의_원래_지역이다()
        {
            Scene a = CreateScene();
            Scene b = CreateScene();
            Component first = AddComponent(a, RuntimeType("UPlayGround.SceneContext"));
            Component second = AddComponent(b, RuntimeType("UPlayGround.SceneContext"));
            first.GetType().GetField("MapID").SetValue(first, "lake");
            second.GetType().GetField("MapID").SetValue(second, "village");
            Type ownership = RuntimeType("UPlayGround.Manager.World.RegionOwnership");
            var resolve = ownership.GetMethod("Resolve", new[] { typeof(Component) });
            Assert.That(resolve.Invoke(null, new object[] { first }), Is.EqualTo("lake"));
            Assert.That(resolve.Invoke(null, new object[] { second }), Is.EqualTo("village"));
            Assert.That(resolve.Invoke(null, new object[] { null }), Is.Null);
        }

        [Test]
        public void 지역_경계의_진입과_이탈은_서로_다른_거리를_사용한다()
        {
            Type world = RuntimeType("UPlayGround.Manager.World.ContinuousWorldController");
            var contains = world.GetMethod("Contains");
            var bounds = new Rect(930, 435, 54, 110);
            var edge = new Vector3(983, 200, 450);
            Assert.That(contains.Invoke(null, new object[] { bounds, edge, -2f }), Is.False);
            Assert.That(contains.Invoke(null, new object[] { bounds, edge, 2f }), Is.True);
            var distance = world.GetMethod("DistanceToBounds");
            Assert.That((float)distance.Invoke(null, new object[] { bounds, new Vector3(994, 500, 450) }), Is.EqualTo(10f));
        }

        private static Component AddComponent(Scene scene, Type type, bool active = true)
        {
            var go = new GameObject(type.Name);
            go.SetActive(active);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.AddComponent(type);
        }

        [Test]
        public void 대시가_한_프레임에_지역을_관통해도_경계_검사에서_놓치지_않는다()
        {
            var crosses = RuntimeType("UPlayGround.Manager.World.ContinuousWorldController").GetMethod("CrossesBounds");
            var bounds = new Rect(930, 435, 54, 70);
            Assert.That(crosses.Invoke(null, new object[] {bounds, new Vector3(1000, 200, 450), new Vector3(900, 200, 450)}), Is.True);
            Assert.That(crosses.Invoke(null, new object[] {bounds, new Vector3(1000, 200, 510), new Vector3(900, 200, 510)}), Is.False);
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        public void 도시_원본_좌표는_회전_배치_뒤에도_왕복_복원된다(float yaw)
        {
            Type type = RuntimeType("UPlayGround.Manager.World.ContinuousWorldRegion");
            object region = Activator.CreateInstance(type);
            type.GetField("SourceOffset").SetValue(region, new Vector3(1400, 40, 280));
            type.GetField("SourceYaw").SetValue(region, yaw);
            var source = new Vector3(180, 10, 55);
            var world = (Vector3)type.GetMethod("ToWorld").Invoke(region, new object[] {source});
            var restored = (Vector3)type.GetMethod("ToSource").Invoke(region, new object[] {world});
            Assert.That(Vector3.Distance(source, restored), Is.LessThan(0.001f));
            if (yaw == 180) Assert.That(Vector3.Distance(world, new Vector3(1220, 50, 225)), Is.LessThan(0.001f));
        }

        [Test]
        public void 세_지역의_논리_선택은_현재_지역_이탈_여유를_보존한다()
        {
            Type regionType = RuntimeType("UPlayGround.Manager.World.ContinuousWorldRegion");
            Array regions = Array.CreateInstance(regionType, 3);
            for (int i = 0; i < 3; i++)
            {
                object region = Activator.CreateInstance(regionType);
                regionType.GetField("Bounds").SetValue(region, new Rect(i * 100, 0, 80, 80));
                regions.SetValue(region, i);
            }
            Component world = AddComponent(CreateScene(), RuntimeType("UPlayGround.Manager.World.ContinuousWorldController"), false);
            world.GetType().GetField("Regions").SetValue(world, regions);
            var find = world.GetType().GetMethod("FindRegion");
            Assert.That(find.Invoke(world, new object[] {new Vector3(220, 50, 20), null}), Is.SameAs(regions.GetValue(2)));
            Assert.That(find.Invoke(world, new object[] {new Vector3(281, 50, 20), regions.GetValue(2)}), Is.SameAs(regions.GetValue(2)));
            Assert.That(find.Invoke(world, new object[] {new Vector3(284, 50, 20), regions.GetValue(2)}), Is.Null);
            Assert.That(find.Invoke(world, new object[] {new Vector3(190, 50, 20), null}), Is.Null);
        }

        [Test]
        public void 빠른_이동은_먼_다른_지역의_로드를_요구하지_않는다()
        {
            Type regionType = RuntimeType("UPlayGround.Manager.World.ContinuousWorldRegion");
            object destination = Activator.CreateInstance(regionType);
            object distant = Activator.CreateInstance(regionType);
            Component world = AddComponent(CreateScene(), RuntimeType("UPlayGround.Manager.World.ContinuousWorldController"), false);
            world.GetType().GetField("_travelDestination", InstanceFields).SetValue(world, destination);
            world.GetType().GetField("<IsTravelling>k__BackingField", InstanceFields).SetValue(world, true);
            regionType.GetMethod("Initialize", InstanceFields).Invoke(distant, new object[] {world});
            regionType.GetField("Bounds").SetValue(distant, new Rect(1000, 1000, 100, 100));
            regionType.GetField("UnloadDelay").SetValue(distant, 0f);
            regionType.GetMethod("UpdateDemand", InstanceFields).Invoke(distant, new object[] {Vector3.zero});
            Assert.That(regionType.GetProperty("State").GetValue(distant).ToString(), Is.EqualTo("Unloaded"));
            Assert.That(regionType.GetProperty("HasHandle").GetValue(distant), Is.False);
            Assert.That(regionType.GetField("_desired", InstanceFields).GetValue(distant), Is.False);
        }

        private Component AddArrival(Scene scene, string id)
        {
            Component point = AddComponent(scene, ArrivalType);
            ArrivalType.GetField("_id", InstanceFields).SetValue(point, id);
            RegistryType.GetMethod("Register").Invoke(null, new object[] { point });
            _arrivals.Add(point);
            return point;
        }

        private static object FindArrival(string id, Scene scene)
        {
            object[] args = { id, scene, null };
            bool found = (bool)RegistryType.GetMethod("TryGet", new[] { typeof(string), typeof(Scene), ArrivalType.MakeByRefType() })
                .Invoke(null, args);
            return found ? args[2] : null;
        }
    }
}
