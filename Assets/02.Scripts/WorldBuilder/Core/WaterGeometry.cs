using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>강의 수위·수심·흐름과 시각 기하가 같은 선형 종단 함수를 사용한다.</summary>
    public static class WaterGeometry
    {
        public static void Validate(WaterBody body, WorldGrid grid)
        {
            PlacementIdentity.ValidateGuid(body.guid);
            PlacementIdentity.RequireFinite(body.level, body.width, body.depth, body.shoreWidth, body.bankWidth, body.bankClearance,
                body.falloff, body.flowSpeed, body.maxCutFill, body.uvRepeat, body.queryHeight);
            if (!Enum.IsDefined(typeof(WaterBodyKind), body.kind) || body.depth <= 0 || body.shoreWidth < grid.SampleSpacing || body.bankWidth < grid.SampleSpacing
                || body.bankClearance <= 0 || body.falloff <= 0 || body.flowSpeed < 0 || body.maxCutFill <= 0 || body.uvRepeat <= 0 || body.queryHeight < 0)
                throw new ArgumentException("수계 단면·수치가 유효하지 않습니다.");
            if (body.kind == WaterBodyKind.River)
            {
                if (body.route == null || body.route.Length < 2 || body.width <= 2 * body.shoreWidth) throw new ArgumentException("강 경로와 충분한 폭이 필요합니다.");
                for (int i = 0; i < body.route.Length; i++)
                {
                    var p = body.route[i]; PlacementIdentity.RequireFinite(p.x, p.y, p.z);
                    if (p.x < 0 || p.z < 0 || p.x > grid.WorldSize || p.z > grid.WorldSize || p.y - body.depth < grid.OriginY || p.y + body.bankClearance > grid.OriginY + grid.VerticalRange)
                        throw new ArgumentException("강 경로/수위가 월드 범위를 벗어납니다.");
                    if (i == 0) continue;
                    var a = body.route[i - 1];
                    if (HorizontalLength(p - a) < 1e-6) throw new ArgumentException("강 경로의 수평 길이가0입니다.");
                    if (p.y > a.y + 1e-7) throw new ArgumentException("WB400 RiverUphill: 일반 강 수위가 하류 방향으로 역전됩니다.");
                }
                // 스트립 가장자리도 검사하므로 중심선만 교차하지 않는 급회전을 통과시키지 않는다.
                var banks = RiverBanks(body, body.width / 2 + body.bankWidth + body.falloff);
                PlanningGeometry.ValidatePolygon(banks, grid);
            }
            else
            {
                PlanningGeometry.ValidatePolygon(body.boundary, grid);
                if (body.level - body.depth < grid.OriginY || body.level + body.bankClearance > grid.OriginY + grid.VerticalRange) throw new ArgumentException("수위가 월드 고도 밖입니다.");
                if (body.kind == WaterBodyKind.Sea && !Array.Exists(body.boundary, p => p.x == 0 || p.z == 0 || p.x == grid.WorldSize || p.z == grid.WorldSize))
                    throw new ArgumentException("바다에는 명시적인 월드 경계가 필요합니다.");
            }
        }

        public static double HorizontalLength(PlanPoint p) => Math.Sqrt(p.x * p.x + p.z * p.z);
        public static PlanPoint HorizontalDirection(PlanPoint p) { double length = HorizontalLength(p); return length > 1e-12 ? new PlanPoint(p.x / length, 0, p.z / length) : new PlanPoint(); }

        /// <summary>양 끝의 cap은 열어 두며, 길이 방향 수위는 각 선분의 누적 호 길이에 선형이다.</summary>
        public static void ProjectRiver(WaterBody body, double x, double z, out double distance, out double surfaceY, out PlanPoint flow, out double arc, PlanPoint[] banks = null)
        {
            distance = double.PositiveInfinity; surfaceY = 0; flow = default; arc = 0; double length = 0;
            var point = new PlanPoint(x, 0, z);
            banks = banks ?? RiverBanks(body, body.width / 2);
            for (int i = 1; i < body.route.Length; i++)
            {
                var a = body.route[i - 1]; var b = body.route[i];
                double segmentLength = HorizontalLength(b - a);
                int n = body.route.Length;
                var leftA = banks[i - 1]; var leftB = banks[i]; var rightA = banks[2 * n - i]; var rightB = banks[2 * n - 1 - i];
                bool hit = Barycentric(point, leftA, leftB, rightA, out double wa, out double wb, out double wc);
                double fraction = wb;
                if (!hit) { hit = Barycentric(point, rightA, leftB, rightB, out wa, out wb, out wc); fraction = wb + wc; }
                if (hit)
                {
                    distance = PlanningGeometry.DistanceToSegment(point, a, b, out _); surfaceY = a.y + (b.y - a.y) * fraction;
                    flow = PlanPoint.Lerp(NodeFlow(body, i - 1), NodeFlow(body, i), fraction); arc = length + fraction * segmentLength; return;
                }
                double d = PlanningGeometry.DistanceToSegment(point, a, b, out double t);
                if (d < distance)
                { distance = d; surfaceY = a.y + (b.y - a.y) * t; flow = HorizontalDirection(b - a) * body.flowSpeed; arc = length + t * segmentLength; }
                length += segmentLength;
            }
        }
        public static PlanPoint NodeFlow(WaterBody body, int index)
        {
            int last = body.route.Length - 1;
            var before = HorizontalDirection(body.route[index == 0 ? 1 : index] - body.route[Math.Max(0, index - 1)]);
            var after = HorizontalDirection(body.route[Math.Min(last, index + 1)] - body.route[index == last ? index - 1 : index]);
            return HorizontalDirection(before + after) * body.flowSpeed;
        }
        private static bool Barycentric(PlanPoint p, PlanPoint a, PlanPoint b, PlanPoint c, out double wa, out double wb, out double wc)
        {
            double denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Math.Abs(denominator) < 1e-12) { wa = wb = wc = 0; return false; }
            wa = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / denominator;
            wb = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / denominator; wc = 1 - wa - wb;
            return wa >= -1e-9 && wb >= -1e-9 && wc >= -1e-9;
        }

        public static PlanPoint[] RiverBanks(WaterBody body, double halfWidth)
        {
            int n = body.route.Length; var polygon = new PlanPoint[n * 2];
            for (int i = 0; i < n; i++)
            {
                var before = HorizontalDirection(body.route[Math.Max(0, i - 1) == i ? i + 1 : i] - body.route[Math.Max(0, i - 1)]);
                var after = HorizontalDirection(body.route[Math.Min(n - 1, i + 1)] - body.route[i == n - 1 ? i - 1 : i]);
                var direction = HorizontalDirection(before + after);
                double dot = direction.x * after.x + direction.z * after.z;
                if (dot < .25) throw new ArgumentException("수면 스트립의 급회전/miter가 허용 범위를 넘습니다.");
                var offset = new PlanPoint(-direction.z, 0, direction.x) * (halfWidth / dot);
                polygon[i] = body.route[i] + offset; polygon[2 * n - 1 - i] = body.route[i] - offset;
            }
            return polygon;
        }

        /// <summary>경계 안쪽 signed distance, 동일 함수로 shore 단면을 계산한다.</summary>
        public static double SignedDistance(WaterBody body, double x, double z, PlanPoint[] polygon = null)
        {
            polygon = polygon ?? (body.kind == WaterBodyKind.River ? RiverBanks(body, body.width / 2) : body.boundary);
            double nearest = double.PositiveInfinity;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (body.kind == WaterBodyKind.River && (i == body.route.Length - 1 || i == polygon.Length - 1)) continue;
                nearest = Math.Min(nearest, PlanningGeometry.DistanceToSegment(new PlanPoint(x, 0, z), polygon[i], polygon[(i + 1) % polygon.Length], out _));
            }
            return PlanningGeometry.Contains(polygon, x, z) ? nearest : -nearest;
        }
        public static bool TrySample(WaterBody body, double x, double z, out double surfaceY, out double depth, out PlanPoint flow, PlanPoint[] polygon = null)
        {
            surfaceY = body.level; flow = default;
            double inside = SignedDistance(body, x, z, polygon);
            depth = body.depth * Math.Max(0, Math.Min(1, inside / body.shoreWidth));
            if (inside < -1e-7) return false;
            if (body.kind == WaterBodyKind.River) ProjectRiver(body, x, z, out _, out surfaceY, out flow, out _, polygon);
            return true;
        }
        public static double Area(WaterBody body)
        {
            var polygon = body.kind == WaterBodyKind.River ? RiverBanks(body, body.width / 2) : body.boundary;
            double area = 0; for (int i = 0; i < polygon.Length; i++) area += polygon[i].x * polygon[(i + 1) % polygon.Length].z - polygon[(i + 1) % polygon.Length].x * polygon[i].z;
            return Math.Abs(area) / 2;
        }

        /// <summary>오목 다각형을 ear clipping으로 나누어 수역 밖에 삼각형을 만들지 않는다.</summary>
        public static int[] Triangulate(PlanPoint[] polygon)
        {
            var indices = new List<int>(); double area = 0;
            for (int i = 0; i < polygon.Length; i++) area += polygon[i].x * polygon[(i + 1) % polygon.Length].z - polygon[(i + 1) % polygon.Length].x * polygon[i].z;
            for (int i = 0; i < polygon.Length; i++) indices.Add(area > 0 ? i : polygon.Length - 1 - i);
            var triangles = new List<int>(); int budget = polygon.Length * polygon.Length;
            while (indices.Count > 2 && budget-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int a = indices[(i + indices.Count - 1) % indices.Count], b = indices[i], c = indices[(i + 1) % indices.Count];
                    if (Cross(polygon[a], polygon[b], polygon[c]) <= 1e-10) continue;
                    bool contains = false;
                    foreach (int j in indices) if (j != a && j != b && j != c && InTriangle(polygon[j], polygon[a], polygon[b], polygon[c])) { contains = true; break; }
                    if (contains) continue;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b); indices.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) throw new ArgumentException("수면 다각형을 삼각형화할 수 없습니다.");
            }
            if (indices.Count > 2) throw new ArgumentException("수면 삼각형화 한도 초과");
            return triangles.ToArray();
        }
        private static double Cross(PlanPoint a, PlanPoint b, PlanPoint c) => (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
        private static bool InTriangle(PlanPoint p, PlanPoint a, PlanPoint b, PlanPoint c) => Cross(a, b, p) >= -1e-9 && Cross(b, c, p) >= -1e-9 && Cross(c, a, p) >= -1e-9;
    }
}
