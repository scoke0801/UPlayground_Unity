using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.Data.World;
using UPlayGround.Gameplay.World;

namespace UPlayGround.EditorTools
{
    /// <summary>실제 URP 카메라 출력을 고정 시점에서 저장하고 수중 마스크·카메라 수명을 검사한다.</summary>
    public static class WaterLabValidation
    {
        public const string EvidenceFolder = "output/ToonWaterWorld/Rendering";
        private static readonly FieldInfo DebugField = typeof(UnderwaterRendererFeature).GetField("_debugView", BindingFlags.Instance | BindingFlags.NonPublic);

        [UPlaygroundTool("Water/CaptureLab")]
        public static void CaptureLab()
        {
            CaptureLabWithSamples(1);
        }

        /// <summary>단일 샘플 검사에서 빠졌던 MSAA 색/깊이 연결을 실제 URP 출력으로 검사한다.</summary>
        [UPlaygroundTool("Water/CaptureLabMsaa")]
        public static void CaptureLabMsaa()
        {
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            int previous = pipeline.msaaSampleCount;
            try
            {
                foreach (int samples in new[] { 1, 2, 4 })
                {
                    pipeline.msaaSampleCount = samples;
                    CaptureLabWithSamples(samples, "MSAA-" + samples);
                }
            }
            finally { pipeline.msaaSampleCount = previous; }
        }

        private static void CaptureLabWithSamples(int samples, string subfolder = null)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행해야 합니다.");
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", EvidenceFolder));
            if (subfolder != null) output = Path.Combine(output, subfolder);
            Directory.CreateDirectory(output);
            var feature = GetFeature();
            object originalDebug = DebugField.GetValue(feature);
            var dynamics = AssetDatabase.LoadAssetAtPath<WaterDynamicsProfileSO>(WaterLabAuthoring.DynamicsPath);
            if (dynamics == null) throw new InvalidOperationException("WaterLab 파도 프로필 누락");
            Scene previous = SceneManager.GetActiveScene();
            Scene lab = SceneManager.GetSceneByPath(WaterLabAuthoring.ScenePath);
            bool opened = !lab.IsValid() || !lab.isLoaded;
            if (opened) lab = EditorSceneManager.OpenScene(WaterLabAuthoring.ScenePath, OpenSceneMode.Additive);
            var first = dynamics.firstWave;
            var second = dynamics.secondWave;
            var records = new List<object>();
            var cameras = new List<Camera>();
            foreach (var root in lab.GetRootGameObjects()) cameras.AddRange(root.GetComponentsInChildren<Camera>(true));
            SceneManager.SetActiveScene(lab);
            try
            {
                foreach (var root in lab.GetRootGameObjects())
                    foreach (var link in root.GetComponentsInChildren<WaterfallLink>(true))
                        if (link.mist != null) link.mist.Simulate(0.8f, true, true, true);
                foreach (var camera in cameras)
                {
                    camera.scene = lab;
                    DebugField.SetValue(feature, UnderwaterRendererFeature.DebugView.None);
                    records.Add(Capture(camera, output, camera.name, 640, 360, false, samples: samples));
                }
                // 수면선의 픽셀 분리는 시간에 따라 변하는 파도와 분리해서 검사한다.
                var flatFirst = first; flatFirst.amplitude = 0f;
                var flatSecond = second; flatSecond.amplitude = 0f;
                dynamics.firstWave = flatFirst; dynamics.secondWave = flatSecond;
                DebugField.SetValue(feature, UnderwaterRendererFeature.DebugView.OpticalLength);
                foreach (var camera in cameras)
                {
                    if (camera.name != "HalfSubmerged" && camera.name != "UnderLookDown" && camera.name != "DryCave") continue;
                    records.Add(Capture(camera, output, camera.name + "-OpticalLength", 400, 224, true, samples: samples));
                }
                Camera top = cameras.Find(camera => camera.name == "TopDown");
                top.orthographic = true; top.orthographicSize = 17f;
                records.Add(Capture(top, output, "Orthographic-OpticalLength", 320, 240, true, samples: samples));
                top.orthographic = false;
                DebugField.SetValue(feature, UnderwaterRendererFeature.DebugView.None);
                records.Add(CaptureFogOwnership(lab, cameras.Find(camera => camera.name == "HalfSubmerged"), output, samples));
                CaptureWeatherVisibility(lab, cameras.Find(camera => camera.name == "HalfSubmerged"), output, samples, records);
                File.WriteAllText(Path.Combine(output, "captures.json"), JsonConvert.SerializeObject(new
                {
                    utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
                    graphics = SystemInfo.graphicsDeviceType.ToString(), pipeline = GraphicsSettings.currentRenderPipeline.name,
                    colorSpace = QualitySettings.activeColorSpace.ToString(),
                    scope = "URP Editor 고정 카메라의 렌더링과 광학 마스크 검사. 입력·아트 품질·Player/GPU 시간의 인수 결과가 아님.",
                    records
                }, Formatting.Indented));
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(output, "capture-failure.json"), JsonConvert.SerializeObject(new
                {
                    utc = DateTime.UtcNow.ToString("O"), error = exception.ToString(), completed = records
                }, Formatting.Indented));
                throw;
            }
            finally
            {
                DebugField.SetValue(feature, originalDebug);
                dynamics.firstWave = first; dynamics.secondWave = second;
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(lab, true);
            }
        }

        private static object CaptureFogOwnership(Scene scene, Camera camera, string output, int samples)
        {
            UnderwaterProfileSO profile = AssetDatabase.LoadAssetAtPath<UnderwaterProfileSO>(WaterLabAuthoring.Folder + "/LakeOptics.asset");
            Vector3 extinction = profile.extinction;
            FogMode fogMode = RenderSettings.fogMode;
            float density = RenderSettings.fogDensity;
            Color fogColor = RenderSettings.fogColor;
            Vector3 position = camera.transform.position;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", Color.white);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(target, scene);
            target.transform.position = new Vector3(10000f, -2f, 10001f);
            target.transform.localScale = new Vector3(2f, 2f, 1f);
            target.GetComponent<Renderer>().sharedMaterial = material;
            try
            {
                profile.extinction = Vector3.zero;
                RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.4f; RenderSettings.fogColor = Color.black;
                camera.transform.position = new Vector3(10000f, -2f, 9996f);
                return Capture(camera, output, "FogOwnership-UnfoggedUnderwater", 160, 90, false, true, samples);
            }
            finally
            {
                profile.extinction = extinction;
                RenderSettings.fogMode = fogMode; RenderSettings.fogDensity = density; RenderSettings.fogColor = fogColor;
                camera.transform.position = position;
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static void CaptureWeatherVisibility(Scene scene, Camera camera, string output, int samples, List<object> records)
        {
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            var material = new Material(Shader.Find("UPlayGround/Environment/Weather Particle"));
            material.SetColor("_BaseColor", Color.red);
            material.SetFloat("_CameraFadeNear", 0f); material.SetFloat("_CameraFadeFar", 0.1f);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SceneManager.MoveGameObjectToScene(quad, scene);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            quad.transform.localScale = Vector3.one * 4f;
            try
            {
                camera.transform.rotation = Quaternion.identity;
                camera.transform.position = new Vector3(10000f, 2f, 9996f);
                quad.transform.position = new Vector3(10000f, 2f, 10001f);
                records.Add(Capture(camera, output, "Weather-Air", 160, 90, false, samples: samples, weatherExpectation: 1));
                camera.transform.position = new Vector3(10000f, -2f, 9996f);
                quad.transform.position = new Vector3(10000f, -2f, 10001f);
                records.Add(Capture(camera, output, "Weather-SubmergedCamera", 160, 90, false, samples: samples, weatherExpectation: -1));
                camera.transform.position = new Vector3(10000f, 2f, 9996f);
                camera.transform.LookAt(quad.transform.position);
                records.Add(Capture(camera, output, "Weather-BehindSurface", 160, 90, false, samples: samples, weatherExpectation: -1));
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(material);
            }
        }

        internal static UnderwaterRendererFeature GetFeature()
        {
            var pipeline = new SerializedObject(GraphicsSettings.currentRenderPipeline);
            var renderers = pipeline.FindProperty("m_RendererDataList");
            int index = pipeline.FindProperty("m_DefaultRendererIndex").intValue;
            var renderer = renderers.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
            foreach (var feature in renderer.rendererFeatures)
                if (feature is UnderwaterRendererFeature water && water.isActive) return water;
            throw new InvalidOperationException("활성 렌더러에 수면 합성 Feature가 없습니다.");
        }

        internal static object Capture(Camera camera, string folder, string name, int width, int height, bool inspectMask, bool verifyFogOwnership = false, int samples = 1, int weatherExpectation = 0)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            bool previousMsaa = camera.allowMSAA;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf) { name = "WaterLab validation", antiAliasing = samples };
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                camera.allowMSAA = true;
                target.Create(); camera.targetTexture = target;
                if (target.antiAliasing != samples) throw new InvalidOperationException("요청 MSAA 미지원: " + samples);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                long before = UnderwaterRendererFeature.ResolveExecutionCount;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                watch.Stop();
                if (UnderwaterRendererFeature.ResolveExecutionCount != before + 1 || UnderwaterRendererFeature.LastResolvedCamera != camera.GetInstanceID())
                    throw new InvalidOperationException(name + ": 카메라당 Resolve가 정확히 한 번 실행되지 않았습니다.");
                if (Shader.GetGlobalFloat("_WaterSceneActive") != 0f)
                    throw new InvalidOperationException(name + ": 카메라 전역 상태가 복구되지 않았습니다.");
                if (Shader.GetGlobalFloat("_WaterCameraSubmerged") != 0f)
                    throw new InvalidOperationException(name + ": 카메라 잠수 상태가 복구되지 않았습니다.");
                if (UnderwaterRendererFeature.LastRejectedCount != 0)
                    throw new InvalidOperationException(name + ": 수역 버퍼 예산 초과.");
                if (UnderwaterRendererFeature.LastColorSamples != samples || UnderwaterRendererFeature.LastDepthSamples != samples)
                    throw new InvalidOperationException(name + ": 실제 색/깊이 MSAA가 요청과 다릅니다: "
                        + UnderwaterRendererFeature.LastColorSamples + "/" + UnderwaterRendererFeature.LastDepthSamples + " 요청=" + samples);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                Color32[] colors = pixels.GetPixels32();
                Color center = pixels.GetPixel(width / 2, height / 2);
                bool hasWeather = center.r > 0.75f && center.g < 0.2f;
                if (verifyFogOwnership && center.r < 0.95f)
                    throw new InvalidOperationException("수중의 흰 불투명 물체에 공기 Fog가 중복 적용됩니다: " + center);
                int wet = 0, pink = 0;
                for (int i = 0; i < colors.Length; i++)
                {
                    Color32 color = colors[i];
                    if (color.r > 220 && color.b > 220 && color.g < 30) pink++;
                    if (color.g > 20 && color.b < 10) wet++;
                }
                // HDR RT는 선형 값이다. PNG에는 표시용 sRGB를 기록해 실제 화면보다 어둡게 보이지 않게 한다.
                Color[] display = pixels.GetPixels();
                for (int i = 0; i < display.Length; i++) display[i] = display[i].gamma;
                pixels.SetPixels(display); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
                if ((weatherExpectation > 0 && !hasWeather) || (weatherExpectation < 0 && hasWeather))
                    throw new InvalidOperationException(name + ": 날씨 차폐 결과가 기대와 다릅니다: " + center);
                if (pink != 0) throw new InvalidOperationException(name + ": 셰이더 오류 또는 구간 예산 초과(자홍색) " + pink);
                if (inspectMask)
                {
                    if (camera.name == "DryCave" && wet != 0) throw new InvalidOperationException("건조 동굴에 수중 구간이 누출됩니다.");
                    if (camera.name == "HalfSubmerged" && (wet < colors.Length / 5 || wet > colors.Length * 4 / 5))
                        throw new InvalidOperationException("반잠수 화면에 공기·수중 구간이 함께 나타나지 않습니다.");
                    if (camera.name == "UnderLookDown" && wet < colors.Length / 2)
                        throw new InvalidOperationException("완전 잠수 상태의 바닥에 수중 광학 길이가 없습니다.");
                    if (camera.orthographic && wet == 0) throw new InvalidOperationException("정사영에서 수중 광학 길이가 없습니다.");
                }
                return new
                {
                    name, width, height, msaaSamples = target.antiAliasing, camera = camera.transform.position.ToString("F4"), rotation = camera.transform.eulerAngles.ToString("F4"),
                    colorSamples = UnderwaterRendererFeature.LastColorSamples, depthSamples = UnderwaterRendererFeature.LastDepthSamples,
                    near = camera.nearClipPlane, camera.orthographic, bodyCount = UnderwaterRendererFeature.LastBodyCount,
                    cellCount = UnderwaterRendererFeature.LastCellCount, wetPixels = inspectMask ? wet : -1, pinkPixels = pink,
                    centerLinear = center.ToString("F4"),
                    weatherExpectation,
                    cpuRenderRequestMilliseconds = watch.Elapsed.TotalMilliseconds,
                    timingMeaning = "동기 URP SingleCameraRequest의 CPU 경과 시간. GPU 전용 시간·실기 성능이 아님."
                };
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.allowMSAA = previousMsaa;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
