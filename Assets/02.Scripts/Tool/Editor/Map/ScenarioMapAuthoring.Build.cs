using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class Layout
        {
            public string scenePath;
            public string assetDirectory;
            public string mapId;
            public string sourceMapId;
            public string mapDatabasePath;
            public string startingCharacter;
            public Vector3 terrainOrigin;
            public Vector3 terrainSize;
            public int heightResolution;
            public int textureResolution;
            public int seed;
            public string[] removeRoots;
            public string[] terrainLayers;
            public Route[] routes;
            public Clearing[] clearings;
            public Scatter[] scatters;
            public Placement[] placements;
            public View[] views;
            public Lake lake;
            public Lighting lighting;
            public float routeBlend;
            public float forestRouteMargin;
            public float navigationVoxel;
            public TerrainShape shape;
            public MaterialVariant[] materials;
            public SafetySettings safety;
        }

        [Serializable] private sealed class MaterialVariant
        {
            public string source;
            public string name;
            public ColorProperty[] colors;
        }

        [Serializable] private sealed class SafetySettings
        {
            public float boundaryHeight;
            public float boundaryDepth;
            public float lakeRadiusScale;
            public string shoreFencePrefab;
            public Vector2 shoreFenceAngles;
            public Vector3 shoreFenceScale;
            public float shoreFenceSpacing;
            public float shoreFenceRadiusScale;
            public Color mapWaterColor;
        }

        [Serializable] private sealed class ColorProperty
        {
            public string property;
            public Color value;
        }

        [Serializable] private sealed class Route
        {
            public string name;
            public float width;
            public Vector3[] points;
        }

        [Serializable] private sealed class Clearing
        {
            public string name;
            public Vector3 center;
            public float radius;
            public float blend;
        }

        [Serializable] private sealed class Scatter
        {
            public string name;
            public string[] prefabs;
            public Vector3 center;
            public Vector2 extent;
            public int count;
            public float minimumScale;
            public float maximumScale;
            public float spacing;
            public float routeMargin;
            public float maximumSlope;
            public float maximumRouteDistance;
            public float protectedRadiusScale = 1f;
            public bool hasCollision;
        }

        [Serializable] private sealed class Placement
        {
            public string name;
            public string prefab;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
            public bool snapToTerrain;
            public float groundOffset;
            public bool hasCollision;
        }

        [Serializable] private sealed class View
        {
            public string name;
            public Vector3 position;
            public Vector3 target;
            public float fieldOfView;
        }

        [Serializable] private sealed class Lake
        {
            public Vector3 center;
            public Vector2 radius;
            public float depth;
            public string material;
        }

        [Serializable] private sealed class Lighting
        {
            public Vector3 sunRotation;
            public Color sunColor;
            public float sunIntensity;
            public Color skyColor;
            public Color groundColor;
            public Color fogColor;
            public float fogStart;
            public float fogEnd;
        }

        [Serializable] private sealed class TerrainShape
        {
            public float baseHeight;
            public float referenceZ;
            public float northRise;
            public float noiseFrequency;
            public float noiseBias;
            public float noiseAmplitude;
            public float edgeRise;
            public float edgeInner;
            public float edgeOuter;
            public float floorRadius;
            public float floorBlend;
            public float pixelError;
            public float baseMapDistance;
            public float lakeInner;
            public float lakeOuter;
            public float routeTerrainInner;
            public float routeTerrainOuter;
            public float routeHeightSmoothing;
        }

        private readonly struct FloorPoint
        {
            public readonly Vector3 Position;
            public FloorPoint(Vector3 position) { Position = position; }
        }

        /// <summary>개별 환경 부품으로 새 씬을 만들고, 이미 만든 씬은 수작업을 보존하며 연다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/생명의 호수 시나리오 맵 열기")]
        public static void BuildOrOpen()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            if (File.Exists(layout.scenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(layout.scenePath);
                Validate();
                return;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 새 맵을 만드세요.");
            ValidateAssetPaths(layout);
            Directory.CreateDirectory(Path.GetDirectoryName(layout.scenePath));
            Directory.CreateDirectory(layout.assetDirectory);
            AssetDatabase.Refresh();
            // 씬의 실행선과 오브젝트 간 참조를 보존하되 배경·지형·NavMesh는 새로 저작한다.
            Scene scene = EditorSceneManager.OpenScene(SourceScenePath);
            var identities = CaptureIdentities(scene);
            List<FloorPoint> floors = CollectGameplayFloors(scene, layout);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                if (Array.Exists(layout.removeRoots, name => name == root.name)
                    || root.name.StartsWith("---", StringComparison.Ordinal)
                    || prefabPath.StartsWith("Assets/ExternalAssets/", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(root);
            }
            RemoveUnusedEnemies(scene);
            var environment = new GameObject("AuthoredEnvironment");
            Terrain terrain = CreateTerrain(layout, floors, environment.transform);
            CreateLake(layout, environment.transform);
            foreach (Placement placement in layout.placements)
                PlaceEnvironment(placement, terrain, environment.transform);
            CreateScatters(layout, terrain, environment.transform);
            ApplyMaterialVariants(layout, environment);
            CreateRouteFurniture(layout, terrain, environment.transform);
            ConfigureLighting(scene, layout);
            Lightmapping.lightingDataAsset = null;
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            RestoreIdentities(scene, identities);
            BakeNavigation(layout, environment);
            if (!EditorSceneManager.SaveScene(scene, layout.scenePath))
                throw new IOException("새 시나리오 맵을 저장하지 못했습니다.");
            // 새 씬 GUID에 맞게 소유 키만 갱신하고 저장 식별자는 그대로 유지한다.
            RestoreIdentities(scene, identities);
            EditorSceneManager.SaveScene(scene);
            CapturePreviews();
            Validate();
            Debug.Log("[ScenarioMap] 개별 환경 에셋으로 생성 완료: " + layout.scenePath);
        }

        private static Layout ReadLayout()
        {
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            if (layout == null || layout.routes == null || layout.routes.Length == 0
                || layout.clearings == null || layout.terrainLayers == null || layout.terrainLayers.Length < 3
                || string.Equals(layout.scenePath, SourceScenePath, StringComparison.OrdinalIgnoreCase)
                || !layout.scenePath.StartsWith("Assets/01.Scenes/Scenario/", StringComparison.Ordinal)
                || !layout.assetDirectory.StartsWith("Assets/10.Datas/World/ScenarioMaps/", StringComparison.Ordinal))
                throw new InvalidOperationException("시나리오 맵 레이아웃과 출력 경로를 확인하세요.");
            return layout;
        }

        private static void ValidateAssetPaths(Layout layout)
        {
            foreach (Placement placement in layout.placements) RequireEnvironmentPrefab(placement.prefab);
            foreach (Scatter scatter in layout.scatters)
                foreach (string path in scatter.prefabs) RequireEnvironmentPrefab(path);
            foreach (string path in layout.terrainLayers)
                if (AssetDatabase.LoadAssetAtPath<TerrainLayer>(path) == null)
                    throw new InvalidOperationException("TerrainLayer 누락: " + path);
            if (AssetDatabase.LoadAssetAtPath<Material>(layout.lake.material) == null)
                throw new InvalidOperationException("수면 머티리얼 누락: " + layout.lake.material);
        }

        private static GameObject RequireEnvironmentPrefab(string path)
        {
            if (!path.StartsWith("Assets/ExternalAssets/Environment/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.Ordinal))
                throw new InvalidOperationException("환경 개별 프리팹 경로가 아닙니다: " + path);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("환경 부품 누락: " + path);
            return prefab;
        }

        private static Dictionary<string, string> CaptureIdentities(Scene scene)
        {
            var identities = new Dictionary<string, string>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                    identities[GetTransformPath(identity.transform)] = identity.Guid;
            return identities;
        }

        private static void RestoreIdentities(Scene scene, Dictionary<string, string> identities)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                    if (identities.TryGetValue(GetTransformPath(identity.transform), out string guid))
                    {
                        identity.EditorSetGuid(guid);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
                    }
        }

        private static void RemoveUnusedEnemies(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "Enemy") continue;
                var unused = new List<GameObject>();
                foreach (Transform group in root.transform)
                    if (!group.gameObject.activeSelf) unused.Add(group.gameObject);
                foreach (GameObject group in unused) UnityEngine.Object.DestroyImmediate(group);
            }
        }

        private static List<FloorPoint> CollectGameplayFloors(Scene scene, Layout layout)
        {
            var floors = new List<FloorPoint>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (GameActor actor in root.GetComponentsInChildren<GameActor>(true))
                    if (actor.gameObject.activeInHierarchy || root.name.StartsWith("[STORY]", StringComparison.Ordinal))
                        floors.Add(new FloorPoint(actor.transform.position - Vector3.up * 0.05f));
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    if (behaviour is UPlayGround.FlowGraph.FlowGraphTriggerVolume)
                    {
                        Vector3 point = behaviour.transform.position;
                        foreach (Terrain source in Terrain.activeTerrains)
                            if (source.gameObject.scene == scene && IsInsideTerrain(point, layout))
                            {
                                point.y = source.SampleHeight(point) + source.transform.position.y;
                                floors.Add(new FloorPoint(point));
                                break;
                            }
                    }
                    if (behaviour.GetType().Name != "FlowGraphRunner") continue;
                    SerializedProperty graph = new SerializedObject(behaviour).FindProperty("_graph");
                    if (graph?.objectReferenceValue == null) continue;
                    var serialized = new SerializedObject(graph.objectReferenceValue);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.Vector3 && property.name == "position"
                            && IsInsideTerrain(property.vector3Value, layout))
                            floors.Add(new FloorPoint(property.vector3Value - Vector3.up * 0.05f));
                }
            }
            return floors;
        }

        private static bool IsInsideTerrain(Vector3 point, Layout layout)
        {
            Vector3 local = point - layout.terrainOrigin;
            return local.x >= 0 && local.z >= 0 && local.x <= layout.terrainSize.x && local.z <= layout.terrainSize.z;
        }

        private static Terrain CreateTerrain(Layout layout, List<FloorPoint> floors, Transform parent)
        {
            var data = new TerrainData
            {
                name = "LakeOfLifeScenario_Terrain", heightmapResolution = layout.heightResolution,
                alphamapResolution = layout.textureResolution, size = layout.terrainSize,
                baseMapResolution = layout.textureResolution
            };
            var layers = new TerrainLayer[layout.terrainLayers.Length];
            for (int i = 0; i < layers.Length; i++) layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layout.terrainLayers[i]);
            data.terrainLayers = layers;
            int resolution = data.heightmapResolution;
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                Vector3 point = layout.terrainOrigin + new Vector3(x * layout.terrainSize.x / (resolution - 1), 0, z * layout.terrainSize.z / (resolution - 1));
                heights[z, x] = Mathf.Clamp01((CalculateHeight(point, layout, floors) - layout.terrainOrigin.y) / layout.terrainSize.y);
            }
            data.SetHeights(0, 0, heights);
            int textureSize = data.alphamapResolution;
            var weights = new float[textureSize, textureSize, layers.Length];
            for (int z = 0; z < textureSize; z++)
            for (int x = 0; x < textureSize; x++)
            {
                Vector3 point = layout.terrainOrigin + new Vector3(x * layout.terrainSize.x / (textureSize - 1), 0, z * layout.terrainSize.z / (textureSize - 1));
                float distance = SampleRoute(point, layout, out _, out float width, out _);
                float dirt = 1f - SmoothRange(width * 0.55f, width + 1.5f, distance);
                float rock = Mathf.InverseLerp(28f, 55f, data.GetSteepness((float)x / (textureSize - 1), (float)z / (textureSize - 1)));
                weights[z, x, 0] = (1f - dirt) * (1f - rock);
                weights[z, x, 1] = dirt * (1f - rock);
                weights[z, x, 2] = rock;
            }
            AssetDatabase.CreateAsset(data, layout.assetDirectory + "/Terrain.asset");
            data.SetAlphamaps(0, 0, weights);
            EditorUtility.SetDirty(data);
            foreach (Texture2D texture in data.alphamapTextures) EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "SculptedTerrain";
            terrainObject.transform.SetParent(parent);
            terrainObject.transform.position = layout.terrainOrigin;
            terrainObject.layer = LayerMask.NameToLayer("Ground");
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.materialTemplate = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) { name = "ScenarioTerrain", enableInstancing = true };
            AssetDatabase.CreateAsset(terrain.materialTemplate, layout.assetDirectory + "/Terrain.mat");
            terrain.heightmapPixelError = layout.shape.pixelError;
            terrain.drawInstanced = true;
            terrain.basemapDistance = layout.shape.baseMapDistance;
            terrain.Flush();
            return terrain;
        }

        private static float CalculateHeight(Vector3 point, Layout layout, List<FloorPoint> floors)
        {
            float distance = SampleRoute(point, layout, out float routeHeight, out float width, out float terrainHeight);
            float noise = Mathf.PerlinNoise(point.x * layout.shape.noiseFrequency, point.z * layout.shape.noiseFrequency);
            float height = layout.shape.baseHeight + (point.z - layout.shape.referenceZ) * layout.shape.northRise
                + (noise - layout.shape.noiseBias) * layout.shape.noiseAmplitude;
            height = Mathf.Lerp(terrainHeight + (noise - layout.shape.noiseBias) * layout.shape.noiseAmplitude,
                height, SmoothRange(layout.shape.routeTerrainInner, layout.shape.routeTerrainOuter, distance));
            float lakeDistance = LakeDistance(point, layout.lake);
            height = Mathf.Lerp(height, layout.lake.center.y - layout.lake.depth,
                1f - SmoothRange(layout.shape.lakeInner, layout.shape.lakeOuter, lakeDistance));
            float edge = Mathf.Min(point.x - layout.terrainOrigin.x,
                layout.terrainOrigin.x + layout.terrainSize.x - point.x,
                point.z - layout.terrainOrigin.z,
                layout.terrainOrigin.z + layout.terrainSize.z - point.z);
            height += (1f - SmoothRange(layout.shape.edgeInner, layout.shape.edgeOuter, edge)) * layout.shape.edgeRise;
            foreach (Clearing clearing in layout.clearings)
            {
                float radius = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(clearing.center.x, clearing.center.z));
                height = Mathf.Lerp(clearing.center.y, height, SmoothRange(clearing.radius, clearing.radius + clearing.blend, radius));
            }
            // 광장의 넓은 평탄화보다 통행로의 경사와 기존 진행 지점 높이를 우선한다.
            height = Mathf.Lerp(routeHeight, height, SmoothRange(width, width + layout.routeBlend, distance));
            foreach (FloorPoint floor in floors)
            {
                float radius = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(floor.Position.x, floor.Position.z));
                if (radius < layout.shape.floorBlend)
                    height = Mathf.Lerp(floor.Position.y, height, SmoothRange(layout.shape.floorRadius, layout.shape.floorBlend, radius));
            }
            return height;
        }

        private static float SampleRoute(Vector3 point, Layout layout, out float height, out float width, out float terrainHeight)
        {
            float distance = float.MaxValue;
            height = 0f;
            width = 0f;
            float weightedHeight = 0f;
            float totalWeight = 0f;
            Vector2 position = new(point.x, point.z);
            foreach (Route route in layout.routes)
            for (int i = 1; i < route.points.Length; i++)
            {
                Vector3 first = route.points[i - 1];
                Vector3 second = route.points[i];
                Vector2 start = new(first.x, first.z);
                Vector2 delta = new(second.x - first.x, second.z - first.z);
                float fraction = Mathf.Clamp01(Vector2.Dot(position - start, delta) / Mathf.Max(delta.sqrMagnitude, 0.001f));
                float current = Vector2.Distance(position, start + delta * fraction);
                float currentHeight = Mathf.Lerp(first.y, second.y, fraction);
                float weight = 1f / (current * current + layout.shape.routeHeightSmoothing * layout.shape.routeHeightSmoothing);
                weight *= weight;
                weightedHeight += currentHeight * weight;
                totalWeight += weight;
                if (current >= distance) continue;
                distance = current;
                height = currentHeight;
                width = route.width;
            }
            terrainHeight = weightedHeight / Mathf.Max(totalWeight, float.Epsilon);
            return distance;
        }

        private static float SmoothRange(float minimum, float maximum, float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(minimum, maximum, value));

        private static float LakeDistance(Vector3 point, Lake lake) =>
            new Vector2((point.x - lake.center.x) / lake.radius.x, (point.z - lake.center.z) / lake.radius.y).magnitude;

        private static void CreateLake(Layout layout, Transform parent)
        {
            const int segments = 96;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * layout.lake.radius.x, 0, Mathf.Sin(angle) * layout.lake.radius.y);
                uv[i + 1] = new Vector2(vertices[i + 1].x, vertices[i + 1].z) * 0.02f;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % segments + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "AuthoredLakeSurface", vertices = vertices, triangles = triangles, uv = uv };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, layout.assetDirectory + "/LakeSurface.asset");
            var water = new GameObject("LakeWater", typeof(MeshFilter), typeof(MeshRenderer));
            water.transform.SetParent(parent);
            water.transform.position = layout.lake.center;
            water.layer = LayerMask.NameToLayer("Ignore Raycast");
            water.GetComponent<MeshFilter>().sharedMesh = mesh;
            water.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(layout.lake.material);
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static GameObject PlaceEnvironment(Placement placement, Terrain terrain, Transform parent)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(RequireEnvironmentPrefab(placement.prefab), parent);
            instance.name = placement.name;
            Vector3 position = placement.position;
            if (placement.snapToTerrain) position.y = terrain.SampleHeight(position) + terrain.transform.position.y + placement.groundOffset;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(placement.rotation));
            instance.transform.localScale = placement.scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = placement.hasCollision;
                collider.gameObject.layer = LayerMask.NameToLayer("Ground");
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider.gameObject);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance;
        }

        private static void CreateScatters(Layout layout, Terrain terrain, Transform parent)
        {
            var random = new System.Random(layout.seed);
            foreach (Scatter scatter in layout.scatters)
            {
                Transform group = new GameObject(scatter.name).transform;
                group.SetParent(parent);
                var positions = new List<Vector3>();
                int attempts = 0;
                while (positions.Count < scatter.count && attempts++ < scatter.count * 30)
                {
                    Vector3 point = scatter.center + new Vector3(((float)random.NextDouble() * 2 - 1) * scatter.extent.x, 0,
                        ((float)random.NextDouble() * 2 - 1) * scatter.extent.y);
                    float routeDistance = SampleRoute(point, layout, out _, out float width, out _);
                    if (routeDistance < width + scatter.routeMargin || LakeDistance(point, layout.lake) < 1.2f) continue;
                    if (scatter.maximumRouteDistance > 0 && routeDistance > scatter.maximumRouteDistance) continue;
                    if (!IsInsideTerrain(point, layout) || IsProtected(point, layout, scatter.routeMargin, scatter.protectedRadiusScale)) continue;
                    Vector3 local = point - terrain.transform.position;
                    if (terrain.terrainData.GetSteepness(local.x / layout.terrainSize.x, local.z / layout.terrainSize.z) > scatter.maximumSlope) continue;
                    if (positions.Exists(other => Vector2.Distance(new Vector2(other.x, other.z), new Vector2(point.x, point.z)) < scatter.spacing)) continue;
                    float scale = Mathf.Lerp(scatter.minimumScale, scatter.maximumScale, (float)random.NextDouble());
                    PlaceEnvironment(new Placement
                    {
                        name = scatter.name + "_" + positions.Count.ToString("D3"), prefab = scatter.prefabs[random.Next(scatter.prefabs.Length)],
                        position = point, rotation = new Vector3(0, (float)random.NextDouble() * 360, 0),
                        scale = Vector3.one * scale, snapToTerrain = true, hasCollision = scatter.hasCollision
                    }, terrain, group);
                    positions.Add(point);
                }
            }
        }

        private static bool IsProtected(Vector3 point, Layout layout, float margin, float radiusScale = 1f)
        {
            foreach (Clearing clearing in layout.clearings)
                if (Vector2.Distance(new Vector2(point.x, point.z), new Vector2(clearing.center.x, clearing.center.z)) < clearing.radius * radiusScale + margin)
                    return true;
            return false;
        }

        private static void ApplyMaterialVariants(Layout layout, GameObject environment)
        {
            if (layout.materials == null) return;
            var replacements = new Dictionary<Material, Material>();
            foreach (MaterialVariant variant in layout.materials)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(variant.source);
                if (source == null) throw new InvalidOperationException("머티리얼 원본 누락: " + variant.source);
                var material = new Material(source) { name = variant.name, enableInstancing = true };
                foreach (ColorProperty color in variant.colors)
                {
                    if (!material.HasProperty(color.property)) throw new InvalidOperationException("색상 프로퍼티 누락: " + color.property);
                    material.SetColor(color.property, color.value);
                }
                AssetDatabase.CreateAsset(material, layout.assetDirectory + "/" + variant.name + ".mat");
                replacements.Add(source, material);
            }
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool hasReplacement = false;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && replacements.TryGetValue(materials[i], out Material replacement))
                    {
                        materials[i] = replacement;
                        hasReplacement = true;
                    }
                if (!hasReplacement) continue;
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static void CreateRouteFurniture(Layout layout, Terrain terrain, Transform parent)
        {
            // 배치 수치는 레이아웃의 개별 Placement가 소유하며 여기서는 계층만 정리한다.
            Transform routeRoot = new GameObject("PlayableRoutes").transform;
            routeRoot.SetParent(parent);
            foreach (Route route in layout.routes)
            {
                Transform routeObject = new GameObject(route.name).transform;
                routeObject.SetParent(routeRoot);
                for (int i = 0; i < route.points.Length; i++)
                {
                    Transform point = new GameObject("Point_" + i.ToString("D2")).transform;
                    point.SetParent(routeObject);
                    Vector3 position = route.points[i];
                    position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
                    point.position = position;
                }
            }
        }

        private static void ConfigureLighting(Scene scene, Layout layout)
        {
            Light sun = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Light light in root.GetComponentsInChildren<Light>())
                    if (light.type == LightType.Directional) sun = light;
            if (sun == null) sun = new GameObject("AfternoonSun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(layout.lighting.sunRotation);
            sun.color = layout.lighting.sunColor;
            sun.intensity = layout.lighting.sunIntensity;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = layout.lighting.skyColor;
            RenderSettings.ambientEquatorColor = Color.Lerp(layout.lighting.skyColor, layout.lighting.groundColor, 0.5f);
            RenderSettings.ambientGroundColor = layout.lighting.groundColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = layout.lighting.fogColor;
            RenderSettings.fogStartDistance = layout.lighting.fogStart;
            RenderSettings.fogEndDistance = layout.lighting.fogEnd;
            var binding = new GameObject("ScenarioLighting").AddComponent<WorldLightingSceneBinding>();
            binding.sunLight = sun;
            binding.disableWorldLighting = true;
        }

        private static void BakeNavigation(Layout layout, GameObject environment)
        {
            NavMesh.RemoveAllNavMeshData();
            NavMeshSurface surface = environment.GetComponent<NavMeshSurface>();
            if (surface == null) surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = LayerMask.GetMask("Ground");
            surface.overrideVoxelSize = true;
            surface.voxelSize = layout.navigationVoxel;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh 생성에 실패했습니다.");
            string path = layout.assetDirectory + "/Navigation.asset";
            NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, path);
            else
            {
                NavMeshData generated = surface.navMeshData;
                surface.RemoveData();
                EditorUtility.CopySerialized(generated, existing);
                surface.navMeshData = existing;
                surface.AddData();
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
