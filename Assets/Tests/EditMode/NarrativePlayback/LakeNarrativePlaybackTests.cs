using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.NarrativePlayTests
{
    /// <summary>실제 부팅과 원본 맵에서 주인공 두 경로의 무언 행동을 끝까지 실행한다.</summary>
    public sealed class LakeNarrativePlaybackTests
    {
        private const string Output = "tmp/narrative-overhaul";
        private static readonly string[] Dialogues =
        {
            "Test/DLG_Test_HwarinRescue",
            "Test/DLG_Test_LianRescue",
            "Test/DLG_Test_MyoRyeongConfrontation",
            "Test/DLG_Test_MyoRyeongJoined",
            "Story/Dialogue/DLG_Lake_ShrineTreasureReveal",
            "Story/Dialogue/DLG_Lake_AlternateSelfVictory",
            "Story/Dialogue/DLG_Lake_Chapter1Return",
            "Story/Dialogue/DLG_Npc_Hazel",
            "Story/Dialogue/DLG_Lake_Intro_Mia"
        };

        [UnityTest]
        public IEnumerator 라온의_무언행동과_결말이_실제_맵에서_완료된다() => Run("Raon");

        [UnityTest]
        public IEnumerator 아린의_무언행동과_결말이_실제_맵에서_완료된다() => Run("Arin");

        [UnityTest]
        public IEnumerator 묘령은_체력을_잃지_않아도_충돌을_멈춘다() => Run("Raon", true);

        private static IEnumerator Run(string name, bool verifyCeasefire = false)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var context = new GameObject("NarrativePlaybackBootstrap").AddComponent(
                Type.GetType("UPlayGround.SceneContext, Assembly-CSharp", true));
            context.GetType().GetField("SceneType").SetValue(context, SceneType.Loading);
            yield return new EnterPlayMode();

            float deadline = Time.realtimeSinceStartup + 180f;
            object manager = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                manager = Instance("UPlayGround.Manager.GameManager");
                if (Get(manager, "BootState")?.ToString() == "Ready") break;
                yield return null;
            }
            Assert.AreEqual("Ready", Get(manager, "BootState")?.ToString(), "실제 매니저 부팅");
            var results = new List<string>();
            // 각 주인공은 새 Play Mode에서 시작해 이전 파티/씬 상태를 물려받지 않는다.
            {
                var character = (CharacterActorType)Enum.Parse(typeof(CharacterActorType), name);
                Call(Instance("UPlayGround.Manager.PartyManager"), "PrepareNewGameStartingCharacter", character);
                object scenes = Instance("UPlayGround.Manager.SceneManager");
                Call(scenes, "LoadScene", "LakeOfLife");
                deadline = Time.realtimeSinceStartup + 180f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (Get(scenes, "LoadState")?.ToString() == "Completed" && Svc.ActorQuery?.Player != null)
                        break;
                    yield return null;
                }
                Assert.AreEqual("Completed", Get(scenes, "LoadState")?.ToString());
                yield return new WaitForSeconds(2f);
                object dialogue = Instance("UPlayGround.Dialogue.DialogueManager");
                if (Svc.Dialogue.IsDialogueActive)
                    Call(dialogue, "RequestSkip", DialogueChannel.Main);
                yield return new WaitForSeconds(1f);

                // 조우 자동 발화와 필드 적은 이 검사의 대상이 아니다. 실제 모델·KCC·대화·UI는 그대로 실행한다.
                foreach (FlowGraphRunner runner in UnityEngine.Object.FindObjectsByType<FlowGraphRunner>(FindObjectsSortMode.None))
                    runner.enabled = false;
                foreach (IWorldActor actor in Svc.ActorQuery.AllActors)
                    if (actor is IStoryActorStaging staging && actor != Svc.ActorQuery.Player)
                        staging.HoldStoryCombat();

                Assert.AreEqual(character, Svc.Party.StoryProtagonistType);
                IWorldActor player = Svc.ActorQuery.Player;
                Vector3 start = player.Transform.position;
                if (verifyCeasefire)
                {
                    yield return VerifyCeasefire(player);
                    yield return new ExitPlayMode();
                    yield break;
                }
                foreach (string path in Dialogues)
                {
                    Call(player, "PlaceAtPose", start, Quaternion.identity);
                    yield return new WaitForSeconds(.3f);
                    var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>("Assets/10.Datas/Dialogue/" + path + ".asset");
                    bool isBranchPreview = path.EndsWith("DLG_Npc_Hazel", StringComparison.Ordinal)
                        || path.EndsWith("DLG_Lake_Intro_Mia", StringComparison.Ordinal);
                    if (isBranchPreview)
                    {
                        // 의뢰의 실제 완료·소비는 별도 상태 테스트가 담당한다. 여기서는 완료 장면의 행동을 재생한다.
                        graph = UnityEngine.Object.Instantiate(graph);
                        graph.startNodeId = path.EndsWith("DLG_Npc_Hazel", StringComparison.Ordinal)
                            ? "hazel_report_01" : "lake_mia_return_01";
                        graph.InvalidateCache();
                    }
                    bool completed = false;
                    bool cancelled = false;
                    DialogueNodeSO current = null;
                    Action<DialogueNodeSO> onNode = node => current = node;
                    EventInfo entered = dialogue.GetType().GetEvent("OnMainNodeEnter");
                    entered.AddEventHandler(dialogue, onNode);
                    IWorldActor partner = null;
                    IDisposable combatHold = null;
                    bool ownsPartner = false;
                    if (path.StartsWith("Test/", StringComparison.Ordinal))
                    {
                        string encounterId = path.Contains("Hwarin") ? "test.combat.honoka_rescue"
                            : path.Contains("Lian") ? "test.combat.lian_rescue" : "test.combat.siuha_duel";
                        Type anchorType = Type.GetType("UPlayGround.Gameplay.Encounter.RecruitmentEncounterAnchor, Assembly-CSharp", true);
                        foreach (UnityEngine.Object anchor in Resources.FindObjectsOfTypeAll(anchorType))
                        {
                            if (Get(anchor, "EncounterId")?.ToString() != encounterId) continue;
                            Assert.IsTrue((bool)Call(anchor, "TryPrepareDialogue"));
                            partner = Get(anchor, "DialoguePartner") as IWorldActor;
                            PlaceNear(player, partner.Transform.position + partner.Transform.forward * 3f,
                                Quaternion.LookRotation(-partner.Transform.forward));
                            yield return new WaitForSeconds(.5f);
                            break;
                        }
                        Assert.IsNotNull(partner, encounterId + " 실제 조우 파트너");
                    }
                    if (isBranchPreview)
                    {
                        Type npcType = Type.GetType("UPlayGround.NpcActor, UPlayGround.Actor", true);
                        foreach (UnityEngine.Object npc in Resources.FindObjectsOfTypeAll(npcType))
                        {
                            if (npc is not Component component || !component.gameObject.scene.IsValid())
                                continue;
                            object data = Call(npc, "GetData");
                            var assigned = data?.GetType().GetField("dialogueGraph")?.GetValue(data) as DialogueGraphSO;
                            if (assigned == null || !path.EndsWith(assigned.name, StringComparison.Ordinal))
                                continue;
                            partner = (IWorldActor)npc;
                            component.gameObject.SetActive(true);
                            Call(player, "PlaceAtPose", component.transform.position + component.transform.forward * 3f,
                                Quaternion.LookRotation(-component.transform.forward));
                            yield return new WaitForSeconds(.5f);
                            CapturePair(name + "_" + assigned.name + "_before", player.Transform, partner.Transform);
                            break;
                        }
                        Assert.IsNotNull(partner, path + "에 연결된 원본 맵 NPC");
                    }
                    if (path.EndsWith("ShrineTreasureReveal", StringComparison.Ordinal))
                    {
                        var variants = AssetDatabase.LoadAssetAtPath<AlternateSelfVariantSetSO>(
                            "Assets/10.Datas/Story/LakeOfLife/AlternateSelfVariantSet_LakeOfLife.asset");
                        Assert.IsTrue(variants.TryGetVariant(character, out var definition));
                        var shrine = AssetDatabase.LoadAssetAtPath<FlowGraphSO>("Assets/10.Datas/Flow/FLOW_LakeShrineChapter1.asset");
                        var spawn = (SpawnStoryActorNode)shrine.GetNode("spawn_alternate");
                        Quaternion facing = Quaternion.Euler(spawn.eulerAngles);
                        PlaceNear(player, spawn.position + facing * Vector3.forward * 4f,
                            Quaternion.LookRotation(-(facing * Vector3.forward)));
                        partner = Services.Get<IActorSpawnService>().SpawnActor(definition.actorId, spawn.position, facing);
                        ownsPartner = true;
                        combatHold = ((IStoryActorStaging)partner).HoldStoryCombat();
                        Camera overviewCamera = Camera.main;
                        Vector3 previousPosition = overviewCamera.transform.position;
                        Quaternion previousRotation = overviewCamera.transform.rotation;
                        overviewCamera.transform.SetPositionAndRotation(spawn.position + Vector3.up * 35f,
                            Quaternion.Euler(90f, 0f, 0f));
                        Capture(name + "_shrine_overview");
                        overviewCamera.transform.SetPositionAndRotation(previousPosition, previousRotation);
                    }
                    if (path.EndsWith("Chapter1Return", StringComparison.Ordinal))
                    {
                        var volumes = UnityEngine.Object.FindObjectsByType<FlowGraphTriggerVolume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        var arrival = Array.Find(volumes, volume => volume.VolumeId == "lake.shrine.arrival");
                        Assert.IsNotNull(arrival);
                        PlaceNear(player, arrival.transform.position, Quaternion.Euler(0, 180, 0));
                        yield return new WaitForSeconds(.5f);
                    }
                    if (path.EndsWith("AlternateSelfVictory", StringComparison.Ordinal))
                    {
                        var shrine = AssetDatabase.LoadAssetAtPath<FlowGraphSO>("Assets/10.Datas/Flow/FLOW_LakeShrineChapter1.asset");
                        var spawn = (SpawnStoryActorNode)shrine.GetNode("spawn_alternate");
                        PlaceNear(player, spawn.position, Quaternion.Euler(0, 180, 0));
                        yield return new WaitForSeconds(.5f);
                    }
                    IDisposable request = Svc.Dialogue.TryStartDialogueTracked(graph,
                        () => completed = true, partner, () => cancelled = true,
                        ownsPartner ? "AlternateSelf" : null);
                    Assert.IsNotNull(request, path);
                    float began = Time.realtimeSinceStartup;
                    deadline = began + 60f;
                    float nextAdvance = began + 1f;
                    string capturedNode = null;
                    string observedNode = null;
                    float captureAt = 0f;
                    bool capturedPair = false;
                    try
                    {
                        while (!completed && !cancelled && Time.realtimeSinceStartup < deadline)
                        {
                            DismissGuide();
                            if (current != null && observedNode != current.nodeId)
                            {
                                observedNode = current.nodeId;
                                captureAt = Time.realtimeSinceStartup + (current.stageMotion != null ? .8f : .35f);
                            }
                            if (current != null && Time.realtimeSinceStartup >= captureAt && capturedNode != current.nodeId
                                && (isBranchPreview || current.stageBeat?.enabled == true || current.textPresentation != DialogueTextPresentation.Standard
                                    || path.EndsWith("ShrineTreasureReveal", StringComparison.Ordinal)))
                            {
                                Capture(name + "_" + current.nodeId);
                                capturedNode = current.nodeId;
                                if (current.textPresentation != DialogueTextPresentation.Standard)
                                {
                                    AssertCinematicText(current.dialogueText);
                                    Capture(name + "_" + current.nodeId + "_screen", true);
                                }
                                if (ownsPartner && !capturedPair)
                                {
                                    CapturePair(name, player.Transform, partner.Transform);
                                    capturedPair = true;
                                }
                            }
                            if (current != null && current.nodeType == NodeType.Talk
                                && current.autoAdvanceDuration <= 0f
                                && Time.realtimeSinceStartup >= nextAdvance)
                            {
                                Call(dialogue, "CompleteTyping");
                                Call(dialogue, "Advance", DialogueChannel.Main);
                                nextAdvance = Time.realtimeSinceStartup + 1f;
                            }
                            yield return null;
                        }
                        results.Add(name + "/" + path + ": completed=" + completed
                            + ", cancelled=" + cancelled + ", seconds=" + (Time.realtimeSinceStartup - began));
                        Directory.CreateDirectory(Output);
                        File.WriteAllLines(Output + "/playback-" + name + ".txt", results);
                        Assert.IsFalse(cancelled, path + " 무언 행동이 중단됨");
                        Assert.IsTrue(completed, path + " 재생 제한시간 초과");
                        if (ownsPartner)
                        {
                            combatHold.Dispose();
                            combatHold = null;
                            yield return VerifyAlternateCombat(name, player, partner);
                        }
                    }
                    finally
                    {
                        entered.RemoveEventHandler(dialogue, onNode);
                        request.Dispose();
                        if (ownsPartner && partner is Component component) UnityEngine.Object.Destroy(component.gameObject);
                        else if (!isBranchPreview && partner is Component encounterPartner)
                            encounterPartner.gameObject.SetActive(false);
                        combatHold?.Dispose();
                        if (isBranchPreview) UnityEngine.Object.Destroy(graph);
                    }
                    yield return new WaitForSeconds(1f);
                }
            }
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifyCeasefire(IWorldActor player)
        {
            const string encounterId = "test.combat.siuha_duel";
            object story = Instance("UPlayGround.Story.StoryManager");
            object store = story.GetType().GetField("_recruitmentStateStore",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(story);
            // 선행 수색은 이 검사의 대상이 아니다. 실제 조우를 소개 대기 상태로 준비한다.
            Assert.IsTrue((bool)Call(store, "TryBeginIntroduction", encounterId));
            var encounters = Svc.RecruitmentEncounters;
            Assert.AreEqual(RecruitmentEncounterStartResult.IntroductionPending,
                encounters.TryStartOrResume(encounterId));
            Assert.IsTrue(encounters.TryGetDialoguePartner(encounterId, out IWorldActor partner));
            Assert.IsTrue(encounters.TryBeginIntroductionDialogueAttempt(encounterId, out var attempt));
            encounters.ConfirmDialogueCompleted(attempt);
            Call(player, "SetInvincible", true);
            Call(player, "PlaceAtPose", partner.Transform.position + Vector3.back * 4f, Quaternion.identity);
            float health = (float)Get(partner, "CurrentHealth");
            Assert.Greater(health, 0f);
            Assert.AreEqual(RecruitmentEncounterStartResult.CombatStarted,
                encounters.TryStartCombatAfterIntroduction(encounterId, attempt));
            float began = Time.time;
            float deadline = Time.realtimeSinceStartup + 25f;
            Type guide = Type.GetType("UPlayGround.UI.Guide.GuidePopupRuntime, UPlayGround.UI", true);
            MethodInfo isGuideOpen = guide.GetMethod("IsOpen");
            MethodInfo closeGuide = guide.GetMethod("Close");
            while (encounters.GetPhase(encounterId) == RecruitmentEncounterPhase.CombatActive
                && Time.realtimeSinceStartup < deadline)
            {
                if ((bool)isGuideOpen.Invoke(null, null)) closeGuide.Invoke(null, null);
                yield return null;
            }
            Assert.AreEqual(RecruitmentEncounterPhase.CombatResolved, encounters.GetPhase(encounterId));
            Assert.IsTrue(partner.IsAlive);
            Assert.AreEqual(health, (float)Get(partner, "CurrentHealth"), "HP 패배 없이 정지해야 합니다.");
            Assert.GreaterOrEqual(Time.time - began, 11.5f);
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/myo-ceasefire.txt", "CombatResolved, health=" + health
                + ", elapsed=" + (Time.time - began));
            Call(player, "SetInvincible", false);
        }

        private static IEnumerator VerifyAlternateCombat(string name, IWorldActor player, IWorldActor alternate)
        {
            Call(player, "SetInvincible", true);
            Call(Get(alternate, "Detection"), "AcquireTarget", player.Transform, true);
            float deadline = Time.realtimeSinceStartup + 20f;
            object combat = Get(alternate, "Combat");
            bool attacked = false;
            Type guide = Type.GetType("UPlayGround.UI.Guide.GuidePopupRuntime, UPlayGround.UI", true);
            while (Time.realtimeSinceStartup < deadline)
            {
                if ((bool)guide.GetMethod("IsOpen").Invoke(null, null)) guide.GetMethod("Close").Invoke(null, null);
                if (Get(combat, "CurrentSkill") != null && (bool)Get(Get(alternate, "Abilities"), "HasActiveAbility"))
                {
                    attacked = true;
                    Capture(name + "_alternate_combat");
                    break;
                }
                yield return null;
            }
            Call(player, "SetInvincible", false);
            Assert.IsTrue(attacked, "공개 홀드 해제 후 분신이 실제 GAS 공격을 실행해야 합니다.");
            File.WriteAllText(Output + "/combat-" + name + ".txt", "공개 종료 후 AI/GAS 공격 실행 확인");
        }

        private static void DismissGuide()
        {
            Type guide = Type.GetType("UPlayGround.UI.Guide.GuidePopupRuntime, UPlayGround.UI", true);
            if ((bool)guide.GetMethod("IsOpen").Invoke(null, null)) guide.GetMethod("Close").Invoke(null, null);
        }

        private static void AssertCinematicText(string expected)
        {
            Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            foreach (UnityEngine.Object value in Resources.FindObjectsOfTypeAll(textType))
            {
                if (value is not Component component || !component.gameObject.activeInHierarchy) continue;
                if (Get(value, "text")?.ToString() != expected) continue;
                Assert.Greater((float)Get(value, "alpha"), 0f, "종료 문구 페이드 표시");
                return;
            }
            Assert.Fail("활성화된 종료 문구를 찾지 못했습니다: " + expected);
        }

        private static void PlaceNear(IWorldActor player, Vector3 position, Quaternion rotation)
        {
            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = hit.point + Vector3.up * .1f;
            Call(player, "PlaceAtPose", position, rotation);
        }

        private static void Capture(string name, bool includeOverlay = false)
        {
            Camera camera = Camera.main;
            Assert.IsNotNull(camera);
            Directory.CreateDirectory(Output);
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var overlays = new List<(Canvas canvas, Camera camera, float distance)>();
            int cullingMask = camera.cullingMask;
            try
            {
                if (includeOverlay)
                {
                    camera.cullingMask |= LayerMask.GetMask("UI");
                    foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    {
                        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                        overlays.Add((canvas, canvas.worldCamera, canvas.planeDistance));
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = camera;
                        canvas.planeDistance = camera.nearClipPlane + .1f;
                        camera.cullingMask |= 1 << canvas.gameObject.layer;
                    }
                    Canvas.ForceUpdateCanvases();
                }
                camera.targetTexture = target;
                camera.Render();
                if (includeOverlay)
                {
                    var rendered = new HashSet<Camera> { camera };
                    foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    {
                        Camera uiCamera = canvas.worldCamera;
                        if (uiCamera == null || !rendered.Add(uiCamera)) continue;
                        RenderTexture uiTarget = uiCamera.targetTexture;
                        CameraClearFlags flags = uiCamera.clearFlags;
                        try
                        {
                            uiCamera.targetTexture = target;
                            uiCamera.clearFlags = CameraClearFlags.Depth;
                            uiCamera.Render();
                        }
                        finally
                        {
                            uiCamera.targetTexture = uiTarget;
                            uiCamera.clearFlags = flags;
                        }
                    }
                }
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                foreach (var overlay in overlays)
                {
                    overlay.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    overlay.canvas.worldCamera = overlay.camera;
                    overlay.canvas.planeDistance = overlay.distance;
                }
                camera.cullingMask = cullingMask;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        private static void CapturePair(string name, Transform player, Transform partner)
        {
            Camera camera = Camera.main;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            try
            {
                Vector3 center = (player.position + partner.position) * .5f + Vector3.up;
                camera.transform.position = center + Vector3.right * 4f + Vector3.forward * 2f + Vector3.up * .4f;
                camera.transform.LookAt(center);
                Capture(name + "_protagonist_and_alternate");
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position, rotation);
            }
        }

        private static object Instance(string name)
        {
            Type type = Type.GetType(name + ", Assembly-CSharp", true);
            return type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null);
        }

        private static object Get(object target, string name) =>
            target?.GetType().GetProperty(name)?.GetValue(target);

        private static object Call(object target, string name, params object[] arguments)
        {
            Assert.IsNotNull(target, name);
            foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != name) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != arguments.Length) continue;
                bool matches = true;
                for (int i = 0; i < parameters.Length; i++)
                    if (arguments[i] != null && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                        matches = false;
                if (matches) return method.Invoke(target, arguments);
            }
            throw new MissingMethodException(target.GetType().FullName, name);
        }
    }
}
