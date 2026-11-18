using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldPlanningTests
    {
        private WorldGrid _grid;
        private HeightField _source;
        private List<TerrainOperation> _operations;
        private List<PlanRegion> _regions;
        private List<PoiPlan> _pois;
        private List<StructurePlan> _structures;
        private List<RoadPlan> _roads;
        private List<BridgePlan> _bridges;
        private List<PlanZone> _zones;
        private static string Id(int value) => value.ToString("x32");
        [SetUp]
        public void Setup()
        {
            _grid = new WorldGrid(128, 129, 0, 655.35); var samples = new ushort[129 * 129];
            for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++) samples[z * 129 + x] = (ushort)(x >= 60 && x <= 68 && z >= 30 && z <= 100 ? 200 : 1000);
            _source = new HeightField(129, 129, samples); _operations = new List<TerrainOperation>();
            _regions = new List<PlanRegion> { new PlanRegion { Guid = Id(1), Boundary = Rect(0, 0, 64, 128) }, new PlanRegion { Guid = Id(2), Boundary = Rect(64, 0, 128, 128) } };
            _pois = new List<PoiPlan> { new PoiPlan { guid = Id(3), regionGuid = Id(1), anchor = new PlanPoint(32, 11, 32), hasConfirmedElevation = true, isLocked = true }, new PoiPlan { guid = Id(4), regionGuid = Id(2), anchor = new PlanPoint(96, 11, 96), hasConfirmedElevation = true } };
            _structures = new List<StructurePlan> { Structure(5, 3, 0), Structure(6, 4, 180) };
            _bridges = new List<BridgePlan> { new BridgePlan { Guid = Id(7), PrefabGuid = Id(101), WaterPlanGuid = Id(9), Start = new PlanPoint(56, 12, 64), End = new PlanPoint(72, 12, 64), PresetSpan = 16,
                HasConfirmedWaterLevel = true, PlannedWaterLevel = 8, WaterZone = new PlanZone { guid = Id(10), boundary = Rect(60, 30, 68, 100) } } };
            _roads = new List<RoadPlan> { new RoadPlan { Guid = Id(8), StartStructureGuid = Id(5), EndStructureGuid = Id(6), RequiredBridgeGuid = Id(7), Options = new RoadOptions() } };
            _zones = new List<PlanZone>();
        }
        private static StructurePlan Structure(int id, int poi, double yaw) => new StructurePlan { Guid = Id(id), PoiGuid = Id(poi), PrefabGuid = Id(100), Yaw = yaw, HalfX = 4, HalfZ = 4, PlotMargin = 4, Falloff = 8,
            SupportPoints = new[] { new PlanPoint(-3, 0, -3), new PlanPoint(3, 0, -3), new PlanPoint(-3, 0, 3), new PlanPoint(3, 0, 3), new PlanPoint(0, 0, 0) }, EntranceSockets = new[] { new PlanPoint(0, 0, 8) } };
        private static PlanPoint[] Rect(double x0, double z0, double x1, double z1) => new[] { new PlanPoint(x0, 0, z0), new PlanPoint(x1, 0, z0), new PlanPoint(x1, 0, z1), new PlanPoint(x0, 0, z1) };
        private WorldPlanningResult Build() => WorldPlanningEngine.Build(Id(201), Id(202), _source, _grid, 64, _operations, _regions, _pois, _structures, _roads, _zones, _bridges, Array.Empty<PlacementRecord>());
        private static string Issues(WorldPlanningResult result) => string.Join("\n", result.Diagnostics.Select(d => d.elementGuid + " " + d.issue + " " + d.message));
        [Test] public void BridgeHoleApproachSlopeAndUnequalSocketHeightAreRejected()
        {
            _bridges[0].MaxApproachSlope = 1; Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.BridgeApproachRejected));
            _bridges[0].MaxApproachSlope = 12;
            _operations.Add(new TerrainOperation(Id(303), TerrainOperationKind.HoleCut, new SampleRect(55, 63, 3, 3)));
            Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.Unsupported));
            _operations.Clear(); _bridges[0].End.y += .1;
            Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.BridgeApproachRejected));
        }
        [Test] public void FixedFootprintCannotBeCrossedByLockedRoad()
        {
            var baseline = Build(); Assert.That(baseline.HasErrors, Is.False, Issues(baseline));
            _roads[0].Knots = baseline.Roads[0].Knots; _roads[0].IsLocked = true;
            int index = baseline.Roads[0].Samples.Length / 4; var point = baseline.Roads[0].Samples[index];
            var direction = (baseline.Roads[0].Samples[index + 1] - point).Normalized; point += new PlanPoint(-direction.z, 0, direction.x);
            var pin = new PlacementRecord { id = Id(500), x = point.x, y = point.y, z = point.z, halfX = .2, halfZ = .2, isFixed = true };
            var result = WorldPlanningEngine.Build(Id(201), Id(202), _source, _grid, 64, _operations, _regions, _pois, _structures, _roads, _zones, _bridges, new[] { pin });
            Assert.That(result.HasErrors); Assert.That(result.Diagnostics.Any(d => d.issue == PlanningIssue.CurveRejected));
        }
        [Test] public void NarrowReservationAndHoleBetweenRoadSampleLinesAreRejected()
        {
            var values = Enumerable.Repeat((ushort)1000, 129 * 129).ToArray();
            var flat = TerrainOperationEvaluator.Build(new HeightField(129, 129, values), _grid, Array.Empty<TerrainOperation>());
            var from = new PlanPoint(40, 10, 64); var to = new PlanPoint(60, 10, 64); var options = new RoadOptions();
            var zone = new PlanZone { guid = Id(501), boundary = Rect(49.8, 64.6, 50.2, 65) };
            Assert.That(WorldRoadPathfinder.CanWalk(from, to, _grid, flat, options, new[] { zone }, Array.Empty<BridgePlan>()), Is.False);
            var hole = new TerrainOperation(Id(502), TerrainOperationKind.HoleCut, new SampleRect(50, 63, 1, 1));
            flat = TerrainOperationEvaluator.Build(new HeightField(129, 129, values), _grid, new[] { hole });
            Assert.That(WorldRoadPathfinder.CanWalk(from, to, _grid, flat, options, Array.Empty<PlanZone>(), Array.Empty<BridgePlan>()), Is.False);
        }
        [Test] public void PlotMarginCutFillAndNonFiniteSupportAreRejected()
        {
            var values = new ushort[129 * 129]; for (int z = 0; z < 129; z++) for (int x = 0; x < 129; x++) values[z * 129 + x] = _source[z, x];
            for (int z = 30; z <= 34; z++) for (int x = 40; x <= 42; x++) values[z * 129 + x] = 5000;
            _source = new HeightField(129, 129, values); Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.CutFillExceeded));
            _structures[0].SupportPoints[0] = new PlanPoint(double.NaN, 0, 0); Assert.Throws<ArgumentException>(() => Build());
        }
        [Test]
        public void T13_PlansSupportedPlotsRoadAndBridgeWithoutFillingWater()
        {
            var result = Build(); Assert.That(result.HasErrors, Is.False, Issues(result));
            Assert.That(result.Structures.Count, Is.EqualTo(3)); Assert.That(result.Roads.Count, Is.EqualTo(1));
            Assert.That(result.Roads[0].Samples.Any(p => (p - _bridges[0].Start).Length < .05)); Assert.That(result.Roads[0].Samples.Any(p => (p - _bridges[0].End).Length < .05));
            Assert.That(result.Terrain.GetHeight(64, 64), Is.EqualTo(2).Within(.001));
            Assert.That(result.Terrain.GetHeight(32, 32), Is.EqualTo(11).Within(.001));
            Assert.That(result.Terrain.GetHeight(64, 56), Is.EqualTo(12).Within(.05));
            Assert.That(result.Reservations.Count, Is.GreaterThan(3));
        }
        [Test]
        public void RepeatedAndReorderedInputsKeepCanonicalResultAndLockedPoi()
        {
            var first = Build(); Assert.That(first.HasErrors, Is.False, Issues(first)); string hash = first.ComputeHash();
            Assert.That(Build().ComputeHash(), Is.EqualTo(hash)); _structures.Reverse(); _regions.Reverse(); _pois.Reverse();
            Assert.That(Build().ComputeHash(), Is.EqualTo(hash)); Assert.That(_pois.Single(p => p.guid == Id(3)).anchor.x, Is.EqualTo(32));
        }
        [Test]
        public void PlotProposalReadsInteriorAndRejectsExcessiveCutFill()
        {
            var placement = new PlacementRecord { x = 32, z = 32, halfX = 4, halfZ = 4 };
            Assert.That(WorldPlanningEngine.ProposeElevation(placement, _grid, TerrainOperationEvaluator.Build(_source, _grid, _operations)), Is.EqualTo(10).Within(.001));
            _pois[0].anchor.y = 20; var result = Build(); Assert.That(result.Diagnostics.Any(d => d.issue == PlanningIssue.CutFillExceeded));
        }
        [Test]
        public void MissingWaterSpanAndClearanceAreExplicitErrors()
        {
            _bridges[0].HasConfirmedWaterLevel = false; Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.MissingWaterPlan));
            _bridges[0].HasConfirmedWaterLevel = true; _bridges[0].PresetSpan = 2; Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.InvalidSpan));
            _bridges[0].PresetSpan = 16; _bridges[0].PlannedWaterLevel = 20; Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.InsufficientClearance));
            _bridges[0].PlannedWaterLevel = 8; _roads[0].RequiredBridgeGuid = Id(999); Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.MissingBridgePreset));
        }
        [Test]
        public void LockedPathIsNeverReplacedAndInvalidSmoothedCurveIsRejected()
        {
            _roads[0].IsLocked = true; Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.NoPath));
            _roads[0].RequiredBridgeGuid = null;
            _roads[0].Knots = RoadGeometry.CreateKnots(new[] { new PlanPoint(32, 11, 40), new PlanPoint(96, 11, 88) });
            var saved = _roads[0].Knots[0].position;
            Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.CurveRejected)); Assert.That(_roads[0].Knots[0].position.x, Is.EqualTo(saved.x));
            _roads[0].RequiredBridgeGuid = Id(7); _roads[0].Options.MaxCurvature = 1000;
            _roads[0].Knots = RoadGeometry.CreateKnots(new[] { new PlanPoint(32, 11, 40), _bridges[0].Start, new PlanPoint(56, 12, 20), new PlanPoint(72, 12, 20), _bridges[0].End, new PlanPoint(96, 11, 88) }, 0);
            Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.DisconnectedEntrance), "양 교대 방문만 하고 상판을 우회하는 고정 경로는 거절해야 합니다.");
        }
        [Test]
        public void UserProtectionAndSculptArePreservedInsteadOfSilentlyOverwritten()
        {
            _operations.Add(new TerrainOperation(Id(301), TerrainOperationKind.AddDelta, new SampleRect(32, 32, 1, 1), .5f));
            var result = Build(); Assert.That(result.HasErrors, Is.True); Assert.That(_operations.Count, Is.EqualTo(1));
            _operations.Clear(); _operations.Add(new TerrainOperation(Id(302), TerrainOperationKind.ProtectRegion, new SampleRect(32, 32, 2, 2)));
            Assert.That(Build().Diagnostics.Any(d => d.issue == PlanningIssue.ProtectedArea));
        }
        [Test]
        public void ChunkCutCopiesGlobalNormalsTangentsAndAccumulatedUv()
        {
            var options = new RoadOptions(); var points = new[] { new PlanPoint(40, 10, 20), new PlanPoint(80, 12, 20) };
            var chunks = RoadGeometry.BuildChunks(Id(8), points, options, 64, Array.Empty<BridgePlan>());
            Assert.That(chunks.Count, Is.EqualTo(2));
            for (int edge = 0; edge < 2; edge++)
            {
                var a = chunks[0].Vertices[chunks[0].Vertices.Length - 2 + edge]; var b = chunks[1].Vertices[edge];
                Assert.That(a.Position.x, Is.EqualTo(b.Position.x)); Assert.That(a.Position.z, Is.EqualTo(b.Position.z)); Assert.That(a.Position.y, Is.EqualTo(b.Position.y));
                Assert.That(a.Normal.y, Is.EqualTo(b.Normal.y)); Assert.That(a.Tangent.x, Is.EqualTo(b.Tangent.x)); Assert.That(a.V, Is.EqualTo(b.V)); Assert.That(b.V, Is.GreaterThan(0));
            }
        }
        [Test]
        public void RoadMeshEndsExactlyAtBothBridgeCaps()
        {
            var chunks = RoadGeometry.BuildChunks(Id(8), new[] { new PlanPoint(40, 12, 64), new PlanPoint(90, 12, 64) }, new RoadOptions(), 64, _bridges);
            Assert.That(chunks.Count, Is.EqualTo(2));
            Assert.That(chunks[0].Vertices[chunks[0].Vertices.Length - 1].Position.x, Is.EqualTo(56).Within(1e-8));
            Assert.That(chunks[1].Vertices[0].Position.x, Is.EqualTo(72).Within(1e-8));
        }
        [Test]
        public void SelfCrossingRegionAndDuplicateIdsAreRejectedWithoutMutation()
        {
            _regions[0].Boundary = new[] { new PlanPoint(0, 0, 0), new PlanPoint(64, 0, 128), new PlanPoint(0, 0, 128), new PlanPoint(64, 0, 0) };
            Assert.Throws<ArgumentException>(() => Build());
            _regions[0].Boundary = Rect(0, 0, 64, 128); _pois[0].guid = _pois[1].guid; Assert.Throws<ArgumentException>(() => Build());
        }
    }
}

