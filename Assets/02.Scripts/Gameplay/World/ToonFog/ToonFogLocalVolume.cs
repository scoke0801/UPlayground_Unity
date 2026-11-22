using UnityEngine;

namespace UPlayGround.Gameplay.World.ToonFog
{
    /// <summary>시선 구간에 실제로 밀도를 더하는 회전 상자 또는 타원체다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ToonFogLocalVolume : MonoBehaviour
    {
        public ToonFogShape shape;
        [Min(0)] public float density = 0.015f;
        public Color tint = Color.gray;
        public bool useRegionTint = true;
        [Range(0, 0.49f), Tooltip("로컬 반축 대비 경계 완화 폭입니다.")] public float edgeFeather01 = 0.15f;
        [Range(0, 1)] public float noiseAmount;
        [Min(0)] public float noiseFrequency = 0.03f;
        public Vector2 windVelocity;
        public int priority;
        public int stableId;
        [Range(0, 1)] public float weight = 1;
        [Min(0)] public float maxVisibleDistance;

        private void OnEnable() { if (!ToonFogWorld.Volumes.Contains(this)) ToonFogWorld.Volumes.Add(this); }
        private void OnDisable() => ToonFogWorld.Volumes.Remove(this);

        internal int Id => stableId != 0 ? stableId : GetInstanceID();

        /// <summary>보수적인 월드 경계와 월드 길이를 보존하는 역변환을 확인한다.</summary>
        public bool TryGetGeometry(out Bounds bounds, out Matrix4x4 inverse)
        {
            bounds = default;
            inverse = default;
            var matrix = transform.localToWorldMatrix;
            float determinant = matrix.determinant;
            if (!float.IsFinite(determinant) || Mathf.Abs(determinant) < 1e-12f) return false;
            var extents = new Vector3(GetExtent(matrix.GetRow(0)), GetExtent(matrix.GetRow(1)), GetExtent(matrix.GetRow(2)));
            bounds = new Bounds(matrix.GetColumn(3), extents * 2);
            inverse = transform.worldToLocalMatrix;
            return float.IsFinite(bounds.center.sqrMagnitude) && float.IsFinite(extents.sqrMagnitude);
        }

        private float GetExtent(Vector4 row) => shape == ToonFogShape.Sphere
            ? new Vector3(row.x, row.y, row.z).magnitude
            : Mathf.Abs(row.x) + Mathf.Abs(row.y) + Mathf.Abs(row.z);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = TryGetGeometry(out _, out _) ? new Color(0.8f, 1, 0.7f) : Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawShape(1);
            if (edgeFeather01 > 0) DrawShape(1 - edgeFeather01);
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawShape(float radius)
        {
            if (shape == ToonFogShape.Box) Gizmos.DrawWireCube(Vector3.zero, Vector3.one * (2 * radius));
            else Gizmos.DrawWireSphere(Vector3.zero, radius);
        }
    }
}
