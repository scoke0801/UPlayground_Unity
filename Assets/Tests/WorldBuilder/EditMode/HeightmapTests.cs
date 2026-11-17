using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class HeightmapTests
    {
        [Test]
        public void T01_OrientationFixtureHasCanonicalCorners()
        {
            WorldBuilderSampleImporter.ValidateOrientationFixture();
            var field = WorldBuilderSampleImporter.LoadRaw(WorldBuilderSampleImporter.OrientationRaw, 33);
            Assert.That(field[0, 0] / 65535.0, Is.EqualTo(.1).Within(1.0 / 65535));
            Assert.That(field[0, 32] / 65535.0, Is.EqualTo(.3).Within(1.0 / 65535));
            Assert.That(field[32, 0] / 65535.0, Is.EqualTo(.6).Within(1.0 / 65535));
            Assert.That(field[32, 32] / 65535.0, Is.EqualTo(.8).Within(1.0 / 65535));
        }

        [TestCase(HeightmapRowOrigin.South)]
        [TestCase(HeightmapRowOrigin.North)]
        public void T02_AllUshortValuesRoundtripExactly(HeightmapRowOrigin origin)
        {
            var samples = new ushort[65536];
            for (int i = 0; i < samples.Length; i++) samples[i] = (ushort)i;
            var field = new HeightField(256, 256, samples);
            var bytes = Raw16HeightmapCodec.Encode(field, origin);
            CollectionAssert.AreEqual(bytes, Raw16HeightmapCodec.Encode(Raw16HeightmapCodec.Decode(bytes, 256, 256, origin), origin));
            var normalized = WorldBuilderSampleImporter.ToNormalized(field);
            CollectionAssert.AreEqual(bytes, Raw16HeightmapCodec.Encode(Raw16HeightmapCodec.FromNormalized(normalized), origin));
        }

        [Test]
        public void T02_TechnicalFixtureChecksumRoundtripAndSplitJoin()
        {
            var bytes = File.ReadAllBytes(WorldBuilderSampleImporter.TechnicalRaw);
            var field = Raw16HeightmapCodec.Decode(bytes, 2049, 2049, HeightmapRowOrigin.South);
            var tiles = new HeightField[4, 4];
            for (int z = 0; z < 4; z++)
            for (int x = 0; x < 4; x++) tiles[z, x] = field.ExtractTile(x, z, 512);
            CollectionAssert.AreEqual(bytes, Raw16HeightmapCodec.Encode(HeightField.JoinTiles(tiles), HeightmapRowOrigin.South));
            for (int z = 0; z < 4; z++)
            for (int x = 0; x < 4; x++)
            {
                var fixture = File.ReadAllBytes(WorldBuilderSampleImporter.HandoffRoot + $"/core/samples/tiles/tile_{x:00}_{z:00}_513_r16le.raw");
                CollectionAssert.AreEqual(fixture, Raw16HeightmapCodec.Encode(tiles[z, x], HeightmapRowOrigin.South));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void T03_T04_UnityTerrainRoundtripAndNeighbors(bool regional)
        {
            var profile = ScriptableObject.CreateInstance<WorldProfile>();
            if (!regional) profile.Configure(2048, 2049, -80, 480, 512);
            var field = regional ? WorldBuilderSampleImporter.LoadRegionalSource(WorldBuilderSampleImporter.RegionalRaw)
                : WorldBuilderSampleImporter.LoadRaw(WorldBuilderSampleImporter.TechnicalRaw, 2049);
            var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewPreviewScene();
            Terrain[,] terrains = null;
            try
            {
                terrains = WorldBuilderSampleImporter.CreateTerrains(field, profile, scene);
                double maxError = 0, maxExportError = 0, maxSeamError = 0;
                var readback = new float[4, 4][,];
                for (int tz = 0; tz < 4; tz++)
                for (int tx = 0; tx < 4; tx++)
                {
                    var terrain = terrains[tz, tx];
                    Assert.That(terrain.transform.position, Is.EqualTo(new Vector3(tx * 512, profile.OriginY, tz * 512)));
                    Assert.That(terrain.terrainData.heightmapResolution, Is.EqualTo(513));
                    Assert.That(terrain.terrainData.holesResolution, Is.EqualTo(512));
                    Assert.That(terrain.allowAutoConnect, Is.True);
                    Assert.That(terrain.GetComponent<TerrainCollider>().terrainData, Is.SameAs(terrain.terrainData));
                    Assert.That(terrain.leftNeighbor, Is.SameAs(tx > 0 ? terrains[tz, tx - 1] : null));
                    Assert.That(terrain.topNeighbor, Is.SameAs(tz < 3 ? terrains[tz + 1, tx] : null));
                    Assert.That(terrain.rightNeighbor, Is.SameAs(tx < 3 ? terrains[tz, tx + 1] : null));
                    Assert.That(terrain.bottomNeighbor, Is.SameAs(tz > 0 ? terrains[tz - 1, tx] : null));
                    var heights = terrain.terrainData.GetHeights(0, 0, 513, 513);
                    readback[tz, tx] = heights;
                    var exported = Raw16HeightmapCodec.FromNormalized(heights);
                    for (int z = 0; z < 513; z++)
                    for (int x = 0; x < 513; x++)
                    {
                        double input = field[tz * 512 + z, tx * 512 + x] / 65535.0;
                        maxError = Math.Max(maxError, Math.Abs(heights[z, x] - input) * profile.VerticalRange);
                        maxExportError = Math.Max(maxExportError, Math.Abs(exported[z, x] / 65535.0 - input) * profile.VerticalRange);
                    }
                }
                for (int tz = 0; tz < 4; tz++)
                for (int tx = 0; tx < 4; tx++)
                for (int i = 0; i < 513; i++)
                {
                    if (tx < 3) maxSeamError = Math.Max(maxSeamError, Math.Abs(readback[tz, tx][i, 512] - readback[tz, tx + 1][i, 0]) * profile.VerticalRange);
                    if (tz < 3) maxSeamError = Math.Max(maxSeamError, Math.Abs(readback[tz, tx][512, i] - readback[tz + 1, tx][0, i]) * profile.VerticalRange);
                }
                TestContext.WriteLine($"{(regional ? "C" : "B")}: Unity 오차={maxError:R}m, RAW 재출력={maxExportError:R}m, 경계={maxSeamError:R}m");
                Assert.That(maxError, Is.LessThanOrEqualTo(.03));
                Assert.That(maxExportError, Is.LessThanOrEqualTo(.03));
                Assert.That(maxSeamError, Is.LessThanOrEqualTo(.01));
            }
            finally
            {
                if (terrains != null) foreach (var terrain in terrains) if (terrain != null) UnityEngine.Object.DestroyImmediate(terrain.terrainData);
                EditorSceneManager.ClosePreviewScene(scene);
                if (previousScene.IsValid() && previousScene.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void T27_ReferenceImagesAndNorthFirstRawAreRejected()
        {
            var images = Directory.GetFiles(WorldBuilderSampleImporter.HandoffRoot + "/atlas", "*.png");
            Assert.That(images.Length, Is.GreaterThanOrEqualTo(7));
            foreach (string image in images)
            {
                Assert.Throws<InvalidDataException>(() => WorldBuilderSampleImporter.LoadRegionalSource(image));
                Assert.Throws<InvalidDataException>(() => WorldBuilderSampleImporter.LoadRaw(image, 2049));
            }
            Assert.Throws<InvalidDataException>(() => WorldBuilderSampleImporter.LoadRegionalSource(WorldBuilderSampleImporter.HandoffRoot + "/atlas/01_World_Heightmap_2049_R16LE.raw"));
        }

        [Test]
        public void T28_RegionalChecksumResolutionAndNorthReferenceFlip()
        {
            var field = WorldBuilderSampleImporter.LoadRegionalSource(WorldBuilderSampleImporter.RegionalRaw);
            Assert.That(field.Width, Is.EqualTo(2049));
            Assert.That(field.Height, Is.EqualTo(2049));
            var bytes = Raw16HeightmapCodec.Encode(field, HeightmapRowOrigin.South);
            Assert.That(WorldBuilderSampleImporter.ComputeSha256(bytes), Is.EqualTo(WorldBuilderSampleImporter.RegionalSha256));
            var referenceBytes = File.ReadAllBytes(WorldBuilderSampleImporter.HandoffRoot + "/atlas/01_World_Heightmap_2049_R16LE.raw");
            var flippedReference = Raw16HeightmapCodec.Decode(referenceBytes, 2049, 2049, HeightmapRowOrigin.North);
            CollectionAssert.AreEqual(bytes, Raw16HeightmapCodec.Encode(flippedReference, HeightmapRowOrigin.South));
            Assert.Throws<InvalidDataException>(() => WorldBuilderSampleImporter.LoadRaw(WorldBuilderSampleImporter.RegionalRaw, 2049, new string('0', 64)));
        }

        [Test]
        public void InvalidNumericInputsAndConflictingSeamsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => Raw16HeightmapCodec.Decode(new byte[7], 2, 2, HeightmapRowOrigin.South));
            Assert.Throws<ArgumentOutOfRangeException>(() => Raw16HeightmapCodec.Decode(new byte[8], 2, 2, (HeightmapRowOrigin)9));
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, -0.01f, 1.01f })
                Assert.Throws<ArgumentOutOfRangeException>(() => Raw16HeightmapCodec.FromNormalized(new[,] { { value, 0f }, { 0f, 0f } }));
            var samples = new ushort[] { 0, 1, 2, 3 };
            var first = new HeightField(2, 2, samples);
            samples[0] = 100;
            Assert.That(first[0, 0], Is.Zero);
            Assert.Throws<ArgumentException>(() => HeightField.JoinTiles(new[,] { { first, new HeightField(2, 2, new ushort[4]) } }));
            Assert.Throws<ArgumentOutOfRangeException>(() => first.ExtractTile(1, 0, 1));
        }

        [Test]
        public void CoordinatesAndProfileKeepRegionalAndTechnicalRangesSeparate()
        {
            var profile = ScriptableObject.CreateInstance<WorldProfile>();
            try
            {
                var grid = profile.CreateGrid();
                Assert.That(grid.SampleSpacing, Is.EqualTo(1));
                Assert.That(grid.SampleToWorld(2048), Is.EqualTo(2048));
                Assert.That(grid.WorldToSample(512.5), Is.EqualTo(512.5));
                Assert.That(grid.SampleToHeight(0), Is.Zero);
                Assert.That(grid.SampleToHeight(65535), Is.EqualTo(700));
                Assert.Throws<ArgumentOutOfRangeException>(() => grid.WorldToSample(double.NaN));
                Assert.Throws<ArgumentOutOfRangeException>(() => profile.Configure(2048, 2048, 0, 700, 512));
                Assert.That(profile.SampleCount, Is.EqualTo(2049));
                profile.Configure(2048, 2049, -80, 480, 512);
                Assert.That(profile.CreateGrid().SampleToHeight(0), Is.EqualTo(-80));
                Assert.That(profile.CreateGrid().SampleToHeight(65535), Is.EqualTo(400));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void InvalidTerrainInputPreservesActiveSceneAndCreatesNothing()
        {
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool previousDirty = previous.isDirty;
            var scene = EditorSceneManager.NewPreviewScene();
            var profile = ScriptableObject.CreateInstance<WorldProfile>();
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            try
            {
                Assert.Throws<InvalidDataException>(() => WorldBuilderSampleImporter.CreateTerrains(new HeightField(2, 2, new ushort[4]), profile, scene));
                Assert.That(scene.rootCount, Is.Zero);
                Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Is.EqualTo(previous));
                Assert.That(previous.isDirty, Is.EqualTo(previousDirty));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
