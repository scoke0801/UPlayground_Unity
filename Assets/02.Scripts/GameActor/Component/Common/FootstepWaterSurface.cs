using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>평면 수면의 실제 삼각형 영역과 수심으로 얕은 물의 디딤을 구분한다.</summary>
    public sealed class FootstepWaterSurface : MonoBehaviour
    {
        public MeshFilter surfaceMesh;
        public FootstepSurfaceProfileSO profile;
        [Min(0f)] public float maximumDepth = 0.55f;
        [Min(0f)] public float minimumDepth = 0.015f;
        private Vector3[] _vertices;
        private int[] _triangles;
        private Bounds _bounds;
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

        /// <summary>수면 바깥과 수면보다 높은 다리, 깊은 물을 제외한다.</summary>
        public bool TryGetPoint(Vector3 groundPoint, out Vector3 waterPoint)
        {
            waterPoint = default;
            if (_vertices == null || _triangles == null || surfaceMesh == null) return false;
            Transform meshTransform = surfaceMesh.transform;
            Vector3 local = meshTransform.InverseTransformPoint(groundPoint);
            if (local.x < _bounds.min.x || local.x > _bounds.max.x
                || local.z < _bounds.min.z || local.z > _bounds.max.z) return false;
            Vector2 p = new(local.x, local.z);
            for (int i = 0; i < _triangles.Length; i += 3)
            {
                Vector3 a = _vertices[_triangles[i]];
                Vector3 b = _vertices[_triangles[i + 1]];
                Vector3 c = _vertices[_triangles[i + 2]];
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < 0.000001f) continue;
                float u = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.y - c.z)) / denominator;
                float v = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.y - c.z)) / denominator;
                if (u < 0f || v < 0f || u + v > 1f) continue;
                waterPoint = meshTransform.TransformPoint(new Vector3(local.x, a.y * u + b.y * v + c.y * (1f - u - v), local.z));
                float depth = waterPoint.y - groundPoint.y;
                return depth >= minimumDepth && depth <= maximumDepth;
            }
            return false;
        }
    }
}
