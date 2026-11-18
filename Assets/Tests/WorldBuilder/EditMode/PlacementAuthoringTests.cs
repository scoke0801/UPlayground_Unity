using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class PlacementAuthoringTests
    {
        private string _worldFolder, _dataFolder;
        private WorldDefinition _definition;
        private Scene _previous, _scene;
        [SetUp]
        public void Setup()
        {
            _previous = SceneManager.GetActiveScene(); string id = Guid.NewGuid().ToString("N");
            _worldFolder = "Assets/WorldBuilder/Worlds/" + id; _dataFolder = "Assets/10.Datas/WorldBuilder/Test_" + id;
            _definition = WorldPlacementValidationFixture.Create(id, _worldFolder, _dataFolder, 64, 65, 32, NewSceneMode.Single);
            _scene = SceneManager.GetActiveScene();
        }
        [TearDown]
        public void Cleanup()
        {
            if (_definition != null) Undo.ClearUndo(_definition);
            if (_scene.IsValid() && _scene.isLoaded)
            {
                if (SceneManager.sceneCount == 1) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else EditorSceneManager.CloseScene(_scene, true);
            }
            if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
            if (!string.IsNullOrEmpty(_worldFolder)) AssetDatabase.DeleteAsset(_worldFolder);
            if (!string.IsNullOrEmpty(_dataFolder)) AssetDatabase.DeleteAsset(_dataFolder);
        }
        [Test]
        public void T11_DeletePinAndTransformUndoRedoRestoreSceneAndData()
        {
            string before = _definition.PlacementHash;
            var target = _definition.Placements.First(p => !p.isFixed);
            WorldPlacementAuthoring.ApplyOverride(_definition, WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.Delete));
            string after = _definition.PlacementHash; Assert.That(after, Is.Not.EqualTo(before));
            for (int i = 0; i < 20; i++)
            {
                Undo.PerformUndo(); Assert.That(_definition.PlacementHash, Is.EqualTo(before)); AssertPreviewMatches();
                Undo.PerformRedo(); Assert.That(_definition.PlacementHash, Is.EqualTo(after)); AssertPreviewMatches();
            }
            var manual = _definition.Placements.First(p => p.isFixed && string.IsNullOrEmpty(p.lineage));
            var transform = WorldPlacementAuthoring.CreateTargetOverride(manual, PlacementOverrideKind.Transform);
            transform.placement.x += 1; transform.placement.yaw = 90; transform.placement.scaleY = 2;
            WorldPlacementAuthoring.ApplyOverride(_definition, transform);
            string transformed = _definition.PlacementHash;
            for (int i = 0; i < 20; i++)
            {
                Undo.PerformUndo(); Assert.That(_definition.PlacementHash, Is.EqualTo(after)); AssertPreviewMatches();
                Undo.PerformRedo(); Assert.That(_definition.PlacementHash, Is.EqualTo(transformed)); AssertPreviewMatches();
            }
            var pin = _definition.Placements.First(p => p.isFixed && !string.IsNullOrEmpty(p.lineage));
            WorldPlacementAuthoring.ChangeVersion(_definition, 7, 1);
            Assert.That(WorldPlacementAuthoring.LastResult.Count(PlacementRejection.OrphanOverride), Is.GreaterThanOrEqualTo(2));
            Assert.That(_definition.Placements.First(p => p.id == pin.id).x, Is.EqualTo(pin.x)); AssertPreviewMatches();
        }
        [Test]
        public void T07_P02DirtyTriggersPlacementAndPersistsSourceRevisionAndNewHash()
        {
            string sourceRevision = _definition.SourceRevision;
            var target = _definition.Placements.First(p => !p.isFixed);
            using (var terrain = new WorldTerrainAuthoringSession(_definition, WorldBuilderAuthoringAssets.FindTerrains(_definition)))
            {
                string before = _definition.PlacementHash;
                var command = new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"), kind = TerrainOperationKind.AddDelta,
                    x = Math.Max(0, (int)target.x - 2), z = Math.Max(0, (int)target.z - 2), width = 5, height = 5, value = .25f };
                terrain.AddOperation(command);
                Assert.That(_definition.PlacementHash, Is.Not.EqualTo(before));
                string full = WorldPlacementAuthoring.Evaluate(_definition, TerrainOperationEvaluator.Build(_definition.ReadSource(), _definition.Profile.CreateGrid(), _definition.ReadOperations())).ComputeHash();
                Assert.That(_definition.PlacementHash, Is.EqualTo(full));
                Assert.That((_definition.DirtyLayers & WorldDirtyFlags.Placement), Is.EqualTo(WorldDirtyFlags.None));
                string path = AssetDatabase.GetAssetPath(_definition);
                Assert.That(File.ReadAllText(path), Does.Contain(full));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAssetAtPath<WorldDefinition>(path).PlacementHash, Is.EqualTo(full));
                Assert.That(_definition.SourceRevision, Is.EqualTo(sourceRevision)); Assert.That(_definition.Operations.Count, Is.EqualTo(1));
                Undo.PerformUndo(); Assert.That(_definition.PlacementHash, Is.EqualTo(before)); AssertPreviewMatches();
                Undo.PerformRedo(); Assert.That(_definition.PlacementHash, Is.EqualTo(full)); AssertPreviewMatches();
            }
        }
        [Test]
        public void P02PlacementConflictAfterTerrainWriteRollsBackEntireTransaction()
        {
            var pin = _definition.Placements.First(p => p.isFixed && !string.IsNullOrEmpty(p.lineage));
            using (var terrain = new WorldTerrainAuthoringSession(_definition, WorldBuilderAuthoringAssets.FindTerrains(_definition)))
            {
                string height = terrain.Terrain.Read().ComputeHash(), placements = _definition.PlacementHash;
                int x = Math.Min(54, Math.Max(0, (int)pin.x - 5)), z = Math.Min(54, Math.Max(0, (int)pin.z - 5));
                Assert.Throws<InvalidOperationException>(() => terrain.AddOperation(new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"), kind = TerrainOperationKind.AddDelta, x = x, z = z, width = 11, height = 11, value = 10 }));
                Assert.That(terrain.Terrain.Read().ComputeHash(), Is.EqualTo(height)); Assert.That(_definition.PlacementHash, Is.EqualTo(placements));
                Assert.That(_definition.Operations, Is.Empty); Assert.That(WorldPlacementAuthoring.LastResult.HasErrors); AssertPreviewMatches();
            }
        }
        [Test]
        public void InvalidReplacementAndSeedWithoutEpochPreserveUndoSceneAndDefinition()
        {
            var target = _definition.Placements.First(p => !p.isFixed);
            string before = _definition.PlacementHash; int count = _definition.PlacementOverrides.Count;
            var edit = WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.ReplacePrefab); edit.prefabGuid = Guid.NewGuid().ToString("N");
            Assert.Throws<InvalidOperationException>(() => WorldPlacementAuthoring.ApplyOverride(_definition, edit));
            Assert.That(_definition.PlacementOverrides.Count, Is.EqualTo(count)); Assert.That(_definition.PlacementHash, Is.EqualTo(before));
            Assert.Throws<ArgumentException>(() => WorldPlacementAuthoring.ChangeVersion(_definition, 1, 0)); Assert.That(_definition.PlacementSeed, Is.Zero); AssertPreviewMatches();
        }
        [Test]
        public void LastTileOwnershipAndPreviewSurviveSaveReload()
        {
            var target = _definition.Placements.First(p => p.isFixed && string.IsNullOrEmpty(p.lineage));
            var edit = WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.Transform);
            edit.placement.x = 34; edit.placement.z = 34; edit.placement.scaleX = edit.placement.scaleY = edit.placement.scaleZ = .25;
            WorldPlacementAuthoring.ApplyOverride(_definition, edit);
            var moved = _definition.Placements.First(p => p.id == target.id); Assert.That(moved.ownerX, Is.EqualTo(1)); Assert.That(moved.ownerZ, Is.EqualTo(1));
            Assert.That(EditorSceneManager.SaveScene(_scene)); string path = _scene.path;
            string definitionPath = AssetDatabase.GetAssetPath(_definition);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single); SceneManager.SetActiveScene(_scene);
            _definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(definitionPath);
            AssertPreviewMatches(); WorldPlacementAuthoring.Regenerate(_definition); AssertPreviewMatches();
        }
        [Test]
        public void NestedPrefabEditsAndUnownedChildrenAreNotDestroyedByRegeneration()
        {
            var root = _scene.GetRootGameObjects().Select(go => go.GetComponent<WorldPlacementRoot>()).First(p => p != null);
            var extra = new GameObject("사용자 추가 소품"); extra.transform.SetParent(root.transform);
            Assert.Throws<InvalidOperationException>(() => WorldPlacementAuthoring.Regenerate(_definition)); Assert.That(extra != null);
            UnityEngine.Object.DestroyImmediate(extra);
            var instance = root.GetComponentInChildren<WorldPlacementInstance>(); var child = instance.transform.GetChild(0);
            child.localPosition += Vector3.up; PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            Assert.Throws<InvalidOperationException>(() => WorldPlacementAuthoring.Regenerate(_definition)); Assert.That(child != null);
        }
        private void AssertPreviewMatches()
        {
            var instances = _scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<WorldPlacementInstance>(true)).Where(p => p.definition == _definition).ToArray();
            Assert.That(instances.Length, Is.EqualTo(_definition.Placements.Count));
            foreach (var p in _definition.Placements)
            {
                var instance = instances.Single(i => i.record.id == p.id);
                Assert.That(Vector3.Distance(instance.transform.position, new Vector3((float)p.x, (float)p.y, (float)p.z)), Is.LessThan(.001f));
                Assert.That(instance.record.sourceIdentity, Is.EqualTo(p.sourceIdentity));
            }
        }
    }
}
