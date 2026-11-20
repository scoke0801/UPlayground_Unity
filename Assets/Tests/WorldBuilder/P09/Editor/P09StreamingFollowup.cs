using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Runtime;
using UPlayGround.Manager.World;
using Game = UPlayGround.Manager;
using EngineScenes = UnityEngine.SceneManagement.SceneManager;

namespace UPlayGround.Editor
{
    // P09 인수 검증에서만 실행하는 교차 씬·저장·생산 pin 재현.
    internal static class P09StreamingFollowup
    {
        private const string Output = "output/WorldBuilder/P09/followup";
        private const string SceneName = "P08Bootstrap";
        private const string WorldId = "a808a808a808a808a808a808a808a808";

        public static void Start()
        {
            if (!Application.isPlaying || EngineScenes.GetActiveScene().name != "Title")
                throw new InvalidOperationException("Title의 Play Mode에서 실행하세요.");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/streaming.log", "");
            Game.GameManager.Instance.StartCoroutine(Verify());
        }

        private static IEnumerator Verify()
        {
            var manager = Game.SceneManager.Instance;
            var save = Game.SaveManager.Instance;
            var saveField = typeof(Game.SaveManager).GetField("_saveFolder", BindingFlags.Instance | BindingFlags.NonPublic);
            string saves = Path.GetFullPath(Output + "/test-saves-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(saves);
            // Play 종료까지 자동 저장도 검증 폴더에 남긴다.
            saveField.SetValue(save, saves);
            string failure = null;
            string completed = null;
            NavMeshAgent agent = null;
            bool rejectEntryChunk = false;
            void OnFailure(string scene, string reason) { Log("failure " + scene + " · " + reason); if (scene == SceneName) failure = reason; }
            void OnComplete(string scene) { Log("complete " + scene); completed = scene; }
            void InjectAgent(Scene scene, LoadSceneMode mode)
            {
                if (rejectEntryChunk && scene.name == SceneName)
                {
                    var entry = scene.GetRootGameObjects().Select(root => root.GetComponent<WorldChunkStreamer>())
                        .Single(component => component != null);
                    entry.DiagnosticFailure = id => id == "tile_0_0";
                    return;
                }
                var chunk = scene.GetRootGameObjects().Select(r => r.GetComponent<WorldChunkContent>())
                    .FirstOrDefault(c => c != null && c.worldGuid == WorldId && c.chunkId == "tile_0_0");
                if (chunk == null) return;
                var root = new GameObject("P09 링크 검증 NavMeshAgent");
                root.transform.SetParent(chunk.content.transform, false);
                root.transform.position = new Vector3(400, 11, 222);
                agent = root.AddComponent<NavMeshAgent>();
                agent.autoTraverseOffMeshLink = false;
                agent.speed = 18;
                agent.acceleration = 48;
            }

            manager.OnLoadFailed += OnFailure;
            manager.OnLoadComplete += OnComplete;
            EngineScenes.sceneLoaded += InjectAgent;
            try
            {
                Require(manager.TryLoadScene(SceneName, "p09_missing_arrival_anchor"), "첫 진입 요청 수락");
                yield return Until(() => failure != null && completed == "Title" && !manager.IsLoading, "실패 통보 뒤 Title 복귀");
                Require(failure.Contains("도착 지점") && WorldBuilderRuntimeBridge.Active == null, "존재하지 않는 앵커 거절과 월드 해제");
                Log("PASS cross-scene missing arrival");

                completed = null;
                failure = null;
                rejectEntryChunk = true;
                Require(manager.TryLoadScene(SceneName, null), "청크 실패를 주입한 첫 진입 요청 수락");
                yield return Until(() => failure != null && completed == "Title" && !manager.IsLoading, "청크 실패 통보 뒤 Title 복귀");
                rejectEntryChunk = false;
                Require(failure.Contains("검증용 청크 초기화 실패") && WorldBuilderRuntimeBridge.Active == null, "목적지 청크 실패와 월드 해제");
                Log("PASS cross-scene first entry chunk failure");

                completed = null;
                manager.LoadScene(SceneName);
                yield return Until(() => completed == SceneName && WorldBuilderRuntimeBridge.Active != null
                    && WorldBuilderRuntimeBridge.Active.IsPrepared && !manager.IsLoading, "정상 월드 진입");
                var bridge = WorldBuilderRuntimeBridge.Active;
                var streamer = bridge.GetComponent<WorldChunkStreamer>();
                var actor = UnityEngine.Object.FindObjectsByType<UPlayGround.PlayerActor>(FindObjectsSortMode.None).Single();
                Log("player spawned " + actor.transform.position);
                Vector3 requested = new Vector3(640, 11, 278);
                completed = null;
                manager.LoadScene(SceneName, requested);
                yield return Until(() => completed != null && !manager.IsLoading, "게임 이동 경로로 저장 지점 이동");
                Vector3 position = actor.transform.position;
                Require(Vector2.Distance(new Vector2(position.x, position.z), new Vector2(requested.x, requested.z)) < 2, "저장 지점 도착");
                var identity = UnityEngine.Object.FindObjectsByType<WorldBakedIdentity>(FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.gameObject.activeSelf
                        && streamer.FindChunk(item.transform.position) == streamer.FindChunk(requested)
                        && Vector3.Distance(item.transform.position, new Vector3(528, 12, 250)) > 40);
                Require(identity != null, "저장할 동쪽 청크 오브젝트");
                string consumed = identity.id;
                identity.gameObject.SetActive(false);
                var result = save.SaveGameDetailed(0);
                Require(result.Succeeded && File.Exists(result.FilePath), "격리 슬롯 파일 생성");
                completed = null;
                manager.LoadSceneDirect("Title");
                yield return Until(() => completed == "Title" && !manager.IsLoading, "저장 뒤 원래 씬 퇴장");
                completed = null;
                Require(save.LoadGameToScene(0), "타이틀에서 저장 슬롯 읽기");
                yield return Until(() => completed == SceneName && WorldBuilderRuntimeBridge.Active != null
                    && WorldBuilderRuntimeBridge.Active.IsPrepared && !manager.IsLoading, "저장 월드 재진입");
                actor = UnityEngine.Object.FindObjectsByType<UPlayGround.PlayerActor>(FindObjectsSortMode.None).Single();
                Require(Vector3.Distance(actor.transform.position, position) < 2, "저장 좌표 복원 " + position + " → " + actor.transform.position);
                var restored = UnityEngine.Object.FindObjectsByType<WorldBakedIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .SingleOrDefault(item => item.id == consumed);
                Require(restored != null && !restored.gameObject.activeSelf, "저장한 오브젝트 소비 상태 복원 " + consumed);
                Log("PASS save reentry; consumed identity exercised");

                streamer = WorldBuilderRuntimeBridge.Active.GetComponent<WorldChunkStreamer>();
                yield return Until(() => agent != null && agent.isOnNavMesh, "실제 발행 씬의 두 지상 청크·NavMesh");
                var start = new Vector3(400, 11, 222);
                var end = new Vector3(640, 11, 278);
                int from = streamer.FindChunk(start), to = streamer.FindChunk(end);
                var pins = (IDictionary)typeof(WorldChunkStreamer).GetField("_pins", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(streamer);
                Require(from >= 0 && to >= 0 && from != to, "서로 다른 링크 양끝 청크");
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    Require(NavMesh.SamplePosition(start, out var source, 3, NavMesh.AllAreas)
                        && agent.Warp(source.position) && agent.SetDestination(end), "실제 NavMesh 경로 요청");
                    yield return Until(() => agent.isOnOffMeshLink && pins.Contains(from) && pins.Contains(to), "생산 브리지의 링크 pin 획득");
                    if (attempt == 1)
                    {
                        streamer.DiagnosticMinimumResidency = 0;
                        streamer.TrackPlayer(null);
                    }
                    yield return new WaitForSecondsRealtime(.55f);
                    Require(agent.isOnOffMeshLink && streamer.GetState(from) == WorldChunkState.Ready
                        && streamer.GetState(to) == WorldChunkState.Ready && pins.Contains(from) && pins.Contains(to), "링크 위에서 양쪽 청크와 pin 유지");
                    Log("link " + attempt + " pins=" + streamer.PinCount + " begin=" + agent.currentOffMeshLinkData.startPos
                        + " end=" + agent.currentOffMeshLinkData.endPos);

                    if (attempt == 0)
                    {
                        agent.CompleteOffMeshLink();
                        yield return Until(() => !agent.isOnOffMeshLink, "링크 완료");
                        agent.ResetPath();
                    }
                    else agent.enabled = false;

                    yield return Until(() => streamer.PinCount == 0, attempt == 0 ? "완료 뒤 pin 해제" : "취소 뒤 pin 해제");
                    Log("PASS " + (attempt == 0 ? "completion" : "cancellation") + " pin count=0");
                }
                yield return Until(() => streamer.GetState(from) == WorldChunkState.Unloaded
                    && streamer.GetState(to) == WorldChunkState.Unloaded, "취소 뒤 링크 양끝 청크 회수");
                streamer.TrackPlayer(actor.transform);
                Log("COMPLETE");
            }
            finally
            {
                manager.OnLoadFailed -= OnFailure;
                manager.OnLoadComplete -= OnComplete;
                EngineScenes.sceneLoaded -= InjectAgent;
            }
        }

        private static IEnumerator Until(Func<bool> predicate, string step)
        {
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    Log("FAIL timeout " + step);
                    throw new TimeoutException(step);
                }
                yield return null;
            }
            Log("OK " + step);
        }

        private static void Require(bool value, string step)
        {
            if (value) { Log("OK " + step); return; }
            Log("FAIL " + step);
            throw new InvalidOperationException(step);
        }

        private static void Log(string message) => File.AppendAllText(Output + "/streaming.log", message + "\n");
    }
}
