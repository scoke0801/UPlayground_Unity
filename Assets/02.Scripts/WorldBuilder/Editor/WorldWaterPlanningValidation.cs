using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>P04 계획 ID를 그대로 해석하고 기존 교량·도로를 옮기지 않은 상태에서 수계 변경을 검사한다.</summary>
    internal static class WorldWaterPlanningValidation
    {
        internal static void Validate(WorldDefinition definition, WaterBuildResult result, IReadOnlyList<TerrainOperationRecord> candidateRecords = null)
        {
            var plan = definition.Planning; if (plan == null) return;
            foreach (var poi in plan.pois)
            {
                if (!poi.requiresWaterPlan && string.IsNullOrEmpty(poi.waterPlanGuid)) continue;
                var water = result.Bodies.Find(b => b.guid == poi.waterPlanGuid);
                if (water == null) { result.Reject(poi.guid, "WB411", "미해결 P04 수계 참조: " + poi.waterPlanGuid, poi.anchor); continue; }
                double level = water.level;
                if (water.kind == WaterBodyKind.River) WaterGeometry.ProjectRiver(water, poi.anchor.x, poi.anchor.z, out _, out level, out _, out _);
                if (poi.anchor.y < level + poi.floodFreeboard) result.Reject(poi.guid, "WB412", "해당 구간 실제 수위에 대한 POI 여유고 부족", poi.anchor);
            }
            foreach (var record in plan.bridges)
            {
                var bridge = record.ToCore(); var body = result.Bodies.Find(b => b.guid == bridge.WaterPlanGuid);
                if (body == null) { result.Reject(bridge.Guid, "WB411", "미해결 P04 교량 수계 참조: " + bridge.WaterPlanGuid, bridge.Start); continue; }
                int count = Math.Max(1, (int)Math.Ceiling((bridge.End - bridge.Start).Length / .5)); bool crossed = false;
                var right = WaterGeometry.HorizontalDirection(PlanPoint.Cross(new PlanPoint(0, 1, 0), bridge.End - bridge.Start));
                for (int i = 0; i <= count; i++) for (int side = -1; side <= 1; side++)
                {
                    var p = PlanPoint.Lerp(bridge.Start, bridge.End, (double)i / count) + right * (side * bridge.DeckWidth / 2);
                    if (!WaterGeometry.TrySample(body, p.x, p.z, out double level, out _, out _)) continue;
                    crossed = true;
                    if (p.y - bridge.DeckThickness < level + bridge.Clearance - 1e-5) result.Reject(bridge.Guid, "WB412", "실제 수면에 대한 교량 하부 여유고 부족", p);
                    if (!PlanningGeometry.Contains(bridge.WaterZone.boundary, p.x, p.z)) result.Reject(bridge.Guid, "WB412", "실제 수계가 계획 횡단 영역을 벗어납니다.", p);
                    if (i == 0 || i == count) result.Reject(bridge.Guid, "WB412", "교대 소켓이 실제 수역 안입니다.", p);
                }
                var middle = PlanPoint.Lerp(bridge.Start, bridge.End, .5);
                if (!crossed || !WaterGeometry.TrySample(body, middle.x, middle.z, out _, out _, out _)) result.Reject(bridge.Guid, "WB411", "교량이 연결 수역을 더 이상 횡단하지 않습니다.", middle);
            }
            if (result.HasErrors) return;
            var retained = new List<TerrainOperation>();
            if (candidateRecords != null)
            {
                foreach (var record in candidateRecords) if (record.ownerPlanGuid != plan.planGuid) retained.Add(record.ToCore(definition.SourceRevision));
            }
            else
            {
                foreach (var record in definition.Operations)
                    if (record.ownerPlanGuid != plan.planGuid && (definition.WaterSystem == null || record.ownerPlanGuid != definition.WaterSystem.systemGuid)) retained.Add(record.ToCore(definition.SourceRevision));
                foreach (var patch in result.Patches) retained.Add(patch.ToCore());
            }
            // 지형 편집 검증에서는 이미 현재 수계 패치가 retained에 필요하다.
            if (candidateRecords == null && result.Patches.Count == 0 && definition.WaterSystem != null)
                foreach (var record in definition.Operations) if (record.ownerPlanGuid == definition.WaterSystem.systemGuid) retained.Add(record.ToCore(definition.SourceRevision));
            var regions = new List<PlanRegion>(); foreach (var region in plan.regions) regions.Add(region.ToCore());
            var structures = new List<StructurePlan>(); foreach (var structure in plan.structures) structures.Add(structure.ToCore());
            bool planningChanged = candidateRecords != null && OwnedHash(candidateRecords, plan.planGuid, definition.SourceRevision) != OwnedHash(definition.Operations, plan.planGuid, definition.SourceRevision);
            var roads = new List<RoadPlan>();
            foreach (var road in plan.roads)
            {
                var copy = road.ToCore();
                if (!planningChanged) foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var preview in root.GetComponentsInChildren<WorldRoadPreview>(true))
                    if (preview.roadGuid == copy.Guid) { copy.IsLocked = true; copy.Knots = preview.appliedKnots; }
                roads.Add(copy);
            }
            var bridges = new List<BridgePlan>(); foreach (var bridge in plan.bridges) bridges.Add(bridge.ToCore());
            var fixedPlacements = new List<PlacementRecord>(); foreach (var p in definition.Placements) if (p.isFixed) fixedPlacements.Add(p.Clone());
            var grid = definition.Profile.CreateGrid();
            var validation = WorldPlanningEngine.Build(definition.WorldGuid, plan.planGuid, definition.ReadSource(), grid, definition.Profile.TileCells * grid.SampleSpacing,
                retained, regions, plan.pois, structures, roads, plan.reservations, bridges, fixedPlacements);
            foreach (var issue in validation.Diagnostics) result.Reject(issue.elementGuid, "WB413", "기존 P04 계약: " + issue.issue + " " + issue.message, default);
        }
        private static string OwnedHash(IReadOnlyList<TerrainOperationRecord> records, string owner, string revision)
        {
            var operations = new List<TerrainOperation>(); foreach (var record in records) if (record.ownerPlanGuid == owner) operations.Add(record.ToCore(revision));
            return TerrainOperationEvaluator.ComputeStackHash(operations);
        }
    }
}
