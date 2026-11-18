using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>계획 다각형·회전 footprint·곡선 표본의 공통 기하 규약.</summary>
    public static class PlanningGeometry
    {
        public static bool Contains(PlanPoint[] polygon, double x, double z)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[j]; var b = polygon[i];
                double cross = (x - a.x) * (b.z - a.z) - (z - a.z) * (b.x - a.x);
                if (Math.Abs(cross) < 1e-8 && x >= Math.Min(a.x, b.x) && x <= Math.Max(a.x, b.x) && z >= Math.Min(a.z, b.z) && z <= Math.Max(a.z, b.z)) return true;
                if ((a.z > z) != (b.z > z) && x < (b.x - a.x) * (z - a.z) / (b.z - a.z) + a.x) inside = !inside;
            }
            return inside;
        }
        public static bool SegmentsCross(PlanPoint a, PlanPoint b, PlanPoint c, PlanPoint d)
        {
            double abC = Cross(a, b, c), abD = Cross(a, b, d), cdA = Cross(c, d, a), cdB = Cross(c, d, b);
            return abC * abD < -1e-12 && cdA * cdB < -1e-12;
        }
        private static double Cross(PlanPoint a, PlanPoint b, PlanPoint p) => (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x);
        private static bool Touches(PlanPoint a, PlanPoint b, PlanPoint p) => Math.Abs(Cross(a, b, p)) < 1e-8 && p.x >= Math.Min(a.x, b.x) - 1e-8 && p.x <= Math.Max(a.x, b.x) + 1e-8 && p.z >= Math.Min(a.z, b.z) - 1e-8 && p.z <= Math.Max(a.z, b.z) + 1e-8;
        public static void ValidatePolygon(PlanPoint[] points, WorldGrid grid)
        {
            if (points == null || points.Length < 3) throw new ArgumentException("다각형에는 꼭짓점3개 이상이 필요합니다.");
            double area = 0;
            for (int i = 0; i < points.Length; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Length];
                PlacementIdentity.RequireFinite(a.x, a.z);
                if (a.x < 0 || a.z < 0 || a.x > grid.WorldSize || a.z > grid.WorldSize || (a - b).Length < 1e-8) throw new ArgumentException("다각형이 월드를 벗어나거나 중복 점이 있습니다.");
                area += a.x * b.z - b.x * a.z;
                for (int j = i + 2; j < points.Length; j++)
                    if ((j + 1) % points.Length != i && (SegmentsCross(a, b, points[j], points[(j + 1) % points.Length]) || Touches(a, b, points[j]) || Touches(a, b, points[(j + 1) % points.Length]) || Touches(points[j], points[(j + 1) % points.Length], a) || Touches(points[j], points[(j + 1) % points.Length], b))) throw new ArgumentException("자기 교차·접촉 다각형은 허용하지 않습니다.");
            }
            if (Math.Abs(area) < 1e-8) throw new ArgumentException("다각형 면적이0입니다.");
        }
        public static PlanPoint LocalToWorld(PlanPoint local, PlacementRecord record)
        {
            double angle = record.yaw * Math.PI / 180, c = Math.Cos(angle), s = Math.Sin(angle);
            return new PlanPoint(record.x + c * local.x + s * local.z, record.y + local.y, record.z - s * local.x + c * local.z);
        }
        public static PlanPoint[] Footprint(PlacementRecord p, double margin = 0)
        {
            double x = p.halfX + margin, z = p.halfZ + margin;
            return new[] { LocalToWorld(new PlanPoint(-x, 0, -z), p), LocalToWorld(new PlanPoint(x, 0, -z), p), LocalToWorld(new PlanPoint(x, 0, z), p), LocalToWorld(new PlanPoint(-x, 0, z), p) };
        }
        public static bool Overlaps(PlanPoint[] a, PlanPoint[] b)
        {
            foreach (var p in a) if (Contains(b, p.x, p.z)) return true;
            foreach (var p in b) if (Contains(a, p.x, p.z)) return true;
            for (int i = 0; i < a.Length; i++) for (int j = 0; j < b.Length; j++)
                if (SegmentsCross(a[i], a[(i + 1) % a.Length], b[j], b[(j + 1) % b.Length])) return true;
            return false;
        }
        public static double DistanceToSegment(PlanPoint p, PlanPoint a, PlanPoint b, out double t)
        {
            double dx = b.x - a.x, dz = b.z - a.z, length = dx * dx + dz * dz;
            t = length < 1e-12 ? 0 : Math.Max(0, Math.Min(1, ((p.x - a.x) * dx + (p.z - a.z) * dz) / length));
            double x = a.x + t * dx - p.x, z = a.z + t * dz - p.z;
            return Math.Sqrt(x * x + z * z);
        }
        public static bool OnBridge(PlanPoint p, BridgePlan bridge, double inset = 0)
        {
            double distance = DistanceToSegment(p, bridge.Start, bridge.End, out double t);
            return distance <= bridge.DeckWidth / 2 - inset && t >= 0 && t <= 1
                && (p.x - bridge.Start.x) * (bridge.End.x - bridge.Start.x) + (p.z - bridge.Start.z) * (bridge.End.z - bridge.Start.z) >= -1e-8
                && (p.x - bridge.End.x) * (bridge.Start.x - bridge.End.x) + (p.z - bridge.End.z) * (bridge.Start.z - bridge.End.z) >= -1e-8;
        }
        public static bool IsInWater(PlanPoint p, IReadOnlyList<BridgePlan> bridges)
        { foreach (var b in bridges) if (Contains(b.WaterZone.boundary, p.x, p.z)) return true; return false; }
        public static bool TryBridgeHeight(PlanPoint p, IReadOnlyList<BridgePlan> bridges, double inset, out double height)
        {
            foreach (var b in bridges) if (OnBridge(p, b, inset)) { DistanceToSegment(p, b.Start, b.End, out double t); height = PlanPoint.Lerp(b.Start, b.End, t).y; return true; }
            height = 0; return false;
        }
        public static double SmoothFalloff(double distance, double width)
        { if (distance <= 0) return 1; if (width <= 0 || distance >= width) return 0; double t = 1 - distance / width; return t * t * (3 - 2 * t); }
    }

    /// <summary>보호 영역·수계 횡단·경사 비용을 읽는 결정론적 A* 경로 제안.</summary>
    public static class WorldRoadPathfinder
    {
        private static readonly int[] OffsetX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] OffsetZ = { -1, -1, -1, 0, 0, 1, 1, 1 };
        public static PlanPoint[] Find(WorldGrid grid, TerrainBuildResult terrain, PlanPoint start, PlanPoint end, RoadOptions options, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges)
        {
            var surface = new PlacementTerrain(grid, terrain);
            int n = checked((int)Math.Ceiling(grid.WorldSize / options.SearchStep) + 1);
            if ((long)n * n > 5000000) throw new ArgumentException("경로 탐색 격자가 너무 큽니다.");
            int sx = (int)Math.Round(start.x / options.SearchStep), sz = (int)Math.Round(start.z / options.SearchStep);
            int ex = (int)Math.Round(end.x / options.SearchStep), ez = (int)Math.Round(end.z / options.SearchStep);
            int initial = sz * n + sx, goal = ez * n + ex;
            if (sx < 0 || sz < 0 || sx >= n || sz >= n || ex < 0 || ez < 0 || ex >= n || ez >= n) return null;
            var costs = new Dictionary<int, double>(); var parents = new Dictionary<int, int>(); var closed = new HashSet<int>();
            var queue = new MinHeap(); costs[initial] = 0; queue.Push(initial, 0); int visited = 0;
            while (queue.Count > 0)
            {
                int current = queue.Pop(); if (!closed.Add(current)) continue;
                if (++visited > options.MaxVisited) return null;
                if (current == goal) return Reconstruct(parents, current, n, grid, terrain, surface, options, start, end, zones, bridges);
                int x = current % n, z = current / n;
                var from = Point(x, z, grid, surface, options, bridges);
                for (int i = 0; i < OffsetX.Length; i++)
                {
                    int nx = x + OffsetX[i], nz = z + OffsetZ[i];
                    if (nx < 0 || nz < 0 || nx >= n || nz >= n) continue;
                    var to = Point(nx, nz, grid, surface, options, bridges);
                    if (!CanWalk(from, to, grid, terrain, options, zones, bridges)) continue;
                    if (OffsetX[i] != 0 && OffsetZ[i] != 0)
                    {
                        var a = Point(nx, z, grid, surface, options, bridges); var b = Point(x, nz, grid, surface, options, bridges);
                        if (!CanWalk(from, a, grid, terrain, options, zones, bridges) || !CanWalk(from, b, grid, terrain, options, zones, bridges)) continue;
                    }
                    int next = nz * n + nx;
                    double horizontal = Math.Sqrt(Math.Pow(to.x - from.x, 2) + Math.Pow(to.z - from.z, 2));
                    double slope = Math.Abs(to.y - from.y) / horizontal;
                    double candidate = costs[current] + horizontal * (1 + options.SlopeCost * slope + options.EarthworkCost * Math.Abs(to.y - surface.Height(to.x, to.z)));
                    if (costs.TryGetValue(next, out double old) && candidate >= old) continue;
                    costs[next] = candidate; parents[next] = current;
                    queue.Push(next, candidate + Math.Sqrt(Math.Pow(to.x - end.x, 2) + Math.Pow(to.z - end.z, 2)));
                }
            }
            return null;
        }
        private static PlanPoint Point(int x, int z, WorldGrid grid, PlacementTerrain terrain, RoadOptions options, IReadOnlyList<BridgePlan> bridges)
        {
            var p = new PlanPoint(Math.Min(grid.WorldSize, x * options.SearchStep), 0, Math.Min(grid.WorldSize, z * options.SearchStep));
            p.y = PlanningGeometry.TryBridgeHeight(p, bridges, 0, out double height) ? height : terrain.Height(p.x, p.z);
            return p;
        }
        public static bool CanWalk(PlanPoint from, PlanPoint to, WorldGrid grid, TerrainBuildResult terrain, RoadOptions options, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges)
        {
            double distance = Math.Sqrt(Math.Pow(to.x - from.x, 2) + Math.Pow(to.z - from.z, 2));
            if (distance < 1e-8) return false;
            if (Math.Atan2(Math.Abs(to.y - from.y), distance) * 180 / Math.PI > options.MaxSlope) return false;
            int steps = Math.Max(1, (int)Math.Ceiling(distance / Math.Min(grid.SampleSpacing, options.SampleStep)));
            var side = new PlanPoint(-(to.z - from.z) / distance, 0, (to.x - from.x) / distance) * (options.Width / 2);
            var strip = new[] { from + side, to + side, to - side, from - side };
            foreach (var zone in zones) if (zone.blocksRoad && PlanningGeometry.Overlaps(strip, zone.boundary)) return false;
            // 좁은 Hole도 중심/가장자리 검사선 사이로 빠져나가지 않게 셀 면적과 대조한다.
            int cells = grid.SampleCount - 1;
            double x0 = grid.WorldSize, z0 = grid.WorldSize, x1 = 0, z1 = 0;
            foreach (var corner in strip) { x0 = Math.Min(x0, corner.x); z0 = Math.Min(z0, corner.z); x1 = Math.Max(x1, corner.x); z1 = Math.Max(z1, corner.z); }
            for (int z = Math.Max(0, (int)Math.Floor(z0 / grid.SampleSpacing)); z <= Math.Min(cells - 1, (int)Math.Floor(z1 / grid.SampleSpacing)); z++)
            for (int x = Math.Max(0, (int)Math.Floor(x0 / grid.SampleSpacing)); x <= Math.Min(cells - 1, (int)Math.Floor(x1 / grid.SampleSpacing)); x++)
            {
                if (terrain.GetSurface(z, x)) continue;
                double wx = grid.SampleToWorld(x), wz = grid.SampleToWorld(z), step = grid.SampleSpacing;
                var hole = new[] { new PlanPoint(wx, 0, wz), new PlanPoint(wx + step, 0, wz), new PlanPoint(wx + step, 0, wz + step), new PlanPoint(wx, 0, wz + step) };
                if (!PlanningGeometry.Overlaps(strip, hole)) continue;
                bool supported = false;
                foreach (var bridge in bridges) if (PlanningGeometry.OnBridge(hole[0], bridge) && PlanningGeometry.OnBridge(hole[1], bridge) && PlanningGeometry.OnBridge(hole[2], bridge) && PlanningGeometry.OnBridge(hole[3], bridge)) { supported = true; break; }
                if (!supported) return false;
            }
            var surface = new PlacementTerrain(grid, terrain);
            for (int i = 0; i <= steps; i++)
            {
                var p = PlanPoint.Lerp(from, to, i / (double)steps);
                if (!CanOccupy(p, grid, terrain, zones, bridges) || !CanOccupy(p + side, grid, terrain, zones, bridges) || !CanOccupy(p - side, grid, terrain, zones, bridges)) return false;
                if (PlanningGeometry.TryBridgeHeight(p, bridges, 0, out double height)) { if (Math.Abs(height - p.y) > .05) return false; }
                else if (Math.Abs(surface.Height(p.x, p.z) - p.y) > options.MaxCutFill) return false;
            }
            return true;
        }
        private static bool CanOccupy(PlanPoint p, WorldGrid grid, TerrainBuildResult terrain, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges)
        {
            if (p.x < 0 || p.z < 0 || p.x > grid.WorldSize || p.z > grid.WorldSize) return false;
            foreach (var zone in zones) if (zone.blocksRoad && PlanningGeometry.Contains(zone.boundary, p.x, p.z)) return false;
            if (PlanningGeometry.IsInWater(p, bridges) && !PlanningGeometry.TryBridgeHeight(p, bridges, 0, out _)) return false;
            int x = Math.Min(grid.SampleCount - 2, (int)grid.WorldToSample(p.x)), z = Math.Min(grid.SampleCount - 2, (int)grid.WorldToSample(p.z));
            return terrain.GetSurface(z, x) || PlanningGeometry.TryBridgeHeight(p, bridges, 0, out _);
        }
        private static PlanPoint[] Reconstruct(Dictionary<int, int> parents, int id, int n, WorldGrid grid, TerrainBuildResult field, PlacementTerrain terrain, RoadOptions options, PlanPoint start, PlanPoint end, IReadOnlyList<PlanZone> zones, IReadOnlyList<BridgePlan> bridges)
        {
            var points = new List<PlanPoint>();
            while (true) { points.Add(Point(id % n, id / n, grid, terrain, options, bridges)); if (!parents.TryGetValue(id, out id)) break; }
            points.Reverse(); points[0] = start; points[points.Count - 1] = end;
            var simplified = new List<PlanPoint> { points[0] };
            int current = 0;
            while (current < points.Count - 1)
            {
                int next = points.Count - 1;
                while (next > current + 1 && !CanWalk(points[current], points[next], grid, field, options, zones, bridges)) next--;
                simplified.Add(points[next]); current = next;
            }
            return RoadGeometry.SplitBridgeCaps(simplified.ToArray(), bridges);
        }
        private sealed class MinHeap
        {
            private readonly List<(int id, double cost)> _values = new List<(int, double)>();
            public int Count => _values.Count;
            private static bool Less((int id, double cost) a, (int id, double cost) b) => a.cost < b.cost || (a.cost == b.cost && a.id < b.id);
            public void Push(int id, double cost)
            {
                _values.Add((id, cost)); int i = _values.Count - 1;
                while (i > 0) { int parent = (i - 1) / 2; if (!Less(_values[i], _values[parent])) break; var swap = _values[i]; _values[i] = _values[parent]; _values[parent] = swap; i = parent; }
            }
            public int Pop()
            {
                int id = _values[0].id; _values[0] = _values[_values.Count - 1]; _values.RemoveAt(_values.Count - 1); int i = 0;
                while (2 * i + 1 < _values.Count)
                {
                    int child = 2 * i + 1; if (child + 1 < _values.Count && Less(_values[child + 1], _values[child])) child++;
                    if (!Less(_values[child], _values[i])) break; var swap = _values[i]; _values[i] = _values[child]; _values[child] = swap; i = child;
                }
                return id;
            }
        }
    }
}
