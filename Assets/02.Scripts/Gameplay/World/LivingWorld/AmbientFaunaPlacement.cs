using System;
using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    public enum FaunaPlacementShape { Box, Sphere }
    public enum FaunaDensityMode { ManualCount, Surface, Volume }

    /// <summary>배치 볼륨·표면 샘플·제외 공간과 베이크의 유효 측정값을 보관한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(AmbientFaunaGroup))]
    public sealed class AmbientFaunaPlacement : MonoBehaviour
    {
        public FaunaPlacementShape Shape;
        public Vector3 Size = new(3, 2, 3);
        [Min(0.05f)] public float Radius = 1.5f;
        public FaunaDensityMode DensityMode;
        [Min(0)] public float Density = 1;
        [Min(1)] public int MaximumCount = 8;
        [Min(1)] public int Samples = 32;
        public int Seed = 37;
        public bool UseSurface = true;
        public LayerMask SurfaceMask = Physics.DefaultRaycastLayers;
        [Range(0, 89)] public float MaximumSlope = 40;
        [Min(0.01f)] public float FlightHeight = 0.8f;
        [Min(0.01f)] public float Clearance = 0.35f;
        [Min(0)] public float MinimumSpacing = 0.8f;
        public Collider[] Exclusions = Array.Empty<Collider>();
        [HideInInspector] public string SettingsHash, GeometryHash;
        [HideInInspector] public float ValidMeasure;
        [HideInInspector] public int AnchorCount, BakeVersion;

        /// <summary>볼륨과 단위가 유효하지 않은 저작을 베이크 전에 거절한다.</summary>
        public bool TryValidate(out string error)
        {
            error = null;
            if (!Data.World.AmbientFaunaProfileSO.IsFinite(Size.x + Size.y + Size.z + Radius + Density
                + FlightHeight + Clearance + MinimumSpacing + MaximumSlope)
                || Size.x <= 0 || Size.y <= 0 || Size.z <= 0 || Radius <= 0 || Density < 0
                || MaximumCount < 1 || Samples < 1 || Samples > 4096 || Clearance <= 0 || FlightHeight <= 0
                || MinimumSpacing < 0 || MaximumSlope < 0 || MaximumSlope >= 90)
                error = "배치 볼륨·밀도·표면 옵션이 유효하지 않습니다.";
            else if ((DensityMode == FaunaDensityMode.Surface && !UseSurface)
                || (DensityMode == FaunaDensityMode.Volume && UseSurface))
                error = "Surface 밀도는 표면 샘플, Volume 밀도는 체적 샘플을 사용해야 합니다.";
            return error == null;
        }

        /// <summary>설정 또는 제외 영역 변경을 기존 베이크와 대조한다.</summary>
        public string ComputeSettingsHash()
        {
            string value = JsonUtility.ToJson(new Settings { Shape = Shape, Size = Size, Radius = Radius,
                DensityMode = DensityMode, Density = Density, MaximumCount = MaximumCount, Samples = Samples,
                Seed = Seed, UseSurface = UseSurface, SurfaceMask = SurfaceMask, MaximumSlope = MaximumSlope,
                FlightHeight = FlightHeight, Clearance = Clearance, MinimumSpacing = MinimumSpacing });
            if (Exclusions != null)
                foreach (var exclusion in Exclusions)
                    value += exclusion == null ? "null" : JsonUtility.ToJson(exclusion.bounds) + exclusion.transform.localToWorldMatrix;
            return Hash128.Compute(value).ToString();
        }

        /// <summary>안전 구가 제외 공간에 닿으면 전체 앵커를 거절한다.</summary>
        public bool IntersectsExclusion(Vector3 center, float radius)
        {
            if (Exclusions == null) return false;
            foreach (var exclusion in Exclusions)
                if (exclusion != null && exclusion.bounds.SqrDistance(center) <= radius * radius) return true;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.5f, 0.7f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (Shape == FaunaPlacementShape.Box) Gizmos.DrawWireCube(Vector3.zero, Size);
            else Gizmos.DrawWireSphere(Vector3.zero, Radius);
        }

        [Serializable] private struct Settings
        {
            public FaunaPlacementShape Shape;
            public FaunaDensityMode DensityMode;
            public Vector3 Size;
            public float Radius, Density, MaximumSlope, FlightHeight, Clearance, MinimumSpacing;
            public int MaximumCount, Samples, Seed, SurfaceMask;
            public bool UseSurface;
        }
    }
}
