using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Manager;
using UPlayGround.Manager.World;
using UPlayGround.Dialogue;

namespace UPlayGround.Tool.Editor.Map
{
    [InitializeOnLoad]
    public static partial class ScenarioMapAuthoring
    {
        private const string SmokeRequestedKey = "UPlayGround.ScenarioMap.SmokeRequested";
        private const string SmokeSucceededKey = "UPlayGround.ScenarioMap.SmokeSucceeded";
        private const string SmokeSceneKey = "UPlayGround.ScenarioMap.SmokeScene";
        private const string SmokeKeepPlayingKey = "UPlayGround.ScenarioMap.KeepPlaying";
        private static SmokeReport s_smoke;
        private static PlayerActor s_smokePlayer;
        private static Gamepad s_smokePad;
        private static double s_smokeDeadline;
        private static double s_smokeNextStep;
        private static int s_smokeStep;
        private static Vector3 s_walkStart;

        [Serializable] private sealed class SmokeReport
        {
            public string scene;
            public string bootState;
            public string mapId;
            public string sceneLoadState;
            public Vector3 spawn;
            public Vector3 walkEnd;
            public float gamepadWalkDistance;
            public int flowRunners;
            public int validatedResources;
            public bool isGrounded;
            public bool hasNavigation;
            public bool hasLoadingOverlay;
            public bool cameraReady;
            public bool succeeded;
            public bool hasPlayerActor;
            public string partyPreparation;
            public int warningCount;
            public List<string> warnings = new();
            public List<string> errors = new();
            public List<DistrictWalkResult> districtWalks = new();
        }

        static ScenarioMapAuthoring()
        {
            EditorApplication.playModeStateChanged += OnSmokePlayModeChanged;
            if (SessionState.GetBool(SmokeRequestedKey, false)) EditorApplication.delayCall += ResumeSmoke;
        }

        private static void ResumeSmoke()
        {
            if (EditorApplication.isPlaying) BeginSmoke();
        }

        /// <summary>실제 씬 초기화·플레이어 스폰·게임패드 보행을 Play Mode에서 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 플레이 검증")]
        public static void RunPlayModeSmoke()
        {
            RequireEditMode();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Layout layout = ReadLayout();
            EditorSceneManager.OpenScene(layout.scenePath);
            Validate();
            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            Vector3 sample = layout.routes[0].points[3] - terrain.transform.position;
            float[,,] weights = terrain.terrainData.GetAlphamaps(
                Mathf.RoundToInt(sample.x / layout.terrainSize.x * (terrain.terrainData.alphamapWidth - 1)),
                Mathf.RoundToInt(sample.z / layout.terrainSize.z * (terrain.terrainData.alphamapHeight - 1)), 1, 1);
            Debug.Log($"[ScenarioMap] 길 표면 가중치: {weights[0, 0, 0]:F2}, {weights[0, 0, 1]:F2}, {weights[0, 0, 2]:F2}");
            StartSmoke(layout.scenePath);
        }

        /// <summary>동일한 실행 조건으로 원본 씬의 초기화 문제를 비교한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 원본 플레이 비교")]
        public static void RunSourcePlayModeSmoke()
        {
            RequireEditMode();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            StartSmoke(SourceScenePath);
        }

        /// <summary>기존 부팅·로딩 경로로 새 맵을 시작하고 플레이를 계속한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 플레이")]
        public static void PlayScenarioMap()
        {
            RequireEditMode();
            if (Application.isBatchMode) throw new InvalidOperationException("배치 실행에서는 플레이 검증 도구를 사용하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Layout layout = ReadLayout();
            EditorSceneManager.OpenScene(layout.scenePath);
            Validate();
            StartSmoke(layout.scenePath, true);
        }

        private static void StartSmoke(string scenePath, bool keepPlaying = false)
        {
            SessionState.SetBool(SmokeRequestedKey, true);
            SessionState.SetBool(SmokeSucceededKey, false);
            SessionState.SetString(SmokeSceneKey, scenePath);
            SessionState.SetBool(SmokeKeepPlayingKey, keepPlaying);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("ScenarioSmokeBootstrap").AddComponent<SceneContext>();
            bootstrap.SceneType = UPlayGround.Data.EnumType.SceneType.Loading;
            BeginSmoke();
            EditorApplication.EnterPlaymode();
        }

        private static void OnSmokePlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(SmokeRequestedKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                BeginSmoke();
            }
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.update -= TickSmoke;
            Application.logMessageReceived -= CaptureSmokeLog;
            if (s_smokePad != null) InputSystem.RemoveDevice(s_smokePad);
            s_smokePad = null;
            s_smoke = null;
            s_smokePlayer = null;
            bool succeeded = SessionState.GetBool(SmokeSucceededKey, false);
            string scenePath = SessionState.GetString(SmokeSceneKey, "");
            SessionState.EraseBool(SmokeRequestedKey);
            SessionState.EraseBool(SmokeSucceededKey);
            SessionState.EraseString(SmokeSceneKey);
            SessionState.EraseBool(SmokeKeepPlayingKey);
            if (File.Exists(scenePath)) EditorSceneManager.OpenScene(scenePath);
            if (Application.isBatchMode) EditorApplication.Exit(succeeded ? 0 : 1);
        }

        private static void BeginSmoke()
        {
            if (s_smoke != null) return;
            s_smoke = new SmokeReport { scene = SessionState.GetString(SmokeSceneKey, ReadLayout().scenePath) };
            s_smokeStep = 0;
            s_smokeDeadline = EditorApplication.timeSinceStartup + 180;
            Application.logMessageReceived += CaptureSmokeLog;
            EditorApplication.update += TickSmoke;
            Debug.Log("[ScenarioMap] Play Mode 검증 시작");
        }

        private static void CaptureSmokeLog(string message, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert) s_smoke.errors.Add(message);
            else if (type == LogType.Warning)
            {
                s_smoke.warningCount++;
                if (!s_smoke.warnings.Contains(message)) s_smoke.warnings.Add(message);
            }
        }

        private static void TickSmoke()
        {
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (now > s_smokeDeadline) throw new TimeoutException("실제 씬의 초기화 또는 보행 검증 시간이 초과되었습니다.");
                if (!EditorApplication.isPlaying) return;
                if (Application.isBatchMode && EditorApplication.isPaused) EditorApplication.isPaused = false;
                if (s_smokeStep == 0)
                {
                    GameManager bootstrap = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (bootstrap == null || bootstrap.BootState != GameBootState.Ready) return;
                    if (!Enum.TryParse(ReadLayout().startingCharacter,
                            out UPlayGround.Data.EnumType.CharacterActorType character)
                        || character == UPlayGround.Data.EnumType.CharacterActorType.None)
                        throw new InvalidOperationException("레이아웃의 시작 캐릭터를 확인하세요.");
                    PartyManager.Instance.PrepareNewGameStartingCharacter(character);
                    UPlayGround.Manager.SceneManager.Instance.LoadScene(Path.GetFileNameWithoutExtension(s_smoke.scene));
                    s_smokeStep = 10;
                    return;
                }
                if (s_smokeStep == 10)
                {
                    GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (manager == null) return;
                    s_smoke.bootState = manager.BootState.ToString();
                    if (manager.BootState == GameBootState.Failed) throw new InvalidOperationException(manager.InitializationFailure.ToString());
                    if (manager.BootState != GameBootState.Ready) return;
                    var sceneManager = UPlayGround.Manager.SceneManager.Instance;
                    s_smoke.sceneLoadState = sceneManager.LoadState.ToString();
                    if (sceneManager.LoadState == SceneLoadState.Failed) throw new InvalidOperationException(sceneManager.LastLoadFailure);
                    PlayerActor actor = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
                    s_smoke.hasPlayerActor = actor != null;
                    s_smoke.mapId = UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
                    s_smoke.flowRunners = UnityEngine.Object.FindObjectsByType<FlowGraphRunner>(FindObjectsSortMode.None).Length;
                    if (actor != null)
                    {
                        s_smoke.spawn = actor.transform.position;
                        s_smoke.hasNavigation = NavMesh.SamplePosition(s_smoke.spawn, out _, 2, NavMesh.AllAreas);
                    }
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    Type partyType = typeof(PartyManager);
                    object isRunning = partyType.GetField("_isPlayerPreparationRunning", flags)?.GetValue(PartyManager.Instance);
                    object isReady = partyType.GetField("_isPlayerPreparationReady", flags)?.GetValue(PartyManager.Instance);
                    var definitions = partyType.GetField("_characterDefinitions", flags)?.GetValue(PartyManager.Instance) as System.Collections.IDictionary;
                    var models = partyType.GetField("_residentPlayerModels", flags)?.GetValue(PartyManager.Instance) as System.Collections.IDictionary;
                    s_smoke.partyPreparation = $"실행={isRunning}, 준비={isReady}, 정의={definitions?.Count}, 모델={models?.Count}";
                    s_smokePlayer = PartyManager.Instance.ActiveCharacter;
                    if (s_smokePlayer == null || s_smokePlayer.ActorController?.Motor == null) return;
                    if (sceneManager.LoadState != SceneLoadState.Completed) return;
                    foreach (RuntimePlacementLoader loader in UnityEngine.Object.FindObjectsByType<RuntimePlacementLoader>(FindObjectsSortMode.None))
                        if (!loader.IsSpawnComplete) return;
                    s_smoke.mapId = UPlayGround.Manager.SceneManager.Instance.CurrentMapID;
                    if (string.IsNullOrEmpty(s_smoke.mapId)) return;
                    if (SessionState.GetBool(SmokeKeepPlayingKey, false))
                    {
                        EditorApplication.update -= TickSmoke;
                        Application.logMessageReceived -= CaptureSmokeLog;
                        SessionState.SetBool(SmokeSucceededKey, s_smoke.errors.Count == 0);
                        s_smoke = null;
                        Debug.Log("[ScenarioMap] 플레이 준비 완료. 맵에서 자유롭게 플레이하세요.");
                        return;
                    }
                    s_smokeNextStep = now + 2;
                    s_smokeStep = 1;
                    return;
                }
                if (now < s_smokeNextStep) return;
                if (s_smokeStep >= 30)
                {
                    TickDistrictSmoke(now);
                    return;
                }
                if (s_smokeStep == 1)
                {
                    s_smoke.hasLoadingOverlay = UnityEngine.Object.FindFirstObjectByType<LoadingSceneController>() != null;
                    s_smoke.cameraReady = CameraManager.Instance.IsSceneCameraReady;
                    if (s_smoke.hasLoadingOverlay || !s_smoke.cameraReady)
                        throw new InvalidOperationException("로딩 화면 해제 또는 카메라 준비가 완료되지 않았습니다.");
                    s_smoke.spawn = s_smokePlayer.transform.position;
                    s_smoke.isGrounded = s_smokePlayer.ActorController.Motor.GroundingStatus.IsStableOnGround;
                    s_smoke.hasNavigation = NavMesh.SamplePosition(s_smoke.spawn, out _, 2, NavMesh.AllAreas);
                    s_smoke.flowRunners = UnityEngine.Object.FindObjectsByType<FlowGraphRunner>(FindObjectsSortMode.None).Length;
                    if (!s_smoke.isGrounded || !s_smoke.hasNavigation || s_smoke.flowRunners == 0)
                        throw new InvalidOperationException("스폰 접지·NavMesh·시나리오 FlowGraph 실행선 중 누락이 있습니다.");
                    // 스킵도 실제 대화 완료 경로를 이용해 입력 잠금 해제를 확인한다.
                    if (DialogueManager.Instance.IsDialogueActive)
                    {
                        DialogueManager.Instance.RequestSkip();
                        s_smokeNextStep = now + 0.5;
                        return;
                    }
                    s_walkStart = s_smokePlayer.transform.position;
                    s_smokePad = InputSystem.AddDevice<Gamepad>();
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState { leftStick = Vector2.up });
                    s_smokeNextStep = now + 2;
                    s_smokeStep = 2;
                    return;
                }
                s_smoke.walkEnd = s_smokePlayer.transform.position;
                Vector3 movement = s_smoke.walkEnd - s_walkStart;
                movement.y = 0;
                s_smoke.gamepadWalkDistance = movement.magnitude;
                if (s_smoke.gamepadWalkDistance < 1) s_smoke.errors.Add("게임패드 이동 입력 후 실제 보행이 1m 미만입니다.");
                if (!s_smokePlayer.ActorController.Motor.GroundingStatus.IsStableOnGround)
                    s_smoke.errors.Add("보행 후 플레이어 접지가 불안정합니다.");
                s_smoke.validatedResources = ValidateQualityRuntime(s_smokePlayer);
                ValidateWorldRuntime(s_smokePlayer);
                CaptureRuntimeView();
                if (!BeginDistrictSmoke(now)) FinishSmoke();
            }
            catch (Exception exception)
            {
                s_smoke.errors.Add(exception.Message);
                FinishSmoke();
            }
        }

        private static void FinishSmoke()
        {
            EditorApplication.update -= TickSmoke;
            Application.logMessageReceived -= CaptureSmokeLog;
            if (s_smokePad != null) InputSystem.RemoveDevice(s_smokePad);
            s_smokePad = null;
            s_smoke.succeeded = s_smoke.errors.Count == 0;
            SessionState.SetBool(SmokeSucceededKey, s_smoke.succeeded);
            Directory.CreateDirectory(ReportDirectory);
            string reportName = s_smoke.scene == SourceScenePath ? "/SourcePlayMode.json" : "/PlayMode.json";
            File.WriteAllText(ReportDirectory + reportName, JsonUtility.ToJson(s_smoke, true));
            Debug.Log("[ScenarioMap] Play Mode 검증: " + (s_smoke.succeeded ? "통과" : "실패"));
            EditorApplication.ExitPlaymode();
        }

        private static void CaptureRuntimeView()
        {
            Camera camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("실제 플레이 카메라가 없습니다.");
            Directory.CreateDirectory(PreviewDirectory);
            RenderTexture target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                texture.Apply();
                File.WriteAllBytes(PreviewDirectory + "/07_PlayMode.png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
