using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Save;

namespace UPlayGround.Combat.Tests
{
    /// <summary>채집 재생의 시간 경계와 저장 복원을 실제 매니저 경로로 검사한다.</summary>
    public sealed class GatheringRegrowthTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private Component _world, _respawn, _clock;
        private GatheringActor _crop;
        private InteractableActorSO _data;
        private UnityEngine.SceneManagement.Scene _testScene;
        private readonly Dictionary<FieldInfo, object> _singletons = new();

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("GatheringRegrowthTest");
            _testScene = EditorSceneManager.NewPreviewScene();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_root, _testScene);
            var ownership = Type.GetType("UPlayGround.Manager.World.RegionOwnership, Assembly-CSharp", true);
            ownership.GetMethod("Register").Invoke(null, new object[] { _testScene, "farm-test" });
            _root.SetActive(false);
            _world = CreateManager("WorldStateManager");
            _clock = CreateManager("GameTimeManager");
            _respawn = CreateManager("InteractionRespawnManager");
            CreateManager("MonsterRespawnManager");
            var scene = CreateManager("SceneManager");
            scene.GetType().GetField("_currentMapID", PrivateInstance).SetValue(scene, "farm-test");
            _respawn.GetType().GetField("_clock", PrivateInstance).SetValue(_respawn, _clock);
            var node = new GameObject("Crop");
            node.transform.SetParent(_root.transform);
            node.AddComponent<SceneEntityId>().EditorSetGuid("crop");
            _crop = node.AddComponent<GatheringActor>();
            _data = ScriptableObject.CreateInstance<InteractableActorSO>();
            _data.regrowGameMinutes = 360;
            _data.hp = 1;
            _crop.Init(_data);
            Call(_respawn, "RegisterRuntimePlacement", _crop, "crop");
            SetTime(100);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_data);
            var ownership = Type.GetType("UPlayGround.Manager.World.RegionOwnership, Assembly-CSharp", true);
            ownership.GetMethod("Unregister").Invoke(null, new object[] { _testScene });
            EditorSceneManager.ClosePreviewScene(_testScene);
            foreach (var entry in _singletons) entry.Key.SetValue(null, entry.Value);
            _singletons.Clear();
        }

        [Test]
        public void 재생_직전에는_숨고_정확한_시각에_한번_복원한다()
        {
            Assert.That(Call(_respawn, "TryConsume", _crop), Is.True);
            Assert.That(_crop.gameObject.activeSelf, Is.False);
            SetTime(459);
            Tick();
            Assert.That(_crop.gameObject.activeSelf, Is.False);
            SetTime(460);
            Tick();
            Assert.That(_crop.gameObject.activeSelf, Is.True);
            Assert.That(Call(_world, "IsInteractableConsumed", "farm-test", "crop"), Is.False);
            Call(_respawn, "TryConsume", _crop);
            Tick();
            Assert.That(_crop.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void 몬스터_재생성은_농작물의_대기시간을_단축하지_않는다()
        {
            Call(_respawn, "TryConsume", _crop);
            Assert.That(Call(_respawn, "RespawnConsumedInteractables"), Is.EqualTo(0));
            Assert.That(_crop.gameObject.activeSelf, Is.False);
            Assert.That(Call(_world, "IsInteractableConsumed", "farm-test", "crop"), Is.True);
        }

        [TestCase(0)]
        [TestCase(360)]
        public void 저장_복원은_소모시각을_보존하고_수확전_저장은_즉시_복구한다(float regrowMinutes)
        {
            _data.regrowGameMinutes = regrowMinutes;
            var before = new GameSaveData();
            Call(_world, "ExportSaveData", before);
            Call(_respawn, "TryConsume", _crop);
            var harvested = new GameSaveData();
            Call(_world, "ExportSaveData", harvested);
            SetTime(300);
            Call(_world, "ImportSaveData", harvested);
            if (regrowMinutes > 0)
                Assert.That(Call(_world, "GetInteractionConsumedMinute", "farm-test", "crop"), Is.EqualTo(100f));
            Assert.That(_crop.gameObject.activeSelf, Is.False);
            Call(_world, "ImportSaveData", before);
            Assert.That(_crop.gameObject.activeSelf, Is.True);
            if (regrowMinutes > 0)
                Assert.That(harvested.world.interactionConsumedMinutes["farm-test"]["crop"], Is.EqualTo(100f));
            Call(_world, "ImportSaveData", harvested);
            Assert.That(_crop.gameObject.activeSelf, Is.False, "획득 후 상태도 다시 적용되어야 한다.");
        }

        [Test]
        public void 이전_저장은_첫_복원부터_시간을_세고_일반채집은_기존규칙을_유지한다()
        {
            var legacy = new GameSaveData();
            legacy.world = new WorldStateSaveData();
            legacy.world.interactionConsumedMinutes = null;
            legacy.world.consumedInteractables["farm-test"] = new List<string> { "crop" };
            Call(_world, "ImportSaveData", legacy);
            Assert.That(Call(_world, "GetInteractionConsumedMinute", "farm-test", "crop"), Is.EqualTo(100f));
            _data.regrowGameMinutes = 0;
            Assert.That(Call(_respawn, "RespawnConsumedInteractables"), Is.EqualTo(1));
            Assert.That(_crop.gameObject.activeSelf, Is.True);
        }

        private Component CreateManager(string name)
        {
            var type = Type.GetType("UPlayGround.Manager." + name + ", Assembly-CSharp", true);
            var instance = type.BaseType.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _singletons.Add(instance, instance.GetValue(null));
            var manager = _root.AddComponent(type);
            instance.SetValue(null, manager);
            return manager;
        }

        private void SetTime(float minute) => _clock.GetType().GetProperty("TotalGameMinutes").SetValue(_clock, minute);

        private void Tick() => _respawn.GetType().GetMethod("RestoreMaturePlacements", PrivateInstance).Invoke(_respawn, null);

        private static object Call(Component target, string method, params object[] arguments)
        {
            foreach (var candidate in target.GetType().GetMethods())
            {
                if (candidate.Name != method) continue;
                var parameters = candidate.GetParameters();
                if (parameters.Length != arguments.Length) continue;
                if (arguments.Length > 0 && !parameters[0].ParameterType.IsInstanceOfType(arguments[0])) continue;
                return candidate.Invoke(target, arguments);
            }
            throw new MissingMethodException(method);
        }
    }
}
