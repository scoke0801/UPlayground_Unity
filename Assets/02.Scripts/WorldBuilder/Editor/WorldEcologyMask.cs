using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    internal sealed class WorldEcologyMask
    {
        private readonly WorldGrid _grid;
        private readonly PlacementTerrain _terrain;
        private readonly TerrainBuildResult _surface;
        private readonly List<(PlacementReservation reservation, PlanPoint[] polygon)> _reserved = new();
        private readonly List<TerrainOperation> _protected = new();
        private readonly List<(PlanPoint[] polygon, double minX, double minZ, double maxX, double maxZ)> _shores = new();
        private readonly List<PlacementOverride> _excludes = new();

        public WorldEcologyMask(WorldDefinition definition, TerrainBuildResult terrain, IReadOnlyList<PlacementReservation> extras = null)
        {
            _grid = definition.Profile.CreateGrid();
            _terrain = new PlacementTerrain(_grid, terrain);
            _surface = terrain;
            foreach (var reservation in definition.PlacementReservations) _reserved.Add((reservation, reservation.boundary));
            if (extras != null) foreach (var reservation in extras) _reserved.Add((reservation, reservation.boundary));
            foreach (var op in definition.ReadOperations())
                if (op.Enabled && op.Kind == TerrainOperationKind.ProtectRegion && (op.Protection & (TerrainProtection.Placement | TerrainProtection.Vegetation)) != 0)
                    _protected.Add(op);
            foreach (var edit in definition.PlacementOverrides)
                if (edit.kind == PlacementOverrideKind.ExcludeRegion) _excludes.Add(edit);
            if (definition.WaterSystem == null) return;
            foreach (var record in definition.WaterSystem.bodies)
            {
                var body = record.ToCore();
                var polygon = body.kind == WaterBodyKind.River ? WaterGeometry.RiverBanks(body, body.width / 2) : body.boundary;
                var bounds = Bounds(polygon);
                _shores.Add((polygon, bounds.minX, bounds.minZ, bounds.maxX, bounds.maxZ));
            }
        }

        public double ShoreDistance(double x, double z)
        {
            double best = double.PositiveInfinity;
            foreach (var shore in _shores)
            {
                if (Math.Max(shore.minX - x, x - shore.maxX) > best || Math.Max(shore.minZ - z, z - shore.maxZ) > best) continue;
                var polygon = shore.polygon;
                var point = new PlanPoint(x, 0, z);
                for (int i = 0; i < polygon.Length; i++)
                    best = Math.Min(best, PlanningGeometry.DistanceToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length], out _));
            }
            return best;
        }

        public double Height(double x, double z) => _terrain.Height(x, z);
        public double Slope(double x, double z) => _terrain.Slope(x, z);

        public bool Occupied(double x, double z)
        {
            int cellX = Math.Min(_grid.SampleCount - 2, (int)(x / _grid.SampleSpacing));
            int cellZ = Math.Min(_grid.SampleCount - 2, (int)(z / _grid.SampleSpacing));
            if (!_surface.GetSurface(cellZ, cellX)) return true;
            foreach (var entry in _reserved)
            {
                var r = entry.reservation;
                if (x < r.minX || x > r.maxX || z < r.minZ || z > r.maxZ) continue;
                if (entry.polygon == null || entry.polygon.Length == 0 || PlanningGeometry.Contains(entry.polygon, x, z)) return true;
            }
            foreach (var op in _protected)
            {
                int sx = (int)(x / _grid.SampleSpacing), sz = (int)(z / _grid.SampleSpacing);
                if (op.Bounds.Contains(sx, sz) && op.GetMask((sz - op.Bounds.Z) * op.Bounds.Width + sx - op.Bounds.X) > .5f) return true;
            }
            return false;
        }

        public PlacementRejection Check(PlacementRecord candidate, string tag = null)
        {
            var support = _terrain.CheckSupport(candidate);
            if (support != PlacementRejection.None) return support;
            foreach (var entry in _reserved)
                if (Overlaps(candidate, entry.reservation)) return PlacementRejection.ReservedArea;
            foreach (var edit in _excludes)
                if ((string.IsNullOrEmpty(edit.ruleTag) || edit.ruleTag == tag) && PlacementSpatialHash.Overlaps(candidate,
                    PlacementSpatialHash.Rectangle(edit.minX, edit.minZ, edit.maxX, edit.maxZ), 0, 0)) return PlacementRejection.ReservedArea;
            return CheckProtection(candidate);
        }

        public PlacementRejection CheckNewFixedPlacementConstraints(PlacementRecord candidate, string owner)
        {
            foreach (var entry in _reserved)
                if (entry.reservation.ownerPlanGuid == owner && Overlaps(candidate, entry.reservation)) return PlacementRejection.ReservedArea;
            return PlacementRejection.None;
        }

        private PlacementRejection CheckProtection(PlacementRecord candidate)
        {
            double step = _grid.SampleSpacing;
            PlacementSpatialHash.Bounds(candidate, 0, out double x0, out double z0, out double x1, out double z1);
            foreach (var op in _protected)
            {
                int left = Math.Max(op.Bounds.X, (int)Math.Floor(x0 / step)), right = Math.Min(op.Bounds.X + op.Bounds.Width - 1, (int)Math.Floor(x1 / step));
                int bottom = Math.Max(op.Bounds.Z, (int)Math.Floor(z0 / step)), top = Math.Min(op.Bounds.Z + op.Bounds.Height - 1, (int)Math.Floor(z1 / step));
                for (int z = bottom; z <= top; z++)
                for (int x = left; x <= right; x++)
                    if (op.GetMask((z - op.Bounds.Z) * op.Bounds.Width + x - op.Bounds.X) > .5f &&
                        PlacementSpatialHash.Overlaps(candidate, PlacementSpatialHash.Rectangle(x * step, z * step, (x + 1) * step, (z + 1) * step), 0, 0))
                        return PlacementRejection.ReservedArea;
            }
            return PlacementRejection.None;
        }

        internal static bool Overlaps(PlacementRecord candidate, PlacementReservation reservation)
        {
            PlacementSpatialHash.Bounds(candidate, 0, out double x0, out double z0, out double x1, out double z1);
            if (x1 <= reservation.minX || x0 >= reservation.maxX || z1 <= reservation.minZ || z0 >= reservation.maxZ) return false;
            return reservation.boundary != null && reservation.boundary.Length > 0
                ? PlanningGeometry.Overlaps(PlanningGeometry.Footprint(candidate), reservation.boundary)
                : PlacementSpatialHash.Overlaps(candidate, PlacementSpatialHash.Rectangle(reservation.minX, reservation.minZ, reservation.maxX, reservation.maxZ), 0, 0);
        }

        internal static (double minX, double minZ, double maxX, double maxZ) Bounds(PlanPoint[] polygon)
        {
            double x0 = double.PositiveInfinity, z0 = double.PositiveInfinity, x1 = double.NegativeInfinity, z1 = double.NegativeInfinity;
            foreach (var p in polygon)
            {
                x0 = Math.Min(x0, p.x); z0 = Math.Min(z0, p.z);
                x1 = Math.Max(x1, p.x); z1 = Math.Max(z1, p.z);
            }
            return (x0, z0, x1, z1);
        }
    }
}

