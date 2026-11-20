using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldPlanningAuthoringTests
    {
        private WorldDefinition _definition;
        private string _folder, _data;
        [SetUp] public void Setup()
        {
            string id = Guid.NewGuid().ToString("N"); _folder = "Assets/WorldBuilder/Worlds/" + id; _data = "Assets/10.Datas/WorldBuilder/Test_" + id;
            _definition = WorldPlanningValidationFixture.Create(id, _folder, _data, false, NewSceneMode.Single);
        }
        [TearDown] public void Cleanup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (_folder != null) AssetDatabase.DeleteAsset(_folder);
            if (_data != null) AssetDatabase.DeleteAsset(_data);
        }
        private void AssertPreview()
        {
            var plan = _definition.Planning;
            var root = UnityEngine.Object.FindObjectsByType<WorldPlanningPreview>(FindObjectsSortMode.None).Single(p => p.definition == _definition);
            Assert.That(root.appliedHash, Is.EqualTo(plan.AppliedHash));
            Assert.That(root.GetComponentsInChildren<WorldPlanningInstance>().Length, Is.EqualTo(plan.AppliedStructures.Count));
            foreach (var instance in root.GetComponentsInChildren<WorldPlanningInstance>())
            {
                var expected = plan.AppliedStructures.Single(p => p.id == instance.record.id);
                Assert.That(Vector3.Distance(instance.transform.position, new Vector3((float)expected.x, (float)expected.y, (float)expected.z)), Is.LessThan(.001f));
                Assert.That(Quaternion.Angle(instance.transform.rotation, Quaternion.Euler(0, (float)expected.yaw, 0)), Is.LessThan(.001f));
            }
            Assert.That(root.GetComponentsInChildren<WorldRoadPreview>().Length, Is.EqualTo(plan.AppliedRoadGuids.Count));
            Assert.That(WorldPlanningAuthoring.Evaluate(_definition, plan).ComputeHash(), Is.EqualTo(plan.AppliedHash));
            Assert.That(new WorldTerrainStateAdapter(_definition.Profile, WorldBuilderAuthoringAssets.FindTerrains(_definition)).Read().ComputeHash(), Is.EqualTo(_definition.AppliedTerrainHash));
        }
        [Test] public void T13_MovePoiUndoRedoPreservesPinsSourceAndCanonicalResult()
        {
            var plan = _definition.Planning; string before = plan.AppliedHash, source = _definition.SourceRevision;
            var pin = _definition.Placements.Single(p => p.isFixed).Clone(); var anchor = plan.pois[0].anchor;
            Assert.That(WorldPlanningAuthoring.ProposePoiElevation(_definition, plan.pois[0].guid, anchor).y, Is.EqualTo(10).Within(.02)); Assert.That(plan.AppliedHash, Is.EqualTo(before));
            WorldPlanningAuthoring.MovePoi(_definition, plan.pois[0].guid, new PlanPoint(anchor.x + 4, anchor.y, anchor.z));
            string after = plan.AppliedHash; Assert.That(after, Is.Not.EqualTo(before));
            for (int i = 0; i < 20; i++)
            {
                Undo.PerformUndo(); Assert.That(plan.AppliedHash, Is.EqualTo(before)); AssertPreview();
                Undo.PerformRedo(); Assert.That(plan.AppliedHash, Is.EqualTo(after)); AssertPreview();
            }
            Assert.That(_definition.SourceRevision, Is.EqualTo(source));
            Assert.That(_definition.Placements.Single(p => p.id == pin.id).x, Is.EqualTo(pin.x));
            WorldPlanningAuthoring.Regenerate(_definition, plan); Assert.That(plan.AppliedHash, Is.EqualTo(after)); AssertPreview();
        }
        [Test] public void T13_FirstApplicationUndoRestoresTerrainWithoutPlanningRoot()
        {
            var plan = _definition.Planning; string hash = plan.AppliedHash;
            Undo.PerformUndo(); Assert.That(_definition.Planning, Is.Null); Assert.That(_definition.Operations, Is.Empty);
            Assert.That(UnityEngine.Object.FindObjectsByType<WorldPlanningPreview>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(new WorldTerrainStateAdapter(_definition.Profile, WorldBuilderAuthoringAssets.FindTerrains(_definition)).Read().ComputeHash(), Is.EqualTo(_definition.AppliedTerrainHash));
            Undo.PerformRedo(); Assert.That(_definition.Planning, Is.EqualTo(plan)); Assert.That(plan.AppliedHash, Is.EqualTo(hash)); AssertPreview();
        }
        [Test] public void T13_OpenTerrainSessionRefreshesCacheAfterPlanningTransaction()
        {
            using (var session = new WorldTerrainAuthoringSession(_definition, WorldBuilderAuthoringAssets.FindTerrains(_definition)))
            {
                var plan = _definition.Planning; var anchor = plan.pois[0].anchor;
                WorldPlanningAuthoring.MovePoi(_definition, plan.pois[0].guid, new PlanPoint(anchor.x + 4, anchor.y, anchor.z));
                session.AddOperation(new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"), kind = TerrainOperationKind.AddDelta, x = 0, z = 120, width = 3, height = 3, value = .25f });
                string full = WorldPlacementAuthoring.Evaluate(_definition, TerrainOperationEvaluator.Build(_definition.ReadSource(), _definition.Profile.CreateGrid(), _definition.ReadOperations())).ComputeHash();
                Assert.That(_definition.PlacementHash, Is.EqualTo(full)); Assert.That(session.Terrain.Read().ComputeHash(), Is.EqualTo(_definition.AppliedTerrainHash));
                Undo.PerformUndo(); AssertPreview(); Undo.PerformRedo();
                Assert.That(session.Terrain.Read().ComputeHash(), Is.EqualTo(_definition.AppliedTerrainHash));
            }
        }
        [Test] public void T13_InvalidMoveRestoresEntireOperationAndClearSupportsUndo()
        {
            var plan = _definition.Planning; string before = plan.AppliedHash, placements = _definition.PlacementHash;
            var anchor = plan.pois[0].anchor; int operations = _definition.Operations.Count;
            Assert.Throws<InvalidOperationException>(() => WorldPlanningAuthoring.MovePoi(_definition, plan.pois[0].guid, new PlanPoint(64, 11, 64)));
            Assert.That(plan.pois[0].anchor.x, Is.EqualTo(anchor.x)); Assert.That(plan.AppliedHash, Is.EqualTo(before));
            Assert.That(_definition.PlacementHash, Is.EqualTo(placements)); Assert.That(_definition.Operations.Count, Is.EqualTo(operations)); AssertPreview();
            WorldPlanningAuthoring.ClearGeneratedPlan(_definition); Assert.That(plan.AppliedStructures, Is.Empty); AssertPreview();
            for (int cycle = 0; cycle < 20; cycle++)
            {
                Undo.PerformUndo(); Assert.That(plan.AppliedHash, Is.EqualTo(before)); AssertPreview();
                Undo.PerformRedo(); Assert.That(plan.AppliedStructures, Is.Empty); AssertPreview();
            }
        }
        [Test] public void T13_DownstreamPlacementFailureRestoresPlanTerrainAndUserChild()
        {
            var plan = _definition.Planning; string hash = plan.AppliedHash, terrain = _definition.AppliedTerrainHash;
            var parent = UnityEngine.Object.FindFirstObjectByType<WorldPlacementInstance>(); var child = new GameObject("보존할 수동 자식"); child.transform.SetParent(parent.transform, false);
            var anchor = plan.pois[0].anchor;
            Assert.Throws<InvalidOperationException>(() => WorldPlanningAuthoring.MovePoi(_definition, plan.pois[0].guid, new PlanPoint(anchor.x + 4, anchor.y, anchor.z)));
            Assert.That(plan.AppliedHash, Is.EqualTo(hash)); Assert.That(_definition.AppliedTerrainHash, Is.EqualTo(terrain)); Assert.That(child != null); AssertPreview();
        }
        [Test] public void T13_SplineCaptureLocksAndSaveReloadPreservesMeshAndPlan()
        {
            var plan = _definition.Planning; var preview = UnityEngine.Object.FindFirstObjectByType<WorldRoadPreview>();
            var container = preview.GetComponent<SplineContainer>(); var knot = container.Spline[1]; knot.Position.y += .01f; container.Spline.SetKnot(1, knot);
            Assert.Throws<InvalidOperationException>(() => WorldPlanningAuthoring.Regenerate(_definition, plan));
            WorldPlanningAuthoring.CaptureSpline(_definition, preview.roadGuid); Assert.That(plan.roads[0].isLocked); AssertPreview();
            string hash = plan.AppliedHash, path = SceneManager.GetActiveScene().path, definition = AssetDatabase.GetAssetPath(_definition);
            Assert.That(EditorSceneManager.SaveScene(SceneManager.GetActiveScene())); EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            _definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(definition); WorldPlanningAuthoring.Regenerate(_definition, _definition.Planning);
            Assert.That(_definition.Planning.AppliedHash, Is.EqualTo(hash)); AssertPreview();
        }
        [Test] public void DerivedMeshStorePreservesExistingAssetOfDifferentType()
        {
            var chunk = new RoadChunk { TileX = 0, TileZ = 0, Triangles = new[] { 0, 1, 2 }, Vertices = new[] {
                new RoadVertex { Position = new PlanPoint(0, 0, 0), Normal = new PlanPoint(0, 1, 0), Tangent = new PlanPoint(1, 0, 0) },
                new RoadVertex { Position = new PlanPoint(1, 0, 0), Normal = new PlanPoint(0, 1, 0), Tangent = new PlanPoint(1, 0, 0) },
                new RoadVertex { Position = new PlanPoint(0, 0, 1), Normal = new PlanPoint(0, 1, 0), Tangent = new PlanPoint(1, 0, 0) } } };
            var store = typeof(WorldPlanningAuthoring).GetMethod("SaveMesh", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var created = new System.Collections.Generic.List<string>();
            var mesh = (Mesh)store.Invoke(null, new object[] { _definition, chunk, created }); string path = AssetDatabase.GetAssetPath(mesh);
            AssetDatabase.DeleteAsset(path); var other = ScriptableObject.CreateInstance<WorldProfile>(); AssetDatabase.CreateAsset(other, path);
            string guid = AssetDatabase.AssetPathToGUID(path), bytes = WorldDefinition.HashBytes(System.IO.File.ReadAllBytes(path));
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => store.Invoke(null, new object[] { _definition, chunk, created }));
            Assert.That(error.InnerException, Is.TypeOf<System.IO.IOException>()); Assert.That(AssetDatabase.LoadAssetAtPath<WorldProfile>(path), Is.EqualTo(other));
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid)); Assert.That(WorldDefinition.HashBytes(System.IO.File.ReadAllBytes(path)), Is.EqualTo(bytes));
        }
    }
}
