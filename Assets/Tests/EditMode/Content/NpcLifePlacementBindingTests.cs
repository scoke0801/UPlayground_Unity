using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Content.Tests
{
    /// <summary>실제 생활 바인딩의 로더 간 GUID 충돌·해제·재스폰 연결을 검사한다.</summary>
    public sealed class NpcLifePlacementBindingTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> _objects = new();
        private readonly List<Component> _bindings = new();
        private Scene _scene;
        private static Type BindingType => Type.GetType("UPlayGround.Gameplay.Interaction.LivingWorld.NpcLifePlacementBinding, Assembly-CSharp", true);
        private static Type LoaderType => Type.GetType("UPlayGround.Manager.World.RuntimePlacementLoader, Assembly-CSharp", true);
        private static Type ActorType(string name) => Type.GetType(name + ", UPlayGround.Actor", true);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Fields).Invoke(target, args);

        [SetUp]
        public void SetUp() => _scene = EditorSceneManager.NewPreviewScene();

        [TearDown]
        public void TearDown()
        {
            for (int i = _bindings.Count - 1; i >= 0; i--) Call(_bindings[i], "OnDisable");
            _bindings.Clear();
            for (int i = _objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(_objects[i]);
            _objects.Clear(); EditorSceneManager.ClosePreviewScene(_scene);
        }

        private GameObject Create(string name)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, _scene); _objects.Add(go); return go;
        }

        private Component CreateBinding(string guid, string map, out Component loader, out Component routine, bool enable = true)
        {
            loader = Create("배치 로더").AddComponent(LoaderType);
            routine = Create("생활 일과").AddComponent(ActorType("UPlayGround.Components.LivingWorld.NpcRoutine"));
            Set(routine, "_mapId", map);
            var binding = Create("생활 바인딩").AddComponent(BindingType); _bindings.Add(binding);
            Set(binding, "_loader", loader);
            Type entryType = BindingType.GetNestedType("Entry"); var entries = Array.CreateInstance(entryType, 1);
            object entry = Activator.CreateInstance(entryType);
            entryType.GetField("sceneEntityGuid").SetValue(entry, guid); entryType.GetField("routine").SetValue(entry, routine);
            entries.SetValue(entry, 0); Set(binding, "_entries", entries);
            if (enable) Call(binding, "OnEnable");
            return binding;
        }

        private Component CreateBody(string guid, Component loader)
        {
            var body = Create("기존 주민 몸"); body.SetActive(false); body.AddComponent<CapsuleCollider>();
            body.AddComponent(ActorType("UPlayGround.Components.NpcBrain"));
            var npc = body.AddComponent(ActorType("UPlayGround.NpcActor"));
            var id = body.AddComponent(ActorType("UPlayGround.Components.SceneEntityId"));
            id.GetType().GetMethod("RuntimeSetGuid").Invoke(id, new object[] { guid });
            ((IList)Get(loader, "_instances")).Add(body);
            return npc;
        }

        private static object BoundRoutine(Component body)
        {
            var controller = body.GetComponent(ActorType("UPlayGround.MovementController.NpcMovementController"));
            return controller.GetType().GetProperty("Routine").GetValue(controller);
        }

        [Test]
        public void 같은_맵_GUID의_다른_로더_바인딩은_양쪽_실행을_거부한다()
        {
            var first = CreateBinding("resident", "map", out var loader, out var routine);
            var body = CreateBody("resident", loader); Call(first, "OnNpcSpawned", "resident", body);
            Assert.AreSame(routine, BoundRoutine(body));
            var second = CreateBinding("resident", "map", out _, out _);
            Assert.IsNull(BoundRoutine(body));
            Assert.IsNotEmpty((string)BindingType.GetProperty("BindingFailure").GetValue(first));
            Assert.IsNotEmpty((string)BindingType.GetProperty("BindingFailure").GetValue(second));
            Call(second, "OnDisable");
            Assert.AreSame(routine, BoundRoutine(body), "충돌한 바인딩 해제 뒤 남은 몸에 재연결해야 합니다.");
        }

        [Test]
        public void 로더_내_같은_GUID의_몸_중복은_기존_실행도_정리한다()
        {
            var binding = CreateBinding("resident", "map", out var loader, out var routine);
            var first = CreateBody("resident", loader); Call(binding, "OnNpcSpawned", "resident", first);
            Assert.AreSame(routine, BoundRoutine(first));
            var second = CreateBody("resident", loader); Call(binding, "OnNpcSpawned", "resident", second);
            Assert.IsNull(BoundRoutine(first)); Assert.IsNull(BoundRoutine(second));
            Assert.IsNotEmpty((string)BindingType.GetProperty("BindingFailure").GetValue(binding));
        }

        [Test]
        public void 디스폰과_재스폰은_새_몸에만_연결한다()
        {
            var binding = CreateBinding("resident", "map", out var loader, out var routine);
            var first = CreateBody("resident", loader); Call(binding, "OnNpcSpawned", "resident", first);
            Call(binding, "OnCleared"); ((IList)Get(loader, "_instances")).Clear();
            Assert.IsNull(BoundRoutine(first));
            var second = CreateBody("resident", loader); Call(binding, "OnNpcSpawned", "resident", second);
            Assert.AreSame(routine, BoundRoutine(second)); Assert.IsNull(BoundRoutine(first));
        }

        [Test]
        public void 무효_바인딩의_비활성화는_다른_소유자의_몸을_해제하지_않는다()
        {
            var valid = CreateBinding("resident", "map", out var loader, out var routine);
            var body = CreateBody("resident", loader); Call(valid, "OnNpcSpawned", "resident", body);
            var invalid = CreateBinding("other", "map", out _, out _, false);
            var entries = (Array)Get(invalid, "_entries");
            entries.GetValue(0).GetType().GetField("routine").SetValue(entries.GetValue(0), routine);
            Set(invalid, "_loader", null); Call(invalid, "OnEnable"); Call(invalid, "OnDisable");
            Assert.AreSame(routine, BoundRoutine(body));
        }
    }
}
