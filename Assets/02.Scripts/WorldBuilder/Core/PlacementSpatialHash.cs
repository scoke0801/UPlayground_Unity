using System;
using System.Collections.Generic;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>회전 footprint의 SAT와 전역 공간 해시를 공유한다.</summary>
    public sealed class PlacementSpatialHash
    {
        private readonly double _cellSize;
        private readonly Dictionary<(int, int), List<PlacementRecord>> _cells = new Dictionary<(int, int), List<PlacementRecord>>();
        public PlacementSpatialHash(double cellSize)
        {
            PlacementIdentity.RequireFinite(cellSize);
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            _cellSize = cellSize;
        }
        public void Add(PlacementRecord record)
        {
            Bounds(record, record.spacing, out double x0, out double z0, out double x1, out double z1);
            for (int z = Cell(z0); z <= Cell(z1); z++)
            for (int x = Cell(x0); x <= Cell(x1); x++)
            {
                if (!_cells.TryGetValue((x, z), out var list)) _cells.Add((x, z), list = new List<PlacementRecord>());
                list.Add(record);
            }
        }
        public PlacementRejection Check(PlacementRecord record)
        {
            Bounds(record, record.spacing, out double x0, out double z0, out double x1, out double z1);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool spacing = false;
            for (int z = Cell(z0); z <= Cell(z1); z++)
            for (int x = Cell(x0); x <= Cell(x1); x++)
            {
                if (!_cells.TryGetValue((x, z), out var list)) continue;
                foreach (var other in list)
                {
                    if (!seen.Add(other.id)) continue;
                    if (Overlaps(record, other, 0, 0)) return PlacementRejection.FootprintOverlap;
                    double required = Math.Max(record.spacing, other.spacing);
                    if (Overlaps(record, other, required / 2, required / 2)) spacing = true;
                }
            }
            return spacing ? PlacementRejection.SpacingRejected : PlacementRejection.None;
        }
        private int Cell(double value) => checked((int)Math.Floor(value / _cellSize));
        public static void Bounds(PlacementRecord p, double padding, out double x0, out double z0, out double x1, out double z1)
        {
            double angle = p.yaw * Math.PI / 180, c = Math.Abs(Math.Cos(angle)), s = Math.Abs(Math.Sin(angle));
            double dx = c * (p.halfX + padding) + s * (p.halfZ + padding), dz = s * (p.halfX + padding) + c * (p.halfZ + padding);
            x0 = p.x - dx; x1 = p.x + dx; z0 = p.z - dz; z1 = p.z + dz;
        }
        public static bool Overlaps(PlacementRecord a, PlacementRecord b, double padA, double padB)
        {
            double ar = a.yaw * Math.PI / 180, br = b.yaw * Math.PI / 180;
            // Unity의 +Y yaw는 +X를 -Z로 돌린다.
            double ac = Math.Cos(ar), az = -Math.Sin(ar), bc = Math.Cos(br), bz = -Math.Sin(br);
            return !Separated(ac, az, a, b, ac, az, bc, bz, padA, padB) && !Separated(-az, ac, a, b, ac, az, bc, bz, padA, padB)
                && !Separated(bc, bz, a, b, ac, az, bc, bz, padA, padB) && !Separated(-bz, bc, a, b, ac, az, bc, bz, padA, padB);
        }
        private static bool Separated(double ax, double az, PlacementRecord a, PlacementRecord b, double ac, double asi, double bc, double bsi, double pa, double pb)
        {
            double ra = Math.Abs(ax * ac + az * asi) * (a.halfX + pa) + Math.Abs(-ax * asi + az * ac) * (a.halfZ + pa);
            double rb = Math.Abs(ax * bc + az * bsi) * (b.halfX + pb) + Math.Abs(-ax * bsi + az * bc) * (b.halfZ + pb);
            return Math.Abs((b.x - a.x) * ax + (b.z - a.z) * az) >= ra + rb;
        }
        public static bool Contains(PlacementRecord p, double x, double z)
        {
            double angle = p.yaw * Math.PI / 180, c = Math.Cos(angle), s = -Math.Sin(angle), dx = x - p.x, dz = z - p.z;
            return Math.Abs(dx * c + dz * s) <= p.halfX && Math.Abs(-dx * s + dz * c) <= p.halfZ;
        }
        public static PlacementRecord Rectangle(double x0, double z0, double x1, double z1)
            => new PlacementRecord { x = (x0 + x1) / 2, z = (z0 + z1) / 2, halfX = (x1 - x0) / 2, halfZ = (z1 - z0) / 2 };
    }

    /// <summary>최종 P02 높이·Hole을 보간하고 footprint 전체의 지지를 검사한다.</summary>
    public sealed class PlacementTerrain
    {
        private readonly TerrainBuildResult _terrain;
        private readonly WorldGrid _grid;
        public PlacementTerrain(WorldGrid grid, TerrainBuildResult terrain)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            if (terrain.SampleCount != grid.SampleCount) throw new ArgumentException("지형 규격이 다릅니다.");
        }
        public double Height(double x, double z)
        {
            double sx = _grid.WorldToSample(x), sz = _grid.WorldToSample(z);
            int ix = Math.Min(_grid.SampleCount - 2, (int)sx), iz = Math.Min(_grid.SampleCount - 2, (int)sz);
            double tx = sx - ix, tz = sz - iz;
            return (1 - tz) * ((1 - tx) * _terrain.GetHeight(iz, ix) + tx * _terrain.GetHeight(iz, ix + 1))
                + tz * ((1 - tx) * _terrain.GetHeight(iz + 1, ix) + tx * _terrain.GetHeight(iz + 1, ix + 1));
        }
        public double Slope(double x, double z)
        {
            double d = _grid.SampleSpacing, x0 = Math.Max(0, x - d), x1 = Math.Min(_grid.WorldSize, x + d);
            double z0 = Math.Max(0, z - d), z1 = Math.Min(_grid.WorldSize, z + d);
            double dx = (Height(x1, z) - Height(x0, z)) / (x1 - x0), dz = (Height(x, z1) - Height(x, z0)) / (z1 - z0);
            return Math.Atan(Math.Sqrt(dx * dx + dz * dz)) * 180 / Math.PI;
        }
        public PlacementRejection CheckSupport(PlacementRecord p)
        {
            if (p.surfaceId != "terrain") return PlacementRejection.SurfaceMismatch;
            PlacementSpatialHash.Bounds(p, 0, out double x0, out double z0, out double x1, out double z1);
            if (x0 < 0 || z0 < 0 || x1 > _grid.WorldSize || z1 > _grid.WorldSize) return PlacementRejection.OutsideRegion;
            double min = Height(p.x, p.z), max = min, step = _grid.SampleSpacing;
            int cells = _grid.SampleCount - 1;
            // footprint와 교차하는 모든 Hole 셀과 높이 표본을 읽어 내부 절벽도 놓치지 않는다.
            for (int z = Math.Max(0, (int)Math.Floor(z0 / step)); z <= Math.Min(cells - 1, (int)Math.Floor(z1 / step)); z++)
            for (int x = Math.Max(0, (int)Math.Floor(x0 / step)); x <= Math.Min(cells - 1, (int)Math.Floor(x1 / step)); x++)
            {
                var cell = PlacementSpatialHash.Rectangle(x * step, z * step, (x + 1) * step, (z + 1) * step);
                if (!PlacementSpatialHash.Overlaps(p, cell, 0, 0)) continue;
                if (!_terrain.GetSurface(z, x)) return PlacementRejection.HoleOverlap;
                for (int dz = 0; dz <= 1; dz++)
                for (int dx = 0; dx <= 1; dx++) { double h = _terrain.GetHeight(z + dz, x + dx); min = Math.Min(min, h); max = Math.Max(max, h); }
            }
            return max - min > p.maxSupportDelta ? PlacementRejection.Unsupported : PlacementRejection.None;
        }
    }
}
