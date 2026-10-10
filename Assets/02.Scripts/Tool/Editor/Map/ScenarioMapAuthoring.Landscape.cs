using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>진행 오브젝트를 유지하고 곡선 동선·식생 군락·지도 이미지를 함께 갱신한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 경관 개선")]
        public static void ImproveLandscape()
        {
            RequireEditMode();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 경관을 적용하세요.");
            Layout layout = ReadLayout();
            ValidateAssetPaths(layout);
            var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
            var quality = JsonUtility.FromJson<QualityLayout>(File.ReadAllText(QualityPath));
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            GameObject environment = FindQualityTransform(scene, "AuthoredEnvironment").gameObject;
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            var identities = new Dictionary<SceneEntityId, string>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                    identities.Add(identity, identity.Guid);
            string backup = BackupQualityAssets(layout);
            string mapPath = layout.assetDirectory + "/MapBackground.png";
            File.Copy(mapPath, backup + "/MapBackground.png");
            try
            {
                var routes = new List<Route>(layout.routes);
                routes.AddRange(world.routes);
                layout.routes = routes.ToArray();
                var clearings = new List<Clearing>(layout.clearings);
                clearings.AddRange(world.zones);
                layout.clearings = clearings.ToArray();
                ApplyLandscapeDistrict(layout, terrain, environment.transform);
                RemoveQualityVegetation(environment.transform, layout, quality);
                CreateScatters(layout, terrain, environment.transform);
                ClearSettlementVegetation(layout, environment.transform);
                ClearWorldVegetation(environment.transform, layout, world);
                foreach (Placement placement in layout.placements)
                {
                    if (!placement.applyWithLandscape) continue;
                    Transform existing = environment.transform.Find(placement.name);
                    if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    PlaceEnvironment(placement, terrain, environment.transform);
                }
                ApplyExistingQualityMaterials(layout, environment);
                PaintQualityRoutes(layout, terrain);
                Transform previous = environment.transform.Find("PlayableRoutes");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                CreateRouteFurniture(layout, terrain, environment.transform);
                Physics.SyncTransforms();
                BakeNavigation(layout, environment);
                foreach (var pair in identities)
                    if (pair.Key == null || pair.Key.Guid != pair.Value)
                        throw new InvalidOperationException("경관 변경 중 저장 식별자가 변경되었습니다: " + pair.Value);
                Validate();
                ValidateWorld(world, terrain, FindQualityTransform(scene, "WorldExpansionActors"));
                ValidateBoundaries(layout, terrain, environment.transform.Find("PlayBoundaries"));
                ValidateLandscapeDistrict(layout, terrain, scene);
                SaveQualityScene(scene, layout);
                AssetDatabase.SaveAssets();
                CaptureMapBackground(layout, true);
                CapturePreviewImages(false);
                File.WriteAllText(ReportDirectory + "/Landscape.txt",
                    "경관 적용·동선·저장 ID 검증 통과\n보존 ID: " + identities.Count + "\n백업: " + backup);
                Debug.Log("[ScenarioMap] 곡선 동선·군락 경관 적용 및 지도 재촬영 완료");
            }
            catch
            {
                RestoreQualityBackup(layout, backup);
                File.Copy(backup + "/MapBackground.png", mapPath, true);
                AssetDatabase.ImportAsset(mapPath, ImportAssetOptions.ForceUpdate);
                throw;
            }
        }
    }
}
