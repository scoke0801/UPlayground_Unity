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
        private EnvironmentDestructionRecipeSO _fixtureRecipe;
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
            _fixtureRecipe = ScriptableObject.CreateInstance<EnvironmentDestructionRecipeSO>();
            _definition.destruction = _fixtureRecipe;
            var weather = Create("Weather").AddComponent<EnvironmentController>();
            var target = Create("Crate");
            target.AddComponent<SceneEntityId>().RuntimeSetGuid("test-crate-guid");
            target.AddComponent<ReactiveEnvironmentDestruction>();
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
            if (_fixtureRecipe != null) UnityEngine.Object.DestroyImmediate(_fixtureRecipe);
        }

        [Test]
        public void LightningCommitPersistsPendingHeatAndDoesNotReplay()
        {
            var profile = ScriptableObject.CreateInstance<EnvironmentLightningProfileSO>();
            try
            {
                profile.profileId = "test.lightning";
                profile.blockerLayers = 1;
                var state = new EnvironmentLightningSaveState { mapId = "ReactiveEnvironmentTest",
                    profileId = profile.profileId, randomState = 7, remainingMinutes = 4 };
                var states = new Dictionary<string, EnvironmentLightningSaveState> { { state.mapId, state } };
                Set(_world, "_lightningStates", states);
                var strike = new EnvironmentLightningStrike(state.mapId, state.profileId, 1, 20,
                    _target.ReactionPosition, Vector3.up, _definition.surfaceLayerId, 2, 1);
                Invoke(_world, "CommitLightning", state, profile, strike, _target);
                Invoke(_world, "CommitLightning", state, profile, strike, _target);
                var save = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", save);
                Assert.That(save.reactiveEnvironment[0].pendingHeat, Is.EqualTo(1));
                Assert.That(save.environmentLightning[0].committedSequence, Is.EqualTo(1ul));
                save = JsonConvert.DeserializeObject<WorldStateSaveData>(JsonConvert.SerializeObject(save));
                Invoke(_world, "ImportEnvironment", save);
                var restored = (Dictionary<string, EnvironmentLightningSaveState>)typeof(WorldStateManager)
                    .GetField("_lightningStates", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_world);
                Invoke(_world, "CommitLightning", restored[state.mapId], profile, strike, _target);
                var after = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", after);
                Assert.That(after.reactiveEnvironment[0].pendingHeat, Is.EqualTo(1));
                Assert.That(restored[state.mapId].isResumed, Is.False);
                Invoke(_world, "TickEnvironment", 0.2f);
                Assert.That(_target.State.heat, Is.GreaterThan(0));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void TerrainInventoryScansEveryLayerAndLastTileWithoutChangingDensity()
        {
            var data = new TerrainData();
            try
            {
                data.SetDetailResolution(64, 8);
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03.Prefabs/Environment/Reactive/PF_ReactiveFarmDetail.prefab");
                data.detailPrototypes = new[] {
                    new DetailPrototype { prototype = prefab, usePrototypeMesh = true, useInstancing = true },
                    new DetailPrototype { prototype = prefab, usePrototypeMesh = true, useInstancing = true } };
                var first = new int[64,64]; first[0,0] = 3;
                var last = new int[64,64]; last[63,63] = 5;
                data.SetDetailLayer(0,0,0,first); data.SetDetailLayer(0,0,1,last);
                var terrain = Create("全域Terrain").AddComponent<Terrain>(); terrain.terrainData = data;
                var inventory = ReactiveEnvironmentWorldAudit.InspectTerrain(terrain);
                Assert.That(inventory.layers.Count, Is.EqualTo(2));
                Assert.That(inventory.layers[0].densitySum, Is.EqualTo(3));
                Assert.That(inventory.layers[1].densitySum, Is.EqualTo(5));
                Assert.That(inventory.layers[1].nonzeroPixels, Is.EqualTo(1));
                Assert.That(inventory.layers[1].occupiedTiles[0], Is.EqualTo(new RectInt(32,32,32,32)));
                Assert.That(data.GetDetailLayer(63,63,1,1,1)[0,0], Is.EqualTo(5));
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void EffectOwnershipDoesNotExcludeMixedEnvironmentMeshes()
        {
            var effects = Create("표현 전용"); effects.AddComponent<ParticleSystem>();
            var mixed = Create("메시와 표현 혼합"); mixed.AddComponent<LODGroup>();
            var mesh = Create("메시"); mesh.transform.SetParent(mixed.transform); mesh.AddComponent<MeshRenderer>();
            var particles = Create("입자"); particles.transform.SetParent(mixed.transform); particles.AddComponent<ParticleSystem>();
            var report = ReactiveEnvironmentWorldAudit.Inspect(_scene);
            Assert.That(report.entries.Find(e => e.hierarchyPath.EndsWith("표현 전용")).status, Is.EqualTo("ExistingSystem"));
            Assert.That(report.entries.Find(e => e.hierarchyPath.EndsWith("메시와 표현 혼합")).status, Is.EqualTo("Unclassified"));
        }

        [Test]
        public void ProjectileContactExplosionAndZoneShareOneElementImpulse()
        {
            var budget = new UPlayGround.Combat.EnvironmentHitImpulseBudget();
            var payload = new EnvironmentHitPayload { heatImpulse = 2f, waterImpulse = 0.3f };
            Assert.That(budget.Consume(_target, payload).heatImpulse, Is.EqualTo(2f));
            var repeat = budget.Consume(_target, payload);
            Assert.That(repeat.heatImpulse, Is.Zero);
            Assert.That(repeat.waterImpulse, Is.Zero);
            budget.Clear();
            Assert.That(budget.Consume(_target, payload).heatImpulse, Is.EqualTo(2f));
        }

        [Test]
        public void InvalidLightningSavePreservesCurrentEnvironment()
        {
            _world.ApplyEnvironmentImpact(_target, 101, 0, 0, 1, 0);
            var save = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", save);
            save.environmentLightning.Add(new EnvironmentLightningSaveState {
                mapId = "ReactiveEnvironmentTest", profileId = "test", randomState = 0 });
            Assert.Throws<TargetInvocationException>(() => Invoke(_world, "ImportEnvironment", save));
            var after = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", after);
            Assert.That(after.reactiveEnvironment[0].pendingHeat, Is.EqualTo(1));
        }

        [Test]
        public void WorldStateRegistersForPresentationAndDestructionFixedTicks()
        {
            Assert.That(_world, Is.InstanceOf<IFixedUpdatableManager>(),
                "GameManager는 고정 갱신 계약을 구현한 매니저만 연출·낙하 tick에 등록한다.");
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

        [TestCase(-35f)]
        [TestCase(-31.9f)]
        [TestCase(-4.1f)]
        [TestCase(0f)]
        [TestCase(4.1f)]
        [TestCase(31.9f)]
        [TestCase(35f)]
        public void SparseSourceGridPreservesDistanceFalloffAtSignedCellBoundaries(float distance)
        {
            var emitter = Create("희소 공급원").AddComponent<EnvironmentElementEmitter>();
            emitter.transform.position = Vector3.right * distance;
            emitter.radius = 32;
            emitter.waterPerSecond = 1;
            emitter.gameObject.SetActive(true);
            _world.RegisterEnvironmentEmitter(emitter);
            Invoke(_world, "PrepareEnvironmentSources", 0.2f);
            var bindings = (System.Collections.IList)typeof(WorldStateManager)
                .GetField("_environmentBindings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_world);
            Invoke(_world, "ApplyEnvironmentSources", bindings[0], 0.2f);
            float actual = (float)bindings[0].GetType().GetField("sourceWater",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(bindings[0]);
            Assert.That(actual, Is.EqualTo(Mathf.Max(0, 1 - Mathf.Abs(distance) / 32)).Within(0.00001f));
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

        [TestCase(1f)]
        [TestCase(2.2f)]
        public void FallingPoseIsRestoredWithoutReplayingTheTransition(float acceleration)
        {
            _target.transform.position = new Vector3(12000,4000,12000);
            var recipe = ScriptableObject.CreateInstance<EnvironmentDestructionRecipeSO>();
            recipe.mode = EnvironmentDestructionMode.DirectionalFall;
            recipe.fallSeconds = 1; recipe.trunkLength = 2; recipe.trunkRadius = 0.1f;
            recipe.fallAcceleration = acceleration;
            recipe.fallBlockerLayers = 1;
            var destruction = _target.GetComponent<ReactiveEnvironmentDestruction>();
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

        [Test]
        public void NonDestructiveObjectNeedsNoFormsOrRecipe()
        {
            _world.UnregisterEnvironment(_target);
            _definition.reaction.canBreak = false;
            _definition.destruction = null;
            Set(_target, "_intact", null); Set(_target, "_remnant", null);
            Assert.That(_target.IsAuthoringValid(), Is.True);
            Assert.That(_world.RegisterEnvironment(_target), Is.True);
            Assert.That(_world.ApplyEnvironmentImpact(_target, 101, 0, 1000, 1, 0.2f), Is.True);
            Invoke(_world, "TickEnvironment", 0.2f);
            Assert.That(_target.State.isBroken, Is.False);
            Assert.That(_target.State.durability, Is.EqualTo(100));
            Assert.That(_drops.created, Is.Zero);
        }

        [Test]
        public void TerrainRangeBurnSaveReloadAndPartitionFailurePreserveSource()
        {
            var data = new TerrainData { size = new Vector3(32, 10, 32) };
            var material = new Material(Shader.Find("UPlayGround/Environment/Reactive Toon"));
            var profile = ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
            var prototype = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(prototype, _scene);
            UnityEngine.Object.DestroyImmediate(prototype.GetComponent<Collider>());
            prototype.GetComponent<Renderer>().sharedMaterial = material;
            prototype.SetActive(false);
            var root = Create("TerrainFixture");
            var terrain = root.AddComponent<Terrain>();
            var collider = root.AddComponent<TerrainCollider>();
            TerrainDetailReaction adapter = null;
            try
            {
                data.SetDetailResolution(32, 8);
                data.detailPrototypes = new[] { new DetailPrototype { prototype = prototype,
                    usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit } };
                var density = new int[32, 32];
                for (int z = 0; z < 32; z++) for (int x = 0; x < 32; x++) density[z,x] = 2;
                data.SetDetailLayer(0, 0, 0, density);
                terrain.terrainData = data; collider.terrainData = data;
                root.AddComponent<SceneEntityId>().RuntimeSetGuid("terrain-guid");
                adapter = root.AddComponent<TerrainDetailReaction>();
                profile.definitionId = "fixture.terrain";
                profile.reaction.canBreak = false; profile.reaction.initialFuel = 1; profile.reaction.burnRate = 10;
                Set(adapter, "_definition", profile);
                Set(adapter, "_weather", _target.GetType().GetField("_weather", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_target));
                Set(adapter, "_detailLayers", new[] {0}); Set(adapter, "_reactiveDetailPrefabs", new[] {prototype});
                Set(adapter, "_contentRevision", "fixture-revision");
                Set(adapter, "_detailPixelsPerCell", 8); Set(adapter, "_cellRegion", new RectInt(0,0,4,4));
                root.SetActive(true);
                adapter.Initialize(_world);
                Assert.That(adapter.CellCount, Is.EqualTo(16));
                Assert.That(terrain.terrainData, Is.Not.SameAs(data));
                Assert.That(collider.terrainData, Is.SameAs(terrain.terrainData));
                var hits = new List<UPlayGround.Combat.CombatHit>();
                var collected = new HashSet<IDamageable>();
                var shape = UPlayGround.Combat.CombatHitboxShape.Sphere(new Vector3(4,0,4), 1);
                _world.CollectCellHits(_target.transform, shape, null, collected, hits);
                Assert.That(hits.Count, Is.EqualTo(1));
                _world.CollectCellHits(_target.transform, shape, null, collected, hits);
                Assert.That(hits.Count, Is.EqualTo(1), "동일 공격의 sweep는 셀당 한 번");
                var attack = new UPlayGround.Data.AttackData { environment = new EnvironmentHitPayload {heatImpulse=1} };
                var projectile = Create("EnvironmentAreaProjectile").AddComponent<ProjectileRuntime>();
                projectile.transform.position = new Vector3(4,0,4);
                Set(projectile,"_attackData",attack);
                Set(projectile,"_environmentCellAttack",PlayerAttackController.Copy(attack));
                var bridge = new CellHitBridge(_world);
                Services.Register(bridge);
                try
                {
                    Invoke(projectile,"ApplyEnvironmentCellArea",1f,1f);
                    Invoke(projectile,"ApplyEnvironmentCellArea",1f,1f);
                    var cooldowns = (Dictionary<IDamageable,float>)projectile.GetType().GetField("_areaCooldowns",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(projectile);
                    Assert.That(cooldowns.Count,Is.EqualTo(1));
                    cooldowns.Clear();
                    Invoke(projectile,"ApplyEnvironmentCellArea",1f,1f);
                    var pending = new WorldStateSaveData(); Invoke(_world,"ExportEnvironment",pending);
                    Assert.That(pending.reactiveEnvironment.Find(record => record.guid == "terrain-guid:detail:0:0").pendingHeat,Is.EqualTo(1));
                }
                finally { Services.Unregister(bridge); }
                projectile.OnReturnedToPool();
                Assert.That(((List<UPlayGround.Combat.CombatHit>)projectile.GetType().GetField("_environmentCellHits",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(projectile)).Count,Is.Zero);
                Assert.That(((HashSet<IDamageable>)projectile.GetType().GetField("_environmentCollected",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(projectile)).Count,Is.Zero);
                Invoke(_world, "TickEnvironment", 0.2f);
                Assert.That(terrain.terrainData.GetDetailLayer(0,0,1,1,0)[0,0], Is.Zero);
                Assert.That(data.GetDetailLayer(0,0,1,1,0)[0,0], Is.EqualTo(2));
                int writes = adapter.DensityWrites;
                Invoke(_world, "TickEnvironment", 0.2f);
                Assert.That(adapter.DensityWrites, Is.EqualTo(writes), "밀도는 coverage 변경 때만 쓴다");
                var save = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", save);
                save = JsonConvert.DeserializeObject<WorldStateSaveData>(JsonConvert.SerializeObject(save));
                Invoke(_world, "ImportEnvironment", new WorldStateSaveData());
                Assert.That(terrain.terrainData.GetDetailLayer(0,0,1,1,0)[0,0], Is.EqualTo(2));
                Invoke(_world, "ImportEnvironment", save);
                Assert.That(terrain.terrainData.GetDetailLayer(0,0,1,1,0)[0,0], Is.Zero);
                adapter.Release();
                Assert.That(terrain.terrainData, Is.SameAs(data));
                adapter.Initialize(_world);
                Assert.That(terrain.terrainData.GetDetailLayer(0,0,1,1,0)[0,0], Is.Zero);
                adapter.Release();
                Set(adapter, "_partitionVersion", 2);
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "반응형 환경 저장의 정의·버전·tick 잔여 시간이 호환되지 않습니다.");
                Assert.Throws<InvalidOperationException>(() => adapter.Initialize(_world));
                Assert.That(terrain.terrainData, Is.SameAs(data));
            }
            finally
            {
                adapter?.Release();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(prototype);
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GatheringAndCombatShareRewardAndLegacyConsumption()
        {
            var data = ScriptableObject.CreateInstance<UPlayGround.Data.Actor.InteractableActorSO>();
            try
            {
                data.hp = 100;
                var actor = _target.gameObject.AddComponent<GatheringActor>();
                actor.Init(data);
                Set(_target, "_ownsGathering", true);
                Assert.That(_target.IsAuthoringValid(), Is.True);
                Set(_target, "_isBound", true); Set(_target, "_world", _world);
                Assert.That(actor.HasReactiveOwner, Is.True);
                Set(actor,"_isGathering",true);
                Assert.That(actor.IsInteracting(),Is.True);
                Assert.That(_target.TryGather(100), Is.True);
                Assert.That(actor.IsInteracting(),Is.False);
                actor.StopInteract();
                Assert.That(_target.TryGather(100), Is.False);
                Assert.That(actor.CanInteract(), Is.False);
                Invoke(_world, "TickEnvironment", 0.2f);
                Assert.That(_drops.created, Is.EqualTo(1));
                Assert.That(_world.ApplyEnvironmentCombatHit(_target, 100), Is.False);
                var oldSave = new WorldStateSaveData {
                    consumedInteractables = new Dictionary<string,List<string>> {
                        { "ReactiveEnvironmentTest", new List<string> { _target.StableId } } } };
                Invoke(_world, "ImportEnvironment", oldSave);
                Assert.That(_target.State.isBroken, Is.True);
                Invoke(_world, "TickEnvironment", 0.2f);
                Assert.That(_drops.calls, Is.EqualTo(1), "구 채집 완료를 다시 보상하지 않는다");
                Invoke(_world, "ImportEnvironment", new WorldStateSaveData());
                Assert.That(_target.State.isBroken, Is.False);
                Assert.That(actor.CanInteract(), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(data); }
        }

        [Test]
        public void PlacementRecordRecreationKeepsEnvironmentId()
        {
            var loader = Create("PlacementLoader").AddComponent<UPlayGround.Manager.World.RuntimePlacementLoader>();
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Environment/Reactive/PF_ReactiveCargoCrate.prefab");
            Assert.That(prefab, Is.Not.Null);
            var record = new UPlayGround.Data.World.WorldPlacementRecord {
                prefab = prefab, placementGuid = "placement-source", sceneEntityGuid = "environment-placement",
                position = Vector3.one, rotation = Quaternion.identity, scale = Vector3.one };
            var first = (GameObject)Invoke(loader, "SpawnViaPrefab", record);
            Assert.That(first.GetComponent<SceneEntityId>().Guid, Is.EqualTo(record.sceneEntityGuid));
            UnityEngine.Object.DestroyImmediate(first);
            var second = (GameObject)Invoke(loader, "SpawnViaPrefab", record);
            Assert.That(second.GetComponent<SceneEntityId>().Guid, Is.EqualTo(record.sceneEntityGuid));
            Assert.That(prefab.GetComponent<SceneEntityId>().Guid, Is.Empty);
        }

        [Test]
        public void AuthoringPreservesExistingIdAndUndoRestoresProfile()
        {
            string id = _target.StableId;
            var replacement = ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
            replacement.definitionId = "fixture.replacement"; replacement.reaction.canBreak = false;
            try
            {
                ReactiveEnvironmentAuthoring.Apply(_target.gameObject, replacement,
                    (EnvironmentController)_target.GetType().GetField("_weather", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_target));
                Assert.That(_target.Definition, Is.SameAs(replacement));
                Assert.That(_target.StableId, Is.EqualTo(id));
                UnityEditor.Undo.PerformUndo();
                Assert.That(_target.Definition, Is.SameAs(_definition));
                Assert.That(_target.StableId, Is.EqualTo(id));
            }
            finally { UnityEngine.Object.DestroyImmediate(replacement); }
        }

        [Test]
        public void CurrentTerrainPresetUsesOwnedDetailAndPreservesSource()
        {
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>(
                "Assets/10.Datas/World/OpenWorld/WorldThemesABC/Terrain.asset");
            var preset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(
                "Assets/10.Datas/Environment/Profiles/LakeVillageDetails.preset");
            var root = Create("CurrentTerrainPreset");
            root.transform.position = new Vector3(12000,4000,12000);
            var terrain = root.AddComponent<Terrain>(); terrain.terrainData = data;
            var adapter = root.AddComponent<TerrainDetailReaction>();
            Assert.That(preset.ApplyTo(adapter), Is.True);
            root.GetComponent<SceneEntityId>().RuntimeSetGuid("actual-terrain-fixture");
            Set(adapter, "_weather", _target.GetType().GetField("_weather", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_target));
            int before = 0; foreach (int count in data.GetDetailLayer(464,408,16,16,1)) before += count;
            try
            {
                Assert.That(adapter.ValidateAuthoring(), Is.Null);
                adapter.Initialize(_world);
                Assert.That(adapter.CellCount, Is.GreaterThan(0));
                Assert.That(terrain.terrainData.treeInstanceCount, Is.EqualTo(data.treeInstanceCount));
                Assert.That(terrain.terrainData.detailPrototypes[1].useInstancing, Is.True);
                var cells = (Array)adapter.GetType().GetField("_cells",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(adapter);
                IEnvironmentReactionTarget seed = null;
                foreach (var value in cells) if (value != null) { seed = (IEnvironmentReactionTarget)value; break; }
                Assert.That(_world.ApplyEnvironmentImpact(seed,999,0,0,1,0),Is.True);
                for(int tick=0;tick<8;tick++) Invoke(_world,"TickEnvironment",0.2f);
                var spread = new WorldStateSaveData(); Invoke(_world,"ExportEnvironment",spread);
                int burning = 0;
                foreach(var record in spread.reactiveEnvironment)
                    if(record.guid.StartsWith("actual-terrain-fixture:detail:") && record.state.combustion == UPlayGround.Core.ReactiveEnvironment.EnvironmentCombustion.Burning) burning++;
                Assert.That(burning,Is.GreaterThan(1),"실제 셀 간격과 연소 시간 안에서 인접 셀 발화");
                adapter.Release();
                Assert.That(terrain.terrainData, Is.SameAs(data));
                int after = 0; foreach (int count in data.GetDetailLayer(464,408,16,16,1)) after += count;
                Assert.That(after, Is.EqualTo(before));
            }
            finally { adapter.Release(); }
        }

        [Test]
        public void CellGeometryRejectsBoundsOnlyFalsePositives()
        {
            var method = typeof(TerrainDetailReaction).GetMethod("IntersectsCell", BindingFlags.Static | BindingFlags.NonPublic);
            var cell = new Bounds(new Vector3(0.5f,0.25f,0.5f), new Vector3(1,0.5f,1));
            var outside = new[] {
                UPlayGround.Combat.CombatHitboxShape.Sphere(new Vector3(1.1f,0.25f,1.1f),0.12f),
                UPlayGround.Combat.CombatHitboxShape.Capsule(new Vector3(1.25f,0.25f,1.25f),
                    new Vector3(-1,0.25f,3.5f),new Vector3(3.5f,0.25f,-1),0.1f),
                UPlayGround.Combat.CombatHitboxShape.Box(new Vector3(1.25f,0.25f,1.25f),
                    Quaternion.Euler(0,135,0),new Vector3(0.05f,0.1f,3)) };
            foreach (var shape in outside) Assert.That(method.Invoke(null,new object[]{shape,cell}),Is.False);
            var inside = new[] {
                UPlayGround.Combat.CombatHitboxShape.Sphere(new Vector3(1,0.25f,0.5f),0.01f),
                UPlayGround.Combat.CombatHitboxShape.Capsule(Vector3.zero,new Vector3(-1,0.25f,0.5f),new Vector3(2,0.25f,0.5f),0.1f),
                UPlayGround.Combat.CombatHitboxShape.Box(cell.center,Quaternion.Euler(0,45,0),new Vector3(0.2f,0.2f,0.2f)) };
            foreach (var shape in inside) Assert.That(method.Invoke(null,new object[]{shape,cell}),Is.True);
        }

        [Test]
        public void OnlyAuthoredCollidersResolveToReactiveObject()
        {
            var included = _target.gameObject.AddComponent<BoxCollider>();
            var child = Create("ExcludedCollider"); child.transform.SetParent(_target.transform);
            var excluded = child.AddComponent<BoxCollider>();
            Set(_target,"_hitColliders",new Collider[]{included});
            var resolve = typeof(UPlayGround.Combat.CombatHitDetector).GetMethod("ResolveDamageable", BindingFlags.Static|BindingFlags.Public);
            Assert.That(resolve.Invoke(null,new object[]{included}),Is.SameAs(_target));
            Assert.That(resolve.Invoke(null,new object[]{excluded}),Is.Null);
        }

        [Test]
        public void OwnedContentBindingsKeepAllLodsAndOneTreeState()
        {
            foreach (string name in new[] {"CargoCrate","ArenaTree","FarmGrass","FarmTree"})
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03.Prefabs/Environment/Reactive/PF_Reactive" + name + ".prefab");
                Assert.That(prefab, Is.Not.Null, name);
                var clone = UnityEngine.Object.Instantiate(prefab);
                clone.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, _scene);
                clone.GetComponent<SceneEntityId>().RuntimeSetGuid("owned-"+name);
                var reaction = clone.GetComponent<ReactiveEnvironmentObject>();
                Set(reaction,"_weather",_target.GetType().GetField("_weather",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_target));
                Invoke(reaction,"Awake");
                try
                {
                    Assert.That(reaction.IsAuthoringValid(),Is.True,name);
                    Assert.That(_world.RegisterEnvironment(reaction),Is.True,name);
                    Assert.That(reaction.GetComponentsInChildren<ReactiveEnvironmentObject>(true).Length,Is.EqualTo(1));
                    Assert.That(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(clone),Is.Zero);
                    if (name != "FarmTree") continue;
                    _world.ApplyEnvironmentImpact(reaction,500,0,100,0,0);
                    Assert.That(reaction.State.isBroken,Is.True);
                    Assert.That(clone.GetComponent<GatheringActor>().CanInteract(),Is.False);
                    Assert.That(clone.transform.Find("Intact").gameObject.activeSelf,Is.False);
                    Assert.That(clone.transform.Find("Remnant").gameObject.activeSelf,Is.True);
                    var bindings = new UnityEditor.SerializedObject(reaction).FindProperty("_surfaceBindings");
                    var block = new MaterialPropertyBlock();
                    for(int i=0;i<bindings.arraySize;i++)
                    {
                        var binding=bindings.GetArrayElementAtIndex(i);
                        var renderer=(Renderer)binding.FindPropertyRelative("renderer").objectReferenceValue;
                        renderer.GetPropertyBlock(block,0);
                        Assert.That(block.GetVector("_EnvState0").z,Is.EqualTo(1),renderer.name);
                    }
                }
                finally { _world.UnregisterEnvironment(reaction); UnityEngine.Object.DestroyImmediate(clone); }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AuthoredFarmTreeAndActualTerrainCellsSpreadBothWays(bool treeIsSource)
        {
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/WorldThemesABC/Terrain.asset");
            Assert.That(data, Is.Not.Null, "현재 Terrain 원본");
            var preset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>("Assets/10.Datas/Environment/Profiles/LakeVillageDetails.preset");
            var root = Create("양방향 전파 Terrain"); root.transform.position = new Vector3(12000, 4000, 12000);
            root.AddComponent<Terrain>().terrainData = data;
            var adapter = root.AddComponent<TerrainDetailReaction>(); preset.ApplyTo(adapter);
            root.GetComponent<SceneEntityId>().RuntimeSetGuid("tree-grass-terrain");
            Set(adapter, "_weather", _target.GetType().GetField("_weather", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_target));
            var treeProfile = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<ReactiveEnvironmentDefinitionSO>(
                "Assets/10.Datas/Environment/Profiles/FarmTree.asset"));
            treeProfile.destruction = _fixtureRecipe;
            // 실제 수지 프로필과 Terrain 셀을 검증하며 별도 검사가 다루는 나무 렌더 형상은 생략한다.
            treeProfile.surfaceBackend = EnvironmentSurfaceBackend.LegacyTint;
            try
            {
                adapter.Initialize(_world);
                var cells = (Array)adapter.GetType().GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(adapter);
                IEnvironmentReactionTarget grass = null;
                foreach (var value in cells) if (value != null) { grass = (IEnvironmentReactionTarget)value; break; }
                Assert.That(grass, Is.Not.Null);
                _world.UnregisterEnvironment(_target);
                Set(_target, "_definition", treeProfile);
                _target.GetComponent<SceneEntityId>().RuntimeSetGuid("tree-near-grass");
                _target.transform.position = grass.ReactionPosition + Vector3.right * 3;
                Assert.That(_world.RegisterEnvironment(_target), Is.True);
                Assert.That(treeProfile.surfaceLayerId, Is.EqualTo(grass.Definition.surfaceLayerId));
                IEnvironmentReactionTarget source = treeIsSource ? _target : grass;
                IEnvironmentReactionTarget receiver = treeIsSource ? grass : _target;
                Assert.That(_world.ApplyEnvironmentImpact(source, 54321, 0, 0, 1, 0), Is.True);
                Invoke(_world, "TickEnvironment", 0.2f);
                var save = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", save);
                Assert.That(save.reactiveEnvironment.Find(r => r.guid == receiver.StableId).state.heat, Is.Zero, "이번 tick 점화는 다음 tick부터 전파");
                bool ignited = false;
                for (int i = 0; i < 50; i++)
                {
                    Invoke(_world, "TickEnvironment", 0.2f);
                    save = new WorldStateSaveData(); Invoke(_world, "ExportEnvironment", save);
                    if (save.reactiveEnvironment.Find(r => r.guid == receiver.StableId).state.combustion == UPlayGround.Core.ReactiveEnvironment.EnvironmentCombustion.Burning)
                    { ignited = true; break; }
                }
                Assert.That(ignited, Is.True, treeIsSource ? "나무→실제 Detail 셀" : "실제 Detail 셀→나무");
            }
            finally
            {
                adapter.Release(); _world.UnregisterEnvironment(_target);
                Set(_target, "_definition", _definition);
                UnityEngine.Object.DestroyImmediate(treeProfile);
            }
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

        private sealed class CellHitBridge : UPlayGround.Combat.IEnvironmentCellHitService
        {
            private readonly WorldStateManager _world;
            public CellHitBridge(WorldStateManager world) => _world = world;
            public void CollectCellHits(Transform owner, in UPlayGround.Combat.CombatHitboxShape shape,
                ISet<IDamageable> ignored, ISet<IDamageable> collected, List<UPlayGround.Combat.CombatHit> results) =>
                _world.CollectCellHits(owner, shape, ignored, collected, results);
        }

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
