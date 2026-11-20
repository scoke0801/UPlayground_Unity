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
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class EcologyAuthoringTests
    {
        private string _worldFolder, _dataFolder;
        private WorldDefinition _definition;
        private WorldEcology _source;
        private WorldEcologyPreview Preview => UnityEngine.Object.FindFirstObjectByType<WorldEcologyPreview>();
        private WorldTerrainStateAdapter Terrain => new(_definition.Profile, WorldBuilderAuthoringAssets.FindTerrains(_definition));

        [SetUp]
        public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string id = Guid.NewGuid().ToString("N");
            _worldFolder = "Assets/WorldBuilder/Worlds/" + id;
            _dataFolder = "Assets/10.Datas/WorldBuilder/Test_" + id;
            _definition = WorldWaterValidationFixture.Create(id, _worldFolder, _dataFolder, false, NewSceneMode.Single);
            _source = WorldEcologyValidationFixture.CreateInput(_definition, _dataFolder, false);
        }

        [TearDown]
        public void Cleanup()
        {
            WorldEcologyAuthoring.FailureInjection = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (_worldFolder != null) AssetDatabase.DeleteAsset(_worldFolder);
            if (_dataFolder != null) AssetDatabase.DeleteAsset(_dataFolder);
        }

        [Test]
        public void CutsAcrossNativeTileBoundaryPreserveOtherCaveAndManualCutOnMoveDeleteUndoAndReopen()
        {
            var cave = _source.caves[0];
            foreach (var module in cave.modules) module.position += new Vector3(0, 0, .25f);
            var copy = JsonUtility.FromJson<CaveLayout>(JsonUtility.ToJson(cave));
            copy.caveGuid = Guid.NewGuid().ToString("N");
            for (int i = 0; i < copy.modules.Count; i++)
            {
                var module = copy.modules[i];
                string old = module.moduleId;
                module.moduleId = Guid.NewGuid().ToString("N");
                module.surfaceId = Guid.NewGuid().ToString("N");
                module.preset = cave.modules[i].preset;
                module.position += new Vector3(0, 0, 6.2f);
                foreach (var connection in copy.connections)
                {
                    if (connection.moduleA == old) connection.moduleA = module.moduleId;
                    if (connection.moduleB == old) connection.moduleB = module.moduleId;
                }
            }
            _source.caves.Add(copy);
            using (var session = new WorldTerrainAuthoringSession(_definition, Terrain.Terrains))
                session.AddOperation(new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"),
                    kind = TerrainOperationKind.HoleCut, x = 50, z = 14, width = 1, height = 1, stage = 50, value = 1 });
            string revision = _definition.SourceRevision, water = _definition.WaterSystem.AppliedHash;
            WorldEcologyAuthoring.Regenerate(_definition, _source);
            AssertHole(50, 14); AssertHole(50, 18); AssertHole(78, 16);
            string both = Terrain.Read().ComputeHash();
            var secondRoot = Preview.GetComponentsInChildren<WorldCaveGroup>().Single(g => g.caveGuid == copy.caveGuid);
            WorldEcologyAuthoring.Move(_definition, _source, cave.caveGuid, new Vector3(0, 0, -8));
            Assert.That(Preview.GetComponentsInChildren<WorldCaveGroup>().Single(g => g.caveGuid == copy.caveGuid), Is.SameAs(secondRoot));
            AssertHole(50, 14); AssertHole(50, 18); AssertHole(78, 8);
            Assert.That(Terrain.Read().GetSurface(16, 78), Is.True);
            Undo.PerformUndo();
            Assert.That(Terrain.Read().ComputeHash(), Is.EqualTo(both));
            Undo.PerformRedo();
            AssertHole(78, 8);
            WorldEcologyAuthoring.Delete(_definition, _source, cave.caveGuid);
            AssertHole(50, 14); AssertHole(50, 18);
            Assert.That(Terrain.Read().GetSurface(8, 78), Is.True);
            Assert.That(_definition.WaterSystem.AppliedHash, Is.EqualTo(water));
            Assert.That(_definition.SourceRevision, Is.EqualTo(revision));
            string path = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            AssertHole(50, 14); AssertHole(50, 18);
            Assert.That(Preview.modules.Length, Is.EqualTo(3));
        }

        [TestCase("AfterAssets")]
        [TestCase("AfterTerrain")]
        [TestCase("AfterScene")]
        [TestCase("AfterSceneIo")]
        [TestCase("DuringCutWrite")]
        public void InjectedFailurePreservesAppliedTerrainBiomesTreesDetailsAndScene(string stage)
        {
            WorldEcologyAuthoring.Regenerate(_definition, _source);
            string terrain = Terrain.Read().ComputeHash(), ecology = JsonUtility.ToJson(_source), data = JsonUtility.ToJson(_definition);
            string output = Preview.terrainOutputHash, hierarchy = Preview.hierarchyHash;
            var scene = Preview.gameObject.scene;
            string cuts = _worldFolder + "/Authoring/CaveCuts";
            var cutFiles = Directory.GetFiles(cuts).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            int waterCount = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaterVolume>()).Count();
            WorldEcologyAuthoring.FailureInjection = point =>
            {
                if (stage == "DuringCutWrite" && point == stage)
                {
                    string pending = Directory.GetFiles(cuts, "*.tmp").Single();
                    using var _ = new FileStream(pending, FileMode.Open, FileAccess.Write, FileShare.None);
                    Assert.Fail("HoleCut 임시 파일의 쓰기 잠금을 우회했습니다.");
                }
                if (stage == "AfterSceneIo")
                {
                    if (point != "AfterScene") return;
                    string sourcePath = AssetDatabase.GetAssetPath(_source);
                    Assert.That(File.Exists(sourcePath), Is.True);
                    // 이미 존재하는 파일은 CreateNew에서 실제 디스크 I/O 예외를 반환한다.
                    using var _ = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write);
                    Assert.Fail("기존 원본 파일의 배타적 생성이 성공했습니다.");
                }
                if (point == stage) throw new IOException(point);
            };
            var offset = new Vector3(0, 0, stage == "DuringCutWrite" ? .25f : 6);
            Assert.Throws<IOException>(() => WorldEcologyAuthoring.Move(_definition, _source, _source.caves[0].caveGuid, offset));
            WorldEcologyAuthoring.FailureInjection = null;
            Assert.That(Terrain.Read().ComputeHash(), Is.EqualTo(terrain));
            Assert.That(JsonUtility.ToJson(_source), Is.EqualTo(ecology));
            Assert.That(JsonUtility.ToJson(_definition), Is.EqualTo(data));
            Assert.That(Preview.hierarchyHash, Is.EqualTo(hierarchy));
            Assert.That(Directory.GetFiles(cuts).OrderBy(path => path, StringComparer.Ordinal), Is.EqualTo(cutFiles));
            Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaterVolume>()).Count(), Is.EqualTo(waterCount));
            WorldEcologyAuthoring.Regenerate(_definition, _source);
            Assert.That(Preview.terrainOutputHash, Is.EqualTo(output));
            if (stage == "DuringCutWrite")
                Assert.DoesNotThrow(() => WorldEcologyAuthoring.Move(_definition, _source, _source.caves[0].caveGuid, offset));
        }

        [Test]
        public void OneCaveCutWritesBothNativeTilesAndRestoresTheSharedEdge()
        {
            var cave = _source.caves.Single();
            var entrance = cave.modules[0];
            var cap = cave.modules[1];
            entrance.position = new Vector3(64, 1, 18);
            entrance.yaw = 0;
            cap.position = new Vector3(64, 1, 25.5f);
            cap.yaw = 0;
            cap.preset = _source.caveCatalog.Single(p => p.kind == CaveModuleKind.EndCap);
            cave.modules.RemoveAt(2);
            cave.connections.RemoveAt(1);
            WorldEcologyAuthoring.Regenerate(_definition, _source);
            AssertHole(63, 14);
            AssertHole(64, 14);
            WorldEcologyAuthoring.Delete(_definition, _source, cave.caveGuid);
            Assert.That(Terrain.Read().GetSurface(14, 63), Is.True);
            Assert.That(Terrain.Read().GetSurface(14, 64), Is.True);
            Undo.PerformUndo();
            AssertHole(63, 14);
            AssertHole(64, 14);
        }

        [Test]
        public void NativeLayersAndVegetationAreDeterministicAndManualEditsAreProtected()
        {
            // 작은 검증 월드에서 12m 간격 P03 소품이 모든 나무 후보의 간격을 막지 않도록 한다.
            // 기존 Pin 및 P04/P05 예약은 다시 평가된다.
            var props = _definition.PlacementRules.Single();
            props.isEnabled = false;
            EditorUtility.SetDirty(props);
            AssetDatabase.SaveAssetIfDirty(props);
            WorldEcologyAuthoring.Regenerate(_definition, _source);
            string output = Preview.terrainOutputHash;
            var ids = Preview.trees.Select(t => t.id).ToArray();
            Assert.That(ids.Length, Is.GreaterThan(0));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
            WorldEcologyAuthoring.Regenerate(_definition, _source, new[] { 3, 2, 0, 1 });
            Assert.That(Preview.terrainOutputHash, Is.EqualTo(output));
            Assert.That(Preview.trees.Select(t => t.id), Is.EqualTo(ids));
            var tiles = Terrain.Terrains;
            var a = tiles[0, 0].terrainData;
            var b = tiles[0, 1].terrainData;
            Assert.That(a.terrainLayers, Is.EqualTo(b.terrainLayers));
            var west = a.GetAlphamaps(0, 0, a.alphamapWidth, a.alphamapHeight);
            var east = b.GetAlphamaps(0, 0, b.alphamapWidth, b.alphamapHeight);
            for (int z = 0; z < west.GetLength(0); z++)
            for (int x = 0; x < west.GetLength(1); x++)
            {
                float sum = 0;
                for (int i = 0; i < west.GetLength(2); i++)
                {
                    sum += west[z, x, i];
                    if (x == west.GetLength(1) - 1) Assert.That(west[z, x, i], Is.EqualTo(east[z, 0, i]));
                }
                Assert.That(sum, Is.EqualTo(1));
            }
            int details = 0;
            foreach (var tile in tiles)
                for (int i = 0; i < tile.terrainData.detailPrototypes.Length; i++)
                    foreach (int value in tile.terrainData.GetDetailLayer(0, 0, 64, 64, i)) details += value;
            Assert.That(details, Is.GreaterThan(0));
            Undo.PerformUndo();
            Assert.That(Preview.terrainOutputHash, Is.EqualTo(output));
            Undo.PerformRedo();
            Assert.That(Preview.terrainOutputHash, Is.EqualTo(output));
            var manual = new GameObject("수동 소품");
            manual.transform.SetParent(Preview.transform);
            Assert.Throws<InvalidOperationException>(() => WorldEcologyAuthoring.Regenerate(_definition, _source));
            Assert.That(manual, Is.Not.Null);
        }

        [Test]
        public void FloorUsesCaveColliderAndConflictsLeaveOwnedOutputIntact()
        {
            WorldEcologyAuthoring.Regenerate(_definition, _source);
            var floor = _source.caves[0].modules[1];
            Physics.SyncTransforms();
            Assert.That(Preview.TryGetFloor(floor.surfaceId, new Vector2(64, 15), out Vector3 support), Is.True);
            Assert.That(support.y, Is.EqualTo(1).Within(.01));
            Assert.That(Terrain.Read().GetHeight(15, 64), Is.GreaterThan(8));
            Assert.That(Preview.TryGetFloor("terrain", new Vector2(64, 15), out _), Is.False);
            string terrain = Terrain.Read().ComputeHash();
            Assert.Throws<CaveConflict>(() => WorldEcologyAuthoring.Move(_definition, _source, _source.caves[0].caveGuid, Vector3.up * 8));
            Assert.That(Terrain.Read().ComputeHash(), Is.EqualTo(terrain));
        }

        private void AssertHole(int x, int z)
        {
            var grid = Terrain.Terrains;
            int cells = _definition.Profile.TileCells;
            Assert.That(grid[z / cells, x / cells].terrainData.GetHoles(x % cells, z % cells, 1, 1)[0, 0], Is.False, x + "," + z);
        }
    }
}

