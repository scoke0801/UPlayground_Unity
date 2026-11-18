using System;
using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Editor;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class PlacementTests
    {
        private const string World = "11111111111111111111111111111111", Layer = "22222222222222222222222222222222", Rule = "33333333333333333333333333333333", Prefab = "44444444444444444444444444444444";
        private WorldGrid _grid;
        private TerrainBuildResult _terrain;
        private PlacementRule _rule;
        private List<PlacementPrefab> _prefabs;
        private List<PlacementOverride> _edits;
        private List<PlacementReservation> _reservations;
        [SetUp]
        public void Setup()
        {
            _grid = new WorldGrid(64, 65, 0, 100);
            var heights = new float[65 * 65]; var surfaces = new bool[64 * 64];
            for (int i = 0; i < heights.Length; i++) heights[i] = 10;
            for (int i = 0; i < surfaces.Length; i++) surfaces[i] = true;
            _terrain = new TerrainBuildResult(65, heights, surfaces);
            _rule = new PlacementRule { LayerGuid = Layer, RuleGuid = Rule, PrefabGuid = Prefab, CellSize = 7, Spacing = 3, MaxX = 64, MaxZ = 64 };
            _prefabs = new List<PlacementPrefab> { new PlacementPrefab { Guid = Prefab, HalfX = 1, HalfZ = 2 } };
            _edits = new List<PlacementOverride>(); _reservations = new List<PlacementReservation>();
        }
        private PlacementBuildResult Build(int epoch = 0, int seed = 42, int[] order = null, bool parallel = false)
            => PlacementEngine.Build(World, epoch, seed, _grid, _terrain, 32, new[] { _rule }, _prefabs, _edits, _reservations, order, parallel);

        [Test]
        public void RotatedOblongFootprintMatchesUnityYawInsteadOfMirroringIt()
        {
            var oblong = new PlacementRecord { x = 20, z = 20, halfX = 1, halfZ = 4, yaw = 45 };
            var alongLocalZ = new PlacementRecord { x = 22.5, z = 22.5, halfX = .1, halfZ = .1 };
            var outsideLocalX = new PlacementRecord { x = 22.5, z = 17.5, halfX = .1, halfZ = .1 };
            Assert.That(PlacementSpatialHash.Overlaps(oblong, alongLocalZ, 0, 0));
            Assert.That(PlacementSpatialHash.Overlaps(oblong, outsideLocalX, 0, 0), Is.False);
        }
        [Test]
        public void T06_ThreeRunsReverseParallelAndRuleOrderHaveIdenticalCanonicalOutput()
        {
            string hash = Build().ComputeHash(); Assert.That(Build().Records.Count, Is.GreaterThan(5));
            for (int i = 0; i < 3; i++) Assert.That(Build().ComputeHash(), Is.EqualTo(hash));
            Assert.That(Build(order: new[] { 3, 2, 1, 0 }).ComputeHash(), Is.EqualTo(hash));
            Assert.That(Build(order: new[] { 2, 0, 3, 1 }, parallel: true).ComputeHash(), Is.EqualTo(hash));
            var other = new PlacementRule { LayerGuid = Layer, RuleGuid = "55555555555555555555555555555555", PrefabGuid = Prefab, CellSize = 9, MaxX = 64, MaxZ = 64 };
            var a = PlacementEngine.Build(World, 0, 42, _grid, _terrain, 32, new[] { _rule, other }, _prefabs, _edits, _reservations);
            var b = PlacementEngine.Build(World, 0, 42, _grid, _terrain, 32, new[] { other, _rule }, _prefabs, _edits, _reservations, new[] { 3, 1, 0, 2 }, true);
            Assert.That(a.ComputeHash(), Is.EqualTo(b.ComputeHash()));
        }
        [Test]
        public void T07_LocalHeightAndHoleEditsReevaluateToFullResult()
        {
            var source = new HeightField(65, 65, new ushort[65 * 65]);
            var ops = new List<TerrainOperation>();
            var previous = TerrainOperationEvaluator.Build(source, _grid, ops);
            for (int i = 0; i < 3; i++)
            {
                var rect = new SampleRect(26 + i, 28 + i, 8, 8);
                ops.Add(new TerrainOperation(Guid.NewGuid().ToString("N"), TerrainOperationKind.AddDelta, rect, i + 1));
                var dirty = TerrainOperationEvaluator.Rebuild(source, _grid, ops, previous, rect);
                var full = TerrainOperationEvaluator.Build(source, _grid, ops);
                _terrain = dirty; var dirtyPlacement = Build();
                _terrain = full; var fullPlacement = Build();
                Assert.That(dirtyPlacement.ComputeHash(), Is.EqualTo(fullPlacement.ComputeHash())); previous = dirty;
            }
            Assert.That((DirtyDependencyGraph.Expand(WorldDirtyFlags.Holes) & WorldDirtyFlags.Placement) != 0);
        }
        [Test]
        public void T11_DeleteNeverReturnsInEpochPinSurvivesSeedAndReportsOrphans()
        {
            var initial = Build(); var deleted = initial.Records[0]; var pin = initial.Records[1];
            _edits.Add(WorldPlacementAuthoring.CreateTargetOverride(deleted, PlacementOverrideKind.Delete));
            _edits.Add(WorldPlacementAuthoring.CreatePin(pin));
            for (int i = 0; i < 3; i++)
            {
                var result = Build(); Assert.That(result.Records.Exists(p => p.id == deleted.id), Is.False);
                var kept = result.Records.Find(p => p.id == pin.id); Assert.That(kept.x, Is.EqualTo(pin.x)); Assert.That(kept.y, Is.EqualTo(pin.y));
            }
            var changed = Build(1, 43);
            Assert.That(changed.Count(PlacementRejection.OrphanOverride), Is.EqualTo(2));
            Assert.That(changed.Records.Find(p => p.id == pin.id).sourceIdentity, Is.EqualTo(pin.sourceIdentity));
            _rule.RuleGuid = "55555555555555555555555555555555";
            var lost = Build(); Assert.That(lost.Records.Exists(p => p.id == pin.id)); Assert.That(lost.Count(PlacementRejection.OrphanOverride), Is.EqualTo(2));
        }
        [Test]
        public void T12_FixedRotatedFootprintAndCrossBoundarySpacingAreProtected()
        {
            var manual = Manual(32, 32); manual.placement.scaleX = manual.placement.scaleY = manual.placement.scaleZ = 4; manual.placement.yaw = 37;
            _edits.Add(manual);
            var result = Build(); Assert.That(result.HasErrors, Is.False);
            Assert.That(result.Count(PlacementRejection.FootprintOverlap) + result.Count(PlacementRejection.SpacingRejected), Is.GreaterThan(0));
            foreach (var a in result.Records)
            foreach (var b in result.Records) if (string.CompareOrdinal(a.id, b.id) < 0)
            { Assert.That(PlacementSpatialHash.Overlaps(a, b, a.spacing / 2, b.spacing / 2), Is.False); }
            Assert.That(result.Records.Exists(p => p.ownerX == 0)); Assert.That(result.Records.Exists(p => p.ownerX == 1));
            Assert.That(Build(order: new[] { 3, 2, 1, 0 }, parallel: true).ComputeHash(), Is.EqualTo(result.ComputeHash()));
        }
        [Test]
        public void ManualMoveRotateScaleDeleteAndReplaceKeepIdentity()
        {
            var manual = Manual(20, 20); _edits.Add(manual);
            var target = Build().Records.Find(p => p.isFixed);
            var transform = WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.Transform);
            transform.placement.x = 30; transform.placement.yaw = 43; transform.placement.scaleX = transform.placement.scaleY = transform.placement.scaleZ = 2;
            _edits.Add(transform);
            var result = Build(); var moved = result.Records.Find(p => p.id == target.id);
            Assert.That(moved.x, Is.EqualTo(30)); Assert.That(moved.yaw, Is.EqualTo(43)); Assert.That(moved.halfX, Is.EqualTo(2));
            string other = "66666666666666666666666666666666";
            _prefabs.Add(new PlacementPrefab { Guid = other, HalfX = .5, HalfZ = .5 });
            var replace = WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.ReplacePrefab); replace.prefabGuid = other; _edits.Add(replace);
            Assert.That(Build().Records.Find(p => p.id == target.id).prefabGuid, Is.EqualTo(other));
            _edits.Add(WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.Delete));
            Assert.That(Build().Records.Exists(p => p.id == target.id), Is.False);
        }
        [TestCase(PlacementRejection.HeightRejected)]
        [TestCase(PlacementRejection.SlopeRejected)]
        [TestCase(PlacementRejection.HoleOverlap)]
        [TestCase(PlacementRejection.ReservedArea)]
        [TestCase(PlacementRejection.Unsupported)]
        [TestCase(PlacementRejection.SurfaceMismatch)]
        public void EnvironmentalFiltersActuallyReject(PlacementRejection reason)
        {
            if (reason == PlacementRejection.HeightRejected) _rule.MaxHeight = 9;
            if (reason == PlacementRejection.SurfaceMismatch) _rule.SurfaceId = "bridge";
            if (reason == PlacementRejection.ReservedArea) _reservations.Add(new PlacementReservation { guid = Guid.NewGuid().ToString("N"), minX = 0, minZ = 0, maxX = 64, maxZ = 64 });
            if (reason == PlacementRejection.HoleOverlap) _terrain = new TerrainBuildResult(65, _terrain.CopyHeights(), new bool[64 * 64]);
            if (reason == PlacementRejection.SlopeRejected || reason == PlacementRejection.Unsupported)
            {
                var heights = _terrain.CopyHeights();
                for (int z = 0; z < 65; z++) for (int x = 0; x < 65; x++) heights[z * 65 + x] = x;
                _terrain = new TerrainBuildResult(65, heights, _terrain.CopySurfaces());
                _rule.MaxSlope = reason == PlacementRejection.SlopeRejected ? 10 : 90; _rule.MaxSupportDelta = 0;
            }
            var result = Build(); Assert.That(result.Count(reason), Is.GreaterThan(0)); Assert.That(result.Records, Is.Empty);
        }
        [Test]
        public void ExcludeSurvivesSeedAndTagSelectsRules()
        {
            _edits.Add(new PlacementOverride { overrideGuid = Guid.NewGuid().ToString("N"), kind = PlacementOverrideKind.ExcludeRegion, minX = 0, minZ = 0, maxX = 64, maxZ = 64, ruleTag = "props" });
            Assert.That(Build().Records.Count, Is.GreaterThan(0));
            _rule.Tag = "props"; Assert.That(Build(1, 43).Records, Is.Empty);
        }
        [Test]
        public void DuplicateIdentityInvalidDataAndFixedConflictFailExplicitly()
        {
            var manual = Manual(20, 20); _edits.Add(manual);
            var duplicate = manual.Clone(); duplicate.overrideGuid = Guid.NewGuid().ToString("N"); _edits.Add(duplicate);
            Assert.That(Build().Count(PlacementRejection.DuplicateId), Is.GreaterThan(0));
            _edits.Clear(); _edits.Add(Manual(20, 20)); _edits.Add(Manual(20, 20)); Assert.That(Build().HasErrors);
            _edits.Clear(); _rule.CellSize = double.NaN; Assert.Throws<ArgumentException>(() => Build());
        }
        [Test]
        public void ReplacementFootprintConflictIsErrorAndSourceIdentityIsRetained()
        {
            var target = Build().Records[0]; string other = "66666666666666666666666666666666";
            _prefabs.Add(new PlacementPrefab { Guid = other, HalfX = 50, HalfZ = 50 });
            var replace = WorldPlacementAuthoring.CreateTargetOverride(target, PlacementOverrideKind.ReplacePrefab); replace.prefabGuid = other; _edits.Add(replace);
            Assert.That(Build().HasErrors);
        }
        [Test]
        public void MandatoryShortfallIsErrorDecorationShortfallIsWarning()
        {
            _rule.Density = 0; Assert.That(Build().HasErrors, Is.False); Assert.That(Build().Count(PlacementRejection.TargetCount), Is.EqualTo(1));
            _rule.Mandatory = true; _rule.MinimumCount = 1; Assert.That(Build().HasErrors);
        }
        private static PlacementOverride Manual(double x, double z)
        {
            string id = Guid.NewGuid().ToString("N");
            return new PlacementOverride { overrideGuid = Guid.NewGuid().ToString("N"), kind = PlacementOverrideKind.Manual,
                placement = new PlacementRecord { id = id, sourceIdentity = "manual:" + World + ":" + id, prefabGuid = Prefab, x = x, y = 10, z = z, maxSupportDelta = 1, spacing = 3, anchor = PlacementAnchor.Absolute } };
        }
    }
}
