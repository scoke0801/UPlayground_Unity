using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;
using Object = UnityEngine.Object;

namespace UPlayGround.WorldBuilder.Editor
{
    internal static class WorldChunkBake
    {
        public static string CatalogPath(WorldBakeCatalog bake) => Path.GetDirectoryName(bake.scenePath).Replace('\\', '/') + "/Chunks.asset";

        public static void Stage(WorldDefinition definition, WorldBakeCatalog bake, Action validateSource, Action<string> checkpoint)
        {
            var original = SceneManager.GetActiveScene();
            var source = EditorSceneManager.OpenScene(bake.scenePath, OpenSceneMode.Additive);
            var scenes = new List<Scene>();
            try
            {
                // 검증이 끝난 재개방 씬에서 바로 분할해 전체 지형을 중복으로 로드하지 않는다.
                validateSource();
                var roots = source.GetRootGameObjects();
                var terrain = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true))
                    .OrderBy(t => t.transform.position.z).ThenBy(t => t.transform.position.x).ToArray();
                int count = definition.Profile.TileCount;
                float size = definition.Profile.WorldSize / count;
                if (terrain.Length != count * count) throw new InvalidDataException("청크 Terrain 수가 맞지 않습니다.");
                var dependencies = terrain.Select(_ => new HashSet<int>()).ToArray();
                var content = new WorldChunkContent[terrain.Length];
                string folder = Path.GetDirectoryName(bake.scenePath).Replace('\\', '/');
                var catalog = ScriptableObject.CreateInstance<WorldChunkCatalog>();
                catalog.bake = bake;
                catalog.sourceRevision = definition.SourceRevision;
                catalog.bakeCatalogGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(bake));
                catalog.pois = definition.Planning?.pois.Select(p => new WorldChunkPoi {
                    id = p.guid, regionId = p.regionGuid, waterId = p.waterPlanGuid,
                    position = new Vector3((float)p.anchor.x, (float)p.anchor.y, (float)p.anchor.z), locked = p.isLocked
                }).ToArray() ?? Array.Empty<WorldChunkPoi>();
                catalog.tileSize = size;
                catalog.worldBounds = new Bounds(new Vector3(size * count / 2, definition.Profile.OriginY + definition.Profile.VerticalRange / 2, size * count / 2),
                    new Vector3(size * count, definition.Profile.VerticalRange, size * count));
                catalog.chunks = new WorldChunkEntry[terrain.Length];
                for (int i = 0; i < terrain.Length; i++)
                {
                    var tile = new Vector2Int(i % count, i / count);
                    var id = WorldNavigationBake.TileId(tile.x, tile.y);
                    var key = "WorldBuilder/" + bake.worldGuid + "/" + bake.buildId + "/" + id;
                    var data = terrain[i].terrainData;
                    var origin = terrain[i].transform.position;
                    if (origin.x != size * tile.x || origin.z != size * tile.y) throw new InvalidDataException("Terrain의 타일 좌표가 맞지 않습니다.");
                    catalog.chunks[i] = new WorldChunkEntry { id = id, sceneKey = key, tile = tile,
                        bounds = new Bounds(origin + data.size / 2, data.size), proxy = Proxy(terrain[i], folder, id) };
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    scenes.Add(scene);
                    var marker = NewRoot(id, scene);
                    var chunk = marker.AddComponent<WorldChunkContent>();
                    content[i] = chunk;
                    chunk.worldGuid = bake.worldGuid;
                    chunk.revision = bake.buildId;
                    chunk.chunkId = id;
                    chunk.content = NewRoot("청크 콘텐츠", scene);
                    // 준비 전 청크와 같은 상태로 옮겨 타일의 렌더링·충돌 등록을 빨리 해제한다.
                    chunk.content.SetActive(false);
                    chunk.terrain = terrain[i];
                    terrain[i].SetNeighbors(null, null, null, null);
                    terrain[i].allowAutoConnect = false;
                    Move(terrain[i].gameObject, i);
                    // Unity는 제목 없는 씬을 열어 둔 상태에서 다음 Additive 씬을 만들 수 없다.
                    if (!EditorSceneManager.SaveScene(scene, folder + "/" + id + ".unity"))
                        throw new IOException("청크 스테이징 씬을 저장할 수 없습니다 · " + id);
                }

                var navigation = roots.Select(r => r.GetComponent<WorldBakedNavigation>()).Single(n => n != null);
                var crossingOwners = new List<(int index, WorldBridgeAccessGate gate, Collider barrier)>[terrain.Length];
                for (int i = 0; i < crossingOwners.Length; i++) crossingOwners[i] = new();
                var surfaceOwners = catalog.chunks.Select((entry, index) => (entry.id, index)).ToDictionary(p => p.id, p => p.index);
                foreach (var root in roots)
                {
                    if (root.scene != source || root == navigation.gameObject || root.GetComponent<Camera>() || root.GetComponent<Light>()) continue;
                    bool container = root.transform.childCount > 0 && root.GetComponents<Component>()
                        .All(c => c is Transform or WorldWaterQuery or WorldWaterfallEffects);
                    var objects = container ? root.transform.Cast<Transform>().Select(t => t.gameObject).ToArray() : new[] { root };
                    foreach (var obj in objects)
                    {
                        var cave = obj.GetComponentInChildren<WorldCaveModule>(true);
                        var water = obj.GetComponentInChildren<WaterVolume>(true);
                        var bounds = water != null ? water.Bounds : BoundsOf(obj);
                        int owner = Tile(cave != null || water != null ? bounds.center : obj.transform.position);
                        for (int i = 0; i < terrain.Length; i++)
                        {
                            if (i == owner || !OverlapsXZ(bounds, catalog.chunks[i].bounds)) continue;
                            dependencies[i].Add(owner);
                            if (cave != null) dependencies[owner].Add(i);
                        }
                        if (cave != null) surfaceOwners.Add(WorldNavigationBake.CaveId(cave.caveGuid), owner);
                        foreach (var volume in obj.GetComponentsInChildren<WaterVolume>(true)) volume.BindTerrains(Array.Empty<Terrain>());
                        var effects = root.GetComponent<WorldWaterfallEffects>();
                        if (effects != null)
                        {
                            var emitters = effects.emitters.Where(e => e.landing != null && e.landing.IsChildOf(obj.transform)).ToArray();
                            if (emitters.Length != 0)
                            {
                                var owned = obj.AddComponent<WorldWaterfallEffects>();
                                owned.preset = effects.preset;
                                owned.emitters = emitters;
                            }
                        }
                        Move(obj, owner);
                    }
                }

                for (int i = 0; i < bake.crossings.Length; i++)
                {
                    var crossing = bake.crossings[i];
                    int left = surfaceOwners[crossing.fromSurface], right = surfaceOwners[crossing.toSurface];
                    var gate = navigation.crossingGates[i];
                    if (gate != null)
                    {
                        int owner = scenes.IndexOf(gate.gameObject.scene);
                        if (owner < 0) throw new InvalidDataException("교량 소유 청크 누락");
                        Connect(left, owner);
                        Connect(right, owner);
                        crossingOwners[owner].Add((i, gate, null));
                    }
                    else if (!string.IsNullOrEmpty(crossing.portalId))
                    {
                        Connect(left, right);
                        int outside = crossing.fromSurface.StartsWith("cave_", StringComparison.Ordinal) ? right : left;
                        var barrier = NewRoot("포털 준비 장벽 " + crossing.portalId, scenes[outside]);
                        barrier.transform.SetParent(content[outside].content.transform, true);
                        barrier.transform.position = (crossing.from + crossing.to) * .5f + Vector3.up * bake.playerHeight * .45f;
                        Vector3 forward = crossing.to - crossing.from;
                        forward.y = 0;
                        if (forward.sqrMagnitude > 0) barrier.transform.rotation = Quaternion.LookRotation(forward);
                        var collider = barrier.AddComponent<BoxCollider>();
                        collider.size = new Vector3(crossing.width + bake.playerRadius * 2, bake.playerHeight * 1.5f, .25f);
                        crossingOwners[outside].Add((i, null, collider));
                    }
                }

                catalog.boundaryCrossings = BuildBoundaryLinks(bake, catalog, terrain, count);
                for (int i = 0; i < terrain.Length; i++)
                {
                    var chunk = content[i];
                    var entry = catalog.chunks[i];
                    entry.dependencies = dependencies[i].OrderBy(n => n).ToArray();
                    entry.surfaces = surfaceOwners.Where(p => p.Value == i).Select(p => p.Key).ToArray();
                    chunk.identities = chunk.content.GetComponentsInChildren<WorldBakedIdentity>(true);
                    entry.instanceIds = chunk.identities.Select(identity => identity.id).ToArray();
                    chunk.water = chunk.content.GetComponentsInChildren<WaterVolume>(true);
                    chunk.crossingIndices = crossingOwners[i].Select(c => c.index).ToArray();
                    chunk.gates = crossingOwners[i].Select(c => c.gate).ToArray();
                    chunk.crossingBarriers = crossingOwners[i].Select(c => c.barrier).ToArray();
                    checkpoint("청크 씬 · " + entry.id);
                    if (!EditorSceneManager.SaveScene(scenes[i], folder + "/" + entry.id + ".unity"))
                        throw new IOException("청크 씬 저장 실패 · " + entry.id);
                }

                AssetDatabase.CreateAsset(catalog, CatalogPath(bake));

                int Tile(Vector3 center) => Mathf.Clamp(Mathf.FloorToInt(center.x / size), 0, count - 1) +
                    Mathf.Clamp(Mathf.FloorToInt(center.z / size), 0, count - 1) * count;

                void Connect(int a, int b)
                {
                    if (a == b) return;
                    dependencies[a].Add(b);
                    dependencies[b].Add(a);
                }

                void Move(GameObject obj, int index)
                {
                    obj.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(obj, scenes[index]);
                    obj.transform.SetParent(content[index].content.transform, true);
                }
            }
            finally
            {
                foreach (var scene in scenes) EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.CloseScene(source, true);
                SceneManager.SetActiveScene(original);
            }
        }

        public static void Validate(WorldBakeCatalog bake, bool reopen)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(CatalogPath(bake));
            if (catalog == null || catalog.bake != bake || string.IsNullOrWhiteSpace(catalog.sourceRevision) ||
                catalog.chunks.Length == 0 || catalog.chunks.Select(e => e.sceneKey).Distinct().Count() != catalog.chunks.Length ||
                !catalog.chunks.SelectMany(e => e.instanceIds).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(bake.placementIds.OrderBy(id => id, StringComparer.Ordinal)))
                throw new InvalidDataException("청크 catalog의 revision 또는 인스턴스 ID 오류.");
            string folder = Path.GetDirectoryName(bake.scenePath).Replace('\\', '/');
            foreach (var entry in catalog.chunks)
            {
                string path = folder + "/" + entry.id + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null || entry.proxy == null ||
                    entry.dependencies.Any(i => i < 0 || i >= catalog.chunks.Length))
                    throw new InvalidDataException("청크 씬 또는 의존성 누락 · " + entry.id);
                WorldBakePipeline.ValidateRuntimeDependencies(new[] { path, CatalogPath(bake) });
                if (!reopen) continue;
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var marker = scene.GetRootGameObjects().Select(r => r.GetComponent<WorldChunkContent>()).Single(c => c != null);
                    var data = marker.terrain.terrainData;
                    if (marker.worldGuid != bake.worldGuid || marker.revision != bake.buildId || marker.chunkId != entry.id ||
                        marker.content.activeSelf || data.size != entry.bounds.size ||
                        data.heightmapResolution != catalog.tileSize + 1 || data.holesResolution != catalog.tileSize ||
                        marker.terrain.GetComponent<TerrainCollider>().terrainData != data ||
                        marker.content.GetComponentsInChildren<Component>(true).Any(c => c == null))
                        throw new InvalidDataException("재개방한 청크의 revision·Collider·스크립트 오류 · " + entry.id);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        public static void RegisterAddressables(WorldBakeCatalog bake)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(CatalogPath(bake));
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Addressables 설정이 없습니다.");
            string name = "WorldBuilder-" + bake.worldGuid;
            var group = settings.FindGroup(name) ?? settings.CreateGroup(name, false, false, false, null, typeof(BundledAssetGroupSchema));
            group.GetSchema<BundledAssetGroupSchema>().BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            string folder = Path.GetDirectoryName(bake.scenePath).Replace('\\', '/');
            Register(CatalogPath(bake), WorldChunkCatalog.Address(bake.worldGuid, bake.buildId));
            foreach (var entry in catalog.chunks)
                Register(folder + "/" + entry.id + ".unity", entry.sceneKey);
            AssetDatabase.SaveAssetIfDirty(group);
            AssetDatabase.SaveAssetIfDirty(settings);

            void Register(string path, string address)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                var current = settings.FindAssetEntry(guid);
                if (current != null && current.parentGroup != group) throw new InvalidOperationException("다른 그룹이 청크를 소유합니다.");
                if (settings.groups.Where(g => g != null).SelectMany(g => g.entries)
                    .Any(other => other.address == address && other.guid != guid))
                    throw new InvalidOperationException("청크 Addressables 키 충돌 · " + address);
                settings.CreateOrMoveEntry(guid, group).SetAddress(address);
            }
        }

        private static bool OverlapsXZ(Bounds a, Bounds b) =>
            a.min.x < b.max.x && a.max.x > b.min.x && a.min.z < b.max.z && a.max.z > b.min.z;

        private static BakedCrossing[] BuildBoundaryLinks(WorldBakeCatalog bake, WorldChunkCatalog catalog, Terrain[] terrains, int count)
        {
            var result = new List<BakedCrossing>();
            var filter = new NavMeshQueryFilter { agentTypeID = bake.agentTypeId, areaMask = NavMesh.AllAreas };
            var path = new NavMeshPath();
            var surfaces = bake.surfaces.ToDictionary(s => s.id);
            for (int a = 0; a < terrains.Length; a++)
            {
                if (a % count < count - 1) Probe(a, a + 1, Vector3.right, Vector3.forward);
                if (a / count < count - 1) Probe(a, a + count, Vector3.forward, Vector3.right);
            }
            return result.ToArray();

            void Probe(int a, int b, Vector3 direction, Vector3 along)
            {
                var left = catalog.chunks[a];
                var right = catalog.chunks[b];
                // 저작자가 지정한 교량 경계를 자동 링크로 우회하지 않는다.
                if (bake.crossings.Any(c => c.fromSurface == left.id && c.toSurface == right.id ||
                    c.fromSurface == right.id && c.toSurface == left.id)) return;
                var from = NavMesh.AddNavMeshData(surfaces[left.id].data);
                var to = NavMesh.AddNavMeshData(surfaces[right.id].data);
                try
                {
                    float inset = Mathf.Max(1.25f, bake.agentRadius * 2);
                    Vector3 center = (left.bounds.center + right.bounds.center) * .5f;
                    for (int sample = -3; sample <= 3; sample++)
                    {
                        var edge = center + along * (sample * catalog.tileSize / 8);
                        var p = edge - direction * inset;
                        var q = edge + direction * inset;
                        p.y = terrains[a].SampleHeight(p) + terrains[a].transform.position.y;
                        q.y = terrains[b].SampleHeight(q) + terrains[b].transform.position.y;
                        if (Mathf.Abs(p.y - q.y) > Mathf.Tan(bake.agentSlope * Mathf.Deg2Rad) * (2 * inset)) continue;
                        float tolerance = Mathf.Min(inset * .75f, 1f);
                        if (!NavMesh.SamplePosition(p, out var source, tolerance, filter) ||
                            !NavMesh.SamplePosition(q, out var target, tolerance, filter)) continue;
                        if (Reachable(source.position, target.position)) return;
                        var crossing = new BakedCrossing { id = left.id + "_" + right.id, fromSurface = left.id,
                            toSurface = right.id, from = source.position, to = target.position, width = bake.agentRadius * 2 };
                        var link = NavMesh.AddLink(WorldBakedNavigation.LinkData(crossing, bake.agentTypeId));
                        try
                        {
                            if (!link.valid || !Reachable(crossing.from, crossing.to)) continue;
                            result.Add(crossing);
                            return;
                        }
                        finally { if (link.valid) link.Remove(); }
                    }
                }
                finally { if (to.valid) to.Remove(); if (from.valid) from.Remove(); }
            }

            bool Reachable(Vector3 from, Vector3 to) =>
                NavMesh.CalculatePath(from, to, filter, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        private static GameObject NewRoot(string name, Scene scene)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static Bounds BoundsOf(GameObject obj)
        {
            var bounds = new Bounds(obj.transform.position, Vector3.zero);
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
            foreach (var collider in obj.GetComponentsInChildren<Collider>(true)) bounds.Encapsulate(collider.bounds);
            return bounds;
        }

        private static Mesh Proxy(Terrain terrain, string folder, string id)
        {
            const int resolution = 65;
            var vertices = new Vector3[resolution * resolution];
            var triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            var data = terrain.terrainData;
            var origin = terrain.transform.position;
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1), v = z / (float)(resolution - 1);
                vertices[z * resolution + x] = origin + new Vector3(u * data.size.x, data.GetInterpolatedHeight(u, v), v * data.size.z);
            }

            int next = 0;
            for (int z = 0; z < resolution - 1; z++)
            for (int x = 0; x < resolution - 1; x++)
            {
                int a = z * resolution + x;
                triangles[next++] = a;
                triangles[next++] = a + resolution;
                triangles[next++] = a + 1;
                triangles[next++] = a + 1;
                triangles[next++] = a + resolution;
                triangles[next++] = a + resolution + 1;
            }
            var mesh = new Mesh { name = "Proxy " + id, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh, folder + "/Proxy_" + id + ".asset");
            return mesh;
        }
    }
}


