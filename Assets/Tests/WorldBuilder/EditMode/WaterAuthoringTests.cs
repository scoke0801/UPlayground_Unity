using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Contracts;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WaterAuthoringTests
    {
        private string _folder, _data;
        private WorldDefinition _definition;
        private WaterSystemDefinition Source => _definition.WaterSystem;
        private WorldWaterQuery Query => UnityEngine.Object.FindObjectsByType<WorldWaterQuery>(FindObjectsSortMode.None).Single(q => q.definition == _definition);
        [SetUp] public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string id = Guid.NewGuid().ToString("N"); _folder = "Assets/WorldBuilder/Worlds/" + id; _data = "Assets/10.Datas/WorldBuilder/Test_" + id;
            _definition = WorldWaterValidationFixture.Create(id, _folder, _data, false, NewSceneMode.Single);
        }
        [TearDown] public void Cleanup()
        {
            WorldWaterAuthoring.FailureInjection = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (_folder != null) AssetDatabase.DeleteAsset(_folder); if (_data != null) AssetDatabase.DeleteAsset(_data);
        }
        private string PhysicalHash() => new WorldTerrainStateAdapter(_definition.Profile, WorldBuilderAuthoringAssets.FindTerrains(_definition)).Read().ComputeHash();
        private void AssertApplied()
        {
            Assert.That(PhysicalHash(), Is.EqualTo(_definition.AppliedTerrainHash)); Assert.That(Query.appliedHash, Is.EqualTo(Source.AppliedHash));
            Assert.That(WorldWaterAuthoring.Evaluate(_definition, Source).HasErrors, Is.False);
        }
        [Test] public void WaterLevelUndoRedoRestoresSoSceneNativeTerrainAndP04Pins()
        {
            string sourceRevision = _definition.SourceRevision, before = PhysicalHash(), hash = Source.AppliedHash, overrides = JsonUtility.ToJson(_definition.PlacementOverrides.Single());
            string plan = JsonUtility.ToJson(_definition.Planning);
            WorldWaterAuthoring.SetLevel(_definition, Source, Source.bodies[2].waterGuid, 7.8);
            string after = PhysicalHash(); Assert.That(after, Is.Not.EqualTo(before)); AssertApplied();
            Undo.PerformUndo(); Assert.That(PhysicalHash(), Is.EqualTo(before)); Assert.That(Source.AppliedHash, Is.EqualTo(hash)); AssertApplied();
            Undo.PerformRedo(); Assert.That(PhysicalHash(), Is.EqualTo(after)); AssertApplied();
            Assert.That(_definition.SourceRevision, Is.EqualTo(sourceRevision)); Assert.That(JsonUtility.ToJson(_definition.PlacementOverrides.Single()), Is.EqualTo(overrides)); Assert.That(JsonUtility.ToJson(_definition.Planning), Is.EqualTo(plan));
        }
        [Test] public void MoveDeleteUndoAndRepeatRebuildPreserveOtherWaterAndOperations()
        {
            var source = Source; string before = PhysicalHash(); string bodyId = source.bodies[2].waterGuid;
            WorldWaterAuthoring.Edit(_definition, source, () => { for (int i = 0; i < source.bodies[2].boundary.Length; i++) source.bodies[2].boundary[i].x += 1; });
            string moved = PhysicalHash(); Assert.That(moved, Is.Not.EqualTo(before)); Undo.PerformUndo(); Assert.That(PhysicalHash(), Is.EqualTo(before)); Undo.PerformRedo(); Assert.That(PhysicalHash(), Is.EqualTo(moved));
            WorldWaterAuthoring.Edit(_definition, source, () => { source.waterfalls.Clear(); source.bodies.RemoveAt(1); });
            Assert.That(Query.volumes.Length, Is.EqualTo(2)); Assert.That(source.bodies.Any(b => b.waterGuid == bodyId));
            Undo.PerformUndo(); Assert.That(Query.volumes.Length, Is.EqualTo(3)); AssertApplied();
            string hash = source.AppliedHash; WorldWaterAuthoring.Regenerate(_definition, source); Assert.That(source.AppliedHash, Is.EqualTo(hash)); AssertApplied();
        }
        [Test] public void PresetMaterialEditRegeneratesButDerivedMaterialEditIsProtected()
        {
            string sourceHash = Source.AppliedSourceHash, physical = PhysicalHash();
            Source.waterfallPreset.foamMaterial.SetColor("_BaseColor", new Color(.4f, .7f, .8f, .6f));
            WorldWaterAuthoring.Regenerate(_definition, Source);
            Assert.That(Source.AppliedSourceHash, Is.Not.EqualTo(sourceHash)); Assert.That(PhysicalHash(), Is.EqualTo(physical));
            var surface = Query.volumes[0].GetComponentInChildren<MeshRenderer>().sharedMaterial;
            string original = EditorJsonUtility.ToJson(surface);
            try
            {
                surface.SetColor("_ShallowColor", Color.magenta);
                Assert.Throws<InvalidOperationException>(() => WorldWaterAuthoring.Regenerate(_definition, Source));
            }
            finally { EditorJsonUtility.FromJsonOverwrite(original, surface); }
            WorldWaterAuthoring.Regenerate(_definition, Source); AssertApplied();
        }
        [TestCase("AfterAssets")] [TestCase("AfterTerrain")] [TestCase("AfterScene")]
        public void InjectedFailureRestoresEntireAuthoringResult(string point)
        {
            string physical = PhysicalHash(), source = JsonUtility.ToJson(Source), definition = JsonUtility.ToJson(_definition), scene = Query.hierarchyHash;
            WorldWaterAuthoring.FailureInjection = stage => { if (stage == point) throw new IOException("주입 실패 " + point); };
            Assert.Throws<IOException>(() => WorldWaterAuthoring.Edit(_definition, Source, () => { Source.bodies[2].level -= .1; Source.waterfalls[0].lowerLanding.y -= .1; }));
            WorldWaterAuthoring.FailureInjection = null;
            Assert.That(PhysicalHash(), Is.EqualTo(physical)); Assert.That(JsonUtility.ToJson(Source), Is.EqualTo(source)); Assert.That(JsonUtility.ToJson(_definition), Is.EqualTo(definition)); Assert.That(Query.hierarchyHash, Is.EqualTo(scene)); AssertApplied();
        }
        [Test] public void MissingBridgeWaterAndExcessWaterLevelAreConflictsWithoutRebinding()
        {
            string physical = PhysicalHash(); string id = Source.bodies[0].waterGuid;
            Assert.Throws<InvalidOperationException>(() => WorldWaterAuthoring.Edit(_definition, Source, () => Source.bodies[0].waterGuid = Guid.NewGuid().ToString("N")));
            Assert.That(Source.bodies[0].waterGuid, Is.EqualTo(id)); Assert.That(PhysicalHash(), Is.EqualTo(physical));
            Assert.Throws<InvalidOperationException>(() => WorldWaterAuthoring.Edit(_definition, Source, () => { foreach (int i in Enumerable.Range(0, Source.bodies[0].route.Length)) Source.bodies[0].route[i].y = 10; }));
            Assert.That(PhysicalHash(), Is.EqualTo(physical)); AssertApplied();
        }
        [Test] public void WaterQueryExclusionsPriorityAndExistingAdapterAgree()
        {
            var query = Query; Assert.That(query.TrySample(new Vector3(64, 7, 60), out WaterSample sample));
            Assert.That(sample.SurfaceY, Is.EqualTo(8).Within(.001)); Assert.That(sample.Depth, Is.EqualTo(2).Within(.03)); Assert.That(sample.Flow.z, Is.GreaterThan(0));
            Assert.That(query.TrySample(new Vector3(64, 6.2f, 42), out _), Is.False); Assert.That(query.TrySample(new Vector3(64, 7, 42), out _), Is.True);
            Assert.That(query.TrySample(new Vector3(60, 7, 60), out _), Is.False); Assert.That(query.TrySample(new Vector3(64, 0, 60), out _), Is.False);
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(new Vector3(64, 7, 60), out float level)); Assert.That(level, Is.EqualTo(sample.SurfaceY));
            Assert.That(query.TrySample(new Vector3(24, 16, 108), out _), Is.False, "폭포 curtain은 수영 수면이 아니다.");
            var a = new WaterSample { Priority = 1, SpecificityArea = 10, BodyId = "a" }; var b = new WaterSample { Priority = 1, SpecificityArea = 10, BodyId = "b" };
            Assert.That(WaterSample.Precedes(a, b)); b.Priority = 2; Assert.That(WaterSample.Precedes(b, a)); b.Priority = 1; b.SpecificityArea = 9; Assert.That(WaterSample.Precedes(b, a));
            query.volumes = query.volumes.Reverse().ToArray(); Assert.That(query.TrySample(new Vector3(64, 7, 60), out var reversed)); Assert.That(reversed.BodyId, Is.EqualTo(sample.BodyId));
        }
        [Test] public void UncapturedWaterChildAndBrokenDepthSculptAreNotDiscarded()
        {
            var child = new GameObject("사용자 보존 자식"); child.transform.SetParent(Query.transform, false);
            Assert.Throws<InvalidOperationException>(() => WorldWaterAuthoring.Regenerate(_definition, Source)); Assert.That(child != null); UnityEngine.Object.DestroyImmediate(child);
            using (var session = new WorldTerrainAuthoringSession(_definition, WorldBuilderAuthoringAssets.FindTerrains(_definition)))
            {
                string before = PhysicalHash(); Assert.Throws<InvalidOperationException>(() => session.AddOperation(new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"), kind = TerrainOperationKind.SetAbsolute, stage = 40, x = 64, z = 60, width = 1, height = 1, value = 7.9f }));
                Assert.That(PhysicalHash(), Is.EqualTo(before));
            }
        }
        [Test] public void T16_DistanceAttenuationAudioLimitAndExplicitVfxBounds()
        {
            var effects = Query.GetComponent<WorldWaterfallEffects>(); var emitter = effects.emitters.Single();
            Assert.That(Vector3.Distance(emitter.spray.transform.position, new Vector3((float)Source.waterfalls[0].upperLip.x, (float)Source.waterfalls[0].upperLip.y, (float)Source.waterfalls[0].upperLip.z)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(emitter.mist.transform.position, emitter.landing.position), Is.LessThan(.001f));
            Assert.That(emitter.audio.clip.samples, Is.GreaterThan(0)); Assert.That(emitter.mist.main.cullingMode, Is.EqualTo(ParticleSystemCullingMode.PauseAndCatchup));
            var old = effects.observer.transform.position; effects.observer.transform.position = emitter.landing.position + Vector3.up * 5;
            effects.Tick(); Assert.That(emitter.mist.emission.rateOverTime.constant, Is.GreaterThan(0)); Assert.That(emitter.audio.volume, Is.GreaterThan(0));
            effects.observer.transform.position = emitter.landing.position + Vector3.up * (effects.preset.farDistance + 1); effects.Tick(); Assert.That(emitter.mist.emission.rateOverTime.constant, Is.EqualTo(0)); Assert.That(emitter.audio.volume, Is.EqualTo(0));
            effects.observer.transform.position = old;
            var bounds = emitter.spray.GetComponent<ParticleSystemRenderer>().localBounds;
            Assert.That(bounds.Contains(emitter.spray.transform.InverseTransformPoint(emitter.landing.position)));
        }
        [Test] public void NewP04StructureAfterWaterKeepsOwnedOperationsAndExistingConnection()
        {
            var plan = _definition.Planning;
            plan.pois.Add(new PoiPlan { guid = Guid.NewGuid().ToString("N"), regionGuid = plan.regions[0].regionGuid, anchor = new PlanPoint(20, 11, 20), hasConfirmedElevation = true });
            var preset = UnityEngine.Object.Instantiate(plan.structures[0].preset); preset.requiresAccess = false;
            AssetDatabase.CreateAsset(preset, _data + "/OptionalStructure.asset");
            plan.structures.Add(new WorldPlan.StructureRecord { structureGuid = Guid.NewGuid().ToString("N"), poiGuid = plan.pois.Last().guid, preset = preset });
            WorldPlanningAuthoring.Regenerate(_definition, plan);
            Assert.That(plan.AppliedStructures.Count, Is.EqualTo(4)); AssertApplied();
        }
        [Test] public void OverlapSpecificityGuidPriorityAndRaycastUseActualVolumes()
        {
            var go = new GameObject("겹침 검증");
            try
            {
                var body = Source.bodies[0].ToCore(); body.guid = "ffffffffffffffffffffffffffffffff"; body.kind = WaterBodyKind.Lake; body.level = 8;
                body.boundary = new[] { new PlanPoint(62, 0, 50), new PlanPoint(66, 0, 50), new PlanPoint(66, 0, 70), new PlanPoint(62, 0, 70) };
                var volume = go.AddComponent<WaterVolume>(); var terrains = WorldBuilderAuthoringAssets.FindTerrains(_definition).Cast<Terrain>().ToArray(); volume.Initialize(body, Array.Empty<WaterExclusion>(), terrains);
                Query.volumes = Query.volumes.Concat(new[] { volume }).ToArray();
                Assert.That(Query.TrySample(new Vector3(64, 7, 60), out var sample)); Assert.That(sample.BodyId, Is.EqualTo(body.guid), "같은 우선순위에서는 좁은 실제 포함 수역 우선");
                Query.volumes = Query.volumes.Reverse().ToArray(); Assert.That(Query.TrySample(new Vector3(64, 7, 60), out var reverse)); Assert.That(reverse.BodyId, Is.EqualTo(sample.BodyId));
                Assert.That(Query.TryRaycastDown(new Vector3(64, 100, 60), 100, out var ray)); Assert.That(ray.BodyId, Is.EqualTo(sample.BodyId));
                body.priority = -1; volume.Initialize(body, Array.Empty<WaterExclusion>(), terrains); Assert.That(Query.TrySample(new Vector3(64, 7, 60), out sample)); Assert.That(sample.BodyId, Is.EqualTo(Source.bodies[0].waterGuid));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void PlanningAndWaterOwnerGuidCollisionCannotDeleteExistingPlanOperations()
        {
            string before = JsonUtility.ToJson(_definition); var alias = ScriptableObject.CreateInstance<WaterSystemDefinition>();
            alias.systemGuid = _definition.Planning.planGuid; alias.worldGuid = _definition.WorldGuid;
            try
            {
                Assert.Throws<InvalidOperationException>(() => WorldWaterAuthoring.Evaluate(_definition, alias));
                Assert.That(Assert.Throws<InvalidOperationException>(() => _definition.SetWaterSystem(alias)).Message, Does.Contain("소유자 GUID"));
                Assert.That(JsonUtility.ToJson(_definition), Is.EqualTo(before)); AssertApplied();
            }
            finally { UnityEngine.Object.DestroyImmediate(alias); }
        }
    }
}
