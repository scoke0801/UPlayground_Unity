using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Gameplay.World.ToonFog;

namespace UPlayGround.Tool.Editor.ToonFog
{
    /// <summary>열린 씬을 저장하지 않고 실제 URP 고정 카메라의 비교 화면을 생성한다.</summary>
    public static class ToonFogRenderValidation
    {
        private const string Folder = "docs/work-status/toon-fog/captures";
        private const string GameScene = "Assets/01.Scenes/Scenario/DroughtVillage.unity";
        private const int Width = 640, Height = 360;

        public static string CaptureSample()
        {
            return WithScene(ToonFogSampleBuilder.ScenePath, scene =>
            {
                var controls = Find<ToonFogSampleControls>(scene);
                var world = controls.world;
                var camera = controls.sampleCamera;
                bool previousEnabled = world.fogEnabled;
                bool previousCapture = world.captureLegacyRefraction;
                float previousDensity = controls.density;
                bool previousOrthographic = camera.orthographic;
                Vector3 position = camera.transform.position;
                Quaternion rotation = camera.transform.rotation;
                Scene previousScene = camera.scene;
                camera.scene = scene;
                var report = new List<string>();
                try
                {
                    controls.SelectView(0);
                    world.fogEnabled = false;
                    Color32[] off = Render(camera, "sample-distance-off");
                    world.fogEnabled = true;
                    controls.density = 0;
                    Color32[] neutral = Render(camera, null);
                    report.Add(Compare("V01 밀도 0", off, neutral, expectChange: false));
                    controls.density = 1;
                    Color32[] on = Render(camera, "sample-distance-on");
                    report.Add(Compare("거리 안개 On", off, on, expectChange: true));

                    camera.orthographic = true;
                    Color32[] excluded = Render(camera, null);
                    world.fogEnabled = false;
                    report.Add(Compare("V12 직교 카메라", excluded, Render(camera, null), expectChange: false));
                    camera.orthographic = false;
                    world.fogEnabled = true;
                    report.Add(Compare("V12 원근 카메라 복귀", on, Render(camera, null), expectChange: false));

                    controls.SelectView(1);
                    Render(camera, "sample-local-on");
                    controls.SelectView(4);
                    Render(camera, "sample-height-on");
                    controls.SelectView(2);
                    world.fogEnabled = false;
                    Color32[] refractionOff = Render(camera, "sample-materials-off");
                    world.fogEnabled = true;
                    report.Add(Compare("굴절 재질 On", refractionOff, Render(camera, "sample-materials-on"), expectChange: true));
                    controls.SelectView(3);
                    Render(camera, "sample-regions-on");
                    return string.Join("\n", report);
                }
                finally
                {
                    world.fogEnabled = previousEnabled;
                    world.captureLegacyRefraction = previousCapture;
                    controls.density = previousDensity;
                    camera.orthographic = previousOrthographic;
                    camera.transform.SetPositionAndRotation(position, rotation);
                    camera.scene = previousScene;
                }
            });
        }

        public static string CaptureGame()
        {
            return WithScene(GameScene, scene =>
            {
                var world = Find<ToonFogWorld>(scene);
                var source = Find<ToonFogWeatherAdapter>(scene);
                if (world == null || world.profile == null || world.quality == null || world.environmentSource != source)
                    throw new InvalidOperationException("게임 씬의 프로필·품질·환경 입력 참조가 누락되었습니다.");

                var cameraObject = new GameObject("툰 안개 비교 카메라");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.nearClipPlane = 0.25f;
                camera.farClipPlane = 1600;
                camera.fieldOfView = 58;
                camera.scene = scene;
                bool original = world.fogEnabled;
                try
                {
                    Transform start = null;
                    foreach (var root in scene.GetRootGameObjects()) if (root.name == "Arrival_FromLake") start = root.transform;
                    if (start == null) throw new InvalidOperationException("게임 씬의 도착 지점이 없습니다.");
                    camera.transform.SetPositionAndRotation(start.position + Vector3.up * 3 - start.forward * 4,
                        Quaternion.LookRotation(start.forward + Vector3.down * 0.13f));
                    world.fogEnabled = false;
                    Color32[] off = Render(camera, "drought-village-off");
                    world.fogEnabled = true;
                    return Compare("게임 씬 고정 카메라", off, Render(camera, "drought-village-on"), expectChange: true);
                }
                finally
                {
                    world.fogEnabled = original;
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            });
        }

        private static string WithScene(string path, Func<Scene, string> capture)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행합니다.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("검증 씬의 미저장 편집이 있어 고정 화면을 실행할 수 없습니다.");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try { return capture(scene); }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static Color32[] Render(Camera camera, string name)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                if (Shader.GetGlobalFloat("_UPGToonFogEnabled") > 0.5f || Shader.GetGlobalInt("_UPGToonFogLocalCount") != 0)
                    throw new InvalidOperationException("카메라 렌더링 후 안개 상태가 남았습니다.");
                RenderTexture.active = target;
                read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                Color32[] pixels = read.GetPixels32();
                int pink = 0;
                foreach (var color in pixels) if (color.r > 245 && color.b > 245 && color.g < 10) pink++;
                if (pink > 0) throw new InvalidOperationException($"오류 셰이더로 보이는 픽셀 {pink}개: {name}");
                if (name != null)
                {
                    Directory.CreateDirectory(Folder);
                    File.WriteAllBytes(Path.Combine(Folder, name + ".png"), read.EncodeToPNG());
                }
                return pixels;
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                UnityEngine.Object.DestroyImmediate(read);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static string Compare(string label, Color32[] previous, Color32[] current, bool expectChange)
        {
            int changed = 0, alphaChanged = 0;
            int max = 0;
            for (int i = 0; i < current.Length; i++)
            {
                var a = previous[i];
                var b = current[i];
                int distance = Math.Max(Math.Abs(a.r - b.r), Math.Max(Math.Abs(a.g - b.g), Math.Abs(a.b - b.b)));
                if (distance > 1) changed++;
                if (a.a != b.a) alphaChanged++;
                max = Math.Max(max, distance);
            }
            string result = $"{label} | RGB 변경 {changed}/{current.Length}, 최대 채널차 {max}, alpha 변경 {alphaChanged}";
            if ((changed > 0) != expectChange || (!expectChange && alphaChanged > 0))
                throw new InvalidOperationException(result);
            return result;
        }
    }
}
