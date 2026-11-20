using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>P09 전용 씬에서 실제 청크와 프레임 비용을 기록한다.</summary>
    public sealed class WorldBuilderP09Probe : MonoBehaviour
    {
        public WorldChunkStreamer streamer;
        public Camera view;
        public bool p08Baseline;

        private readonly FrameTiming[] _timings = new FrameTiming[1];
        private readonly List<(string phase, int round, int frame, float delta, double cpu, double main, double gpu, long gc, long coordinator, long activation)> _frames = new(65536);
        private readonly List<(string phase, int round, int chunk, WorldChunkState previous, WorldChunkState next, double elapsed, int frame)> _transitions = new(16384);
        private ProfilerRecorder _allocations, _coordinator, _activation;
        private WorldChunkState[] _previous;
        private double[] _changed;
        private int _run;
        private string _phase;
        private string _output;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string MemoryOutputVariable = "UPLAYGROUND_P09_MEMORY_OUTPUT";
        private string _memoryOutput;
#endif
        private int _nativePeak;
        private long _unityPeak, _workingSetPeak, _privatePeak;
        private int _tally;
        private readonly List<(string kind, string scene, double timestamp, int frame, int round)> _events = new(4096);
#if !UNITY_EDITOR
        private IEnumerator Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            _output = Path.GetDirectoryName(Application.dataPath);
            if (p08Baseline)
            {
                _output = Path.Combine(_output, "player-p08");
                Directory.CreateDirectory(_output);
#if DEVELOPMENT_BUILD
                ConfigureMemoryCapture();
#endif
            }
            yield return Measure();
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(next);
                yield break;
            }
            Application.Quit();
        }
#else
        private IEnumerator Start()
        {
            if (!p08Baseline) yield break;
            Application.runInBackground = true;
            ConfigureMemoryCapture();
            _output = _memoryOutput ?? "output/WorldBuilder/P09/editor-p08";
            Directory.CreateDirectory(_output);
            yield return Measure();
        }
#endif

        private IEnumerator Measure()
        {
            string error = "catalog 대기";
            yield return streamer.WaitForCatalog(value => error = value);
            if (error != null) throw new InvalidOperationException(error);
            var catalog = streamer.Catalog;
            int count = catalog.chunks.Length;
            string log = Path.Combine(_output, "runtime-" + count + ".log");
            File.WriteAllText(log, $"{catalog.worldBounds.size} | {Application.unityVersion} | {SystemInfo.processorType} | {SystemInfo.graphicsDeviceName} | {SystemInfo.graphicsDeviceType} | {Screen.width}x{Screen.height} | {QualitySettings.names[QualitySettings.GetQualityLevel()]} | {GraphicsSettings.currentRenderPipeline}\n");
            _previous = new WorldChunkState[count];
            _changed = new double[count];
            _allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _coordinator = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "WorldBuilder.Streaming.Update");
            _activation = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "WorldBuilder.Streaming.ActivateGroup");
            streamer.ChunkPreparing += OnPreparing;
            streamer.ChunkVisibilityChanged += OnVisibility;
            if (p08Baseline) yield return CaptureP08();
            var tracker = new GameObject("P09 경로 추적").transform;
            Vector3 near = catalog.chunks[0].bounds.center;
            Vector3 far = catalog.chunks[count - 1].bounds.center;
            streamer.DiagnosticMinimumResidency = 0;
            streamer.TrackPlayer(tracker);
            for (_run = 1; _run <= 5; _run++)
            {
                yield return Move(near, count - 1, "warm");
                yield return Move(far, 0, "warm");
            }

            _phase = null;
            yield return Collect();
            Mark("warm");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_memoryOutput != null) yield return CaptureMemory("warm");
#endif
            _phase = "steady";
            for (int frame = 0; frame < 120; frame++) yield return null;
            for (_run = 1; _run <= 50; _run++)
            {
                yield return Move(near, count - 1, "travel");
                yield return Move(far, 0, "travel");
                if (_run % 10 == 0) Mark("round " + _run);
            }
            _phase = null;
            Mark("uncollected");
            yield return Collect();
            Mark("collected");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_memoryOutput != null) yield return CaptureMemory("collected");
#endif
            WriteSamples();
            if (!p08Baseline)
            {
                yield return VerifyStreamFailures(near, far, log);
                yield return VerifyAgent(near, log);
            }
            _phase = "close";
            catalog = null;
            Destroy(tracker.gameObject);
            yield return streamer.CloseWorld();
            _phase = null;
            streamer.ChunkPreparing -= OnPreparing;
            streamer.ChunkVisibilityChanged -= OnVisibility;
            yield return Collect();
            Mark("closed");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_memoryOutput != null) yield return CaptureMemory("closed");
#endif
            if (streamer.OwnedSceneCount != 0 || streamer.OwnedAssetCount != 0 || streamer.PinCount != 0 || SceneManager.sceneCount != 1)
                throw new InvalidOperationException("월드 종료 뒤 씬 또는 핸들이 남았습니다.");
            File.AppendAllText(log, $"sampled peak: unity={_unityPeak}; workingSet={_workingSetPeak}; private={_privatePeak}; nativeObjects={_nativePeak}; samples={_tally}\n");
            WriteSamples();
            File.AppendAllText(log, "COMPLETE\n");

            void WriteSamples()
            {
                File.WriteAllLines(Path.Combine(_output, "frames-" + count + ".csv"),
                    new[] { "phase,round,frame,delta_ms,cpu_ms,main_ms,gpu_ms,gc_alloc_bytes,coordinator_ns,activation_ns" }.Concat(_frames.Select(sample => FormattableString.Invariant(
                        $"{sample.phase},{sample.round},{sample.frame},{sample.delta:F3},{sample.cpu:F3},{sample.main:F3},{sample.gpu:F3},{sample.gc},{sample.coordinator},{sample.activation}"))));
                File.WriteAllLines(Path.Combine(_output, "chunks-" + count + ".csv"),
                    new[] { "phase,round,chunk,previous,next,state_elapsed_ms,frame" }.Concat(_transitions.Select(sample => FormattableString.Invariant(
                        $"{sample.phase},{sample.round},{sample.chunk},{sample.previous},{sample.next},{sample.elapsed:F3},{sample.frame}"))));
                File.WriteAllLines(Path.Combine(_output, "events-" + count + ".csv"),
                    new[] { "event,scene,realtime_ms,frame,round" }.Concat(_events.Select(sample => FormattableString.Invariant(
                        $"{sample.kind},{sample.scene},{sample.timestamp:F3},{sample.frame},{sample.round}"))));
            }

            IEnumerator Move(Vector3 destination, int retired, string phase)
            {
                _phase = phase;
                string failure = "목적지 대기";
                yield return streamer.PrepareDestination(destination, value => failure = value);
                if (failure != null) throw new InvalidOperationException(failure);
                tracker.position = destination;
                view.transform.SetPositionAndRotation(new Vector3(destination.x, 65, destination.z), Quaternion.Euler(25, 45, 0));
                yield return WaitFor(() => IsSettled(retired));
                if (_run % 5 == 0) Peak();
            }

            bool IsSettled(int retired)
            {
                if (streamer.GetState(retired) != WorldChunkState.Unloaded) return false;
                for (int index = 0; index < count; index++)
                    if (streamer.GetState(index) is not (WorldChunkState.Ready or WorldChunkState.Unloaded)) return false;
                return true;
            }

            void Mark(string stage)
            {
                Peak();
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                var objects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                int ready = Enumerable.Range(0, count).Count(i => streamer.Catalog != null && streamer.GetState(i) == WorldChunkState.Ready);
                // 의존 배열은 레지던시 집합이며, 실제 pin lease 수는 별도로 기록한다.
                int dependencyEdges = Enumerable.Range(0, count).Where(i => streamer.Catalog != null && streamer.GetState(i) == WorldChunkState.Ready)
                    .Sum(i => catalog.chunks[i].dependencies.Length);
                long terrainBytes = Terrain.activeTerrains.Sum(terrain => Profiler.GetRuntimeMemorySizeLong(terrain.terrainData));
                long navBytes = catalog?.bake.surfaces.Sum(surface => Profiler.GetRuntimeMemorySizeLong(surface.data)) ?? 0;
                long proxyBytes = catalog?.chunks.Sum(chunk => Profiler.GetRuntimeMemorySizeLong(chunk.proxy)) ?? 0;
                File.AppendAllText(log, $"{stage} | active={SceneManager.sceneCount}; ready={ready}; scenes={streamer.OwnedSceneCount}; assets={streamer.OwnedAssetCount}; pins={streamer.PinCount}; dependencyEdges={dependencyEdges}; objects={objects.Length}; unity={Profiler.GetTotalAllocatedMemoryLong()}; mono={Profiler.GetMonoUsedSizeLong()}; managed={GC.GetTotalMemory(false)}; workingSet={process.WorkingSet64}; private={process.PrivateMemorySize64}; terrain={objects.Count(o => o is TerrainData)}; nav={objects.Count(o => o is NavMeshData)}; mesh={objects.Count(o => o is Mesh)}; ownedTerrain={terrainBytes}; catalogNav={navBytes}; catalogProxy={proxyBytes}\n");
            }

            void Peak()
            {
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                _tally++;
                _unityPeak = Math.Max(_unityPeak, Profiler.GetTotalAllocatedMemoryLong());
                _workingSetPeak = Math.Max(_workingSetPeak, process.WorkingSet64);
                _privatePeak = Math.Max(_privatePeak, process.PrivateMemorySize64);
                _nativePeak = Math.Max(_nativePeak, Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Length);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void ConfigureMemoryCapture()
        {
            string output = Environment.GetEnvironmentVariable(MemoryOutputVariable);
            if (string.IsNullOrWhiteSpace(output)) return;
            _memoryOutput = Path.GetFullPath(output);
            Directory.CreateDirectory(_memoryOutput);
            foreach (string phase in new[] { "warm", "collected", "closed" })
                if (File.Exists(Path.Combine(_memoryOutput, phase + ".snap")))
                    throw new IOException("새 메모리 스냅샷 폴더가 필요합니다: " + _memoryOutput);
        }

        private IEnumerator CaptureMemory(string phase)
        {
            var path = Path.Combine(_memoryOutput, phase + ".snap");
            bool? captured = null;
            const Unity.Profiling.Memory.CaptureFlags flags = Unity.Profiling.Memory.CaptureFlags.ManagedObjects
                | Unity.Profiling.Memory.CaptureFlags.NativeObjects | Unity.Profiling.Memory.CaptureFlags.NativeAllocations;
            Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(path, (_, success) => captured = success, flags);
            float deadline = Time.realtimeSinceStartup + 120f;
            yield return new WaitUntil(() => captured.HasValue || Time.realtimeSinceStartup >= deadline);
            if (!captured.HasValue) throw new TimeoutException("메모리 스냅샷 생성 시간 초과: " + path);
            if (captured != true || !File.Exists(path) || new FileInfo(path).Length == 0)
                throw new IOException("로컬 메모리 스냅샷 생성 실패: " + path);
        }
#endif

        private void OnPreparing(Scene scene) => RecordEvent("preparing", scene);
        private void OnVisibility(Scene scene, bool ready) => RecordEvent(ready ? "ready" : "retiring", scene);

        private void RecordEvent(string kind, Scene scene)
        {
            _events.Add((kind, scene.name, Time.realtimeSinceStartupAsDouble * 1000, Time.frameCount, _run));
        }

        private IEnumerator CaptureP08()
        {
            var bake = streamer.Catalog.bake;
            var bridge = bake.crossings.First(crossing => !string.IsNullOrEmpty(crossing.bridgeId));
            var cave = bake.crossings.First(crossing => !string.IsNullOrEmpty(crossing.portalId));
            Vector3 target = (bridge.from + bridge.to) * .5f;
            string log = Path.Combine(_output, "T26.log");
            var proxyRenderers = streamer.GetComponentsInChildren<MeshRenderer>(true);
            int proxyInitial = proxyRenderers.Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy);
            yield return Capture("proxy", target, new Vector3(-54, 42, -65));
            string error = null;
            yield return streamer.PrepareDestination(target, message => error = message);
            if (error != null) throw new InvalidOperationException(error);
            var tracker = new GameObject("P09 렌더 기준점");
            tracker.transform.position = target;
            streamer.TrackPlayer(tracker.transform);
            yield return Capture("bridge-water", target, new Vector3(-54, 42, -65));
            yield return Capture("cave-portal", cave.to, cave.from - cave.to + Vector3.up * 1.4f);
            var waterfalls = FindObjectsByType<WorldWaterfallEffects>(FindObjectsSortMode.None);
            foreach (var effects in waterfalls)
            {
                var landing = effects.emitters.FirstOrDefault(emitter => emitter.landing != null)?.landing;
                if (landing == null) continue;
                yield return Capture("waterfall", landing.position, new Vector3(-14, 6, -18));
                break;
            }
            var terrains = Terrain.activeTerrains;
            int trees = terrains.Sum(terrain => terrain.terrainData.treeInstanceCount);
            int detailTiles = terrains.Count(terrain => terrain.terrainData.detailPrototypes.Length > 0);
            int holes = 0;
            foreach (var terrain in terrains)
            {
                var surface = terrain.terrainData.GetHoles(0, 0, 512, 512);
                foreach (bool covered in surface) if (!covered) holes++;
            }
            int shaderErrors = FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(renderer => renderer.sharedMaterials)
                .Concat(terrains.Select(terrain => terrain.materialTemplate))
                .Count(material => material == null || material.shader == null || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader");
            File.WriteAllText(log, $"pipeline={GraphicsSettings.currentRenderPipeline}; camera={Screen.width}x{Screen.height}; proxy={proxyInitial}->{proxyRenderers.Count(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)}; terrains={terrains.Length}; holeCells={holes}; trees={trees}; detailTiles={detailTiles}; waterfalls={waterfalls.Length}; shaderErrors={shaderErrors}\n");
            Destroy(tracker);
            streamer.TrackPlayer(null);

            IEnumerator Capture(string name, Vector3 point, Vector3 offset)
            {
                view.transform.SetPositionAndRotation(point + offset, Quaternion.LookRotation(-offset));
                yield return new WaitForSecondsRealtime(.45f);
                const int width = 1280, height = 720;
                var targetTexture = RenderTexture.GetTemporary(width, height, 24);
                var oldTarget = RenderTexture.active;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    var request = new RenderPipeline.StandardRequest { destination = targetTexture };
                    if (!RenderPipeline.SupportsRenderRequest(view, request))
                        throw new InvalidOperationException("현재 렌더 파이프라인에서 카메라 캡처를 지원하지 않습니다.");
                    RenderPipeline.SubmitRenderRequest(view, request);
                    RenderTexture.active = targetTexture;
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    File.WriteAllBytes(Path.Combine(_output, "T26-" + name + ".png"), texture.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = oldTarget;
                    RenderTexture.ReleaseTemporary(targetTexture);
                    Destroy(texture);
                }
            }
        }

        private IEnumerator VerifyStreamFailures(Vector3 near, Vector3 far, string log)
        {
            int index = streamer.FindChunk(near);
            string chunkId = streamer.Catalog.chunks[index].id;
            if (streamer.GetState(index) != WorldChunkState.Unloaded || !streamer.CanTraverse(far, far))
                throw new InvalidOperationException("P09 실패 시험의 시작 청크가 다릅니다.");
            streamer.DiagnosticDelay = .25f;
            streamer.DiagnosticFailure = id => id == chunkId;
            string failure = null;
            yield return streamer.PrepareDestination(near, message => failure = message);
            if (failure == null || streamer.CanTraverse(near, near) || !streamer.CanTraverse(far, far))
                throw new InvalidOperationException("청크 실패가 출발지에 영향을 주었습니다.");
            streamer.DiagnosticFailure = null;
            bool called = false;
            var request = streamer.PrepareDestination(near, _ => called = true);
            try
            {
                if (!request.MoveNext()) throw new InvalidOperationException("철회 요청이 즉시 종료됐습니다.");
                yield return WaitFor(() => streamer.GetState(index) == WorldChunkState.Loading);
            }
            finally { (request as IDisposable)?.Dispose(); }
            streamer.DiagnosticDelay = 0;
            yield return WaitFor(() => streamer.GetState(index) == WorldChunkState.Unloaded);
            if (called || !streamer.CanTraverse(far, far))
                throw new InvalidOperationException("철회된 요청 뒤 콜백 또는 출발지 상태 오류");
            File.AppendAllText(log, "load delay, injected failure, withdrawal and source readiness | PASS\n");
        }

        private IEnumerator VerifyAgent(Vector3 near, string log)
        {
            string error = null;
            yield return streamer.PrepareDestination(near, value => error = value);
            if (error != null) throw new InvalidOperationException(error);
            var crossing = streamer.Catalog.bake.crossings[0];
            var root = new GameObject("P09 NavMeshAgent");
            root.transform.position = crossing.from;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = streamer.Catalog.bake.agentTypeId;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(crossing.from - Vector3.right * 3, out var start, 2, filter) ||
                !NavMesh.SamplePosition(crossing.to + Vector3.right * 3, out var finish, 2, filter) || !agent.Warp(start.position))
                throw new InvalidOperationException("Agent의 양끝을 찾을 수 없습니다.");
            using (streamer.Pin(start.position))
            using (streamer.Pin(finish.position))
            {
                agent.speed = 8;
                agent.autoTraverseOffMeshLink = true;
                if (!agent.SetDestination(finish.position)) throw new InvalidOperationException("Agent 경로 요청 실패");
                yield return WaitFor(() => agent.isOnOffMeshLink);
                if (!streamer.CanTraverse(start.position, start.position) ||
                    !streamer.CanTraverse(finish.position, finish.position) || streamer.PinCount != 2)
                    throw new InvalidOperationException("Agent 링크 점유 및 도착 청크 핀 오류");
                yield return WaitFor(() => !agent.pathPending && Vector3.Distance(root.transform.position, finish.position) < .5f);
                File.AppendAllText(log, $"agent crossed | {start.position} -> {root.transform.position} | pins={streamer.PinCount}\n");
            }
            agent.autoTraverseOffMeshLink = false;
            if (streamer.PinCount != 0 || !agent.SetDestination(start.position))
                throw new InvalidOperationException("Agent 도착 뒤 pin 회수 또는 복귀 경로 실패");
            using (streamer.Pin(start.position))
            using (streamer.Pin(finish.position))
            {
                yield return WaitFor(() => agent.isOnOffMeshLink);
                agent.ResetPath();
            }
            Destroy(root);
            if (streamer.PinCount != 0) throw new InvalidOperationException("Agent 취소 후 pin 회수 실패");
            File.AppendAllText(log, "agent complete and cancel | pins=0 | explicit leases\n");
        }

        private void LateUpdate()
        {
            if (_phase == null || streamer.Catalog == null) return;
            var now = Time.realtimeSinceStartupAsDouble;
            FrameTimingManager.CaptureFrameTimings();
            var timing = FrameTimingManager.GetLatestTimings(1, _timings) > 0 ? _timings[0] : default;
            int frame = Time.frameCount;
            _frames.Add((_phase, _run, frame, Time.unscaledDeltaTime * 1000, timing.cpuFrameTime,
                timing.cpuMainThreadFrameTime, timing.gpuFrameTime, Read(_allocations), Read(_coordinator), Read(_activation)));
            for (int i = 0; i < _previous.Length; i++)
            {
                var next = streamer.GetState(i);
                if (_previous[i] == next) continue;
                _transitions.Add((_phase, _run, i, _previous[i], next, (now - _changed[i]) * 1000, frame));
                _previous[i] = next;
                _changed[i] = now;
            }
        }

        private static IEnumerator WaitFor(Func<bool> predicate)
        {
            double end = Time.realtimeSinceStartupAsDouble + 45;
            while (!predicate() && Time.realtimeSinceStartupAsDouble < end) yield return null;
            if (!predicate()) throw new TimeoutException("청크 또는 Agent 대기 시간 초과");
        }

        private static IEnumerator Collect()
        {
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private static long Read(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue : -1;

        private void OnDestroy()
        {
            _allocations.Dispose();
            _coordinator.Dispose();
            _activation.Dispose();
        }
    }
}

