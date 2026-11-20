using System;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    public sealed class CaveConflict : InvalidOperationException
    {
        public string ElementId { get; }
        public Vector3 Position { get; }

        public CaveConflict(string id, Vector3 position, string cause) : base($"CaveConflict [{id}] {position} · {cause}")
        { ElementId = id; Position = position; }
    }

    internal sealed class CaveEvaluation
    {
        internal sealed class Module
        {
            public CaveLayout Cave;
            public CaveModuleLayout Source;
            public PlacementRecord Footprint;
            public readonly Dictionary<string, (CaveSocket socket, WorldCavePortal portal)> Ports = new();
            public float Low, High;
            public bool Entrance => Source.preset.kind == CaveModuleKind.Entrance;
            public Quaternion Rotation => Quaternion.Euler(0, Source.yaw, 0);
            public float Floor(double x, double z)
            {
                Vector3 local = Quaternion.Inverse(Rotation) * (new Vector3((float)x, Source.position.y, (float)z) - Source.position);
                var p = Source.preset;
                return Source.position.y + Mathf.Lerp(p.floorAtMinZ, p.floorAtMaxZ, (local.z - p.footprintCenter.y) / p.footprintSize.y + .5f);
            }

            public CaveConflict Conflict(string cause) => new(Source.moduleId, Source.position, cause);
        }

        internal sealed class Cut
        {
            public Module Module;
            public SampleRect Bounds;
            public float[] Masks;
        }

        public readonly List<Module> Modules = new();
        public readonly List<Cut> Cuts = new();
        public readonly List<CaveDryVolume> DryVolumes = new();
        public readonly List<WorldCavePortal> Portals = new();

        public static CaveEvaluation Build(WorldDefinition definition, WorldEcology source, TerrainBuildResult terrain,
            IReadOnlyList<TerrainOperation> operations)
        {
            var result = new CaveEvaluation();
            if (source.caves.Count == 0) return result;
            var grid = definition.Profile.CreateGrid();
            var heights = new PlacementTerrain(grid, terrain);
            var motor = source.playerPrefab != null ? source.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true) : null;
            if (motor == null || motor.Capsule == null || motor.transform.lossyScale != Vector3.one) throw new CaveConflict(source.ecologyGuid, default, "단위 스케일 플레이어 KCC 프리팹이 필요합니다.");
            WorldEcologyAuthoring.RequireAsset(source.playerPrefab);
            var catalog = new HashSet<string>(StringComparer.Ordinal);
            foreach (var preset in source.caveCatalog)
            {
                if (preset == null || !Guid.TryParseExact(preset.catalogId, "N", out _) || !catalog.Add(preset.catalogId))
                    throw new CaveConflict(source.ecologyGuid, default, "동굴 카탈로그 ID 누락 또는 중복");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var plannedWater = new HashSet<string>(StringComparer.Ordinal);
            if (definition.Planning != null && definition.WaterSystem != null)
            foreach (var bridge in definition.Planning.bridges)
                if (bridge.waterZone != null && definition.WaterSystem.bodies.Any(body => body.waterGuid == bridge.waterPlanGuid) &&
                    !definition.Planning.reservations.Any(zone => zone.guid == bridge.waterZone.guid))
                    plannedWater.Add(PlanningIdentity.Derive(definition.WorldGuid, definition.Planning.planGuid, bridge.waterZone.guid, "zone-reservation"));
            foreach (var cave in source.caves.OrderBy(c => c?.caveGuid, StringComparer.Ordinal))
            {
                AddId(ids, cave?.caveGuid, default);
                if (cave.modules.Count == 0) throw new CaveConflict(cave.caveGuid, default, "모듈이 없습니다.");
                var modules = new Dictionary<string, Module>(StringComparer.Ordinal);
                foreach (var item in cave.modules.OrderBy(m => m?.moduleId, StringComparer.Ordinal))
                {
                    AddId(ids, item?.moduleId, item?.position ?? default);
                    AddId(ids, item.surfaceId, item.position);
                    var module = ReadModule(definition, source, cave, item, motor, grid, ids);
                    modules.Add(item.moduleId, module);
                    foreach (var other in result.Modules)
                        if (Math.Min(module.High, other.High) - Math.Max(module.Low, other.Low) > .001
                            && PlacementSpatialHash.Overlaps(module.Footprint, other.Footprint, -.0005, -.0005))
                            throw module.Conflict("모듈 체적 침범 · " + other.Source.moduleId);
                    foreach (var reservation in definition.PlacementReservations)
                    {
                        if (reservation.ownerPlanGuid == source.ecologyGuid) continue;
                        if (!module.Entrance && definition.WaterSystem != null &&
                            (reservation.ownerPlanGuid == definition.WaterSystem.systemGuid || plannedWater.Contains(reservation.guid))) continue;
                        if (WorldEcologyMask.Overlaps(module.Footprint, reservation))
                            throw module.Conflict("구조물·도로·필수 동선·보호 예약 침범 · " + reservation.guid);
                    }
                    foreach (var prop in definition.Placements)
                        if (prop.isFixed && Math.Abs(prop.y - module.High) < motor.Capsule.height &&
                            PlacementSpatialHash.Overlaps(module.Footprint, prop, 0, 0))
                            throw module.Conflict("수동 배치 침범 · " + prop.id);
                    result.Modules.Add(module);
                    var preset = item.preset;
                    float halfLength = preset.footprintSize.y / 2;
                    result.DryVolumes.Add(new CaveDryVolume {
                        caveGuid = cave.caveGuid, moduleId = item.moduleId, clearHeight = preset.clearHeight,
                        start = item.position + module.Rotation * new Vector3(preset.footprintCenter.x, preset.floorAtMinZ, preset.footprintCenter.y - halfLength),
                        end = item.position + module.Rotation * new Vector3(preset.footprintCenter.x, preset.floorAtMaxZ, preset.footprintCenter.y + halfLength),
                        extent = new WaterExclusion {
                            guid = PlanningIdentity.Derive(definition.WorldGuid, cave.caveGuid, item.moduleId, "dry"),
                            boundary = PlanningGeometry.Footprint(module.Footprint), minY = module.Low, maxY = module.High
                        }
                    });
                    CheckCoverage(result, module, heights, grid, terrain, operations);
                }
                result.ValidateGraph(cave, modules, heights, grid, terrain, motor);
                foreach (var prop in cave.props)
                {
                    AddId(ids, prop?.placementId, default);
                    var owner = modules.Values.FirstOrDefault(m => m.Source.surfaceId == prop.surfaceId);
                    if (owner == null || prop.prefab == null || !PlacementSpatialHash.Contains(owner.Footprint, prop.worldXZ.x, prop.worldXZ.y))
                        throw new CaveConflict(prop.placementId, new Vector3(prop.worldXZ.x, 0, prop.worldXZ.y), "동굴 바닥 배치의 surfaceId 또는 프리팹 오류");
                    WorldEcologyAuthoring.RequireAsset(prop.prefab);
                }
            }
            return result;
        }

        private static Module ReadModule(WorldDefinition definition, WorldEcology source, CaveLayout cave, CaveModuleLayout item,
            KinematicCharacterMotor motor, WorldGrid grid, HashSet<string> ids)
        {
            var module = new Module { Cave = cave, Source = item };
            var p = item.preset;
            if (p == null || !source.caveCatalog.Contains(p) || p.prefab == null || p.prefab.transform.localScale != Vector3.one)
                throw module.Conflict("카탈로그 모듈·프리팹 누락 또는 루트 스케일 오류");
            WorldEcologyAuthoring.RequireAsset(p.prefab);
            var floor = p.prefab.GetComponent<WorldCaveModule>();
            if (floor == null || floor.floorColliders == null || floor.floorColliders.Length == 0 ||
                floor.floorColliders.Any(c => c == null || !c.enabled || c.isTrigger || !c.transform.IsChildOf(p.prefab.transform) ||
                    (motor.CollidableLayers.value & (1 << c.gameObject.layer)) == 0) ||
                p.prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw module.Conflict("정적 프리팹의 바닥 Collider 참조가 필요합니다.");
            PlacementIdentity.RequireFinite(item.position.x, item.position.y, item.position.z, item.yaw, p.floorAtMinZ, p.floorAtMaxZ,
                p.positionTolerance, p.angleTolerance, p.cover, p.clearWidth, p.clearHeight, p.footprintSize.x, p.footprintSize.y,
                p.footprintCenter.x, p.footprintCenter.y);
            if (p.positionTolerance <= 0 || p.angleTolerance <= 0 || p.cover < 0 || p.footprintSize.x <= 0 || p.footprintSize.y <= 0 ||
                p.clearWidth < 2 * motor.Capsule.radius || p.clearHeight < motor.Capsule.height ||
                p.allowedYaw == null || !p.allowedYaw.Any(a => Mathf.Abs(Mathf.DeltaAngle(a, item.yaw)) <= p.angleTolerance))
                throw module.Conflict("회전·허용오차·이동 폭/높이 오류");
            if (Mathf.Atan2(Mathf.Abs(p.floorAtMaxZ - p.floorAtMinZ), p.footprintSize.y) * Mathf.Rad2Deg > motor.MaxStableSlopeAngle)
                throw module.Conflict("KCC 최대 안정 경사 초과");
            var center = item.position + module.Rotation * new Vector3(p.footprintCenter.x, 0, p.footprintCenter.y);
            module.Footprint = new PlacementRecord { x = center.x, z = center.z, yaw = item.yaw, halfX = p.footprintSize.x / 2, halfZ = p.footprintSize.y / 2 };
            module.Low = item.position.y + Mathf.Min(p.floorAtMinZ, p.floorAtMaxZ);
            module.High = item.position.y + Mathf.Max(p.floorAtMinZ, p.floorAtMaxZ) + p.clearHeight;
            PlanningGeometry.ValidatePolygon(PlanningGeometry.Footprint(module.Footprint), grid);
            var socketIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socket in p.sockets)
            {
                AddId(socketIds, socket?.id, item.position);
                PlacementIdentity.RequireFinite(socket.floor.x, socket.floor.y, socket.floor.z, socket.yaw, socket.width, socket.height);
                Vector3 position = item.position + module.Rotation * socket.floor;
                Vector3 outward = module.Rotation * Quaternion.Euler(0, socket.yaw, 0) * Vector3.forward;
                if (string.IsNullOrWhiteSpace(socket.profile) || socket.width < 2 * motor.Capsule.radius || socket.height < motor.Capsule.height ||
                    !PlacementSpatialHash.Contains(module.Footprint, position.x - outward.x * .01, position.z - outward.z * .01) ||
                    PlacementSpatialHash.Contains(module.Footprint, position.x + outward.x * .01, position.z + outward.z * .01) ||
                    Math.Abs(module.Floor(position.x, position.z) - position.y) > p.positionTolerance)
                    throw module.Conflict("소켓 접합면·규격·바닥 높이 오류 · " + socket.id);
                string id = PlanningIdentity.Derive(definition.WorldGuid, cave.caveGuid, item.moduleId, socket.id);
                AddId(ids, id, position);
                module.Ports.Add(socket.id, (socket, new WorldCavePortal { id = id, caveGuid = cave.caveGuid, moduleId = item.moduleId,
                    surfaceId = item.surfaceId, position = position, outward = outward, width = socket.width, height = socket.height }));
            }
            int required = p.kind == CaveModuleKind.EndCap ? 1 : p.kind == CaveModuleKind.Branch ? 3 : 2;
            if (module.Ports.Count < required || module.Ports.Values.Count(x => x.socket.exterior) != (module.Entrance ? 1 : 0))
                throw module.Conflict("모듈 유형에 필요한 소켓/외부 포털이 없습니다.");
            if (module.Entrance) WorldEcologyAuthoring.RequireAsset(p.collarPrefab);
            return module;
        }

        private static void CheckCoverage(CaveEvaluation result, Module module, PlacementTerrain terrain, WorldGrid grid,
            TerrainBuildResult surfaceMap, IReadOnlyList<TerrainOperation> operations)
        {
            double step = grid.SampleSpacing;
            int cells = grid.SampleCount - 1;
            PlacementSpatialHash.Bounds(module.Footprint, 0, out double x0, out double z0, out double x1, out double z1);
            var rect = new SampleRect(Math.Max(0, (int)Math.Floor(x0 / step)), Math.Max(0, (int)Math.Floor(z0 / step)),
                Math.Min(cells, (int)Math.Ceiling(x1 / step)) - Math.Max(0, (int)Math.Floor(x0 / step)),
                Math.Min(cells, (int)Math.Ceiling(z1 / step)) - Math.Max(0, (int)Math.Floor(z0 / step)));
            var mask = module.Entrance ? new float[rect.Width * rect.Height] : null;
            for (int z = rect.Z; z < rect.Z + rect.Height; z++)
            for (int x = rect.X; x < rect.X + rect.Width; x++)
            {
                var cell = PlacementSpatialHash.Rectangle(x * step, z * step, (x + 1) * step, (z + 1) * step);
                if (!PlacementSpatialHash.Overlaps(module.Footprint, cell, 0, 0)) continue;
                if (!module.Entrance && !surfaceMap.GetSurface(z, x))
                    throw new CaveConflict(module.Source.moduleId, new Vector3((float)cell.x, module.High, (float)cell.z), "천장 위 Terrain Hole 발견");
                double minSurfaceOffset = double.PositiveInfinity, maxSurfaceOffset = double.NegativeInfinity;
                for (int iz = 0; iz <= 1; iz++)
                for (int ix = 0; ix <= 1; ix++)
                {
                    double px = (x + ix) * step, pz = (z + iz) * step;
                    double surface = terrain.Height(px, pz), floor = module.Floor(px, pz), roof = floor + module.Source.preset.clearHeight;
                    if (!module.Entrance && surface + .02 < roof + module.Source.preset.cover)
                        throw new CaveConflict(module.Source.moduleId, new Vector3((float)px, (float)surface, (float)pz), "천장 위 피복 부족");
                    minSurfaceOffset = Math.Min(minSurfaceOffset, surface - floor);
                    maxSurfaceOffset = Math.Max(maxSurfaceOffset, surface - floor);
                }
                foreach (var op in operations)
                    if (op.Enabled && op.Kind == TerrainOperationKind.ProtectRegion && (op.Protection & (TerrainProtection.Holes | TerrainProtection.Placement)) != 0
                        && op.Bounds.Contains(x, z) && op.GetMask((z - op.Bounds.Z) * op.Bounds.Width + x - op.Bounds.X) > .5f)
                        throw new CaveConflict(module.Source.moduleId, new Vector3((float)cell.x, module.Low, (float)cell.z), "수동 지형 보호 영역 침범");
                if (mask != null && maxSurfaceOffset >= -.03 && minSurfaceOffset <= module.Source.preset.clearHeight + module.Source.preset.cover)
                    mask[(z - rect.Z) * rect.Width + x - rect.X] = 1;
            }
            if (mask == null) return;
            if (!mask.Contains(1)) throw module.Conflict("입구 footprint에서 실제 지표 교차를 찾지 못했습니다.");
            result.Cuts.Add(new Cut { Module = module, Bounds = rect, Masks = mask });
        }

        private void ValidateGraph(CaveLayout cave, Dictionary<string, Module> modules, PlacementTerrain heights, WorldGrid grid,
            TerrainBuildResult terrain, KinematicCharacterMotor motor)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            var links = modules.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
            foreach (var link in cave.connections)
            {
                if (link == null || string.IsNullOrEmpty(link.moduleA) || string.IsNullOrEmpty(link.moduleB) ||
                    !modules.TryGetValue(link.moduleA, out var a) || !modules.TryGetValue(link.moduleB, out var b) || a == b ||
                    string.IsNullOrEmpty(link.socketA) || string.IsNullOrEmpty(link.socketB) ||
                    !a.Ports.TryGetValue(link.socketA, out var pa) || !b.Ports.TryGetValue(link.socketB, out var pb))
                    throw new CaveConflict(cave.caveGuid, default, "연결의 모듈 또는 소켓이 소실됐습니다.");
                float tolerance = Mathf.Min(a.Source.preset.positionTolerance, b.Source.preset.positionTolerance);
                Vector3 delta = pa.portal.position - pb.portal.position;
                if (pa.socket.exterior || pb.socket.exterior || !used.Add(pa.portal.id) || !used.Add(pb.portal.id) || pa.socket.profile != pb.socket.profile ||
                    Math.Abs(pa.socket.width - pb.socket.width) > tolerance || Math.Abs(pa.socket.height - pb.socket.height) > tolerance ||
                    Vector3.Angle(pa.portal.outward, -pb.portal.outward) > Mathf.Min(a.Source.preset.angleTolerance, b.Source.preset.angleTolerance) ||
                    new Vector2(delta.x, delta.z).magnitude > tolerance || Math.Abs(delta.y) > Mathf.Min(tolerance, motor.MaxStepHeight))
                    throw new CaveConflict(pa.portal.id, pa.portal.position, "소켓 재사용·방향·규격·접합 오차·바닥 단차");
                links[link.moduleA].Add(link.moduleB);
                links[link.moduleB].Add(link.moduleA);
            }
            string start = null;
            foreach (var module in modules.Values)
            foreach (var port in module.Ports.Values)
            {
                Portals.Add(port.portal);
                if (!port.socket.exterior)
                {
                    if (!used.Contains(port.portal.id)) throw new CaveConflict(port.portal.id, port.portal.position, "연결되지 않은 내부 소켓");
                    continue;
                }
                start ??= module.Source.moduleId;
                var across = Vector3.Cross(Vector3.up, port.portal.outward);
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 p = port.portal.position + port.portal.outward * (float)Math.Max(grid.SampleSpacing, motor.Capsule.radius)
                        + across * (port.socket.width * .5f - motor.Capsule.radius) * i;
                    if (p.x < 0 || p.z < 0 || p.x >= grid.WorldSize || p.z >= grid.WorldSize ||
                        !terrain.GetSurface((int)(p.z / grid.SampleSpacing), (int)(p.x / grid.SampleSpacing)) ||
                        Math.Abs(heights.Height(p.x, p.z) - port.portal.position.y) > motor.MaxStepHeight ||
                        heights.Slope(p.x, p.z) > motor.MaxStableSlopeAngle)
                        throw new CaveConflict(port.portal.id, p, "외부 지면의 지지·단차·경사 불일치");
                }
            }
            if (start == null) throw new CaveConflict(cave.caveGuid, default, "외부 포털이 없습니다.");
            var visited = new HashSet<string>(StringComparer.Ordinal) { start };
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
                foreach (string next in links[queue.Dequeue()]) if (visited.Add(next)) queue.Enqueue(next);
            if (visited.Count != modules.Count) throw new CaveConflict(cave.caveGuid, default, "외부 포털에서 연결되지 않는 모듈이 있습니다.");
        }

        private static void AddId(HashSet<string> ids, string id, Vector3 position)
        {
            if (!Guid.TryParseExact(id, "N", out var guid) || guid.ToString("N") != id || !ids.Add(id))
                throw new CaveConflict(id, position, "ID 누락·형식 오류·중복");
        }
    }
}

