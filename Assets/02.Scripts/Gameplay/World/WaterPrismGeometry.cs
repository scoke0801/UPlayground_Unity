using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.World
{
    /// <summary>평면의 볼록 수면은 외곽만 남겨 시각 메시의 세분화와 광학 체적 비용을 분리한다.</summary>
    internal sealed class WaterPrismGeometry
    {
        private Vector3[] _vertices;
        private int[] _triangles;

        public int TriangleCount => _triangles != null ? _triangles.Length / 3 : 0;

        public void Rebuild(Mesh mesh)
        {
            _vertices = mesh != null ? mesh.vertices : System.Array.Empty<Vector3>();
            _triangles = mesh != null ? mesh.triangles : System.Array.Empty<int>();
            if (_triangles.Length < 6 || !IsPlanar()) return;
            var edges = new Dictionary<ulong, int>();
            for (int i = 0; i < _triangles.Length; i += 3)
            {
                CountEdge(edges, _triangles[i], _triangles[i + 1]);
                CountEdge(edges, _triangles[i + 1], _triangles[i + 2]);
                CountEdge(edges, _triangles[i + 2], _triangles[i]);
            }
            var neighbors = new Dictionary<int, List<int>>();
            int boundaryEdges = 0;
            foreach (var pair in edges)
            {
                if (pair.Value != 1) continue;
                int a = (int)(pair.Key >> 32), b = (int)(pair.Key & uint.MaxValue);
                AddNeighbor(neighbors, a, b);
                AddNeighbor(neighbors, b, a);
                boundaryEdges++;
            }
            if (boundaryEdges < 3) return;
            int first = -1;
            foreach (var pair in neighbors)
            {
                if (pair.Value.Count != 2) return;
                if (first < 0) first = pair.Key;
            }
            var polygon = new List<int>();
            int previous = -1, current = first;
            do
            {
                polygon.Add(current);
                var adjacent = neighbors[current];
                int next = adjacent[0] != previous ? adjacent[0] : adjacent[1];
                previous = current;
                current = next;
                if (polygon.Count > boundaryEdges) return;
            } while (current != first);
            // 구멍이나 여러 섬이 있으면 원본 삼각형을 그대로 사용한다.
            if (polygon.Count != boundaryEdges) return;
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; i < polygon.Count && polygon.Count > 3; i++)
                {
                    Vector3 a = _vertices[polygon[(i + polygon.Count - 1) % polygon.Count]];
                    Vector3 b = _vertices[polygon[i]];
                    Vector3 c = _vertices[polygon[(i + 1) % polygon.Count]];
                    if (Vector3.Cross((b - a).normalized, (c - b).normalized).sqrMagnitude > 0.000001f) continue;
                    polygon.RemoveAt(i);
                    removed = true;
                    break;
                }
            } while (removed);
            float sign = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector3 a = _vertices[polygon[i]], b = _vertices[polygon[(i + 1) % polygon.Count]], c = _vertices[polygon[(i + 2) % polygon.Count]];
                float cross = Vector3.Cross(b - a, c - b).y;
                if (Mathf.Abs(cross) < 0.000001f) continue;
                if (sign != 0 && Mathf.Sign(cross) != sign) return;
                sign = Mathf.Sign(cross);
            }
            var triangles = new int[(polygon.Count - 2) * 3];
            for (int i = 1; i < polygon.Count - 1; i++)
            {
                int offset = (i - 1) * 3;
                triangles[offset] = polygon[0]; triangles[offset + 1] = polygon[i]; triangles[offset + 2] = polygon[i + 1];
            }
            _triangles = triangles;
        }

        public void GetTriangle(int index, Matrix4x4 matrix, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            int start = index * 3;
            a = matrix.MultiplyPoint3x4(_vertices[_triangles[start]]);
            b = matrix.MultiplyPoint3x4(_vertices[_triangles[start + 1]]);
            c = matrix.MultiplyPoint3x4(_vertices[_triangles[start + 2]]);
        }

        private bool IsPlanar()
        {
            Vector3 a = _vertices[_triangles[0]], b = _vertices[_triangles[1]], c = _vertices[_triangles[2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            if (Mathf.Abs(normal.y) < 0.0001f) return false;
            for (int i = 0; i < _vertices.Length; i++)
                if (Mathf.Abs(Vector3.Dot(normal, _vertices[i] - a)) > 0.0001f) return false;
            return true;
        }

        private static void CountEdge(Dictionary<ulong, int> edges, int a, int b)
        {
            ulong key = ((ulong)(uint)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
            edges.TryGetValue(key, out int count);
            edges[key] = count + 1;
        }

        private static void AddNeighbor(Dictionary<int, List<int>> neighbors, int vertex, int adjacent)
        {
            if (!neighbors.TryGetValue(vertex, out var list)) neighbors[vertex] = list = new List<int>(2);
            list.Add(adjacent);
        }
    }
}
