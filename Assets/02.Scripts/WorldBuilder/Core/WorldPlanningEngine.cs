using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>부지·도로·교량의 저작 입력을 비파괴 지형 명령과 검증 결과로 평가한다.</summary>
    public static class WorldPlanningEngine
    {
        public static WorldPlanningResult Build(string worldGuid, string planGuid, HeightField source, WorldGrid grid, double tileSize,
            IReadOnlyList<TerrainOperation> userOperations, IReadOnlyList<PlanRegion> regions, IReadOnlyList<PoiPlan> pois,
            IReadOnlyList<StructurePlan> structures, IReadOnlyList<RoadPlan> roads, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges,
            IReadOnlyList<PlacementRecord> fixedPlacements)
        {
            PlacementIdentity.ValidateGuid(worldGuid); PlacementIdentity.ValidateGuid(planGuid);
            Validate(grid, tileSize, regions, pois, structures, roads, zones, bridges);
            var result = new WorldPlanningResult { Terrain = TerrainOperationEvaluator.Build(source, grid, userOperations) };
            var terrain = new PlacementTerrain(grid, result.Terrain);
            var regionMap = new Dictionary<string, PlanRegion>(StringComparer.Ordinal); foreach (var region in regions) regionMap.Add(region.Guid, region);
            var poiMap = new Dictionary<string, PoiPlan>(StringComparer.Ordinal); foreach (var poi in pois) poiMap.Add(poi.guid, poi);
            var orderedStructures = new List<StructurePlan>(structures); orderedStructures.Sort((a, b) => string.CompareOrdinal(a.Guid, b.Guid));
            var orderedBridges = new List<BridgePlan>(bridges); orderedBridges.Sort((a, b) => string.CompareOrdinal(a.Guid, b.Guid));
            var structureZones = new List<PlanZone>(zones);
            foreach (var bridge in bridges) structureZones.Add(bridge.WaterZone);
            foreach (var definition in orderedStructures) BuildStructure(worldGuid, planGuid, definition, poiMap, regionMap, grid, terrain, structureZones, fixedPlacements, result);
            foreach (var bridge in orderedBridges) BuildBridge(worldGuid, planGuid, bridge, grid, terrain, zones, fixedPlacements, result);
            if (result.HasErrors) return result;
            result.Terrain = BuildTerrain(source, grid, userOperations, result.Patches);
            ValidateBridgeGrading(orderedBridges, grid, result);
            ValidateProtectedPatches(userOperations, zones, grid, result);
            ValidateFinalSupport(orderedStructures, orderedBridges, grid, result);
            if (result.HasErrors) return result;
            var structureMap = new Dictionary<string, PlannedStructure>(StringComparer.Ordinal); foreach (var structure in result.Structures) structureMap.Add(structure.Placement.id, structure);
            var orderedRoads = new List<RoadPlan>(roads); orderedRoads.Sort((a, b) => string.CompareOrdinal(a.Guid, b.Guid));
            var roadZones = new List<PlanZone>(zones);
            foreach (var placement in fixedPlacements) roadZones.Add(new PlanZone { guid = placement.id, boundary = PlanningGeometry.Footprint(placement), blocksTerrain = false, blocksPlacement = false });
            foreach (var road in orderedRoads) BuildRoad(worldGuid, planGuid, road, grid, tileSize, structureMap, roadZones, bridges, result);
            if (result.HasErrors) return result;
            result.Terrain = BuildTerrain(source, grid, userOperations, result.Patches);
            ValidateProtectedPatches(userOperations, zones, grid, result);
            ValidateFinalSupport(orderedStructures, orderedBridges, grid, result);
            foreach (var road in result.Roads)
            {
                var options = orderedRoads.Find(r => r.Guid == road.Guid).Options;
                if (!ValidateRoad(road.Samples, options, grid, result.Terrain, zones, bridges, true, out string reason)) result.Reject(road.Guid, PlanningIssue.CurveRejected, "최종 지형: " + reason);
            }
            foreach (var structure in orderedStructures)
                if (structure.RequiresAccess && !orderedRoads.Exists(r => r.StartStructureGuid == structure.Guid || r.EndStructureGuid == structure.Guid))
                    result.Reject(structure.Guid, PlanningIssue.DisconnectedEntrance, "필수 건물에 연결된 도로가 없습니다.");
            var placementZones = new List<PlanZone>(zones); var zoneIds = new HashSet<string>(StringComparer.Ordinal); foreach (var zone in zones) zoneIds.Add(zone.guid);
            foreach (var bridge in bridges) if (zoneIds.Add(bridge.WaterZone.guid)) placementZones.Add(bridge.WaterZone);
            foreach (var zone in placementZones)
            {
                if (!zone.blocksPlacement) continue;
                double x0 = grid.WorldSize, z0 = grid.WorldSize, x1 = 0, z1 = 0;
                foreach (var p in zone.boundary) { x0 = Math.Min(x0, p.x); z0 = Math.Min(z0, p.z); x1 = Math.Max(x1, p.x); z1 = Math.Max(z1, p.z); }
                result.Reservations.Add(new PlacementReservation { guid = PlanningIdentity.Derive(worldGuid, planGuid, zone.guid, "zone-reservation"), minX = x0, minZ = z0, maxX = x1, maxZ = z1, boundary = (PlanPoint[])zone.boundary.Clone() });
            }
            result.Reservations.Sort((a, b) => string.CompareOrdinal(a.guid, b.guid));
            return result;
        }
        /// <summary>중앙 돌출을 포함한 내부 표본의 중앙값을 제안한다. 확정 고도로 자동 저장하지 않는다.</summary>
        public static double ProposeElevation(PlacementRecord placement, WorldGrid grid, TerrainBuildResult result)
        {
            var heights = new List<double>(); var terrain = new PlacementTerrain(grid, result);
            VisitFootprint(placement, grid, (x, z) => heights.Add(terrain.Height(x, z)));
            heights.Sort(); if (heights.Count == 0) throw new ArgumentException("부지 표본이 없습니다."); return heights[heights.Count / 2];
        }
        private static void BuildStructure(string world, string plan, StructurePlan definition, Dictionary<string, PoiPlan> pois, Dictionary<string, PlanRegion> regions,
            WorldGrid grid, PlacementTerrain terrain, IReadOnlyList<PlanZone> zones, IReadOnlyList<PlacementRecord> fixedPlacements, WorldPlanningResult result)
        {
            if (!pois.TryGetValue(definition.PoiGuid, out var poi) || !regions.TryGetValue(poi.regionGuid, out var region)) { result.Reject(definition.Guid, PlanningIssue.OutsideRegion, "POI/Region 참조가 없습니다."); return; }
            if (!poi.hasConfirmedElevation) { result.Reject(definition.Guid, PlanningIssue.UnconfirmedElevation, "부지 기준 고도를 먼저 확정하세요."); return; }
            if (poi.requiresWaterPlan && (!poi.hasConfirmedWaterPlan || string.IsNullOrEmpty(poi.waterPlanGuid))) { result.Reject(definition.Guid, PlanningIssue.MissingWaterPlan, "수변 POI에는 확정된 계획 수위 입력이 필요합니다."); return; }
            var placement = new PlacementRecord { id = definition.Guid, sourceIdentity = "manual:" + world + ":" + definition.Guid, prefabGuid = definition.PrefabGuid, isFixed = true, anchor = PlacementAnchor.Absolute,
                x = poi.anchor.x + definition.Offset.x, y = poi.anchor.y + definition.Offset.y + definition.PivotHeight, z = poi.anchor.z + definition.Offset.z, yaw = definition.Yaw, halfX = definition.HalfX, halfZ = definition.HalfZ, maxSupportDelta = definition.SupportTolerance };
            double floorY = placement.y - definition.PivotHeight;
            if (poi.requiresWaterPlan && floorY <= poi.plannedFloodLevel + poi.floodFreeboard) { result.Reject(definition.Guid, PlanningIssue.MissingWaterPlan, "건물 바닥이 계획 홍수 고도와 여유고보다 낮습니다."); return; }
            if (!FitsRegion(placement, region.Boundary)) { result.Reject(definition.Guid, PlanningIssue.OutsideRegion, "건물 footprint가 소속 Region 밖입니다."); return; }
            if (Conflicts(placement, zones, fixedPlacements, result)) return;
            double sum = 0, max = 0, cutFill = 0; int count = 0;
            VisitFootprint(placement, grid, (x, z) => { double slope = terrain.Slope(x, z); sum += slope; max = Math.Max(max, slope); cutFill = Math.Max(cutFill, Math.Abs(terrain.Height(x, z) - floorY)); count++; });
            if (max > definition.MaxSlope || sum / count > definition.MaxAverageSlope) { result.Reject(definition.Guid, PlanningIssue.SlopeExceeded, "부지 내부 경사가 프리셋 제한을 넘습니다."); return; }
            if (cutFill > definition.MaxCutFill) { result.Reject(definition.Guid, PlanningIssue.CutFillExceeded, "부지 절토/성토 제한을 넘습니다."); return; }
            var probe = placement.Clone(); probe.maxSupportDelta = double.MaxValue;
            var support = terrain.CheckSupport(probe);
            if (support != PlacementRejection.None) { result.Reject(definition.Guid, PlanningIssue.HoleOverlap, "부지에 Hole 또는 지지면 누락이 있습니다."); return; }
            var entrances = new PlanPoint[definition.EntranceSockets.Length]; for (int i = 0; i < entrances.Length; i++) entrances[i] = PlanningGeometry.LocalToWorld(definition.EntranceSockets[i], placement);
            result.Structures.Add(new PlannedStructure { Placement = placement, PoiGuid = poi.guid, FloorY = floorY, Entrances = entrances });
            var patch = BuildPlotPatch(world, plan, definition, placement, grid);
            ValidatePatchCutFill(patch, grid, terrain, definition.MaxCutFill, result); result.Patches.Add(patch);
            AddReservation(world, plan, placement, definition.PlotMargin, result);
        }
        private static bool FitsRegion(PlacementRecord p, PlanPoint[] boundary)
        {
            var corners = PlanningGeometry.Footprint(p);
            foreach (var corner in corners) if (!PlanningGeometry.Contains(boundary, corner.x, corner.z)) return false;
            for (int i = 0; i < corners.Length; i++) for (int j = 0; j < boundary.Length; j++)
                if (PlanningGeometry.SegmentsCross(corners[i], corners[(i + 1) % corners.Length], boundary[j], boundary[(j + 1) % boundary.Length])) return false;
            return true;
        }
        private static bool Conflicts(PlacementRecord placement, IReadOnlyList<PlanZone> zones, IReadOnlyList<PlacementRecord> fixedPlacements, WorldPlanningResult result)
        {
            var footprint = PlanningGeometry.Footprint(placement);
            foreach (var zone in zones) if (zone.blocksTerrain && PlanningGeometry.Overlaps(footprint, zone.boundary)) { result.Reject(placement.id, PlanningIssue.ProtectedArea, "보호/예약 영역과 겹칩니다."); return true; }
            foreach (var fixedPlacement in fixedPlacements) if (PlacementSpatialHash.Overlaps(placement, fixedPlacement, 0, 0)) { result.Reject(placement.id, PlanningIssue.FootprintOverlap, "수동/Pin 배치와 겹칩니다."); return true; }
            foreach (var existing in result.Structures) if (PlacementSpatialHash.Overlaps(placement, existing.Placement, 0, 0)) { result.Reject(placement.id, PlanningIssue.FootprintOverlap, "고정 구조물끼리 겹칩니다."); return true; }
            return false;
        }
        private static GradePatch BuildPlotPatch(string world, string plan, StructurePlan definition, PlacementRecord p, WorldGrid grid)
        {
            double angle = p.yaw * Math.PI / 180, c = Math.Cos(angle), s = Math.Sin(angle);
            // 회전한 부지의 AABB로 패치 영역을 다시 잡는다.
            PlacementSpatialHash.Bounds(p, definition.PlotMargin + definition.Falloff, out double x0, out double z0, out double x1, out double z1);
            var patch = CreatePatch(world, plan, definition.Guid, "plot", x0, z0, x1, z1, grid, 30);
            Fill(patch, grid, (x, z) =>
            {
                double dx = x - p.x, dz = z - p.z;
                double localX = c * dx - s * dz, localZ = s * dx + c * dz;
                double distance = Math.Max(Math.Abs(localX) - p.halfX - definition.PlotMargin, Math.Abs(localZ) - p.halfZ - definition.PlotMargin);
                return (p.y - definition.PivotHeight, PlanningGeometry.SmoothFalloff(distance, definition.Falloff));
            });
            return patch;
        }
        private static void BuildBridge(string world, string plan, BridgePlan bridge, WorldGrid grid, PlacementTerrain terrain, IReadOnlyList<PlanZone> zones,
            IReadOnlyList<PlacementRecord> fixedPlacements, WorldPlanningResult result)
        {
            if (!bridge.HasConfirmedWaterLevel || string.IsNullOrEmpty(bridge.WaterPlanGuid)) { result.Reject(bridge.Guid, PlanningIssue.MissingWaterPlan, "교량 계획 수위를 확정하세요."); return; }
            if (string.IsNullOrEmpty(bridge.PrefabGuid) || bridge.PresetSpan <= 0) { result.Reject(bridge.Guid, PlanningIssue.MissingBridgePreset, "교량 span 프리셋이 필요합니다."); return; }
            double length = Math.Sqrt(Math.Pow(bridge.End.x - bridge.Start.x, 2) + Math.Pow(bridge.End.z - bridge.Start.z, 2));
            if (Math.Abs(length - bridge.PresetSpan) > bridge.SpanTolerance) { result.Reject(bridge.Guid, PlanningIssue.InvalidSpan, "교량 소켓 거리와 프리셋 span이 다릅니다."); return; }
            if (Math.Min(bridge.Start.y, bridge.End.y) - bridge.DeckThickness < bridge.PlannedWaterLevel + bridge.Clearance) { result.Reject(bridge.Guid, PlanningIssue.InsufficientClearance, "교량 하부가 계획 수위 여유고보다 낮습니다."); return; }
            if (Math.Abs(bridge.Start.y - bridge.End.y) > .001) { result.Reject(bridge.Guid, PlanningIssue.BridgeApproachRejected, "수평 교량 프리셋은 양쪽 소켓 높이가 같아야 합니다."); return; }
            if (PlanningGeometry.Contains(bridge.WaterZone.boundary, bridge.Start.x, bridge.Start.z) || PlanningGeometry.Contains(bridge.WaterZone.boundary, bridge.End.x, bridge.End.z))
            { result.Reject(bridge.Guid, PlanningIssue.BridgeApproachRejected, "양쪽 교대 소켓이 계획 수계 밖의 지지면에 있어야 합니다."); return; }
            var middle = PlanPoint.Lerp(bridge.Start, bridge.End, .5);
            var p = new PlacementRecord { id = bridge.Guid, sourceIdentity = "manual:" + world + ":" + bridge.Guid, prefabGuid = bridge.PrefabGuid, isFixed = true, anchor = PlacementAnchor.Absolute,
                x = middle.x, y = middle.y, z = middle.z, yaw = Math.Atan2(bridge.End.x - bridge.Start.x, bridge.End.z - bridge.Start.z) * 180 / Math.PI, halfX = bridge.DeckWidth / 2, halfZ = length / 2 };
            if (Conflicts(p, zones, fixedPlacements, result)) return;
            result.Structures.Add(new PlannedStructure { Placement = p, IsBridge = true, Entrances = new[] { bridge.Start, bridge.End } });
            AddReservation(world, plan, p, 0, result);
            foreach (var endpoint in new[] { bridge.Start, bridge.End })
            {
                double width = bridge.DeckWidth / 2, radius = width + bridge.ApproachLength;
                string domain = endpoint.x == bridge.Start.x && endpoint.z == bridge.Start.z ? "bridge-start" : "bridge-end";
                var patch = CreatePatch(world, plan, bridge.Guid, domain, endpoint.x - radius, endpoint.z - radius, endpoint.x + radius, endpoint.z + radius, grid, 31);
                Fill(patch, grid, (x, z) =>
                {
                    var point = new PlanPoint(x, 0, z); if (PlanningGeometry.Contains(bridge.WaterZone.boundary, x, z)) return (0.0, 0.0);
                    double weight = PlanningGeometry.SmoothFalloff(Math.Sqrt(Math.Pow(x - endpoint.x, 2) + Math.Pow(z - endpoint.z, 2)) - width, bridge.ApproachLength);
                    if (weight > 0 && Math.Abs(terrain.Height(x, z) - endpoint.y) > bridge.MaxApproachCutFill) result.Reject(bridge.Guid, PlanningIssue.CutFillExceeded, "교량 접근부 절토/성토 제한을 넘습니다.");
                    return (endpoint.y, weight);
                });
                result.Patches.Add(patch);
            }
        }
        private static void BuildRoad(string world, string plan, RoadPlan road, WorldGrid grid, double tileSize, Dictionary<string, PlannedStructure> structures,
            IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges, WorldPlanningResult result)
        {
            if (!structures.TryGetValue(road.StartStructureGuid, out var start) || !structures.TryGetValue(road.EndStructureGuid, out var end)
                || road.StartSocket < 0 || road.EndSocket < 0 || road.StartSocket >= start.Entrances.Length || road.EndSocket >= end.Entrances.Length)
            { result.Reject(road.Guid, PlanningIssue.MissingSocket, "도로 양쪽 출입 소켓이 필요합니다."); return; }
            RoadKnot[] knots = road.Knots;
            BridgePlan requiredBridge = null;
            if (!string.IsNullOrEmpty(road.RequiredBridgeGuid))
            {
                foreach (var bridge in bridges) if (bridge.Guid == road.RequiredBridgeGuid) requiredBridge = bridge;
                if (requiredBridge == null) { result.Reject(road.Guid, PlanningIssue.MissingBridgePreset, "주동선에 지정한 필수 교량 ID가 없습니다."); return; }
            }
            var blockers = new List<PlanZone>(zones);
            foreach (var structure in structures.Values) if (!structure.IsBridge) blockers.Add(new PlanZone { guid = structure.Placement.id, boundary = PlanningGeometry.Footprint(structure.Placement), blocksTerrain = false, blocksPlacement = false });
            if (knots == null || knots.Length == 0)
            {
                if (road.IsLocked) { result.Reject(road.Guid, PlanningIssue.NoPath, "고정 도로에는 저작한 spline knot가 필요합니다."); return; }
                var searchBlockers = new List<PlanZone>(zones);
                foreach (var structure in structures.Values) if (!structure.IsBridge) searchBlockers.Add(new PlanZone { guid = structure.Placement.id, boundary = PlanningGeometry.Footprint(structure.Placement, grid.SampleSpacing), blocksTerrain = false, blocksPlacement = false });
                var points = FindRoadPoints(start.Entrances[road.StartSocket], end.Entrances[road.EndSocket], requiredBridge, grid, result.Terrain, road.Options, searchBlockers, bridges);
                if (points == null) { result.Reject(road.Guid, PlanningIssue.NoPath, "금지 영역·수계·경사 조건을 통과하는 경로가 없습니다."); return; }
                knots = RoadGeometry.CreateKnots(points);
                // 곡선 완화가 금지 영역을 가로지르면 동일 경로의 접선을 단계적으로 줄여 다시 평가한다.
                foreach (double tension in new[] { 1.0, .5, .25, 0.0 })
                {
                    knots = RoadGeometry.CreateKnots(points, tension);
                    if (requiredBridge != null) AlignBridgeKnots(knots, requiredBridge, tension);
                    if (ValidateRoad(RoadGeometry.Sample(knots, Math.Min(grid.SampleSpacing, road.Options.SampleStep)), road.Options, grid, result.Terrain, blockers, bridges, false, out _)) break;
                }
            }
            var samples = RoadGeometry.SplitBridgeCaps(RoadGeometry.Sample(knots, Math.Min(grid.SampleSpacing, road.Options.SampleStep)), bridges);
            if (requiredBridge != null && !CrossesBridge(samples, requiredBridge, road.Options.Width / 2))
            { result.Reject(road.Guid, PlanningIssue.DisconnectedEntrance, "주동선은 지정한 교량의 양쪽 소켓을 통과해야 합니다."); return; }
            if ((samples[0] - start.Entrances[road.StartSocket]).Length > .01 || (samples[samples.Length - 1] - end.Entrances[road.EndSocket]).Length > .01)
            { result.Reject(road.Guid, PlanningIssue.DisconnectedEntrance, "저작 도로의 시작/끝이 출입 소켓과 다릅니다."); return; }
            if (!ValidateRoad(samples, road.Options, grid, result.Terrain, blockers, bridges, false, out string reason)) { result.Reject(road.Guid, PlanningIssue.CurveRejected, reason); return; }
            var output = new PlannedRoad { Guid = road.Guid, Knots = knots, Samples = samples, Chunks = RoadGeometry.BuildChunks(road.Guid, samples, road.Options, tileSize, bridges) };
            result.Roads.Add(output);
            var patch = BuildRoadPatch(world, plan, road, samples, grid, bridges, result.Structures);
            ValidatePatchCutFill(patch, grid, new PlacementTerrain(grid, result.Terrain), road.Options.MaxCutFill, result); result.Patches.Add(patch);
            for (int i = 1; i < samples.Length; i++)
            {
                double margin = road.Options.ExclusionWidth / 2;
                var a = samples[i - 1]; var b = samples[i];
                result.Reservations.Add(new PlacementReservation { guid = PlanningIdentity.Derive(world, plan, road.Guid, "road-reservation-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    minX = Math.Min(a.x, b.x) - margin, minZ = Math.Min(a.z, b.z) - margin, maxX = Math.Max(a.x, b.x) + margin, maxZ = Math.Max(a.z, b.z) + margin });
            }
        }
        private static PlanPoint[] FindRoadPoints(PlanPoint start, PlanPoint end, BridgePlan required, WorldGrid grid, TerrainBuildResult terrain, RoadOptions options, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges)
        {
            if (required == null) return WorldRoadPathfinder.Find(grid, terrain, start, end, options, zones, bridges);
            bool forward = (start - required.Start).Length + (end - required.End).Length <= (start - required.End).Length + (end - required.Start).Length;
            var a = forward ? required.Start : required.End; var b = forward ? required.End : required.Start;
            var first = WorldRoadPathfinder.Find(grid, terrain, start, a, options, zones, bridges); var last = WorldRoadPathfinder.Find(grid, terrain, b, end, options, zones, bridges);
            if (first == null || last == null || !WorldRoadPathfinder.CanWalk(a, b, grid, terrain, options, zones, bridges)) return null;
            var result = new List<PlanPoint>(first); result.Add(b); for (int i = 1; i < last.Length; i++) result.Add(last[i]); return result.ToArray();
        }
        private static bool CrossesBridge(PlanPoint[] samples, BridgePlan bridge, double inset)
        {
            foreach (bool forward in new[] { true, false })
            {
                var start = forward ? bridge.Start : bridge.End; var end = forward ? bridge.End : bridge.Start;
                for (int i = 0; i < samples.Length; i++)
                {
                    if ((samples[i] - start).Length >= .05) continue;
                    for (int j = i; j < samples.Length; j++)
                    {
                        if (!PlanningGeometry.OnBridge(samples[j], bridge, inset) || Math.Abs(samples[j].y - bridge.Start.y) > .05) break;
                        if (j > i && (samples[j] - end).Length < .05) return true;
                    }
                }
            }
            return false;
        }
        private static void AlignBridgeKnots(RoadKnot[] knots, BridgePlan bridge, double tension)
        {
            for (int i = 0; i < knots.Length; i++)
            {
                if ((knots[i].position - bridge.Start).Length > .001 && (knots[i].position - bridge.End).Length > .001) continue;
                var direction = (bridge.End - bridge.Start).Normalized;
                if (i + 1 < knots.Length && (knots[i + 1].position - knots[i].position).x * direction.x + (knots[i + 1].position - knots[i].position).z * direction.z < 0) direction = direction * -1;
                knots[i].tangentIn = direction * -(i == 0 ? 0 : (knots[i].position - knots[i - 1].position).Length / 3 * tension);
                knots[i].tangentOut = direction * (i == knots.Length - 1 ? 0 : (knots[i + 1].position - knots[i].position).Length / 3 * tension);
            }
        }
        private static bool ValidateRoad(PlanPoint[] samples, RoadOptions options, WorldGrid grid, TerrainBuildResult terrain, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges, bool final, out string reason)
        {
            reason = null;
            var surface = new PlacementTerrain(grid, terrain);
            for (int i = 0; i < samples.Length; i++)
            {
                var p = samples[i]; PlacementIdentity.RequireFinite(p.x, p.y, p.z);
                if (p.x < 0 || p.z < 0 || p.x > grid.WorldSize || p.z > grid.WorldSize) { reason = "월드 밖 곡선 표본 " + i; return false; }
                bool bridge = PlanningGeometry.TryBridgeHeight(p, bridges, options.Width / 2, out double bridgeHeight);
                if (PlanningGeometry.IsInWater(p, bridges) && (!bridge || Math.Abs(p.y - bridgeHeight) > .05)) { reason = "교량 표면 이탈 " + i + " (" + p.x + "," + p.y + "," + p.z + ")"; return false; }
                if (!bridge && Math.Abs(p.y - surface.Height(p.x, p.z)) > (final ? .05 : options.MaxCutFill)) { reason = "도로 지지/절토 높이 차 " + i + " = " + Math.Abs(p.y - surface.Height(p.x, p.z)); return false; }
                if (i == 0) continue;
                if (!WorldRoadPathfinder.CanWalk(samples[i - 1], p, grid, terrain, options, zones, bridges)) { reason = "도로 폭·경사·금지 영역 이탈 " + i + " (" + p.x + "," + p.y + "," + p.z + ")"; return false; }
                if (i >= samples.Length - 1) continue;
                var a = p - samples[i - 1]; var b = samples[i + 1] - p;
                double length = (a.Length + b.Length) / 2;
                if (length < 1e-8 || Math.Acos(Math.Max(-1, Math.Min(1, (a.x * b.x + a.y * b.y + a.z * b.z) / (a.Length * b.Length)))) / length > options.MaxCurvature) { reason = "도로 곡률 제한 초과 " + i; return false; }
            }
            return true;
        }
        private static GradePatch BuildRoadPatch(string world, string plan, RoadPlan road, PlanPoint[] samples, WorldGrid grid, IReadOnlyList<BridgePlan> bridges, IReadOnlyList<PlannedStructure> structures)
        {
            double x0 = grid.WorldSize, z0 = grid.WorldSize, x1 = 0, z1 = 0, margin = road.Options.Width / 2 + road.Options.Falloff;
            foreach (var p in samples) { x0 = Math.Min(x0, p.x); z0 = Math.Min(z0, p.z); x1 = Math.Max(x1, p.x); z1 = Math.Max(z1, p.z); }
            var patch = CreatePatch(world, plan, road.Guid, "road", x0 - margin, z0 - margin, x1 + margin, z1 + margin, grid, 32);
            Fill(patch, grid, (x, z) =>
            {
                var p = new PlanPoint(x, 0, z);
                if (PlanningGeometry.IsInWater(p, bridges)) return (0.0, 0.0);
                foreach (var structure in structures) if (!structure.IsBridge && PlacementSpatialHash.Contains(structure.Placement, x, z)) return (0.0, 0.0);
                double best = double.MaxValue, height = 0;
                for (int i = 1; i < samples.Length; i++)
                {
                    double distance = PlanningGeometry.DistanceToSegment(p, samples[i - 1], samples[i], out double t);
                    if (distance >= best) continue; best = distance; height = PlanPoint.Lerp(samples[i - 1], samples[i], t).y;
                }
                return (height, PlanningGeometry.SmoothFalloff(best - road.Options.Width / 2, road.Options.Falloff));
            });
            return patch;
        }
        private static GradePatch CreatePatch(string world, string plan, string element, string domain, double x0, double z0, double x1, double z1, WorldGrid grid, int stage)
        {
            int x = Math.Max(0, (int)Math.Floor(x0 / grid.SampleSpacing)), z = Math.Max(0, (int)Math.Floor(z0 / grid.SampleSpacing));
            int right = Math.Min(grid.SampleCount - 1, (int)Math.Ceiling(x1 / grid.SampleSpacing)), top = Math.Min(grid.SampleCount - 1, (int)Math.Ceiling(z1 / grid.SampleSpacing));
            var rect = new SampleRect(x, z, right - x + 1, top - z + 1);
            return new GradePatch { ElementGuid = element, OperationGuid = PlanningIdentity.Derive(world, plan, element, domain), Bounds = rect, Values = new float[rect.Width * rect.Height], Masks = new float[rect.Width * rect.Height], Stage = stage };
        }
        private static void Fill(GradePatch patch, WorldGrid grid, Func<double, double, (double height, double mask)> evaluate)
        {
            for (int z = 0; z < patch.Bounds.Height; z++) for (int x = 0; x < patch.Bounds.Width; x++)
            {
                var value = evaluate(grid.SampleToWorld(x + patch.Bounds.X), grid.SampleToWorld(z + patch.Bounds.Z)); int index = z * patch.Bounds.Width + x;
                patch.Values[index] = (float)value.height; patch.Masks[index] = (float)value.mask;
            }
        }
        private static TerrainBuildResult BuildTerrain(HeightField source, WorldGrid grid, IReadOnlyList<TerrainOperation> operations, IReadOnlyList<GradePatch> patches)
        { var all = new List<TerrainOperation>(operations); foreach (var patch in patches) all.Add(patch.ToCore()); return TerrainOperationEvaluator.Build(source, grid, all); }
        private static void VisitFootprint(PlacementRecord p, WorldGrid grid, Action<double, double> visit)
        {
            PlacementSpatialHash.Bounds(p, 0, out double x0, out double z0, out double x1, out double z1);
            if (x0 < 0 || z0 < 0 || x1 > grid.WorldSize || z1 > grid.WorldSize) throw new ArgumentException("footprint가 월드를 벗어납니다.");
            foreach (var corner in PlanningGeometry.Footprint(p)) visit(corner.x, corner.z);
            visit(p.x, p.z);
            for (int z = (int)Math.Ceiling(z0 / grid.SampleSpacing); z * grid.SampleSpacing <= z1; z++)
            for (int x = (int)Math.Ceiling(x0 / grid.SampleSpacing); x * grid.SampleSpacing <= x1; x++)
                if (PlacementSpatialHash.Contains(p, x * grid.SampleSpacing, z * grid.SampleSpacing)) visit(x * grid.SampleSpacing, z * grid.SampleSpacing);
        }
        private static void ValidateFinalSupport(List<StructurePlan> definitions, List<BridgePlan> bridges, WorldGrid grid, WorldPlanningResult result)
        {
            var terrain = new PlacementTerrain(grid, result.Terrain);
            foreach (var structure in result.Structures)
            {
                if (structure.IsBridge)
                {
                    var bridge = bridges.Find(b => b.Guid == structure.Placement.id);
                    foreach (var socket in structure.Entrances)
                    {
                        double sideX = Math.Cos(structure.Placement.yaw * Math.PI / 180) * bridge.DeckWidth / 2, sideZ = -Math.Sin(structure.Placement.yaw * Math.PI / 180) * bridge.DeckWidth / 2;
                        int supportSteps = Math.Max(1, (int)Math.Ceiling(bridge.DeckWidth / grid.SampleSpacing));
                        for (int i = 0; i <= supportSteps; i++)
                        {
                            double side = -1 + 2.0 * i / supportSteps;
                            if (!HasSurface(result.Terrain, grid, socket.x + sideX * side, socket.z + sideZ * side) || Math.Abs(terrain.Height(socket.x + sideX * side, socket.z + sideZ * side) - socket.y) > bridge.SupportTolerance)
                                result.Reject(bridge.Guid, PlanningIssue.Unsupported, "교량 교대의 최종 지지면 고도가 소켓과 다릅니다.");
                        }
                    }
                    foreach (var road in result.Roads)
                        for (int i = 1; i < road.Samples.Length; i++)
                        {
                            var current = road.Samples[i]; var previous = road.Samples[i - 1];
                            if ((current - bridge.Start).Length > bridge.ApproachLength && (current - bridge.End).Length > bridge.ApproachLength) continue;
                            double horizontal = Math.Sqrt(Math.Pow(current.x - previous.x, 2) + Math.Pow(current.z - previous.z, 2));
                            double slope = Math.Atan2(Math.Abs(current.y - previous.y), horizontal) * 180 / Math.PI;
                            if (slope > bridge.MaxApproachSlope) result.Reject(bridge.Guid, PlanningIssue.BridgeApproachRejected, "교량 접근부 최종 경사가 제한을 넘습니다.");
                        }
                    continue;
                }
                var definition = definitions.Find(d => d.Guid == structure.Placement.id);
                VisitFootprint(structure.Placement, grid, (x, z) => { if (Math.Abs(terrain.Height(x, z) - structure.FloorY) > definition.SupportTolerance) result.Reject(definition.Guid, PlanningIssue.Unsupported, "부지 내부의 최종 지지면이 건물 바닥과 다릅니다."); });
                foreach (var support in definition.SupportPoints)
                {
                    var world = PlanningGeometry.LocalToWorld(support, structure.Placement);
                    if (world.x < 0 || world.z < 0 || world.x > grid.WorldSize || world.z > grid.WorldSize || Math.Abs(terrain.Height(world.x, world.z) - world.y) > definition.SupportTolerance)
                        result.Reject(definition.Guid, PlanningIssue.Unsupported, "명시한 건물 지지점이 지형과 맞지 않습니다.");
                }
            }
        }
        private static bool HasSurface(TerrainBuildResult terrain, WorldGrid grid, double x, double z)
        {
            if (x < 0 || z < 0 || x >= grid.WorldSize || z >= grid.WorldSize) return false;
            return terrain.GetSurface((int)grid.WorldToSample(z), (int)grid.WorldToSample(x));
        }
        private static void ValidatePatchCutFill(GradePatch patch, WorldGrid grid, PlacementTerrain terrain, double limit, WorldPlanningResult result)
        {
            for (int z = 0; z < patch.Bounds.Height; z++) for (int x = 0; x < patch.Bounds.Width; x++)
            {
                int i = z * patch.Bounds.Width + x; if (patch.Masks[i] == 0) continue;
                double delta = Math.Abs(patch.Values[i] - terrain.Height(grid.SampleToWorld(x + patch.Bounds.X), grid.SampleToWorld(z + patch.Bounds.Z))) * patch.Masks[i];
                if (delta > limit + .00001) { result.Reject(patch.ElementGuid, PlanningIssue.CutFillExceeded, "falloff를 포함한 지형 패치의 절토/성토 제한을 넘습니다."); return; }
            }
        }
        private static void ValidateBridgeGrading(List<BridgePlan> bridges, WorldGrid grid, WorldPlanningResult result)
        {
            var terrain = new PlacementTerrain(grid, result.Terrain); double step = grid.SampleSpacing;
            foreach (var bridge in bridges) foreach (var patch in result.Patches) if (patch.ElementGuid == bridge.Guid)
                for (int z = 0; z < patch.Bounds.Height; z++) for (int x = 0; x < patch.Bounds.Width; x++)
                {
                    if (patch.Masks[z * patch.Bounds.Width + x] == 0) continue;
                    double wx = grid.SampleToWorld(x + patch.Bounds.X), wz = grid.SampleToWorld(z + patch.Bounds.Z);
                    // 계획 수계 경계의 절벽은 접근 가능한 교대 바깥 지면 경사와 구분한다.
                    if (PlanningGeometry.Contains(bridge.WaterZone.boundary, wx - step, wz) || PlanningGeometry.Contains(bridge.WaterZone.boundary, wx + step, wz) || PlanningGeometry.Contains(bridge.WaterZone.boundary, wx, wz - step) || PlanningGeometry.Contains(bridge.WaterZone.boundary, wx, wz + step)) continue;
                    if (terrain.Slope(wx, wz) > bridge.MaxApproachSlope) result.Reject(bridge.Guid, PlanningIssue.BridgeApproachRejected, "교량 성토 접근부 경사가 제한을 넘습니다.");
                }
        }
        private static void ValidateProtectedPatches(IReadOnlyList<TerrainOperation> operations, IReadOnlyList<PlanZone> zones, WorldGrid grid, WorldPlanningResult result)
        {
            foreach (var patch in result.Patches) for (int z = 0; z < patch.Bounds.Height; z++) for (int x = 0; x < patch.Bounds.Width; x++)
            {
                int index = z * patch.Bounds.Width + x; if (patch.Masks[index] == 0) continue;
                int sx = x + patch.Bounds.X, sz = z + patch.Bounds.Z; double wx = grid.SampleToWorld(sx), wz = grid.SampleToWorld(sz);
                foreach (var zone in zones) if (zone.blocksTerrain && PlanningGeometry.Contains(zone.boundary, wx, wz)) { result.Reject(patch.ElementGuid, PlanningIssue.ProtectedArea, "지형 패치가 보호 영역을 침범합니다."); return; }
                foreach (var op in operations) if (op.Enabled && op.Kind == TerrainOperationKind.ProtectRegion && (op.Protection & TerrainProtection.Height) != 0 && op.Bounds.Contains(sx, sz)
                    && op.GetMask((sz - op.Bounds.Z) * op.Bounds.Width + sx - op.Bounds.X) > .5) { result.Reject(patch.ElementGuid, PlanningIssue.ProtectedArea, "사용자 높이 보호 명령이 지형 패치를 차단합니다."); return; }
            }
        }
        private static void AddReservation(string world, string plan, PlacementRecord p, double margin, WorldPlanningResult result)
        {
            PlacementSpatialHash.Bounds(p, margin, out double x0, out double z0, out double x1, out double z1);
            result.Reservations.Add(new PlacementReservation { guid = PlanningIdentity.Derive(world, plan, p.id, "reservation"), minX = x0, minZ = z0, maxX = x1, maxZ = z1 });
        }
        private static void Validate(WorldGrid grid, double tileSize, IReadOnlyList<PlanRegion> regions, IReadOnlyList<PoiPlan> pois, IReadOnlyList<StructurePlan> structures, IReadOnlyList<RoadPlan> roads, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges)
        {
            PlacementIdentity.RequireFinite(tileSize); if (tileSize <= 0) throw new ArgumentException("타일 크기가 잘못됐습니다.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            Action<string> add = id => { PlacementIdentity.ValidateGuid(id); if (!ids.Add(id)) throw new ArgumentException("계획 ID가 중복됐습니다."); };
            foreach (var region in regions) { add(region.Guid); PlanningGeometry.ValidatePolygon(region.Boundary, grid); }
            foreach (var poi in pois)
            {
                add(poi.guid); PlacementIdentity.ValidateGuid(poi.regionGuid); PlacementIdentity.RequireFinite(poi.anchor.x, poi.anchor.y, poi.anchor.z, poi.plannedFloodLevel, poi.floodFreeboard);
                PlanRegion owner = null; foreach (var region in regions) if (region.Guid == poi.regionGuid) owner = region;
                if (owner == null || !PlanningGeometry.Contains(owner.Boundary, poi.anchor.x, poi.anchor.z) || poi.floodFreeboard < 0) throw new ArgumentException("POI 앵커의 소속 Region/홍수 여유고를 확인하세요.");
            }
            foreach (var zone in zones) { add(zone.guid); PlanningGeometry.ValidatePolygon(zone.boundary, grid); }
            foreach (var s in structures)
            {
                add(s.Guid); PlacementIdentity.ValidateGuid(s.PoiGuid); PlacementIdentity.ValidateGuid(s.PrefabGuid);
                PlacementIdentity.RequireFinite(s.HalfX, s.HalfZ, s.PivotHeight, s.Yaw, s.Offset.x, s.Offset.y, s.Offset.z, s.MaxCutFill, s.MaxAverageSlope, s.MaxSlope, s.PlotMargin, s.Falloff, s.SupportTolerance);
                if (s.HalfX <= 0 || s.HalfZ <= 0 || s.MaxCutFill < 0 || s.PlotMargin < 0 || s.Falloff <= 0 || s.MaxSlope < 0 || s.MaxSlope >= 90 || s.MaxAverageSlope < 0 || s.MaxAverageSlope > s.MaxSlope || s.SupportTolerance < 0
                    || s.SupportPoints == null || s.SupportPoints.Length == 0 || s.EntranceSockets == null || s.EntranceSockets.Length == 0) throw new ArgumentException("건물 footprint·지지점·출입 소켓·제한을 확인하세요.");
                foreach (var point in s.SupportPoints) PlacementIdentity.RequireFinite(point.x, point.y, point.z);
                foreach (var point in s.EntranceSockets) PlacementIdentity.RequireFinite(point.x, point.y, point.z);
            }
            foreach (var b in bridges)
            {
                add(b.Guid); PlacementIdentity.RequireFinite(b.Start.x, b.Start.y, b.Start.z, b.End.x, b.End.y, b.End.z, b.DeckWidth, b.DeckThickness, b.PresetSpan, b.SpanTolerance, b.Clearance, b.ApproachLength, b.MaxApproachCutFill, b.SupportTolerance, b.PlannedWaterLevel, b.MaxApproachSlope);
                PlanningGeometry.ValidatePolygon(b.WaterZone.boundary, grid);
                if (b.DeckWidth <= 0 || b.DeckThickness <= 0 || b.SpanTolerance < 0 || b.Clearance < 0 || b.ApproachLength <= 0 || b.MaxApproachCutFill < 0 || b.SupportTolerance < 0 || b.MaxApproachSlope <= 0 || b.MaxApproachSlope >= 90) throw new ArgumentException("교량 프리셋 범위를 확인하세요.");
            }
            foreach (var road in roads)
            {
                add(road.Guid); if (!string.IsNullOrEmpty(road.RequiredBridgeGuid)) PlacementIdentity.ValidateGuid(road.RequiredBridgeGuid); if (road.Options == null) throw new ArgumentException("도로 프리셋이 없습니다."); var o = road.Options;
                PlacementIdentity.RequireFinite(o.Width, o.ExclusionWidth, o.Falloff, o.MaxSlope, o.MaxCutFill, o.SampleStep, o.SearchStep, o.SlopeCost, o.EarthworkCost, o.MaxCurvature, o.UvRepeat, o.SurfaceOffset);
                if (o.Width <= 0 || o.ExclusionWidth < o.Width || o.Falloff <= 0 || o.MaxSlope <= 0 || o.MaxSlope >= 90 || o.MaxCutFill < 0 || o.SampleStep <= 0 || o.SearchStep <= 0 || o.MaxVisited < 1 || o.SlopeCost < 0 || o.EarthworkCost < 0 || o.MaxCurvature <= 0 || o.UvRepeat <= 0 || o.SurfaceOffset < 0) throw new ArgumentException("도로 수치 범위를 확인하세요.");
                if (road.Knots != null) foreach (var k in road.Knots) { if (k == null) throw new ArgumentException("빈 knot입니다."); PlacementIdentity.RequireFinite(k.position.x, k.position.y, k.position.z, k.tangentIn.x, k.tangentIn.y, k.tangentIn.z, k.tangentOut.x, k.tangentOut.y, k.tangentOut.z); }
            }
        }
    }
}
