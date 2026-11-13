using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Item;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Gameplay.World.ReactiveEnvironment;
using UPlayGround.Manager;
using UPlayGround.Manager.World;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>실제 월드 저장 어댑터의 pending 보상·재로드·언로드 경계를 검사한다.</summary>
    public sealed class ReactiveEnvironmentIntegrationTests
    {
        private UnityEngine.SceneManagement.Scene _scene;
        private WorldStateManager _world;
        private ReactiveEnvironmentObject _target;
        private ReactiveEnvironmentDefinitionSO _definition;
        private DropServices _drops;

        [SetUp]
        public void SetUp()
        {
            Assert.That(Application.isPlaying, Is.False);
            Assert.That(Services.TryGet<IItemService>(out _), Is.False, "실행 중인 서비스가 있는 환경에서는 실행하지 않습니다.");
            Assert.That(Services.TryGet<IGroundDropService>(out _), Is.False);
            _scene = EditorSceneManager.NewPreviewScene();
            RegionOwnership.Register(_scene, "ReactiveEnvironmentTest");
            var root = Create("Fixture");
            _world = root.AddComponent<WorldStateManager>();
            _definition = ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
            _definition.definitionId = "test.crate";
            var weather = Create("Weather").AddComponent<EnvironmentController>();
            var target = Create("Crate");
            target.AddComponent<SceneEntityId>().RuntimeSetGuid("test-crate-guid");
            _target = target.AddComponent<ReactiveEnvironmentObject>();
            var intact = Create("Intact"); intact.transform.SetParent(target.transform);
            var remnant = Create("Remnant"); remnant.transform.SetParent(target.transform);
            Set(_target, "_definition", _definition); Set(_target, "_weather", weather);
            Set(_target, "_intact", intact); Set(_target, "_remnant", remnant);
            Set(_target, "_rainSamples", new[] { target.transform });
            Invoke(_target, "Awake");
            Assert.That(_world.RegisterEnvironment(_target), Is.True);
            _drops = new DropServices();
            Services.Register(_drops);
        }

        [TearDown]
        public void TearDown()
        {
            if (_drops != null) { Services.Unregister(_drops); UnityEngine.Object.DestroyImmediate(_drops.Settings); }
            if (_scene.IsValid()) { RegionOwnership.Unregister(_scene); EditorSceneManager.ClosePreviewScene(_scene); }
            if (_definition != null) UnityEngine.Object.DestroyImmediate(_definition);
        }

        [Test]
        public void FireSpreadsOnlyAfterIgnitionCommitAndRespectsSurfaceLayer()
        {
            _definition.spreadRadius = 3;
            _definition.spreadHeatPerSecond = 5;
            _definition.fireBlockerLayers = 1;
            var clone = UnityEngine.Object.Instantiate(_target.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, _scene);
            clone.transform.position = Vector3.right;
            clone.GetComponent<SceneEntityId>().RuntimeSetGuid("second");
            var second = clone.GetComponent<ReactiveEnvironmentObject>();
            Invoke(second, "Awake");
            Assert.That(_world.RegisterEnvironment(second), Is.True);
            _world.ApplyEnvironmentImpact(_target, 1, 0, 0, 1, 0);
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(second.State.heat, Is.Zero);
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(second.State.heat, Is.GreaterThan(0));
            var isolated = ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
            try
            {
                isolated.definitionId = _definition.definitionId;
                isolated.surfaceLayerId = 1;
                Set(second, "_definition", isolated);
                var before = second.State.heat;
                Invoke(_world, "TickEnvironment", 0.2f);
                Assert.That(second.State.heat, Is.LessThanOrEqualTo(before + 0.12f));
            }
            finally { UnityEngine.Object.DestroyImmediate(isolated); }
        }

        [Test]
        public void ShortWaterEmitterContributesItsPartialFinalTick()
        {
            var emitter = Create("Water").AddComponent<EnvironmentElementEmitter>();
            emitter.waterPerSecond = 10;
            emitter.lifetime = 0.1f;
            emitter.gameObject.SetActive(true);
            _world.RegisterEnvironmentEmitter(emitter);
            Invoke(_world, "TickEnvironment", 0.1f);
            Assert.That(_target.State.wetness, Is.Zero);
            Invoke(_world, "TickEnvironment", 0.1f);
            Assert.That(_target.State.wetness, Is.GreaterThan(0.9f));
            float wet = _target.State.wetness;
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(_target.State.wetness, Is.LessThan(wet));
        }

        [Test]
        public void PendingDestructionRestoresAndRewardsOnlyOnce()
        {
            Assert.That(_world.ApplyEnvironmentImpact(_target, 1, 0, 100, 1, 0), Is.True);
            var pending = new WorldStateSaveData();
            Invoke(_world, "ExportEnvironment", pending);
            Assert.That(pending.reactiveEnvironment[0].rewardCommitted, Is.False);
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(_drops.created, Is.EqualTo(1));
            Assert.That(pending.reactiveEnvironment[0].rewardCommitted, Is.False, "저장 스냅샷은 실행 상태의 별칭이 아니어야 합니다.");
            var restored = JsonConvert.DeserializeObject<WorldStateSaveData>(JsonConvert.SerializeObject(pending));
            Invoke(_world, "ImportEnvironment", restored);
            Assert.That(_drops.calls, Is.EqualTo(1), "로드 적용 도중에는 보상을 만들지 않습니다.");
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(_drops.created, Is.EqualTo(1));
            var committed = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", committed);
            Assert.That(committed.reactiveEnvironment[0].rewardCommitted, Is.True);
            Invoke(_world, "ImportEnvironment", committed);
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(_drops.calls, Is.EqualTo(2));
            Assert.That(_target.State.isBroken, Is.True);
            Assert.That(_target.State.fuel, Is.LessThan(20));
        }

        [Test]
        public void UnloadRetainsPendingHeatAndOldSaveResetsState()
        {
            _world.ApplyEnvironmentImpact(_target, 1, 0, 20, 1, 0);
            _world.UnregisterEnvironment(_target);
            Assert.That(_world.RegisterEnvironment(_target), Is.True);
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(_target.State.charAmount, Is.GreaterThan(0));
            Invoke(_world, "ImportEnvironment", new WorldStateSaveData { reactiveEnvironment = null });
            Assert.That(_target.State.durability, Is.EqualTo(100));
            Assert.That(_target.State.fuel, Is.EqualTo(20));
        }

        [Test]
        public void FallingPoseIsRestoredWithoutReplayingTheTransition()
        {
            _target.transform.position = new Vector3(12000,4000,12000);
            var recipe = ScriptableObject.CreateInstance<EnvironmentDestructionRecipeSO>();
            recipe.mode = EnvironmentDestructionMode.DirectionalFall;
            recipe.fallSeconds = 1; recipe.trunkLength = 2; recipe.trunkRadius = 0.1f;
            recipe.fallBlockerLayers = 1;
            var destruction = _target.gameObject.AddComponent<ReactiveEnvironmentDestruction>();
            var pivot = Create("CutPivot"); pivot.transform.SetParent(_target.transform.Find("Remnant"),false);
            var fallback = Create("Collapse"); fallback.transform.SetParent(_target.transform.Find("Remnant"),false);
            Set(destruction,"_fallPivot",pivot.transform); Set(destruction,"_blockedFallback",fallback);
            Set(_target,"_destruction",destruction);
            _definition.destruction = recipe;
            try
            {
                destruction.SetImpactDirection(Vector3.right);
                _world.ApplyEnvironmentImpact(_target,1,0,100,0,0);
                Invoke(_world,"FixedTickEnvironment",0.25f);
                var save = new WorldStateSaveData(); Invoke(_world,"ExportEnvironment",save);
                Assert.That(save.reactiveEnvironment[0].destruction.progress, Is.GreaterThan(0));
                Quaternion pose = pivot.transform.localRotation;
                Invoke(_world,"FixedTickEnvironment",0.25f);
                Invoke(_world,"ImportEnvironment",save);
                Assert.That(Quaternion.Angle(pose,pivot.transform.localRotation), Is.LessThan(0.01f));
                Invoke(_world,"ImportEnvironment",new WorldStateSaveData());
                Assert.That(Quaternion.Angle(Quaternion.identity,pivot.transform.localRotation), Is.LessThan(0.01f));
            }
            finally { _definition.destruction = null; UnityEngine.Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void RoofBreakSwitchesTheSharedShelterCollider()
        {
            var recipe = ScriptableObject.CreateInstance<EnvironmentDestructionRecipeSO>();
            recipe.changesShelter = true;
            var destruction = _target.gameObject.AddComponent<ReactiveEnvironmentDestruction>();
            Set(_target,"_destruction",destruction); Set(_target,"_world",_world);
            _definition.destruction = recipe;
            var effects = Create("Effects").AddComponent<EnvironmentEffects>();
            var volume = Create("Shelter").AddComponent<BoxCollider>();
            volume.transform.SetParent(_target.transform,false);
            volume.size = Vector3.one * 5; volume.gameObject.SetActive(true);
            _target.gameObject.SetActive(true);
            Set(destruction,"_shelterColliders",new Collider[] {volume});
            effects.BindShelters(new[] {volume},0);
            try
            {
                Assert.That(effects.IsRainSheltered(Vector3.zero), Is.True);
                _world.ApplyEnvironmentImpact(_target,1,0,100,0,0);
                Assert.That(effects.IsRainSheltered(Vector3.zero), Is.False);
                Assert.That(_world.EnvironmentShelterRevision, Is.EqualTo(1));
                Assert.That(_world.EnvironmentCollisionRevision, Is.EqualTo(1));
            }
            finally { _definition.destruction = null; UnityEngine.Object.DestroyImmediate(recipe); }
        }

        [Test]
        public void InvalidEnvironmentLoadPreservesOtherWorldProgress()
        {
            _world.RecordPermanentKill("map","monster");
            _world.RecordConsumedInteractable("map","gathering");
            _world.SetRespawnState(new MonsterRespawnState {mapId="map",guid="respawn"});
            var save=new GameSaveData(); Invoke(_world,"ExportEnvironment",save.world);
            save.world.reactiveEnvironment[0].state.fuel=float.NaN;
            Assert.Throws<ArgumentException>(()=>_world.ImportSaveData(save));
            Assert.That(_world.IsPermanentlyKilled("map","monster"),Is.True);
            Assert.That(_world.IsInteractableConsumed("map","gathering"),Is.True);
            Assert.That(_world.TryGetRespawnState("map","respawn",out _),Is.True);
        }

        [Test]
        public void InvalidLoadLeavesExistingEnvironmentStateIntact()
        {
            _world.ApplyEnvironmentImpact(_target, 1, 0, 20, 0, 0);
            var save = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", save);
            save.reactiveEnvironment[0].state.fuel = float.NaN;
            Assert.Throws<TargetInvocationException>(() => Invoke(_world, "ImportEnvironment", save));
            var after = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", after);
            Assert.That(after.reactiveEnvironment[0].state.durability, Is.EqualTo(80));
            Assert.That(after.reactiveEnvironment[0].state.fuel, Is.EqualTo(20));
        }

        [Test]
        public void DisabledShelterStopsBlockingRain()
        {
            var effects = Create("Effects").AddComponent<EnvironmentEffects>();
            var shelter = Create("Shelter").AddComponent<BoxCollider>();
            shelter.gameObject.SetActive(true);
            shelter.size = Vector3.one * 4;
            effects.BindShelters(new[] { shelter }, 0);
            Assert.That(effects.IsRainSheltered(Vector3.zero), Is.True);
            shelter.enabled = false;
            Assert.That(effects.IsRainSheltered(Vector3.zero), Is.False);
        }

        private GameObject Create(string name)
        {
            var root = new GameObject(name); root.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, _scene);
            return root;
        }
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private sealed class DropServices : IItemService, IGroundDropService
        {
            private readonly HashSet<string> _sources = new();
            public int calls, created;
            public bool IsItemDBLoaded => true;
            public GroundDropSettingsSO Settings { get; } = ScriptableObject.CreateInstance<GroundDropSettingsSO>();
            public event Action<string> Collected { add { } remove { } }
            public IReadOnlyList<GroundDropView> VisibleDrops => Array.Empty<GroundDropView>();
            public ItemSO GetItemData(int key) => null;
            public ItemSO GetItemData(ItemIdType key) => null;
            public List<ItemInstance> GetDropItemList(List<ItemDropList> table) => new();
            public List<ItemInstance> GetDropItemList(EnemyDropTableSO table) => new();
            public void SpawnItems(IReadOnlyList<ItemInstance> items, Vector3 position, string sourceId = null, Transform source = null)
            { calls++; if (_sources.Add(sourceId)) created++; }
            public bool CanPlayerCollect(string id) => false;
            public bool TryPlayerCollect(string id) => false;
            public bool TryReserve(string id, object owner, float duration) => false;
            public void Release(string id, object owner) { }
            public bool TryPetCollect(string id, object owner, Vector3 position, float distance) => false;
            public string RegisterPlaced(string id, ItemInstance item, Transform representation) => null;
            public void ResetPlaced(string id) { }
        }
    }
}
