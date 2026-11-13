#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UPlayGround.Data.World;

namespace UPlayGround.Tests.AmbientFauna
{
    /// <summary>실제 GPU 버퍼·이동·정지·재할당과 URP 출력의 차이를 Editor Play Mode에서 확인한다.</summary>
    public sealed class AmbientFaunaGpuTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string Evidence = "output/AmbientFauna/GpuImplementation/";
        private GameObject _root;
        private AmbientFaunaProfileSO _profile;
        private float _timeScale;
        private static Type RuntimeType(string name) => Type.GetType("UPlayGround.Gameplay.World.LivingWorld." + name + ", Assembly-CSharp", true);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Get(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
        [StructLayout(LayoutKind.Sequential)] private struct Particle
        {
            public Vector4 PositionSize, VelocityPhase, HeadingSeed;
        }
        [SetUp] public void Prepare() { _timeScale = Time.timeScale; Time.timeScale = 1; Directory.CreateDirectory(Evidence); }
        [TearDown] public void Cleanup()
        {
            Time.timeScale = _timeScale;
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_profile != null) UnityEngine.Object.DestroyImmediate(_profile);
        }

        [UnityTest]
        public IEnumerator GPU_이동과_정지_재할당_해제가_개체별_CPU_객체없이_동작한다()
        {
            Assert.IsTrue(SystemInfo.supportsComputeShaders && SystemInfo.supportsAsyncGPUReadback);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/01.Scenes/Test/AmbientFaunaTest.unity", new LoadSceneParameters(LoadSceneMode.Single));
            _profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(
                "Assets/10.Datas/World/AmbientFauna/Butterfly.asset"));
            _profile.HourDensity = AnimationCurve.Linear(0, 1, 24, 1);
            _profile.FadeSeconds = 0.03f;
            _root = new GameObject("FaunaGpuLifecycle"); _root.SetActive(false);
            Component habitat = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var child = new GameObject("GpuGroup"); child.transform.SetParent(_root.transform);
            child.transform.localPosition = Vector3.up * 2;
            Component group = child.AddComponent(RuntimeType("AmbientFaunaGroup"));
            Set(group, "_profile", _profile);
            Set(group, "_capacity", 32);
            Set(group, "_anchors", new[] { new Vector4(0, 0, 0, 1) });
            Set(group, "_bakedLocalToWorld", child.transform.localToWorldMatrix);
            _root.SetActive(true);
            yield return null; yield return null;
            Assert.IsTrue((bool)Get(group, "IsReady"));
            Assert.AreEqual(32, Get(group, "AllocatedCapacity"));
            var buffer = (GraphicsBuffer)Get(group, "DebugParticleBuffer");
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(buffer);
            while (!request.done) yield return null;
            Assert.IsFalse(request.hasError);
            Particle[] initial = request.GetData<Particle>().ToArray();
            Validate(initial, 32);
            for (int i = 0; i < 6; i++) yield return null;
            request = AsyncGPUReadback.Request(buffer);
            while (!request.done) yield return null;
            Particle[] moved = request.GetData<Particle>().ToArray();
            Validate(moved, 32);
            Assert.AreNotEqual(initial[0].PositionSize, moved[0].PositionSize);
            Assert.AreNotEqual(initial[0].VelocityPhase.w, initial[1].VelocityPhase.w);
            // 관측점과 개체의 정확한 중첩에서도 GPU 나눗셈이 NaN을 만들지 않아야 한다.
            var observer = new GameObject("Observer"); observer.transform.SetParent(_root.transform);
            observer.transform.position = child.transform.position;
            Set(habitat, "_observerOverride", observer.transform);
            var centered = moved[0]; centered.PositionSize.x = centered.PositionSize.y = centered.PositionSize.z = 0;
            buffer.SetData(new[] { centered }, 0, 0, 1);
            yield return null; yield return null;
            request = AsyncGPUReadback.Request(buffer);
            while (!request.done) yield return null;
            Validate(request.GetData<Particle>().ToArray(), 32);
            Time.timeScale = 0;
            yield return null;
            request = AsyncGPUReadback.Request(buffer);
            while (!request.done) yield return null;
            Particle[] paused = request.GetData<Particle>().ToArray();
            for (int i = 0; i < 4; i++) yield return null;
            request = AsyncGPUReadback.Request(buffer);
            while (!request.done) yield return null;
            var still = request.GetData<Particle>();
            for (int i = 0; i < paused.Length; i++)
            {
                Assert.AreEqual(paused[i].PositionSize, still[i].PositionSize);
                Assert.AreEqual(paused[i].VelocityPhase, still[i].VelocityPhase);
            }
            Time.timeScale = 1;
            int objects = _root.GetComponentsInChildren<Transform>(true).Length;
            uint generation = (uint)Get(group, "Generation");
            Set(group, "_capacity", 128);
            group.GetType().GetMethod("OnValidate", Private).Invoke(group, null);
            yield return null; yield return null;
            Assert.AreEqual(128, Get(group, "AllocatedCapacity"));
            Assert.AreEqual(128, Get(habitat, "AllocatedCapacity"));
            Assert.AreEqual(objects, _root.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(0, _root.GetComponentsInChildren<Animator>(true).Length);
            Assert.Greater((uint)Get(group, "Generation"), generation);
            request = AsyncGPUReadback.Request((GraphicsBuffer)Get(group, "DebugParticleBuffer"));
            while (!request.done) yield return null;
            Validate(request.GetData<Particle>().ToArray(), 128);
            ((Behaviour)habitat).enabled = false;
            Assert.AreEqual(0, Get(habitat, "AllocatedCapacity"));
            Assert.IsNull(Get(group, "DebugParticleBuffer"));
            ((Behaviour)habitat).enabled = true;
            yield return null; yield return null;
            Assert.AreEqual(128, Get(group, "AllocatedCapacity"));
            ((Behaviour)group).enabled = false;
            Assert.AreEqual(0, Get(habitat, "AllocatedCapacity"));
            ((Behaviour)group).enabled = true;
            yield return null; yield return null;
            Assert.AreEqual(128, Get(group, "AllocatedCapacity"));
            File.WriteAllText(Evidence + "gpu-lifecycle.json", "{\"capacity32\":true,\"capacity128\":true,\"finiteAndBounded\":true,\"pausedStateUnchanged\":true,\"cpuObjectCountUnchanged\":true,\"releaseReactivation\":true,\"device\":\"" + SystemInfo.graphicsDeviceName + "\"}");
        }

        private static void Validate(Particle[] states, int expected)
        {
            Assert.AreEqual(expected, states.Length);
            foreach (Particle particle in states)
            {
                for (int j = 0; j < 4; j++)
                {
                    Assert.IsTrue(AmbientFaunaProfileSO.IsFinite(particle.PositionSize[j]));
                    Assert.IsTrue(AmbientFaunaProfileSO.IsFinite(particle.VelocityPhase[j]));
                    Assert.IsTrue(AmbientFaunaProfileSO.IsFinite(particle.HeadingSeed[j]));
                }
                Assert.LessOrEqual(((Vector3)particle.PositionSize).magnitude, 1.001f);
                Assert.Greater(particle.PositionSize.w, 0);
            }
        }

        [UnityTest]
        public IEnumerator URP_나비_셰이더가_실제_색상_픽셀을_출력한다()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/01.Scenes/Test/AmbientFaunaTest.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var habitat = UnityEngine.Object.FindFirstObjectByType(RuntimeType("AmbientFaunaHabitat")) as Behaviour;
            Assert.IsNotNull(habitat);
            var driverType = Type.GetType("UPlayGround.CombatValidation.AmbientFaunaPreviewDriver, Assembly-CSharp", true);
            ((Behaviour)UnityEngine.Object.FindFirstObjectByType(driverType)).enabled = false;
            Call(habitat, "SetPreviewEnvironment", 12f, 0f, 0f);
            Camera camera = Camera.main;
            Assert.IsNotNull(camera);
            camera.transform.position = new Vector3(-3, 2.1f, -2.5f);
            camera.transform.LookAt(new Vector3(-3, 1.35f, 0));
            camera.fieldOfView = 42;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            try
            {
                habitat.enabled = false;
                yield return null; yield return null;
                Color32[] before = Capture(target, image, Evidence + "butterfly-off.png");
                habitat.enabled = true;
                yield return new WaitForSeconds(1.1f);
                var groups = UnityEngine.Object.FindObjectsByType(RuntimeType("AmbientFaunaGroup"), FindObjectsSortMode.None);
                var lines = new System.Collections.Generic.List<string> {
                    "hour=" + habitat.GetType().GetField("_hour", Private).GetValue(habitat),
                    "capacity=" + Get(habitat, "AllocatedCapacity") };
                foreach (var group in groups) lines.Add(group.name + " state=" + Get(group, "State")
                    + " ready=" + Get(group, "IsReady") + " allocated=" + Get(group, "AllocatedCapacity")
                    + " density=" + Get(group, "DisplayDensity") + " bounds=" + Get(group, "WorldBounds"));
                File.WriteAllText(Evidence + "render-diagnostics.txt", string.Join("\n", lines));
                Color32[] after = Capture(target, image, Evidence + "butterfly-on.png");
                int changed = 0;
                for (int i = 0; i < after.Length; i++)
                    if (Math.Abs(after[i].r - before[i].r) + Math.Abs(after[i].g - before[i].g) + Math.Abs(after[i].b - before[i].b) > 30) changed++;
                Assert.Greater(changed, 30, "Indirect 출력이 실제 색상에 반영되지 않았습니다.");
                File.WriteAllText(Evidence + "render-check.json", "{\"changedPixels\":" + changed + ",\"width\":1280,\"height\":720,\"gpu\":\"" + SystemInfo.graphicsDeviceName + "\"}");
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static Color32[] Capture(RenderTexture target, Texture2D image, string path)
        {
            var groups = UnityEngine.Object.FindObjectsByType(RuntimeType("AmbientFaunaGroup"), FindObjectsSortMode.None);
            foreach (var group in groups) Call(group, "ApplyWorldInputs", 0f, Vector3.zero, false, Vector3.zero);
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); return image.GetPixels32();
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
#endif
