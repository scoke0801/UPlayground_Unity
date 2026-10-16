using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Data.Story;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Gameplay.Encounter;
using UPlayGround.Gameplay.Interaction;
using UPlayGround.Dialogue;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string FirstRecruitmentDirectory = "output/ScenarioGameplay/P04";
        private const string FirstRecruitmentAssets = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario";

        [Serializable] private sealed class FirstRecruitmentPlacement
        {
            public Vector3 encounter;
            public Vector3 clue;
            public string meleePrefab;
            public WorldLayout firstCombat;
            public float firstCombatChaseDistance;
            public string[] sightlinePlacements;
        }

        /// <summary>보관된 개체 ID를 복원하고 첫 영입에 필요한 두 러너만 씬에 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P04 첫 영입 배치")]
        public static void ApplyFirstRecruitment()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            var placement = JsonUtility.FromJson<FirstRecruitmentPlacement>(
                File.ReadAllText(FirstRecruitmentAssets + "/FirstRecruitment.json"));
            var quest = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(FirstRecruitmentAssets + "/FLOW_ScenarioFirstQuest.asset");
            var rescue = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(FirstRecruitmentAssets + "/FLOW_ScenarioHwarinRescue.asset");
            var definition = AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(FirstRecruitmentAssets + "/RecruitmentEncounter_ScenarioHwarin.asset");
            if (quest == null || rescue == null || definition == null)
                throw new InvalidOperationException("먼저 P04 실행 데이터를 준비하세요.");
            var layout = ReadLayout();
            var scene = EditorSceneManager.OpenScene(layout.scenePath);
            // 씬 열기의 UnloadUnusedAssets는 지역 변수로만 들고 있던 SO를 해제할 수 있다.
            quest = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(FirstRecruitmentAssets + "/FLOW_ScenarioFirstQuest.asset");
            rescue = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(FirstRecruitmentAssets + "/FLOW_ScenarioHwarinRescue.asset");
            definition = AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(FirstRecruitmentAssets + "/RecruitmentEncounter_ScenarioHwarin.asset");
            if (quest == null || rescue == null || definition == null) throw new InvalidOperationException("씬 로드 후 P04 자산 참조 누락");
            const string rootName = "[STORY] RecruitmentEncounter_HwarinRescue";
            if (GameObject.Find(rootName) != null && GameObject.Find("ScenarioFirstQuest") != null)
            {
                var existing = GameObject.Find(rootName).GetComponent<RecruitmentEncounterAnchor>();
                if (existing.Definition != definition)
                {
                    string referenceBackup = FirstRecruitmentDirectory + "/BeforeDefinitionRepair.unity";
                    if (!File.Exists(referenceBackup)) File.Copy(scene.path, referenceBackup);
                    var existingSettings = new SerializedObject(existing);
                    existingSettings.FindProperty("_definition").objectReferenceValue = definition;
                    existingSettings.ApplyModifiedPropertiesWithoutUndo();
                    if (existing.Definition == null) throw new InvalidOperationException("화린 정의 연결 실패");
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("화린 정의 저장 실패");
                }
                ApplyFirstRecruitmentCombat(placement);
                return;
            }
            if (GameObject.Find(rootName) != null || GameObject.Find("ScenarioFirstQuest") != null)
                throw new InvalidOperationException("P04 배치가 일부만 있습니다. 기존 배치를 먼저 확인하세요.");
            Directory.CreateDirectory(FirstRecruitmentDirectory);
            string backup = FirstRecruitmentDirectory + "/BeforeFirstRecruitment.unity";
            if (File.Exists(backup)) throw new InvalidOperationException("기존 P04 백업을 보존합니다: " + backup);
            File.Copy(layout.scenePath, backup);
            try
            {
                var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03.Prefabs/Test/RecruitmentEncounter_Test_HwarinRescue.prefab"), scene);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = rootName;
                root.SetActive(false);
                root.transform.position = GroundPoint(placement.encounter, terrain);
                root.GetComponent<FlowGraphRunner>().SetGraph(rescue);
                var anchor = new SerializedObject(root.GetComponent<RecruitmentEncounterAnchor>());
                anchor.FindProperty("_definition").objectReferenceValue = definition;
                anchor.ApplyModifiedPropertiesWithoutUndo();
                if (root.GetComponent<RecruitmentEncounterAnchor>().Definition == null)
                    throw new InvalidOperationException("화린 정의 연결 실패");
                var baseline = JsonUtility.FromJson<BaselineScene>(File.ReadAllText(BaselineDirectory + "/ScenarioScene.runtime.json"));
                var lines = new List<string>();
                foreach (var identity in root.GetComponentsInChildren<SceneEntityId>(true))
                {
                    string path = GetTransformPath(identity.transform);
                    var original = baseline.objects.Find(item => item.path == path);
                    if (original == null || string.IsNullOrEmpty(original.sceneEntityId))
                        throw new InvalidOperationException("P00 저장 ID 대응 누락: " + path);
                    identity.EditorSetGuid(original.sceneEntityId);
                    lines.Add(path + " | " + original.sceneEntityId);
                }
                foreach (var participant in root.GetComponentsInChildren<RecruitmentEncounterParticipant>(true))
                {
                    Vector3 point = GroundPoint(participant.transform.position, terrain);
                    if (!NavMesh.SamplePosition(point, out _, 2, NavMesh.AllAreas))
                        throw new InvalidOperationException("참가자 NavMesh 누락: " + participant.ParticipantId);
                    participant.transform.position = point;
                    lines.Add(participant.ParticipantId + " | " + point.ToString("F3"));
                }
                root.transform.Find("StoryClue_RedCloth").position = GroundPoint(placement.clue, terrain);
                root.SetActive(true);
                var questRoot = new GameObject("ScenarioFirstQuest");
                questRoot.SetActive(false);
                var runner = questRoot.AddComponent<FlowGraphRunner>();
                runner.SetGraph(quest);
                var settings = new SerializedObject(runner);
                settings.FindProperty("_fireManualEntryOnStart").boolValue = true;
                settings.FindProperty("_manualEntryIdOnStart").stringValue = "MapReady";
                settings.ApplyModifiedPropertiesWithoutUndo();
                questRoot.SetActive(true);
                AssertBaselineReferences(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("P04 씬 저장 실패");
                File.WriteAllLines(FirstRecruitmentDirectory + "/Placement.txt", lines);
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.Copy(backup, layout.scenePath, true);
                AssetDatabase.ImportAsset(layout.scenePath, ImportAssetOptions.ForceUpdate);
                throw;
            }
            ApplyFirstRecruitmentCombat(placement);
        }

        private static void ApplyFirstRecruitmentCombat(FirstRecruitmentPlacement placement)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
            ApplyFirstRecruitmentGuide(placement);
            ApplyFirstRecruitmentWeaponEffects();
            ApplyFirstRecruitmentSightline(placement, terrain);
            var clue = GameObject.Find("[STORY] RecruitmentEncounter_HwarinRescue/StoryClue_RedCloth")
                .GetComponent<FlowGraphInteractable>();
            var clueSettings = new SerializedObject(clue);
            if (!string.IsNullOrEmpty(clueSettings.FindProperty("_requiredQuestId").stringValue)
                || clueSettings.FindProperty("_shouldDeactivateAfterTrigger").boolValue)
            {
                string clueBackup = FirstRecruitmentDirectory + "/BeforeClueGate.unity";
                if (!File.Exists(clueBackup)) File.Copy(scene.path, clueBackup);
                clueSettings.FindProperty("_requiredQuestId").stringValue = "";
                clueSettings.FindProperty("_shouldDeactivateAfterTrigger").boolValue = false;
                clueSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("단서 조건 저장 실패");
            }
            string backup = FirstRecruitmentDirectory + "/BeforeCombat.unity";
            if (GameObject.Find("ScenarioFirstCombat") != null)
            {
                ApplyFirstCombatBoundary(placement, terrain);
                return;
            }
            if (File.Exists(backup)) throw new InvalidOperationException("기존 전투 배치 백업이 있습니다: " + backup);
            File.Copy(scene.path, backup);
            try
            {
                var participants = GameObject.Find("[STORY] RecruitmentEncounter_HwarinRescue")
                    .GetComponentsInChildren<RecruitmentEncounterParticipant>(true);
                var participant = Array.Find(participants, item => item.ParticipantId == "skeleton_bow_b");
                if (participant == null) throw new InvalidOperationException("기존 두 번째 참가자 ID가 없습니다.");
                var previous = participant.Actor;
                var previousSettings = new SerializedObject(previous);
                string actorId = previousSettings.FindProperty("_actorId").stringValue;
                string entityId = previous.GetComponent<SceneEntityId>().Guid;
                var replacement = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(placement.meleePrefab), previous.transform.parent);
                replacement.name = previous.name;
                replacement.transform.SetPositionAndRotation(previous.transform.position, previous.transform.rotation);
                var actor = replacement.GetComponent<MonsterActor>();
                var actorSettings = new SerializedObject(actor);
                actorSettings.FindProperty("_actorId").stringValue = actorId;
                actorSettings.FindProperty("_recruitableAs").intValue = 0;
                actorSettings.ApplyModifiedPropertiesWithoutUndo();
                replacement.GetComponent<SceneEntityId>().EditorSetGuid(entityId);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.GetComponent<SceneEntityId>());
                var participantSettings = new SerializedObject(participant);
                participantSettings.FindProperty("_actor").objectReferenceValue = actor;
                participantSettings.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var firstCombat = new GameObject("ScenarioFirstCombat");
                PlaceWorldMonsters(placement.firstCombat, terrain, firstCombat.transform);
                ApplyFirstCombatBoundary(placement, terrain);
                AssertBaselineReferences(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("전투 배치 저장 실패");
                File.WriteAllText(FirstRecruitmentDirectory + "/CombatPlacement.txt",
                    "E00: FirstRecruitment.json의 일반 근접 1체. 기존 일반 적 보상·재생성 계약.\n" +
                    "E01: 근접 2체. 두 번째 참가자의 기존 skeleton_bow_b/Actor/SceneEntity ID 보존.\n" +
                    "실제 전투 감각·마을 추격·완주 검사는 아직 미실행.\n");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.Copy(backup, ReadLayout().scenePath, true);
                AssetDatabase.ImportAsset(ReadLayout().scenePath, ImportAssetOptions.ForceUpdate);
                throw;
            }
        }

        private static void ApplyFirstCombatBoundary(FirstRecruitmentPlacement placement, Terrain terrain)
        {
            var actor = GameObject.Find("ScenarioFirstCombat").GetComponentInChildren<MonsterActor>();
            var entry = placement.firstCombat.monsters[0];
            if (actor.GetComponent<SceneEntityId>().Guid != entry.id)
                throw new InvalidOperationException("첫 전투 개체 식별자가 배치 데이터와 다릅니다.");
            Vector3 point = GroundPoint(entry.position, terrain);
            if (!NavMesh.SamplePosition(point, out _, 2, NavMesh.AllAreas))
                throw new InvalidOperationException("첫 전투 배치의 NavMesh가 없습니다.");
            actor.transform.SetPositionAndRotation(point, Quaternion.Euler(0, entry.yaw, 0));
            var detection = actor.GetComponent<EnemyDetection>();
            var settings = new SerializedObject(detection);
            settings.FindProperty("_maxChaseDistanceFromAnchor").floatValue = placement.firstCombatChaseDistance;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(detection);
            var scene = actor.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("첫 전투 안전 경계 저장 실패");
        }

        private static void ApplyFirstRecruitmentGuide(FirstRecruitmentPlacement placement)
        {
            string graphPath = FirstRecruitmentAssets + "/DLG_ScenarioGuide.asset";
            string npcPath = FirstRecruitmentAssets + "/NPC_Guide_Scenario.asset";
            if (!File.Exists(graphPath))
            {
                var created = new List<string>();
                try
                {
                    var graph = CopyFirstRecruitmentAsset<DialogueGraphSO>(
                        "Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Lake_Intro_Guide.asset", graphPath, created);
                    var npc = CopyFirstRecruitmentAsset<ScriptableObject>(
                        "Assets/10.Datas/Actor/Npc/NPC_Story_Guide.asset", npcPath, created);
                    var beat = ScriptableObject.CreateInstance<DialogueNodeSO>();
                    beat.name = "GuideRoadStep";
                    beat.nodeId = "scenario_guide_road_step";
                    beat.nodeType = NodeType.Event;
                    beat.nextNodeId = "lake_guide_intro_04";
                    beat.stageBeat = new DialogueStageBeat
                    {
                        enabled = true, actorSpeakerId = "안내인", targetSpeakerId = "안내인",
                        useInitialTargetPose = true, localOffset = Vector3.forward * 1.2f,
                        move = true, faceMovement = true, holdSeconds = .6f,
                        speedMultiplier = .35f, stopDistance = .35f, timeoutSeconds = 2f,
                        timeoutPolicy = DialogueStageTimeoutPolicy.ContinueDialogue
                    };
                    AssetDatabase.AddObjectToAsset(beat, graph);
                    graph.nodes.Add(beat);
                    var previous = graph.nodes.Find(node => node.nodeId == "lake_guide_intro_03");
                    previous.nextNodeId = beat.nodeId;
                    graph.silentParticipantSpeakerIds.Clear();
                    var data = new SerializedObject(npc);
                    data.FindProperty("dialogueGraph").objectReferenceValue = graph;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(previous);
                    EditorUtility.SetDirty(beat);
                    EditorUtility.SetDirty(graph);
                    AssetDatabase.SaveAssetIfDirty(graph);
                    AssetDatabase.SaveAssetIfDirty(npc);
                }
                catch
                {
                    foreach (string path in created) AssetDatabase.DeleteAsset(path);
                    throw;
                }
            }
            var guide = GameObject.Find("NPC/NPC_Guide").GetComponent<NpcActor>();
            var settings = new SerializedObject(guide);
            var npcData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(npcPath);
            if (npcData == null) throw new InvalidOperationException("시나리오 안내인 데이터 누락");
            settings.FindProperty("_data").objectReferenceValue = npcData;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Vector3 direction = placement.firstCombat.monsters[0].position - guide.transform.position;
            direction.y = 0;
            guide.transform.rotation = Quaternion.LookRotation(direction);
            PrefabUtility.RecordPrefabInstancePropertyModifications(guide);
            PrefabUtility.RecordPrefabInstancePropertyModifications(guide.transform);
            var scene = guide.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("안내인 동선 저장 실패");
        }

        /// <summary>기본 새 게임 목적지를 바꾸지 않고 라온 한 명으로 개발 플레이를 시작한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P04 라온 시작")]
        public static void PlayFirstRecruitmentRaon() => StartFirstRecruitment("Raon");

        /// <summary>슬롯2의 아린 영입 전 저장을 불러와 사용자 테스트를 시작한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P04 아린 시작")]
        public static void PlayFirstRecruitmentArin()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            ValidateFirstRecruitmentArinSave();
            SessionState.SetString(BaselineCharacterKey, "Arin");
            StartSmoke(ReadLayout().scenePath, keepPlaying: true, loadSlot: 1);
        }

        private static void ValidateFirstRecruitmentArinSave()
        {
            string path = Path.Combine(Application.persistentDataPath, "saves", "save_slot_1.sav");
            if (!File.Exists(path))
                throw new InvalidOperationException("P04 아린 테스트용 슬롯2가 없습니다. 영입 전 저장본을 준비하세요. 다른 슬롯은 불러오지 않습니다.");
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<UPlayGround.Data.Save.GameSaveData>(
                UPlayGround.Manager.SaveCrypto.Decrypt(File.ReadAllBytes(path)));
            var party = data?.party;
            if (party?.loadSceneName != Path.GetFileNameWithoutExtension(ReadLayout().scenePath)
                || party.storyProtagonistType != "Arin"
                || party.roster == null || party.roster.Count != 1 || party.roster[0] != "Arin")
                throw new InvalidOperationException("슬롯2가 시나리오의 아린 단독 영입 전 저장이 아닙니다. 저장을 덮어쓰지 말고 내용을 확인하세요.");
        }

        private static void StartFirstRecruitment(string character)
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            SessionState.SetString(BaselineCharacterKey, character);
            StartSmoke(ReadLayout().scenePath, keepPlaying: true);
        }

        /// <summary>원본 저장 식별자를 유지한 채 첫 영입의 선행 발견·복원용 그래프를 분리한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P04 실행 데이터 준비")]
        public static void PrepareFirstRecruitmentData()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            Directory.CreateDirectory(FirstRecruitmentDirectory);
            var paths = new[]
            {
                FirstRecruitmentAssets + "/FLOW_ScenarioFirstQuest.asset",
                FirstRecruitmentAssets + "/FLOW_ScenarioHwarinRescue.asset",
                FirstRecruitmentAssets + "/RecruitmentEncounter_ScenarioHwarin.asset",
                FirstRecruitmentAssets + "/DLG_ScenarioHwarinRescue.asset"
            };
            // 재실행으로 이미 저작한 에셋을 덮어쓰지 않는다.
            foreach (string path in paths)
                if (File.Exists(path) || File.Exists(path + ".meta"))
                    throw new InvalidOperationException("P04 준비 결과가 이미 있습니다: " + path);
            var created = new List<string>();
            try
            {
                var quest = CopyFirstRecruitmentAsset<FlowGraphSO>(
                    "Assets/10.Datas/Flow/FLOW_LakeSearchQuestLine.asset", paths[0], created);
                var rescue = CopyFirstRecruitmentAsset<FlowGraphSO>(
                    "Assets/10.Datas/Flow/Test/FLOW_Test_HwarinRescue.asset", paths[1], created);
                var definition = CopyFirstRecruitmentAsset<RecruitmentEncounterDefinitionSO>(
                    "Assets/10.Datas/Story/Test/RecruitmentEncounter_Test_HwarinRescue.asset", paths[2], created);
                var dialogue = CopyFirstRecruitmentAsset<DialogueGraphSO>(
                    "Assets/10.Datas/Dialogue/Test/DLG_Test_HwarinRescue.asset", paths[3], created);
                var portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/Portrait/Player_Hwarin_Full.png");
                if (portrait == null) throw new InvalidOperationException("화린 모델 대응 이미지 누락");
                foreach (var node in dialogue.nodes)
                    if (node.speakerId == "화린")
                    {
                        node.portrait = portrait;
                        if (node.stageBeat != null && node.stageBeat.enabled)
                        {
                            // 자기소개 전 이동 대신 동행 의사를 확인하는 대사로 연결한다.
                            node.stageBeat = null;
                            node.nodeType = NodeType.Talk;
                            node.speakerId = "Protagonist";
                            node.listenerSpeakerId = "화린";
                            node.portrait = null;
                            node.dialogueText = "혼자 찾기엔 위험해요. 같이 가요.";
                        }
                        EditorUtility.SetDirty(node);
                    }
                foreach (string suffix in new[] { "01", "02", "03", "04", "05", "13", "14" })
                {
                    var node = dialogue.GetNode("dlg_test_honoka_rescue_" + suffix);
                    node.shotType = suffix == "01" || suffix == "05" || suffix == "14"
                        ? UPlayGround.Data.DialogueShotType.TwoShot
                        : UPlayGround.Data.DialogueShotType.OverTheShoulderSpeaker;
                    node.listenerSpeakerId = node.speakerId == "Protagonist" ? "화린" : "Protagonist";
                    if (suffix == "03") node.dialogueText = "어디서 헤어졌어요?";
                    if (suffix == "04") node.dialogueText = "이 길 안쪽에서. 돌아오면 날 찾으라고 천을 묶어 뒀어.";
                    if (suffix == "13") node.dialogueText = "고마워. 난 화린이야. 말은 편하게 해도 돼.";
                    EditorUtility.SetDirty(node);
                }
                EditorUtility.SetDirty(dialogue);
                AssetDatabase.SaveAssetIfDirty(dialogue);
                ((PlayDialogueRequiredNode)rescue.GetNode("play_dialogue")).dialogue = dialogue;
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("_requiredFlagKey").stringValue = "";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // 조사 사실은 Quest가 아직 열리지 않아도 남기고, 공개 후 플래그에서 다시 반영한다.
                RedirectFirstRecruitmentEdge(rescue, "entry_red_cloth", "Out", "check_red_cloth_unexamined");
                ((PlayDialogueNode)rescue.GetNode("play_red_cloth_dialogue")).continueWhenCancelled = false;
                AddFirstRecruitmentFactSync(quest, "red_cloth", "lake.story.red_cloth_examined", "sync_joined");
                AddFirstRecruitmentFactSync(quest, "joined", "lake.story.honoka_joined", "check_briefing");
                RedirectFirstRecruitmentEdge(quest, "notify_guide", "Out", "sync_red_cloth");
                RedirectFirstRecruitmentEdge(quest, "sync_guide", "False", "sync_red_cloth");

                // 현장에 도달한 플레이어에게 이미 지나온 표식을 다시 클릭하도록 요구하지 않는다.
                rescue.nodes.Add(new SetFlagNode { id = "mark_cloth_at_encounter", flagKey = "lake.story.red_cloth_examined", value = true });
                rescue.nodes.Add(new NotifyQuestStoryEventNode { id = "notify_cloth_at_encounter", eventId = "lake.story.red_cloth_examined" });
                RedirectFirstRecruitmentEdge(rescue, "entry_volume", "Out", "mark_cloth_at_encounter");
                AddFirstRecruitmentEdge(rescue, "mark_cloth_at_encounter", "Out", "notify_cloth_at_encounter");
                AddFirstRecruitmentEdge(rescue, "notify_cloth_at_encounter", "Out", "resume_encounter");
                foreach (var graph in new[] { quest, rescue })
                {
                    var errors = new List<string>();
                    if (!graph.Validate(errors)) throw new InvalidOperationException(string.Join("\n", errors));
                    EditorUtility.SetDirty(graph);
                    AssetDatabase.SaveAssetIfDirty(graph);
                }
                AssetDatabase.SaveAssetIfDirty(definition);
                File.WriteAllLines(FirstRecruitmentDirectory + "/PreparedAssets.txt", paths);
            }
            catch
            {
                // 이 호출이 새로 만든 파일만 제거한다. 기존 사용자 에셋은 건드리지 않는다.
                foreach (string path in created) AssetDatabase.DeleteAsset(path);
                throw;
            }
        }

        private static void ApplyFirstRecruitmentSightline(FirstRecruitmentPlacement placement, Terrain terrain)
        {
            if (placement.sightlinePlacements == null) return;
            var layout = ReadLayout();
            var environment = GameObject.Find("AuthoredEnvironment");
            bool changed = false;
            foreach (string name in placement.sightlinePlacements)
            {
                var source = Array.Find(layout.placements, item => item.name == name);
                var target = environment.transform.Find(name);
                if (source == null || target == null)
                    throw new InvalidOperationException("첫 전투 시야 배치 누락: " + name);
                Vector3 position = GroundPoint(source.position, terrain) + Vector3.up * source.groundOffset;
                if ((target.position - position).sqrMagnitude < .0001f) continue;
                target.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                changed = true;
            }
            if (!changed) return;
            Physics.SyncTransforms();
            BakeNavigation(layout, environment);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(environment.scene);
        }

        private static void ApplyFirstRecruitmentWeaponEffects()
        {
            var root = GameObject.Find("[STORY] RecruitmentEncounter_HwarinRescue");
            foreach (var trail in root.GetComponentsInChildren<INab.Common.WeaponTrailEffect>(true))
            {
                trail.trailUsageType = INab.Common.WeaponTrailEffect.TrailUsageType.Manual;
                // 복사된 전투 대역에도 MotionSet 검격이 사용할 실제 칼날 기준점이 필요하다.
                var spawner = trail.GetComponent<UPlayGround.Particle.WeaponSlashVfxSpawner>();
                if (spawner == null)
                    spawner = trail.gameObject.AddComponent<UPlayGround.Particle.WeaponSlashVfxSpawner>();
                var settings = new SerializedObject(spawner);
                settings.FindProperty("bladeBase").objectReferenceValue = trail.lineBottomTransform;
                settings.FindProperty("bladeTip").objectReferenceValue = trail.lineTipTransform;
                settings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(trail);
            }
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static T CopyFirstRecruitmentAsset<T>(string source, string destination, List<string> created)
            where T : UnityEngine.Object
        {
            if (!AssetDatabase.CopyAsset(source, destination))
                throw new InvalidOperationException("P04 자산 복사 실패: " + source);
            created.Add(destination);
            var asset = AssetDatabase.LoadAssetAtPath<T>(destination);
            if (asset == null) throw new InvalidOperationException("P04 자산 로드 실패: " + destination);
            return asset;
        }

        private static void AddFirstRecruitmentFactSync(FlowGraphSO graph, string suffix, string flag, string next)
        {
            graph.nodes.Add(new CheckFlagNode { id = "sync_" + suffix, flagKey = flag, expectedValue = true });
            graph.nodes.Add(new NotifyQuestStoryEventNode { id = "notify_" + suffix, eventId = flag });
            AddFirstRecruitmentEdge(graph, "sync_" + suffix, "True", "notify_" + suffix);
            AddFirstRecruitmentEdge(graph, "sync_" + suffix, "False", next);
            AddFirstRecruitmentEdge(graph, "notify_" + suffix, "Out", next);
        }

        private static void RedirectFirstRecruitmentEdge(FlowGraphSO graph, string from, string port, string to)
        {
            var edge = graph.connections.Find(value => value.fromNodeId == from && value.fromPort == port);
            if (edge == null) throw new InvalidOperationException("기존 연결 누락: " + from + "." + port);
            edge.toNodeId = to;
        }

        private static void AddFirstRecruitmentEdge(FlowGraphSO graph, string from, string port, string to)
        {
            graph.connections.Add(new FlowConnection { fromNodeId = from, fromPort = port, toNodeId = to, toPort = "In" });
        }
    }
}
