using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Data.UI;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>새 맵의 지도와 지역 실행선을 연결하고 기존 시작 지역을 보존한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 실행 연결")]
        public static void ConnectScenarioMap()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            if (string.IsNullOrWhiteSpace(layout.mapId) || layout.mapId != Path.GetFileNameWithoutExtension(layout.scenePath))
                throw new InvalidOperationException("지역 ID와 씬 이름이 일치해야 합니다.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(layout.scenePath);
            Validate();
            var database = AssetDatabase.LoadAssetAtPath<MapConfigDatabaseSO>(layout.mapDatabasePath);
            if (database == null) throw new InvalidOperationException("맵 데이터베이스가 없습니다.");
            MinimapIconConfigSO sourceConfig = database.GetConfig(layout.sourceMapId);
            MapRegionInfoSO sourceRegion = database.GetRegionInfo(layout.sourceMapId);
            if (sourceConfig == null || sourceRegion == null) throw new InvalidOperationException("원본 지역 실행선이 없습니다.");
            string configPath = layout.assetDirectory + "/MinimapConfig.asset";
            string regionPath = layout.assetDirectory + "/RegionInfo.asset";
            foreach (MapConfigDatabaseSO.Entry entry in database.Entries)
                if (entry.mapId == layout.mapId && (AssetDatabase.GetAssetPath(entry.config) != configPath
                    || AssetDatabase.GetAssetPath(entry.regionInfo) != regionPath))
                    throw new InvalidOperationException("지역 ID를 다른 에셋이 사용하고 있습니다: " + layout.mapId);
            EditorBuildSettingsScene[] previousBuildScenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene scene in previousBuildScenes)
                if (Path.GetFileNameWithoutExtension(scene.path) == layout.mapId && scene.path != layout.scenePath)
                    throw new InvalidOperationException("동일한 이름의 빌드 씬이 있습니다: " + scene.path);
            SceneContext context = UnityEngine.Object.FindFirstObjectByType<SceneContext>();
            if (context == null) throw new InvalidOperationException("씬 컨텍스트가 없습니다.");

            MinimapIconConfigSO config = AssetDatabase.LoadAssetAtPath<MinimapIconConfigSO>(configPath);
            MapRegionInfoSO region = AssetDatabase.LoadAssetAtPath<MapRegionInfoSO>(regionPath);
            if (config == null)
            {
                if (File.Exists(configPath)) throw new InvalidOperationException("지도 설정 파일의 타입이 다릅니다.");
                Sprite background = CaptureMapBackground(layout);
                config = UnityEngine.Object.Instantiate(sourceConfig);
                config.name = "MinimapConfig";
                config.backgroundSprite = background;
                config.captureCenter = new Vector2(layout.terrainOrigin.x + layout.terrainSize.x / 2,
                    layout.terrainOrigin.z + layout.terrainSize.z / 2);
                config.captureWorldSize = Mathf.Max(layout.terrainSize.x, layout.terrainSize.z);
                config.captureWorldSizeXY = new Vector2(layout.terrainSize.x, layout.terrainSize.z);
                config.mapZoom = sourceConfig.mapZoom * config.captureWorldSize / sourceConfig.captureWorldSize;
                config.flipX = false;
                config.flipY = false;
                config.rotationDegrees = 0;
                AssetDatabase.CreateAsset(config, configPath);
            }
            if (region == null)
            {
                if (File.Exists(regionPath)) throw new InvalidOperationException("지역 설정 파일의 타입이 다릅니다.");
                region = UnityEngine.Object.Instantiate(sourceRegion);
                region.name = "RegionInfo";
                // 원본 포탈은 새 지형 밖의 좌표이며 새 맵에는 해당 포탈이 없다.
                region.portals.Clear();
                AssetDatabase.CreateAsset(region, regionPath);
            }
            AssetDatabase.SaveAssets();

            byte[] previousDatabase = File.ReadAllBytes(layout.mapDatabasePath);
            const string buildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";
            byte[] previousBuildSettings = File.ReadAllBytes(buildSettingsPath);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("시나리오 맵 실행 연결");
            Undo.RecordObjects(new UnityEngine.Object[] { database, context }, "시나리오 맵 실행 연결");
            try
            {
                var serializedDatabase = new SerializedObject(database);
                SerializedProperty entries = serializedDatabase.FindProperty("_entries");
                bool hasEntry = false;
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("mapId").stringValue == layout.mapId) hasEntry = true;
                if (!hasEntry)
                {
                    int index = entries.arraySize;
                    entries.InsertArrayElementAtIndex(index);
                    SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("mapId").stringValue = layout.mapId;
                    entry.FindPropertyRelative("config").objectReferenceValue = config;
                    entry.FindPropertyRelative("regionInfo").objectReferenceValue = region;
                    serializedDatabase.ApplyModifiedProperties();
                }
                var buildScenes = new List<EditorBuildSettingsScene>(previousBuildScenes);
                int existingIndex = buildScenes.FindIndex(scene => scene.path == layout.scenePath);
                if (existingIndex < 0) buildScenes.Add(new EditorBuildSettingsScene(layout.scenePath, true));
                else buildScenes[existingIndex] = new EditorBuildSettingsScene(layout.scenePath, true);
                // SceneManager는 Unity 기본 씬 로딩을 사용하므로 빌드 목록에만 새 씬을 추가한다.
                EditorBuildSettings.scenes = buildScenes.ToArray();
                context.MapID = layout.mapId;
                var serializedContext = new SerializedObject(context);
                serializedContext.FindProperty("_mapConfigDB").objectReferenceValue = database;
                serializedContext.ApplyModifiedProperties();
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
                if (!EditorSceneManager.SaveScene(context.gameObject.scene)) throw new IOException("씬 연결 저장에 실패했습니다.");
                Undo.CollapseUndoOperations(undoGroup);
                Directory.CreateDirectory(ReportDirectory);
                File.WriteAllText(ReportDirectory + "/Integration.txt",
                    $"씬: {layout.scenePath}\n지역: {layout.mapId}\n원본 흐름: {layout.sourceMapId}\n" +
                    $"기본 시작 지역: {database.DefaultStartMapId}\n지도: {configPath}\nFlowGraph: {region.flowGraphs.Count}\n");
                Debug.Log("[ScenarioMap] 지도·지역 실행선·빌드 씬 연결 완료: " + layout.mapId);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                EditorBuildSettings.scenes = previousBuildScenes;
                File.WriteAllBytes(buildSettingsPath, previousBuildSettings);
                File.WriteAllBytes(layout.mapDatabasePath, previousDatabase);
                AssetDatabase.ImportAsset(layout.mapDatabasePath, ImportAssetOptions.ForceUpdate);
                EditorSceneManager.SaveScene(context.gameObject.scene);
                throw;
            }
        }

        private static Sprite CaptureMapBackground(Layout layout, bool refresh = false)
        {
            string imagePath = layout.assetDirectory + "/MapBackground.png";
            if (!refresh && File.Exists(imagePath)) return AssetDatabase.LoadAssetAtPath<Sprite>(imagePath)
                ?? throw new InvalidOperationException("기존 지도 이미지를 Sprite로 가져오세요.");
            var cameraObject = new GameObject("MapBackgroundCapture");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = layout.terrainSize.z / 2;
            camera.transform.position = layout.terrainOrigin + new Vector3(layout.terrainSize.x / 2, 700, layout.terrainSize.z / 2);
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 1400;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.ambientGroundColor;
            const int height = 1700;
            int width = Mathf.RoundToInt(height * layout.terrainSize.x / layout.terrainSize.z);
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            bool previousFog = RenderSettings.fog;
            Renderer water = GameObject.Find("AuthoredEnvironment/LakeWater").GetComponent<Renderer>();
            Material previousWater = water.sharedMaterial;
            var mapWater = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mapWater.SetColor("_BaseColor", layout.safety.mapWaterColor);
            try
            {
                RenderSettings.fog = false;
                // 환경 수면 셰이더는 정사영에서 사라지므로 지도 촬영 때만 물 색을 고정한다.
                water.sharedMaterial = mapWater;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(imagePath, texture.EncodeToPNG());
            }
            finally
            {
                RenderSettings.fog = previousFog;
                water.sharedMaterial = previousWater;
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(mapWater);
            }
            AssetDatabase.ImportAsset(imagePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
        }
    }
}
