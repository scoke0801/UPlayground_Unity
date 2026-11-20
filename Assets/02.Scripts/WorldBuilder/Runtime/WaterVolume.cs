using UnityEngine;
using UPlayGround.Contracts;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>bounds broadphase 후 실제 다각형과 높이장 바닥을 검사한다. curtain은 이 체적을 갖지 않는다.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class WaterVolume : MonoBehaviour, IWaterQuery
    {
        [SerializeField] private WaterBody _body;
        [SerializeField] private PlanPoint[] _polygon;
        [SerializeField] private WaterExclusion[] _exclusions;
        [SerializeField] private WorldDefinition _ecology;
        [SerializeField] private Terrain[] _terrains;
        [SerializeField] private Bounds _bounds;
        [SerializeField] private double _area;
        private CaveDryVolume[] _bakedDryVolumes;
        public string BodyId => _body?.guid;
        public Bounds Bounds => _bounds;
        public void BindTerrains(Terrain[] terrains) => _terrains = terrains;
        public void BindEcology(WorldDefinition definition) => _ecology = definition;
        private void Awake()
        {
            if (TryGetComponent<WorldBakedWaterExclusions>(out var baked)) _bakedDryVolumes = baked.volumes;
        }
        public void Initialize(WaterBody body, WaterExclusion[] exclusions, Terrain[] terrains)
        {
            _body = body.Clone(); _polygon = body.kind == WaterBodyKind.River ? WaterGeometry.RiverBanks(body, body.width / 2) : (PlanPoint[])body.boundary.Clone();
            _exclusions = exclusions; _terrains = terrains; _area = WaterGeometry.Area(body);
            _bounds = new Bounds(ToVector(_polygon[0]), Vector3.zero);
            foreach (var p in _polygon) _bounds.Encapsulate(ToVector(p));
            double min = body.level, max = body.level;
            if (body.kind == WaterBodyKind.River) foreach (var p in body.route) { min = System.Math.Min(min, p.y); max = System.Math.Max(max, p.y); }
            var low = _bounds.min; var high = _bounds.max; low.y = (float)(min - body.depth - body.maxCutFill); high.y = (float)(max + body.queryHeight);
            _bounds.SetMinMax(low, high);
        }
        public bool TrySample(Vector3 position, out WaterSample sample)
        {
            sample = default;
            if (_body == null || !_bounds.Contains(position) || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return false;
            if (_exclusions != null) foreach (var exclusion in _exclusions)
                if (position.y >= exclusion.minY && position.y <= exclusion.maxY && PlanningGeometry.Contains(exclusion.boundary, position.x, position.z)) return false;
            if (_ecology != null)
            {
                var dry = _ecology.EcologyDryVolumes;
                for (int i = 0; i < dry.Count; i++)
                    if (dry[i].Contains(position)) return false;
            }
            if (_bakedDryVolumes != null) foreach (var dry in _bakedDryVolumes)
                if (dry.Contains(position)) return false;
            if (!WaterGeometry.TrySample(_body, position.x, position.z, out double level, out _, out PlanPoint flow, _polygon)) return false;
            if (!TryGetBottom(position, out float bottom) || level <= bottom + .001 || position.y < bottom - .03 || position.y > level + _body.queryHeight) return false;
            sample = new WaterSample { BodyId = _body.guid, SurfaceY = (float)level, Depth = (float)level - bottom, Flow = ToVector(flow), Priority = _body.priority, SpecificityArea = _area };
            return true;
        }
        private bool TryGetBottom(Vector3 p, out float bottom)
        {
            bottom = 0;
            foreach (var terrain in _terrains)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                var origin = terrain.transform.position; var size = terrain.terrainData.size;
                if (p.x < origin.x || p.z < origin.z || p.x > origin.x + size.x || p.z > origin.z + size.z) continue;
                bottom = terrain.terrainData.GetInterpolatedHeight((p.x - origin.x) / size.x, (p.z - origin.z) / size.z) + origin.y; return true;
            }
            return false;
        }
        public bool TryRaycastDown(Vector3 origin, float distance, out WaterSample sample)
        {
            sample = default;
            if (_body == null || !float.IsFinite(distance) || distance < 0 || !float.IsFinite(origin.x) || !float.IsFinite(origin.y) || !float.IsFinite(origin.z)) return false;
            if (!WaterGeometry.TrySample(_body, origin.x, origin.z, out double level, out _, out _, _polygon) || level > origin.y || origin.y - level > distance) return false;
            return TrySample(new Vector3(origin.x, (float)level, origin.z), out sample);
        }
        private static Vector3 ToVector(PlanPoint p) => new Vector3((float)p.x, (float)p.y, (float)p.z);
    }
}
