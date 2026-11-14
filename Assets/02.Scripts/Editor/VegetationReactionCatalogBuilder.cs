using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>검토한 원본 목록에서 프로젝트 소유 대응표·재질·Detail 프리팹을 만든다.</summary>
    public static class VegetationReactionCatalogBuilder
    {
        [Serializable] private sealed class Input { public Row[] rows; }
        [Serializable] private sealed class Row { public string mesh; }

        /// <summary>기존 산출물은 덮어쓰지 않고 재사용한다. 원본과 씬은 변경하지 않는다.</summary>
        public static VegetationReactionCatalogSO Prepare(Scene scene, string inventoryPath, string folder,
            string[] excludedMeshes, ReactiveEnvironmentDefinitionSO vegetation, ReactiveEnvironmentDefinitionSO terrain)
        {
            var existing = AssetDatabase.LoadAssetAtPath<VegetationReactionCatalogSO>(folder + "/Catalog.asset");
            if (existing != null) return existing;
            if (Directory.Exists(folder)) throw new InvalidOperationException("불완전하거나 기존 사용자 폴더가 있습니다: " + folder);
            string staging = folder + ".staging-" + Guid.NewGuid().ToString("N");
            try
            {
                var catalog = PrepareAt(scene, inventoryPath, staging, excludedMeshes, vegetation, terrain);
                string error = AssetDatabase.MoveAsset(staging, folder);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                return catalog;
            }
            catch
            {
                if (AssetDatabase.IsValidFolder(staging)) AssetDatabase.DeleteAsset(staging);
                throw;
            }
        }

        private static VegetationReactionCatalogSO PrepareAt(Scene scene, string inventoryPath, string folder,
            string[] excludedMeshes, ReactiveEnvironmentDefinitionSO vegetation, ReactiveEnvironmentDefinitionSO terrain)
        {
            string catalogPath = folder + "/Catalog.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VegetationReactionCatalogSO>(catalogPath);
            if (existing != null) return existing;
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(folder + "/Materials");
            Directory.CreateDirectory(folder + "/Details");
            var catalog = ScriptableObject.CreateInstance<VegetationReactionCatalogSO>();
            catalog.sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
            catalog.protectedVegetation = UnityEngine.Object.Instantiate(vegetation);
            catalog.protectedVegetation.name = "ProtectedVegetation";
            catalog.protectedVegetation.definitionId = "environment.protectedVegetation";
            catalog.protectedVegetation.reaction.canIgnite = false;
            catalog.protectedVegetation.reaction.canBreak = false;
            catalog.protectedVegetation.reaction.initialFuel = 0;
            catalog.protectedVegetation.spreadRadius = 0;
            catalog.protectedVegetation.spreadHeatPerSecond = 0;
            catalog.protectedVegetation.presentation = null;
            AssetDatabase.CreateAsset(catalog.protectedVegetation, folder + "/ProtectedVegetation.asset");
            catalog.terrainDefinition = UnityEngine.Object.Instantiate(terrain);
            catalog.terrainDefinition.name = "TerrainVegetation";
            catalog.terrainDefinition.definitionId = "environment.terrainVegetation";
            catalog.terrainDefinition.spreadRadius = 32;
            AssetDatabase.CreateAsset(catalog.terrainDefinition, folder + "/TerrainVegetation.asset");
            var excluded = new HashSet<string>(excludedMeshes);
            var seen = new HashSet<string>();
            foreach (var row in JsonUtility.FromJson<Input>("{\"rows\":" + File.ReadAllText(inventoryPath) + "}").rows)
            {
                string guid = AssetDatabase.AssetPathToGUID(row.mesh);
                if (string.IsNullOrEmpty(guid) || !seen.Add(guid)) continue;
                catalog.sources.Add(new VegetationReactionCatalogSO.Source { meshGuid = guid,
                    definition = excluded.Contains(row.mesh) ? null : vegetation,
                    reason = excluded.Contains(row.mesh) ? "수면·비식생 부속 형상: 원래 소유 시스템 유지" : "전수 검토한 식생 메시 원본. 비파괴 반응, 원본 형상/LOD 유지" });
            }
            AssetDatabase.CreateAsset(catalog, catalogPath);
            var materialMap = new Dictionary<Material, Material>();
            foreach (var plant in VegetationReactionAuthoring.Collect(scene, catalog))
            foreach (var renderer in plant.surfaces)
            foreach (var material in renderer.sharedMaterials) EnsureMaterial(material, folder, catalog, materialMap);
            Terrain target = null;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var value in root.GetComponentsInChildren<Terrain>(true))
            {
                if (target != null) throw new InvalidOperationException("Terrain별 대응표를 분리하세요.");
                target = value;
            }
            if (target != null)
            {
                var details = target.terrainData.detailPrototypes;
                catalog.detailPrefabs = new GameObject[details.Length];
                for (int i = 0; i < details.Length; i++)
                {
                    var source = details[i].prototype;
                    if (source == null) throw new InvalidOperationException("메시 Detail 원본이 필요합니다.");
                    var filter = source.GetComponent<MeshFilter>();
                    var renderer = source.GetComponent<MeshRenderer>();
                    if (filter == null || renderer == null) throw new InvalidOperationException("Detail 원본 메시/재질이 없습니다.");
                    var preview = EditorSceneManager.NewPreviewScene();
                    try
                    {
                        var instance = new GameObject("ReactiveDetail_" + i);
                        SceneManager.MoveGameObjectToScene(instance, preview);
                        instance.transform.localScale = source.transform.localScale;
                        instance.transform.localRotation = source.transform.localRotation;
                        instance.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                        var output = instance.AddComponent<MeshRenderer>();
                        var materials = renderer.sharedMaterials;
                        for (int j = 0; j < materials.Length; j++) materials[j] = EnsureMaterial(materials[j], folder, catalog, materialMap);
                        output.sharedMaterials = materials;
                        output.shadowCastingMode = renderer.shadowCastingMode;
                        output.receiveShadows = renderer.receiveShadows;
                        catalog.detailPrefabs[i] = PrefabUtility.SaveAsPrefabAsset(instance, folder + "/Details/Layer" + i + ".prefab");
                    }
                    finally { EditorSceneManager.ClosePreviewScene(preview); }
                }
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static Material EnsureMaterial(Material source, string folder, VegetationReactionCatalogSO catalog,
            Dictionary<Material, Material> materials)
        {
            if (source == null) throw new InvalidOperationException("원본 재질이 누락됐습니다.");
            if (materials.TryGetValue(source, out var existing)) return existing;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
            string shaderGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source.shader));
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(VegetationReactionShaderAuthoring.OutputFolder + "/" + shaderGuid + ".shader");
            if (shader == null) throw new InvalidOperationException("파생 셰이더 누락: " + source.shader.name);
            var material = CreateMaterialCopy(source, shader);
            string path = folder + "/Materials/" + guid + "_" + localId + ".mat";
            AssetDatabase.CreateAsset(material, path);
            catalog.surfaces.Add(new VegetationReactionCatalogSO.Surface { original = source, reactive = material });
            materials.Add(source, material);
            return material;
        }

        /// <summary>Variant 부모의 셰이더 상속을 해제하고 유효한 원본 속성값을 보존한다.</summary>
        internal static Material CreateMaterialCopy(Material source, Shader shader)
        {
            var material = new Material(source) { name = source.name + "_Reactive" };
            if (material.isVariant) material.parent = null;
            material.shader = shader;
            material.SetFloat("_AllowFuelCoverage", 0);
            material.enableInstancing = source.enableInstancing;
            return material;
        }
    }
}
