using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class TerrainAuthoringSessionTests
    {
        private string _id, _folder, _export;
        private UnityEngine.SceneManagement.Scene _scene;
        private WorldDefinition _definition;
        private WorldTerrainAuthoringSession _session;
        private Terrain[,] _terrains;
        private string _sourceHash;

        [SetUp]
        public void SetUp()
        {
            _id = Guid.NewGuid().ToString("N");
            _folder = "Assets/WorldBuilder/Worlds/" + _id;
            WorldBuilderAuthoringAssets.EnsureFolder(_folder + "/Terrain");
            var profile = ScriptableObject.CreateInstance<WorldProfile>();
            profile.Configure(64, 65, 0, 100, 32);
            AssetDatabase.CreateAsset(profile, _folder + "/Profile.asset");
            var samples = new ushort[65 * 65];
            for (int z = 0; z < 65; z++)
            for (int x = 0; x < 65; x++) samples[z * 65 + x] = (ushort)(10000 + (x + z) % 1000);
            var field = new HeightField(65, 65, samples);
            var bytes = Raw16HeightmapCodec.Encode(field, HeightmapRowOrigin.South);
            File.WriteAllBytes(_folder + "/Source.bytes", bytes);
            AssetDatabase.ImportAsset(_folder + "/Source.bytes", ImportAssetOptions.ForceSynchronousImport);
            var source = AssetDatabase.LoadAssetAtPath<TextAsset>(_folder + "/Source.bytes");
            _sourceHash = WorldDefinition.HashBytes(bytes);
            _scene = EditorSceneManager.NewPreviewScene();
            _terrains = WorldBuilderSampleImporter.CreateTerrains(field, profile, _scene, _folder + "/Terrain");
            _definition = ScriptableObject.CreateInstance<WorldDefinition>();
            _definition.Initialize(_id, profile, source, "");
            AssetDatabase.CreateAsset(_definition, _folder + "/Definition.asset");
            _session = new WorldTerrainAuthoringSession(_definition, _terrains);
            Undo.ClearUndo(_definition);
            _export = "output/WorldBuilder/P02/tests/" + _id + ".raw";
        }

        [TearDown]
        public void TearDown()
        {
            _session?.Dispose();
            if (_definition != null) Undo.ClearUndo(_definition);
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
            if (_export != null && File.Exists(_export)) File.Delete(_export);
            if (_export != null && File.Exists(_export + ".json")) File.Delete(_export + ".json");
        }

        [Test]
        public void T05_T08_SculptCornerCaptureUpdatesFourTilesAndSurvivesRebuild()
        {
            _session.BeginSculpt();
            float before = _session.Terrain.Read().GetHeight(32, 32);
            EditHeight(_terrains[0, 0], 32, 32, .01f);
            Assert.Throws<InvalidDataException>(() => _session.EnsureReadyForBake());
            _session.CaptureSculpt();
            Assert.That(_definition.Operations.Count, Is.EqualTo(1));
            Assert.That(_definition.Operations[0].width, Is.EqualTo(1));
            float captured = _session.Terrain.Read().GetHeight(32, 32);
            Assert.That(captured - before, Is.EqualTo(1).Within(.004));
            string hash = _session.Terrain.Read().ComputeHash();
            for (int i = 0; i < 3; i++) { _session.Regenerate(); Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(hash)); }
            float reference = _terrains[0, 0].terrainData.GetHeights(32, 32, 1, 1)[0, 0];
            Assert.That(_terrains[0, 1].terrainData.GetHeights(0, 32, 1, 1)[0, 0], Is.EqualTo(reference));
            Assert.That(_terrains[1, 0].terrainData.GetHeights(32, 0, 1, 1)[0, 0], Is.EqualTo(reference));
            Assert.That(_terrains[1, 1].terrainData.GetHeights(0, 0, 1, 1)[0, 0], Is.EqualTo(reference));
            AssertSourcePreserved();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void T08_RawSessionCapturesAgainstExportAndDoesNotDoubleApply(bool absolute)
        {
            _session.AddOperation(Command(TerrainOperationKind.AddDelta, 5, 30));
            _session.ExportRaw(_export);
            Assert.That(File.ReadAllText(_export + ".json"), Does.Contain(_definition.WorldGuid));
            var bytes = File.ReadAllBytes(_export);
            int offset = 2 * (40 * 65 + 40);
            int original = bytes[offset] | bytes[offset + 1] << 8;
            int edited = original + 1000;
            bytes[offset] = (byte)edited;
            bytes[offset + 1] = (byte)(edited >> 8);
            File.WriteAllBytes(_export, bytes);
            _session.ImportRaw(_export, absolute);
            Assert.That(_definition.Operations.Count, Is.EqualTo(2));
            var patch = _definition.Operations[1].ToCore(_definition.SourceRevision);
            Assert.That(patch.GetValue(0), Is.EqualTo((absolute ? edited : 1000) / 65535f * 100).Within(.00001));
            string hash = _session.Terrain.Read().ComputeHash();
            for (int i = 0; i < 3; i++) { _session.Regenerate(); Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(hash)); }
            _session.EnsureReadyForBake();
            AssertSourcePreserved();
        }

        [Test]
        public void T09_UntrackedEditsBlockRebuildAndCaptureOrRevertResolvesThem()
        {
            EditHeight(_terrains[0, 0], 4, 4, .02f);
            Assert.Throws<InvalidOperationException>(() => _session.Regenerate());
            Assert.Throws<InvalidOperationException>(() => _session.EnsureReadyForBake());
            _session.CaptureSculpt();
            _session.EnsureReadyForBake();
            string captured = _session.Terrain.Read().ComputeHash();
            EditHeight(_terrains[0, 0], 4, 4, .03f);
            Assert.Throws<InvalidOperationException>(() => _session.EnsureReadyForBake());
            _session.RevertUncaptured();
            Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(captured));
            _session.EnsureReadyForBake();
        }

        [Test]
        public void T08_SourceOrStackChangeConflictsWithActiveSession()
        {
            _session.BeginSculpt();
            _session.AddOperation(Command(TerrainOperationKind.Flatten, 30, 30));
            Assert.Throws<InvalidOperationException>(() => _session.CaptureSculpt());
            Assert.That(_definition.ActiveSession, Is.Not.Null);
            _session.RevertUncaptured();
            _session.BeginSculpt();
            _definition.Profile.Configure(256, 65, 0, 100, 32);
            Assert.Throws<InvalidOperationException>(() => _session.CaptureSculpt());
            AssertSourcePreserved();
        }

        [TestCase(TerrainOperationKind.AddDelta)]
        [TestCase(TerrainOperationKind.HoleCut)]
        public void T10_HeightAndHoleUndoRedoTwentyCyclesPreservesValuesAndReferences(TerrainOperationKind kind)
        {
            string before = _session.Terrain.Read().ComputeHash();
            string dataGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_terrains[1, 1].terrainData));
            _session.AddOperation(Command(kind, .5f, 40));
            string after = _session.Terrain.Read().ComputeHash();
            Assert.That(after, Is.Not.EqualTo(before));
            for (int i = 0; i < 20; i++)
            {
                Undo.PerformUndo();
                Assert.That(_session.LastError, Is.Null);
                Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(before), "Undo " + i);
                Assert.That(_definition.Operations.Count, Is.Zero);
                Undo.PerformRedo();
                Assert.That(_session.LastError, Is.Null);
                Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(after), "Redo " + i);
                Assert.That(_definition.Operations.Count, Is.EqualTo(1));
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_terrains[1, 1].terrainData)), Is.EqualTo(dataGuid));
                Assert.That(_terrains[1, 1].GetComponent<TerrainCollider>().terrainData, Is.EqualTo(_terrains[1, 1].terrainData));
            }
            AssertSourcePreserved();
        }

        [Test]
        public void InvalidCommandLeavesAuthoringAndNativeTerrainUntouched()
        {
            string before = _session.Terrain.Read().ComputeHash();
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.AddOperation(Command(TerrainOperationKind.SetAbsolute, 1000, 40)));
            Assert.That(_definition.Operations.Count, Is.Zero);
            Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(before));
        }

        [Test]
        public void SessionAndPatchesSurviveAssetReloadAndRebinding()
        {
            _session.BeginSculpt();
            string guid = _definition.ActiveSession.sessionGuid;
            EditHeight(_terrains[1, 1], 4, 4, .01f);
            _session.Dispose();
            var loaded = AssetDatabase.LoadAssetAtPath<WorldDefinition>(_folder + "/Definition.asset");
            Assert.That(loaded.ActiveSession.sessionGuid, Is.EqualTo(guid));
            _session = new WorldTerrainAuthoringSession(loaded, _terrains);
            _session.CaptureSculpt();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(loaded.Operations[0].patch), ImportAssetOptions.ForceSynchronousImport);
            _session.EnsureReadyForBake();
            Assert.That(loaded.Operations[0].patch, Is.Not.Null);
            Assert.That(loaded.ActiveSession, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureAppendsAfterHigherPriorityAndLaterStageHeightCommands(bool raw)
        {
            var command = Command(TerrainOperationKind.SetAbsolute, 20, 80);
            command.priority = 7;
            _session.AddOperation(command);
            if (raw)
            {
                _session.ExportRaw(_export);
                var bytes = File.ReadAllBytes(_export);
                int offset = 2 * (33 * 65 + 33), value = 14000;
                bytes[offset] = (byte)value; bytes[offset + 1] = (byte)(value >> 8);
                File.WriteAllBytes(_export, bytes);
                _session.ImportRaw(_export, true);
                Assert.That(_session.Terrain.Read().GetHeight(33, 33), Is.EqualTo(value / 65535f * 100).Within(.004));
            }
            else
            {
                _session.BeginSculpt();
                EditHeight(_terrains[1, 1], 1, 1, .01f);
                _session.CaptureSculpt();
                Assert.That(_session.Terrain.Read().GetHeight(33, 33), Is.EqualTo(21).Within(.004));
            }
            _session.Regenerate();
            Assert.That(_definition.Operations[1].stage, Is.GreaterThanOrEqualTo(command.stage));
            Assert.That(_definition.Operations[1].priority, Is.GreaterThanOrEqualTo(command.priority));
        }

        [Test]
        public void RawMetadataFromOtherWorldOrSessionIsRejectedWithoutMutation()
        {
            _session.ExportRaw(_export);
            string original = File.ReadAllText(_export + ".json"), hash = _session.Terrain.Read().ComputeHash();
            File.WriteAllText(_export + ".json", original.Replace(_id, Guid.NewGuid().ToString("N")));
            Assert.Throws<InvalidDataException>(() => _session.ImportRaw(_export, false));
            File.WriteAllText(_export + ".json", original.Replace(_definition.ActiveSession.sessionGuid, Guid.NewGuid().ToString("N")));
            Assert.Throws<InvalidDataException>(() => _session.ImportRaw(_export, false));
            Assert.That(_definition.Operations.Count, Is.Zero);
            Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(hash));
            Assert.That(_definition.ActiveSession, Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ForeignUndoDoesNotOverwriteTerrainOrDirtyThisWorld(bool sculptActive)
        {
            if (sculptActive) _session.BeginSculpt();
            var foreign = ScriptableObject.CreateInstance<WorldProfile>();
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(foreign, "다른 도구의 설정");
                foreign.Configure(256, 257, 0, 100, 32);
                Undo.FlushUndoRecordObjects();
                EditHeight(_terrains[0, 0], 4, 4, .01f);
                string edited = _session.Terrain.Read().ComputeHash();
                Undo.PerformUndo();
                Assert.That(_session.LastError, Is.Null);
                Assert.That(_session.Terrain.Read().ComputeHash(), Is.EqualTo(edited));
                Assert.That(_definition.DirtyLayers, Is.EqualTo(WorldDirtyFlags.None));
                Assert.That(foreign.WorldSize, Is.EqualTo(2048));
            }
            finally { Undo.ClearUndo(foreign); UnityEngine.Object.DestroyImmediate(foreign); }
        }

        [Test]
        public void NativeBrushUndoAndRedoDuringSculptDoNotRunAuthoringRebuild()
        {
            _session.BeginSculpt();
            var data = _terrains[0, 0].terrainData;
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(data, "Unity 브러시 수정");
                EditHeight(_terrains[0, 0], 4, 4, .01f);
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(_session.LastError, Is.Null);
                Undo.PerformRedo();
                Assert.That(_session.LastError, Is.Null);
                Assert.That(_definition.ActiveSession, Is.Not.Null);
                Assert.That(_definition.DirtyLayers, Is.EqualTo(WorldDirtyFlags.None));
                Assert.That(_definition.Operations.Count, Is.Zero);
            }
            finally { Undo.ClearUndo(data); }
        }

        private static TerrainOperationRecord Command(TerrainOperationKind kind, float value, int stage) => new TerrainOperationRecord
        {
            operationGuid = Guid.NewGuid().ToString("N"), kind = kind, value = value, x = 32, z = 32, width = 8, height = 8, stage = stage
        };

        private static void EditHeight(Terrain terrain, int x, int z, float delta)
        {
            var heights = terrain.terrainData.GetHeights(x, z, 1, 1);
            heights[0, 0] += delta;
            terrain.terrainData.SetHeightsDelayLOD(x, z, heights);
            terrain.terrainData.SyncHeightmap();
        }

        private void AssertSourcePreserved() => Assert.That(WorldDefinition.HashBytes(AssetDatabase.LoadAssetAtPath<TextAsset>(_folder + "/Source.bytes").bytes), Is.EqualTo(_sourceHash));
    }
}
