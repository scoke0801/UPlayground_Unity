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
using Unity.Profiling;
using UPlayGround.Data.World;

namespace UPlayGround.Tests.AmbientFauna
{
    /// <summary>실제 GPU 버퍼·이동·정지·재할당과 URP 출력의 차이를 Editor Play Mode에서 확인한다.</summary>
    public sealed class AmbientFaunaGpuTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string Evidence = "output/AmbientFauna/Visibility/";
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

        private static IEnumerator AwaitCapacity(Component group, int capacity)
        {
            for (int frame = 0; frame < 120 && (int)Get(group, "AllocatedCapacity") != capacity; frame++) yield return null;
            Assert.AreEqual(capacity, Get(group, "AllocatedCapacity"), "가시성/예산 평가 이후에도 할당이 복구되지 않았습니다.");
        }

        [UnityTest]
        public IEnumerator GPU_시야밖에서는_버퍼와_Dispatch를_중단하고_재진입시_복구한다()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/01.Scenes/Test/AmbientFaunaTest.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var camera = Camera.main;
            camera.transform.position = new Vector3(0, 2, -6);
            camera.transform.rotation = Quaternion.Euler(0, 180, 0);
            _profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(
                "Assets/10.Datas/World/AmbientFauna/Butterfly.asset"));
            _profile.HourDensity = AnimationCurve.Linear(0, 1, 24, 1);
            _profile.FadeSeconds = 0.03f;
            _root = new GameObject("VisibilityLifecycle"); _root.SetActive(false);
            var habitat = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var fallback = new GameObject("CpuFallback"); fallback.transform.SetParent(_root.transform);
            var cpu = fallback.AddComponent(RuntimeType("WildlifeCreature"));
            Set(cpu, "_profile", AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/10.Datas/World/Wildlife/Butterfly_01.asset"));
            var child = new GameObject("VisibilityGroup"); child.transform.SetParent(_root.transform); child.transform.position = Vector3.up * 2;
            var group = child.AddComponent(RuntimeType("AmbientFaunaGroup"));
            Set(group, "_profile", _profile); Set(group, "_capacity", 32);
            Set(group, "_anchors", new[] { new Vector4(0, 0, 0, 1) });
            Set(group, "_bakedLocalToWorld", child.transform.localToWorldMatrix);
            var fallbacks = Array.CreateInstance(RuntimeType("WildlifeCreature"), 1); fallbacks.SetValue(cpu, 0);
            Set(group, "_cpuFallbacks", fallbacks);
            _root.SetActive(true);
            for (int frame = 0; frame < 12; frame++) yield return null;
            Assert.AreEqual(0, Get(group, "AllocatedCapacity"), "처음부터 화면 밖인 그룹이 GPU를 할당했습니다.");
            Assert.AreEqual(0u, Get(group, "DebugSimulationDispatchCount"));
            Assert.IsFalse(fallback.activeSelf, "화면 밖 GPU 대신 CPU 나비가 켜졌습니다.");
            camera.transform.LookAt(child.transform.position);
            yield return AwaitCapacity(group, 32);
            for (int frame = 0; frame < 6; frame++) yield return null;
            uint visibleDispatches = (uint)Get(group, "DebugSimulationDispatchCount");
            Assert.Greater(visibleDispatches, 0u);
            long visibleBytes = (long)Get(group, "BufferBytes");
            camera.transform.rotation = Quaternion.Euler(0, 180, 0);
            for (int frame = 0; frame < 6; frame++) yield return null;
            Assert.AreEqual(0, Get(group, "AllocatedCapacity"));
            Assert.AreEqual(0L, Get(group, "BufferBytes"));
            Assert.AreEqual(0, Get(habitat, "AllocatedCapacity"));
            uint hiddenDispatches = (uint)Get(group, "DebugSimulationDispatchCount");
            uint generation = (uint)Get(group, "Generation");
            for (int frame = 0; frame < 30; frame++) yield return null;
            Assert.AreEqual(hiddenDispatches, Get(group, "DebugSimulationDispatchCount"));
            Assert.AreEqual(generation, Get(group, "Generation"), "비가시 상태에서 할당/해제가 반복됐습니다.");
            Assert.IsFalse(fallback.activeSelf);
            camera.transform.LookAt(child.transform.position);
            yield return AwaitCapacity(group, 32);
            Assert.Greater((uint)Get(group, "DebugSimulationDispatchCount"), hiddenDispatches);
            Time.timeScale = 0;
            camera.transform.rotation = Quaternion.Euler(0, 180, 0);
            for (int frame = 0; frame < 6; frame++) yield return null;
            Assert.AreEqual(0L, Get(group, "BufferBytes"), "일시정지 중 시야 이탈 버퍼가 남았습니다.");
            Time.timeScale = 1;
            camera.transform.LookAt(child.transform.position);
            yield return AwaitCapacity(group, 32);
            int visibleMask = camera.cullingMask;
            camera.cullingMask = 0;
            for (int frame = 0; frame < 6; frame++) yield return null;
            Assert.AreEqual(0L, Get(group, "BufferBytes"), "카메라 레이어에서 제외된 그룹의 버퍼가 남았습니다.");
            Assert.IsFalse(fallback.activeSelf, "카메라 레이어 제외가 CPU 원본을 깨웠습니다.");
            camera.cullingMask = visibleMask;
            yield return AwaitCapacity(group, 32);
            camera.transform.position = new Vector3(0, 2, -100);
            camera.transform.LookAt(child.transform.position);
            for (int frame = 0; frame < 6; frame++) yield return null;
            Assert.AreEqual(0L, Get(group, "BufferBytes"), "원거리지만 시야 안인 그룹의 버퍼가 남았습니다.");
            ((Behaviour)group).enabled = false;
            Assert.IsTrue(fallback.activeSelf, "그룹 비활성화에서 CPU 복구 계약이 깨졌습니다.");
            File.WriteAllText(Evidence + "visibility.json", "{\"initiallyHiddenBytes\":0,\"visibleBytes\":" + visibleBytes
                + ",\"hiddenBytes\":0,\"hiddenFrames\":30,\"hiddenAdditionalDispatches\":0,\"noAllocationChurn\":true,\"reentry\":true,\"pausedCull\":true,\"distanceCull\":true,\"layerCull\":true,\"fallbackKeptOffWhileHidden\":true,\"fallbackRestoredOnDisable\":true}");
        }
        [UnityTest]
        public IEnumerator GPU_초기분포와_방향전환이_공통고리로_정렬되지_않는다()
        {
            var shader = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Assets/06.Shaders/AmbientFauna/AmbientFauna.compute"));
            var particles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 64, 48);
            var anchors = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 16);
            try
            {
                anchors.SetData(new[] { new Vector4(0, 0, 0, 1) });
                int initialize = shader.FindKernel("Initialize"), simulate = shader.FindKernel("Simulate");
                foreach (int kernel in new[] { initialize, simulate })
                {
                    shader.SetBuffer(kernel, "_AF_Particles", particles);
                    shader.SetBuffer(kernel, "_AF_Anchors", anchors);
                }
                shader.SetInt("_AF_Capacity", 64); shader.SetInt("_AF_AnchorCount", 1);
                shader.SetInt("_AF_Seed", 74);
                shader.SetVector("_AF_SizeSpeed", new Vector4(0.012f, 0.02f, 0.05f, 0.18f));
                shader.SetVector("_AF_Movement", new Vector4(2, 1, 2, 0));
                shader.SetVector("_AF_Wander", new Vector4(3, 0.8f, 0.45f, 0.15f));
                shader.SetFloat("_AF_MeshRadius", 0.5f);
                shader.SetFloat("_AF_FrameDelta", 1f / 60);
                shader.Dispatch(initialize, 1, 1, 1);
                var request = AsyncGPUReadback.Request(particles);
                while (!request.done) yield return null;
                Assert.IsFalse(request.hasError);
                var initial = request.GetData<Particle>().ToArray();
                float radiusSum = 0, radiusSquaredSum = 0;
                foreach (var p in initial)
                {
                    float radius = new Vector3(p.PositionSize.x, p.PositionSize.y, p.PositionSize.z).magnitude;
                    radiusSum += radius; radiusSquaredSum += radius * radius;
                }
                float radiusDeviation = Mathf.Sqrt(radiusSquaredSum / 64 - Mathf.Pow(radiusSum / 64, 2));
                Assert.Greater(radiusDeviation, 0.05f, "초기 개체가 같은 반경의 고리에 놓였습니다.");
                var evidence = new System.Text.StringBuilder("behavior,time,polarization\n");
                for (int behavior = 0; behavior < 3; behavior++)
                {
                    shader.SetInt("_AF_Behavior", behavior);
                    shader.Dispatch(initialize, 1, 1, 1);
                    for (int sample = 1; sample <= 3; sample++)
                    {
                        for (int frame = (sample - 1) * 180; frame < sample * 180; frame++)
                        {
                            shader.SetFloat("_AF_Time", frame / 60f);
                            shader.Dispatch(simulate, 1, 1, 1);
                        }
                        request = AsyncGPUReadback.Request(particles);
                        while (!request.done) yield return null;
                        Assert.IsFalse(request.hasError);
                        var states = request.GetData<Particle>();
                        Vector3 headings = Vector3.zero;
                        foreach (var p in states)
                        {
                            Vector3 position = new(p.PositionSize.x, p.PositionSize.y, p.PositionSize.z);
                            Assert.IsFalse(float.IsNaN(position.sqrMagnitude));
                            Assert.LessOrEqual(position.magnitude, 1.001f);
                            headings += new Vector3(p.VelocityPhase.x, p.VelocityPhase.y, p.VelocityPhase.z).normalized;
                        }
                        float polarization = headings.magnitude / 64;
                        Assert.Less(polarization, 0.45f, "개체의 이동 방향이 군집 전체에서 정렬됐습니다.");
                        evidence.AppendLine($"{behavior},{sample * 3},{polarization.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    }
                }
                File.WriteAllText(Evidence + "independent-motion.csv", evidence.ToString());
                File.WriteAllText(Evidence + "initial-volume.json", "{\"radiusStandardDeviation\":"
                    + radiusDeviation.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            }
            finally { particles.Dispose(); anchors.Dispose(); UnityEngine.Object.DestroyImmediate(shader); }
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
            var region = _root.AddComponent(Type.GetType("UPlayGround.Manager.World.StreamedRegionContent, Assembly-CSharp", true));
            region.GetType().GetField("MapId").SetValue(region, "fauna.tests");
            region.GetType().GetField("ContentRoot").SetValue(region, _root);
            _root.SetActive(true);
            Assert.IsFalse((bool)Get(habitat, "IsRegionActive"), "지역 준비 전에 서식지가 공개됐습니다.");
            Call(region, "SetFaunaActive", true);
            yield return null; yield return null;
            Assert.IsTrue((bool)Get(group, "IsReady"));
            yield return AwaitCapacity(group, 32);
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
            yield return AwaitCapacity(group, 128);
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
            yield return AwaitCapacity(group, 128);
            ((Behaviour)group).enabled = false;
            Assert.AreEqual(0, Get(habitat, "AllocatedCapacity"));
            ((Behaviour)group).enabled = true;
            yield return null; yield return null;
            yield return AwaitCapacity(group, 128);
            for (int cycle = 0; cycle < 100; cycle++)
            {
                Call(region, "SetFaunaActive", false);
                Assert.IsNull(Get(group, "DebugParticleBuffer"));
                Assert.AreEqual(0, Get(habitat, "AllocatedCapacity"));
                Assert.AreEqual(0, Get(habitat, "CombatSubscriptionCount"));
                Assert.AreEqual(0, Get(habitat, "TerrainSubscriptionCount"));
                Call(region, "SetFaunaActive", true);
                yield return null; yield return null;
                yield return AwaitCapacity(group, 128);
            }
            Assert.IsTrue((bool)Call(habitat, "EmitCapsuleStimulus", child.transform.position, child.transform.position, 1f, 5f, 1f, 777));
            yield return null; yield return null;
            request = AsyncGPUReadback.Request((GraphicsBuffer)Get(group, "DebugParticleBuffer"));
            while (!request.done) yield return null;
            Validate(request.GetData<Particle>().ToArray(), 128);
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Ambient Fauna Tick", 256))
            {
                for (int frame = 0; frame < 120; frame++) yield return null;
                var samples = recorder.ToArray();
                File.WriteAllText(Evidence + "editor-cpu-samples.csv", "nanoseconds\n" + string.Join("\n", Array.ConvertAll(samples, s => s.Value.ToString())));
                File.WriteAllText(Evidence + "editor-cpu-context.json", "{\"mode\":\"Editor isolated test scene; not performance acceptance\",\"cycles\":100,\"frames\":120,\"counterValid\":" + recorder.Valid.ToString().ToLowerInvariant() + "}");
            }
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
        public IEnumerator 네_종의_GPU_행동과_부유입자_바람_순환이_유한하다()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/01.Scenes/Test/AmbientFaunaTest.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var rows = new System.Collections.Generic.List<string> { "species,meanSpeed,meanX,capacity" };
            foreach (string species in new[] { "Butterfly", "Gnat", "Firefly", "AirMote" })
            {
                _profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(
                    "Assets/10.Datas/World/AmbientFauna/" + species + ".asset"));
                _profile.HourDensity = AnimationCurve.Linear(0, 1, 24, 1);
                _profile.ParticleLifetime = 0.25f;
                _profile.FadeSeconds = 0.03f;
                _root = new GameObject("FaunaSpeciesGpu"); _root.SetActive(false);
                var habitat = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
                var child = new GameObject("GpuSpecies"); child.transform.SetParent(_root.transform);
                child.transform.position = Vector3.up * 2;
                var group = child.AddComponent(RuntimeType("AmbientFaunaGroup"));
                Set(group, "_profile", _profile); Set(group, "_capacity", 16);
                Set(group, "_anchors", new[] { new Vector4(0, 0, 0, 1) });
                Set(group, "_bakedLocalToWorld", child.transform.localToWorldMatrix);
                _root.SetActive(true);
                yield return null; yield return null;
                yield return AwaitCapacity(group, 16);
                // 검사 전용 고정 적분이다. 일반 루프의 readback이나 이중 갱신에 사용하지 않는다.
                for (int step = 0; step < 40; step++)
                {
                    Set(group, "_lastFrame", -1);
                    Call(group, "ApplyWorldInputs", 0.025f, Vector3.zero, false, Vector3.right * 0.5f);
                }
                var request = AsyncGPUReadback.Request((GraphicsBuffer)Get(group, "DebugParticleBuffer"));
                while (!request.done) yield return null;
                Assert.IsFalse(request.hasError);
                var states = request.GetData<Particle>().ToArray();
                Validate(states, 16);
                float speed = 0, x = 0;
                foreach (var state in states) { speed += ((Vector3)state.VelocityPhase).magnitude; x += state.PositionSize.x; }
                rows.Add(species + "," + (speed / 16).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "," + (x / 16).ToString(System.Globalization.CultureInfo.InvariantCulture) + ",16");
                if (species == "Firefly")
                {
                    _profile.HourDensity = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(
                        "Assets/10.Datas/World/AmbientFauna/Firefly.asset").HourDensity;
                    _profile.RainDensity = 0;
                    Call(habitat, "SetPreviewEnvironment", 12f, 0f, 0f);
                    yield return new WaitForSeconds(0.35f);
                    Assert.AreEqual(0, Get(group, "AllocatedCapacity"), "낮에 반딧불 버퍼가 반환되지 않았습니다.");
                    Call(habitat, "SetPreviewEnvironment", 22f, 0f, 0f);
                    yield return new WaitForSeconds(0.35f);
                    yield return AwaitCapacity(group, 16);
                    Call(habitat, "SetPreviewEnvironment", 22f, 1f, 0f);
                    yield return new WaitForSeconds(0.35f);
                    Assert.AreEqual(0, Get(group, "AllocatedCapacity"), "강수 억제 뒤 버퍼가 반환되지 않았습니다.");
                    Call(habitat, "SetPreviewEnvironment", 22f, 0f, 0f);
                    yield return new WaitForSeconds(0.35f);
                    yield return AwaitCapacity(group, 16);
                    var terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(2, 4, 2) };
                    var terrainRoot = Terrain.CreateTerrainGameObject(terrainData);
                    terrainRoot.transform.SetParent(_root.transform);
                    terrainRoot.transform.position = child.transform.position - new Vector3(1, 2, 1);
                    try
                    {
                        var heights = new float[33, 33];
                        for (int row = 0; row < 33; row++) for (int column = 0; column < 33; column++) heights[row, column] = 0.5f;
                        terrainData.SetHeights(0, 0, heights);
                        yield return null;
                        Assert.IsTrue((bool)Get(group, "IsGeometryInvalidated"), "지형 높이 변경 콜백이 군집을 억제하지 않았습니다.");
                        Assert.AreEqual(0, Get(group, "AllocatedCapacity"));
                    }
                    finally { UnityEngine.Object.DestroyImmediate(terrainRoot); UnityEngine.Object.DestroyImmediate(terrainData); }
                }
                UnityEngine.Object.DestroyImmediate(_root); _root = null;
                UnityEngine.Object.DestroyImmediate(_profile); _profile = null;
                Assert.AreEqual(0, Get(habitat, "AllocatedCapacity"));
            }
            File.WriteAllLines(Evidence + "species-gpu.csv", rows);
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
