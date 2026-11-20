using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;
using Game = UPlayGround.Manager;
using EngineScenes = UnityEngine.SceneManagement.SceneManager;

namespace UPlayGround.Editor
{
    /// <summary>지역 월드에서 프로젝트 플레이어 입력·KCC·TPS 카메라를 검증한다.</summary>
    public static partial class WorldBuilderP10PlayProbe
    {
        private const string World = "35c2ca802bd64575961ac7cda330ac91";
        private const string Definition = "Assets/10.Datas/WorldBuilder/" + World + "_Authoring.asset";
        private const string Output = "output/WorldBuilder/P10/TPS";

        public static void Start()
        {
            if (!Application.isPlaying || EngineScenes.GetActiveScene().name != "Title")
                throw new InvalidOperationException("Title Play Mode에서 P10 검증을 시작하세요.");
            if (Game.SceneManager.Instance.IsLoading)
                throw new InvalidOperationException("타이틀 로드가 끝난 뒤 실행하세요.");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "probe.log"), "");
            Game.GameManager.Instance.StartCoroutine(RunGuarded());
        }

        private static IEnumerator RunGuarded()
        {
            var probe = Run();
            try
            {
                while (true)
                {
                    object next;
                    try
                    {
                        if (!probe.MoveNext()) yield break;
                        next = probe.Current;
                    }
                    catch (Exception exception)
                    {
                        Log("FAIL " + exception);
                        break;
                    }
                    yield return next;
                }
            }
            finally { (probe as IDisposable)?.Dispose(); }
        }

        private static IEnumerator Run()
        {
            var definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(Definition);
            var party = AssetDatabase.LoadAssetAtPath<PartyConfigSO>("Assets/10.Datas/Party/PartyConfig.asset");
            var saveField = typeof(Game.SaveManager).GetField("_saveFolder", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(definition != null && definition.Planning != null && party != null && saveField != null, "검증 원본");
            string saveFolder = Path.GetFullPath(Path.Combine(Output, "isolated-saves-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(saveFolder);
            // 에디터 Play 종료까지 자동 저장도 격리 경로를 사용한다.
            saveField.SetValue(Game.SaveManager.Instance, saveFolder);
            Log("ISOLATED " + Path.GetFileName(saveFolder));

            var title = EngineScenes.GetActiveScene();
            string path = AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            var scene = EngineScenes.GetSceneByPath(path);
            Require(scene.isLoaded && EngineScenes.SetActiveScene(scene), "지역 씬 활성화");

            var cameraRoot = new GameObject("P10 검증 메인 카메라", typeof(Camera), typeof(AudioListener));
            var camera = cameraRoot.GetComponent<Camera>();
            cameraRoot.tag = "MainCamera";
            camera.farClipPlane = 3000;
            yield return EngineScenes.UnloadSceneAsync(title);

            var roads = definition.Planning.roads.Select(road => RoadGeometry.Sample(road.knots, 1)).ToArray();
            var bridge = definition.Planning.bridges.Single();
            var gate = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldBridgeAccessGate>()).Single();
            gate.SetEndpointReady(0, true);
            gate.SetEndpointReady(1, true);
            Require(gate.CanEnter, "교량 양쪽 준비");
            var spawn = Point(roads[0][0]);
            var spawnRoot = new GameObject("P10 검증 액터");
            var actor = Game.ActorSpawnManager.Instance.SpawnActor(party.playerActorId, spawn + Vector3.up * .15f, Quaternion.identity,
                parent: spawnRoot.transform) as PlayerActor;
            Require(actor != null, "프로젝트 플레이어 생성");
            actor.gameObject.SetActive(true);
            var context = spawnRoot.AddComponent<SceneContext>();
            context.MapID = World;
            context.SceneType = SceneType.GamePlay;
            context.ShowExplorationHud = false;
            Require(Game.SceneManager.Instance.NotifySceneContextReady(context), "지역 게임플레이 컨텍스트");

            var partyManager = Game.PartyManager.Instance;
            float deadline = Time.realtimeSinceStartup + 45;
            bool partyReady = false;
            yield return new WaitUntil(() =>
                (partyReady = partyManager.EnsurePendingSceneRestoreApplied(actor)) ||
                Time.realtimeSinceStartup >= deadline);
            Require(partyReady && partyManager.ActiveCharacter == actor && actor.Animator != null,
                "파티 캐릭터 모델과 애니메이션 준비");
            Log($"PARTY {partyManager.ActiveCharacterType}");

            var cameraManager = Game.CameraManager.Instance;
            deadline = Time.realtimeSinceStartup + 25;
            yield return new WaitUntil(() => cameraManager.IsSceneCameraInitialized || Time.realtimeSinceStartup >= deadline);
            Require(cameraManager.PrepareSceneCamera(actor.transform), "프로젝트 TPS 카메라 초기화");
            deadline = Time.realtimeSinceStartup + 10;
            yield return new WaitUntil(() => cameraManager.IsSceneCameraReadyFor(actor.transform) || Time.realtimeSinceStartup >= deadline);
            Require(cameraManager.IsSceneCameraReadyFor(actor.transform), "TPS 카메라 추적");
            Require(actor.ActorController?.Motor != null, "플레이어 KCC");

            var motor = actor.ActorController.Motor;
            var movement = actor.ActorController as UPlayGround.MovementController.PlayerMovementController;
            Require(movement != null, "카메라 기준 플레이어 입력");
            Log($"ACTOR {actor.name} radius={motor.Capsule.radius:F2} slope={motor.MaxStableSlopeAngle:F1}");
            var pad = InputSystem.AddDevice<Gamepad>();
            int passed = 0;
            try
            {
                var bridgePoints = Enumerable.Range(0, 9).Select(i => Vector3.Lerp(bridge.startSocket, bridge.endSocket, i / 8f)).ToArray();
                var routes = new[]
                {
                    (name: "castle", points: ShortRoute(roads[0], false)),
                    (name: "riverside", points: ShortRoute(roads[0], true)),
                    (name: "lakeside", points: ShortRoute(roads[2], false)),
                    (name: "bridge", points: bridgePoints),
                    (name: "tile-boundary", points: TileCrossing(roads[2]))
                };
                foreach (var route in routes)
                {
                    var start = route.points[0];
                    var forward = Vector3.ProjectOnPlane(route.points[1] - start, Vector3.up).normalized;
                    actor.TeleportTo(start + Vector3.up * .18f, Quaternion.LookRotation(forward));
                    cameraManager.SetRotation(Quaternion.LookRotation(forward).eulerAngles.y, 16);
                    cameraManager.SnapToTarget(start);
                    InputSystem.QueueStateEvent(pad, new GamepadState());
                    yield return new WaitForSecondsRealtime(.35f);
                    int groundedFrames = 0, samples = 0;
                    float lowest = float.PositiveInfinity;
                    bool reached = true;
                    Vector3 lastTarget = start;
                    foreach (var target in route.points.Skip(1))
                    {
                        lastTarget = target;
                        deadline = Time.realtimeSinceStartup + 12;
                        while (HorizontalDistance(actor.transform.position, target) > 1f && Time.realtimeSinceStartup < deadline)
                        {
                            var delta = Vector3.ProjectOnPlane(target - actor.transform.position - motor.BaseVelocity * .2f, Vector3.up);
                            var cameraForward = movement.CameraForwardDirection;
                            var cameraRight = Vector3.Cross(Vector3.up, cameraForward);
                            var stick = new Vector2(Vector3.Dot(delta.normalized, cameraRight), Vector3.Dot(delta.normalized, cameraForward));
                            float magnitude = Mathf.Clamp(delta.magnitude * .3f, .33f, .65f);
                            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = stick * magnitude });
                            yield return null;
                            samples++;
                            if (motor.GroundingStatus.IsStableOnGround) groundedFrames++;
                            lowest = Mathf.Min(lowest, actor.transform.position.y - target.y);
                        }

                        if (HorizontalDistance(actor.transform.position, target) <= 1f) continue;
                        reached = false;
                        break;
                    }

                    InputSystem.QueueStateEvent(pad, new GamepadState());
                    bool pass = reached && samples > 0 && groundedFrames >= samples * .8f && lowest >= -3;
                    if (pass) passed++;
                    Log($"{(pass ? "PASS" : "FAIL")} {route.name}: reached={reached} grounded={groundedFrames}/{samples} " +
                        $"deltaY={lowest:F2} position={actor.transform.position} target={lastTarget} " +
                        $"error={HorizontalDistance(actor.transform.position, lastTarget):F2} camera={cameraManager.CurrentCameraMode}");
                }

                var terrains = WorldBuilderAuthoringAssets.FindTerrains(definition);
                foreach (var concept in definition.Profile.RegionConcepts)
                {
                    var anchor = ViewAnchor(concept.RegionId, definition);
                    var position = GroundView(terrains, anchor);
                    float yaw = concept.RegionId is "castle_plains" or "lakeside_village" ? 180 : 0;
                    actor.TeleportTo(position + Vector3.up * .15f, Quaternion.Euler(0, yaw, 0));
                    cameraManager.SetRotation(yaw, 12);
                    cameraManager.SnapToTarget(position);
                    yield return new WaitForSecondsRealtime(.4f);
                    Capture(camera, Path.Combine(Output, concept.RegionId + ".png"));
                    Log($"CAPTURE {concept.RegionId} {actor.transform.position} pitch={camera.transform.eulerAngles.x:F1}");
                }
            }
            finally
            {
                InputSystem.RemoveDevice(pad);
            }

            Log($"DONE movement={passed}/5 captures={definition.Profile.RegionConcepts.Count}");
        }

        private static Vector3[] ShortRoute(PlanPoint[] road, bool reverse) =>
            Enumerable.Range(0, 11).Select(i => Point(road[reverse ? road.Length - 1 - i * 2 : i * 2])).ToArray();

        private static Vector3[] TileCrossing(PlanPoint[] road)
        {
            for (int i = 10; i < road.Length - 10; i++)
                if ((int)(road[i - 1].x / 512) != (int)(road[i].x / 512) || (int)(road[i - 1].z / 512) != (int)(road[i].z / 512))
                    return Enumerable.Range(0, 11).Select(j => Point(road[i - 10 + j * 2])).ToArray();
            throw new InvalidOperationException("타일 경계를 지나는 도로 표본이 없습니다.");
        }

        private static Vector2 ViewAnchor(string region, WorldDefinition definition)
        {
            if (region == "riverside_village") return new Vector2((float)definition.Planning.pois[1].anchor.x, (float)definition.Planning.pois[1].anchor.z - 13);
            if (region == "lakeside_village") return new Vector2((float)definition.Planning.pois[2].anchor.x, (float)definition.Planning.pois[2].anchor.z + 13);
            if (region == "castle_plains") return new Vector2((float)definition.Planning.pois[0].anchor.x, (float)definition.Planning.pois[0].anchor.z + 13);
            var regionMask = definition.Ecology.biomes.Single(biome => biome.name == region).mask;
            return regionMask.Aggregate(Vector2.zero, (sum, point) => sum + point) / regionMask.Length;
        }

        private static Vector3 GroundView(Terrain[,] terrains, Vector2 anchor)
        {
            Vector3 nearest = default;
            float best = float.PositiveInfinity;
            for (int z = -96; z <= 96; z += 8)
            for (int x = -96; x <= 96; x += 8)
            {
                var point = anchor + new Vector2(x, z);
                float distance = x * x + z * z;
                if (distance >= best || point.x < 0 || point.y < 0 || point.x >= 2048 || point.y >= 2048) continue;
                var tile = terrains[(int)(point.y / 512), (int)(point.x / 512)];
                var local = (new Vector3(point.x, 0, point.y) - tile.transform.position) / 512;
                if (tile.terrainData.GetSteepness(local.x, local.z) > 27) continue;
                nearest = new Vector3(point.x, tile.transform.position.y + tile.terrainData.GetInterpolatedHeight(local.x, local.z), point.y);
                best = distance;
            }
            Require(float.IsFinite(best), "지역 내 안전한 카메라 관찰 위치");
            return nearest;
        }

        private static void Capture(Camera camera, string path)
        {
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var surface = RenderTexture.GetTemporary(1280, 720, 24);
            var previous = RenderTexture.active;
            var target = camera.targetTexture;
            try
            {
                camera.targetTexture = surface;
                camera.Render();
                RenderTexture.active = surface;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = target;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(surface);
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        private static Vector3 Point(PlanPoint p) => new((float)p.x, (float)p.y, (float)p.z);
        private static void Log(string value) => File.AppendAllText(Path.Combine(Output, "probe.log"), value + Environment.NewLine);
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    }
}
