using UnityEngine;
using UPlayGround.Components;
using Body = UPlayGround.Gameplay.World.WaterGpuRecords.Body;
using Cell = UPlayGround.Gameplay.World.WaterGpuRecords.Cell;
using DryVolume = UPlayGround.Gameplay.World.WaterGpuRecords.DryVolume;
using Stamp = UPlayGround.Gameplay.World.WaterGpuRecords.Stamp;

namespace UPlayGround.Gameplay.World
{
    /// <summary>Render Graph가 소유하는 카메라별 불변 수역 자료. GPU 구조와 16바이트 단위를 맞춘다.</summary>
    internal sealed class WaterRenderSnapshot
    {
        public const int MaxBodies = WaterGpuRecords.MaxBodies;
        public const int MaxCells = WaterGpuRecords.MaxCells;
        public const int MaxDryVolumes = WaterGpuRecords.MaxDryVolumes;

        public readonly Body[] Bodies = new Body[MaxBodies];
        public readonly Cell[] Cells = new Cell[MaxCells];
        public readonly DryVolume[] DryVolumes = new DryVolume[MaxDryVolumes];
        public readonly Stamp[] Stamps = new Stamp[WaterContactStamps.Capacity];
        public readonly UnderwaterRegion[] Regions = new UnderwaterRegion[MaxBodies];
        public readonly Mesh[] Meshes = new Mesh[MaxBodies];
        public readonly Material[] Materials = new Material[MaxBodies];
        public readonly Matrix4x4[] Matrices = new Matrix4x4[MaxBodies];
        public readonly MaterialPropertyBlock[] Properties = new MaterialPropertyBlock[MaxBodies];
        public readonly int[] SurfacePasses = new int[MaxBodies];
        public int BodyCount, CellCount, DryCount, StampCount, RejectedCount;
        public float Time;
        private readonly MaterialPropertyBlock _fallbackProperties = new();
        private static readonly Plane[] FrustumPlanes = new Plane[6];

        public WaterRenderSnapshot()
        {
            for (int i = 0; i < MaxBodies; i++) Properties[i] = new MaterialPropertyBlock();
        }

        public static bool HasRenderableRegions(Camera camera)
        {
            if (camera == null) return false;
            GeometryUtility.CalculateFrustumPlanes(camera, FrustumPlanes);
            var regions = UnderwaterRegion.ActiveRegions;
            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (region == null || !region.isActiveAndEnabled || region.Surface == null
                    || !region.Surface.isActiveAndEnabled || region.Profile == null) continue;
                if (!IsVisibleToCamera(region, camera)) continue;
                region.RefreshBinding();
                var renderer = region.SurfaceRenderer;
                if (renderer != null && renderer.enabled && renderer.sharedMaterial != null
                    && renderer.sharedMaterial.FindPass("WaterSurfaceInfo") >= 0) return true;
            }
            return false;
        }

        public void Capture(Camera camera)
        {
            BodyCount = CellCount = DryCount = StampCount = RejectedCount = 0;
            if (camera == null) return;
            GeometryUtility.CalculateFrustumPlanes(camera, FrustumPlanes);
            WaterContactEmitter.SampleRegistered();
            Time = FootstepWaterSurface.WaterTime;
            var regions = UnderwaterRegion.ActiveRegions;
            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (region == null || !region.isActiveAndEnabled || region.Profile == null
                    || region.Surface == null || !region.Surface.isActiveAndEnabled) continue;
                if (!IsVisibleToCamera(region, camera)) continue;
                region.RefreshBinding();
                var renderer = region.SurfaceRenderer;
                var surface = region.Surface;
                if (renderer == null || !renderer.enabled || surface.surfaceMesh == null) continue;
                Material material = renderer.sharedMaterial;
                int pass = material != null ? material.FindPass("WaterSurfaceInfo") : -1;
                if (pass < 0) continue;
                var prisms = region.GetPrisms();
                if (prisms == null) continue;
                int dryCount = surface.dryVolumes != null ? surface.dryVolumes.Length : 0;
                if (BodyCount == MaxBodies || CellCount + prisms.TriangleCount > MaxCells || DryCount + dryCount > MaxDryVolumes)
                {
                    RejectedCount++;
                    renderer.GetPropertyBlock(_fallbackProperties);
                    _fallbackProperties.SetFloat("_WaterManaged", 0f);
                    renderer.SetPropertyBlock(_fallbackProperties);
                    continue;
                }
                CaptureBody(region, material, pass, dryCount);
            }
            for (int i = BodyCount; i < MaxBodies; i++)
            {
                Regions[i] = null;
                Meshes[i] = null;
                Materials[i] = null;
            }
            CaptureStamps();
            CaptureWaterfalls();
        }

        private static bool IsVisibleToCamera(UnderwaterRegion region, Camera camera)
        {
            var surface = region.Surface;
            if (surface.surfaceMesh == null || (camera.cullingMask & (1 << surface.surfaceMesh.gameObject.layer)) == 0
                || (camera.scene.IsValid() && camera.scene != region.gameObject.scene)) return false;
            region.RefreshBinding();
            var renderer = region.SurfaceRenderer;
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) return false;
            Bounds bounds = renderer.bounds;
            float waveHeight = surface.dynamics != null ? surface.dynamics.MaximumDisplacement : 0f;
            bounds.Expand(Vector3.up * (waveHeight * 2f));
            // 수면이 화면 위로 나가도 수중 광학은 필요하므로 수면 대신 전체 체적을 검사한다.
            if (surface.useVolumeBounds)
            {
                Bounds volume = surface.volumeBounds;
                Matrix4x4 matrix = surface.transform.localToWorldMatrix;
                Vector3 x = matrix.MultiplyVector(Vector3.right * volume.extents.x);
                Vector3 y = matrix.MultiplyVector(Vector3.up * volume.extents.y);
                Vector3 z = matrix.MultiplyVector(Vector3.forward * volume.extents.z);
                Vector3 extents = new(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                    Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                bounds.Encapsulate(new Bounds(matrix.MultiplyPoint3x4(volume.center), extents * 2f));
            }
            else
            {
                Vector3 min = bounds.min;
                min.y = Mathf.Min(min.y, camera.transform.position.y - camera.farClipPlane);
                bounds.SetMinMax(min, bounds.max);
            }
            return GeometryUtility.TestPlanesAABB(FrustumPlanes, bounds);
        }

        private void CaptureBody(UnderwaterRegion region, Material material, int pass, int dryCount)
        {
            var surface = region.Surface;
            var profile = region.Profile;
            var dynamics = surface.dynamics;
            int index = BodyCount;
            int start = CellCount;
            var prisms = region.GetPrisms();
            Matrix4x4 meshMatrix = surface.surfaceMesh.transform.localToWorldMatrix;
            for (int t = 0; t < prisms.TriangleCount; t++)
            {
                prisms.GetTriangle(t, meshMatrix, out var a, out var b, out var c);
                // 수직 벽은 높이장이 아니므로 체적 셀로 등록하지 않는다.
                if (Mathf.Abs(Vector3.Cross(b - a, c - a).y) < 0.000001f) continue;
                Cells[CellCount++] = new Cell { A = a, B = b, C = c };
            }
            if (CellCount == start) return;
            int dryStart = DryCount;
            for (int i = 0; i < dryCount; i++)
                DryVolumes[DryCount++] = new DryVolume { Min = surface.dryVolumes[i].min, Max = surface.dryVolumes[i].max };
            Vector3 extinction = profile.extinction;
            Vector4 wave0 = dynamics != null ? dynamics.firstWave.GetParameters() : Vector4.zero;
            Vector4 wave1 = dynamics != null ? dynamics.secondWave.GetParameters() : Vector4.zero;
            Vector3 flow = surface.CurrentVelocity;
            Bodies[index] = new Body
            {
                WorldToLocal = surface.transform.worldToLocalMatrix,
                VolumeMin = surface.volumeBounds.min, VolumeMax = surface.volumeBounds.max,
                Scattering = new Vector4(profile.scatteringColor.linear.r, profile.scatteringColor.linear.g, profile.scatteringColor.linear.b, profile.minimumLight),
                Extinction = new Vector4(extinction.x, extinction.y, extinction.z, profile.maximumVisibility),
                Caustic = new Vector4(profile.causticStrength, profile.causticScale, profile.causticSpeed, 0f),
                Wave0 = wave0, Wave1 = wave1,
                Shallow = material.GetColor("_ShallowColor").linear,
                Deep = material.GetColor("_DeepColor").linear,
                Foam = material.GetColor("_FoamColor").linear,
                Style0 = new Vector4(material.GetFloat("_RefractionStrength"), material.GetFloat("_ReflectionStrength"), material.GetFloat("_Smoothness"), material.GetFloat("_AmbientFloor")),
                Style1 = new Vector4(material.GetFloat("_SurfaceOpacity"), material.GetFloat("_UnderwaterOpacity"), material.GetFloat("_FoamWidth"), material.GetFloat("_FoamStrength")),
                Style2 = new Vector4(material.GetFloat("_FoamScale"), material.GetFloat("_FoamSpeed"), material.GetFloat("_EdgeDistance"), material.GetFloat("_DepthDistance")),
                Ripple = dynamics != null ? new Vector4(dynamics.rippleDuration, dynamics.rippleSpeed, dynamics.rippleWidth, dynamics.rippleStrength) : Vector4.zero,
                // 마스크가 없는 수역에서는 비를 만들지 않는다. 실제 노출은 정보 패스에서 샘플한다.
                Rain = dynamics != null && region.rainExposure != null ? new Vector4(dynamics.rainStrength * region.RainIntensity, dynamics.rainCellSize, dynamics.rainFrequency, 0f) : Vector4.zero,
                Range = new Vector4(start, CellCount - start, dryStart, DryCount - dryStart),
                Authority = new Vector4(surface.priority, surface.useVolumeBounds ? 1f : 0f, 0f, 0f),
                Flow = new Vector4(flow.x, flow.y, flow.z, 0f)
            };
            Regions[index] = region;
            Meshes[index] = surface.surfaceMesh.sharedMesh;
            Materials[index] = material;
            Matrices[index] = surface.surfaceMesh.transform.localToWorldMatrix;
            SurfacePasses[index] = pass;
            var properties = Properties[index];
            region.SurfaceRenderer.GetPropertyBlock(properties);
            properties.SetFloat("_WaterManaged", 1f);
            properties.SetVector("_WaterWave0", wave0);
            properties.SetVector("_WaterWave1", wave1);
            region.SurfaceRenderer.SetPropertyBlock(properties);
            properties.SetFloat("_WaterBodyIndex", index + 1);
            if (region.rainExposure != null) properties.SetTexture("_WaterRainExposure", region.rainExposure);
            properties.SetVector("_WaterRain", Bodies[index].Rain);
            Bounds bounds = region.SurfaceRenderer.bounds;
            properties.SetVector("_WaterExposureRect", new Vector4(bounds.min.x, bounds.min.z,
                1f / Mathf.Max(bounds.size.x, 0.001f), 1f / Mathf.Max(bounds.size.z, 0.001f)));
            BodyCount++;
        }

        private void CaptureStamps()
        {
            for (int i = 0; i < WaterContactStamps.Capacity; i++)
            {
                var stamp = WaterContactStamps.GetStamp(i);
                if (stamp.Surface == null || stamp.Surface.dynamics == null
                    || Time - stamp.Time >= stamp.Surface.dynamics.rippleDuration) continue;
                for (int body = 0; body < BodyCount; body++)
                {
                    if (Regions[body].Surface != stamp.Surface) continue;
                    Stamps[StampCount++] = new Stamp
                    {
                        PositionTime = new Vector4(stamp.Position.x, stamp.Position.y, stamp.Position.z, stamp.Time),
                        Parameters = new Vector4(stamp.Strength, body, 0f, 0f)
                    };
                    break;
                }
            }
        }

        private void CaptureWaterfalls()
        {
            var links = WaterfallLink.ActiveLinks;
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                if (link == null || !link.isActiveAndEnabled || link.target == null || link.impact == null) continue;
                for (int body = 0; body < BodyCount; body++)
                {
                    if (Regions[body].Surface != link.target || !link.target.TrySampleHeight(link.impact.position, out var position)) continue;
                    float interval = Mathf.Max(0.01f, link.impactInterval);
                    float latest = Mathf.Floor(Time / interval) * interval;
                    for (int ring = 0; ring < 3 && StampCount < Stamps.Length; ring++)
                    {
                        Stamps[StampCount++] = new Stamp
                        {
                            PositionTime = new Vector4(position.x, position.y, position.z, latest - ring * interval),
                            Parameters = new Vector4(link.impactStrength, body, 0f, 0f)
                        };
                    }
                    break;
                }
            }
        }
    }
}
