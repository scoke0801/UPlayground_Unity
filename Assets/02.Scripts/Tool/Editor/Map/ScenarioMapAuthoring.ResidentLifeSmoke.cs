using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.Components;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Interaction;
using UPlayGround.Manager;
using UPlayGround.UI;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string ResidentLifeSmokeKey = "UPlayGround.ScenarioMap.ResidentLifeSmoke";
        private static string ResidentLifeSmokeDirectory => Array.IndexOf(Environment.GetCommandLineArgs(), "-p04Regression") >= 0
            ? "output/ScenarioGameplay/P04/ResidentRegression" : "output/ScenarioGameplay/P03/Workbench/Smoke";
        private static bool IsResidentLifeSmoke => SessionState.GetBool(ResidentLifeSmokeKey, false);
        private static NpcActor s_lifeDaon;
        private static Vector3 s_lifeDaonHome, s_lifeDaonStart;
        private static float s_lifeDaonDistance;
        private static double s_lifeWaitUntil;
        private static int s_lifeJunInstance;
        private static int s_lifePortraitIndex;
        private static NpcActor s_lifePortraitActor;
        private static bool s_lifePauseBeforeBacklog;
        private static int s_lifeAfterBacklogStep;
        private static double s_workReviewEnd;
        private static readonly HashSet<int> s_workMotionSamples = new();
        private static bool s_workPropObserved;
        private static readonly string[] LifePortraitActors = { "NPC_Story_Guide", "NPC_Joan", "NPC_Hazel", "NPC_Mia", "NPC_Penny", "NPC_Morgan", "NPC_Joy", "NPC_Lucia", "NPC_Jun", "Npc_Sodam" };
        private static readonly List<string> s_lifeChecks = new();
        private static readonly string[] LifeFlags = { "lake.side.jun_rescued", "lake.side.herb_finished", "lake.side.lantern_finished", "lake.side.salt_finished" };

        /// <summary>배회·대화 복귀·실제 저장 파일·씬 재진입으로 주민 생활 상태를 검증한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/주민 생활 복원 검증")]
        public static void RunResidentLifeSmoke()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            Directory.CreateDirectory(ResidentLifeSmokeDirectory);
            SessionState.SetBool(ResidentLifeSmokeKey, true);
            SessionState.SetString(BaselineCharacterKey, "Raon");
            StartSmoke(ReadLayout().scenePath);
        }

        private static void BeginResidentLifeSmoke(double now)
        {
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            s_smokeDeadline = now + 360;
            s_lifeChecks.Clear();
            s_lifeDaon = GameObject.Find("ScenarioResidents/Npc_Daon").GetComponent<NpcActor>();
            s_lifeDaonHome = (Vector3)typeof(NpcBrain).GetField("_spawnPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(s_lifeDaon.GetComponent<NpcBrain>());
            PlaceLifePlayer(new Vector3(1009, 0, 439));
            s_lifeDaonStart = s_lifeDaon.transform.position;
            s_lifeDaonDistance = 0;
            s_lifeWaitUntil = now + 15;
            // 검증 전용 폴더로 저장 경로를 격리하여 사용자 슬롯에는 쓰지 않는다.
            string saveFolder = Path.GetFullPath(ResidentLifeSmokeDirectory + "/Saves");
            Directory.CreateDirectory(saveFolder);
            typeof(SaveManager).GetField("_saveFolder", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(SaveManager.Instance, saveFolder);
            CheckLife(SaveManager.Instance.SaveGameDetailed(0).Succeeded, "사건 전 실제 저장 파일 생성");
            s_smokeStep = 60;
        }

        private static void TickResidentLifeSmoke(double now)
        {
            // 배치 모드는 Game View를 연속 렌더하지 않으므로 촬영 전에 스킨 컬링을 갱신한다.
            WarmLifeView();
            if (s_smokeStep == 60 || s_smokeStep == 63)
            {
                Vector3 delta = s_lifeDaon.transform.position - s_lifeDaonStart;
                delta.y = 0;
                s_lifeDaonDistance = Mathf.Max(s_lifeDaonDistance, delta.magnitude);
                Vector3 home = s_lifeDaon.transform.position - s_lifeDaonHome;
                home.y = 0;
                if (home.magnitude > 2.3f) throw new InvalidOperationException("다온이 배회 생활권을 벗어났습니다: " + home.magnitude);
                if (now < s_lifeWaitUntil) return;
                CheckLife(s_lifeDaonDistance > 0.25f, s_smokeStep == 60 ? "다온 15초 배회·생활권 유지" : "다온 대화 후 15초 배회 복귀");
                if (s_smokeStep == 63) { BeginLifeRestoreChecks(now); return; }
                s_lifePortraitActor = Svc.ActorQuery.FindActor("Npc_Sodam") as NpcActor;
                CheckLife(s_lifePortraitActor != null && s_lifePortraitActor.CanInteract(), "소담 초기 등록·상호작용 가능");
                PlaceLifePlayerNear(s_lifePortraitActor);
                s_workMotionSamples.Clear();
                s_workPropObserved = false;
                s_workReviewEnd = now + 14;
                s_lifeWaitUntil = now + 1;
                s_smokeStep = 80;
                return;
            }
            if (now < s_lifeWaitUntil) return;
            if (s_smokeStep >= 80 && s_smokeStep <= 87)
            {
                TickWorkbenchInputCheck(now);
                return;
            }
            if (s_smokeStep == 76)
            {
                var dialogue = DialogueManager.Instance;
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>("Assets/10.Datas/Actor/ScenarioResidents/DLG_Npc_Sodam.asset");
                CheckLife(dialogue.IsDialogueActive && dialogue.History.Count > 0
                    && dialogue.History.Last().SpeakerName == "소담"
                    && dialogue.History.Last().RichBody == graph.nodes.Single(x => x.nodeId == "talk_0").dialogueText,
                    "소담 사건 전 발화·화자 일치");
                CheckLifeHair(s_lifePortraitActor);
                CheckWorkbenchFraming();
                CaptureRuntimeView();
                File.Copy(ResidentLifeSmokeDirectory + "/PlayMode.png", ResidentLifeSmokeDirectory + "/Npc_Sodam_Before.png", true);
                s_smokeStep = 77;
                return;
            }
            if (s_smokeStep == 77)
            {
                if (DialogueManager.Instance.IsDialogueActive)
                {
                    DialogueManager.Instance.Advance();
                    s_lifeWaitUntil = now + 0.5;
                    return;
                }
                CheckLife(!s_lifePortraitActor.IsInteracting() && !s_lifePortraitActor.IsDialogueStaged,
                    "소담 정상 종료 후 상호작용·대화 홀드 반환");
                PlaceLifePlayerNear(s_lifeDaon);
                s_lifeDaon.Interact(s_smokePlayer);
                s_lifeWaitUntil = now + 1;
                s_smokeStep = 61;
                return;
            }
            if (s_smokeStep == 61)
            {
                CheckLife(DialogueManager.Instance.IsDialogueActive && s_lifeDaon.IsInteracting(), "다온 배회 중 대화 시작");
                CheckLifeHair(s_lifeDaon);
                Svc.Flags.SetFlag("lake.side.herb_finished", true);
                s_lifeWaitUntil = now + 1;
                s_smokeStep = 62;
                return;
            }
            if (s_smokeStep == 62)
            {
                if (DialogueManager.Instance.IsDialogueActive)
                {
                    CheckLife(FindLifePresentation("Hazel").IsCompletedVisible == false, "대화 중 사건 상태 변화 보류");
                    CheckLife(FindLifePresentation("Sodam").IsCompletedVisible == false, "대화 중 소담 사건 상태 변화 보류");
                    DialogueManager.Instance.Advance();
                    s_lifeWaitUntil = now + 0.4;
                    return;
                }
                CheckLife(!s_lifeDaon.IsInteracting(), "다온 정상 종료·상호작용 해제");
                s_lifeDaonStart = s_lifeDaon.transform.position;
                s_lifeDaonDistance = 0;
                s_lifeWaitUntil = now + 15;
                s_smokeStep = 63;
                return;
            }
            if (s_smokeStep == 64)
            {
                AssertLifeState(true);
                var jun = Svc.ActorQuery.FindActor("NPC_Jun") as NpcActor;
                CheckLife(jun != null && jun.GetInstanceID() == s_lifeJunInstance, "구조 현장 준 재사용·중복 생성 없음");
                CheckLife(SaveManager.Instance.SaveGameDetailed(1).Succeeded, "사건 후 실제 저장 파일 생성");
                CheckLife(SaveManager.Instance.LoadGame(0), "사건 전 저장 파일 로드");
                s_lifeWaitUntil = now + 2;
                s_smokeStep = 65;
                return;
            }
            if (s_smokeStep == 65)
            {
                AssertLifeState(false);
                CheckLife(CountLifeJun() == 0, "사건 전 로드 시 마을 준 제거");
                CheckLife(SaveManager.Instance.LoadGame(1), "사건 후 저장 파일 로드");
                s_lifeWaitUntil = now + 3;
                s_smokeStep = 66;
                return;
            }
            if (s_smokeStep == 66)
            {
                AssertLifeState(true);
                CheckLife(CountLifeJun() == 1, "사건 후 로드 시 준 한 명 복원");
                CheckLife(SaveManager.Instance.LoadGame(1), "동일 저장 반복 로드");
                s_lifeWaitUntil = now + 2;
                s_smokeStep = 67;
                return;
            }
            if (s_smokeStep == 67)
            {
                AssertLifeState(true);
                CheckLife(CountLifeJun() == 1, "반복 로드 시 준 중복 없음");
                UPlayGround.Manager.SceneManager.Instance.LoadScene("LakeOfLifeScenario");
                s_lifeWaitUntil = now + 5;
                s_smokeStep = 68;
                return;
            }
            if (s_smokeStep == 68)
            {
                File.WriteAllText(ResidentLifeSmokeDirectory + "/ReentryState.txt",
                    UPlayGround.Manager.SceneManager.Instance.LoadState + " | " +
                    UPlayGround.Manager.SceneManager.Instance.LastLoadFailure + " | " +
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                if (UPlayGround.Manager.SceneManager.Instance.LoadState != SceneLoadState.Completed) return;
                s_smokePlayer = PartyManager.Instance.ActiveCharacter;
                if (s_smokePlayer == null || GameObject.Find("ResidentLife/Jun") == null) return;
                AssertLifeState(true);
                CheckLife(CountLifeJun() == 1, "씬 재진입 시 완료 배치·준 한 명 복원");
                s_lifeDaon = GameObject.Find("ScenarioResidents/Npc_Daon").GetComponent<NpcActor>();
                CheckLife(!s_lifeDaon.IsInteracting() && !s_lifeDaon.IsDialogueStaged, "씬 재진입 시 다온 대화 홀드 잔존 없음");
                PlaceLifePlayer(new Vector3(1060, 0, 412));
                s_lifeWaitUntil = now + 2;
                s_smokeStep = 69;
                return;
            }
            if (s_smokeStep == 69)
            {
                CaptureRuntimeView();
                var elder = Svc.ActorQuery.FindActor("Npc_Yeonmok") as NpcActor;
                CheckLife(elder != null && elder.CanInteract(), "연목 등록·대화 가능");
                PlaceLifePlayerNear(elder);
                elder.Interact(s_smokePlayer);
                s_lifeWaitUntil = now + 1;
                s_smokeStep = 70;
                return;
            }
            if (s_smokeStep == 70)
            {
                var dialogue = DialogueManager.Instance;
                CheckLifeHair(Svc.ActorQuery.FindActor("Npc_Yeonmok") as NpcActor);
                Sprite portrait = dialogue.PortraitTable.GetPortrait("연목");
                var history = dialogue.History;
                CheckLife(dialogue.IsDialogueActive && portrait != null && history.Count > 0
                    && history[history.Count - 1].SpeakerName == "연목"
                    && history[history.Count - 1].Portrait == portrait, "연목 화자·초상화·이력 일치");
                CheckLife(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None)
                    .Any(x => x.isActiveAndEnabled && x.sprite == portrait), "연목 대화 화면 초상화 표시");
                CaptureRuntimeView();
                File.Copy(ResidentLifeSmokeDirectory + "/PlayMode.png", ResidentLifeSmokeDirectory + "/YeonmokDialogue.png", true);
                BeginLifeBacklogCheck(now, 71);
                return;
            }
            if (s_smokeStep == 71)
            {
                CheckLife(!DialogueManager.Instance.IsDialogueActive, "연목 대화 취소·복귀");
                s_lifePortraitIndex = 0;
                s_smokeStep = 72;
            }
            if (s_smokeStep == 72 && s_lifePortraitIndex < LifePortraitActors.Length)
            {
                string id = LifePortraitActors[s_lifePortraitIndex];
                s_lifePortraitActor = Svc.ActorQuery.FindActor(id) as NpcActor;
                CheckLife(s_lifePortraitActor != null && s_lifePortraitActor.CanInteract(), id + " 대화 가능");
                PlaceLifePlayerNear(s_lifePortraitActor);
                s_lifePortraitActor.Interact(s_smokePlayer);
                s_lifeWaitUntil = now + 2;
                s_smokeStep = 73;
                return;
            }
            if (s_smokeStep == 73)
            {
                var dialogue = DialogueManager.Instance;
                CheckLifeHair(s_lifePortraitActor);
                string id = LifePortraitActors[s_lifePortraitIndex];
                CheckLife(dialogue.IsDialogueActive && dialogue.History.Count > 0, id + " 실제 대화 시작");
                var line = dialogue.History[dialogue.History.Count - 1];
                if (id == "Npc_Sodam")
                {
                    CheckWorkbenchFraming();
                    var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>("Assets/10.Datas/Actor/ScenarioResidents/DLG_Npc_Sodam.asset");
                    CheckLife(line.SpeakerName == "소담" && line.RichBody == graph.nodes.Single(x => x.nodeId == "after_work").dialogueText,
                        "소담 약초 완료 발화·화자 일치");
                    CheckLife(UnityEngine.Object.FindObjectsByType<NpcActor>(FindObjectsSortMode.None).Count(x => x.ActorId == id) == 1,
                        "소담 저장 로드·씬 재진입 후 중복 없음");
                }
                CheckLife(line.Portrait != null, id + " 대화 이력 초상화 연결");
                CheckLife(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None)
                    .Any(x => x.isActiveAndEnabled && x.sprite == line.Portrait), id + " 실제 대화 UI 초상화 표시");
                CaptureRuntimeView();
                File.Copy(ResidentLifeSmokeDirectory + "/PlayMode.png", ResidentLifeSmokeDirectory + "/" + id + "_Dialogue.png", true);
                BeginLifeBacklogCheck(now, 74);
                return;
            }
            if (s_smokeStep == 74)
            {
                CheckLife(!DialogueManager.Instance.IsDialogueActive && !s_lifePortraitActor.IsInteracting(),
                    LifePortraitActors[s_lifePortraitIndex] + " 취소 후 상호작용 반환");
                if (s_lifePortraitActor.ActorId == "Npc_Sodam") CheckWorkbenchResume();
                s_lifePortraitIndex++;
                s_smokeStep = 72;
                return;
            }
            if (s_smokeStep == 75)
            {
                CheckLife(DialogueManager.Instance.IsPaused == s_lifePauseBeforeBacklog,
                    "이력 닫기 트윈 후 대화 정지 상태 복원");
                DialogueManager.Instance.CancelDialogue();
                s_lifeWaitUntil = now + 1;
                s_smokeStep = s_lifeAfterBacklogStep;
                return;
            }
            File.WriteAllLines(ResidentLifeSmokeDirectory + "/Checks.txt", s_lifeChecks);
            FinishSmoke();
        }

        private static void BeginLifeRestoreChecks(double now)
        {
            var flags = (GlobalFlagManager)Svc.Flags;
            flags.LoadFlags(new Dictionary<string, bool>());
            Services.TryGet<IActorSpawnService>(out var spawn);
            var jun = spawn.SpawnActor("NPC_Jun", s_lifeDaonHome + Vector3.forward * 3, Quaternion.identity) as NpcActor;
            CheckLife(jun != null, "준 구조 현장 액터 생성");
            s_lifeJunInstance = jun.GetInstanceID();
            var completed = new Dictionary<string, bool>();
            foreach (string flag in LifeFlags) completed[flag] = true;
            flags.LoadFlags(completed);
            s_lifeWaitUntil = now + 3;
            s_smokeStep = 64;
        }

        private static void AssertLifeState(bool completed)
        {
            foreach (string name in new[] { "Jun", "Mia", "Hazel", "Penny", "Yeonmok", "Sodam" })
            {
                var presentation = FindLifePresentation(name);
                CheckLife(presentation.IsCompletedVisible == completed, name + (completed ? " 완료 상태 복원" : " 초기 상태 복원"));
                var serialized = new SerializedObject(presentation);
                foreach (string field in new[] { "_beforeVisuals", "_afterVisuals" })
                {
                    var visuals = serialized.FindProperty(field);
                    for (int i = 0; i < visuals.arraySize; i++)
                    {
                        var visual = visuals.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                        CheckLife(visual != null && visual.activeSelf == (field == "_afterVisuals" ? completed : !completed), name + " " + field + " 소품 복원");
                    }
                }
                var actor = serialized.FindProperty("_actor").objectReferenceValue as NpcActor;
                var anchor = serialized.FindProperty(completed ? "_afterAnchor" : "_beforeAnchor").objectReferenceValue as Transform;
                if (actor == null || anchor == null) continue;
                Vector3 delta = actor.transform.position - anchor.position;
                CheckLife(Mathf.Abs(delta.y) < 0.35f, name + " 복원 높이·접지 위치");
                delta.y = 0;
                CheckLife(delta.magnitude < 0.6f, name + " 실제 위치 복원");
                Vector3 home = (Vector3)typeof(NpcBrain).GetField("_spawnPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(actor.GetComponent<NpcBrain>());
                CheckLife(Vector3.Distance(home, anchor.position) < 0.05f, name + " 생활 중심 복원");
            }
            var bottle = GameObject.Find("AuthoredEnvironment/LandscapeDistrict").transform.Find("Hazel_Bottle");
            CheckLife(bottle.gameObject.activeSelf == completed, "헤이즐 약병 상태 복원");
            var worker = Svc.ActorQuery.FindActor("Npc_Sodam") as NpcActor;
            CheckLife(worker != null && worker.GetComponent<NpcBrain>().WorkMotion != null,
                "저장 복원 후 소담 작업 모션 참조 유지");
        }

        private static void TickWorkbenchInputCheck(double now)
        {
            if (s_smokeStep == 80)
            {
                var actor = s_lifePortraitActor;
                var brain = actor.GetComponent<NpcBrain>();
                if (actor.Animator.CurrentMotionAsset == brain.WorkMotion && brain.WorkMotion != null)
                {
                    int motion = actor.Animator.CurrentMotionIndex;
                    if (s_workMotionSamples.Add(motion)) CaptureWorkbenchRuntime("Work_" + motion);
                    var prop = actor.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Sodam_HandHerbs(Clone)");
                    if (prop != null && !s_workPropObserved)
                    {
                        Transform hand = actor.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
                        CheckLife(Vector3.Distance(prop.position, hand.position) < 0.15f, "소담 약초 소품 실제 손 부착");
                        s_workPropObserved = true;
                        CaptureWorkbenchRuntime("Work_HandProp");
                    }
                }
                if (now < s_workReviewEnd) return;
                CheckLife(s_workMotionSamples.Contains(0) && s_workMotionSamples.Contains(1) && s_workMotionSamples.Contains(2),
                    "소담 분류·전달·휴식 타임라인 실제 재생");
                CheckLife(s_workPropObserved, "소담 손 소품 타임라인 발화");
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                s_smokeStep = 81;
            }
            else if (s_smokeStep == 81)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                CheckLife(DialogueManager.Instance.IsDialogueActive && s_lifePortraitActor.IsInteracting(), "가상 게임패드 RT 소담 대화 시작");
                CheckLife(!s_lifePortraitActor.GetComponentsInChildren<Transform>().Any(x => x.name == "Sodam_HandHerbs(Clone)"), "대화 진입 시 작업 소품 정리");
                s_smokeStep = 82;
            }
            else if (s_smokeStep == 82)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
                s_smokeStep = 83;
            }
            else if (s_smokeStep == 83)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                CheckLife(UIManager.Instance.IsUIActive(DialogueUIKeys.DialogueBacklog), "가상 게임패드 LB 이력 열기");
                s_smokeStep = 84;
            }
            else if (s_smokeStep == 84)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.East));
                s_smokeStep = 85;
            }
            else if (s_smokeStep == 85)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                CheckLife(!UIManager.Instance.IsUIActive(DialogueUIKeys.DialogueBacklog) && DialogueManager.Instance.IsDialogueActive,
                    "가상 게임패드 B 이력만 닫기·대화 유지");
                s_smokeStep = 86;
            }
            else if (s_smokeStep == 86)
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.East));
                s_smokeStep = 87;
            }
            else
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                CheckLife(!DialogueManager.Instance.IsDialogueActive && !s_lifePortraitActor.IsInteracting(), "가상 게임패드 B 대화 취소");
                CheckWorkbenchResume();
                s_lifePortraitActor.Interact(s_smokePlayer);
                s_smokeStep = 76;
            }
            s_lifeWaitUntil = now + 1.5;
        }

        private static void CheckWorkbenchResume()
        {
            var brain = s_lifePortraitActor.GetComponent<NpcBrain>();
            CheckLife(brain.WorkMotion != null && s_lifePortraitActor.Animator.CurrentMotionAsset == brain.WorkMotion,
                "대화 취소 후 소담 작업 모션 재개");
            CheckLife(Quaternion.Angle(s_lifePortraitActor.transform.rotation, brain.HomeRotation) < 4,
                "대화 후 작업대 방향 복귀");
        }

        private static void CheckWorkbenchFraming()
        {
            var head = s_lifePortraitActor.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            Vector3 viewport = Camera.main.WorldToViewportPoint(head.position);
            CheckLife(viewport.z > 0 && viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.05f && viewport.y < 0.95f,
                "소담 대화 중 머리 프레임 내부 유지: " + viewport.ToString("F3"));
        }

        private static void CaptureWorkbenchRuntime(string name)
        {
            var root = new GameObject("작업장 검수 카메라");
            var camera = root.AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.transform.position = new Vector3(1007.2f, 64.4f, 406f);
            camera.transform.LookAt(new Vector3(1004.4f, 63.2f, 408.15f));
            var target = RenderTexture.GetTemporary(1600, 900, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                UnityEngine.VFX.VFXManager.PrepareCamera(camera);
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                texture.Apply();
                File.WriteAllBytes(ResidentLifeSmokeDirectory + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static NpcLifePresentation FindLifePresentation(string name)
            => GameObject.Find("ResidentLife/" + name).GetComponent<NpcLifePresentation>();

        private static int CountLifeJun() => UnityEngine.Object.FindObjectsByType<NpcActor>(FindObjectsSortMode.None).Count(x => x.ActorId == "NPC_Jun");

        private static void PlaceLifePlayer(Vector3 position)
        {
            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            if (!NavMesh.SamplePosition(position, out var hit, 2, NavMesh.AllAreas)) throw new InvalidOperationException("검증 시작점 NavMesh 누락");
            s_smokePlayer.ActorController.Motor.SetPosition(hit.position);
        }

        private static void PlaceLifePlayerNear(NpcActor actor)
        {
            var motor = s_smokePlayer.ActorController.Motor;
            Terrain terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            foreach (float angle in new[] { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f })
            {
                Vector3 point = actor.transform.position + Quaternion.Euler(0, angle, 0) * actor.transform.forward * 2;
                point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                if (!NavMesh.SamplePosition(point, out var hit, 0.5f, NavMesh.AllAreas)) continue;
                float radius = motor.Capsule.radius;
                var obstacles = Physics.OverlapCapsule(hit.position + Vector3.up * (radius + 0.08f),
                    hit.position + Vector3.up * (motor.Capsule.height - radius), radius,
                    motor.CollidableLayers, QueryTriggerInteraction.Ignore);
                if (obstacles.Any(x => !x.transform.IsChildOf(s_smokePlayer.transform))) continue;
                motor.SetPosition(hit.position);
                CheckLife(true, actor.ActorId + " 접근점 NavMesh·캐릭터 캡슐 여유 확인");
                return;
            }
            throw new InvalidOperationException(actor.ActorId + " 주변에 장애물 없는 대화 접근점이 없습니다.");
        }

        private static void CheckLife(bool success, string message)
        {
            if (!success) throw new InvalidOperationException(message);
            s_lifeChecks.Add(message);
            File.WriteAllLines(ResidentLifeSmokeDirectory + "/Checks.txt", s_lifeChecks);
        }

        private static void CheckLifeHair(NpcActor actor)
        {
            int count = 0;
            foreach (var constraint in actor.GetComponentsInChildren<ParentConstraint>())
            {
                if (!constraint.enabled || !constraint.constraintActive || constraint.sourceCount != 1) continue;
                var source = constraint.GetSource(0);
                if (source.sourceTransform == null || source.weight < 0.999f || constraint.weight < 0.999f) continue;
                Quaternion expected = source.sourceTransform.rotation * Quaternion.Euler(constraint.GetRotationOffset(0));
                Vector3 expectedPosition = source.sourceTransform.position + source.sourceTransform.rotation * constraint.GetTranslationOffset(0);
                CheckLife(Vector3.Distance(expectedPosition, constraint.transform.position) < 0.01f,
                    actor.ActorId + " " + constraint.name + " 실제 리그 위치 추종");
                CheckLife(Quaternion.Angle(expected, constraint.transform.rotation) < 1f,
                    actor.ActorId + " " + constraint.name + " 실제 리그 회전 추종");
                count++;
            }
            CheckLife(count > 0, actor.ActorId + " 헤어 제약 검사 대상 존재");
        }

        private static void WarmLifeView()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            var target = RenderTexture.GetTemporary(1600, 900, 24);
            var previous = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void BeginLifeBacklogCheck(double now, int nextStep)
        {
            var dialogue = DialogueManager.Instance;
            s_lifePauseBeforeBacklog = dialogue.IsPaused;
            var line = dialogue.History[dialogue.History.Count - 1];
            var popup = UIManager.Instance.ShowUI(DialogueUIKeys.DialogueBacklog);
            CheckLife(popup != null && popup.activeInHierarchy && dialogue.IsPaused, "이력 팝업 표시·대화 정지");
            try
            {
                var entry = popup.GetComponentsInChildren<UIDialogueBacklogEntry>().Last();
                var texts = entry.GetComponentsInChildren<TMPro.TMP_Text>();
                CheckLife(texts.Any(x => x.text == line.SpeakerName) && texts.Any(x => x.text == line.RichBody),
                    line.SpeakerName + " 실제 이력 UI 화자·본문 표시");
                CheckLife(entry.GetComponentsInChildren<UnityEngine.UI.Image>().Any(x => x.isActiveAndEnabled && x.sprite == line.Portrait),
                    line.SpeakerName + " 실제 이력 UI 초상화 표시");
            }
            finally { UIManager.Instance.HideUI(DialogueUIKeys.DialogueBacklog); }
            // UI_PopupBase는 닫기 트윈 완료 시 OnHide에서 정지 상태를 돌려준다.
            s_lifeAfterBacklogStep = nextStep;
            s_lifeWaitUntil = now + 1;
            s_smokeStep = 75;
        }
    }
}
