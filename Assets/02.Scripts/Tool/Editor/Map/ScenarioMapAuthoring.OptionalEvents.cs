using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Gameplay.Interaction;
using UPlayGround.TriggerSystem;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string OptionalOutput = "output/ScenarioGameplay/P06";
        private const string OptionalSource = FirstRecruitmentAssets + "/OptionalEvents.json";

        [Serializable] private sealed class OptionalPlacement
        {
            public WorldMonster[] removedMonsters, islandMonsters;
            public string westGroup;
            public OptionalPoint[] volumes, junActors;
        }
        [Serializable] private sealed class OptionalPoint { public string name; public Vector3 position; }

        /// <summary>저장 ID를 재생성하지 않고 조사 위치와 선택 사건의 복원 연결을 갱신한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P06 조사 위치 갱신")]
        public static void RefreshOptionalPositions()
        {
            RequireEditMode();
            RequireLegacyScenarioContent();
            RequireSavedTraversalScenes();
            var previous = EditorSceneManager.GetSceneManagerSetup();
            string scenePath = ReadLayout().scenePath;
            string backup = OptionalOutput + "/BeforePosition_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".unity";
            Directory.CreateDirectory(OptionalOutput);
            File.Copy(scenePath, backup);
            string junPath = FirstRecruitmentAssets + "/FLOW_ScenarioJunRescue.asset";
            File.Copy(junPath, backup + ".jun");
            File.Copy(HerbFlowPath, backup + ".side");
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var placement = JsonUtility.FromJson<OptionalPlacement>(File.ReadAllText(OptionalSource));
                var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
                var report = new List<string>();
                foreach (var point in placement.volumes)
                {
                    ValidateOptionalPosition(point.name, point.position, terrain, report);
                    string root = point.name.StartsWith("Jun", StringComparison.Ordinal) ? "ScenarioOptionalEvents" : "ScenarioHerbRoute";
                    var target = GameObject.Find(root + "/" + point.name);
                    if (target == null) throw new InvalidOperationException("P06 조사 대상 누락: " + point.name);
                    target.transform.position = GroundPoint(point.position, terrain);
                }
                var west = GameObject.Find("WorldExpansionActors").transform.Find(placement.westGroup).GetComponent<TriggerComposer>();
                var westSettings = new SerializedObject(west);
                westSettings.FindProperty("_repeat").enumValueIndex = (int)TriggerRepeatPolicy.Always;
                westSettings.ApplyModifiedPropertiesWithoutUndo();
                PrepareOptionalRecovery(AssetDatabase.LoadAssetAtPath<FlowGraphSO>(junPath), AssetDatabase.LoadAssetAtPath<FlowGraphSO>(HerbFlowPath));
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("P06 조사 위치 저장 실패");
                File.WriteAllLines(OptionalOutput + "/InvestigationPositions.txt", report);
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                File.Copy(backup, scenePath, true);
                File.Copy(backup + ".jun", junPath, true);
                File.Copy(backup + ".side", HerbFlowPath, true);
                AssetDatabase.ImportAsset(junPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(HerbFlowPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
                throw;
            }
            finally { if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        /// <summary>기존 저장 식별자를 보존하며 선택 사건과 섬의 조우를 시나리오에 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/P06 선택 사건 적용")]
        public static void ApplyOptionalEvents()
        {
            RequireEditMode();
            RequireLegacyScenarioContent();
            RequireSavedTraversalScenes();
            var placement = JsonUtility.FromJson<OptionalPlacement>(File.ReadAllText(OptionalSource));
            var previous = EditorSceneManager.GetSceneManagerSetup();
            string scenePath = ReadLayout().scenePath;
            string hazelPath = FirstRecruitmentAssets + "/DLG_Npc_Hazel_Scenario.asset";
            string[] originals = { scenePath, WorldPath, HerbFlowPath, hazelPath };
            string backup = OptionalOutput + "/BeforeApply_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
            Directory.CreateDirectory(backup);
            for (int i = 0; i < originals.Length; i++) File.Copy(originals[i], backup + "/" + i);
            var created = new List<string>();
            var report = new List<string>();
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                if (GameObject.Find("ScenarioOptionalEvents") != null)
                    throw new InvalidOperationException("P06가 이미 적용되었습니다. 기존 자산을 재생성하지 않습니다.");
                var terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
                var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
                foreach (var point in placement.volumes.Concat(placement.junActors))
                    ValidateOptionalPosition(point.name, point.position, terrain, report);
                foreach (var actor in placement.islandMonsters)
                    ValidateOptionalPosition(actor.id, actor.position, terrain, report);
                var side = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(HerbFlowPath);
                var source = AssetDatabase.LoadAssetAtPath<FlowGraphSO>("Assets/10.Datas/Flow/FLOW_LakeSideQuestRoute.asset");
                // 기존 약초 그래프 GUID와 graphId를 유지하면서 누락된 선택 사건만 복원한다.
                foreach (var node in source.nodes)
                    if (side.GetNode(node.id) == null) side.nodes.Add(CloneOptionalNode(node));
                foreach (var edge in source.connections)
                    if (!side.connections.Any(e => e.fromNodeId == edge.fromNodeId && e.fromPort == edge.fromPort && e.toNodeId == edge.toNodeId))
                        side.connections.Add(JsonUtility.FromJson<FlowConnection>(JsonUtility.ToJson(edge)));
                foreach (string suffix in new[] { "herb_bed", "lantern_1", "lantern_2", "lantern_3" })
                {
                    string check = "optional_unexamined_" + suffix;
                    side.nodes.Add(new CheckFlagNode { id = check, flagKey = "lake.side." + suffix + "_examined", expectedValue = false });
                    RedirectFirstRecruitmentEdge(side, "entry_" + suffix, "Out", check);
                    AddFirstRecruitmentEdge(side, check, "True", "play_" + suffix);
                }
                // 수레 조사는 전투와 독립한다. 기존 서식 무리의 처치는 별도 트리거가 기록한다.
                RedirectFirstRecruitmentEdge(side, "entry_west_road", "Out", "play_west_inspection");
                side.connections.RemoveAll(e => e.fromNodeId == "mark_west_cleared");
                foreach (var entry in side.nodes.OfType<EntryNode>()) entry.repeatPolicy = FlowRepeatPolicy.WhileIdle;
                SaveReunionFlow(side);
                var jun = CopyOptional<FlowGraphSO>("Assets/10.Datas/Flow/FLOW_LakeJunRescue.asset", "FLOW_ScenarioJunRescue.asset", created);
                RedirectFirstRecruitmentEdge(jun, "entry_jun_trail", "Out", "notify_jun_trail_found");
                RedirectFirstRecruitmentEdge(jun, "entry_jun_rescue", "Out", "check_rescue_incomplete_from_volume");
                RedirectFirstRecruitmentEdge(jun, "entry_rescue_group_defeated", "Out", "check_rescue_incomplete_from_defeat");
                // 구조와 귀환 보고는 별개다. 미아와 정상 종료한 대화에서만 귀환 목표를 완료한다.
                RedirectFirstRecruitmentEdge(jun, "notify_jun_party_found", "Out", "mark_jun_rescued");
                foreach (var spawn in jun.nodes.OfType<SpawnStoryActorNode>())
                    spawn.position = GroundPoint(Array.Find(placement.junActors, p => p.name == spawn.actorId).position, terrain);
                foreach (var entry in jun.nodes.OfType<EntryNode>()) entry.repeatPolicy = FlowRepeatPolicy.WhileIdle;
                var rescue = CopyOptional<DialogueGraphSO>("Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Lake_JunRescue.asset", "DLG_ScenarioJunRescue.asset", created);
                // 처음 만난 경우에도 성립하는 기존 발화만 연결한다. 미아에게 들었다는 전제를 제거한다.
                rescue.GetNode("jun_rescue_01").nextNodeId = "jun_rescue_05";
                SaveOptionalDialogue(rescue);
                ((PlayDialogueNode)jun.GetNode("play_jun_rescue_dialogue")).dialogue = rescue;
                SaveReunionFlow(jun);
                PrepareOptionalRecovery(jun, side);
                ApplyOptionalVolumes(placement, side, jun, terrain);
                PrepareOptionalResidents(created, hazelPath);
                var facts = CreateOptionalFacts(created);
                var optionalRoot = GameObject.Find("ScenarioOptionalEvents");
                optionalRoot.SetActive(false);
                var factRunner = optionalRoot.AddComponent<FlowGraphRunner>();
                factRunner.SetGraph(facts);
                var factSettings = new SerializedObject(factRunner);
                factSettings.FindProperty("_fireManualEntryOnStart").boolValue = true;
                factSettings.ApplyModifiedPropertiesWithoutUndo();
                optionalRoot.SetActive(true);
                ApplyOptionalWorld(placement, world, terrain, created, report);
                AssertBaselineReferences(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("P06 씬 저장 실패");
                var worldJson = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(WorldPath));
                var monsters = (Newtonsoft.Json.Linq.JArray)worldJson["monsters"];
                var excluded = new HashSet<string>(placement.removedMonsters.Select(m => m.id));
                foreach (var monster in monsters.ToArray())
                    if (excluded.Contains((string)monster["id"])) monster.Remove();
                foreach (var monster in placement.islandMonsters)
                    monsters.Add(Newtonsoft.Json.Linq.JObject.Parse(JsonUtility.ToJson(monster)));
                File.WriteAllText(WorldPath, worldJson.ToString() + "\n");
                AssetDatabase.ImportAsset(WorldPath);
                File.WriteAllLines(OptionalOutput + "/Placement.txt", report);
                File.WriteAllLines(OptionalOutput + "/CreatedAssets.txt", created);
                File.WriteAllText(OptionalOutput + "/Apply.txt", "P06 씬·데이터 적용 성공. 실제 플레이는 미검증.\n");
            }
            catch
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                for (int i = 0; i < originals.Length; i++)
                {
                    File.Copy(backup + "/" + i, originals[i], true);
                    AssetDatabase.ImportAsset(originals[i], ImportAssetOptions.ForceUpdate);
                }
                foreach (string path in created) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally { if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static FlowNode CloneOptionalNode(FlowNode node) => (FlowNode)JsonUtility.FromJson(JsonUtility.ToJson(node), node.GetType());

        private static void PrepareOptionalRecovery(FlowGraphSO jun, FlowGraphSO side)
        {
            if (jun.GetNode("optional_loaded") == null)
            {
                jun.nodes.Add(new OnFlagsReloadedEntryNode { id = "optional_loaded", entryId = "Loaded", cancelRunningContexts = true, repeatPolicy = FlowRepeatPolicy.WhileIdle });
                jun.nodes.Add(new WaitPlayerInVolumeNode { id = "optional_wait_after_load", volumeId = "encounter_jun_rescue" });
                AddFirstRecruitmentEdge(jun, "optional_loaded", "Out", "optional_wait_after_load");
                AddFirstRecruitmentEdge(jun, "optional_wait_after_load", "Out", "check_rescue_incomplete_from_volume");
                jun.nodes.Add(new WaitPlayerInVolumeNode { id = "optional_wait_for_survivor", volumeId = "encounter_jun_rescue" });
                foreach (var edge in jun.connections)
                    if (edge.toNodeId == "spawn_jun") edge.toNodeId = "optional_wait_for_survivor";
                AddFirstRecruitmentEdge(jun, "optional_wait_for_survivor", "Out", "spawn_jun");
            }
            if (side.GetNode("optional_loaded") == null)
                side.nodes.Add(new OnFlagsReloadedEntryNode { id = "optional_loaded", entryId = "Loaded", cancelRunningContexts = true });
            SaveReunionFlow(jun);
            SaveReunionFlow(side);
        }

        private static T CopyOptional<T>(string source, string name, List<string> created) where T : UnityEngine.Object
        {
            string path = FirstRecruitmentAssets + "/" + name;
            if (File.Exists(path)) throw new InvalidOperationException("기존 P06 자산을 보존합니다: " + path);
            return CopyFirstRecruitmentAsset<T>(source, path, created);
        }

        private static void ValidateOptionalPosition(string name, Vector3 point, Terrain terrain, List<string> report)
        {
            var ground = GroundPoint(point, terrain);
            if (!NavMesh.SamplePosition(ground, out var hit, 3, NavMesh.AllAreas))
                throw new InvalidOperationException("P06 접근 NavMesh 없음: " + name + " " + ground);
            report.Add(name + " | ground=" + ground.ToString("F2") + " | nav=" + hit.position.ToString("F2"));
        }

        private static void ApplyOptionalVolumes(OptionalPlacement placement, FlowGraphSO side, FlowGraphSO jun, Terrain terrain)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HerbSourcePrefab));
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "ScenarioOptionalEvents";
            root.SetActive(false);
            foreach (var runner in root.GetComponents<FlowGraphRunner>()) UnityEngine.Object.DestroyImmediate(runner);
            var junRunner = root.AddComponent<FlowGraphRunner>();
            junRunner.SetGraph(jun);
            var herbRoot = GameObject.Find("ScenarioHerbRoute");
            var sideRunner = herbRoot.GetComponent<FlowGraphRunner>();
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
            {
                var point = Array.Find(placement.volumes, p => p.name == child.name);
                if (point == null) { UnityEngine.Object.DestroyImmediate(child.gameObject); continue; }
                bool isJun = child.name.StartsWith("Jun", StringComparison.Ordinal);
                if (!isJun) child.SetParent(herbRoot.transform);
                child.position = GroundPoint(point.position, terrain);
                var volume = new SerializedObject(child.GetComponent<FlowGraphTriggerVolume>());
                volume.FindProperty("_runner").objectReferenceValue = isJun ? junRunner : sideRunner;
                volume.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var interactable in root.GetComponentsInChildren<FlowGraphInteractable>(true).Concat(herbRoot.GetComponentsInChildren<FlowGraphInteractable>(true)))
            {
                var settings = new SerializedObject(interactable);
                settings.FindProperty("_requiredQuestId").stringValue = "";
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            root.SetActive(true);
        }

        private static void PrepareOptionalResidents(List<string> created, string hazelPath)
        {
            foreach (string name in new[] { "Mia", "Penny", "Morgan" })
            {
                string filename = name == "Mia" ? "DLG_Lake_Intro_Mia" : "DLG_Npc_" + name;
                var graph = CopyOptional<DialogueGraphSO>("Assets/10.Datas/Dialogue/Story/Dialogue/" + filename + ".asset", filename + "_Scenario.asset", created);
                if (name == "Mia")
                {
                    graph.startNodeId = "lake_mia_cond_rescued";
                    graph.GetNode("lake_mia_cond_rescued").falseNextNodeId = "lake_mia_cond_spoken";
                    graph.GetNode("lake_mia_cond_spoken").trueNextNodeId = "lake_mia_repeat";
                    var actions = graph.GetNode("lake_mia_return_event").eventActions;
                    actions.InsertRange(0, graph.GetNode("lake_mia_mark_spoken").eventActions);
                }
                if (name == "Morgan")
                {
                    var ready = ScriptableObject.CreateInstance<FlagConditionSO>();
                    ready.name = "Cond_WestRoadReady";
                    ready.flagKey = "lake.side.west_road_ready";
                    AssetDatabase.AddObjectToAsset(ready, graph);
                    graph.GetNode("morgan_cond_has_salt").condition = ready;
                }
                PreserveOptionalTracking(graph);
                SaveOptionalDialogue(graph);
                var data = CopyOptional<NpcActorSO>("Assets/10.Datas/Actor/Npc/NPC_" + name + ".asset", "NPC_" + name + "_Scenario.asset", created);
                data.dialogueGraph = graph;
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
                var npc = UnityEngine.Object.FindObjectsByType<NpcActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(actor => actor.ActorId == "NPC_" + name);
                var settings = new SerializedObject(npc);
                settings.FindProperty("_data").objectReferenceValue = data;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
            }
            var hazel = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(hazelPath);
            PreserveOptionalTracking(hazel);
            SaveOptionalDialogue(hazel);
        }

        private static void PreserveOptionalTracking(DialogueGraphSO graph)
        {
            foreach (var action in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graph)).OfType<StartQuestDialogueActionSO>())
            {
                var settings = new SerializedObject(action);
                settings.FindProperty("_trackOnAccept").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            // 수락 플래그 구독자가 완료 사실을 재전달할 때 퀘스트가 먼저 존재해야 한다.
            foreach (var node in graph.nodes)
                node.eventActions = node.eventActions.OrderBy(a => a is StartQuestDialogueActionSO ? 0 : 1).ToList();
        }

        private static void SaveOptionalDialogue(DialogueGraphSO graph)
        {
            foreach (var node in graph.nodes) EditorUtility.SetDirty(node);
            if (!graph.TryValidatePlayback(out string error)) throw new InvalidOperationException(error);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
        }

        private static FlowGraphSO CreateOptionalFacts(List<string> created)
        {
            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            graph.graphId = "flow_scenario_optional_facts";
            string[] flags = { "lake.story.jun_trail_found", "lake.side.jun_rescued", "lake.side.jun_finished", "lake.side.herb_bed_examined", "lake.side.lantern_1_examined", "lake.side.lantern_2_examined", "lake.side.lantern_3_examined", "lake.side.west_road_cleared", "lake.side.driver_tracks_found" };
            string[] events = { flags[0], "lake.story.jun_party_found", "lake.side.jun_returned", flags[3], flags[4], flags[5], flags[6], flags[7], flags[8] };
            graph.nodes.Add(new ManualEntryNode { id = "resume", entryId = "Resume", repeatPolicy = FlowRepeatPolicy.WhileIdle });
            AddFirstRecruitmentEdge(graph, "resume", "Out", "fact_0");
            graph.nodes.Add(new OnFlagsReloadedEntryNode { id = "loaded", entryId = "Loaded", repeatPolicy = FlowRepeatPolicy.WhileIdle });
            AddFirstRecruitmentEdge(graph, "loaded", "Out", "fact_0");
            string[] accepted = { "lake.story.mia_spoken", "lake.side.herb_accepted", "lake.side.lantern_accepted", "lake.side.salt_accepted" };
            int index = 0;
            foreach (string flag in flags.Concat(accepted))
            {
                string id = "changed_" + index++;
                graph.nodes.Add(new OnFlagChangedEntryNode { id = id, entryId = id, flagKey = flag, requiredValue = true, repeatPolicy = FlowRepeatPolicy.WhileIdle });
                AddFirstRecruitmentEdge(graph, id, "Out", "fact_0");
            }
            for (int i = 0; i < flags.Length; i++)
            {
                string next = i + 1 < flags.Length ? "fact_" + (i + 1) : "lantern_ready_0";
                graph.nodes.Add(new CheckFlagNode { id = "fact_" + i, flagKey = flags[i], expectedValue = true });
                graph.nodes.Add(new NotifyQuestStoryEventNode { id = "notify_" + i, eventId = events[i] });
                AddFirstRecruitmentEdge(graph, "fact_" + i, "True", "notify_" + i);
                AddFirstRecruitmentEdge(graph, "fact_" + i, "False", next);
                AddFirstRecruitmentEdge(graph, "notify_" + i, "Out", next);
            }
            for (int i = 0; i < 3; i++)
            {
                graph.nodes.Add(new CheckFlagNode { id = "lantern_ready_" + i, flagKey = flags[i + 4], expectedValue = true });
                AddFirstRecruitmentEdge(graph, "lantern_ready_" + i, "True", i == 2 ? "mark_lantern_ready" : "lantern_ready_" + (i + 1));
                AddFirstRecruitmentEdge(graph, "lantern_ready_" + i, "False", "west_ready_0");
            }
            graph.nodes.Add(new SetFlagNode { id = "mark_lantern_ready", flagKey = "lake.side.lantern_cleared", value = true });
            AddFirstRecruitmentEdge(graph, "mark_lantern_ready", "Out", "west_ready_0");
            graph.nodes.Add(new CheckFlagNode { id = "west_ready_0", flagKey = flags[7], expectedValue = true });
            graph.nodes.Add(new CheckFlagNode { id = "west_ready_1", flagKey = flags[8], expectedValue = true });
            graph.nodes.Add(new SetFlagNode { id = "mark_west_ready", flagKey = "lake.side.west_road_ready", value = true });
            AddFirstRecruitmentEdge(graph, "west_ready_0", "True", "west_ready_1");
            AddFirstRecruitmentEdge(graph, "west_ready_1", "True", "mark_west_ready");
            string path = FirstRecruitmentAssets + "/FLOW_ScenarioOptionalFacts.asset";
            if (File.Exists(path)) throw new InvalidOperationException("기존 P06 사실 동기화 그래프가 있습니다.");
            AssetDatabase.CreateAsset(graph, path);
            created.Add(path);
            SaveReunionFlow(graph);
            return graph;
        }

        private static void ApplyOptionalWorld(OptionalPlacement placement, WorldLayout world, Terrain terrain, List<string> created, List<string> report)
        {
            var removed = new HashSet<string>(placement.removedMonsters.Select(m => m.id));
            foreach (var identity in UnityEngine.Object.FindObjectsByType<SceneEntityId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (removed.Contains(identity.Guid)) UnityEngine.Object.DestroyImmediate(identity.gameObject);
            world.monsters = world.monsters.Where(m => !removed.Contains(m.id)).Concat(placement.islandMonsters).ToArray();
            var root = GameObject.Find("WorldExpansionActors").transform;
            PlaceWorldMonsters(new WorldLayout { monsters = placement.islandMonsters, meleeSlots = 1, rangedSlots = 1, breather = world.breather, alertRadius = 8 }, terrain, root);
            var group = root.Find(placement.westGroup);
            if (group == null) throw new InvalidOperationException("서쪽 서식 무리 누락: " + placement.westGroup);
            var source = ScriptableObject.CreateInstance<GroupDefeatedTriggerSourceSO>();
            var action = ScriptableObject.CreateInstance<SetFlagTriggerActionSO>();
            string sourcePath = FirstRecruitmentAssets + "/OptionalWestGroupSource.asset";
            string actionPath = FirstRecruitmentAssets + "/OptionalWestClearedAction.asset";
            foreach (string path in new[] { sourcePath, actionPath })
                if (File.Exists(path)) throw new InvalidOperationException("기존 P06 트리거 자산: " + path);
            var actionSettings = new SerializedObject(action);
            actionSettings.FindProperty("_key").stringValue = "lake.side.west_road_cleared";
            actionSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(source, sourcePath); created.Add(sourcePath);
            AssetDatabase.CreateAsset(action, actionPath); created.Add(actionPath);
            var trigger = new SerializedObject(group.gameObject.AddComponent<TriggerComposer>());
            trigger.FindProperty("_source").objectReferenceValue = source;
            trigger.FindProperty("_action").objectReferenceValue = action;
            trigger.FindProperty("_triggerId").stringValue = "scenario.optional.west_group";
            // 이전 저장으로 돌아온 뒤에도 재시도한다. 실제 완료 여부는 영속 플래그와 퀘스트가 소유한다.
            trigger.FindProperty("_repeat").enumValueIndex = (int)TriggerRepeatPolicy.Always;
            trigger.ApplyModifiedPropertiesWithoutUndo();
            report.Add("도전 제외=" + removed.Count + "; 기존 서식=25; 섬 추가=" + placement.islandMonsters.Length);
        }
    }
}
