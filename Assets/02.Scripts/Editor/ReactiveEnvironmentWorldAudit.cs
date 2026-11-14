using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Gameplay.World.ReactiveEnvironment;
using UPlayGround.Manager.World;

namespace UPlayGround.EditorTools
{
    /// <summary>비활성·중첩·모든 LOD의 현재 연결과 미분류 대상을 저장 없이 조사한다.</summary>
    public static class ReactiveEnvironmentWorldAudit
    {
        [Serializable] public sealed class Report
        {
            public string scenePath, mapId;
            public int rendererCount, logicalRoots, reactiveRoots, unclassifiedRoots, terrainCount, treeInstances;
            public int gatheringReviewRoots, pendingRoots;
            public List<Entry> entries = new();
            public List<Placement> runtimePlacements = new();
            public List<string> terrains = new();
            public List<TerrainInventory> terrainDetails = new();
        }
        [Serializable] public sealed class Entry
        {
            public string globalId, hierarchyPath, sourceGuid, sourcePath, sceneEntityId, placementGuid, owner;
            public string status, reason;
            public Vector3 position, scale;
            public Quaternion rotation;
            public bool active;
            public int renderers, slots, colliders, lodGroups;
            public List<string> materials = new();
            public List<string> meshes = new();
            public List<string> ownershipEvidence = new();
            public int effectRenderers;
        }
        [Serializable] public sealed class Placement
        {
            public string dataPath, placementGuid, sceneEntityGuid, actorId, prefabGuid, prefabPath, sourceKind;
        }

        [Serializable] public sealed class TerrainInventory
        {
            public string hierarchyPath, dataPath, dataGuid, dependencyHash, sceneEntityId;
            public int width, height;
            public List<DetailLayer> layers = new();
        }
        [Serializable] public sealed class DetailLayer
        {
            public int index, nonzeroPixels;
            public long densitySum;
            public string prefabPath, prefabGuid, texturePath;
            public bool useInstancing, usePrototypeMesh;
            public float minWidth, maxWidth, minHeight, maxHeight;
            public List<RectInt> occupiedTiles = new();
        }

        /// <summary>씬을 변경하지 않으며 미지원 대상을 임의 제외하지 않는다.</summary>
        public static Report Inspect(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("로드된 씬이 필요합니다.");
            var report = new Report { scenePath = scene.path, mapId = RegionOwnership.Resolve(scene) };
            var roots = new Dictionary<GameObject, Entry>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    report.rendererCount++;
                    GameActor actor = renderer.GetComponentInParent<GameActor>(true);
                    var reaction = renderer.GetComponentInParent<ReactiveEnvironmentObject>(true);
                    var lod = renderer.GetComponentInParent<LODGroup>(true);
                    GameObject logical = actor != null ? actor.gameObject : reaction != null ? reaction.gameObject
                        : lod != null ? lod.gameObject : PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
                    if (logical == null) logical = renderer.gameObject;
                    if (!roots.TryGetValue(logical, out var entry))
                    {
                        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(logical);
                        var id = logical.GetComponent<SceneEntityId>();
                        var placement = logical.GetComponent<WorldPlacementMetadata>();
                        entry = new Entry { globalId = GlobalObjectId.GetGlobalObjectIdSlow(logical).ToString(),
                            hierarchyPath = AnimationUtility.CalculateTransformPath(logical.transform, null),
                            sourcePath = path, sourceGuid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path),
                            sceneEntityId = id != null ? id.Guid : null, placementGuid = placement != null ? placement.PlacementGuid : null,
                            position = logical.transform.position, rotation = logical.transform.rotation, scale = logical.transform.lossyScale,
                            active = logical.activeSelf, colliders = logical.GetComponentsInChildren<Collider>(true).Length,
                            lodGroups = logical.GetComponentsInChildren<LODGroup>(true).Length,
                            owner = actor != null ? actor.GetType().FullName : null,
                            status = reaction != null ? "Reactive" : actor is GatheringActor ? "GatheringReview" : actor != null ? "ExistingSystem" : "Unclassified",
                            reason = reaction != null ? "반응 구성 존재. 유효성·플레이 별도" : actor is GatheringActor
                                ? "채집 소유권 확인. 환경 인계·재성장·낚시 정책 검수 전이며 완료 제외가 아님"
                                : actor != null ? "기존 액터 소유" : "원본별 프로필·슬롯·소유권 검수 필요" };
                        roots.Add(logical, entry); report.entries.Add(entry);
                        if (entry.status == "Reactive") report.reactiveRoots++;
                        if (entry.status == "Unclassified") report.unclassifiedRoots++;
                        if (entry.status == "GatheringReview") report.gatheringReviewRoots++;
                    }
                    entry.renderers++;
                    if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
                    {
                        entry.effectRenderers++;
                        entry.ownershipEvidence.Add(AnimationUtility.CalculateTransformPath(renderer.transform, logical.transform)
                            + " | " + renderer.GetType().FullName);
                    }
                    var water = renderer.GetComponentInParent<FootstepWaterSurface>(true);
                    if (water != null) entry.ownershipEvidence.Add("수역 | " + GlobalObjectId.GetGlobalObjectIdSlow(water));
                    var filter = renderer.GetComponent<MeshFilter>();
                    Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : filter != null ? filter.sharedMesh : null;
                    if (mesh != null)
                    {
                        string meshPath = AssetDatabase.GetAssetPath(mesh);
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string meshGuid, out long localId);
                        string key = meshPath + " | " + meshGuid + ":" + localId;
                        if (!entry.meshes.Contains(key)) entry.meshes.Add(key);
                    }
                    var materials = renderer.sharedMaterials;
                    entry.slots += materials.Length;
                    foreach (var material in materials)
                    {
                        string path = material != null ? AssetDatabase.GetAssetPath(material) : "<missing>";
                        string key = path + " | " + (material != null && material.shader != null ? material.shader.name : "<missing>");
                        if (!entry.materials.Contains(key)) entry.materials.Add(key);
                    }
                }
                foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    report.terrainCount++;
                    if (terrain.terrainData == null) { report.terrains.Add("TerrainData 없음"); continue; }
                    report.treeInstances += terrain.terrainData.treeInstanceCount;
                    report.terrains.Add(AnimationUtility.CalculateTransformPath(terrain.transform, null) + " | "
                        + AssetDatabase.GetAssetPath(terrain.terrainData) + " | Detail=" + terrain.terrainData.detailPrototypes.Length
                        + " | Resolution=" + terrain.terrainData.detailWidth + " | Adapter=" + (terrain.GetComponent<TerrainDetailReaction>() != null));
                    report.terrainDetails.Add(InspectTerrain(terrain));
                }
                foreach (var loader in root.GetComponentsInChildren<RuntimePlacementLoader>(true))
                {
                    if (loader.PlacementData == null) continue;
                    foreach (var record in loader.PlacementData.Records)
                    {
                        if (record == null) continue;
                        string path = AssetDatabase.GetAssetPath(record.prefab);
                        report.runtimePlacements.Add(new Placement { dataPath = AssetDatabase.GetAssetPath(loader.PlacementData),
                            placementGuid = record.placementGuid, sceneEntityGuid = record.sceneEntityGuid, actorId = record.actorId,
                            prefabPath = path, prefabGuid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path),
                            sourceKind = record.sourceKind.ToString() });
                    }
                }
            }
            foreach (var entry in report.entries)
                if (entry.status == "Unclassified" && entry.effectRenderers == entry.renderers)
                {
                    entry.status = "ExistingSystem";
                    entry.owner = "UnityEngine.VFX";
                    entry.reason = "모든 Renderer가 입자·선·궤적 표현이다. 실제 컴포넌트 근거는 ownershipEvidence 참조.";
                    report.unclassifiedRoots--;
                }
            report.logicalRoots = report.entries.Count;
            report.pendingRoots = report.unclassifiedRoots + report.gatheringReviewRoots;
            return report;
        }

        /// <summary>모든 레이어의 전체 밀도를 읽고 비어 있지 않은 32픽셀 구역을 빠짐없이 기록한다.</summary>
        public static TerrainInventory InspectTerrain(Terrain terrain)
        {
            var data = terrain.terrainData;
            string path = AssetDatabase.GetAssetPath(data);
            var inventory = new TerrainInventory {
                hierarchyPath = AnimationUtility.CalculateTransformPath(terrain.transform, null),
                dataPath = path, dataGuid = AssetDatabase.AssetPathToGUID(path),
                dependencyHash = string.IsNullOrEmpty(path) ? null : AssetDatabase.GetAssetDependencyHash(path).ToString(),
                sceneEntityId = terrain.GetComponent<SceneEntityId>()?.Guid, width = data.detailWidth, height = data.detailHeight };
            var prototypes = data.detailPrototypes;
            for (int layer = 0; layer < prototypes.Length; layer++)
            {
                var prototype = prototypes[layer];
                string prefabPath = AssetDatabase.GetAssetPath(prototype.prototype);
                var detail = new DetailLayer { index = layer, prefabPath = prefabPath,
                    prefabGuid = string.IsNullOrEmpty(prefabPath) ? null : AssetDatabase.AssetPathToGUID(prefabPath),
                    texturePath = AssetDatabase.GetAssetPath(prototype.prototypeTexture),
                    useInstancing = prototype.useInstancing, usePrototypeMesh = prototype.usePrototypeMesh,
                    minWidth = prototype.minWidth, maxWidth = prototype.maxWidth,
                    minHeight = prototype.minHeight, maxHeight = prototype.maxHeight };
                const int tileSize = 32;
                for (int z = 0; z < data.detailHeight; z += tileSize)
                for (int x = 0; x < data.detailWidth; x += tileSize)
                {
                    int width = Mathf.Min(tileSize, data.detailWidth - x), height = Mathf.Min(tileSize, data.detailHeight - z);
                    var density = data.GetDetailLayer(x, z, width, height, layer);
                    bool occupied = false;
                    foreach (int count in density)
                    {
                        detail.densitySum += count;
                        if (count <= 0) continue;
                        detail.nonzeroPixels++; occupied = true;
                    }
                    if (occupied) detail.occupiedTiles.Add(new RectInt(x, z, width, height));
                }
                inventory.layers.Add(detail);
            }
            return inventory;
        }

        /// <summary>기존 파일을 덮어쓰는 보고서만 작성하며 씬·에셋은 저장하지 않는다.</summary>
        public static Report Write(Scene scene, string reportPath)
        {
            string full = Path.GetFullPath(reportPath);
            string allowed = Path.GetFullPath("output/ReactiveEnvironment") + Path.DirectorySeparatorChar;
            if (!full.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("환경 조사 output 경로가 필요합니다.");
            var report = Inspect(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, JsonUtility.ToJson(report, true));
            return report;
        }
    }
}
