using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;
using Object = UnityEngine.Object;

namespace UPlayGround.WorldBuilder.Editor
{
    internal static class WorldBakeStage
    {
        internal static WorldBakeCatalog Save(WorldDefinition definition, WorldNavigationLayout layout, string revision, string buildId, Action<string> step)
        {
            var source = SceneManager.GetActiveScene();
            string scenePath = revision + "/World.unity";
            if (!EditorSceneManager.SaveScene(source, scenePath, saveAsCopy: true))
                throw new IOException("revision 씬 사본 저장 실패");
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var catalog = ScriptableObject.CreateInstance<WorldBakeCatalog>();
                catalog.worldGuid = definition.WorldGuid; catalog.buildId = buildId; catalog.scenePath = scenePath;
                catalog.sceneGuid = AssetDatabase.AssetPathToGUID(scenePath, AssetPathToGUIDOptions.OnlyExistingAssets);
                catalog.anchors = definition.Planning == null ? Array.Empty<BakedAnchor>() :
                    definition.Planning.pois.Select(p => new BakedAnchor { id = p.guid, position = Point(p.anchor), required = p.isLocked }).ToArray();
                AssetDatabase.CreateAsset(catalog, revision + "/Catalog.asset");

                foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = transform.gameObject;
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                var copied = new Dictionary<string, string>(StringComparer.Ordinal);
                string owner = "Assets/WorldBuilder/Worlds/" + definition.WorldGuid + "/";
                string dataFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(definition))?.Replace('\\', '/') + "/";
                string resources = revision + "/RevisionAssets";
                WorldBuilderAuthoringAssets.EnsureFolder(resources);
                var originalTerrain = new Dictionary<TerrainData, TerrainData>();
                foreach (var root in scene.GetRootGameObjects())
                foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
                {
                    step("Terrain · " + terrain.name);
                    var copy = (TerrainData)Copy(terrain.terrainData, true);
                    RemapTerrain(copy);
                    originalTerrain.Add(copy, terrain.terrainData);
                    terrain.terrainData = copy;
                    if (!terrain.TryGetComponent<TerrainCollider>(out var collider))
                        throw new InvalidDataException("TerrainCollider가 없습니다 · " + terrain.name);
                    collider.terrainData = copy;
                }
                WorldNavigationBake.Build(scene, definition.Profile, layout, catalog, revision, originalTerrain, step);
                catalog.trees = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldEcologyPreview>(true))
                    .SelectMany(preview => preview.trees ?? Array.Empty<EcologyTreeRecord>()).ToArray();

                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidDataException("revision 씬에 Missing Script가 있습니다.");
                    if (component is WorldPlacementInstance placement)
                    {
                        SetIdentity(placement.gameObject, placement.record.id);
                        Object.DestroyImmediate(placement);
                        continue;
                    }
                    if (component is WorldPlanningInstance structure)
                    {
                        SetIdentity(structure.gameObject, structure.record.id);
                        Object.DestroyImmediate(structure);
                        continue;
                    }
                    if (component is WorldPlacementRoot or WorldPlanningPreview or WorldRoadPreview or WorldEcologyPreview or
                        WorldWaterQueryProbe or WorldPlanningNavigationPreview)
                    {
                        Object.DestroyImmediate(component);
                        continue;
                    }
                    if (component is WorldWaterQuery query)
                    {
                        query.definition = null; query.source = null; query.appliedHash = null; query.hierarchyHash = null;
                    }
                    if (component is WaterVolume volume)
                    {
                        volume.BindEcology(null);
                        var dry = volume.gameObject.AddComponent<WorldBakedWaterExclusions>();
                        dry.volumes = definition.EcologyDryVolumes.ToArray();
                    }
                    Remap(component);
                }
                catalog.placementIds = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("revision 씬 저장 실패");
                foreach (string dependency in AssetDatabase.GetDependencies(scenePath, true))
                {
                    if (!Owns(dependency) || dependency.StartsWith(revision + "/", StringComparison.Ordinal)) continue;
                    var type = AssetDatabase.GetMainAssetTypeAtPath(dependency);
                    if (type == typeof(TerrainLayer) || type == typeof(GameObject))
                        throw new InvalidDataException("월드 소유 TerrainLayer 또는 Prefab/모델이 revision 밖에 있습니다 · " + dependency);
                }
                return catalog;

                bool Owns(string path) => path.StartsWith(owner, StringComparison.Ordinal) ||
                    (dataFolder != "Assets/10.Datas/WorldBuilder/" && path.StartsWith(dataFolder, StringComparison.Ordinal));

                void SetIdentity(GameObject node, string id)
                {
                    if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("발행 인스턴스 ID 누락·중복 · " + id);
                    node.AddComponent<WorldBakedIdentity>().id = id;
                }

                void RemapTerrain(TerrainData terrain)
                {
                    var layers = terrain.terrainLayers;
                    var bakedLayers = layers.Select(layer => layer == null ? null : (TerrainLayer)Copy(layer, false)).ToArray();
                    bool changed = !layers.SequenceEqual(bakedLayers);
                    if (changed)
                    {
                        var weights = terrain.GetAlphamaps(0, 0, terrain.alphamapWidth, terrain.alphamapHeight);
                        terrain.terrainLayers = bakedLayers;
                        terrain.SetAlphamaps(0, 0, weights);
                    }

                    var trees = terrain.treePrototypes;
                    bool treesChanged = false;
                    foreach (var tree in trees)
                    {
                        if (tree.prefab == null) continue;
                        var prefab = (GameObject)Copy(tree.prefab, false);
                        treesChanged |= tree.prefab != prefab;
                        tree.prefab = prefab;
                    }
                    if (treesChanged)
                    {
                        var instances = terrain.treeInstances;
                        terrain.treePrototypes = trees;
                        terrain.SetTreeInstances(instances, false);
                    }

                    var details = terrain.detailPrototypes;
                    bool detailsChanged = false;
                    foreach (var detail in details)
                    {
                        var prefab = detail.prototype == null ? null : (GameObject)Copy(detail.prototype, false);
                        var texture = detail.prototypeTexture == null ? null : (Texture2D)Copy(detail.prototypeTexture, false);
                        detailsChanged |= detail.prototype != prefab || detail.prototypeTexture != texture;
                        detail.prototype = prefab;
                        detail.prototypeTexture = texture;
                    }
                    if (detailsChanged)
                    {
                        var densities = Enumerable.Range(0, details.Length).Select(layer =>
                            terrain.GetDetailLayer(0, 0, terrain.detailWidth, terrain.detailHeight, layer)).ToArray();
                        terrain.detailPrototypes = details;
                        for (int layer = 0; layer < densities.Length; layer++) terrain.SetDetailLayer(0, 0, layer, densities[layer]);
                    }

                    if (!changed && !treesChanged && !detailsChanged) return;
                    EditorUtility.SetDirty(terrain);
                    AssetDatabase.SaveAssetIfDirty(terrain);
                }

                void Remap(Object obj)
                {
                    using var serialized = new SerializedObject(obj);
                    using var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference ||
                            property.propertyPath == "m_Script" || !property.editable || property.objectReferenceValue == null) continue;
                        var target = property.objectReferenceValue;
                        var copy = Copy(target, false);
                        if (copy != target) property.objectReferenceValue = copy;
                    }
                    if (serialized.ApplyModifiedPropertiesWithoutUndo() && EditorUtility.IsPersistent(obj)) AssetDatabase.SaveAssetIfDirty(obj);
                }

                Object Copy(Object obj, bool force)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path) || path.StartsWith(revision + "/", StringComparison.Ordinal)) return obj;
                    bool own = Owns(path);
                    bool prefab = obj is GameObject && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
                    bool model = own && AssetImporter.GetAtPath(path) is ModelImporter;
                    if (!force && (!own || !prefab && !(model && obj is GameObject) &&
                        obj is not (Texture or Mesh or Material or TerrainLayer or WaterfallPreset))) return obj;
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
                        throw new InvalidDataException("복제할 리소스 GUID를 확인할 수 없습니다 · " + path);
                    if (!copied.TryGetValue(path, out string to))
                    {
                        to = resources + "/" + guid + Path.GetExtension(path);
                        if (!AssetDatabase.CopyAsset(path, to)) throw new IOException("revision 리소스 복사 실패 · " + path);
                        copied.Add(path, to);
                        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(to))
                            if (sub is TerrainData or Material or TerrainLayer or WaterfallPreset) Remap(sub);
                        if (model && AssetImporter.GetAtPath(to) is ModelImporter importer)
                        {
                            bool updated = false;
                            foreach (var reference in importer.GetExternalObjectMap())
                            {
                                if (reference.Value == null) continue;
                                var baked = Copy(reference.Value, false);
                                if (baked == reference.Value) continue;
                                importer.AddRemap(reference.Key, baked);
                                updated = true;
                            }
                            if (updated) importer.SaveAndReimport();
                        }
                        if (prefab)
                        {
                            var contents = PrefabUtility.LoadPrefabContents(to);
                            try
                            {
                                foreach (var component in contents.GetComponentsInChildren<Component>(true))
                                {
                                    if (component == null) throw new InvalidDataException("복제한 Prefab에 Missing Script가 있습니다 · " + path);
                                    Remap(component);
                                }
                                if (PrefabUtility.SaveAsPrefabAsset(contents, to) == null)
                                    throw new IOException("복제한 Prefab 참조 저장 실패 · " + path);
                            }
                            finally { PrefabUtility.UnloadPrefabContents(contents); }
                        }
                    }
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(to))
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long candidate) && candidate == localId) return asset;
                    throw new InvalidDataException("복제한 리소스의 file ID가 다릅니다 · " + path);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(source);
            }
        }

        private static Vector3 Point(Core.PlanPoint point) => new((float)point.x, (float)point.y, (float)point.z);
    }
}

