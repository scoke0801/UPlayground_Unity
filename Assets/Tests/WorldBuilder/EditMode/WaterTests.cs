using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WaterTests
    {
        private static string Id(int n) => n.ToString("x32");
        private static readonly WorldGrid Grid = new WorldGrid(128, 129, 0, 700);
        private static WaterBody River() => new WaterBody { guid = Id(1), kind = WaterBodyKind.River, width = 12, depth = 2, bankClearance = 2, bankWidth = 2,
            shoreWidth = 2, falloff = 4, maxCutFill = 30, route = new[] { new PlanPoint(64, 8, 20), new PlanPoint(64, 8, 108) } };
        private static WaterBody Lake() => new WaterBody { guid = Id(2), kind = WaterBodyKind.Lake, level = 8, depth = 2, bankClearance = 2,
            boundary = new[] { new PlanPoint(20, 0, 20), new PlanPoint(48, 0, 20), new PlanPoint(48, 0, 48), new PlanPoint(20, 0, 48) } };
        private static HeightField Source()
        { var values = new ushort[129 * 129]; for (int i = 0; i < values.Length; i++) values[i] = 936; return new HeightField(129, 129, values); }
        private static WaterBuildResult Build(WaterBody[] bodies, TerrainOperation[] operations = null, PlanZone[] protectedZones = null) =>
            WaterBuildEngine.Build(Id(100), Id(101), Source(), Grid, 64, operations ?? Array.Empty<TerrainOperation>(), bodies, protectedZones ?? Array.Empty<PlanZone>(), Array.Empty<WaterfallEdge>(), 6, 1, .05);

        [Test] public void T15_RiverDepthBanksAndLakeOutletAreValidated()
        { var result = Build(new[] { River(), Lake() }); Assert.That(result.Conflicts, Is.Empty, string.Join(" / ", result.Conflicts)); Assert.That(result.Terrain.GetHeight(60, 64), Is.EqualTo(6).Within(.001)); }

        [Test]
        public void RasterMarginsKeepSlopedBendsAndOpenEndsWithinWaterConstraints()
        {
            var river = River();
            river.route = new[] { new PlanPoint(52, 14, 16), new PlanPoint(57, 12, 37),
                new PlanPoint(69, 11, 71), new PlanPoint(67, 9, 103) };
            river.width = 14;
            river.shoreWidth = river.bankWidth = 4;
            river.shoreRasterMargin = .7;
            river.bankRasterMargin = .55;
            river.waterlineRasterMargin = .12;

            var result = Build(new[] { river });
            Assert.That(result.Conflicts, Is.Empty, string.Join(" / ", result.Conflicts));

            var exposed = new TerrainOperation(Id(103), TerrainOperationKind.SetAbsolute, new SampleRect(65, 70, 7, 5), 15, stage: 50);
            Assert.That(Build(new[] { river }, new[] { exposed }).Conflicts.Any(issue => issue.Code == "WB408"), Is.True);
        }

        [Test]
        public void RasterMarginsOutsideWorldHeightAreRejectedBeforeGrading()
        {
            var lake = Lake();
            lake.level = lake.depth + .1;
            lake.shoreWidth = lake.bankWidth = 4;
            lake.shoreRasterMargin = .7;
            var result = Build(new[] { lake });
            Assert.That(result.Conflicts.Single().Code, Is.EqualTo("WB400"));
            Assert.That(result.Patches, Is.Empty);
        }

        [Test]
        public void ExtrapolatedBankOutsideWorldHeightReturnsConflict()
        {
            var river = River();
            river.route = new[] { new PlanPoint(20, 300, 50), new PlanPoint(70, 6, 50), new PlanPoint(70, 5, 100) };
            river.width = 14;
            river.shoreWidth = river.bankWidth = 4;
            river.shoreRasterMargin = 2;
            river.waterlineRasterMargin = 1;
            var result = Build(new[] { river });
            Assert.That(result.Conflicts.Single().Code, Is.EqualTo("WB400"));
            Assert.That(result.Terrain, Is.Null);
        }
        [Test] public void UphillAndSelfIntersectionAreRejected()
        {
            var river = River(); river.route[1].y = 9; Assert.That(Build(new[] { river }).HasErrors, Is.True);
            river = River(); river.route = new[] { new PlanPoint(30, 8, 30), new PlanPoint(90, 8, 90), new PlanPoint(30, 8, 90), new PlanPoint(90, 8, 30) };
            Assert.That(Build(new[] { river }).HasErrors, Is.True);
        }
        [Test] public void InvalidLakePolygonAndUnboundedSeaAreRejected()
        {
            var lake = Lake(); lake.boundary[1] = lake.boundary[3]; Assert.That(Build(new[] { lake }).HasErrors, Is.True);
            lake = Lake(); lake.kind = WaterBodyKind.Sea; Assert.That(Build(new[] { lake }).HasErrors, Is.True);
        }
        [Test] public void SculptDepthAndOverflowFailuresDoNotOverwriteInput()
        {
            var raise = new TerrainOperation(Id(11), TerrainOperationKind.SetAbsolute, new SampleRect(63, 50, 3, 3), 9, stage: 40);
            Assert.That(Build(new[] { River() }, new[] { raise }).Conflicts.Any(c => c.Code == "WB407"), Is.True);
            var lower = new TerrainOperation(Id(12), TerrainOperationKind.SetAbsolute, new SampleRect(18, 30, 3, 3), 7, stage: 40);
            Assert.That(Build(new[] { Lake() }, new[] { lower }).Conflicts.Any(c => c.Code == "WB401"), Is.True);
            Assert.That(raise.Value, Is.EqualTo(9)); Assert.That(lower.Value, Is.EqualTo(7));
        }
        [Test] public void FalloffCutFillAndProtectionAreChecked()
        {
            var river = River(); river.maxCutFill = .1; Assert.That(Build(new[] { river }).Conflicts.Any(c => c.Code == "WB405"), Is.True);
            var protect = new TerrainOperation(Id(12), TerrainOperationKind.ProtectRegion, new SampleRect(63, 40, 3, 3));
            Assert.That(Build(new[] { River() }, new[] { protect }).Conflicts.Any(c => c.Code == "WB407"), Is.True);
        }
        [Test] public void CanonicalBodyOrderAndDirtyFullAreIdentical()
        {
            var a = Build(new[] { River(), Lake() }); var b = Build(new[] { Lake(), River() }); Assert.That(a.ComputeHash(), Is.EqualTo(b.ComputeHash()));
            var original = TerrainOperationEvaluator.Build(Source(), Grid, Array.Empty<TerrainOperation>());
            var patches = a.Patches.Select(p => p.ToCore()).ToArray(); SampleRect dirty = a.Patches[0].Bounds;
            foreach (var patch in a.Patches) dirty = SampleRect.Union(dirty, patch.Bounds);
            Assert.That(TerrainOperationEvaluator.Rebuild(Source(), Grid, patches, original, dirty).ComputeHash(), Is.EqualTo(a.Terrain.ComputeHash()));
        }
        [Test] public void ChunkPositionLevelUvAndFlowMatchAtSharedBoundary()
        {
            var river = River(); river.route[0].y = 12; river.route[1].y = 8;
            var chunks = WaterMeshBuilder.Build(river, 64, 1); var south = chunks.Where(c => c.TileZ == 0).SelectMany(c => c.Vertices).Where(v => v.Position.z == 64).ToArray();
            var north = chunks.Where(c => c.TileZ == 1).SelectMany(c => c.Vertices).Where(v => v.Position.z == 64).ToArray();
            Assert.That(south.Length, Is.GreaterThan(0));
            foreach (var v in south) Assert.That(north.Any(w => w.Position.x == v.Position.x && w.Position.y == v.Position.y && w.U == v.U && w.V == v.V && w.Flow.x == v.Flow.x && w.Flow.z == v.Flow.z), Is.True);
        }
        [Test] public void BentRiverMeshAndQueryUseIdenticalLinearSurface()
        {
            var river = River(); river.route = new[] { new PlanPoint(30, 20, 25), new PlanPoint(65, 15, 70), new PlanPoint(90, 8, 105) };
            WaterGeometry.Validate(river, Grid);
            foreach (var chunk in WaterMeshBuilder.Build(river, 64, 1))
            for (int i = 0; i < chunk.Triangles.Count; i += 3)
            {
                var a = chunk.Vertices[chunk.Triangles[i]]; var b = chunk.Vertices[chunk.Triangles[i + 1]]; var c = chunk.Vertices[chunk.Triangles[i + 2]];
                var center = (a.Position + b.Position + c.Position) * (1.0 / 3);
                Assert.That(WaterGeometry.TrySample(river, center.x, center.z, out double level, out _, out var flow), Is.True);
                Assert.That(level, Is.EqualTo(center.y).Within(1e-7));
                Assert.That(flow.x, Is.EqualTo((a.Flow.x + b.Flow.x + c.Flow.x) / 3).Within(1e-7));
            }
        }
        [Test] public void SearchIsBoundedAndLockedRouteIsNotRegenerated()
        {
            var river = River(); var terrain = TerrainOperationEvaluator.Build(Source(), Grid, Array.Empty<TerrainOperation>());
            Assert.That(WaterRouteSearch.Propose(river, Grid, terrain, 4, 1, Array.Empty<PlanZone>(), out var conflict), Is.Null); Assert.That(conflict.Code, Is.EqualTo("WB402"));
            river.isRouteLocked = true; var path = WaterRouteSearch.Propose(river, Grid, terrain, 4, 1, Array.Empty<PlanZone>(), out conflict);
            Assert.That(conflict, Is.Null); Assert.That(path, Is.EqualTo(river.route)); Assert.That(path, Is.Not.SameAs(river.route));
        }
        [Test] public void SearchRespectsMandatoryWaypointsAndBlockedRoutes()
        {
            var river = River(); var terrain = TerrainOperationEvaluator.Build(Source(), Grid, Array.Empty<TerrainOperation>());
            var path = WaterRouteSearch.Propose(river, Grid, terrain, 4, 20000, Array.Empty<PlanZone>(), out var conflict);
            Assert.That(conflict, Is.Null, conflict?.ToString()); Assert.That(path.First(), Is.EqualTo(river.route[0])); Assert.That(path.Last(), Is.EqualTo(river.route[1]));
            var blocked = new PlanZone { guid = Id(15), boundary = new[] { new PlanPoint(0, 0, 55), new PlanPoint(128, 0, 55), new PlanPoint(128, 0, 70), new PlanPoint(0, 0, 70) } };
            Assert.That(WaterRouteSearch.Propose(river, Grid, terrain, 4, 20000, new[] { blocked }, out conflict), Is.Null); Assert.That(conflict.Code, Is.EqualTo("WB402"));
        }
        [Test] public void T16_WaterfallSocketsDropPoolAndDirectionAreValidated()
        {
            var upper = River(); upper.route = new[] { new PlanPoint(34, 20, 5), new PlanPoint(34, 20, 15) };
            var lower = Lake(); var edge = new WaterfallEdge { guid = Id(20), upstreamGuid = upper.guid, downstreamGuid = lower.guid, upperLip = upper.route[1], lowerLanding = new PlanPoint(34, 8, 34), width = 4 };
            var result = new WaterBuildResult(); WaterBuildEngine.ValidateWaterfalls(new[] { upper, lower }, new[] { edge }, 6, 1, .05, result); Assert.That(result.HasErrors, Is.False);
            edge.lowerLanding.y = 9; WaterBuildEngine.ValidateWaterfalls(new[] { upper, lower }, new[] { edge }, 6, 1, .05, result); Assert.That(result.HasErrors, Is.True);
        }
        [Test] public void DuplicateWaterGuidIsConflict()
        { var a = River(); var b = Lake(); b.guid = a.guid; Assert.That(Build(new[] { a, b }).HasErrors, Is.True); }
        [Test] public void ValidDeepSculptAndProtectedBedAreNotFalseCutFillConflicts()
        {
            var body = River(); body.maxCutFill = 5;
            var sculpt = new TerrainOperation(Id(11), TerrainOperationKind.SetAbsolute, new SampleRect(64, 50, 1, 1), 0, stage: 40);
            var result = Build(new[] { body }, new[] { sculpt }); Assert.That(result.Conflicts, Is.Empty, string.Join(" / ", result.Conflicts)); Assert.That(result.Terrain.GetHeight(50, 64), Is.EqualTo(0));
            sculpt = new TerrainOperation(Id(11), TerrainOperationKind.SetAbsolute, new SampleRect(64, 50, 1, 1), 5, stage: 40);
            var protect = new TerrainOperation(Id(12), TerrainOperationKind.ProtectRegion, new SampleRect(64, 50, 1, 1));
            result = Build(new[] { body }, new[] { sculpt, protect }); Assert.That(result.Conflicts, Is.Empty, string.Join(" / ", result.Conflicts)); Assert.That(result.Terrain.GetHeight(50, 64), Is.EqualTo(5));
        }
        [Test] public void RemoteLakeInsideRiverBoundingRectangleDoesNotSpendRiverCutFillBudget()
        {
            var river = River(); river.route = new[] { new PlanPoint(20, 8, 20), new PlanPoint(100, 8, 100) }; river.maxCutFill = 5; river.shoreWidth = 4; river.bankWidth = 4;
            var lake = Lake(); lake.level = 20; lake.maxCutFill = 30;
            lake.boundary = new[] { new PlanPoint(25, 0, 80), new PlanPoint(35, 0, 80), new PlanPoint(35, 0, 90), new PlanPoint(25, 0, 90) };
            var result = Build(new[] { river, lake }); Assert.That(result.Conflicts, Is.Empty, string.Join(" / ", result.Conflicts));
        }
        [Test] public void RiverEndsKeepDepthAndNarrowRiverCannotHideSculptDamage()
        {
            var body = River(); var built = Build(new[] { body }); Assert.That(built.Terrain.GetHeight(108, 64), Is.EqualTo(6).Within(.001));
            WaterGeometry.TrySample(body, 64, 108, out _, out double depth, out _); Assert.That(depth, Is.EqualTo(2));
            body.width = 5;
            Assert.That(Build(new[] { body }).HasErrors, Is.False, "미정렬 shoulder를 가진 정상 좁은 강도 생성되어야 한다.");
            var sculpt = new TerrainOperation(Id(13), TerrainOperationKind.SetAbsolute, new SampleRect(64, 50, 1, 3), 7.9f, stage: 40);
            Assert.That(Build(new[] { body }, new[] { sculpt }).Conflicts.Any(c => c.Code == "WB407"), Is.True);
        }
    }
}
