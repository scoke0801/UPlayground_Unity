using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>검증 전에는 입력을 바꾸지 않으며 수계가 소유한 바닥/강둑 패치만 반환한다.</summary>
    public static class WaterBuildEngine
    {
        public static WaterBuildResult Build(string world, string system, HeightField source, WorldGrid grid, double tileSize,
            IReadOnlyList<TerrainOperation> retained, IReadOnlyList<WaterBody> bodies, IReadOnlyList<PlanZone> protectedZones,
            IReadOnlyList<WaterfallEdge> falls, double minimumDrop, double poolMargin, double socketTolerance)
        {
            PlacementIdentity.ValidateGuid(world); PlacementIdentity.ValidateGuid(system);
            var result = new WaterBuildResult(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var body in bodies)
            {
                if (body == null) { result.Reject(system, "WB404", "빈 수계 정의", default); continue; }
                try { WaterGeometry.Validate(body, grid); if (!ids.Add(body.guid)) throw new ArgumentException("waterGuid 중복"); result.Bodies.Add(body.Clone()); }
                catch (ArgumentException e) { result.Reject(body.guid, "WB400", e.Message, body.route != null && body.route.Length > 0 ? body.route[0] : default); }
            }
            result.Bodies.Sort((a, b) => string.CompareOrdinal(a.guid, b.guid));
            if (result.HasErrors) return result;
            var operations = new List<TerrainOperation>(retained);
            foreach (var body in result.Bodies)
            {
                var polygon = body.kind == WaterBodyKind.River ? WaterGeometry.RiverBanks(body, body.width / 2) : body.boundary;
                var patch = BuildPatch(world, system, body, polygon, grid, protectedZones, result);
                result.Patches.Add(patch); operations.Add(patch.ToCore());
            }
            result.Terrain = TerrainOperationEvaluator.Build(source, grid, operations);
            for (int i = 0; i < result.Bodies.Count; i++)
            {
                var patch = result.Patches[i]; var withoutOperations = new List<TerrainOperation>(operations);
                withoutOperations.RemoveAll(op => op.OperationGuid == patch.OperationGuid);
                // 해당 수계만 제거한 결과와 비교해 다른 수계의 변화량을 자기 한도로 오판하지 않는다.
                var withoutBody = TerrainOperationEvaluator.Rebuild(source, grid, withoutOperations, result.Terrain, patch.Bounds);
                ValidateActualChanges(result.Bodies[i], patch, grid, withoutBody, result.Terrain, retained, protectedZones, result);
            }
            foreach (var body in result.Bodies)
            {
                ValidateTerrain(body, grid, result.Terrain, result);
                result.Chunks.AddRange(WaterMeshBuilder.Build(body, tileSize, grid.SampleSpacing));
            }
            ValidateWaterfalls(result.Bodies, falls, minimumDrop, poolMargin, socketTolerance, result);
            return result;
        }

        private static GradePatch BuildPatch(string world, string system, WaterBody body, PlanPoint[] polygon, WorldGrid grid,
            IReadOnlyList<PlanZone> zones, WaterBuildResult result)
        {
            double x0 = grid.WorldSize, z0 = grid.WorldSize, x1 = 0, z1 = 0;
            foreach (var zone in zones) if (zone.blocksTerrain && PlanningGeometry.Overlaps(polygon, zone.boundary)) result.Reject(body.guid, "WB406", "수면의 보호/구조물 공간 침범: " + zone.guid, polygon[0]);
            foreach (var p in polygon) { x0 = Math.Min(x0, p.x); z0 = Math.Min(z0, p.z); x1 = Math.Max(x1, p.x); z1 = Math.Max(z1, p.z); }
            double margin = body.bankWidth + body.falloff, step = grid.SampleSpacing;
            int ix = Math.Max(0, (int)Math.Floor((x0 - margin) / step)), iz = Math.Max(0, (int)Math.Floor((z0 - margin) / step));
            var rect = new SampleRect(ix, iz, Math.Min(grid.SampleCount - 1, (int)Math.Ceiling((x1 + margin) / step)) - ix + 1,
                Math.Min(grid.SampleCount - 1, (int)Math.Ceiling((z1 + margin) / step)) - iz + 1);
            var patch = new GradePatch { ElementGuid = body.guid, OperationGuid = PlanningIdentity.Derive(world, system, body.guid, "water-bed-bank"),
                Bounds = rect, Values = new float[rect.Width * rect.Height], Masks = new float[rect.Width * rect.Height], Stage = 20 };
            for (int z = rect.Z; z < rect.Z + rect.Height; z++) for (int x = rect.X; x < rect.X + rect.Width; x++)
            {
                double wx = x * step, wz = z * step, level = body.level;
                var gradePoint = new PlanPoint(wx, 0, wz);
                if (body.kind == WaterBodyKind.River)
                {
                    var start = body.route[0]; var end = body.route[body.route.Length - 1];
                    var first = WaterGeometry.HorizontalDirection(body.route[1] - start); var last = WaterGeometry.HorizontalDirection(end - body.route[body.route.Length - 2]);
                    double beforeStart = (wx - start.x) * first.x + (wz - start.z) * first.z, afterEnd = (wx - end.x) * last.x + (wz - end.z) * last.z;
                    double rasterPadding = grid.SampleSpacing * Math.Sqrt(2);
                    if (beforeStart < -rasterPadding || afterEnd > rasterPadding) continue;
                    if (beforeStart < 0) gradePoint -= first * beforeStart;
                    if (afterEnd > 0) gradePoint -= last * afterEnd;
                }
                double distance = WaterGeometry.SignedDistance(body, gradePoint.x, gradePoint.z, polygon);
                if (distance < -margin) continue;
                if (body.kind == WaterBodyKind.River)
                {
                    WaterGeometry.ProjectRiver(body, wx, wz, out _, out level, out _, out _, polygon);
                }
                double target;
                // 대각선·열린 cap의 격자 보간에도 요구 깊이를 남기도록 한 셀 대각선의 여유를 둔다.
                double rasterShoreWidth = Math.Max(grid.SampleSpacing * .1, body.shoreWidth - grid.SampleSpacing * Math.Sqrt(2));
                if (distance >= 0) target = level - body.depth * Math.Min(1, distance / rasterShoreWidth);
                else target = level + body.bankClearance * Math.Min(1, -distance / Math.Max(grid.SampleSpacing * .1, body.bankWidth - grid.SampleSpacing * Math.Sqrt(2)));
                double mask = distance >= -body.bankWidth ? 1 : Math.Max(0, 1 - (-distance - body.bankWidth) / body.falloff);
                int index = (z - rect.Z) * rect.Width + x - rect.X; patch.Values[index] = (float)target; patch.Masks[index] = (float)mask;
            }
            return patch;
        }

        private static void ValidateActualChanges(WaterBody body, GradePatch patch, WorldGrid grid, TerrainBuildResult before, TerrainBuildResult after,
            IReadOnlyList<TerrainOperation> retained, IReadOnlyList<PlanZone> zones, WaterBuildResult result)
        {
            var bounds = patch.Bounds.Expand(DirtyDependencyGraph.GetHeightHalo(retained), grid.SampleCount, grid.SampleCount);
            for (int z = bounds.Z; z < bounds.Z + bounds.Height; z++) for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
            {
                double changed = Math.Abs(after.GetHeight(z, x) - before.GetHeight(z, x)); if (changed <= 1e-5) continue;
                var position = new PlanPoint(x * grid.SampleSpacing, after.GetHeight(z, x), z * grid.SampleSpacing);
                if (changed > body.maxCutFill + 1e-5) result.Reject(body.guid, "WB405", "falloff/Smooth halo 포함 실제 절토/성토 한도 초과", position);
                foreach (var zone in zones) if (zone.blocksTerrain && PlanningGeometry.Contains(zone.boundary, position.x, position.z)) result.Reject(body.guid, "WB406", "지형 변경이 보호/구조물 영역에 침범: " + zone.guid, position);
                foreach (var op in retained)
                {
                    if (!op.Enabled || op.Kind != TerrainOperationKind.ProtectRegion || (op.Protection & TerrainProtection.Height) == 0) continue;
                    if (x >= op.Bounds.X && z >= op.Bounds.Z && x < op.Bounds.X + op.Bounds.Width && z < op.Bounds.Z + op.Bounds.Height && op.GetMask((z - op.Bounds.Z) * op.Bounds.Width + x - op.Bounds.X) > .5)
                        result.Reject(body.guid, "WB406", "보호 연산 침범: " + op.OperationGuid, position);
                }
            }
        }

        /// <summary>최종 스택을 검사하므로 사용자 Sculpt/RAW와 다른 소유자의 패치를 숨기지 않는다.</summary>
        public static void ValidateTerrain(WaterBody body, WorldGrid grid, TerrainBuildResult terrain, WaterBuildResult result)
        {
            var polygon = body.kind == WaterBodyKind.River ? WaterGeometry.RiverBanks(body, body.width / 2) : body.boundary;
            var sample = new PlacementTerrain(grid, terrain);
            double minX = grid.WorldSize, minZ = grid.WorldSize, maxX = 0, maxZ = 0;
            foreach (var p in polygon) { minX = Math.Min(minX, p.x); minZ = Math.Min(minZ, p.z); maxX = Math.Max(maxX, p.x); maxZ = Math.Max(maxZ, p.z); }
            double step = grid.SampleSpacing / 2;
            for (double z = Math.Max(0, minZ - body.bankWidth); z <= Math.Min(grid.WorldSize, maxZ + body.bankWidth) + 1e-7; z += step)
            for (double x = Math.Max(0, minX - body.bankWidth); x <= Math.Min(grid.WorldSize, maxX + body.bankWidth) + 1e-7; x += step)
            {
                double distance = WaterGeometry.SignedDistance(body, x, z, polygon);
                if (distance >= -1e-7 || distance < -body.bankWidth) continue;
                double level = body.level;
                if (body.kind == WaterBodyKind.River)
                {
                    var start = body.route[0]; var end = body.route[body.route.Length - 1];
                    var first = WaterGeometry.HorizontalDirection(body.route[1] - start); var last = WaterGeometry.HorizontalDirection(end - body.route[body.route.Length - 2]);
                    if ((x - start.x) * first.x + (z - start.z) * first.z < -1e-7 || (x - end.x) * last.x + (z - end.z) * last.z > 1e-7) continue;
                    WaterGeometry.ProjectRiver(body, x, z, out _, out level, out _, out _, polygon);
                }
                if (sample.Height(x, z) < level - .03)
                    result.Reject(body.guid, "WB401", "호안/강둑 전체 구간의 낮은 출구", new PlanPoint(x, level, z));
            }
            for (double z = minZ; z <= maxZ + 1e-7; z += step) for (double x = minX; x <= maxX + 1e-7; x += step)
            {
                if (!WaterGeometry.TrySample(body, x, z, out double level, out double depth, out _, polygon)) continue;
                double ground = sample.Height(x, z);
                var p = new PlanPoint(x, level, z);
                double inside = WaterGeometry.SignedDistance(body, x, z, polygon);
                if (inside >= body.shoreWidth && ground > level - body.depth + .03)
                    result.Reject(body.guid, "WB407", "요구 깊이가 부족합니다. Sculpt·연산 순서·격자 대비 shore 폭을 확인하세요.", p);
                if (ground > level + .03) result.Reject(body.guid, "WB408", "수면이 지형 속에 묻혀 있습니다.", p);
                int ix = Math.Min(grid.SampleCount - 2, (int)(x / grid.SampleSpacing)), iz = Math.Min(grid.SampleCount - 2, (int)(z / grid.SampleSpacing));
                if (!terrain.GetSurface(iz, ix)) result.Reject(body.guid, "WB409", "수계 바닥에 Hole이 있습니다. 제외 체적과 별도 바닥을 저작하세요.", p);
            }
            // 전체 경계의 외측 연결선을 검사한다. 강 양 끝은 명시적 입출구다.
            for (int i = 0; i < polygon.Length; i++)
            {
                if (body.kind == WaterBodyKind.River && (i == body.route.Length - 1 || i == polygon.Length - 1)) continue;
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Length]; int count = Math.Max(1, (int)Math.Ceiling(WaterGeometry.HorizontalLength(b - a) / step));
                var direction = WaterGeometry.HorizontalDirection(b - a); var normal = new PlanPoint(-direction.z, 0, direction.x);
                for (int j = 0; j <= count; j++)
                {
                    var p = PlanPoint.Lerp(a, b, (double)j / count); double level = body.level;
                    if (body.kind == WaterBodyKind.River) WaterGeometry.ProjectRiver(body, p.x, p.z, out _, out level, out _, out _, polygon);
                    var outside = p + normal * body.bankWidth;
                    if (PlanningGeometry.Contains(polygon, outside.x, outside.z)) outside = p - normal * body.bankWidth;
                    if (outside.x < 0 || outside.z < 0 || outside.x > grid.WorldSize || outside.z > grid.WorldSize)
                    { if (body.kind != WaterBodyKind.Sea) result.Reject(body.guid, "WB401", "강둑/호안이 월드 밖으로 열립니다.", p); continue; }
                    if (sample.Height(outside.x, outside.z) < level + body.bankClearance - .03)
                        result.Reject(body.guid, "WB401", "의도하지 않은 낮은 출구/강둑 여유고 부족", outside);
                }
            }
        }

        public static void ValidateWaterfalls(IReadOnlyList<WaterBody> bodies, IReadOnlyList<WaterfallEdge> falls, double minimumDrop, double poolMargin, double tolerance, WaterBuildResult result)
        {
            var ids = new HashSet<string>(); var graph = new Dictionary<string, List<string>>();
            foreach (var edge in falls)
            {
                try { PlacementIdentity.ValidateGuid(edge.guid); PlacementIdentity.RequireFinite(edge.width, edge.upperLip.x, edge.upperLip.y, edge.upperLip.z, edge.lowerLanding.x, edge.lowerLanding.y, edge.lowerLanding.z); }
                catch (ArgumentException e) { result.Reject(edge.guid, "WB410", e.Message, edge.upperLip); continue; }
                WaterBody upper = null, lower = null; foreach (var body in bodies) { if (body.guid == edge.upstreamGuid) upper = body; if (body.guid == edge.downstreamGuid) lower = body; }
                if (!ids.Add(edge.guid) || upper == null || lower == null || upper == lower || upper.kind != WaterBodyKind.River || edge.width <= 0 || edge.width > upper.width || edge.upperLip.y - edge.lowerLanding.y < minimumDrop)
                { result.Reject(edge.guid, "WB410", "폭포 ID/유향 연결/낙차/폭 오류", edge.upperLip); continue; }
                if (!graph.TryGetValue(upper.guid, out var links)) { links = new List<string>(); graph.Add(upper.guid, links); } links.Add(lower.guid);
                if ((upper.route[upper.route.Length - 1] - edge.upperLip).Length > tolerance || !WaterGeometry.TrySample(lower, edge.lowerLanding.x, edge.lowerLanding.z, out double level, out double depth, out _)
                    || Math.Abs(level - edge.lowerLanding.y) > tolerance || depth < poolMargin || WaterGeometry.SignedDistance(lower, edge.lowerLanding.x, edge.lowerLanding.z) < edge.width / 2 + poolMargin)
                    result.Reject(edge.guid, "WB410", "상단 강 끝/하부 pool 소켓 또는 하부 여유가 맞지 않습니다.", edge.lowerLanding);
            }
            foreach (var pair in graph) if (Cycles(pair.Key, graph, new HashSet<string>(), new HashSet<string>())) { result.Reject(pair.Key, "WB410", "폭포 유향 연결에 순환이 있습니다.", default); break; }
        }
        private static bool Cycles(string id, Dictionary<string, List<string>> graph, HashSet<string> visiting, HashSet<string> done)
        {
            if (done.Contains(id)) return false; if (!visiting.Add(id)) return true;
            if (graph.TryGetValue(id, out var links)) foreach (string child in links) if (Cycles(child, graph, visiting, done)) return true;
            visiting.Remove(id); done.Add(id); return false;
        }
    }
}
