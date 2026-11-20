using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    internal static class WorldNavigationBake
    {
        internal static void Build(Scene scene, WorldProfile profile, WorldNavigationLayout layout, WorldBakeCatalog catalog,
            string folder, IReadOnlyDictionary<TerrainData, TerrainData> originalTerrain, Action<string> checkpoint)
        {
            var settings = NavMesh.GetSettingsByID(layout.agentTypeId);
            var player = layout.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true);
            catalog.agentTypeId = settings.agentTypeID;
            catalog.agentRadius = settings.agentRadius; catalog.agentHeight = settings.agentHeight;
            catalog.agentStep = settings.agentClimb; catalog.agentSlope = settings.agentSlope;
            catalog.playerRadius = player.Capsule.radius; catalog.playerHeight = player.Capsule.height;
            catalog.playerStep = player.MaxStepHeight; catalog.playerSlope = player.MaxStableSlopeAngle;

            var all = scene.GetRootGameObjects();
            var caveModules = all.SelectMany(root => root.GetComponentsInChildren<WorldCaveModule>(true)).ToArray();
            var portalMarkers = all.SelectMany(root => root.GetComponentsInChildren<WorldEcologyPreview>(true))
                .SelectMany(preview => preview.portals).ToArray();
            var gateOwners = all.SelectMany(root => root.GetComponentsInChildren<WorldPlanningInstance>(true))
                .Where(marker => marker.GetComponentInChildren<WorldBridgeAccessGate>(true) != null).ToArray();
            var markups = new List<NavMeshBuildMarkup>();
            var bridgeBarriers = new HashSet<Transform>();
            foreach (var owner in gateOwners)
            foreach (var barrier in owner.GetComponentInChildren<WorldBridgeAccessGate>(true).EntryBarriers)
            {
                bridgeBarriers.Add(barrier.transform);
                markups.Add(new NavMeshBuildMarkup { root = barrier.transform, ignoreFromBuild = true });
            }
            Physics.SyncTransforms();

            var surfaces = new List<BakedSurface>();
            var signature = new StringBuilder();
            float tileSize = profile.WorldSize / profile.TileCount;
            for (int z = 0; z < profile.TileCount; z++)
            for (int x = 0; x < profile.TileCount; x++)
            {
                string id = TileId(x, z);
                var bounds = new Bounds(new Vector3((x + .5f) * tileSize, profile.OriginY + profile.VerticalRange / 2, (z + .5f) * tileSize),
                    new Vector3(tileSize, profile.VerticalRange + 20, tileSize));
                BuildSurface(id, bounds, null);
            }
            foreach (var group in caveModules.GroupBy(module => module.caveGuid).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                PlacementIdentity.ValidateGuid(group.Key);
                var colliders = group.SelectMany(module => module.GetComponentsInChildren<Collider>(true)).Where(c => c.enabled && !c.isTrigger).ToArray();
                if (colliders.Length == 0) throw new InvalidDataException("동굴의 Collider가 없습니다 · " + group.Key);
                var bounds = colliders[0].bounds;
                foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);
                bounds.Expand(new Vector3(.02f, 2, .02f));
                BuildSurface(CaveId(group.Key), bounds, group.Key);
            }
            catalog.surfaces = surfaces.ToArray();
            catalog.geometryHash = WorldDefinition.HashBytes(Encoding.UTF8.GetBytes(signature.ToString()));
            var gateList = new WorldBridgeAccessGate[layout.crossings.Length];
            var gates = new HashSet<WorldBridgeAccessGate>();
            var baked = new List<BakedCrossing>();
            var filter = new NavMeshQueryFilter { agentTypeID = layout.agentTypeId, areaMask = NavMesh.AllAreas };
            if (NavMesh.CalculateTriangulation().vertices.Length != 0)
                throw new InvalidOperationException("Bake 경로를 검증하기 전에 다른 활성 NavMesh를 닫으세요.");

            foreach (var crossing in layout.crossings)
            {
                bool fromCave = crossing.fromSurface.StartsWith("cave_", StringComparison.Ordinal);
                bool toCave = crossing.toSurface.StartsWith("cave_", StringComparison.Ordinal);
                if ((fromCave || toCave) && (fromCave == toCave || string.IsNullOrEmpty(crossing.portalId)) ||
                    !(fromCave || toCave) && !string.IsNullOrEmpty(crossing.portalId))
                    throw new InvalidDataException("동굴 Navigation 링크에는 지상 surface와 외부 포털 ID가 필요합니다 · " + crossing.id);
                if (!string.IsNullOrEmpty(crossing.portalId))
                {
                    var portal = portalMarkers.SingleOrDefault(p => p.id == crossing.portalId);
                    if (portal == null || (crossing.fromSurface != CaveId(portal.caveGuid) && crossing.toSurface != CaveId(portal.caveGuid)))
                        throw new InvalidDataException("동굴 링크가 지정 포털과 그 동굴 surface를 연결하지 않습니다 · " + crossing.id);
                    var inside = crossing.fromSurface == CaveId(portal.caveGuid) ? crossing.from : crossing.to;
                    var outside = crossing.fromSurface == CaveId(portal.caveGuid) ? crossing.to : crossing.from;
                    if (Vector3.Dot(inside - portal.position, portal.outward) >= 0 || Vector3.Dot(outside - portal.position, portal.outward) <= 0 ||
                        (inside - portal.position).magnitude > layout.endpointTolerance * 2 || (outside - portal.position).magnitude > layout.endpointTolerance * 2)
                        throw new InvalidDataException("동굴 포털 링크의 안쪽·바깥쪽 방향 또는 거리가 다릅니다 · " + crossing.id);
                }
                if (!string.IsNullOrEmpty(crossing.bridgeId))
                {
                    var owner = gateOwners.SingleOrDefault(m => m.record.id == crossing.bridgeId);
                    var gate = owner != null ? owner.GetComponentInChildren<WorldBridgeAccessGate>(true) : null;
                    if (gate == null || !gate.IsConfigured || !gates.Add(gate))
                        throw new InvalidDataException("교량 준비 게이트가 없거나 복수 링크가 같은 게이트를 소유합니다 · " + crossing.bridgeId);
                    Vector3 a = gate.EntryBarriers[0].transform.position;
                    Vector3 b = gate.EntryBarriers[1].transform.position;
                    if (DistanceToSpan(crossing.from, a, b) > layout.endpointTolerance ||
                        DistanceToSpan(crossing.to, a, b) > layout.endpointTolerance)
                        throw new InvalidDataException("교량 링크 끝점이 교량 통과 구간을 벗어납니다 · " + crossing.id);
                    gateList[baked.Count] = gate;
                }
                baked.Add(new BakedCrossing { id = crossing.id, portalId = crossing.portalId, bridgeId = crossing.bridgeId,
                    fromSurface = crossing.fromSurface, toSurface = crossing.toSurface, width = crossing.width,
                    from = Snap(crossing.fromSurface, crossing.from, true), to = Snap(crossing.toSurface, crossing.to, true) });
            }
            catalog.crossings = baked.ToArray();
            catalog.routes = layout.routes.Select(route => new BakedRoute { id = route.id, fromSurface = route.fromSurface,
                toSurface = route.toSurface, requiredCrossingId = route.requiredCrossingId,
                from = Snap(route.fromSurface, route.from), to = Snap(route.toSurface, route.to) }).ToArray();
            for (int i = 0; i < catalog.anchors.Length; i++)
            {
                if (!catalog.anchors[i].required) continue;
                var point = catalog.anchors[i].position;
                catalog.anchors[i].position = catalog.routes.SelectMany(route => new[] { route.from, route.to })
                    .OrderBy(endpoint => (endpoint - point).sqrMagnitude).First();
            }
            ValidatePaths(catalog);
            var root = new GameObject("WorldBuilder Navigation");
            SceneManager.MoveGameObjectToScene(root, scene);
            var navigation = root.AddComponent<WorldBakedNavigation>();
            navigation.catalog = catalog;
            navigation.crossingGates = gateList;

            void BuildSurface(string id, Bounds bounds, string cave)
            {
                checkpoint("Navigation · " + id);
                var sources = new List<NavMeshBuildSource>();
                var halo = bounds;
                halo.Expand(layout.sourceHalo * 2);
                NavMeshBuilder.CollectSources(halo, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
                sources.RemoveAll(source => source.component == null || source.component.gameObject.scene != scene ||
                    bridgeBarriers.Contains(source.component.transform) ||
                    source.component.GetComponentInParent<WorldCaveModule>()?.caveGuid != cave);
                if (sources.Count == 0) throw new InvalidDataException("NavMesh의 Collider 원본이 없습니다 · " + id);
                string geometryHash = WorldDefinition.HashBytes(Encoding.UTF8.GetBytes(string.Join("\n",
                    sources.Select(source => SourceKey(source, originalTerrain)).OrderBy(key => key, StringComparer.Ordinal))));
                signature.Append(id).Append(' ').AppendLine(geometryHash);
                var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
                if (data == null) throw new InvalidDataException("NavMesh Bake 실패 · " + id);
                AssetDatabase.CreateAsset(data, folder + "/Nav_" + id + ".asset");
                surfaces.Add(new BakedSurface { id = id, geometryHash = geometryHash, bounds = bounds, data = data });
            }

            Vector3 Snap(string id, Vector3 position, bool unique = false)
            {
                var index = surfaces.FindIndex(surface => surface.id == id);
                if (index < 0) throw new InvalidDataException("Navigation surface가 없습니다 · " + id);
                if (!Sample(surfaces[index], position, layout.endpointTolerance, out Vector3 hit))
                    throw new InvalidDataException("지정한 surface에서 링크 또는 필수 지점을 찾지 못했습니다 · " + id + " " + position);
                if (unique)
                {
                    const float attachmentTolerance = .12f;
                    foreach (var other in surfaces)
                    {
                        if (other.id == id || other.bounds.SqrDistance(hit) > attachmentTolerance * attachmentTolerance) continue;
                        if (Sample(other, hit, attachmentTolerance, out _))
                            throw new InvalidDataException("링크 끝점이 둘 이상의 surface에 붙습니다 · " + id + " / " + other.id + " " + hit);
                    }
                }
                return hit;
            }

            bool Sample(BakedSurface surface, Vector3 point, float distance, out Vector3 target)
            {
                target = default;
                var handle = NavMesh.AddNavMeshData(surface.data);
                try
                {
                    if (!handle.valid || !NavMesh.SamplePosition(point, out var hit, distance, filter)) return false;
                    target = hit.position;
                    return true;
                }
                finally { if (handle.valid) handle.Remove(); }
            }
        }

        internal static void ValidatePaths(WorldBakeCatalog catalog)
        {
            if (NavMesh.CalculateTriangulation().vertices.Length != 0)
                throw new InvalidOperationException("Bake 경로를 검증하기 전에 다른 활성 NavMesh를 닫으세요.");
            var filter = new NavMeshQueryFilter { agentTypeID = catalog.agentTypeId, areaMask = NavMesh.AllAreas };
            foreach (var crossing in catalog.crossings)
            {
                var first = RegisterSurface(catalog.surfaces.Single(s => s.id == crossing.fromSurface));
                NavMeshDataInstance second = default;
                NavMeshLinkInstance link = default;
                try
                {
                    if (crossing.fromSurface != crossing.toSurface)
                    {
                        second = RegisterSurface(catalog.surfaces.Single(s => s.id == crossing.toSurface));
                        if (Reachable(crossing.from, crossing.to, filter))
                            throw new InvalidDataException("링크 없이 양쪽 surface가 연결돼 있습니다 · " + crossing.id);
                    }
                    else if (crossing.from == crossing.to)
                        throw new InvalidDataException("교량 링크의 양 끝이 같은 위치에 붙었습니다 · " + crossing.id);
                    link = RegisterLink(crossing);
                    if (!Reachable(crossing.from, crossing.to, filter))
                        throw new InvalidDataException("링크를 통해 양쪽 surface를 횡단할 수 없습니다 · " + crossing.id);
                }
                finally
                {
                    if (link.valid) link.Remove();
                    if (second.valid) second.Remove();
                    first.Remove();
                }
            }

            var handles = new List<NavMeshDataInstance>();
            var links = new NavMeshLinkInstance[catalog.crossings.Length];
            try
            {
                foreach (var surface in catalog.surfaces) handles.Add(RegisterSurface(surface));
                for (int i = 0; i < links.Length; i++) links[i] = RegisterLink(catalog.crossings[i]);
                foreach (var route in catalog.routes)
                {
                    if (!Reachable(route.from, route.to, filter))
                        throw new InvalidDataException("필수 경로가 끊겼습니다 · " + route.id);
                    if (string.IsNullOrEmpty(route.requiredCrossingId)) continue;
                    int index = Array.FindIndex(catalog.crossings, link => link.id == route.requiredCrossingId);
                    if (index < 0) throw new InvalidDataException("필수 링크 ID 누락 · " + route.id);
                    var crossing = catalog.crossings[index];
                    links[index].Remove();
                    bool forward = Reachable(route.from, crossing.from, filter) && Reachable(crossing.to, route.to, filter);
                    bool reverse = Reachable(route.from, crossing.to, filter) && Reachable(crossing.from, route.to, filter);
                    if (!forward && !reverse) throw new InvalidDataException("필수 경로에서 지정 링크에 접근할 수 없습니다 · " + route.id);
                    links[index] = RegisterLink(crossing);
                }
                var required = catalog.anchors.Where(anchor => anchor.required).ToArray();
                for (int i = 1; i < required.Length; i++)
                    if (!Reachable(required[0].position, required[i].position, filter))
                        throw new InvalidDataException("필수 POI가 시작점에서 접근 불가능합니다 · " + required[i].id);
            }
            finally
            {
                foreach (var link in links) if (link.valid) link.Remove();
                foreach (var handle in handles) if (handle.valid) handle.Remove();
            }

            NavMeshDataInstance RegisterSurface(BakedSurface surface)
            {
                var handle = NavMesh.AddNavMeshData(surface.data);
                if (!handle.valid) throw new InvalidDataException("NavMesh 등록 실패 · " + surface.id);
                return handle;
            }

            NavMeshLinkInstance RegisterLink(BakedCrossing link)
            {
                var handle = NavMesh.AddLink(WorldBakedNavigation.LinkData(link, catalog.agentTypeId));
                if (!handle.valid) throw new InvalidDataException("NavMesh 링크 등록 실패 · " + link.id);
                return handle;
            }
        }

        internal static bool Reachable(Vector3 from, Vector3 to, NavMeshQueryFilter filter)
        {
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(from, to, filter, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        internal static string TileId(int x, int z) => "tile_" + x + "_" + z;
        internal static string CaveId(string cave) => "cave_" + cave;

        private static float DistanceToSpan(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 direction = b - a;
            float t = direction.sqrMagnitude > 0 ? Mathf.Clamp01(Vector3.Dot(point - a, direction) / direction.sqrMagnitude) : 0;
            return Vector3.Distance(point, a + t * direction);
        }

        private static string SourceKey(NavMeshBuildSource source, IReadOnlyDictionary<TerrainData, TerrainData> originalTerrain)
        {
            var asset = source.sourceObject;
            if (asset is TerrainData data && originalTerrain.TryGetValue(data, out var original)) asset = original;
            string guid = "";
            long fileId = 0;
            if (asset != null) AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out fileId);
            string path = AssetDatabase.GetAssetPath(asset);
            return source.shape + "|" + source.area + "|" + source.transform.ToString("R") + "|" + source.size.ToString("R") +
                "|" + guid + "|" + fileId + "|" + (string.IsNullOrEmpty(path) ? "" : AssetDatabase.GetAssetDependencyHash(path).ToString());
        }
    }
}

