using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>전역 삼각형을 타일 평면에서 절단해 위치·수위·UV·흐름을 함께 보간한다.</summary>
    public static class WaterMeshBuilder
    {
        public static List<WaterChunk> Build(WaterBody body, double tileSize, double sampleStep)
        {
            var triangles = new List<WaterVertex>();
            if (body.kind == WaterBodyKind.River) BuildRiver(body, sampleStep, triangles);
            else
            {
                foreach (int index in WaterGeometry.Triangulate(body.boundary))
                {
                    var p = body.boundary[index]; p.y = body.level;
                    triangles.Add(new WaterVertex { Position = p, U = p.x / body.uvRepeat, V = p.z / body.uvRepeat });
                }
            }
            var chunks = new SortedDictionary<long, WaterChunk>();
            for (int i = 0; i < triangles.Count; i += 3)
            {
                var a = triangles[i]; var b = triangles[i + 1]; var c = triangles[i + 2];
                int x0 = (int)Math.Floor(Math.Min(a.Position.x, Math.Min(b.Position.x, c.Position.x)) / tileSize);
                int x1 = (int)Math.Ceiling(Math.Max(a.Position.x, Math.Max(b.Position.x, c.Position.x)) / tileSize) - 1;
                int z0 = (int)Math.Floor(Math.Min(a.Position.z, Math.Min(b.Position.z, c.Position.z)) / tileSize);
                int z1 = (int)Math.Ceiling(Math.Max(a.Position.z, Math.Max(b.Position.z, c.Position.z)) / tileSize) - 1;
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    var polygon = new List<WaterVertex> { a, b, c };
                    polygon = Clip(polygon, x * tileSize, true, true); polygon = Clip(polygon, (x + 1) * tileSize, true, false);
                    polygon = Clip(polygon, z * tileSize, false, true); polygon = Clip(polygon, (z + 1) * tileSize, false, false);
                    if (polygon.Count < 3) continue;
                    long key = ((long)z << 32) | (uint)x;
                    if (!chunks.TryGetValue(key, out var chunk)) { chunk = new WaterChunk { BodyGuid = body.guid, TileX = x, TileZ = z }; chunks.Add(key, chunk); }
                    for (int j = 1; j < polygon.Count - 1; j++)
                    {
                        if (PlanPoint.Cross(polygon[j].Position - polygon[0].Position, polygon[j + 1].Position - polygon[0].Position).Length < 1e-10) continue;
                        int start = chunk.Vertices.Count; chunk.Vertices.Add(polygon[0]); chunk.Vertices.Add(polygon[j]); chunk.Vertices.Add(polygon[j + 1]);
                        chunk.Triangles.Add(start); chunk.Triangles.Add(start + 1); chunk.Triangles.Add(start + 2);
                    }
                }
            }
            var result = new List<WaterChunk>(); foreach (var pair in chunks) if (pair.Value.Triangles.Count > 0) result.Add(pair.Value); return result;
        }
        private static void BuildRiver(WaterBody body, double step, List<WaterVertex> triangles)
        {
            var banks = WaterGeometry.RiverBanks(body, body.width / 2); int n = body.route.Length; double arc = 0;
            for (int i = 1; i < n; i++)
            {
                double length = WaterGeometry.HorizontalLength(body.route[i] - body.route[i - 1]); int count = 1;
                var flowA = WaterGeometry.NodeFlow(body, i - 1); var flowB = WaterGeometry.NodeFlow(body, i);
                for (int j = 0; j < count; j++)
                {
                    var a = Vertex(body, PlanPoint.Lerp(banks[i - 1], banks[i], (double)j / count), 0, arc + length * j / count, flowA);
                    var b = Vertex(body, PlanPoint.Lerp(banks[2 * n - i], banks[2 * n - 1 - i], (double)j / count), 1, arc + length * j / count, flowA);
                    var c = Vertex(body, PlanPoint.Lerp(banks[i - 1], banks[i], (double)(j + 1) / count), 0, arc + length * (j + 1) / count, flowB);
                    var d = Vertex(body, PlanPoint.Lerp(banks[2 * n - i], banks[2 * n - 1 - i], (double)(j + 1) / count), 1, arc + length * (j + 1) / count, flowB);
                    triangles.Add(a); triangles.Add(c); triangles.Add(b); triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
                arc += length;
            }
        }
        private static WaterVertex Vertex(WaterBody body, PlanPoint p, double u, double arc, PlanPoint flow)
        {
            return new WaterVertex { Position = p, U = u, V = arc / body.uvRepeat, Flow = flow };
        }
        private static List<WaterVertex> Clip(List<WaterVertex> input, double boundary, bool xAxis, bool keepGreater)
        {
            var result = new List<WaterVertex>(); if (input.Count == 0) return result;
            var a = input[input.Count - 1]; double av = xAxis ? a.Position.x : a.Position.z; bool aInside = keepGreater ? av >= boundary : av <= boundary;
            foreach (var b in input)
            {
                double bv = xAxis ? b.Position.x : b.Position.z; bool bInside = keepGreater ? bv >= boundary : bv <= boundary;
                if (aInside != bInside)
                {
                    // 작은 축 좌표부터 보간해 이웃 타일에서도 반올림 순서를 맞춘다.
                    var lower = av < bv ? a : b; var upper = av < bv ? b : a;
                    double low = Math.Min(av, bv), high = Math.Max(av, bv);
                    var v = WaterVertex.Lerp(lower, upper, (boundary - low) / (high - low));
                    if (xAxis) v.Position.x = boundary; else v.Position.z = boundary; result.Add(v);
                }
                if (bInside) result.Add(b); a = b; av = bv; aInside = bInside;
            }
            return result;
        }
    }
}

