using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Data.World;
using UPlayGround.Gameplay.World;

namespace UPlayGround.EditorTools
{
    /// <summary>정규 호수의 데이터 연결과 실제 렌더 근거를 별도 단계로 기록한다.</summary>
    public static class WaterWorldIntegrationValidation
    {
        private const string WorldPath = "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
        private const string Evidence = "output/ToonWaterWorld/Rendering";

        [UPlaygroundTool("Water/IntegrateExistingLake")]
        public static void IntegrateExistingLake()
        {
            RunInWorld(() =>
            {
                const string path = WaterLabAuthoring.Folder + "/LakeWorldDynamics.asset";
                var dynamics = AssetDatabase.LoadAssetAtPath<WaterDynamicsProfileSO>(path);
                if (dynamics == null)
                {
                    dynamics = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WaterDynamicsProfileSO>(WaterLabAuthoring.DynamicsPath));
                    var first = dynamics.firstWave; first.wavelength = 48f; dynamics.firstWave = first;
                    var second = dynamics.secondWave; second.wavelength = 28f; dynamics.secondWave = second;
                    AssetDatabase.CreateAsset(dynamics, path);
                }
                Scene scene = SceneManager.GetActiveScene();
                if (scene.isDirty) throw new InvalidOperationException("미저장 정규 씬은 변경하지 않습니다.");
                foreach (var region in UnderwaterRegion.ActiveRegions)
                {
                    if (region.gameObject.scene != scene) continue;
                    Undo.RecordObject(region.Surface, "큰 호수 파도 프로필 연결");
                    region.Surface.dynamics = dynamics;
                    EditorUtility.SetDirty(region.Surface);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                WaterSceneIntegration.IntegrateLoadedScene();
                var records = new List<object>();
                foreach (var region in UnderwaterRegion.ActiveRegions)
                {
                    if (region.gameObject.scene != scene) continue;
                    records.Add(new
                    {
                        region = region.name, visualVertices = region.Surface.surfaceMesh.sharedMesh.vertexCount,
                        visualTriangles = region.Surface.surfaceMesh.sharedMesh.triangles.Length / 3,
                        queryTriangles = region.Surface.TriangleCount, gpuCells = region.PrismTriangleCount,
                        waveErrorBound = WaterWorldValidator.EstimateWaveInterpolationError(region.Surface),
                        mesh = AssetDatabase.GetAssetPath(region.Surface.surfaceMesh.sharedMesh),
                        dynamics = AssetDatabase.GetAssetPath(region.Surface.dynamics), rainExposure = region.rainExposure != null
                    });
                }
                var errors = WaterWorldValidator.InspectLoaded(out var warnings);
                Save("scene-integration.json", new { utc = DateTime.UtcNow.ToString("O"), records, errors, warnings });
                if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            });
        }

        [UPlaygroundTool("Water/CaptureExistingLake")]
        public static void CaptureExistingLake()
        {
            RunInWorld(() =>
            {
                var records = new List<object>();
                var failures = new List<string>();
                var feature = WaterLabValidation.GetFeature();
                var debug = typeof(UnderwaterRendererFeature).GetField("_debugView", BindingFlags.Instance | BindingFlags.NonPublic);
                object previous = debug.GetValue(feature);
                Scene scene = SceneManager.GetActiveScene();
                var camera = new GameObject("Water world validation camera").AddComponent<Camera>();
                camera.enabled = false; camera.scene = scene; camera.fieldOfView = 55f; camera.farClipPlane = 800f;
                var data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.requiresDepthTexture = true; data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                try
                {
                    foreach (var region in UnderwaterRegion.ActiveRegions)
                    {
                        if (region.gameObject.scene != scene) continue;
                        region.Surface.TrySampleHeight(region.SurfaceRenderer.bounds.center, out var water);
                        for (int view = 0; view < 2; view++)
                        {
                            camera.transform.position = water + (view == 0 ? new Vector3(0f, 4f, -10f) : new Vector3(0f, -2f, -6f));
                            camera.transform.LookAt(water + (view == 0 ? Vector3.forward * 10f : new Vector3(0f, -2f, 10f)));
                            string name = view == 0 ? "LakeVillage-Above" : "LakeVillage-Under";
                            debug.SetValue(feature, UnderwaterRendererFeature.DebugView.None);
                            TryCapture(camera, name, records, failures);
                            debug.SetValue(feature, UnderwaterRendererFeature.DebugView.IntervalBudget);
                            TryCapture(camera, name + "-Budget", records, failures);
                        }
                        break;
                    }
                    Save("world-captures.json", new { utc = DateTime.UtcNow.ToString("O"), records, failures });
                }
                finally
                {
                    debug.SetValue(feature, previous);
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }
                if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
            });
        }

        private static void TryCapture(Camera camera, string name, List<object> records, List<string> failures)
        {
            try { records.Add(WaterLabValidation.Capture(camera, Path.GetFullPath(Evidence), name, 640, 360, false)); }
            catch (Exception exception) { failures.Add(name + ": " + exception.Message); }
        }

        private static void RunInWorld(Action action)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행해야 합니다.");
            EditorApplication.LockReloadAssemblies();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(WorldPath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (opened) scene = EditorSceneManager.OpenScene(WorldPath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                action();
            }
            catch (Exception exception)
            {
                Save("world-operation-failure.json", new { utc = DateTime.UtcNow.ToString("O"), error = exception.ToString() });
                throw;
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void Save(string name, object data)
        {
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Path.Combine(Evidence, name), JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }
}
