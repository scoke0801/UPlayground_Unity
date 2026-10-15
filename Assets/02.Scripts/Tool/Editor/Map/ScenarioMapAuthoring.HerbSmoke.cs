using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UPlayGround.Data.Quest;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Interaction;
using UPlayGround.Manager;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string HerbSmokeKey = "UPlayGround.ScenarioMap.HerbSmoke";
        private const string HerbQuestId = "quest_sub_hazel_moved_herb";
        private static bool IsHerbSmoke => SessionState.GetBool(HerbSmokeKey, false);
        private static readonly Stack<IEnumerator> HerbSteps = new();
        private static readonly List<string> HerbChecks = new();
        private static double s_herbWait;
        private static int s_herbFrame;

        /// <summary>아이템·완료 플래그 주입 없이 입력·채집·대화와 격리 저장을 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/약초 의뢰 진행 검증")]
        public static void RunHerbRouteSmoke()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            Directory.CreateDirectory(HerbDirectory);
            SessionState.SetBool(HerbSmokeKey, true);
            SessionState.SetString(BaselineCharacterKey, "Raon");
            StartSmoke(ReadLayout().scenePath);
        }

        private static void BeginHerbSmoke(double now)
        {
            s_smokeDeadline = now + 1800;
            s_smokeStep = 1000;
            HerbChecks.Clear();
            HerbSteps.Clear();
            HerbSteps.Push(PlayHerbRoute());
            s_herbWait = now;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            string folder = Path.GetFullPath(HerbDirectory + "/Saves");
            Directory.CreateDirectory(folder);
            typeof(SaveManager).GetField("_saveFolder", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(SaveManager.Instance, folder);
        }

        private static void TickHerbSmoke(double now)
        {
            if (s_herbFrame == Time.frameCount || now < s_herbWait) return;
            s_herbFrame = Time.frameCount;
            WarmLifeView();
            if (HerbSteps.Count == 0) { FinishSmoke(); return; }
            var step = HerbSteps.Peek();
            if (!step.MoveNext()) { HerbSteps.Pop(); return; }
            if (step.Current is IEnumerator child) HerbSteps.Push(child);
            else if (step.Current is float delay) s_herbWait = now + delay;
        }

        private static void CheckHerb(bool value, string label)
        {
            HerbChecks.Add((value ? "PASS " : "FAIL ") + label);
            File.WriteAllLines(HerbDirectory + "/Checks.txt", HerbChecks);
            if (!value) throw new InvalidOperationException(label);
        }

        private static NpcActor Hazel => Svc.ActorQuery.FindActor("NPC_Hazel") as NpcActor;
        private static FlowGraphInteractable HerbBed => GameObject.Find("ScenarioHerbRoute/HerbBedVolume").GetComponent<FlowGraphInteractable>();
        private static GatheringActor Herb(int index) => GameObject.Find("ScenarioHerbRoute").transform.Find("LakeHerb0" + index).GetComponent<GatheringActor>();
        private static bool HasHerbFlag(string suffix) => Svc.Flags.GetFlag("lake.side." + suffix);

        private static IEnumerator PlayHerbRoute()
        {
            if (Environment.GetCommandLineArgs().Contains("-herbPreGatherOnly"))
            {
                yield return PlayPreGatherRoute();
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("-herbResumeCollected"))
            {
                CheckHerb(true, "실제 채집으로 만든 슬롯 3에서 후속 검수 재개: 아이템·플래그 주입 없음");
                yield return FinishSavedHerbRoute();
                yield break;
            }
            CheckHerb(InventoryManager.Instance.GetItemCount(100012) == 0 && !HasHerbFlag("herb_finished"), "새 검수 상태: 약초·완료 플래그 주입 없음");
            CheckHerb(SaveManager.Instance.SaveGameDetailed(0).Succeeded, "격리 슬롯 0: 수주 전 저장");
            yield return WalkToHazel("시작→헤이즐 작업대");
            yield return HerbInteract(Hazel);
            yield return 1f;
            CheckHerb(DialogueManager.Instance.IsDialogueActive, "RT 헤이즐 대화 시작");
            CheckHerb(DialogueManager.Instance.History.Last().SpeakerName == "헤이즐", "헤이즐 화자·본문 기록");
            var portrait = DialogueManager.Instance.PortraitTable.GetPortrait("헤이즐");
            var dialogueUi = UIManager.Instance.GetUI<UPlayGround.UI.UI_Scene_Dialogue>(DialogueUIKeys.MainDialogue);
            CheckHerb(portrait != null && dialogueUi.GetComponentsInChildren<UnityEngine.UI.Image>()
                .Any(x => x.isActiveAndEnabled && x.sprite == portrait), "헤이즐 실제 대화 UI 초상화 표시");
            Vector3 head = Camera.main.WorldToViewportPoint(Hazel.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head).position);
            CheckHerb(head.z > 0 && head.x > 0.05f && head.x < 0.95f && head.y > 0.05f && head.y < 0.95f,
                "헤이즐 대화 카메라 머리 프레임 내부: " + head.ToString("F3"));
            CaptureHerb("HazelOffer");
            yield return HerbButton(GamepadButton.LeftShoulder);
            CheckHerb(UIManager.Instance.IsUIActive(DialogueUIKeys.DialogueBacklog), "LB 이력 열기");
            var entry = UnityEngine.Object.FindObjectsByType<UPlayGround.UI.UIDialogueBacklogEntry>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text == "헤이즐"));
            CheckHerb(entry != null && entry.GetComponentsInChildren<UnityEngine.UI.Image>().Any(x => x.isActiveAndEnabled && x.sprite == portrait),
                "실제 이력 UI 헤이즐 화자·초상화 표시");
            yield return HerbButton(GamepadButton.East);
            yield return HerbButton(GamepadButton.East);
            CheckHerb(!HasHerbFlag("herb_accepted") && !DialogueManager.Instance.IsDialogueActive, "수주 대화 취소: 미수락 유지");
            yield return HerbInteract(Hazel);
            yield return FinishHerbDialogue();
            CheckHerb(QuestManager.Instance.GetQuestStatus(HerbQuestId) == QuestStatus.Active && HasHerbFlag("herb_accepted"), "대화 정상 종료로 수주");
            CheckHerb(SaveManager.Instance.SaveGameDetailed(1).Succeeded, "수주 중 저장");
            CheckHerb(SaveManager.Instance.LoadGame(1), "수주 중 로드");
            yield return 3f;
            yield return WalkHerbTo(HerbBed.transform.position + Vector3.back * 1.2f, "헤이즐→물가 조사");
            yield return HerbInteract(HerbBed);
            yield return 0.5f;
            // 조사 대화는 System 채널이므로 Main UI 취소와 별도로 실제 대화 취소 API를 검사한다.
            DialogueManager.Instance.CancelDialogue(DialogueChannel.System);
            yield return 1f;
            CheckHerb(!HasHerbFlag("herb_bed_examined") && HerbBed.CanInteract(), "조사 System 취소 후 미완료·재시도 가능 (API)");
            yield return HerbInteract(HerbBed);
            yield return 6f;
            CheckHerb(HasHerbFlag("herb_bed_examined") && !HerbBed.CanInteract(), "조사 정상 종료→목표·플래그 기록 및 중복 조사 차단");
            CaptureHerb("HerbBed");
            CheckHerb(SaveManager.Instance.SaveGameDetailed(2).Succeeded, "조사 후 저장");
            yield return GatherHerb(1);
            CheckHerb(InventoryManager.Instance.GetItemCount(100012) is >= 1 and <= 2, "첫 채집 실물 수량 1~2 (아이템 주입 없음)");
            yield return WalkToHazel("수량 부족→헤이즐");
            int insufficient = InventoryManager.Instance.GetItemCount(100012);
            yield return HerbInteract(Hazel);
            yield return FinishHerbDialogue();
            CheckHerb(!HasHerbFlag("herb_finished") && InventoryManager.Instance.GetItemCount(100012) == insufficient, "부족 수량 납품: 소비·완료 없음");
            yield return GatherHerb(2);
            if (InventoryManager.Instance.GetItemCount(100012) < 3) yield return GatherHerb(3);
            CheckHerb(SaveManager.Instance.SaveGameDetailed(3).Succeeded, "채집 진행 중 저장");
            yield return FinishSavedHerbRoute();
        }

        private static IEnumerator FinishSavedHerbRoute()
        {
            CheckHerb(SaveManager.Instance.LoadGame(3), "채집 진행 중 로드");
            yield return 3f;
            yield return ReenterHerbScene();
            CheckHerb(HasHerbFlag("herb_bed_examined") && !Herb(1).gameObject.activeSelf, "진행 중 재진입: 조사·소진 채집물 복원");
            yield return DeliverHerb();
            CheckHerb(SaveManager.Instance.SaveGameDetailed(4).Succeeded, "완료 후 저장");
            int gold = InventoryManager.Instance.Gold;
            int potions = InventoryManager.Instance.GetItemCount(200001);
            long experience = GetHerbTotalExperience();
            yield return HerbInteract(Hazel);
            yield return FinishHerbDialogue();
            CheckHerb(InventoryManager.Instance.Gold == gold && InventoryManager.Instance.GetItemCount(200001) == potions, "완료 후 재대화: 중복 보상 없음");
            CheckHerb(GetHerbTotalExperience() == experience, "완료 후 재대화: 경험치 중복 없음");
            CheckHerb(SaveManager.Instance.LoadGame(4), "완료 후 로드");
            yield return 3f;
            yield return ReenterHerbScene();
            CheckHerb(HasHerbFlag("herb_finished") && QuestManager.Instance.GetQuestStatus(HerbQuestId) == QuestStatus.Completed, "완료 후 재진입: 완료 유지");
            CheckHerb(InventoryManager.Instance.Gold == gold && InventoryManager.Instance.GetItemCount(200001) == potions, "완료 로드·재진입: 보상 중복 없음");
            CheckHerb(FindLifePresentation("Hazel").IsCompletedVisible && FindLifePresentation("Sodam").IsCompletedVisible, "실제 완료에 따른 헤이즐·소담 생활 변화");
            CaptureHerb("Completed");
            yield return PlayPreGatherRoute();
        }

        private static IEnumerator PlayPreGatherRoute()
        {
            // 독립된 두 번째 경로도 저장 복구로 시작하며 아이템·플래그를 직접 변경하지 않는다.
            CheckHerb(SaveManager.Instance.LoadGame(0), "선행 채집 검사용 수주 전 저장 복구");
            yield return 3f;
            yield return ReenterHerbScene();
            yield return GatherHerb(1);
            yield return GatherHerb(2);
            if (InventoryManager.Instance.GetItemCount(100012) < 3) yield return GatherHerb(3);
            yield return WalkToHazel("선행 채집→수주");
            yield return HerbInteract(Hazel);
            yield return FinishHerbDialogue();
            var quest = QuestManager.Instance.GetActiveQuestRuntime(HerbQuestId);
            CheckHerb(quest.ObjectiveProgress["obj_gather_forest_herb"] == 3, "선행 채집을 수주 시 인정");
            int herbs = InventoryManager.Instance.GetItemCount(100012);
            yield return HerbInteract(Hazel);
            yield return FinishHerbDialogue();
            CheckHerb(!HasHerbFlag("herb_finished") && InventoryManager.Instance.GetItemCount(100012) == herbs, "조사 전 납품 차단·선행 채집 보존");
            yield return WalkHerbTo(HerbBed.transform.position + Vector3.back * 1.2f, "선행 채집 후 조사");
            yield return HerbInteract(HerbBed);
            yield return 6f;
            yield return DeliverHerb();
        }

        private static IEnumerator DeliverHerb()
        {
            yield return WalkToHazel("채집→납품");
            int herbs = InventoryManager.Instance.GetItemCount(100012);
            int gold = InventoryManager.Instance.Gold;
            int potions = InventoryManager.Instance.GetItemCount(200001);
            long experience = GetHerbTotalExperience();
            yield return HerbInteract(Hazel);
            yield return FinishHerbDialogue();
            CheckHerb(QuestManager.Instance.GetQuestStatus(HerbQuestId) == QuestStatus.Completed && HasHerbFlag("herb_finished"), "납품 대화로 완료·생활 플래그 확정");
            CheckHerb(InventoryManager.Instance.GetItemCount(100012) == herbs - 3, "납품 약초 정확히 3개 소비");
            CheckHerb(InventoryManager.Instance.Gold == gold + 80 && InventoryManager.Instance.GetItemCount(200001) == potions + 3, "Quest 단일 보상: 80골드·회복약 3개");
            var party = PartyManager.Instance;
            var config = (UPlayGround.Data.Party.PartyConfigSO)typeof(PartyManager)
                .GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(party);
            float multiplier = (config != null ? config.battleMemberExpRate : 1f)
                * party.GetCharacterMultiplier(party.ActiveCharacterType, UPlayGround.Data.Ability.PassiveModifierType.ExperienceGain);
            long expected = UPlayGround.Data.Ability.PassiveModifierCalculator.CalculateExperience(90, multiplier);
            CheckHerb(GetHerbTotalExperience() == experience + expected,
                "Quest 경험치 기본 90·캐릭터 보정 " + multiplier + " / 기대=" + expected + " / 실제 증가=" + (GetHerbTotalExperience() - experience));
        }

        private static long GetHerbTotalExperience()
        {
            var party = PartyManager.Instance;
            var type = party.ActiveCharacterType;
            long total = party.GetExp(type);
            var required = typeof(PartyManager).GetMethod("RequiredExpOf", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int level = 1; level < party.GetLevel(type); level++)
                total += (long)required.Invoke(party, new object[] { type, level });
            return total;
        }

        private static IEnumerator ReenterHerbScene()
        {
            while (TryDismissTraversalGuide()) yield return .5f;
            File.AppendAllText(HerbDirectory + "/SceneReentry.txt",
                "before: position=" + s_smokePlayer.transform.position + " timeScale=" + Time.timeScale
                + " guide=" + UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen()
                + " dialogue=" + DialogueManager.Instance.IsDialogueActive + "\n");
            UPlayGround.Manager.SceneManager.Instance.LoadScene("LakeOfLifeScenario");
            yield return 3f;
            double deadline = EditorApplication.timeSinceStartup + 120;
            while (UPlayGround.Manager.SceneManager.Instance.LoadState != SceneLoadState.Completed)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new TimeoutException("약초 씬 재진입: " + UPlayGround.Manager.SceneManager.Instance.LoadState);
                yield return null;
            }
            s_smokePlayer = PartyManager.Instance.ActiveCharacter;
            yield return 2f;
            yield return WalkHerbTo(s_smoke.walkEnd, "재진입 시작 통로");
        }

        private static IEnumerator GatherHerb(int index)
        {
            var herb = Herb(index);
            yield return WalkHerbTo(herb.transform.position + Vector3.back * 1.1f, "채집 " + index);
            yield return HerbInteract(herb);
            double deadline = EditorApplication.timeSinceStartup + 30;
            while (herb != null && herb.gameObject.activeSelf && EditorApplication.timeSinceStartup < deadline) yield return null;
            CheckHerb(herb == null || !herb.gameObject.activeSelf, "가상 RT→채집 모션→획득·소진 " + index);
            yield return 1f;
        }

        private static IEnumerator HerbInteract(IInteractable target)
        {
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 1f;
            CheckHerb(ReferenceEquals(ActorSvc.Objects.InteractionHandler.CurrentClosestInteractable, target), "실제 근접 탐색 대상 일치: " + target.GetInteractionTransform().name
                + " / 실제=" + ActorSvc.Objects.InteractionHandler.CurrentClosestInteractable?.GetInteractionTransform().name);
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
            yield return 0.25f;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 0.5f;
        }

        private static IEnumerator HerbButton(GamepadButton button)
        {
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(button));
            yield return 0.25f;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 0.75f;
        }

        private static IEnumerator FinishHerbDialogue()
        {
            double deadline = EditorApplication.timeSinceStartup + 60;
            while (DialogueManager.Instance.IsDialogueActive && EditorApplication.timeSinceStartup < deadline)
            {
                yield return HerbButton(GamepadButton.South);
            }
            CheckHerb(!DialogueManager.Instance.IsDialogueActive, "가상 A 대화 정상 종료");
            yield return 1f;
        }

        private static IEnumerator WalkToHazel(string label)
        {
            yield return WalkHerbTo(Hazel.transform.position + Vector3.back * 1.5f, label);
        }

        private static IEnumerator WalkHerbTo(Vector3 target, string label)
        {
            var path = new NavMeshPath();
            CheckHerb(NavMesh.SamplePosition(target, out var hit, 2, NavMesh.AllAreas)
                && NavMesh.CalculatePath(s_smokePlayer.transform.position, hit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete, "보행 경로 확보: " + label);
            foreach (var corner in path.corners.Skip(1))
            {
                float best = float.PositiveInfinity;
                double progress = EditorApplication.timeSinceStartup;
                while (true)
                {
                    if (TryDismissTraversalGuide())
                    {
                        progress = EditorApplication.timeSinceStartup;
                        yield return null;
                        continue;
                    }
                    Vector3 direction = Vector3.ProjectOnPlane(corner - s_smokePlayer.transform.position, Vector3.up);
                    float distance = direction.magnitude;
                    if (distance < 0.6f) break;
                    if (distance < best - 0.1f) { best = distance; progress = EditorApplication.timeSinceStartup; }
                    if (EditorApplication.timeSinceStartup - progress > 15)
                        throw new InvalidOperationException("보행 정체 " + label + " at " + s_smokePlayer.transform.position + " target " + corner);
                    Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    direction.Normalize();
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState { leftStick = new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)) });
                    yield return null;
                }
            }
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            yield return 0.75f;
            CheckHerb(s_smokePlayer.ActorController.Motor.GroundingStatus.IsStableOnGround, "가상 보행 도착·접지: " + label + " " + s_smokePlayer.transform.position.ToString("F2"));
        }

        private static void CaptureHerb(string name)
        {
            CaptureRuntimeView();
            File.Copy(HerbDirectory + "/PlayMode.png", HerbDirectory + "/" + name + ".png", true);
        }
    }
}
