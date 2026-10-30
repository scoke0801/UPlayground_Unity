using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Manager.World;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Editor
{
    /// <summary>원본 시나리오를 보존하며 호수·마을·도시의 연속 월드 사본을 재생성한다.</summary>
    public static partial class OpenWorldSliceAuthoring
    {
        public const string HostPath = "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
        public const string RegionPath = "Assets/01.Scenes/Scenario/OpenWorld/Regions/DroughtVillageRegion.unity";
        private const string LakePath = "Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity";
        private const string VillagePath = "Assets/01.Scenes/Scenario/DroughtVillage.unity";
        private const string DataFolder = "Assets/10.Datas/World/OpenWorld";
        private const string ReportFolder = "output/OpenWorld";
        private const string RegionKey = "OpenWorld.DroughtVillage.Content";
        public static bool DiagnosticFailAfterSurface;

        /// <summary>생성본만 백업·교체하며 실패하면 직전 생성본을 복구한다.</summary>
        [UPlaygroundTool("OpenWorld/대표 구간 재생성")]
        public static void Build()
        {
            RequireCleanEditor();
            Directory.CreateDirectory(ReportFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(RegionPath));
            Directory.CreateDirectory(DataFolder);
            var recipe = LoadCityRecipe();
            string[] outputs = new[] { HostPath, RegionPath, DataFolder + "/Terrain.asset", DataFolder + "/Navigation.asset", DataFolder + "/SouthTerrain.asset" }
                .Concat(recipe.cities.Select(c => c.regionPath)).ToArray();
            var backup = new Dictionary<string, byte[]>();
            foreach (string path in outputs)
                if (File.Exists(path)) backup[path] = File.ReadAllBytes(path);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Addressables 설정이 없습니다.");
            var oldBuildScenes = EditorBuildSettings.scenes;
            string previousGuid = AssetDatabase.AssetPathToGUID(RegionPath);
            var previousEntry = settings.FindAssetEntry(previousGuid);
            var previousGroup = previousEntry?.parentGroup;
            string previousAddress = previousEntry?.address;
            string lakeHash = Hash(LakePath), villageHash = Hash(VillagePath);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Scene host = default, village = default;
            var cityScenes = new List<Scene>();
            var cityHashes = recipe.cities.ToDictionary(c => c.sourcePath, c => Hash(c.sourcePath));
            var cityEntries = recipe.cities.ToDictionary(c => c.regionPath, c => settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(c.regionPath)));
            var cityGroups = cityEntries.ToDictionary(p => p.Key, p => p.Value?.parentGroup);
            var cityAddresses = cityEntries.ToDictionary(p => p.Key, p => p.Value?.address);
            bool succeeded = false;
            try
            {
                // 열린 생성본을 디스크에서 덮어쓰지 않는다. 원래 열려 있던 씬은 finally에서 복원한다.
                EditorSceneManager.OpenScene("Assets/01.Scenes/GameLogic/Boot.unity", OpenSceneMode.Single);
                CopySourceScene(LakePath, HostPath);
                CopySourceScene(VillagePath, RegionPath);
                host = EditorSceneManager.OpenScene(HostPath, OpenSceneMode.Additive);
                village = EditorSceneManager.OpenScene(RegionPath, OpenSceneMode.Additive);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);
                WriteManifest(host, village, lakeHash, villageHash);
                var ids = Components<Components.SceneEntityId>(host).Concat(Components<Components.SceneEntityId>(village))
                    .ToDictionary(id => id, id => id.Guid);
                BuildSurface(host, village);
                if (DiagnosticFailAfterSurface) throw new InvalidOperationException("진단용 표면 생성 후 실패");
                BuildContent(host, village, "DroughtVillage");
                var context = Components<SceneContext>(host).Single();
                var world = context.gameObject.AddComponent<ContinuousWorldController>();
                world.ResidentMapId = "LakeOfLifeScenario";
                world.SafeEntryPosition = new Vector3(994, 63, 449);
                var neighbor = new ContinuousWorldRegion {
                    MapId = "DroughtVillage", LegacySceneName = "DroughtVillage", SceneKey = RegionKey,
                    Bounds = new Rect(930, 435, 54, 70), SafeEntryPosition = world.SafeEntryPosition,
                    NeighborEntryPosition = new Vector3(978, 63, 451),
                    Grounds = new Collider[] { Components<TerrainCollider>(host).Single() }
                };
                neighbor.EntryBarrier = CreateEntryBarrier(host, neighbor.Bounds);
                world.Regions = new[] {neighbor};
                var actorGroundContacts = CaptureActorGroundContacts(host);
                BuildCities(host, world, recipe, cityScenes, ids);
                File.WriteAllLines(ReportFolder + "/actor-ground-contacts.txt", RestoreActorGroundContacts(host, actorGroundContacts));
                foreach (var runner in Components<FlowGraphRunner>(host)) runner.OriginSceneName = world.ResidentMapId;
                foreach (var runner in Components<FlowGraphRunner>(village)) runner.OriginSceneName = neighbor.MapId;
                BakeNavigation(host);
                foreach (var camera in Components<Camera>(host)) camera.useOcclusionCulling = false;
                foreach (var pair in ids)
                    if (pair.Key != null) pair.Key.EditorSetGuid(pair.Value);
                EditorSceneManager.SaveScene(host);
                EditorSceneManager.SaveScene(village);
                foreach (var city in cityScenes) EditorSceneManager.SaveScene(city);
                if (Hash(LakePath) != lakeHash || Hash(VillagePath) != villageHash)
                    throw new InvalidOperationException("원본 씬 내용이 변경되었습니다.");
                foreach (var pair in cityHashes)
                    if (Hash(pair.Key) != pair.Value) throw new InvalidOperationException("도시 원본 씬 내용이 변경되었습니다.");
                foreach (var city in recipe.cities)
                    settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(city.regionPath), settings.DefaultGroup).address = city.sceneKey;
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(RegionPath), settings.DefaultGroup);
                entry.address = RegionKey;
                EditorUtility.SetDirty(settings);
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == HostPath)) scenes.Add(new EditorBuildSettingsScene(HostPath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                SaveOwnedAssets(settings);
                if (previousGroup != null) AssetDatabase.SaveAssetIfDirty(previousGroup);
                succeeded = true;
                File.WriteAllText(ReportFolder + "/authoring-result.txt",
                    "생성 완료\n원본 SHA256 보존\nHost=" + HostPath + "\nRegion=" + RegionPath
                    + "\nPlayer/Addressables 빌드 미실행\n");
            }
            finally
            {
                foreach (var city in cityScenes)
                    if (city.IsValid() && city.isLoaded) EditorSceneManager.CloseScene(city, true);
                if (village.IsValid() && village.isLoaded) EditorSceneManager.CloseScene(village, true);
                if (host.IsValid() && host.isLoaded) EditorSceneManager.CloseScene(host, true);
                if (!succeeded)
                {
                    string generatedGuid = AssetDatabase.AssetPathToGUID(RegionPath);
                    if (previousEntry == null) settings.RemoveAssetEntry(generatedGuid);
                    else settings.CreateOrMoveEntry(generatedGuid, previousGroup).address = previousAddress;
                    foreach (var city in recipe.cities)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(city.regionPath);
                        if (cityEntries[city.regionPath] == null) settings.RemoveAssetEntry(guid);
                        else settings.CreateOrMoveEntry(guid, cityGroups[city.regionPath]).address = cityAddresses[city.regionPath];
                    }
                    EditorBuildSettings.scenes = oldBuildScenes;
                    // dirty 메모리 사본을 먼저 정리해야 파일 복구 뒤 SaveAssets가 다시 덮어쓰지 않는다.
                    SaveOwnedAssets(settings);
                    if (previousGroup != null) AssetDatabase.SaveAssetIfDirty(previousGroup);
                    foreach (string path in outputs)
                    {
                        var loaded = AssetDatabase.LoadMainAssetAtPath(path);
                        if (loaded is TerrainData || loaded is UnityEngine.AI.NavMeshData)
                            Resources.UnloadAsset(loaded);
                        if (backup.TryGetValue(path, out byte[] bytes))
                        {
                            File.WriteAllBytes(path, bytes);
                            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        }
                        else if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                    }
                    AssetDatabase.Refresh();
                }
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void BuildSurface(Scene host, Scene village)
        {
            Terrain lake = Components<Terrain>(host).Single();
            Terrain source = Components<Terrain>(village).Single(t => t.GetComponent<TerrainCollider>() != null);
            var terrain = UnityEngine.Object.Instantiate(lake.terrainData);
            Vector3 origin = lake.transform.position, villageOrigin = source.transform.position;
            Vector3 size = terrain.size, villageSize = source.terrainData.size;
            int resolution = terrain.heightmapResolution;
            float[,] heights = terrain.GetHeights(0, 0, resolution, resolution);
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float wx = origin.x + x * size.x / (resolution - 1);
                float wz = origin.z + z * size.z / (resolution - 1);
                float u = (wx - villageOrigin.x) / villageSize.x;
                float v = (wz - villageOrigin.z) / villageSize.z;
                if (u < 0 || u > 1 || v < 0 || v > 1) continue;
                float edge = Mathf.Min(wx-villageOrigin.x, villageOrigin.x+villageSize.x-wx,
                    wz-villageOrigin.z, villageOrigin.z+villageSize.z-wz);
                float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / 3));
                float height = (source.terrainData.GetInterpolatedHeight(u, v) + villageOrigin.y - origin.y) / size.y;
                heights[z,x] = Mathf.Lerp(heights[z,x], height, blend);
            }
            terrain.SetHeights(0,0,heights);
            CopyVillageGround(terrain, origin, source);
            terrain.treeInstances = terrain.treeInstances.Where(tree =>
            {
                Vector3 p = Vector3.Scale(tree.position, size) + origin;
                return p.x < villageOrigin.x || p.x > villageOrigin.x + villageSize.x
                    || p.z < villageOrigin.z || p.z > villageOrigin.z + villageSize.z;
            }).ToArray();
            string path = DataFolder + "/Terrain.asset";
            TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (existing == null) { AssetDatabase.CreateAsset(terrain, path); existing = terrain; }
            else CopyGeneratedGround(terrain, existing);
            lake.terrainData = existing;
            lake.GetComponent<TerrainCollider>().terrainData = existing;
            foreach (var other in Components<Terrain>(village)) UnityEngine.Object.DestroyImmediate(other.gameObject);

            var environment = host.GetRootGameObjects().Single(r => r.name == "AuthoredEnvironment");
            var art = Components<Transform>(village).Single(t => t.name == "DroughtArt");
            if (art.GetComponentsInChildren<Components.SceneEntityId>(true).Length != 0)
                throw new InvalidOperationException("상주 배경으로 옮길 DroughtArt에 저장 개체가 포함돼 있습니다.");
            // 마을 원경은 제거된 별도 배경 Terrain 높이에 놓여 있어 호수에 옮기면 부유·중복된다.
            Transform distant = art.Find("DistantDryCountryside");
            if (distant != null) UnityEngine.Object.DestroyImmediate(distant.gameObject);
            art.SetParent(null, true);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(art.gameObject, host);
            art.SetParent(environment.transform, true);
            ClearConnectorVegetation(environment.transform);
        }

        private static void BakeNavigation(Scene host)
        {
            var environment = host.GetRootGameObjects().Single(r => r.name == "AuthoredEnvironment");
            foreach (var nav in Components<NavMeshSurface>(host).ToArray())
                UnityEngine.Object.DestroyImmediate(nav);
            var surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            var recipe = LoadCityRecipe();
            if (recipe.navigationVoxelSize > 0)
            {
                surface.overrideVoxelSize = true;
                surface.voxelSize = recipe.navigationVoxelSize;
            }
            BuildNavigationPreferences(environment.transform, recipe);
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("공동 NavMesh 생성 실패");
            string navPath = DataFolder + "/Navigation.asset";
            var existingNav = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath);
            if (existingNav == null) AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            else
            {
                var generated = surface.navMeshData;
                surface.RemoveData();
                EditorUtility.CopySerialized(generated, existingNav);
                surface.navMeshData = existingNav;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(generated);
            }
        }

        private static void CopyVillageGround(TerrainData target, Vector3 origin, Terrain source)
        {
            TerrainData village = source.terrainData;
            var layers = target.terrainLayers.ToList();
            foreach (var layer in village.terrainLayers)
                if (!layers.Contains(layer)) layers.Add(layer);
            int oldCount = target.alphamapLayers;
            float[,,] old = target.GetAlphamaps(0, 0, target.alphamapWidth, target.alphamapHeight);
            target.terrainLayers = layers.ToArray();
            var result = new float[target.alphamapHeight, target.alphamapWidth, layers.Count];
            float[,,] colors = village.GetAlphamaps(0, 0, village.alphamapWidth, village.alphamapHeight);
            int[] map = village.terrainLayers.Select(layer => layers.IndexOf(layer)).ToArray();
            for (int z = 0; z < target.alphamapHeight; z++)
            for (int x = 0; x < target.alphamapWidth; x++)
            {
                float wx = origin.x + x * target.size.x / (target.alphamapWidth - 1);
                float wz = origin.z + z * target.size.z / (target.alphamapHeight - 1);
                float u = (wx - source.transform.position.x) / village.size.x;
                float v = (wz - source.transform.position.z) / village.size.z;
                bool inside = u >= 0 && u <= 1 && v >= 0 && v <= 1;
                for (int layer = 0; layer < oldCount; layer++) result[z,x,layer] = inside ? 0 : old[z,x,layer];
                if (!inside) continue;
                int sx = Mathf.RoundToInt(u * (village.alphamapWidth - 1));
                int sz = Mathf.RoundToInt(v * (village.alphamapHeight - 1));
                for (int layer = 0; layer < map.Length; layer++) result[z,x,map[layer]] += colors[sz,sx,layer];
            }
            target.SetAlphamaps(0, 0, result);
            PaintConnector(target, origin);
            // 마을 건물 밑으로 호수의 풀 밀도가 겹치지 않게 한다.
            for (int layer = 0; layer < target.detailPrototypes.Length; layer++)
            {
                int[,] detail = target.GetDetailLayer(0,0,target.detailWidth,target.detailHeight,layer);
                for (int z = 0; z < target.detailHeight; z++)
                for (int x = 0; x < target.detailWidth; x++)
                {
                    float wx = origin.x + x * target.size.x / target.detailWidth;
                    float wz = origin.z + z * target.size.z / target.detailHeight;
                    if (wx >= source.transform.position.x && wx <= source.transform.position.x + village.size.x
                        && wz >= source.transform.position.z && wz <= source.transform.position.z + village.size.z)
                        detail[z,x] = 0;
                }
                target.SetDetailLayer(0,0,layer,detail);
            }
        }

        private static float ConnectorDistance(Vector3 point)
        {
            Vector2 start = new(998, 449), end = new(974, 451);
            Vector2 delta = end - start, p = new(point.x, point.z);
            return Vector2.Distance(p, start + delta * Mathf.Clamp01(Vector2.Dot(p - start, delta) / delta.sqrMagnitude));
        }

        private static void PaintConnector(TerrainData terrain, Vector3 origin)
        {
            var road = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_Dirt_02.terrainlayer");
            int layer = Array.IndexOf(terrain.terrainLayers, road);
            if (layer < 0) throw new InvalidOperationException("연결 길에 사용할 기존 흙 TerrainLayer가 없습니다.");
            float[,,] weights = terrain.GetAlphamaps(0, 0, terrain.alphamapWidth, terrain.alphamapHeight);
            for (int z = 0; z < terrain.alphamapHeight; z++)
            for (int x = 0; x < terrain.alphamapWidth; x++)
            {
                var p = origin + new Vector3(x * terrain.size.x / (terrain.alphamapWidth - 1), 0,
                    z * terrain.size.z / (terrain.alphamapHeight - 1));
                float blend = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.2f, 2.4f, ConnectorDistance(p)));
                if (blend <= 0) continue;
                for (int i = 0; i < terrain.alphamapLayers; i++) weights[z,x,i] *= 1 - blend;
                weights[z,x,layer] += blend;
            }
            terrain.SetAlphamaps(0, 0, weights);
        }

        private static void ClearConnectorVegetation(Transform environment)
        {
            var removed = new List<string>();
            Transform grass = environment.Find("Meadow_Grass");
            if (grass != null)
                foreach (Transform item in grass)
                    if (ConnectorDistance(item.position) < 2.4f)
                    {
                        removed.Add(AnimationUtility.CalculateTransformPath(item, null));
                        item.gameObject.SetActive(false);
                    }
            Transform understory = environment.Find("ScenicUnderstory");
            if (understory != null)
                foreach (Transform cluster in understory)
                foreach (Transform item in cluster)
                    if (ConnectorDistance(item.position) < 2.4f)
                    {
                        removed.Add(AnimationUtility.CalculateTransformPath(item, null));
                        item.gameObject.SetActive(false);
                    }
            File.WriteAllLines(ReportFolder + "/connector-vegetation.txt", removed);
        }

        private static void BuildContent(Scene host, Scene village, string mapId)
        {
            // 지역 로드 때 원본 NavMesh가 공동 지면 위에 중복 등록되면 진입 경로가 분리된다.
            foreach (var nav in Components<NavMeshSurface>(village)) UnityEngine.Object.DestroyImmediate(nav);
            foreach (var player in Components<PlayerActor>(village)) UnityEngine.Object.DestroyImmediate(player.gameObject);
            foreach (var camera in Components<Camera>(village)) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach (var light in Components<Light>(village))
                if (light.type == LightType.Directional) UnityEngine.Object.DestroyImmediate(light.gameObject);
            foreach (var context in Components<SceneContext>(village)) UnityEngine.Object.DestroyImmediate(context.gameObject);
            foreach (var environment in Components<EnvironmentController>(village)) UnityEngine.Object.DestroyImmediate(environment.gameObject);
            foreach (var binding in Components<Gameplay.Interaction.LivingWorld.NpcRoutineEnvironmentBinding>(village))
            {
                var serialized = new SerializedObject(binding);
                serialized.FindProperty("_consumerMapId").stringValue = mapId;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var volume in Components<UnityEngine.Rendering.Volume>(village))
                if (volume.isGlobal) UnityEngine.Object.DestroyImmediate(volume.gameObject);
            foreach (var portal in Components<PortalActor>(host).Concat(Components<PortalActor>(village)))
            {
                if (!WorldMapIds.Contains(portal.TargetSceneName)) continue;
                portal.enabled = false;
                foreach (var collider in portal.GetComponents<Collider>()) collider.enabled = false;
            }
            var root = new GameObject("RegionContent");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, village);
            foreach (var child in village.GetRootGameObjects())
                if (child != root) child.transform.SetParent(root.transform, true);
            root.SetActive(false);
            var metadata = new GameObject("RegionOwnership");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(metadata, village);
            var content = metadata.AddComponent<StreamedRegionContent>();
            content.MapId = mapId;
            content.ContentRoot = root;
        }

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static GameObject CreateEntryBarrier(Scene host, Rect bounds)
        {
            var root = new GameObject("RegionPreparationBarrier");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, host);
            var positions = new[] {
                new Vector3(bounds.xMin, 100, bounds.center.y), new Vector3(bounds.xMax, 100, bounds.center.y),
                new Vector3(bounds.center.x, 100, bounds.yMin), new Vector3(bounds.center.x, 100, bounds.yMax) };
            for (int i = 0; i < positions.Length; i++)
            {
                var edge = new GameObject("PreparationEdge_" + i);
                edge.transform.SetParent(root.transform, false);
                edge.transform.position = positions[i];
                var collider = edge.AddComponent<BoxCollider>();
                collider.size = i < 2 ? new Vector3(1, 400, bounds.height + 1)
                    : new Vector3(bounds.width + 1, 400, 1);
            }
            return root;
        }

        private static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }

        private static void CopySourceScene(string sourcePath, string targetPath)
        {
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);
            // OnValidate의 일반 복제 감지를 유지하되, 의도된 월드 사본은 같은 저장 개체로 남긴다.
            string text = File.ReadAllText(targetPath).Replace("GlobalObjectId_V1-2-" + sourceGuid + "-",
                "GlobalObjectId_V1-2-" + targetGuid + "-");
            File.WriteAllText(targetPath, text);
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void SaveOwnedAssets(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings)
        {
            AssetDatabase.SaveAssetIfDirty(settings);
            foreach (var group in settings.groups)
                if (group != null && group == settings.DefaultGroup) AssetDatabase.SaveAssetIfDirty(group);
            foreach (string path in new[] {DataFolder + "/Terrain.asset", DataFolder + "/Navigation.asset", DataFolder + "/SouthTerrain.asset"})
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) continue;
                EditorUtility.SetDirty(asset);
                if (asset is TerrainData ground)
                    foreach (var texture in ground.alphamapTextures) EditorUtility.SetDirty(texture);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static void WriteManifest(Scene host, Scene village, string lakeHash, string villageHash)
        {
            var lines = new List<string> { "원본→생성본; 좌표/GUID/저장 ID 유지", LakePath + " SHA256=" + lakeHash,
                VillagePath + " SHA256=" + villageHash, "복구: 원본 씬을 열거나 생성 recipe 재실행. 원본 세이브 변환 없음." };
            foreach (var scene in new[] {host, village})
            foreach (var id in Components<Components.SceneEntityId>(scene))
                lines.Add(scene.name + " | " + id.Guid + " | " + id.name + " | " + id.transform.position.ToString("F4"));
            File.WriteAllLines(ReportFolder + "/slice-manifest.txt", lines);
        }

        private static void RequireCleanEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Edit Mode와 컴파일 완료가 필요합니다.");
            for (int i=0; i<UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬에 미저장 변경이 있습니다. 먼저 사용자가 저장해야 합니다.");
        }

        /// <summary>원본 슬롯을 불러오지 않고 대표 구간의 실제 월드를 연다.</summary>
        [UPlaygroundTool("OpenWorld/호수 여울마을 시작")]
        public static void Open()
        {
            RequireCleanEditor();
            if (!File.Exists(HostPath)) throw new InvalidOperationException("대표 구간 재생성이 필요합니다.");
            Tool.Editor.Map.ScenarioMapAuthoring.PlayAuthoredScene(HostPath, "livingworld.lake.from_drought");
        }
    }
}
