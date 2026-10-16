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
        private static string FirstRecruitmentSmokeDirectory => SessionState.GetString("UPlayGround.Scenario.P04Output", FirstRecruitmentDirectory) + "/" + SessionState.GetString(BaselineCharacterKey, "Raon")
            + (FirstRecruitmentRoute == "normal" ? "" : "_" + FirstRecruitmentRoute);
        private static readonly List<string> FirstRecruitmentChecks = new();
        private static double s_effectObservationTime;
        private static int s_levelUpFrames;

        /// <summary>완료 상태 주입 없이 가상 패드로 첫 영입 경로를 시도하고 실패 지점을 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P04 실제 진행 검증")]
        public static void RunFirstRecruitmentSmoke()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            string character = "Raon";
            string route = "normal";
            var args = Environment.GetCommandLineArgs();
            SessionState.SetBool("UPlayGround.Scenario.P04SwapBeforeGuide", args.Contains("-p04SwapBeforeGuide"));
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "-p04Character") character = args[i + 1];
                if (args[i] == "-p04Route") route = args[i + 1];
                if (args[i] == "-p04Output") SessionState.SetString("UPlayGround.Scenario.P04Output", args[i + 1]);
            }
            if (character != "Raon" && character != "Arin") throw new InvalidOperationException("Raon 또는 Arin을 지정하세요.");
            if (route != "normal" && route != "early-clue" && route != "early-hwarin" && route != "input-probe" && route != "effects-probe" && route != "guide-load-probe") throw new InvalidOperationException("P04 경로 이름을 확인하세요.");
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
            s_effectObservationTime = 0;
            s_levelUpFrames = 0;
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
            if (FirstRecruitmentRoute == "guide-load-probe")
            {
                CheckFirstRecruitment(SaveManager.Instance.SaveGameDetailed(1).Succeeded && SaveManager.Instance.LoadGame(1), "격리 API 저장·로드");
                yield return 2f;
                var guideData = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.UI.GuidePopupDataSO>("Assets/10.Datas/Guide/GuidePopupData_FirstCombat.asset");
                UPlayGround.UI.Guide.GuidePopupRuntime.Open(guideData);
                yield return .4f;
                RecordFirstRecruitmentControl("영상 안내 API 표시 직후 씬 재진입 요청 전");
                CheckFirstRecruitment(UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen(), "영상 안내 표시");
                UPlayGround.Manager.SceneManager.Instance.LoadScene("LakeOfLifeScenario");
                double loadDeadline = EditorApplication.timeSinceStartup + 120;
                yield return 3f;
                while (UPlayGround.Manager.SceneManager.Instance.LoadState != SceneLoadState.Completed
                    && EditorApplication.timeSinceStartup < loadDeadline) yield return .5f;
                File.AppendAllText(FirstRecruitmentSmokeDirectory + "/Control.txt", "영상 재진입 판정 | load="
                    + UPlayGround.Manager.SceneManager.Instance.LoadState + " timeScale=" + Time.timeScale
                    + " guide=" + UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen() + "\n");
                CheckFirstRecruitment(UPlayGround.Manager.SceneManager.Instance.LoadState == SceneLoadState.Completed, "영상 안내 중 씬 재진입 완료");
                s_smokePlayer = PartyManager.Instance.ActiveCharacter;
                RecordFirstRecruitmentControl("영상 안내 중 씬 재진입 후");
                CheckFirstRecruitment(Svc.GameTime?.IsPaused == false && !UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen()
                    && InputManager.Instance.CurrentLayer == UPlayGround.InputDefine.InputLayer.Level_0, "영상 안내 종료·일시정지·입력 레이어 복원");
                yield break;
            }
            if (FirstRecruitmentRoute == "effects-probe")
            {
                yield return VerifyFirstRecruitmentTrails("초기");
                // 진행과 분리한 FX DB 호출 검사. 경험치·완료 플래그는 주입하지 않는다.
                var effect = GameObjectManager.Instance.ShowFX("LevelUp", s_smokePlayer.transform.position,
                    Quaternion.identity, s_smokePlayer.transform, 3f);
                CheckFirstRecruitment(effect != null, "LevelUp FX DB 실제 생성");
                yield return .35f;
                CaptureRuntimeView();
                File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png", FirstRecruitmentSmokeDirectory + "/LevelUpPreview.png", true);
                yield return 3f;
                CheckFirstRecruitment(effect == null, "LevelUp 단발 효과 수명 종료");
                yield break;
            }
            if (FirstRecruitmentRoute == "input-probe")
            {
                // 진행 검증과 분리한 차단 API 회귀: 완료 플래그나 위치를 바꾸지 않는다.
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { leftStick = Vector2.up });
                yield return .5f;
                InputManager.Instance.SuppressPlayerActionInputBriefly(.1f, 1);
                double heldUntil = EditorApplication.timeSinceStartup + .5;
                while (EditorApplication.timeSinceStartup < heldUntil)
                {
                    InputSystem.QueueStateEvent(s_smokePad, new GamepadState { leftStick = Vector2.up });
                    yield return null;
                }
                RecordFirstRecruitmentControl("짧은 입력 차단 종료·스틱 유지");
                var move = (Vector2)typeof(PlayerActor).GetField("_currentMoveInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(s_smokePlayer);
                CheckFirstRecruitment(move.sqrMagnitude > .5f, "차단 종료 후 유지 중인 이동 입력 복구");
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                yield break;
            }
            var protagonist = Svc.Party.StoryProtagonistType;
            yield return VerifyFirstRecruitmentTrails("시작");
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
            yield return HerbButton(GamepadButton.South);
            yield return 1f;
            CaptureRuntimeView();
            File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png", FirstRecruitmentSmokeDirectory + "/HwarinReply.png", true);
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
            var immediateGuideSave = new UPlayGround.Data.Save.GameSaveData();
            GameGuideManager.Instance.ExportSaveData(immediateGuideSave);
            CheckFirstRecruitment(immediateGuideSave.firstTimeGuide.shownGuideIds.Contains("Guide.Companion"), "영입 직후 첫 동료 안내 표시");
            while (TryDismissTraversalGuide()) yield return .5f;
            yield return WalkFirstRecruitment(clue.position, "영입 후 안전 구간 이동");
            yield return 2f;
            while (TryDismissTraversalGuide()) yield return .5f;
            var guideSave = new UPlayGround.Data.Save.GameSaveData();
            GameGuideManager.Instance.ExportSaveData(guideSave);
            CheckFirstRecruitment(guideSave.firstTimeGuide.shownGuideIds.Contains("Guide.Companion"), "이동 뒤 첫 동료 안내 표시 사실 유지");
            if (FirstRecruitmentRoute != "normal")
            {
                CheckFirstRecruitment(!Svc.Flags.GetFlag("lake.story.guide_briefed"), "안내인을 건너뛴 실제 선행 영입");
                bool swapBeforeGuide = SessionState.GetBool("UPlayGround.Scenario.P04SwapBeforeGuide", false);
                if (swapBeforeGuide)
                {
                    yield return HerbButton(GamepadButton.DpadRight);
                    yield return 3f;
                    s_smokePlayer = PartyManager.Instance.ActiveCharacter;
                    CheckFirstRecruitment(PartyManager.Instance.ActiveCharacterType != protagonist, "안내 대화 전 실제 화린 교체");
                }
                int historyStart = DialogueManager.Instance.History.Count;
                yield return WalkFirstRecruitment(guide.transform.position + guide.transform.forward, "선행 영입 후 안내인 복귀");
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState { rightTrigger = 1 });
                yield return .3f;
                InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
                yield return 1f;
                CheckFirstRecruitment(DialogueManager.Instance.IsDialogueActive, "선행 영입 후 안내 대화");
                yield return FinishFirstRecruitmentDialogue();
                if (swapBeforeGuide)
                {
                    string expectedSpeaker = SessionState.GetString(BaselineCharacterKey, "Raon") == "Raon" ? "라온" : "아린";
                    CheckFirstRecruitment(DialogueManager.Instance.History.Skip(historyStart).Any(line => line.SpeakerName == expectedSpeaker),
                        "화린 교체 후 실제 안내 대사 화자 " + expectedSpeaker);
                    CheckFirstRecruitment(PartyManager.Instance.ActiveCharacterType != protagonist, "주인공 대화 종료 뒤 화린 복원");
                    s_smokePlayer = PartyManager.Instance.ActiveCharacter;
                }
                CheckFirstRecruitment(Svc.Flags.GetFlag("lake.story.guide_briefed"), "선행 영입 후 안내 완료");
                CheckFirstRecruitment(Svc.QuestFlow.GetQuestStatus("quest_sub_lake_rescue_honoka")
                    == UPlayGround.Data.Quest.QuestStatus.Completed, "늦게 열린 Q02에 조사·영입 사실 반영");
            }
            if (!SessionState.GetBool("UPlayGround.Scenario.P04SwapBeforeGuide", false))
                yield return HerbButton(GamepadButton.DpadRight);
            yield return 3f;
            CheckFirstRecruitment(Svc.Party.StoryProtagonistType == protagonist && PartyManager.Instance.ActiveCharacterType != protagonist,
                "실제 패드 교체 후 원래 시점 보존");
            yield return VerifyFirstRecruitmentTrails("교체후");
            int rewardGold = InventoryManager.Instance.Gold;
            long rewardExperience = GetHerbTotalExperience();
            CheckFirstRecruitment(SaveManager.Instance.SaveGameDetailed(2).Succeeded, "영입 후 격리 저장");
            CheckFirstRecruitment(SaveManager.Instance.LoadGame(2), "영입 후 로드");
            yield return 5f;
            CheckFirstRecruitment(PartyManager.Instance.Roster.Count == 2 && Svc.Party.StoryProtagonistType == protagonist,
                "영입 후 로드 파티·시점 보존");
            yield return VerifyFirstRecruitmentTrails("로드후");
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
            int instanceEvents = 0;
            foreach (var clip in Resources.FindObjectsOfTypeAll<AnimationClip>())
                foreach (var entry in clip.events)
                    if (entry.objectReferenceParameter is INab.Common.WeaponTrailEffect.TrailEventData)
                        instanceEvents++;
            CheckFirstRecruitment(instanceEvents == 0, "재진입 후 공유 클립의 트레일 인스턴스 참조 0");
            yield return VerifyFirstRecruitmentTrails("재진입후");
        }

        private static IEnumerator VerifyFirstRecruitmentTrails(string label)
        {
            var trails = s_smokePlayer.GetComponentsInChildren<INab.Common.WeaponTrailEffect>();
            if (trails.Length == 0)
            {
                File.AppendAllText(FirstRecruitmentSmokeDirectory + "/Effects.txt", label + " 활성 모델에 속성 트레일 없음\n");
                yield break;
            }
            while (TryDismissTraversalGuide()) yield return .5f;
            double imbueDeadline = EditorApplication.timeSinceStartup + 30;
            while (!s_smokePlayer.HasElementOverride && EditorApplication.timeSinceStartup < imbueDeadline)
            {
                yield return HerbButton(GamepadButton.RightShoulder);
                yield return 2f;
            }
            CheckFirstRecruitment(s_smokePlayer.HasElementOverride, label + " 실제 RB 속성 부여");
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState().WithButton(GamepadButton.West));
            yield return .1f;
            InputSystem.QueueStateEvent(s_smokePad, new GamepadState());
            bool visible = false;
            int frames = 0;
            double deadline = EditorApplication.timeSinceStartup + 2;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                foreach (var trail in trails)
                {
                    if (trail.currentEffectState != INab.Common.WeaponTrailEffect.EffectState.On
                        || trail.vfxComponent == null || !trail.vfxComponent.gameObject.activeInHierarchy
                        || s_smokePlayer.ActorController.CurrentState.StateId != UPlayGround.State.ActorStateId.Attack) continue;
                    visible = true;
                    frames++;
                    CaptureRuntimeView();
                    File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png", FirstRecruitmentSmokeDirectory + "/Trail_" + label + "_" + frames + ".png", true);
                    File.AppendAllText(FirstRecruitmentSmokeDirectory + "/Effects.txt", label + " 실제 공격 | model=" + trail.name
                        + " particles=" + trail.vfxComponent.aliveParticleCount + " mode=" + trail.trailUsageType
                        + " tip=" + trail.lineTipTransform.position + " bottom=" + trail.lineBottomTransform.position
                        + " boundTip=" + trail.vfxComponent.GetVector3("Line Tip_position")
                        + " boundBottom=" + trail.vfxComponent.GetVector3("Line Bottom_position")
                        + " value=" + trail.vfxComponent.GetFloat("Effect Value")
                        + " active=" + trail.vfxComponent.GetBool("Effect Active")
                        + " bounds=" + trail.vfxComponent.GetComponent<Renderer>().bounds + "\n");
                    break;
                }
                if (frames >= 8) break;
                yield return .08f;
            }
            CheckFirstRecruitment(visible, label + " 실제 공격 상태의 트레일 실행·화면 기록");
            yield return 2f;
        }

        private static void ObserveFirstRecruitmentEffects(double now)
        {
            if (now < s_effectObservationTime) return;
            s_effectObservationTime = now + .15;
            var levelUp = GameObject.Find("VFX_Buff_Cast(Clone)");
            if (levelUp == null || s_levelUpFrames >= 3) return;
            s_levelUpFrames++;
            CaptureRuntimeView();
            File.Copy(FirstRecruitmentSmokeDirectory + "/PlayMode.png",
                FirstRecruitmentSmokeDirectory + "/LevelUp_" + s_levelUpFrames + ".png", true);
            File.AppendAllText(FirstRecruitmentSmokeDirectory + "/Effects.txt",
                "실제 LevelUp FX 관찰 | parent=" + levelUp.transform.parent?.name
                + " position=" + levelUp.transform.position + "\n");
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
                + " inputLayer=" + InputManager.Instance.CurrentLayer
                + " editorPadMove=" + s_smokePad.leftStick.ReadValue()
                + " editorActionMove=" + InputManager.Instance.GetAction("PlayerAction", "Move")?.ReadValue<Vector2>()
                + " cachedMove=" + typeof(PlayerActor).GetField("_currentMoveInput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(s_smokePlayer)
                + " suppressed=" + InputManager.Instance.IsPlayerActionInputSuppressed
                + " guide=" + UPlayGround.UI.Guide.GuidePopupRuntime.IsOpen()
                + " load=" + UPlayGround.Manager.SceneManager.Instance.LoadState
                + " position=" + s_smokePlayer.transform.position + "\n");
        }
    }
}
