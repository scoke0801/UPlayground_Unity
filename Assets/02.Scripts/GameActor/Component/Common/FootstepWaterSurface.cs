using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>
    /// 평면 수면의 실제 삼각형 영역을 보관하는 수면 등록부.
    /// 발소리용 얕은 물 판정과 수영 진입용 수면 높이 조회가 같은 메시를 공유한다.
    /// </summary>
    public sealed class FootstepWaterSurface : MonoBehaviour
    {
        public MeshFilter surfaceMesh;
        public FootstepSurfaceProfileSO profile;
        [Min(0f)] public float maximumDepth = 0.55f;
        [Min(0f)] public float minimumDepth = 0.015f;
        private Vector3[] _vertices;
        private int[] _triangles;
        private Bounds _bounds;
        private int _lastTriangleStart;
        private static readonly List<FootstepWaterSurface> Surfaces = new();

        private void OnEnable()
        {
            RefreshMesh();
            if (_vertices != null) Surfaces.Add(this);
        }

        /// <summary>수면 메시가 교체되었을 때 영역 캐시를 다시 만든다.</summary>
        public void RefreshMesh()
        {
            _vertices = null;
            _triangles = null;
            _lastTriangleStart = 0;
            if (surfaceMesh == null || surfaceMesh.sharedMesh == null) return;
            _vertices = surfaceMesh.sharedMesh.vertices;
            _triangles = surfaceMesh.sharedMesh.triangles;
            _bounds = surfaceMesh.sharedMesh.bounds;
        }

        private void OnDisable() => Surfaces.Remove(this);

        /// <summary>지면보다 위에 있고 발이 닿을 만큼 얕은 수면만 반환한다.</summary>
        public static bool TryResolve(Vector3 groundPoint, out Vector3 waterPoint, out FootstepSurfaceProfileSO profile)
        {
            for (int i = 0; i < Surfaces.Count; i++)
            {
                FootstepWaterSurface surface = Surfaces[i];
                if (surface != null && surface.TryGetPoint(groundPoint, out waterPoint))
                {
                    profile = surface.profile;
                    return profile != null;
                }
            }
            waterPoint = default;
            profile = null;
            return false;
        }

        /// <summary>
        /// 월드 좌표 아래·위의 수면 높이를 깊이 제한 없이 반환한다. 수영 진입 판정이 쓴다.
        /// 여러 수면이 겹치면 가장 높은 수면을 택한다.
        /// </summary>
        public static bool TryGetSurfaceHeight(Vector3 worldPosition, out float surfaceY)
        {
            surfaceY = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < Surfaces.Count; i++)
            {
                FootstepWaterSurface surface = Surfaces[i];
                if (surface == null || !surface.TrySampleHeight(worldPosition, out Vector3 point)) continue;
                if (point.y <= surfaceY) continue;
                surfaceY = point.y;
                found = true;
            }
            return found;
        }

        /// <summary>수면 바깥과 수면보다 높은 다리, 깊은 물을 제외한다.</summary>
        public bool TryGetPoint(Vector3 groundPoint, out Vector3 waterPoint)
        {
            if (!TrySampleHeight(groundPoint, out waterPoint)) return false;
            float depth = waterPoint.y - groundPoint.y;
            return depth >= minimumDepth && depth <= maximumDepth;
        }

        /// <summary>XZ가 수면 삼각형 안이면 그 지점의 수면 월드 좌표를 돌려준다.</summary>
        public bool TrySampleHeight(Vector3 worldPosition, out Vector3 waterPoint)
        {
            waterPoint = default;
            if (_vertices == null || _triangles == null || surfaceMesh == null) return false;
            Transform meshTransform = surfaceMesh.transform;
            Vector3 local = meshTransform.InverseTransformPoint(worldPosition);
            if (local.x < _bounds.min.x || local.x > _bounds.max.x
                || local.z < _bounds.min.z || local.z > _bounds.max.z) return false;
            Vector2 p = new(local.x, local.z);

            // 플레이어는 프레임마다 거의 같은 삼각형 위에 있으므로 직전 적중 삼각형부터 확인한다.
            if (TrySampleTriangle(_lastTriangleStart, p, meshTransform, local, out waterPoint)) return true;
            for (int i = 0; i < _triangles.Length; i += 3)
            {
                if (i == _lastTriangleStart || !TrySampleTriangle(i, p, meshTransform, local, out waterPoint)) continue;
                _lastTriangleStart = i;
                return true;
            }
            return false;
        }

        private bool TrySampleTriangle(int triangleStart, Vector2 p, Transform meshTransform, Vector3 local, out Vector3 waterPoint)
        {
            waterPoint = default;
            if (triangleStart + 2 >= _triangles.Length) return false;
            Vector3 a = _vertices[_triangles[triangleStart]];
            Vector3 b = _vertices[_triangles[triangleStart + 1]];
            Vector3 c = _vertices[_triangles[triangleStart + 2]];
            float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(denominator) < 0.000001f) return false;
            float u = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.y - c.z)) / denominator;
            float v = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.y - c.z)) / denominator;
            if (u < 0f || v < 0f || u + v > 1f) return false;
            waterPoint = meshTransform.TransformPoint(new Vector3(local.x, a.y * u + b.y * v + c.y * (1f - u - v), local.z));
            return true;
        }
    }
}
