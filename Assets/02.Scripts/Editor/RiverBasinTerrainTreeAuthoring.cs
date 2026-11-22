using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Gameplay.World;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>저장된 숲을 기준으로 수목 승격 에셋과 씬 참조를 생성한다.</summary>
    public static class RiverBasinTerrainTreeAuthoring
    {
        private const string World = "Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/";
        private const string Data = "Assets/10.Datas/Environment/TerrainTreePromotion";
        private const string Prefabs = "Assets/03.Prefabs/Environment/ReactiveTrees";
        private const string Variants = Prefabs + "/RiverBasinTerrain";
        private static readonly (string Source, string Form, string Color)[] Species =
        {
            ("PF_EastLands_Osmanthus_01_Green_8", "", "Green"),
            ("PF_EastLands_Osmanthus_01_Gold_1", "", "Gold"),
            ("PF_EastLands_Osmanthus_02_Green_5", "02", "Green"),
            ("PF_EastLands_Osmanthus_02_Gold_10", "02", "Gold"),
            ("PF_EastLands_Osmanthus_03_Green_7", "03", "Green"),
            ("PF_EastLands_Osmanthus_03_Copper_0", "03", "Copper")
        };

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Edit Mode에서 저작하세요.");
            string path = World + "Preview/RiverBasin4km.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("대상 씬에 저장 전 변경이 있습니다.");
            var preview = EditorSceneManager.NewPreviewScene();
            var created = new List<string>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                EnsureFolder(Data, created);
                EnsureFolder(Variants, created);
                var species = Species.Select(entry => CreateSpecies(entry, preview, created)).ToArray();
                var roots = scene.GetRootGameObjects();
                var terrains = new List<Terrain>();
                var assets = new List<TerrainTreePromotionTileSO>();
                int eligible = 0;
                foreach (var terrain in roots.SelectMany(root => root.GetComponentsInChildren<Terrain>(true)).OrderBy(value => value.name))
                {
                    var data = terrain.terrainData;
                    var prototypes = data.treePrototypes;
                    var mapped = species.Where(entry => prototypes.Any(prototype => prototype.prefab == entry.source)).ToArray();
                    if (mapped.Length == 0 || data.treeInstanceCount == 0) continue;
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data));
                    var table = LoadOrCreateTable(Data + "/" + guid + ".asset", data, prototypes, mapped, created);
                    eligible += table.placements.Count(placement => !string.IsNullOrEmpty(placement.id));
                    terrains.Add(terrain);
                    assets.Add(table);
                }
                if (terrains.Count == 0) throw new InvalidOperationException("승격 대상 타일이 없습니다.");

                var weather = roots.SelectMany(root => root.GetComponentsInChildren<EnvironmentController>(true)).SingleOrDefault();
                var context = roots.SelectMany(root => root.GetComponentsInChildren<SceneContext>(true)).Single();
                var manager = roots.SelectMany(root => root.GetComponentsInChildren<TerrainTreePromotion>(true)).SingleOrDefault();
                GameObject node = manager != null ? manager.gameObject : new GameObject("지형 수목 반응");
                if (manager == null)
                {
                    SceneManager.MoveGameObjectToScene(node, scene);
                    Undo.RegisterCreatedObjectUndo(node, "지형 수목 반응 연결");
                    manager = Undo.AddComponent<TerrainTreePromotion>(node);
                }
                if (weather == null) weather = AddWeather(scene, roots, node, context.MapID, created);
                WriteReferences(manager, terrains, assets, weather);

                foreach (var table in assets) AssetDatabase.SaveAssetIfDirty(table);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("4km 씬 저장에 실패했습니다.");
                Undo.CollapseUndoOperations(group);
                return $"타일 {terrains.Count}장, 승격 대상 {eligible}그루, 목서 {species.Length}종";
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                for (int i = created.Count - 1; i >= 0; i--) AssetDatabase.DeleteAsset(created[i]);
                throw;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static TerrainTreePromotionTileSO.Species CreateSpecies((string Source, string Form, string Color) entry,
            Scene preview, List<string> created)
        {
            var source = Load<GameObject>("Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR1/" + entry.Source + ".prefab");
            string name = "Osmanthus" + entry.Form;
            var baseActor = Load<GameObject>(Prefabs + "/PF_ReactiveEastLands" + name + ".prefab");
            string suffix = name + entry.Color;
            var sourceRenderers = source.GetComponentsInChildren<MeshRenderer>(true).ToDictionary(renderer => renderer.name);
            var display = new Dictionary<string, Material[]>();
            var replacements = new Dictionary<Material, Material>();
            foreach (var renderer in baseActor.transform.Find("Intact").GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!sourceRenderers.TryGetValue(renderer.name, out var sourceRenderer)
                    || sourceRenderer.GetComponent<MeshFilter>().sharedMesh != renderer.GetComponent<MeshFilter>().sharedMesh)
                    throw new InvalidOperationException(source.name + "의 표시·승격 메시가 다릅니다: " + renderer.name);
                var materials = renderer.sharedMaterials;
                var authored = sourceRenderer.sharedMaterials;
                if (materials.Length != authored.Length) throw new InvalidOperationException(renderer.name + " 재질 슬롯 불일치");
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    string[] colors = original.HasProperty("_Foliage_Color_Bottom")
                        ? new[] { "_Foliage_Color_Bottom", "_Foliage_Color_Top", "_Always_On_Fresnel_Color" } : Array.Empty<string>();
                    string[] values = original.HasProperty("_Hue")
                        ? new[] { "_Hue", "_Saturation", "_Value", "_Contrast" } : Array.Empty<string>();
                    if (colors.Length == 0 && values.Length == 0) continue;
                    if (!replacements.TryGetValue(original, out var variant))
                    {
                        string path = Data + "/" + suffix + "_" + replacements.Count + ".mat";
                        variant = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (variant == null)
                        {
                            variant = new Material(original) { enableInstancing = true };
                            foreach (string property in colors)
                                if (authored[i].HasProperty(property)) variant.SetColor(property, authored[i].GetColor(property));
                            foreach (string property in values)
                                if (authored[i].HasProperty(property)) variant.SetFloat(property, authored[i].GetFloat(property));
                            if (original.HasProperty("_Hue"))
                                foreach (string property in new[] { "_MainTex", "_NormalMap" })
                                    if (authored[i].HasProperty(property)) variant.SetTexture(property, authored[i].GetTexture(property));
                            SaveAsset(variant, path, created);
                        }
                        replacements.Add(original, variant);
                    }
                    materials[i] = variant;
                }
                display.Add(renderer.name, materials);
            }

            var actor = SavePrefab(Variants + "/PF_Reactive_" + suffix + ".prefab", baseActor, preview, created, root =>
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i] != null && replacements.TryGetValue(materials[i], out var replacement)) materials[i] = replacement;
                    renderer.sharedMaterials = materials;
                }
            });
            var standing = SavePrefab(Variants + "/PF_Terrain_" + suffix + ".prefab", source, preview, created, root =>
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (display.TryGetValue(renderer.name, out var materials)) renderer.sharedMaterials = materials;
                }
            });
            var stumpSource = baseActor.transform.Find("Remnant").Cast<Transform>().Single(child => child.name.EndsWith("_Stump", StringComparison.Ordinal));
            var stump = SavePrefab(Variants + "/PF_Terrain_Stump_" + name + ".prefab", stumpSource.gameObject, preview, created, root =>
            {
                root.SetActive(true);
                root.transform.localScale = stumpSource.localScale;
                root.transform.localPosition = Vector3.zero;
            });
            var trunk = baseActor.transform.Find("Intact/TrunkHit").GetComponent<CapsuleCollider>();
            return new TerrainTreePromotionTileSO.Species { source = source, standing = standing, stump = stump,
                actor = actor.GetComponent<ReactiveEnvironmentObject>(),
                trunkCenter = baseActor.transform.InverseTransformPoint(trunk.transform.TransformPoint(trunk.center)),
                trunkRadius = trunk.radius, trunkHeight = trunk.height };
        }

        private static TerrainTreePromotionTileSO LoadOrCreateTable(string path, TerrainData source, TreePrototype[] prototypes,
            TerrainTreePromotionTileSO.Species[] species, List<string> created)
        {
            var table = AssetDatabase.LoadAssetAtPath<TerrainTreePromotionTileSO>(path);
            bool isNew = table == null;
            if (isNew) table = ScriptableObject.CreateInstance<TerrainTreePromotionTileSO>();
            else Undo.RecordObject(table, "지형 나무 배치 ID 갱신");
            var stable = new Dictionary<(GameObject, Vector3), string>();
            if (table.placements != null)
                foreach (var previous in table.placements)
                    if (!string.IsNullOrEmpty(previous.id) && previous.sourcePrototype != null)
                        stable.Add((previous.sourcePrototype, previous.position), previous.id);
            var speciesSources = new HashSet<GameObject>(species.Select(entry => entry.source));
            var instances = source.treeInstances;
            var placements = new TerrainTreePromotionTileSO.Placement[instances.Length];
            for (int i = 0; i < placements.Length; i++)
            {
                var instance = instances[i];
                var prototype = prototypes[instance.prototypeIndex].prefab;
                string id = null;
                if (speciesSources.Contains(prototype) && !stable.TryGetValue((prototype, instance.position), out id))
                    id = Guid.NewGuid().ToString("N");
                placements[i] = new TerrainTreePromotionTileSO.Placement { id = id, position = instance.position,
                    prototype = instance.prototypeIndex, sourcePrototype = id != null ? prototype : null };
            }
            table.source = source;
            table.species = species;
            table.placements = placements;
            if (isNew) SaveAsset(table, path, created);
            else EditorUtility.SetDirty(table);
            return table;
        }

        private static EnvironmentController AddWeather(Scene scene, GameObject[] roots, GameObject node, string mapId, List<string> created)
        {
            const string profile = Data + "/RiverBasin4km_Weather.json";
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(profile) == null)
            {
                if (!AssetDatabase.CopyAsset("Assets/10.Datas/World/LivingWorld/Environment/RiverBasin_Weather_v1.json", profile))
                    throw new InvalidOperationException("RiverBasin 날씨 시간표를 복사하지 못했습니다.");
                created.Add(profile);
            }
            var weather = Undo.AddComponent<EnvironmentController>(node);
            var effectsPrefab = Load<GameObject>("Assets/03.Prefabs/LivingWorld/Environment/EnvironmentEffects.prefab");
            var effectsNode = (GameObject)PrefabUtility.InstantiatePrefab(effectsPrefab, scene);
            Undo.RegisterCreatedObjectUndo(effectsNode, "4km 날씨 연출 연결");
            Undo.SetTransformParent(effectsNode.transform, node.transform, "4km 날씨 연출 연결");
            var effects = effectsNode.GetComponent<EnvironmentEffects>();
            var effectData = new SerializedObject(effects);
            effectData.FindProperty("_roofLayers").intValue = LayerMask.GetMask("Default", "Ground");
            effectData.ApplyModifiedProperties();
            var weatherData = new SerializedObject(weather);
            weatherData.FindProperty("_mapId").stringValue = mapId;
            weatherData.FindProperty("_profile").objectReferenceValue = Load<TextAsset>(profile);
            weatherData.FindProperty("_effects").objectReferenceValue = effects;
            weatherData.FindProperty("_rainAnchor").objectReferenceValue = ((ParticleSystem)effectData.FindProperty("_rain").objectReferenceValue).transform;
            weatherData.ApplyModifiedProperties();
            var binding = new SerializedObject(Undo.AddComponent<EnvironmentSceneBinding>(node));
            binding.FindProperty("_environment").objectReferenceValue = weather;
            var sky = roots.SelectMany(root => root.GetComponentsInChildren<SceneEnvironmentRendering>(true)).SingleOrDefault();
            if (sky != null)
            {
                var skyData = new SerializedObject(sky);
                skyData.FindProperty("_environment").objectReferenceValue = weather;
                skyData.ApplyModifiedProperties();
                binding.FindProperty("_skyRendering").objectReferenceValue = sky;
            }
            binding.ApplyModifiedProperties();
            return weather;
        }

        private static void WriteReferences(TerrainTreePromotion promotion, List<Terrain> terrains,
            List<TerrainTreePromotionTileSO> assets, EnvironmentController weather)
        {
            var data = new SerializedObject(promotion);
            var terrainArray = data.FindProperty("_terrains");
            var tableArray = data.FindProperty("_tiles");
            terrainArray.arraySize = tableArray.arraySize = terrains.Count;
            for (int i = 0; i < terrains.Count; i++)
            {
                terrainArray.GetArrayElementAtIndex(i).objectReferenceValue = terrains[i];
                tableArray.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }
            data.FindProperty("_weather").objectReferenceValue = weather;
            data.ApplyModifiedProperties();
        }

        private static GameObject SavePrefab(string path, GameObject source, Scene preview, List<string> created, Action<GameObject> configure)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var instance = UnityEngine.Object.Instantiate(source);
            SceneManager.MoveGameObjectToScene(instance, preview);
            try
            {
                instance.name = System.IO.Path.GetFileNameWithoutExtension(path);
                configure(instance);
                var saved = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
                if (!success || saved == null) throw new InvalidOperationException(path + " 저장 실패");
                created.Add(path);
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException(path + " 누락");

        private static void SaveAsset(UnityEngine.Object asset, string path, List<string> created)
        {
            AssetDatabase.CreateAsset(asset, path);
            created.Add(path);
        }

        private static void EnsureFolder(string path, List<string> created)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1))))
                throw new InvalidOperationException(path + " 폴더 생성 실패");
            created.Add(path);
        }
    }
}
