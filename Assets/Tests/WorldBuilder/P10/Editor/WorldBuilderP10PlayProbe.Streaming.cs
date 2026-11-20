using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UPlayGround.Manager.World;
using UPlayGround.MovementController;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;
using Game = UPlayGround.Manager;
using EngineScenes = UnityEngine.SceneManagement.SceneManager;

namespace UPlayGround.Editor
{
    public static partial class WorldBuilderP10PlayProbe
    {
        private const string StreamingOutput = "output/WorldBuilder/P10/Streaming";
        private const string Bootstrap = "Assets/WorldBuilder/Worlds/" + World + "/P10Bootstrap.unity";

        /// <summary>Title에서 격리 저장으로 발행 청크와 실제 플레이어 입력을 검사한다.</summary>
        public static void StartStreaming(bool fullLoop = false)
        {
            Require(Application.isPlaying && EngineScenes.GetActiveScene().name == "Title" &&
                !Game.SceneManager.Instance.IsLoading, "Title 로드 완료 뒤 검증을 시작하세요.");
            Directory.CreateDirectory(StreamingOutput);
            File.WriteAllText(StreamingOutput + "/drive.log", string.Empty);
            Game.GameManager.Instance.StartCoroutine(RunWithIsolatedInput(DrivePublished(fullLoop)));
        }

        private static IEnumerator RunWithIsolatedInput(IEnumerator probe)
        {
            var physicalPads = Gamepad.all.Where(pad => pad.native && pad.enabled).ToArray();
            var routines = new Stack<IEnumerator>();
            routines.Push(probe);
            try
            {
                foreach (var pad in physicalPads)
                {
                    InputSystem.DisableDevice(pad);
                    StreamingLog($"입력 격리 {pad.name} {pad.deviceId}");
                }

                while (routines.Count > 0)
                {
                    var routine = routines.Peek();
                    object next;
                    try
                    {
                        if (!routine.MoveNext())
                        {
                            (routines.Pop() as IDisposable)?.Dispose();
                            continue;
                        }
                        next = routine.Current;
                    }
                    catch (Exception error)
                    {
                        StreamingLog("FAIL " + error);
                        yield break;
                    }
                    if (next is IEnumerator nested) routines.Push(nested);
                    else yield return next;
                }
            }
            finally
            {
                while (routines.Count > 0) (routines.Pop() as IDisposable)?.Dispose();
                foreach (var pad in physicalPads)
                    if (pad.added) InputSystem.EnableDevice(pad);
            }
        }

        private static IEnumerator DrivePublished(bool fullLoop)
        {
            var definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(Definition);
            var saveFolder = Path.GetFullPath(Path.Combine(StreamingOutput, "isolated-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(saveFolder);
            var saveField = typeof(Game.SaveManager).GetField("_saveFolder", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(saveField != null, "저장 격리 필드");
            saveField.SetValue(Game.SaveManager.Instance, saveFolder);
            StreamingLog("ISOLATED " + saveFolder);

            void ObserveChunks(Scene scene, LoadSceneMode mode)
            {
                if (scene.path != Bootstrap) return;
                var root = scene.GetRootGameObjects().Select(item => item.GetComponent<WorldChunkStreamer>())
                    .Single(item => item != null);
                var nav = root.GetComponent<WorldBakedNavigation>();
                root.ChunkPreparing += chunk =>
                {
                    var content = chunk.GetRootGameObjects().Select(item => item.GetComponent<WorldChunkContent>())
                        .Single(item => item != null);
                    int index = Array.IndexOf(content.crossingIndices, 0);
                    if (index >= 0)
                        StreamingLog($"BRIDGE PREPARE {chunk.name} link={nav.IsLinkReady(0)} gate={content.gates[index].CanEnter}");
                };
                root.ChunkVisibilityChanged += (chunk, visible) => StreamingLog($"CHUNK {chunk.name} visible={visible}");
            }

            var sceneManager = Game.SceneManager.Instance;
            EngineScenes.sceneLoaded += ObserveChunks;
            try
            {
                Require(sceneManager.TryLoadScene(Path.GetFileNameWithoutExtension(Bootstrap), null), "C 씬 진입 요청");
                yield return WaitFor(() => EngineScenes.GetActiveScene().path == Bootstrap && !sceneManager.IsLoading &&
                    WorldBuilderRuntimeBridge.Active != null && WorldBuilderRuntimeBridge.Active.IsPrepared, "청크 진입");
            }
            finally { EngineScenes.sceneLoaded -= ObserveChunks; }
            var bridge = WorldBuilderRuntimeBridge.Active;
            var streamer = bridge.GetComponent<WorldChunkStreamer>();
            var navigation = bridge.GetComponent<WorldBakedNavigation>();
            var actor = bridge.GetComponentInChildren<PlayerActor>();
            var cameraManager = Game.CameraManager.Instance;
            yield return WaitFor(() => actor != null && Game.PartyManager.Instance.EnsurePendingSceneRestoreApplied(actor) &&
                actor.Animator != null && cameraManager.IsSceneCameraInitialized, "플레이어와 TPS 카메라 준비");
            Require(cameraManager.PrepareSceneCamera(actor.transform), "TPS 카메라 바인딩");
            yield return WaitFor(() => cameraManager.IsSceneCameraReadyFor(actor.transform), "TPS 카메라 추적");
            var camera = Camera.main;
            var movement = actor.ActorController as PlayerMovementController;
            Require(camera != null && movement?.Motor != null, "프로젝트 카메라와 KCC");
            StreamingLog($"ENTRY {actor.transform.position} revision={streamer.Catalog.bake.buildId} handles={streamer.OwnedSceneCount}");
            Capture(camera, StreamingOutput + "/drive-entry.png");

            var entry = actor.transform.position;
            var legs = BuildDriveLegs(definition, entry, fullLoop);
            var direction = legs[0][1] - legs[0][0];
            cameraManager.SetRotation(Quaternion.LookRotation(direction).eulerAngles.y, 16);
            cameraManager.SnapToTarget(entry);
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                for (int leg = 0; leg < legs.Length; leg++)
                    yield return DriveLeg(legs[leg], leg, actor, movement, camera, streamer, navigation, pad);
            }
            finally { InputSystem.RemoveDevice(pad); }

            yield return new WaitForSecondsRealtime(1.25f);
            yield return VerifyDestinationRetry(bridge, streamer, actor, camera);
            StreamingLog("DONE");
        }

        private static Vector3[][] BuildDriveLegs(WorldDefinition definition, Vector3 entry, bool fullLoop)
        {
            var roads = definition.Planning.roads.Select(road =>
                RoadGeometry.Sample(road.knots, fullLoop ? 2 : .85).Select(Point).ToArray()).ToArray();
            var firstRoad = roads[0];
            int checkpoint = Closest(entry);
            Vector3[][] forward;
            if (fullLoop)
                forward = new[] { firstRoad.Skip(checkpoint).ToArray(), roads[1], roads[2], firstRoad.Take(checkpoint + 1).ToArray() };
            else
            {
                var bridge = definition.Planning.bridges.Single();
                var exit = bridge.startSocket + (bridge.startSocket - bridge.endSocket).normalized * 9;
                int end = Closest(exit);
                Require(end > checkpoint, "교량과 경계를 포함하는 접근로");
                forward = new[] { firstRoad.Skip(checkpoint).Take(end - checkpoint + 1).ToArray() };
            }
            return forward.Concat(forward.Reverse().Select(road => road.Reverse().ToArray())).ToArray();

            int Closest(Vector3 point) => Enumerable.Range(0, firstRoad.Length)
                .OrderBy(i => HorizontalDistance(firstRoad[i], point)).First();
        }

        private static IEnumerator DriveLeg(Vector3[] points, int leg, PlayerActor actor, PlayerMovementController movement,
            Camera camera, WorldChunkStreamer streamer, WorldBakedNavigation navigation, Gamepad pad)
        {
            var samples = new List<(float milliseconds, bool stable, int chunk, bool link)>();
            var states = streamer.Catalog.chunks.Select((_, i) => streamer.GetState(i)).ToArray();
            int previous = streamer.FindChunk(actor.transform.position);
            int transitions = 0;
            int grounded = 0;
            Collider lastSupport = null;
            bool bridgeDeck = false;
            Vector3 target = points[0];
            try
            {
                foreach (var waypoint in points)
                {
                    target = waypoint;
                    float deadline = Time.realtimeSinceStartup + 12;
                    while (HorizontalDistance(actor.transform.position, target) > 1.1f)
                    {
                        Require(Time.realtimeSinceStartup < deadline, $"도로 {leg + 1}: {actor.transform.position} → {target}");
                        var direction = Vector3.ProjectOnPlane(target - actor.transform.position - movement.Motor.BaseVelocity * .2f, Vector3.up);
                        var forward = movement.CameraForwardDirection;
                        var right = Vector3.Cross(Vector3.up, forward);
                        var input = new Vector2(Vector3.Dot(direction.normalized, right), Vector3.Dot(direction.normalized, forward));
                        float magnitude = Mathf.Clamp(direction.magnitude * .3f, .33f, .65f);
                        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = input * magnitude });
                        yield return null;
                        int current = streamer.FindChunk(actor.transform.position);
                        bool stable = movement.Motor.GroundingStatus.IsStableOnGround;
                        if (stable) grounded++;
                        bool link = navigation.IsLinkReady(0);
                        samples.Add((Time.unscaledDeltaTime * 1000, stable, current, link));
                        var support = movement.Motor.GroundingStatus.GroundCollider;
                        if (!bridgeDeck && support != null && support != lastSupport)
                        {
                            lastSupport = support;
                            var gate = support.GetComponentInParent<WorldBridgeAccessGate>();
                            if (gate != null)
                            {
                                Require(gate.CanEnter && link, "교량 지지 Collider와 Nav 링크 Ready");
                                bridgeDeck = true;
                                StreamingLog($"BRIDGE {leg + 1} {actor.transform.position} stable={stable}");
                            }
                        }
                        if (current != previous)
                        {
                            Require(current >= 0 && streamer.GetState(current) == WorldChunkState.Ready, "도착 타일 Ready");
                            StreamingLog($"EDGE {leg + 1}.{++transitions} {previous}->{current} {actor.transform.position} grounded={stable} link={link} camera={camera.transform.position}");
                            previous = current;
                            Capture(camera, $"{StreamingOutput}/drive-leg-{leg + 1}-edge-{transitions}.png");
                        }
                        for (int i = 0; i < states.Length; i++)
                        {
                            var state = streamer.GetState(i);
                            if (state == states[i]) continue;
                            StreamingLog($"CHUNK {streamer.Catalog.chunks[i].id} {states[i]}->{state}");
                            states[i] = state;
                        }
                    }
                }
                Require(grounded >= samples.Count * .8f, $"도로 {leg + 1} 접지율");
                StreamingLog($"PASS LEG {leg + 1} ground={grounded}/{samples.Count} edges={transitions} bridge={bridgeDeck} to={target}");
            }
            finally
            {
                InputSystem.QueueStateEvent(pad, new GamepadState());
                File.WriteAllLines($"{StreamingOutput}/drive-leg-{leg + 1}.csv", new[] { "frame_ms,grounded,chunk,bridge_link" }
                    .Concat(samples.Select(sample => $"{sample.milliseconds.ToString("F3", CultureInfo.InvariantCulture)},{sample.stable},{sample.chunk},{sample.link}")));
            }
        }

        private static IEnumerator VerifyDestinationRetry(WorldBuilderRuntimeBridge bridge, WorldChunkStreamer streamer, PlayerActor actor, Camera camera)
        {
            var origin = actor.transform.position;
            var destination = streamer.Catalog.bake.routes.First(route =>
                route.fromSurface == "tile_2_0").from;
            int chunk = streamer.FindChunk(destination);
            using var support = streamer.Pin(origin);
            using var cancellation = new CancellationTokenSource();
            streamer.DiagnosticMinimumResidency = 0;
            streamer.TrackPlayer(null);
            try
            {
                yield return WaitFor(() => streamer.GetState(chunk) == WorldChunkState.Unloaded, "검사용 목적지 해제");
                streamer.DiagnosticDelay = .6f;
                streamer.DiagnosticFailure = id => id == streamer.Catalog.chunks[chunk].id;
                var failed = bridge.TravelAsync(Bootstrap, null, destination, cancellation.Token).AsTask();
                yield return WaitFor(() => streamer.GetState(chunk) == WorldChunkState.Loading, "목적지 로드 대기");
                var cameraPosition = camera.transform.position;
                Require(HorizontalDistance(actor.transform.position, origin) < .15f && streamer.CanTraverse(origin, origin),
                    "지연 중 현재 위치와 지지 타일 유지");
                Capture(camera, StreamingOutput + "/destination-pending.png");
                yield return WaitFor(() => failed.IsCompleted, "의도한 목적지 실패");
                Require(failed.IsFaulted && (failed.Exception?.GetBaseException().Message?.Contains("검증용 청크 초기화 실패") ?? false) &&
                    HorizontalDistance(actor.transform.position, origin) < .15f &&
                    Vector3.Distance(camera.transform.position, cameraPosition) < .5f &&
                    actor.ActorController.Motor.GroundingStatus.IsStableOnGround, "실패 시 플레이어·카메라·접지 유지");
                streamer.DiagnosticFailure = null;
                var retry = bridge.TravelAsync(Bootstrap, null, destination, cancellation.Token).AsTask();
                yield return WaitFor(() => retry.IsCompleted, "목적지 재시도");
                Require(!retry.IsFaulted && !retry.IsCanceled && streamer.GetState(chunk) == WorldChunkState.Ready &&
                    HorizontalDistance(actor.transform.position, destination) < .15f, "재시도 후 준비된 타일에 도착");
                yield return new WaitForSecondsRealtime(.5f);
                Capture(camera, StreamingOutput + "/destination-ready.png");
                StreamingLog($"PASS DELAY, FAILURE, UNLOAD, RETRY {origin} -> {actor.transform.position}");
            }
            finally
            {
                cancellation.Cancel();
                streamer.DiagnosticFailure = null;
                streamer.DiagnosticDelay = 0;
                streamer.DiagnosticMinimumResidency = null;
                streamer.TrackPlayer(actor.transform);
            }
        }

        private static IEnumerator WaitFor(Func<bool> predicate, string step)
        {
            float deadline = Time.realtimeSinceStartup + 45;
            while (!predicate())
            {
                Require(Time.realtimeSinceStartup < deadline, step);
                yield return null;
            }
            StreamingLog("PASS " + step);
        }

        private static void StreamingLog(string value) =>
            File.AppendAllText(StreamingOutput + "/drive.log", value + Environment.NewLine);
    }
}
