using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.EditorTools;
using UPlayGround.Group;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string QualityPath = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario.quality.json";

        [Serializable] private sealed class QualityLayout
        {
            public string[] removeGroups;
            public string[] removedPlacementNames;
            public string[] forbiddenPrefabs;
            public ActorPlacement[] moves;
            public ResourcePlacement[] resources;
            public GroupPlacement[] groups;
            public InteractionVisual[] interactionVisuals;
            public float minimumInteractionClearance;
        }

        [Serializable] private sealed class ResourcePlacement
        {
            public string name;
            public string guid;
            public Vector3 position;
            public string prefab;
            public string data;
            public float scale;
            public Vector3 interactionSize;
        }

        [Serializable] private sealed class ActorPlacement
        {
            public string guid;
            public Vector3 position;
            public float yaw;
        }

        [Serializable] private sealed class GroupPlacement
        {
            public string path;
            public int melee;
            public int ranged;
            public float breather;
            public float playerBreather;
            public float alertRadius;
        }

        [Serializable] private sealed class InteractionVisual
        {
            public string path;
            public string prefab;
            public Vector3 offset;
            public float scale;
        }

        [Serializable] private sealed class QualityReport
        {
            public int movedActors;
            public int tunedGroups;
            public int grassClumps;
            public int resources;
            public int uniqueEnvironmentPrefabs;
            public List<string> errors = new();
        }

        /// <summary>기존 실행선과 저장 ID를 유지하면서 식생·탐색·전투 배치를 개선한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 경험 개선")]
        public static void ImproveScenarioMap()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            var quality = JsonUtility.FromJson<QualityLayout>(File.ReadAllText(QualityPath));
            ValidateAssetPaths(layout);
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 배치를 적용하세요.");
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            var environment = FindQualityTransform(scene, "AuthoredEnvironment").gameObject;
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            var identities = new Dictionary<SceneEntityId, string>();
            var actors = new Dictionary<string, SceneEntityId>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                foreach (SceneEntityId identity in sceneRoot.GetComponentsInChildren<SceneEntityId>(true))
                {
                    identities.Add(identity, identity.Guid);
                    actors.Add(identity.Guid, identity);
                }
            foreach (ActorPlacement move in quality.moves)
                if (!actors.ContainsKey(move.guid)) throw new InvalidOperationException("배치 대상 ID 누락: " + move.guid);
            foreach (GroupPlacement group in quality.groups)
                if (FindQualityTransform(scene, group.path).GetComponent<MonsterGroupController>() == null)
                    throw new InvalidOperationException("몬스터 그룹 누락: " + group.path);
            foreach (InteractionVisual visual in quality.interactionVisuals)
            {
                FindQualityTransform(scene, visual.path);
                RequireEnvironmentPrefab(visual.prefab);
            }
            string backup = BackupQualityAssets(layout);
            try
            {
            RemoveQualityVegetation(environment.transform, layout, quality);
            RemoveForbiddenVisuals(scene, environment.transform, quality);
            CreateScatters(layout, terrain, environment.transform);
            foreach (Placement placement in layout.placements)
                if (placement.name.StartsWith("Quality_", StringComparison.Ordinal))
                {
                    Transform previous = environment.transform.Find(placement.name);
                    if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                    PlaceEnvironment(placement, terrain, environment.transform);
                }
            ApplyExistingQualityMaterials(layout, environment);
            PlaceQualityResources(scene, quality, terrain);
            MoveQualityActors(quality, terrain, actors);
            ConfigureQualityGroups(scene, quality);
            PlaceInteractionVisuals(scene, terrain, quality);
            Transform previousRoutes = environment.transform.Find("PlayableRoutes");
            if (previousRoutes != null) UnityEngine.Object.DestroyImmediate(previousRoutes.gameObject);
            CreateRouteFurniture(layout, terrain, environment.transform);
            PaintQualityRoutes(layout, terrain);
            Physics.SyncTransforms();
            BakeNavigation(layout, environment);
            foreach (var pair in identities)
            {
                pair.Key.EditorSetGuid(pair.Value);
                PrefabUtility.RecordPrefabInstancePropertyModifications(pair.Key);
            }
            Validate();
            ValidateQuality(scene, layout, quality, actors, terrain);
            ValidateBoundaries(layout, terrain, environment.transform.Find("PlayBoundaries"));
            EditorSceneManager.MarkSceneDirty(scene);
            SaveQualityScene(scene, layout);
            AssetDatabase.SaveAssets();
            CaptureMapBackground(layout, true);
            CapturePreviewImages(false);
            Debug.Log("[ScenarioMap] 식생·상호작용·조우 배치 개선 저장 완료");
            }
            catch
            {
                RestoreQualityBackup(layout, backup);
                throw;
            }
        }

        private static Transform FindQualityTransform(Scene scene, string path)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                if (sceneRoot.name == path) return sceneRoot.transform;
                if (!path.StartsWith(sceneRoot.name + "/", StringComparison.Ordinal)) continue;
                Transform result = sceneRoot.transform.Find(path.Substring(sceneRoot.name.Length + 1));
                if (result != null) return result;
            }
            throw new InvalidOperationException("씬 대상 누락: " + path);
        }

        private static string BackupQualityAssets(Layout layout)
        {
            string directory = ReportDirectory + "/QualityBackups/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(directory);
            File.Copy(layout.scenePath, directory + "/LakeOfLifeScenario.unity");
            foreach (string name in new[] { "Terrain.asset", "Navigation.asset" })
                File.Copy(layout.assetDirectory + "/" + name, directory + "/" + name);
            if (File.Exists(layout.assetDirectory + "/LakeBoundary.asset"))
                File.Copy(layout.assetDirectory + "/LakeBoundary.asset", directory + "/LakeBoundary.asset");
            return directory;
        }

        private static void SaveQualityScene(Scene scene, Layout layout)
        {
            // 임시 파일 rename이 실패하는 환경에서도 대상 GUID를 보존한 채 저장한다.
            string staging = Path.GetDirectoryName(layout.scenePath) + "/QualityStaging.unity";
            if (File.Exists(staging)) throw new IOException("이전 임시 씬을 확인하세요: " + staging);
            try
            {
                if (!EditorSceneManager.SaveScene(scene, staging, true)) throw new IOException("임시 씬 저장 실패");
                File.Copy(staging, layout.scenePath, true);
                AssetDatabase.ImportAsset(layout.scenePath, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                if (File.Exists(staging)) AssetDatabase.DeleteAsset(staging);
            }
        }

        private static void RestoreQualityBackup(Layout layout, string backup)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            File.Copy(backup + "/LakeOfLifeScenario.unity", layout.scenePath, true);
            foreach (string name in new[] { "Terrain.asset", "Navigation.asset" })
            {
                string path = layout.assetDirectory + "/" + name;
                File.Copy(backup + "/" + name, path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.ImportAsset(layout.scenePath, ImportAssetOptions.ForceUpdate);
            if (File.Exists(backup + "/LakeBoundary.asset"))
            {
                string boundaryPath = layout.assetDirectory + "/LakeBoundary.asset";
                File.Copy(backup + "/LakeBoundary.asset", boundaryPath, true);
                AssetDatabase.ImportAsset(boundaryPath, ImportAssetOptions.ForceUpdate);
            }
            EditorSceneManager.OpenScene(layout.scenePath);
        }

        private static void PlaceQualityResources(Scene scene, QualityLayout quality, Terrain terrain)
        {
            Transform parent = null;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                if (sceneRoot.name == "QualityInteractions") parent = sceneRoot.transform;
            if (parent == null) parent = new GameObject("QualityInteractions").transform;
            foreach (ResourcePlacement resource in quality.resources)
            {
                Transform existing = parent.Find(resource.name);
                if (existing != null)
                {
                    var current = new SerializedObject(existing.GetComponent<GatheringActor>());
                    current.FindProperty("_interactableData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InteractableActorSO>(resource.data);
                    current.ApplyModifiedPropertiesWithoutUndo();
                    existing.position = GroundPoint(resource.position, terrain);
                    continue;
                }
                var data = AssetDatabase.LoadAssetAtPath<InteractableActorSO>(resource.data);
                if (data == null) throw new InvalidOperationException("채집 데이터 누락: " + resource.data);
                var node = new GameObject(resource.name);
                node.transform.SetParent(parent);
                node.transform.position = GroundPoint(resource.position, terrain);
                node.layer = LayerMask.NameToLayer("InteractableObject");
                var collider = node.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = resource.interactionSize;
                collider.center = Vector3.up * resource.interactionSize.y * 0.5f;
                var actor = node.AddComponent<GatheringActor>();
                var serialized = new SerializedObject(actor);
                serialized.FindProperty("_interactableData").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var identity = node.AddComponent<SceneEntityId>();
                identity.EditorSetGuid(resource.guid);
                PlaceEnvironment(new Placement { name = "ResourceVisual", prefab = resource.prefab,
                    position = node.transform.position, rotation = Vector3.zero,
                    scale = Vector3.one * resource.scale, snapToTerrain = true, hasCollision = false }, terrain, node.transform);
            }
        }

        private static void RemoveQualityVegetation(Transform environment, Layout layout, QualityLayout quality)
        {
            var names = new HashSet<string>(quality.removeGroups);
            foreach (Scatter scatter in layout.scatters) names.Add(scatter.name);
            foreach (string name in quality.removedPlacementNames) names.Add(name);
            var removed = new List<GameObject>();
            foreach (Transform child in environment)
                if (names.Contains(child.name)) removed.Add(child.gameObject);
            foreach (GameObject child in removed) UnityEngine.Object.DestroyImmediate(child);
        }

        private static void RemoveForbiddenVisuals(Scene scene, Transform environment, QualityLayout quality)
        {
            var forbidden = new HashSet<string>(quality.forbiddenPrefabs);
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null || !PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) continue;
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                    if (!forbidden.Contains(path)) continue;
                    if (child.IsChildOf(environment))
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                        continue;
                    }
                    // 퀘스트가 참조하는 오브젝트와 상태 전환은 보존하고 금지된 외형만 숨긴다.
                    foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                    foreach (Collider collider in child.GetComponentsInChildren<Collider>(true))
                    {
                        if (collider.isTrigger) continue;
                        collider.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                    }
                }
        }

        private static void ApplyExistingQualityMaterials(Layout layout, GameObject environment)
        {
            var replacements = new Dictionary<Material, Material>();
            foreach (MaterialVariant variant in layout.materials)
                replacements.Add(AssetDatabase.LoadAssetAtPath<Material>(variant.source),
                    AssetDatabase.LoadAssetAtPath<Material>(layout.assetDirectory + "/" + variant.name + ".mat"));
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool hasChange = false;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && replacements.TryGetValue(materials[i], out Material replacement) && replacement != null)
                    { materials[i] = replacement; hasChange = true; }
                if (!hasChange) continue;
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static void MoveQualityActors(QualityLayout quality, Terrain terrain, Dictionary<string, SceneEntityId> actors)
        {
            foreach (ActorPlacement move in quality.moves)
            {
                Transform actor = actors[move.guid].transform;
                actor.SetPositionAndRotation(GroundPoint(move.position, terrain), Quaternion.Euler(0, move.yaw, 0));
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            }
        }

        private static void ConfigureQualityGroups(Scene scene, QualityLayout quality)
        {
            foreach (GroupPlacement group in quality.groups)
            {
                var serialized = new SerializedObject(FindQualityTransform(scene, group.path).GetComponent<MonsterGroupController>());
                serialized.FindProperty("_meleeSlotCap").intValue = group.melee;
                serialized.FindProperty("_rangedSlotCap").intValue = group.ranged;
                serialized.FindProperty("_breatherDuration").floatValue = group.breather;
                serialized.FindProperty("_playerBreatherDuration").floatValue = group.playerBreather;
                serialized.FindProperty("_alertRadius").floatValue = group.alertRadius;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(serialized.targetObject);
            }
        }

        private static void PlaceInteractionVisuals(Scene scene, Terrain terrain, QualityLayout quality)
        {
            foreach (InteractionVisual visual in quality.interactionVisuals)
            {
                Transform parent = FindQualityTransform(scene, visual.path);
                Transform previous = parent.Find("Quality_InteractionVisual");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                PlaceEnvironment(new Placement { name = "Quality_InteractionVisual", prefab = visual.prefab,
                    position = parent.position + visual.offset, rotation = Vector3.zero,
                    scale = Vector3.one * visual.scale, snapToTerrain = true, hasCollision = false }, terrain, parent);
            }
        }

        private static void PaintQualityRoutes(Layout layout, Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            int size = data.alphamapResolution;
            float[,,] weights = data.GetAlphamaps(0, 0, size, size);
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                Vector3 point = terrain.transform.position + new Vector3(x * data.size.x / (size - 1), 0, z * data.size.z / (size - 1));
                float dirt = SampleTrailWeight(point, layout);
                float rock = weights[z, x, 2];
                weights[z, x, 0] = (1 - dirt) * (1 - rock);
                weights[z, x, 1] = dirt * (1 - rock);
            }
            data.SetAlphamaps(0, 0, weights);
            EditorUtility.SetDirty(data);
            foreach (Texture2D texture in data.alphamapTextures) EditorUtility.SetDirty(texture);
        }

        private static float SampleTrailWeight(Vector3 point, Layout layout)
        {
            float distance = SampleRoute(point, layout, out _, out float width, out _, RouteSampling.GroundPaint);
            float noise = Mathf.PerlinNoise(point.x * layout.pathEdgeFrequency, point.z * layout.pathEdgeFrequency);
            width *= 1 + (noise - 0.5f) * layout.pathEdgeVariation;
            return (1 - SmoothRange(width * 0.55f, width + 1.5f, distance)) * layout.pathDirtStrength;
        }

        private static void ValidateQuality(Scene scene, Layout layout, QualityLayout quality,
            Dictionary<string, SceneEntityId> actors, Terrain terrain)
        {
            var report = new QualityReport { movedActors = quality.moves.Length, tunedGroups = quality.groups.Length };
            var assets = new HashSet<string>();
            var forbidden = new HashSet<string>(quality.forbiddenPrefabs);
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) continue;
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                    if (path.StartsWith("Assets/ExternalAssets/Environment/", StringComparison.Ordinal)) assets.Add(path);
                    if (child.name.StartsWith("Meadow_Grass_", StringComparison.Ordinal)) report.grassClumps++;
                    if (!forbidden.Contains(path)) continue;
                    foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                        if (renderer.enabled) report.errors.Add("금지 외형 잔존: " + GetTransformPath(child));
                }
            foreach (ActorPlacement move in quality.moves)
            {
                Transform actor = actors[move.guid].transform;
                if (!NavMesh.SamplePosition(actor.position, out _, quality.minimumInteractionClearance, NavMesh.AllAreas))
                    report.errors.Add("액터 접근 NavMesh 누락: " + GetTransformPath(actor));
                if (Vector3.Distance(actor.position, GroundPoint(move.position, terrain)) > 0.2f)
                    report.errors.Add("액터 배치 오차: " + move.guid);
            }
            foreach (ResourcePlacement resource in quality.resources)
            {
                Transform node = FindQualityTransform(scene, "QualityInteractions/" + resource.name);
                var actor = node.GetComponent<GatheringActor>();
                if (node.GetComponent<SceneEntityId>().Guid != resource.guid || actor.GetData() == null)
                    report.errors.Add("채집 ID 또는 보상 데이터 누락: " + resource.name);
                if (!NavMesh.SamplePosition(node.position, out _, quality.minimumInteractionClearance, NavMesh.AllAreas))
                    report.errors.Add("채집 접근 경로 누락: " + resource.name);
                report.resources++;
            }
            report.uniqueEnvironmentPrefabs = assets.Count;
            File.WriteAllText(ReportDirectory + "/Quality.json", JsonUtility.ToJson(report, true));
            if (report.errors.Count > 0) throw new InvalidOperationException(string.Join("\n", report.errors));
        }
    }
}
