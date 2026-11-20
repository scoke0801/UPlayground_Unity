using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;
using UPlayGround.Components;

namespace UPlayGround.WorldBuilder.Tests
{
    public sealed class WorldChunkStreamingTests
    {
        private const string World = "a808a808a808a808a808a808a808a808";
        private const string Scope = "WorldBuilder.P08.Previous";

        [UnityTest]
        public IEnumerator T18_PublishedChunksDelayRejectRetryAndKeepBoundariesClosed()
        {
            var published = WorldBakePipeline.ReadPublished(World);
            Assert.That(published, Is.Not.Null);
            var bake = WorldBakePipeline.ReadRevision(World, published.current, reopen: true);
            var catalog = AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(
                System.IO.Path.GetDirectoryName(bake.scenePath).Replace('\\', '/') + "/Chunks.asset");
            var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(WorldBakePipeline.PublishedPath(World));
            Assert.That(catalog.chunks, Has.Length.EqualTo(16));
            Assert.That(catalog.tileSize, Is.EqualTo(512));
            P06TestSceneScope.Capture(Scope);
            SessionState.SetString(Scope + ".Start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().FullName != "UPlayGround.Tool.Editor.Build.BuildManagerWindow") continue;
                SessionState.SetString(Scope + ".Window", window.GetType().AssemblyQualifiedName);
                window.Close();
            }
            string testPath = WorldBakePipeline.WorldRoot(World) + "/P08StreamingTest.unity";
            var active = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(EditorSceneManager.SaveScene(active, testPath), Is.True);
            SessionState.SetString(Scope + ".Test", testPath);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(testPath);
            bool domainReload = !EditorSettings.enterPlayModeOptionsEnabled ||
                (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
            yield return new EnterPlayMode(domainReload);

            var root = new GameObject("P08 청크 검증");
            root.SetActive(false);
            var nav = root.AddComponent<WorldBakedNavigation>();
            var stream = root.AddComponent<WorldChunkStreamer>();
            stream.Configure(manifest, Array.Empty<WorldChunkCatalog>(), nav, null);
            root.SetActive(true);
            string catalogError = null;
            yield return stream.WaitForCatalog(error => catalogError = error);
            Assert.That(catalogError, Is.Null);
            Assert.That(stream.Catalog.bake.buildId, Is.EqualTo(published.current));
            Assert.That(stream.OwnedAssetCount, Is.EqualTo(1));

            Vector3 start = catalog.chunks[0].bounds.center;
            Vector3 far = catalog.chunks[catalog.chunks.Length - 1].bounds.center;
            stream.DiagnosticDelay = .35f;
            bool failedOnce = false;
            stream.DiagnosticFailure = id => !failedOnce && id == catalog.chunks[0].id && (failedOnce = true);
            string error = null;
            bool completed = false;
            root.GetComponent<WorldChunkStreamer>().StartCoroutine(stream.PrepareDestination(start, e => { error = e; completed = true; }));
            yield return null;
            Assert.That(stream.CanTraverse(start, start), Is.False);
            Assert.That(nav.IsLinkReady(0), Is.False);
            yield return Until(() => completed, "청크 실패 통보");
            Assert.That(error, Is.Not.Null);
            Assert.That(stream.CanTraverse(start, start), Is.False);

            stream.DiagnosticFailure = null;
            error = "대기";
            yield return stream.PrepareDestination(start, e => error = e);
            Assert.That(error, Is.Null);
            var tracking = new GameObject("P08 위치 추적").transform;
            tracking.position = start;
            stream.TrackPlayer(tracking);
            Assert.That(stream.CanTraverse(start, start), Is.True);
            Assert.That(stream.CanTraverse(start, far), Is.False);

            var crossing = catalog.bake.crossings[0];
            var filter = new NavMeshQueryFilter { agentTypeID = catalog.bake.agentTypeId, areaMask = NavMesh.AllAreas };
            var path = new NavMeshPath();
            Assert.That(nav.IsLinkReady(0), Is.True);
            Assert.That(NavMesh.CalculatePath(crossing.from, crossing.to, filter, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));

            var portals = catalog.bake.crossings.Where(c => !string.IsNullOrEmpty(c.portalId)).OrderBy(c => c.from.x).ToArray();
            foreach (var portal in portals)
            {
                Assert.That(NavMesh.CalculatePath(portal.from, portal.to, filter, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            }
            var west = portals[0].from - Vector3.right * .5f;
            var east = portals[1].from + Vector3.right * .5f;
            var middle = (west + east) * .5f;
            Assert.That(FootstepWaterSurface.TryResolveSurface(new Vector3(middle.x, 1.4f, middle.z), out _, out _), Is.False);
            Assert.That(FootstepWaterSurface.TryResolveSurface(new Vector3(middle.x, 8, middle.z), out _, out _), Is.True);
            var layout = AssetDatabase.LoadAssetAtPath<WorldNavigationLayout>("Assets/10.Datas/WorldBuilder/P08Validation/NavigationLayout.asset");
            var driver = new WorldCaveTraversalTests.Driver();
            var motor = WorldCaveTraversalTests.CreateMotor(layout.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true), driver);
            var motors = new List<KinematicCharacterMotor> { motor };
            var movers = new List<PhysicsMover>();
            CheckKcc(crossing.from - Vector3.right * .25f, new[] { crossing.to + Vector3.right * .25f },
                c => c.GetComponentInParent<WorldBridgeAccessGate>() != null);
            CheckKcc(west, new[] { middle, east }, c => c.GetComponentInParent<WorldCaveModule>() != null);
            UnityEngine.Object.Destroy(motor.gameObject);
            Debug.Log("[P08 T18] AI 링크와 프로젝트 KCC의 교량·동굴 지지 횡단, 같은 XZ 지상 수계와 동굴 건조 확인");

            // 먼 목적지의 로드 핸들을 얻은 뒤 이동 요청을 철회한다.
            var request = stream.PrepareDestination(far, _ => Assert.Fail("철회된 이동의 콜백이 호출됐습니다."));
            Assert.That(request.MoveNext(), Is.True);
            yield return Until(() => stream.GetState(catalog.chunks.Length - 1) == WorldChunkState.Loading, "목적지 로드 시작");
            (request as IDisposable)?.Dispose();
            yield return Until(() => stream.GetState(catalog.chunks.Length - 1) == WorldChunkState.Unloaded, "철회한 씬 회수");
            Assert.That(tracking.position, Is.EqualTo(start));

            error = "대기";
            yield return stream.PrepareDestination(far, e => error = e);
            Assert.That(error, Is.Null);
            tracking.position = far;
            Assert.That(Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path)
                .Where(p => !string.IsNullOrEmpty(p)).Distinct().Count(), Is.EqualTo(SceneManager.sceneCount));
            var instances = UnityEngine.Object.FindObjectsByType<WorldBakedIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(instances.Select(i => i.id).Distinct().Count(), Is.EqualTo(instances.Length));
            Debug.Log($"[P08 T18] 지연, 초기화 실패, 재시도, 철회, AI 교량 경로 · 활성 씬 {SceneManager.sceneCount} · 소유 {stream.OwnedSceneCount}");
            stream.DiagnosticDelay = 0;
            stream.DiagnosticMinimumResidency = 0;
            using (stream.Pin(far))
            {
                yield return stream.PrepareDestination(start, e => error = e);
                Assert.That(error, Is.Null);
                tracking.position = start;
                yield return null;
                Assert.That(stream.GetState(catalog.chunks.Length - 1), Is.EqualTo(WorldChunkState.Ready));
                Assert.That(stream.PinCount, Is.EqualTo(1));
            }
            yield return Until(() => stream.GetState(catalog.chunks.Length - 1) == WorldChunkState.Unloaded, "pin 해제 후 회수");
            long memoryStart = 0;
            long managedStart = 0;
            long geometryStart = 0;
            int steadyHandles = 0;
            int steadyScenes = 0;
            for (int i = 0; i < 50; i++)
            {
                yield return MoveAndRetire(start, catalog.chunks.Length - 1);
                yield return MoveAndRetire(far, 0);
                Assert.That(nav.IsLinkReady(0), Is.False);
                Assert.That(FootstepWaterSurface.TryResolveSurface(new Vector3(middle.x, 8, middle.z), out _, out _), Is.False);
                if (i == 4)
                {
                    yield return Resources.UnloadUnusedAssets();
                    GC.Collect();
                    memoryStart = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                    managedStart = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                    geometryStart = OwnedGeometrySize();
                    steadyHandles = stream.OwnedSceneCount;
                    steadyScenes = SceneManager.sceneCount;
                }
                if (i % 10 == 9)
                    Debug.Log($"[P08 T19] {i + 1}/50 · 씬 {SceneManager.sceneCount} · 소유 {stream.OwnedSceneCount} · pin {stream.PinCount} · bytes {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()}");
            }
            Assert.That(stream.OwnedSceneCount, Is.EqualTo(steadyHandles));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(steadyScenes));
            long rawEnding = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            long memoryEnd = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            long managedEnd = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            long geometryEnd = OwnedGeometrySize();
            Assert.That(geometryEnd, Is.EqualTo(geometryStart));
            yield return stream.CloseWorld();
            Assert.That(stream.OwnedSceneCount, Is.Zero);
            Assert.That(stream.OwnedAssetCount, Is.Zero);
            Assert.That(stream.PinCount, Is.Zero);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Debug.Log($"[P08 T19] 미회수 원시값 {rawEnding} bytes · 회수 후 warm {memoryStart} → {memoryEnd} bytes " +
                $"· managed {managedStart} → {managedEnd} bytes · 소유 Terrain/NavMesh/proxy {geometryStart} → {geometryEnd} bytes " +
                $"· 종료 씬 {SceneManager.sceneCount} · 소유 {stream.OwnedSceneCount}");
            yield return new ExitPlayMode();

            IEnumerator MoveAndRetire(Vector3 position, int retire)
            {
                string failure = "대기";
                yield return stream.PrepareDestination(position, e => failure = e);
                Assert.That(failure, Is.Null);
                tracking.position = position;
                yield return Until(() => stream.GetState(retire) == WorldChunkState.Unloaded, "반대쪽 끝 청크 회수");
                yield return Until(() => Enumerable.Range(0, catalog.chunks.Length).All(i =>
                    stream.GetState(i) is WorldChunkState.Ready or WorldChunkState.Unloaded), "청크 작업 안정화");
            }

            long OwnedGeometrySize()
            {
                long size = 0;
                foreach (var chunk in UnityEngine.Object.FindObjectsByType<WorldChunkContent>(FindObjectsSortMode.None))
                    size += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(chunk.terrain.terrainData);
                foreach (var surface in catalog.bake.surfaces)
                    size += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(surface.data);
                foreach (var chunk in catalog.chunks)
                    size += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(chunk.proxy);
                return size;
            }

            void CheckKcc(Vector3 from, Vector3[] targets, Predicate<Collider> expectedSupport)
            {
                motor.SetPositionAndRotation(from + Vector3.up * .3f, Quaternion.identity);
                motor.BaseVelocity = Vector3.zero;
                driver.Destination = from;
                Physics.SyncTransforms();
                for (int i = 0; i < 25; i++) KinematicCharacterSystem.Simulate(.02f, motors, movers);
                bool supported = false;
                foreach (var target in targets)
                {
                    driver.Destination = target;
                    int budget = 1600;
                    while (HorizontalDistance() > .017f && budget-- > 0)
                    {
                        KinematicCharacterSystem.Simulate(.02f, motors, movers);
                        var support = motor.GroundingStatus.GroundCollider;
                        if (motor.GroundingStatus.IsStableOnGround && support != null && expectedSupport(support))
                            supported = true;
                    }
                    Assert.That(HorizontalDistance(), Is.LessThanOrEqualTo(.017f), target.ToString());
                    Assert.That(motor.GroundingStatus.IsStableOnGround, Is.True, target.ToString());

                    float HorizontalDistance() => Vector3.ProjectOnPlane(motor.TransientPosition - target, Vector3.up).sqrMagnitude;
                }
                Assert.That(supported, Is.True, from.ToString());
            }
        }

        private static IEnumerator Until(Func<bool> ready, string step)
        {
            float deadline = Time.realtimeSinceStartup + 35;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(ready(), Is.True, step);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (!P06TestSceneScope.HasCaptured(Scope)) yield break;
            var start = SessionState.GetString(Scope + ".Start", "");
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(start);
            P06TestSceneScope.Restore(Scope);
            string path = SessionState.GetString(Scope + ".Test", "");
            if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
            SessionState.EraseString(Scope + ".Start");
            SessionState.EraseString(Scope + ".Test");
            string window = SessionState.GetString(Scope + ".Window", "");
            SessionState.EraseString(Scope + ".Window");
            if (!string.IsNullOrEmpty(window)) EditorWindow.GetWindow(Type.GetType(window, true));
        }
    }
}
