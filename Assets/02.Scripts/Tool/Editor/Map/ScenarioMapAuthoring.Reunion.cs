using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Data.Story;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Gameplay.Encounter;
using UPlayGround.Gameplay.Interaction;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string ReunionOutput = "output/ScenarioGameplay/P05";
        private const string ReunionSource = FirstRecruitmentAssets + "/Reunion.json";

        [Serializable] private sealed class ReunionPlacement
        {
            public ReunionEncounter[] encounters;
            public float reunionHoldSeconds;
            public float chaseDistance;
            public float breatherSeconds;
        }

        [Serializable] private sealed class ReunionEncounter
        {
            public string name;
            public Vector3 center;
            public Vector3 clue;
            public string clueName;
            public ReunionParticipant[] participants;
        }

        [Serializable] private sealed class ReunionParticipant
        {
            public string id;
            public Vector3 position;
            public string copyFrom;
            public string actorId;
            public string entityId;
        }

        /// <summary>리안 재회와 묘령 충돌을 기존 영입 계약과 보관된 개체 ID로 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P05 재회와 충돌 적용")]
        public static void ApplyReunion()
        {
            RequireEditMode();
            RequireSavedTraversalScenes();
            var placement = JsonUtility.FromJson<ReunionPlacement>(File.ReadAllText(ReunionSource));
            var previous = EditorSceneManager.GetSceneManagerSetup();
            string scenePath = ReadLayout().scenePath;
            string questPath = FirstRecruitmentAssets + "/FLOW_ScenarioFirstQuest.asset";
            Directory.CreateDirectory(ReunionOutput);
            string backup = ReunionOutput + "/BeforeApply_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            Directory.CreateDirectory(backup);
            File.Copy(scenePath, backup + "/Scene.unity");
            File.Copy(questPath, backup + "/Quest.asset");
            var created = new List<string>();
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                foreach (var entry in placement.encounters)
                    if (GameObject.Find("[STORY] RecruitmentEncounter_" + entry.name) != null)
                        throw new InvalidOperationException("P05 배치가 이미 있습니다. 재생성하지 않습니다: " + entry.name);
                var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
                var baseline = JsonUtility.FromJson<BaselineScene>(File.ReadAllText(BaselineDirectory + "/ScenarioScene.runtime.json"));
                var report = new List<string>();
                foreach (var entry in placement.encounters)
                {
                    var flow = PrepareReunionFlow(entry.name, placement.reunionHoldSeconds, created);
                    var definition = CopyFirstRecruitmentAsset<RecruitmentEncounterDefinitionSO>(
                        "Assets/10.Datas/Story/Test/RecruitmentEncounter_Test_" + entry.name + ".asset",
                        FirstRecruitmentAssets + "/RecruitmentEncounter_Scenario" + entry.name + ".asset", created);
                    var settings = new SerializedObject(definition);
                    settings.FindProperty("_requiredFlagKey").stringValue = "";
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssetIfDirty(definition);
                    ApplyReunionEncounter(entry, placement, flow, definition, terrain, baseline, report);
                }
                var quest = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(questPath);
                AddFirstRecruitmentFactSync(quest, "lian_marker", "lake.story.lian_marker_examined", "sync_lian_joined");
                AddFirstRecruitmentFactSync(quest, "lian_joined", "lake.story.lian_joined", "sync_tracks");
                AddFirstRecruitmentFactSync(quest, "tracks", "lake.story.drag_tracks_examined", "sync_myo_joined");
                AddFirstRecruitmentFactSync(quest, "myo_joined", "lake.story.siuha_joined", "check_briefing");
                RedirectFirstRecruitmentEdge(quest, "sync_joined", "False", "sync_lian_marker");
                RedirectFirstRecruitmentEdge(quest, "notify_joined", "Out", "sync_lian_marker");
                SaveReunionFlow(quest);
                AssertBaselineReferences(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("P05 씬 저장 실패");
                File.WriteAllLines(ReunionOutput + "/Placement.txt", report);
                File.WriteAllLines(ReunionOutput + "/CreatedAssets.txt", created);
                File.WriteAllText(ReunionOutput + "/Apply.txt", "P05 씬·데이터 적용 성공. 실제 플레이 품질은 미검증.\n");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.Copy(backup + "/Scene.unity", scenePath, true);
                File.Copy(backup + "/Quest.asset", questPath, true);
                foreach (string path in created) AssetDatabase.DeleteAsset(path);
                AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(questPath, ImportAssetOptions.ForceUpdate);
                throw;
            }
            finally
            {
                if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static FlowGraphSO PrepareReunionFlow(string name, float holdSeconds, List<string> created)
        {
            string path = FirstRecruitmentAssets + "/FLOW_Scenario" + name + ".asset";
            if (File.Exists(path)) throw new InvalidOperationException("기존 P05 자산을 보존합니다: " + path);
            var flow = CopyFirstRecruitmentAsset<FlowGraphSO>(
                "Assets/10.Datas/Flow/Test/FLOW_Test_" + name + ".asset", path, created);
            bool isReunion = name == "LianRescue";
            string suffix = isReunion ? "lian_marker" : "drag_tracks";
            string flag = "lake.story." + suffix + "_examined";
            string dialogueId = isReunion ? "play_dialogue" : "play_reconciliation";
            var required = (PlayDialogueRequiredNode)flow.GetNode(dialogueId);
            required.dialogue = CopyReunionDialogue(required.dialogue, created);
            if (isReunion)
            {
                var beat = required.dialogue.GetNode("dlg_test_lian_rescue_06");
                // 재회의 여유를 장거리 강제 이동에 의존하지 않는다. 화린은 리안을 바라보며 숨을 고른다.
                beat.stageBeat.move = false;
                beat.stageBeat.holdSeconds = holdSeconds;
                EditorUtility.SetDirty(beat);
                AssetDatabase.SaveAssetIfDirty(required.dialogue);
            }
            else
            {
                var introduction = (PlayDialogueRequiredNode)flow.GetNode("play_introduction");
                introduction.dialogue = CopyReunionDialogue(introduction.dialogue, created);
                ((PlayRecruitmentPostDialogueNode)flow.GetNode("play_post_dialogue")).dialogue = required.dialogue;
            }
            // 선행 조사에는 아직 영입하지 않은 인물의 대역을 만들지 않는다.
            flow.nodes.Add(new CheckFlagNode { id = "check_clue_companion", flagKey = isReunion
                ? "lake.story.honoka_joined" : "lake.story.lian_joined", expectedValue = true });
            RedirectFirstRecruitmentEdge(flow, "entry_" + suffix, "Out", "check_" + suffix + "_unexamined");
            RedirectFirstRecruitmentEdge(flow, "check_" + suffix + "_unexamined", "True", "check_clue_companion");
            AddFirstRecruitmentEdge(flow, "check_clue_companion", "True", "play_" + suffix + "_dialogue");
            AddFirstRecruitmentEdge(flow, "check_clue_companion", "False", "notify_" + suffix + "_examined");
            ((PlayDialogueNode)flow.GetNode("play_" + suffix + "_dialogue")).continueWhenCancelled = false;
            flow.nodes.Add(new SetFlagNode { id = "mark_clue_at_encounter", flagKey = flag, value = true });
            flow.nodes.Add(new NotifyQuestStoryEventNode { id = "notify_clue_at_encounter", eventId = flag });
            RedirectFirstRecruitmentEdge(flow, "entry_volume", "Out", "mark_clue_at_encounter");
            AddFirstRecruitmentEdge(flow, "mark_clue_at_encounter", "Out", "notify_clue_at_encounter");
            AddFirstRecruitmentEdge(flow, "notify_clue_at_encounter", "Out", "resume_encounter");
            if (isReunion)
            {
                AddFirstRecruitmentFactSync(flow, "early_tracks", "lake.story.drag_tracks_examined", "check_next_quest");
                RedirectFirstRecruitmentEdge(flow, "notify_quest_progress", "Out", "sync_early_tracks");
            }
            SaveReunionFlow(flow);
            return flow;
        }

        private static DialogueGraphSO CopyReunionDialogue(DialogueGraphSO source, List<string> created)
        {
            string path = FirstRecruitmentAssets + "/" + source.name.Replace("DLG_Test_", "DLG_Scenario") + ".asset";
            var graph = CopyFirstRecruitmentAsset<DialogueGraphSO>(AssetDatabase.GetAssetPath(source), path, created);
            foreach (var node in graph.nodes)
            {
                string portrait = node.speakerId switch
                {
                    "화린" => "Player_Hwarin_Full", "리안" => "Player_Lian_Full",
                    "묘령" => "Player_MyoRyeong_Full", _ => null
                };
                if (portrait != null)
                {
                    node.portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/UI/Portrait/" + portrait + ".png");
                    if (node.portrait == null) throw new InvalidOperationException("인물 초상화 누락: " + portrait);
                    EditorUtility.SetDirty(node);
                }
            }
            if (!graph.TryValidatePlayback(out string error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
            return graph;
        }

        private static void SaveReunionFlow(FlowGraphSO graph)
        {
            var errors = new List<string>();
            if (!graph.Validate(errors)) throw new InvalidOperationException(string.Join("\n", errors));
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
        }

        private static void ApplyReunionEncounter(ReunionEncounter entry, ReunionPlacement placement,
            FlowGraphSO flow, RecruitmentEncounterDefinitionSO definition, Terrain terrain,
            BaselineScene baseline, List<string> report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Test/RecruitmentEncounter_Test_" + entry.name + ".prefab");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "[STORY] RecruitmentEncounter_" + entry.name;
            root.SetActive(false);
            root.transform.position = GroundPoint(entry.center, terrain);
            root.GetComponent<FlowGraphRunner>().SetGraph(flow);
            foreach (var identity in root.GetComponentsInChildren<SceneEntityId>(true))
            {
                string path = GetTransformPath(identity.transform);
                var original = baseline.objects.Find(item => item.path == path);
                if (original == null || string.IsNullOrEmpty(original.sceneEntityId))
                    throw new InvalidOperationException("P00 식별자 누락: " + path);
                identity.EditorSetGuid(original.sceneEntityId);
                report.Add(path + " | " + original.sceneEntityId);
            }
            foreach (var item in entry.participants)
            {
                var all = root.GetComponentsInChildren<RecruitmentEncounterParticipant>(true);
                var participant = Array.Find(all, candidate => candidate.ParticipantId == item.id);
                if (participant == null && !string.IsNullOrEmpty(item.copyFrom))
                {
                    var source = Array.Find(all, candidate => candidate.ParticipantId == item.copyFrom);
                    participant = UnityEngine.Object.Instantiate(source, source.transform.parent);
                    participant.name = "[HOSTILE] Skeleton_Sword_C";
                    var copy = new SerializedObject(participant);
                    copy.FindProperty("_participantId").stringValue = item.id;
                    copy.ApplyModifiedPropertiesWithoutUndo();
                    var actorData = new SerializedObject(participant.Actor);
                    actorData.FindProperty("_actorId").stringValue = item.actorId;
                    actorData.ApplyModifiedPropertiesWithoutUndo();
                    participant.Actor.GetComponent<SceneEntityId>().EditorSetGuid(item.entityId);
                }
                if (participant == null) throw new InvalidOperationException("참가자 누락: " + item.id);
                Vector3 point = GroundPoint(item.position, terrain);
                if (!NavMesh.SamplePosition(point, out var hit, 2f, NavMesh.AllAreas))
                    throw new InvalidOperationException("참가자 NavMesh 누락: " + item.id + " " + point);
                participant.transform.position = hit.position;
                participant.Actor.transform.position = hit.position;
                var actor = new SerializedObject(participant.Actor);
                actor.FindProperty("_recruitableAs").intValue = 0;
                actor.ApplyModifiedPropertiesWithoutUndo();
                var detection = new SerializedObject(participant.Actor.GetComponent<EnemyDetection>());
                detection.FindProperty("_maxChaseDistanceFromAnchor").floatValue = placement.chaseDistance;
                detection.ApplyModifiedPropertiesWithoutUndo();
                report.Add(item.id + " | " + hit.position.ToString("F3"));
            }
            var anchor = new SerializedObject(root.GetComponent<RecruitmentEncounterAnchor>());
            anchor.FindProperty("_definition").objectReferenceValue = definition;
            var participants = root.GetComponentsInChildren<RecruitmentEncounterParticipant>(true);
            var list = anchor.FindProperty("_participants");
            list.arraySize = participants.Length;
            for (int i = 0; i < participants.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = participants[i];
            anchor.ApplyModifiedPropertiesWithoutUndo();
            var group = new SerializedObject(root.transform.Find("HostileGroup").GetComponent<UPlayGround.Group.MonsterGroupController>());
            group.FindProperty("_meleeSlotCap").intValue = 1;
            group.FindProperty("_rangedSlotCap").intValue = 1;
            group.FindProperty("_breatherDuration").floatValue = placement.breatherSeconds;
            group.ApplyModifiedPropertiesWithoutUndo();
            var clue = root.transform.Find(entry.clueName);
            clue.position = GroundPoint(entry.clue, terrain);
            var interaction = new SerializedObject(clue.GetComponent<FlowGraphInteractable>());
            interaction.FindProperty("_requiredQuestId").stringValue = "";
            interaction.FindProperty("_requiredFlagKey").stringValue = "";
            interaction.FindProperty("_shouldDeactivateAfterTrigger").boolValue = false;
            interaction.ApplyModifiedPropertiesWithoutUndo();
            foreach (var trail in root.GetComponentsInChildren<INab.Common.WeaponTrailEffect>(true))
            {
                trail.trailUsageType = INab.Common.WeaponTrailEffect.TrailUsageType.Manual;
                var spawner = trail.GetComponent<UPlayGround.Particle.WeaponSlashVfxSpawner>();
                if (spawner == null) spawner = trail.gameObject.AddComponent<UPlayGround.Particle.WeaponSlashVfxSpawner>();
                var data = new SerializedObject(spawner);
                data.FindProperty("bladeBase").objectReferenceValue = trail.lineBottomTransform;
                data.FindProperty("bladeTip").objectReferenceValue = trail.lineTipTransform;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            root.SetActive(true);
        }
    }
}
