using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.EditorTools;
using UPlayGround.Dialogue;
using UPlayGround.Gameplay.Encounter;
using UPlayGround.Manager;
using UPlayGround.Data.Story;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string FirstRecruitmentSmokeKey = "UPlayGround.Scenario.P04Smoke";
        private const string FirstRecruitmentRouteKey = "UPlayGround.Scenario.P04Route";
        private static string FirstRecruitmentRoute => SessionState.GetString(FirstRecruitmentRouteKey, "normal");
        private static bool IsFirstRecruitmentSmoke => SessionState.GetBool(FirstRecruitmentSmokeKey, false);
        private static string FirstRecruitmentSmokeDirectory => FirstRecruitmentDirectory + "/" + SessionState.GetString(BaselineCharacterKey, "Raon")
            + (FirstRecruitmentRoute == "normal" ? "" : "_" + FirstRecruitmentRoute);
        private static readonly List<string> FirstRecruitmentChecks = new();

        /// <summary>완료 상태 주입 없이 가상 패드로 첫 영입 경로를 시도하고 실패 지점을 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P04 실제 진행 검증")]
        public static void RunFirstRecruitmentSmoke()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            string character = "Raon";
            string route = "normal";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "-p04Character") character = args[i + 1];
                if (args[i] == "-p04Route") route = args[i + 1];
            }
            if (character != "Raon" && character != "Arin") throw new InvalidOperationException("Raon 또는 Arin을 지정하세요.");
            if (route != "normal" && route != "early-clue" && route != "early-hwarin") throw new InvalidOperationException("P04 경로 이름을 확인하세요.");
            SessionState.SetString(FirstRecruitmentRouteKey, route);
            SessionState.SetString(BaselineCharacterKey, character);
            SessionState.SetBool(FirstRecruitmentSmokeKey, true);
            Directory.CreateDirectory(FirstRecruitmentSmokeDirectory);
            StartSmoke(ReadLayout().scenePath);
        }

        private static void BeginFirstRecruitmentSmoke(double now)
        {
            s_smokeDeadline = now + 1200;
            s_smokeStep = 1000;
            FirstRecruitmentChecks.Clear();
            HerbSteps.Clear();
            HerbSteps.Push(PlayFirstRecruitment());
            s_herbWait = now;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            string saves = Path.GetFullPath(FirstRecruitmentSmokeDirectory + "/Saves");
            Directory.CreateDirectory(saves);
            typeof(SaveManager).GetField("_saveFolder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(SaveManager.Instance, saves);
        }

        private static void CheckFirstRecruitment(bool passed, string label)
        {
            FirstRecruitmentChecks.Add((passed ? "PASS " : "FAIL ") + label);
            File.WriteAllLines(FirstRecruitmentSmokeDirectory + "/Checks.txt", FirstRecruitmentChecks);
            if (!passed) throw new InvalidOperationException(label);
        }

        private static IEnumerator PlayFirstRecruitment()
        {
            var protagonist = Svc.Party.StoryProtagonistType;
            CheckFirstRecruitment(PartyManager.Instance.Roster.Count == 1, "새 상태 단일 시작 파티");
            var guide = Svc.ActorQuery.FindActor("NPC_Story_Guide") as NpcActor;
            CheckFirstRecruitment(guide != null, "실제 안내인 조회");
            if (FirstRecruitmentRoute == "normal")
            {
            yield return WalkFirstRecruitment(guide.transform.position + guide.transform.forward * 1.5f, "도착→안내인");
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
            yield return .3f;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 1f;
            CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "RT 안내 대화 시작");
            yield return HerbButton(GamepadButton.East);
            CheckFirstRecruitment(!Svc.Flags.GetFlag("lake.story.guide_briefed"), "안내 취소는 완료 아님");
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
            yield return .3f;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 1f;
            yield return FinishFirstRecruitmentDialogue();
            CheckFirstRecruitment(Svc.Flags.GetFlag("lake.story.guide_briefed"), "안내 정상 종료");
            }
            var road = GameObject.Find("ScenarioFirstCombat").GetComponentInChildren<MonsterActor>();
            yield return WalkFirstRecruitment(road.transform.position + Vector3.right * 4, "안내→첫 전투");
            CaptureRuntimeView();
            File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png", FirstRecruitmentSmokeDirectory + "/FirstCombat.png", true);
            yield return FightFirstRecruitment(() => road != null && road.IsAlive(), () => road);
            CheckFirstRecruitment(road == null || !road.IsAlive(), "E00 실제 공격 결과");
            var root = GameObject.Find("[STORY] RecruitmentEncounter_HwarinRescue");
            var clue = root.transform.Find("StoryClue_RedCloth");
            if (FirstRecruitmentRoute != "early-hwarin")
            {
            yield return WalkFirstRecruitment(clue.position, "첫 전투→단서");
                yield return 3f;
                double controlDeadline = EditorApplication.timeSinceStartup + 15;
                while (s_smokePlayer.ActorController.CurrentState.StateId == UPlayGround.State.ActorStateId.Stop
                    || Svc.CinematicStage?.IsActive == true)
                {
                    TryDismissTraversalGuide();
                    if (EditorApplication.timeSinceStartup > controlDeadline) throw new TimeoutException("단서 앞 조작권 복귀 실패");
                    yield return .2f;
                }
            while (TryDismissTraversalGuide()) yield return .5f;
            CaptureRuntimeView();
            File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png", FirstRecruitmentSmokeDirectory + "/Clue.png", true);
            File.WriteAllText(FirstRecruitmentSmokeDirectory + "/ClueState.txt",
                "player=" + s_smokePlayer.transform.position + " clue=" + clue.position
                + " available=" + clue.GetComponent<UPlayGround.Gameplay.Interaction.FlowGraphInteractable>().CanInteract()
                + " flag=" + Svc.Flags.GetFlag("lake.story.red_cloth_examined")
                + " target=" + ActorSvc.Objects.InteractionHandler.CurrentClosestInteractable?.GetInteractionTransform().name
                + " guide=" + UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen()
                + " state=" + s_smokePlayer.ActorController.CurrentState.StateId
                + " break=" + s_smokePlayer.GetCombat()?.FindSpecialBreakAttackTarget()?.name
                + " phase=" + Svc.RecruitmentEncounters.GetPhase(root.GetComponent<RecruitmentEncounterAnchor>().EncounterId));
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
            yield return .3f;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 1f;
            if (!DialogueManager.Instance.IsDialogueActive && TryDismissTraversalGuide())
            {
                yield return 1f;
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                yield return .3f;
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                yield return 1f;
            }
                if (!DialogueManager.Instance.IsDialogueActive)
                {
                    File.AppendAllText(FirstRecruitmentSmokeDirectory + "/ClueState.txt",
                        "\nAfterInput state=" + s_smokePlayer.ActorController.CurrentState.StateId
                        + " flag=" + Svc.Flags.GetFlag("lake.story.red_cloth_examined")
                        + " can=" + s_smokePlayer.CanStartInteraction());
                    yield return 3f;
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                    yield return 1f;
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                    yield return 2f;
                }
                CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "RT 단서 관찰 대화 시작");
            if (FirstRecruitmentRoute == "early-clue")
            {
                yield return HerbButton(GamepadButton.East);
                CheckFirstRecruitment(!DialogueManager.Instance.IsDialogueActive
                    && !Svc.Flags.GetFlag("lake.story.red_cloth_examined")
                    && clue.GetComponent<UPlayGround.Gameplay.Interaction.FlowGraphInteractable>().CanInteract(),
                    "단서 대화 취소·미완료·재시도 가능");
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                yield return .3f;
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                yield return 1f;
                CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "RT 단서 재시도");
            }
            yield return FinishFirstRecruitmentDialogue();
            CheckFirstRecruitment(Svc.Flags.GetFlag("lake.story.red_cloth_examined"), "실제 단서 조사");
            }
            yield return WalkFirstRecruitment(root.transform.position, "단서→공동 전투");
            var anchor = root.GetComponent<RecruitmentEncounterAnchor>();
            var hostiles = root.GetComponentsInChildren<RecruitmentEncounterParticipant>(true)
                .Where(item => item.Role == RecruitmentEncounterRole.Hostile).ToArray();
            if (FirstRecruitmentRoute == "early-hwarin")
            {
                yield return 2f;
                yield return WalkFirstRecruitment(root.transform.position + Vector3.right * 22, "전투 이탈");
                yield return WalkFirstRecruitment(root.transform.position, "전투 재진입");
                CheckFirstRecruitment(Svc.RecruitmentEncounters.GetPhase(anchor.EncounterId) != RecruitmentEncounterPhase.Dormant,
                    "이탈·재진입 후 조우 유지");
            }
            yield return FightFirstRecruitment(
                () => Svc.RecruitmentEncounters.GetPhase(anchor.EncounterId) != RecruitmentEncounterPhase.CombatResolved,
                () => hostiles.Select(item => item.Actor).FirstOrDefault(actor => actor != null && actor.gameObject.activeInHierarchy && actor.IsAlive()));
            yield return 8f;
            CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "공동 전투 후 필수 대화 시작");
            CaptureRuntimeView();
            File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png", FirstRecruitmentSmokeDirectory + "/HwarinDialogue.png", true);
            yield return HerbButton(GamepadButton.East);
            CheckFirstRecruitment(Svc.RecruitmentEncounters.GetPhase(anchor.EncounterId) == RecruitmentEncounterPhase.CombatResolved
                && PartyManager.Instance.Roster.Count == 1 && !DialogueManager.Instance.IsDialogueActive,
                "영입 대화 취소: 전투 승리 보존·영입 보류");
            while (TryDismissTraversalGuide()) yield return .5f;
            CheckFirstRecruitment(SaveManager.Instance.SaveGameDetailed(1).Succeeded, "영입 전 격리 저장");
            RecordFirstRecruitmentControl("영입 전 로드 직전");
            CheckFirstRecruitment(SaveManager.Instance.LoadGame(1), "영입 전 로드");
            yield return 8f;
            RecordFirstRecruitmentControl("영입 전 로드 8초 후");
            if (!DialogueManager.Instance.IsDialogueActive)
            {
                yield return WalkFirstRecruitment(root.transform.position + Vector3.back * 18, "취소 후 거리 확보");
                yield return WalkFirstRecruitment(root.transform.position, "영입 재접근");
                yield return 8f;
            }
            CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "영입 전 로드·재시도 대화");
            if (FirstRecruitmentRoute == "early-hwarin")
            {
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                yield return .2f;
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                yield return 5f;
                CheckFirstRecruitment(!DialogueManager.Instance.IsDialogueActive, "RT 영입 대화 스킵 종료");
            }
            else yield return FinishFirstRecruitmentDialogue();
            CheckFirstRecruitment(Svc.Flags.GetFlag("lake.story.honoka_joined") && PartyManager.Instance.Roster.Count == 2, "정상 종료 영입·파티 2명");
            yield return 2f;
            CheckFirstRecruitment(!UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen(), "영입 직후 교체 팝업 보류");
            yield return WalkFirstRecruitment(clue.position, "영입 후 안전 구간 이동");
            yield return 2f;
            while (TryDismissTraversalGuide()) yield return .5f;
            var guideSave = new UPlayGround.Data.Save.GameSaveData();
            GameGuideManager.Instance.ExportSaveData(guideSave);
            CheckFirstRecruitment(guideSave.firstTimeGuide.shownGuideIds.Contains("Guide.Companion"), "이동 뒤 첫 동료 안내 표시");
            if (FirstRecruitmentRoute != "normal")
            {
                CheckFirstRecruitment(!Svc.Flags.GetFlag("lake.story.guide_briefed"), "안내인을 건너뛴 실제 선행 영입");
                yield return WalkFirstRecruitment(guide.transform.position + guide.transform.forward, "선행 영입 후 안내인 복귀");
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                yield return .3f;
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                yield return 1f;
                CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "선행 영입 후 안내 대화");
                yield return FinishFirstRecruitmentDialogue();
                CheckFirstRecruitment(Svc.Flags.GetFlag("lake.story.guide_briefed"), "선행 영입 후 안내 완료");
                CheckFirstRecruitment(Svc.QuestFlow.GetQuestStatus("quest_sub_lake_rescue_honoka")
                    == UPlayGround.Data.Quest.QuestStatus.Completed, "늦게 열린 Q02에 조사·영입 사실 반영");
            }
            yield return HerbButton(GamepadButton.DpadRight);
            yield return 3f;
            CheckFirstRecruitment(Svc.Party.StoryProtagonistType == protagonist && PartyManager.Instance.ActiveCharacterType != protagonist,
                "실제 패드 교체 후 원래 시점 보존");
            int rewardGold = InventoryManager.Instance.Gold;
            long rewardExperience = GetHerbTotalExperience();
            CheckFirstRecruitment(SaveManager.Instance.SaveGameDetailed(2).Succeeded, "영입 후 격리 저장");
            CheckFirstRecruitment(SaveManager.Instance.LoadGame(2), "영입 후 로드");
            yield return 5f;
            CheckFirstRecruitment(PartyManager.Instance.Roster.Count == 2 && Svc.Party.StoryProtagonistType == protagonist,
                "영입 후 로드 파티·시점 보존");
            var otherStart = protagonist == UPlayGround.Data.EnumType.CharacterActorType.Raon
                ? UPlayGround.Data.EnumType.CharacterActorType.Arin : UPlayGround.Data.EnumType.CharacterActorType.Raon;
            CheckFirstRecruitment(!PartyManager.Instance.Roster.Contains(otherStart), "선택하지 않은 시작 캐릭터 미지급");
            UPlayGround.Manager.SceneManager.Instance.LoadScene("LakeOfLifeScenario");
            yield return 10f;
            double sceneDeadline = EditorApplication.timeSinceStartup + 90;
            while (UPlayGround.Manager.SceneManager.Instance.LoadState != SceneLoadState.Completed
                && EditorApplication.timeSinceStartup < sceneDeadline) yield return .5f;
            CheckFirstRecruitment(UPlayGround.Manager.SceneManager.Instance.LoadState == SceneLoadState.Completed, "영입 후 씬 재진입");
            s_smokePlayer = PartyManager.Instance.ActiveCharacter;
            root = GameObject.Find("[STORY] RecruitmentEncounter_HwarinRescue");
            yield return 3f;
            CheckFirstRecruitment(PartyManager.Instance.Roster.Count == 2 && !DialogueManager.Instance.IsDialogueActive
                && Svc.Flags.GetFlag("lake.story.honoka_joined")
                && Svc.RecruitmentEncounters.GetPhase(root.GetComponent<RecruitmentEncounterAnchor>().EncounterId) == RecruitmentEncounterPhase.Completed,
                "재진입 완료 조우 복원·중복 영입·대화 없음");
            CheckFirstRecruitment(InventoryManager.Instance.Gold == rewardGold && GetHerbTotalExperience() == rewardExperience,
                "영입 후 저장·재진입 재화·경험치 중복 없음");
        }

        private static IEnumerator FinishFirstRecruitmentDialogue()
        {
            double deadline = EditorApplication.timeSinceStartup + 90;
            while (DialogueManager.Instance.IsDialogueActive && EditorApplication.timeSinceStartup < deadline)
                yield return HerbButton(GamepadButton.South);
            CheckFirstRecruitment(!DialogueManager.Instance.IsDialogueActive, "A 입력 대화 정상 종료");
            File.WriteAllLines(FirstRecruitmentSmokeDirectory + "/DialogueHistory.txt",
                DialogueManager.Instance.History.Select(line => line.SpeakerName + " | " + line.RichBody));
            yield return 1f;
        }

        private static IEnumerator FightFirstRecruitment(Func<bool> isPending, Func<MonsterActor> target)
        {
            double deadline = EditorApplication.timeSinceStartup + 180;
            double nextDodge = 0;
            while (isPending() && EditorApplication.timeSinceStartup < deadline)
            {
                if (TryDismissTraversalGuide()) { yield return .5f; continue; }
                if (!s_smokePlayer.IsAlive()) throw new InvalidOperationException("전투 중 플레이어 사망");
                var enemy = target();
                if (enemy != null)
                {
                    Vector3 offset = Vector3.ProjectOnPlane(enemy.transform.position - s_smokePlayer.transform.position, Vector3.up);
                    if (enemy.ActorController.CurrentState.StateId == UPlayGround.State.ActorStateId.Attack
                        && offset.sqrMagnitude < 25 && EditorApplication.timeSinceStartup >= nextDodge)
                    {
                        Vector3 away = -offset.normalized;
                        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                        InputSystem.QueueStateEvent(s_smokePad, new GamepadState
                        {
                            leftStick = new Vector2(Vector3.Dot(away, Vector3.Cross(Vector3.up, forward)), Vector3.Dot(away, forward))
                        }.WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder));
                        yield return .25f;
                        InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                        yield return .75f;
                        nextDodge = EditorApplication.timeSinceStartup + 1.5;
                        continue;
                    }
                    QueueFirstRecruitmentMovement(offset.sqrMagnitude > 3 ? offset : Vector3.zero);
                    yield return .2f;
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.West));
                    yield return .1f;
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                    yield return .15f;
                }
                else yield return .5f;
            }
            CheckFirstRecruitment(!isPending(), "완료·처치 주입 없는 전투 해결");
        }

        private static IEnumerator WalkFirstRecruitment(Vector3 target, string label)
        {
            var path = new NavMeshPath();
            CheckFirstRecruitment(NavMesh.SamplePosition(target, out var hit, 2, NavMesh.AllAreas)
                && NavMesh.CalculatePath(s_smokePlayer.transform.position, hit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete, label + " 경로");
            foreach (var corner in path.corners.Skip(1))
            {
                double deadline = EditorApplication.timeSinceStartup + 90;
                while (Vector3.ProjectOnPlane(corner - s_smokePlayer.transform.position, Vector3.up).magnitude > .6f)
                {
                    if (DialogueManager.Instance.IsDialogueActive)
                    {
                        InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                        yield break;
                    }
                    if (EditorApplication.timeSinceStartup > deadline)
                    {
                        RecordFirstRecruitmentControl(label + " 정체");
                        throw new TimeoutException(label + " 보행 정체: " + s_smokePlayer.transform.position);
                    }
                    if (!TryDismissTraversalGuide()) QueueFirstRecruitmentMovement(corner - s_smokePlayer.transform.position);
                    yield return null;
                }
            }
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return .8f;
        }

        private static void QueueFirstRecruitmentMovement(Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState { leftStick = new Vector2(
                Vector3.Dot(direction, Vector3.Cross(Vector3.up, forward)), Vector3.Dot(direction, forward)) });
        }

        private static void RecordFirstRecruitmentControl(string label)
        {
            File.AppendAllText(FirstRecruitmentSmokeDirectory + "/Control.txt",
                label + " | state=" + s_smokePlayer.ActorController.CurrentState.StateId
                + " staged=" + s_smokePlayer.IsDialogueStaged
                + " sameActor=" + (s_smokePlayer == PartyManager.Instance.ActiveCharacter)
                + " dialogue=" + DialogueManager.Instance.IsDialogueActive
                + " dialogueUI=" + UPlayGround.Manager.UIManager.Instance.IsUIActive(DialogueUIKeys.MainDialogue)
                + " cinematic=" + Svc.CinematicStage?.IsActive
                + " paused=" + Svc.GameTime?.IsPaused + " timeScale=" + Time.timeScale
                + " position=" + s_smokePlayer.transform.position + "\n");
        }
    }
}
