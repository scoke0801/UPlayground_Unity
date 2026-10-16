using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Data.Save;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>선택 사건의 수락 독립성·귀환 경계·배치 ID를 실제 저작 자산에서 검사한다.</summary>
    public sealed class ScenarioOptionalEventsTests
    {
        private const string Folder = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/";

        [Serializable] private sealed class Audit { public List<AuditEntry> monsters; }
        [Serializable] private sealed class AuditEntry { public string prefab; public List<string> issues; }

        [Test]
        public void 섬의_선택적은_현재_BT와_MotionKey_HitPhase가_완전하다()
        {
            Type.GetType("UPlayGround.Tool.Editor.Map.ScenarioMapAuthoring, Assembly-CSharp-Editor", true)
                .GetMethod("InspectWorldMonsters").Invoke(null, null);
            const string path = "output/ScenarioMap/WorldMonsterAudit.json";
            var audit = JsonUtility.FromJson<Audit>(File.ReadAllText(path));
            foreach (string name in new[] { "Monster_ChildPlant_V1", "Monster_ChildPlant_V2", "Monster_Griffin_Dark" })
            {
                var entry = audit.monsters.Single(m => m.prefab.EndsWith("/" + name + ".prefab", StringComparison.Ordinal));
                Assert.IsEmpty(entry.issues, name + ": " + string.Join(";", entry.issues));
            }
            File.Copy(path, "output/ScenarioGameplay/P06/WorldMonsterAudit.json", true);
        }

        [Test]
        public void 현재배치에서_제외된_저장개체는_재소환하지않는다()
        {
            var type = Type.GetType("UPlayGround.Manager.MonsterRespawnManager, Assembly-CSharp", true);
            var root = new GameObject("RemovedPlacementTest");
            root.SetActive(false);
            try
            {
                var manager = root.AddComponent(type);
                var method = type.GetMethod("SpawnFromState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNull(method.Invoke(manager, new object[] { new MonsterRespawnState { guid = "removed_challenge", actorId = "old_actor" } }));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(10f)]
        [TestCase(12f)]
        public void 재생성된적은_현재배치의_추격경계를_복원한다(float distance)
        {
            var manager = Type.GetType("UPlayGround.Manager.MonsterRespawnManager, Assembly-CSharp", true);
            var placementType = manager.GetNestedType("PlacementInfo", BindingFlags.NonPublic);
            var placement = Activator.CreateInstance(placementType);
            placementType.GetField("chaseDistance").SetValue(placement, (float?)distance);
            var root = new GameObject("ChaseBoundaryTest");
            root.SetActive(false);
            try
            {
                var monster = root.AddComponent(Type.GetType("UPlayGround.MonsterActor, UPlayGround.Actor", true));
                var detectionType = Type.GetType("UPlayGround.Components.EnemyDetection, UPlayGround.Actor", true);
                var detection = root.GetComponent(detectionType) ?? root.AddComponent(detectionType);
                manager.GetMethod("RestoreChaseBoundary", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { monster, placement });
                Assert.AreEqual(distance, new SerializedObject(detection).FindProperty("_maxChaseDistanceFromAnchor").floatValue);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase("FLOW_ScenarioHerbRoute", "flow_lake_side_quest_route")]
        [TestCase("FLOW_ScenarioJunRescue", "flow_lake_jun_rescue")]
        [TestCase("FLOW_ScenarioOptionalFacts", "flow_scenario_optional_facts")]
        public void 저장식별자와_그래프연결이_유효하다(string asset, string id)
        {
            var graph = Load<FlowGraphSO>(asset);
            Assert.AreEqual(id, graph.graphId);
            var errors = new List<string>();
            Assert.IsTrue(graph.Validate(errors), string.Join("\n", errors));
        }

        [TestCase("herb_bed")]
        [TestCase("lantern_1")]
        [TestCase("lantern_2")]
        [TestCase("lantern_3")]
        public void 조사는_수락전에도_열리고_취소시_사실을_남기지않는다(string suffix)
        {
            var graph = Load<FlowGraphSO>("FLOW_ScenarioHerbRoute");
            Edge(graph, "entry_" + suffix, "Out", "optional_unexamined_" + suffix);
            Assert.IsFalse(((CheckFlagNode)graph.GetNode("optional_unexamined_" + suffix)).expectedValue);
            Edge(graph, "optional_unexamined_" + suffix, "True", "play_" + suffix);
            Assert.IsFalse(((PlayDialogueNode)graph.GetNode("play_" + suffix)).continueWhenCancelled);
        }

        [Test]
        public void 구조만으로_귀환보고를_완료하지않는다()
        {
            var graph = Load<FlowGraphSO>("FLOW_ScenarioJunRescue");
            Edge(graph, "notify_jun_party_found", "Out", "mark_jun_rescued");
            Assert.IsTrue(((OnFlagsReloadedEntryNode)graph.GetNode("optional_loaded")).cancelRunningContexts);
            Edge(graph, "optional_wait_for_survivor", "Out", "spawn_jun");
            Assert.IsFalse(graph.connections.Any(e => e.toNodeId == "notify_jun_returned"));
            var mia = Load<DialogueGraphSO>("DLG_Lake_Intro_Mia_Scenario");
            Assert.AreEqual("lake_mia_cond_rescued", mia.startNodeId);
            Assert.IsTrue(mia.GetNode("lake_mia_return_event").eventActions.Any(a => a.GetType().Name == "StartQuestDialogueActionSO"));
        }

        [Test]
        public void 수레조사는_전투와_소금보유에_의존하지않는다()
        {
            Edge(Load<FlowGraphSO>("FLOW_ScenarioHerbRoute"), "entry_west_road", "Out", "play_west_inspection");
            var condition = Load<DialogueGraphSO>("DLG_Npc_Morgan_Scenario").GetNode("morgan_cond_has_salt").condition;
            var serialized = new SerializedObject(condition);
            Assert.AreEqual("lake.side.west_road_ready", serialized.FindProperty("flagKey").stringValue);
        }

        [TestCase("DLG_Lake_Intro_Mia_Scenario")]
        [TestCase("DLG_Npc_Hazel_Scenario")]
        [TestCase("DLG_Npc_Penny_Scenario")]
        [TestCase("DLG_Npc_Morgan_Scenario")]
        public void 선택수락은_기존추적을_덮지않는다(string name)
        {
            var actions = AssetDatabase.LoadAllAssetsAtPath(Folder + name + ".asset").Where(a => a.GetType().Name == "StartQuestDialogueActionSO").ToArray();
            Assert.IsNotEmpty(actions);
            foreach (var action in actions) Assert.IsFalse(new SerializedObject(action).FindProperty("_trackOnAccept").boolValue);
        }

        [Test]
        public void 실제씬은_선택사건과_섬배치를_갖고_도전캐릭터를_제외한다()
        {
            var scene = EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity", OpenSceneMode.Additive);
            try
            {
                var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                Assert.AreEqual(1, transforms.Count(t => t.name == "ScenarioOptionalEvents"));
                foreach (string id in new[] { "scenario_o05_bamboo_01", "scenario_o05_bamboo_02", "scenario_o06_rock_01" })
                    Assert.AreEqual(1, transforms.Count(t => t.name == id), id);
                var world = transforms.Single(t => t.name == "WorldExpansionActors");
                var west = world.Find("뿌리_군락");
                var composer = west.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "TriggerComposer");
                Assert.AreEqual(3, new SerializedObject(composer).FindProperty("_repeat").enumValueIndex, "같은 씬에서 이전 저장을 불러온 뒤에도 재시도한다.");
                foreach (Transform group in world)
                    if (group.name.Contains("도전_")) Assert.AreEqual(0, group.childCount, group.name);
                var runners = transforms.SelectMany(t => t.GetComponents<FlowGraphRunner>()).ToArray();
                Assert.AreEqual(1, runners.Count(r => r.Graph != null && r.Graph.graphId == "flow_scenario_optional_facts"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Folder + name + ".asset");
            Assert.IsNotNull(asset, name);
            return asset;
        }
        private static void Edge(FlowGraphSO graph, string from, string port, string to) =>
            Assert.IsTrue(graph.connections.Any(e => e.fromNodeId == from && e.fromPort == port && e.toNodeId == to), from + " → " + to);
    }
}
