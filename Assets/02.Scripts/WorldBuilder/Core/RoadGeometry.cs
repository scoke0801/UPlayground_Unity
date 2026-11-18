using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>전역 곡선에서 한 번 만든 정점·접선·UV를 타일 경계에서 함께 복제한다.</summary>
    public static class RoadGeometry
    {
        public static RoadKnot[] CreateKnots(PlanPoint[] points, double tension = 1)
        {
            if (points == null || points.Length < 2) throw new ArgumentException("경로 점2개 이상이 필요합니다.");
            var knots = new RoadKnot[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var previous = points[Math.Max(0, i - 1)]; var next = points[Math.Min(points.Length - 1, i + 1)];
                var direction = (next - previous).Normalized;
                double incoming = i == 0 ? 0 : (points[i] - previous).Length / 3 * tension;
                double outgoing = i == points.Length - 1 ? 0 : (next - points[i]).Length / 3 * tension;
                knots[i] = new RoadKnot { position = points[i], tangentIn = direction * -incoming, tangentOut = direction * outgoing };
            }
            return knots;
        }
        public static PlanPoint Evaluate(RoadKnot a, RoadKnot b, double t)
        {
            double s = 1 - t;
            return a.position * (s * s * s) + (a.position + a.tangentOut) * (3 * s * s * t) + (b.position + b.tangentIn) * (3 * s * t * t) + b.position * (t * t * t);
        }
        public static PlanPoint[] Sample(RoadKnot[] knots, double step)
        {
            if (knots == null || knots.Length < 2 || step <= 0) throw new ArgumentException("곡선 입력이 잘못됐습니다.");
            var points = new List<PlanPoint> { knots[0].position };
            for (int i = 0; i < knots.Length - 1; i++)
            {
                var a = knots[i]; var b = knots[i + 1];
                double length = a.tangentOut.Length + ((b.position + b.tangentIn) - (a.position + a.tangentOut)).Length + b.tangentIn.Length;
                int count = Math.Max(1, (int)Math.Ceiling(length / step));
                if (points.Count + count > 1000000) throw new ArgumentException("곡선 표본 한도를 초과했습니다.");
                for (int j = 1; j <= count; j++) points.Add(Evaluate(a, b, j / (double)count));
            }
            return points.ToArray();
        }
        public static PlanPoint[] SplitAtTiles(PlanPoint[] samples, double tileSize)
        {
            var result = new List<PlanPoint> { samples[0] };
            for (int i = 1; i < samples.Length; i++)
            {
                var a = samples[i - 1]; var b = samples[i]; var cuts = new List<double>();
                AddCuts(a.x, b.x, tileSize, cuts); AddCuts(a.z, b.z, tileSize, cuts); cuts.Sort(); double previous = -1;
                foreach (double t in cuts) { if (Math.Abs(t - previous) < 1e-10) continue; result.Add(PlanPoint.Lerp(a, b, t)); previous = t; }
                if ((b - result[result.Count - 1]).Length > 1e-9) result.Add(b);
            }
            return result.ToArray();
        }
        private static void AddCuts(double a, double b, double size, List<double> cuts)
        {
            if (Math.Abs(a - b) < 1e-10) return;
            for (int cell = (int)Math.Floor(Math.Min(a, b) / size) + 1; cell * size < Math.Max(a, b); cell++)
            { double t = (cell * size - a) / (b - a); if (t > 1e-10 && t < 1 - 1e-10) cuts.Add(t); }
        }
        public static List<RoadChunk> BuildChunks(string roadGuid, PlanPoint[] samples, RoadOptions options, double tileSize, IReadOnlyList<BridgePlan> bridges)
        {
            var points = SplitBridgeCaps(SplitAtTiles(samples, tileSize), bridges); var rings = new RoadVertex[points.Length * 2]; double distance = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (i > 0) distance += (points[i] - points[i - 1]).Length;
                var tangent = (points[Math.Min(points.Length - 1, i + 1)] - points[Math.Max(0, i - 1)]).Normalized;
                var side = new PlanPoint(tangent.z, 0, -tangent.x).Normalized;
                var normal = PlanPoint.Cross(tangent, side).Normalized;
                if (normal.y < 0) normal = normal * -1;
                for (int edge = 0; edge < 2; edge++) rings[2 * i + edge] = new RoadVertex { Position = points[i] + side * ((edge == 0 ? -1 : 1) * options.Width / 2) + new PlanPoint(0, options.SurfaceOffset, 0), Normal = normal, Tangent = side, U = edge, V = distance / options.UvRepeat };
            }
            var chunks = new List<RoadChunk>(); var vertices = new List<RoadVertex>(); var triangles = new List<int>(); int ownerX = -1, ownerZ = -1;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var middle = PlanPoint.Lerp(points[i], points[i + 1], .5);
                bool bridge = PlanningGeometry.TryBridgeHeight(middle, bridges, 0, out _);
                int x = (int)Math.Floor(middle.x / tileSize), z = (int)Math.Floor(middle.z / tileSize);
                if (bridge || x != ownerX || z != ownerZ)
                {
                    Flush(roadGuid, chunks, vertices, triangles, ownerX, ownerZ);
                    ownerX = x; ownerZ = z;
                }
                if (bridge) continue;
                if (vertices.Count == 0) { vertices.Add(rings[2 * i]); vertices.Add(rings[2 * i + 1]); }
                int count = vertices.Count; vertices.Add(rings[2 * i + 2]); vertices.Add(rings[2 * i + 3]);
                triangles.Add(count - 2); triangles.Add(count); triangles.Add(count - 1); triangles.Add(count - 1); triangles.Add(count); triangles.Add(count + 1);
            }
            Flush(roadGuid, chunks, vertices, triangles, ownerX, ownerZ);
            return chunks;
        }
        private static void Flush(string guid, List<RoadChunk> chunks, List<RoadVertex> vertices, List<int> triangles, int x, int z)
        {
            if (triangles.Count > 0) chunks.Add(new RoadChunk { RoadGuid = guid, TileX = x, TileZ = z, Vertices = vertices.ToArray(), Triangles = triangles.ToArray() });
            vertices.Clear(); triangles.Clear();
        }
        public static PlanPoint[] SplitBridgeCaps(PlanPoint[] samples, IReadOnlyList<BridgePlan> bridges)
        {
            var result = new List<PlanPoint> { samples[0] };
            for (int i = 1; i < samples.Length; i++)
            {
                var a = samples[i - 1]; var b = samples[i]; var cuts = new List<double>();
                foreach (var bridge in bridges)
                {
                    var direction = (bridge.End - bridge.Start).Normalized;
                    foreach (var cap in new[] { bridge.Start, bridge.End })
                    {
                        double d0 = (a.x - cap.x) * direction.x + (a.z - cap.z) * direction.z;
                        double d1 = (b.x - cap.x) * direction.x + (b.z - cap.z) * direction.z;
                        if (Math.Abs(d0 - d1) < 1e-12) continue;
                        double t = d0 / (d0 - d1); if (t <= 1e-10 || t >= 1 - 1e-10) continue;
                        var p = PlanPoint.Lerp(a, b, t);
                        if (Math.Sqrt(Math.Pow(p.x - cap.x, 2) + Math.Pow(p.z - cap.z, 2)) <= bridge.DeckWidth / 2) cuts.Add(t);
                    }
                }
                cuts.Sort(); double previous = -1;
                foreach (double t in cuts) if (Math.Abs(t - previous) > 1e-10) { result.Add(PlanPoint.Lerp(a, b, t)); previous = t; }
                result.Add(b);
            }
            return result.ToArray();
        }
    }
}
