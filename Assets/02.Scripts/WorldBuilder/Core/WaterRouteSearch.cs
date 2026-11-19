using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>침식 없이 수원·하구·경유점을 잇는 유한 비용장 제안. 확정 경로는 입력 그대로 보존한다.</summary>
    public static class WaterRouteSearch
    {
        public static PlanPoint[] Propose(WaterBody body, WorldGrid grid, TerrainBuildResult terrain, double searchStep, int maxVisited,
            IReadOnlyList<PlanZone> blockers, out WaterConflict conflict)
        {
            conflict = null;
            if (body.isRouteLocked) return (PlanPoint[])body.route.Clone();
            if (body.route == null || body.route.Length < 2 || searchStep < grid.SampleSpacing || maxVisited < 1 || grid.WorldSize / searchStep > 2048)
                throw new ArgumentException("수원·하구·유한 탐색 규격이 필요합니다.");
            var surface = new PlacementTerrain(grid, terrain); var output = new List<PlanPoint>(); int visited = 0;
            for (int leg = 1; leg < body.route.Length; leg++)
            {
                var start = body.route[leg - 1]; var end = body.route[leg];
                PlacementIdentity.RequireFinite(start.x, start.y, start.z, end.x, end.y, end.z);
                if (end.y > start.y || start.x < 0 || start.z < 0 || end.x < 0 || end.z < 0 || start.x > grid.WorldSize || start.z > grid.WorldSize || end.x > grid.WorldSize || end.z > grid.WorldSize)
                { conflict = new WaterConflict(body.guid, "WB400", "경유점이 월드 밖이거나 하류 수위가 높습니다.", end); return null; }
                int n = (int)Math.Ceiling(grid.WorldSize / searchStep) + 1;
                int first = Index(start, n, searchStep), goal = Index(end, n, searchStep);
                var costs = new Dictionary<int, double> { [first] = 0 }; var parents = new Dictionary<int, int>(); var closed = new HashSet<int>();
                var queue = new SortedSet<(double cost, int id)> { (0, first) }; bool found = false; PlanPoint last = start;
                while (queue.Count > 0 && visited < maxVisited)
                {
                    var item = queue.Min; queue.Remove(item); if (!closed.Add(item.id)) continue; visited++;
                    last = Point(item.id, n, searchStep, grid.WorldSize);
                    if (item.id == goal) { found = true; break; }
                    int ix = item.id % n, iz = item.id / n;
                    for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = ix + dx, z = iz + dz; if ((dx == 0 && dz == 0) || x < 0 || z < 0 || x >= n || z >= n) continue;
                        int id = z * n + x; if (closed.Contains(id)) continue; var next = Point(id, n, searchStep, grid.WorldSize);
                        if (Blocked(last, next, body.width / 2 + body.bankWidth + body.falloff, blockers, grid.SampleSpacing)) continue;
                        PlanningGeometry.DistanceToSegment(next, start, end, out double t);
                        double level = start.y + (end.y - start.y) * t;
                        double cutFill = Math.Abs(surface.Height(next.x, next.z) - (level - body.depth));
                        if (cutFill > body.maxCutFill) continue;
                        double cost = costs[item.id] + WaterGeometry.HorizontalLength(next - last) * (1 + cutFill);
                        if (costs.TryGetValue(id, out double old) && cost >= old) continue;
                        costs[id] = cost; parents[id] = item.id; queue.Add((cost, id));
                    }
                }
                if (!found) { conflict = new WaterConflict(body.guid, "WB402", visited >= maxVisited ? "강 탐색 한도를 초과했습니다." : "막힘/웅덩이에서 유효한 경로가 없습니다.", last); return null; }
                var points = new List<PlanPoint>(); int current = goal;
                while (true) { points.Add(Point(current, n, searchStep, grid.WorldSize)); if (current == first) break; current = parents[current]; }
                points.Reverse(); points[0] = start; points[points.Count - 1] = end;
                double length = 0; for (int i = 1; i < points.Count; i++) length += WaterGeometry.HorizontalLength(points[i] - points[i - 1]);
                if (length < 1e-6) { conflict = new WaterConflict(body.guid, "WB402", "수원과 하구가 같은 탐색 셀입니다.", start); return null; }
                double arc = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    if (i > 0) arc += WaterGeometry.HorizontalLength(points[i] - points[i - 1]);
                    var p = points[i]; p.y = start.y + (end.y - start.y) * arc / length;
                    if (leg == 1 || i > 0) output.Add(p);
                }
            }
            var proposed = body.Clone(); proposed.route = output.ToArray();
            try { WaterGeometry.Validate(proposed, grid); }
            catch (ArgumentException e) { conflict = new WaterConflict(body.guid, "WB403", "제안 경로의 폭/굽이 충돌: " + e.Message, output[0]); return null; }
            return proposed.route;
        }
        private static int Index(PlanPoint p, int n, double step) => Math.Min(n - 1, (int)Math.Round(p.z / step)) * n + Math.Min(n - 1, (int)Math.Round(p.x / step));
        private static PlanPoint Point(int id, int n, double step, double size) => new PlanPoint(Math.Min(size, id % n * step), 0, Math.Min(size, id / n * step));
        private static bool Blocked(PlanPoint a, PlanPoint b, double radius, IReadOnlyList<PlanZone> blockers, double step)
        {
            int count = Math.Max(1, (int)Math.Ceiling(WaterGeometry.HorizontalLength(b - a) / step));
            for (int i = 0; i <= count; i++)
            {
                var p = PlanPoint.Lerp(a, b, (double)i / count);
                foreach (var zone in blockers)
                {
                    if (!zone.blocksTerrain) continue;
                    if (PlanningGeometry.Contains(zone.boundary, p.x, p.z)) return true;
                    for (int j = 0; j < zone.boundary.Length; j++) if (PlanningGeometry.DistanceToSegment(p, zone.boundary[j], zone.boundary[(j + 1) % zone.boundary.Length], out _) < radius) return true;
                }
            }
            return false;
        }
    }
}
