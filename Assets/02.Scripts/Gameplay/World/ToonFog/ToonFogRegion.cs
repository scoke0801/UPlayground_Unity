using UnityEngine;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Gameplay.World.ToonFog
{
    public enum ToonFogShape { Box, Sphere }

    /// <summary>경계의 월드 거리로 환경 프로필을 선택한다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ToonFogRegion : MonoBehaviour
    {
        public ToonFogProfile profile;
        public ToonFogShape shape;
        public int priority;
        public int stableId;
        [Range(0, 1)] public float weight = 1;
        [Min(0)] public float blendDistance = 15;

        private void OnEnable() { if (!ToonFogWorld.Regions.Contains(this)) ToonFogWorld.Regions.Add(this); }
        private void OnDisable() => ToonFogWorld.Regions.Remove(this);

        internal int Id => stableId != 0 ? stableId : GetInstanceID();

        /// <summary>월드 미터 기준 블렌딩을 보장하는 형상만 허용한다.</summary>
        public bool TryGetAxes(out Vector3 extents, out Vector3 x, out Vector3 y, out Vector3 z)
        {
            var matrix = transform.localToWorldMatrix;
            x = matrix.GetColumn(0);
            y = matrix.GetColumn(1);
            z = matrix.GetColumn(2);
            extents = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (!(extents.x > 1e-5f && extents.y > 1e-5f && extents.z > 1e-5f)
                || !float.IsFinite(extents.sqrMagnitude)) return false;
            x /= extents.x;
            y /= extents.y;
            z /= extents.z;
            if (Mathf.Abs(Vector3.Dot(x, y)) > 0.001f || Mathf.Abs(Vector3.Dot(x, z)) > 0.001f
                || Mathf.Abs(Vector3.Dot(y, z)) > 0.001f) return false;
            return shape == ToonFogShape.Box || Mathf.Abs(extents.x - extents.y) + Mathf.Abs(extents.x - extents.z) < extents.x * 0.001f;
        }

        internal float GetWeight(Vector3 anchor)
        {
            if (profile == null || !profile.IsValid || !float.IsFinite(weight) || weight <= 0
                || !float.IsFinite(blendDistance) || !TryGetAxes(out var extents, out var x, out var y, out var z)) return 0;
            Vector3 offset = anchor - transform.position;
            float distance;
            if (shape == ToonFogShape.Sphere) distance = offset.magnitude - extents.x;
            else
            {
                var outside = new Vector3(Mathf.Abs(Vector3.Dot(offset, x)) - extents.x,
                    Mathf.Abs(Vector3.Dot(offset, y)) - extents.y, Mathf.Abs(Vector3.Dot(offset, z)) - extents.z);
                distance = Vector3.Max(Vector3.zero, outside).magnitude;
            }
            if (distance <= 0) return Mathf.Clamp01(weight);
            return blendDistance > 0 ? Mathf.Clamp01(weight) * (1 - Mathf.SmoothStep(0, 1, distance / blendDistance)) : 0;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.8f, 1);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (shape == ToonFogShape.Box) Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 2);
            else Gizmos.DrawWireSphere(Vector3.zero, 1);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
