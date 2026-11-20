using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class TerrainOperationCoreTests
    {
        private const int Size = 129;
        private static WorldGrid Grid => new WorldGrid(128, Size, 0, 1000);

        [Test]
        public void T05_FourTileCornerProjectsIdenticalGlobalSampleAndMask()
        {
            var masks = new float[] { .25f, .5f, .75f, 1 };
            var op = new TerrainOperation("corner", TerrainOperationKind.AddDelta, new SampleRect(63, 63, 2, 2), 20, masks: masks);
            TerrainBuildResult result = TerrainOperationEvaluator.Build(Source(), Grid, new[] { op });
            var tiles = new float[4][];
            for (int tileZ = 0; tileZ < 2; tileZ++)
            for (int tileX = 0; tileX < 2; tileX++)
            {
                var tile = new float[65 * 65];
                for (int z = 0; z < 65; z++)
                for (int x = 0; x < 65; x++) tile[z * 65 + x] = result.GetHeight(tileZ * 64 + z, tileX * 64 + x);
                tiles[tileZ * 2 + tileX] = tile;
            }
            Assert.That(tiles[0][64 * 65 + 64], Is.EqualTo(tiles[1][64 * 65]));
            Assert.That(tiles[0][64 * 65 + 64], Is.EqualTo(tiles[2][64]));
            Assert.That(tiles[0][64 * 65 + 64], Is.EqualTo(tiles[3][0]));
            Assert.That(result.GetHeight(64, 64) - BaseHeight(64, 64), Is.EqualTo(20));
            Assert.That(result.GetHeight(63, 63) - BaseHeight(63, 63), Is.EqualTo(5));
            Assert.That(op.GetMask(3), Is.EqualTo(1));
        }

        [Test]
        public void T06_ThreeRunsAndForwardReverseParallelFourByFourTilesHaveSameHash()
        {
            HeightField source = Source();
            TerrainOperation[] ops = Stack();
            string expected = TerrainOperationEvaluator.Build(source, Grid, ops).ComputeHash();
            for (int i = 0; i < 3; i++) Assert.That(TerrainOperationEvaluator.Build(source, Grid, ops).ComputeHash(), Is.EqualTo(expected));
            var reversed = (TerrainOperation[])ops.Clone();
            Array.Reverse(reversed);
            Assert.That(TerrainOperationEvaluator.ComputeStackHash(reversed), Is.EqualTo(TerrainOperationEvaluator.ComputeStackHash(ops)));
            Assert.That(TerrainOperationEvaluator.Build(source, Grid, reversed).ComputeHash(), Is.EqualTo(expected));
            Assert.That(RebuildTiles(source, ops, false, false).ComputeHash(), Is.EqualTo(expected));
            Assert.That(RebuildTiles(source, ops, true, false).ComputeHash(), Is.EqualTo(expected));
            Assert.That(RebuildTiles(source, ops, false, true).ComputeHash(), Is.EqualTo(expected));
        }

        [Test]
        public void T07_AddDeleteMoveAndMaskedSmoothRemainBitIdenticalToFullBuild()
        {
            HeightField source = Source();
            var smooth = new TerrainOperation("smooth", TerrainOperationKind.Smooth, SampleRect.Full(Size), stage: 50, radius: 2, iterations: 3);
            var secondSmooth = new TerrainOperation("smooth-local", TerrainOperationKind.Smooth, new SampleRect(20, 20, 90, 90), stage: 60, radius: 1, iterations: 2, masks: Filled(90 * 90, .4f));
            var operations = new List<TerrainOperation> { smooth, secondSmooth };
            TerrainBuildResult previous = TerrainOperationEvaluator.Build(source, Grid, operations);
            var oldBounds = new SampleRect(61, 61, 7, 8);
            var movedBounds = new SampleRect(82, 31, 7, 8);
            var patch = new TerrainOperation("patch", TerrainOperationKind.AddDelta, oldBounds, 40);
            operations.Add(patch);
            previous = AssertDirtyEqualsFull(source, operations, previous, oldBounds);
            operations[2] = new TerrainOperation("patch", TerrainOperationKind.AddDelta, movedBounds, -15);
            previous = AssertDirtyEqualsFull(source, operations, previous, SampleRect.Union(oldBounds, movedBounds));
            operations.RemoveAt(2);
            previous = AssertDirtyEqualsFull(source, operations, previous, movedBounds);
            operations.Add(new TerrainOperation("edge", TerrainOperationKind.SetAbsolute, new SampleRect(0, 0, 2, 2), 900));
            AssertDirtyEqualsFull(source, operations, previous, new SampleRect(0, 0, 2, 2));
        }

        [Test]
        public void HolesUnionSurvivesDeletionAndUsesCellGridAndStrictMaskThreshold()
        {
            HeightField source = Source();
            var a = new TerrainOperation("a", TerrainOperationKind.HoleCut, new SampleRect(62, 62, 3, 3));
            var b = new TerrainOperation("b", TerrainOperationKind.HoleCut, new SampleRect(64, 64, 2, 2));
            TerrainBuildResult result = TerrainOperationEvaluator.Build(source, Grid, new[] { a, b });
            Assert.That(result.GetSurface(64, 64), Is.False);
            result = AssertDirtyEqualsFull(source, new[] { b }, result, a.Bounds);
            Assert.That(result.GetSurface(62, 62), Is.True);
            Assert.That(result.GetSurface(64, 64), Is.False);
            var edge = new TerrainOperation("edge", TerrainOperationKind.HoleCut, new SampleRect(127, 127, 1, 1), masks: new[] { .5f });
            Assert.That(TerrainOperationEvaluator.Build(source, Grid, new[] { edge }).GetSurface(127, 127), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => TerrainOperationEvaluator.Build(source, Grid,
                new[] { new TerrainOperation("outside", TerrainOperationKind.HoleCut, new SampleRect(128, 128, 1, 1)) }));
        }

        [Test]
        public void ProtectUnionBlocksAutomaticHeightAndHoleButAllowsManualPatch()
        {
            var bounds = new SampleRect(63, 63, 2, 2);
            var protect = new TerrainOperation("protect", TerrainOperationKind.ProtectRegion, bounds, stage: 100,
                protection: TerrainProtection.Height | TerrainProtection.Holes);
            var height = new TerrainOperation("auto-height", TerrainOperationKind.AddDelta, bounds, 30, automatic: true);
            var hole = new TerrainOperation("auto-hole", TerrainOperationKind.HoleCut, bounds, automatic: true);
            TerrainBuildResult result = TerrainOperationEvaluator.Build(Source(), Grid, new[] { height, hole, protect });
            Assert.That(result.GetHeight(63, 63), Is.EqualTo(BaseHeight(63, 63)));
            Assert.That(result.GetSurface(63, 63), Is.True);
            var manual = new TerrainOperation("manual", TerrainOperationKind.Flatten, bounds, 400, stage: 70);
            var manualHole = new TerrainOperation("manual-hole", TerrainOperationKind.HoleCut, bounds, stage: 80);
            result = TerrainOperationEvaluator.Build(Source(), Grid, new[] { height, hole, protect, manual, manualHole });
            Assert.That(result.GetHeight(63, 63), Is.EqualTo(400));
            Assert.That(result.GetSurface(63, 63), Is.False);
        }

        [Test]
        public void TargetsAndOrderingUseFixedPlanesAndOrdinalGuidTieBreak()
        {
            var bounds = new SampleRect(3, 3, 1, 1);
            var a = new TerrainOperation("a", TerrainOperationKind.SetAbsolute, bounds, 600, masks: new[] { .5f });
            var b = new TerrainOperation("b", TerrainOperationKind.AddDelta, bounds, 20);
            var c = new TerrainOperation("c", TerrainOperationKind.Flatten, bounds, 200, stage: 50);
            TerrainBuildResult result = TerrainOperationEvaluator.Build(Source(), Grid, new[] { b, a });
            Assert.That(result.GetHeight(3, 3), Is.EqualTo(BaseHeight(3, 3) + (600 - BaseHeight(3, 3)) * .5f + 20));
            Assert.That(TerrainOperationEvaluator.Build(Source(), Grid, new[] { c, b, a }).GetHeight(3, 3), Is.EqualTo(200));
        }

        [Test]
        public void SmoothReadsPreviousIterationWithoutDirectionalAccumulation()
        {
            var source = new HeightField(5, 5, new ushort[25]);
            var grid = new WorldGrid(4, 5, 0, 1000);
            var spike = new TerrainOperation("spike", TerrainOperationKind.SetAbsolute, new SampleRect(2, 2, 1, 1), 90);
            var smooth = new TerrainOperation("smooth", TerrainOperationKind.Smooth, SampleRect.Full(5), stage: 50);
            TerrainBuildResult result = TerrainOperationEvaluator.Build(source, grid, new[] { spike, smooth });
            for (int z = 1; z <= 3; z++)
            for (int x = 1; x <= 3; x++) Assert.That(result.GetHeight(z, x), Is.EqualTo(10));
            Assert.That(result.GetHeight(0, 0), Is.Zero);
        }

        [Test]
        public void SourceOperationAndResultArraysRemainImmutableAcrossReplay()
        {
            HeightField source = Source();
            byte[] raw = Raw16HeightmapCodec.Encode(source, HeightmapRowOrigin.South);
            var values = new[] { 200f };
            var masks = new[] { 1f };
            var op = new TerrainOperation("immutable", TerrainOperationKind.SetAbsolute, new SampleRect(3, 3, 1, 1), values: values, masks: masks);
            values[0] = 900; masks[0] = 0; op.Values[0] = 800; op.Masks[0] = 0;
            TerrainBuildResult result = TerrainOperationEvaluator.Build(source, Grid, new[] { op });
            string hash = result.ComputeHash();
            result.CopyHeights()[0] = -999;
            result.CopySurfaces()[0] = false;
            for (int i = 0; i < 20; i++)
                Assert.That(TerrainOperationEvaluator.Build(source, Grid, new[] { op }).ComputeHash(), Is.EqualTo(hash));
            Assert.That(result.GetHeight(3, 3), Is.EqualTo(200));
            Assert.That(result.ComputeHash(), Is.EqualTo(hash));
            CollectionAssert.AreEqual(raw, Raw16HeightmapCodec.Encode(source, HeightmapRowOrigin.South));
        }

        [Test]
        public void InvalidTypesArraysIdsBoundsAndFinalRangeAreRejected()
        {
            var bounds = new SampleRect(0, 0, 1, 1);
            Assert.Throws<ArgumentException>(() => new TerrainOperation(" ", TerrainOperationKind.AddDelta, bounds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainOperation("x", (TerrainOperationKind)99, bounds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, protection: (TerrainProtection)16));
            Assert.Throws<ArgumentException>(() => new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, values: new[] { float.NaN }));
            Assert.Throws<ArgumentException>(() => new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, masks: new[] { 1.01f }));
            Assert.Throws<ArgumentException>(() => new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, values: new float[2]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainOperation("x", TerrainOperationKind.Smooth, bounds, radius: 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainOperation("x", TerrainOperationKind.Smooth, bounds, iterations: 17));
            var op = new TerrainOperation("same", TerrainOperationKind.AddDelta, bounds);
            Assert.Throws<ArgumentException>(() => TerrainOperationEvaluator.Build(Source(), Grid, new[] { op, op }));
            Assert.Throws<ArgumentOutOfRangeException>(() => TerrainOperationEvaluator.Build(Source(), Grid,
                new[] { new TerrainOperation("x", TerrainOperationKind.AddDelta, new SampleRect(129, 0, 1, 1), enabled: false) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => TerrainOperationEvaluator.Build(Source(), Grid,
                new[] { new TerrainOperation("x", TerrainOperationKind.SetAbsolute, bounds, 1001) }));
            Assert.Throws<ArgumentException>(() => TerrainOperationEvaluator.Build(new HeightField(2, 2, new ushort[4]), Grid, new TerrainOperation[0]));
        }

        [Test]
        public void SnapshotKeepsVersionOneBytesAndLargeSha256()
        {
            var result = new TerrainBuildResult(2, new[] { 1f, -.5f, 0f, 2f }, new[] { true });
            CollectionAssert.AreEqual(new byte[]
            {
                0x57, 0x42, 0x54, 0x53, 1, 0, 0, 0, 2, 0, 0, 0,
                0, 0, 0x80, 0x3f, 0, 0, 0, 0xbf, 0, 0, 0, 0, 0, 0, 0, 0x40, 1
            }, TerrainSnapshotCodec.Encode(result));
            Assert.That(TerrainSnapshotCodec.ComputeSha256(new byte[1024 * 1024]),
                Is.EqualTo("30e14955ebf1352266dc2ff8067e68104607e750abb9d3b36582b8af909fcb58"));
        }

        [Test]
        public void SnapshotAndPatchRoundtripRejectMalformedTrailingAndNonfinitePayloads()
        {
            TerrainBuildResult result = TerrainOperationEvaluator.Build(Source(), Grid, Stack());
            byte[] bytes = TerrainSnapshotCodec.Encode(result);
            Assert.That(bytes[0], Is.EqualTo((byte)'W'));
            Assert.That(bytes[1], Is.EqualTo((byte)'B'));
            Assert.That(TerrainSnapshotCodec.Decode(bytes).ComputeHash(), Is.EqualTo(result.ComputeHash()));
            var trailing = new byte[bytes.Length + 1]; Array.Copy(bytes, trailing, bytes.Length);
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.Decode(trailing));
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.Decode(new byte[3]));
            var nonfinite = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(float.NaN), 0, nonfinite, 12, 4);
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.Decode(nonfinite));
            var invalidBool = (byte[])bytes.Clone(); invalidBool[12 + Size * Size * 4] = 2;
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.Decode(invalidBool));
            var huge = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, huge, 8, 4);
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.Decode(huge));
            float[] values = { 1, 2, 3, 4, 5, 6 }, masks = { 0, .1f, .5f, .9f, 1, 0 };
            byte[] patch = TerrainSnapshotCodec.EncodePatch(3, 2, values, masks);
            TerrainSnapshotCodec.DecodePatch(patch, out int width, out int height, out float[] decodedValues, out float[] decodedMasks);
            Assert.That(width, Is.EqualTo(3)); Assert.That(height, Is.EqualTo(2));
            CollectionAssert.AreEqual(values, decodedValues); CollectionAssert.AreEqual(masks, decodedMasks);
            patch[patch.Length - 1] = 0x7f;
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.DecodePatch(patch, out _, out _, out _, out _));
            Assert.Throws<InvalidDataException>(() => TerrainSnapshotCodec.DecodePatch(bytes, out _, out _, out _, out _));
        }

        [Test]
        public void StackHashIncludesEveryOperationFieldIncludingDisabledArrays()
        {
            var bounds = new SampleRect(3, 4, 1, 1);
            var baseline = new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds);
            var variants = new[]
            {
                new TerrainOperation("y", TerrainOperationKind.AddDelta, bounds),
                new TerrainOperation("x", TerrainOperationKind.Flatten, bounds),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, new SampleRect(4, 4, 1, 1)),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, new SampleRect(3, 5, 1, 1)),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, new SampleRect(3, 4, 2, 1)),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, new SampleRect(3, 4, 1, 2)),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, 1),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, stage: 41),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, priority: 1),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, sequence: 1),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, enabled: false),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, automatic: true),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, radius: 2),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, iterations: 2),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, protection: TerrainProtection.Holes),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, values: new[] { 0f }),
                new TerrainOperation("x", TerrainOperationKind.AddDelta, bounds, masks: new[] { 1f })
            };
            string hash = TerrainOperationEvaluator.ComputeStackHash(new[] { baseline });
            foreach (TerrainOperation variant in variants)
                Assert.That(TerrainOperationEvaluator.ComputeStackHash(new[] { variant }), Is.Not.EqualTo(hash));
        }

        [Test]
        public void DirtyClosureHaloAndRectOverflowAreExplicit()
        {
            WorldDirtyFlags expanded = DirtyDependencyGraph.Expand(WorldDirtyFlags.WaterPlan | WorldDirtyFlags.Structures);
            Assert.That(expanded & (WorldDirtyFlags.Height | WorldDirtyFlags.Slope | WorldDirtyFlags.Material | WorldDirtyFlags.Water |
                WorldDirtyFlags.Placement | WorldDirtyFlags.Navigation | WorldDirtyFlags.Proxy),
                Is.EqualTo(WorldDirtyFlags.Height | WorldDirtyFlags.Slope | WorldDirtyFlags.Material | WorldDirtyFlags.Water |
                WorldDirtyFlags.Placement | WorldDirtyFlags.Navigation | WorldDirtyFlags.Proxy));
            Assert.That(DirtyDependencyGraph.Expand(expanded), Is.EqualTo(expanded));
            Assert.That(DirtyDependencyGraph.Expand(WorldDirtyFlags.VegetationMaterial), Is.EqualTo(WorldDirtyFlags.VegetationMaterial | WorldDirtyFlags.Material));
            Assert.That(DirtyDependencyGraph.Expand(WorldDirtyFlags.Holes), Is.EqualTo(WorldDirtyFlags.Holes | WorldDirtyFlags.Placement | WorldDirtyFlags.Navigation | WorldDirtyFlags.Proxy));
            Assert.That(DirtyDependencyGraph.GetHeightHalo(new[] {
                new TerrainOperation("a", TerrainOperationKind.Smooth, SampleRect.Full(Size), radius: 2, iterations: 3),
                new TerrainOperation("b", TerrainOperationKind.Smooth, SampleRect.Full(Size), enabled: false, radius: 8, iterations: 16),
                new TerrainOperation("c", TerrainOperationKind.Smooth, SampleRect.Full(Size), radius: 1, iterations: 2) }), Is.EqualTo(8));
            SampleRect full = new SampleRect(5, 5, 1, 1).Expand(int.MaxValue, Size, Size);
            Assert.That(full.Width, Is.EqualTo(Size)); Assert.That(full.X, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SampleRect(int.MaxValue, 0, 1, 1));
            Assert.That(new SampleRect(3, 3, 2, 2).Contains(5, 4), Is.False);
        }

        private static TerrainBuildResult AssertDirtyEqualsFull(HeightField source, IReadOnlyList<TerrainOperation> ops,
            TerrainBuildResult previous, SampleRect dirty)
        {
            TerrainBuildResult updated = TerrainOperationEvaluator.Rebuild(source, Grid, ops, previous, dirty);
            TerrainBuildResult full = TerrainOperationEvaluator.Build(source, Grid, ops);
            Assert.That(updated.ComputeHash(), Is.EqualTo(full.ComputeHash()));
            return updated;
        }

        private static TerrainBuildResult RebuildTiles(HeightField source, TerrainOperation[] ops, bool reverse, bool parallel)
        {
            TerrainBuildResult baseline = TerrainOperationEvaluator.Build(source, Grid, new TerrainOperation[0]);
            var regions = new TerrainBuildResult[16];
            Action<int> rebuild = index =>
            {
                int tile = reverse ? 15 - index : index;
                regions[tile] = TerrainOperationEvaluator.Rebuild(source, Grid, ops, baseline,
                    new SampleRect((tile % 4) * 32, (tile / 4) * 32, 33, 33));
            };
            if (parallel) Parallel.For(0, 16, rebuild);
            else for (int i = 0; i < 16; i++) rebuild(i);
            float[] heights = baseline.CopyHeights(); bool[] surfaces = baseline.CopySurfaces();
            for (int tile = 0; tile < 16; tile++)
            {
                int startX = tile % 4 * 32, startZ = tile / 4 * 32;
                for (int z = startZ; z <= startZ + 32; z++)
                for (int x = startX; x <= startX + 32; x++) heights[z * Size + x] = regions[tile].GetHeight(z, x);
                for (int z = startZ; z < startZ + 32; z++)
                for (int x = startX; x < startX + 32; x++) surfaces[z * (Size - 1) + x] = regions[tile].GetSurface(z, x);
            }
            return new TerrainBuildResult(Size, heights, surfaces);
        }

        private static TerrainOperation[] Stack() => new[]
        {
            new TerrainOperation("add", TerrainOperationKind.AddDelta, new SampleRect(61, 60, 8, 9), 40),
            new TerrainOperation("smooth", TerrainOperationKind.Smooth, SampleRect.Full(Size), stage: 50, radius: 2, iterations: 2),
            new TerrainOperation("hole", TerrainOperationKind.HoleCut, new SampleRect(63, 63, 4, 4), stage: 60)
        };

        private static float[] Filled(int count, float value)
        {
            var values = new float[count];
            for (int i = 0; i < count; i++) values[i] = value;
            return values;
        }

        private static HeightField Source()
        {
            var samples = new ushort[Size * Size];
            for (int z = 0; z < Size; z++)
            for (int x = 0; x < Size; x++) samples[z * Size + x] = Sample(z, x);
            return new HeightField(Size, Size, samples);
        }

        private static ushort Sample(int z, int x) => (ushort)(20000 + (z * 37 + x * 23) % 15000);
        private static float BaseHeight(int z, int x) => (float)Grid.SampleToHeight(Sample(z, x));
    }
}
