using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Data.Story;
using UPlayGround.Dialogue;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>P05의 취소·선행 조사·저장 ID와 실제 씬 연결을 검사한다.</summary>
    public sealed class ScenarioReunionTests
    {
        private const string Folder = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/";

        [TestCase("LianRescue", "flow_test_lian_rescue", "test.combat.honoka_rescue", 4)]
        [TestCase("MyoRyeongDuel", "flow_test_siuha_duel", "test.combat.lian_rescue", 9)]
        public void 선행영입과_저장식별자를_보존한다(string name, string graphId, string prerequisite, int recruit)
        {
            var graph = Load<FlowGraphSO>("FLOW_Scenario" + name);
            var errors = new List<string>();
            Assert.IsTrue(graph.Validate(errors), string.Join("\n", errors));
            Assert.AreEqual(graphId, graph.graphId);
            var definition = Load<RecruitmentEncounterDefinitionSO>("RecruitmentEncounter_Scenario" + name);
            Assert.AreEqual(prerequisite, definition.PrerequisiteEncounterId);
            Assert.AreEqual(recruit, (int)definition.RecruitCharacter);
            Assert.IsEmpty(definition.RequiredFlagKey, "단서를 건너뛰어도 영입 순서는 조우 선행 조건으로 보호한다.");
        }

        [TestCase("LianRescue", "lian_marker", "lake.story.honoka_joined")]
        [TestCase("MyoRyeongDuel", "drag_tracks", "lake.story.lian_joined")]
        public void 선행조사는_없는동료를_부르지않고_대화취소는_완료하지않는다(string name, string clue, string companion)
        {
            var graph = Load<FlowGraphSO>("FLOW_Scenario" + name);
            Edge(graph, "entry_" + clue, "Out", "check_" + clue + "_unexamined");
            Assert.AreEqual(companion, ((CheckFlagNode)graph.GetNode("check_clue_companion")).flagKey);
            Edge(graph, "check_clue_companion", "False", "notify_" + clue + "_examined");
            Assert.IsFalse(((PlayDialogueNode)graph.GetNode("play_" + clue + "_dialogue")).continueWhenCancelled);
            Edge(graph, "entry_volume", "Out", "mark_clue_at_encounter");
            Edge(graph, "notify_clue_at_encounter", "Out", "resume_encounter");
        }

        [Test]
        public void 묘령은_12초_충돌과_필수화해_후에만_영입한다()
        {
            var definition = Load<RecruitmentEncounterDefinitionSO>("RecruitmentEncounter_ScenarioMyoRyeongDuel");
            Assert.AreEqual(12f, definition.CombatTimeLimitSeconds);
            var graph = Load<FlowGraphSO>("FLOW_ScenarioMyoRyeongDuel");
            Edge(graph, "play_introduction", "Completed", "start_combat");
            Edge(graph, "play_introduction", "Rejected", "log_dialogue_cancelled");
            Edge(graph, "play_reconciliation", "Completed", "commit_after_victory");
            Edge(graph, "play_reconciliation", "Rejected", "log_dialogue_cancelled");
            Assert.IsInstanceOf<CommitRecruitmentEncounterNode>(graph.GetNode("commit_after_victory"));
        }

        [Test]
        public void 늦은퀘스트공개와_미리본자국을_복원한다()
        {
            var graph = Load<FlowGraphSO>("FLOW_ScenarioFirstQuest");
            Edge(graph, "notify_joined", "Out", "sync_lian_marker");
            Edge(graph, "notify_lian_marker", "Out", "sync_lian_joined");
            Edge(graph, "notify_lian_joined", "Out", "sync_tracks");
            Edge(graph, "notify_tracks", "Out", "sync_myo_joined");
            Edge(graph, "notify_myo_joined", "Out", "check_briefing");
            Edge(Load<FlowGraphSO>("FLOW_ScenarioLianRescue"), "notify_quest_progress", "Out", "sync_early_tracks");
        }

        [TestCase("LianRescue")]
        [TestCase("MyoRyeongConfrontation")]
        [TestCase("MyoRyeongJoined")]
        public void 대화는_유효하며_주인공화자는_활성동료로_고정하지않는다(string name)
        {
            var graph = Load<DialogueGraphSO>("DLG_Scenario" + name);
            Assert.IsTrue(graph.TryValidatePlayback(out string error), error);
            Assert.IsTrue(graph.nodes.Exists(node => node.speakerId == "Protagonist"));
            if (name == "LianRescue")
            {
                var beat = graph.GetNode("dlg_test_lian_rescue_06").stageBeat;
                Assert.IsFalse(beat.move);
                Assert.That(beat.holdSeconds, Is.InRange(5f, 8f));
            }
        }

        [Test]
        public void 씬에_두조우와_적셋이_연결되고_단서취소를_재시도할수있다()
        {
            var scene = EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity", OpenSceneMode.Additive);
            try
            {
                var ids = new HashSet<string>();
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!root.name.StartsWith("[STORY] RecruitmentEncounter_")) continue;
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        Assert.IsNotNull(component, root.name + " Missing Script");
                        var data = new SerializedObject(component);
                        var id = data.FindProperty("_guid");
                        if (component.GetType().Name == "SceneEntityId")
                            Assert.IsTrue(ids.Add(id.stringValue), "중복 저장 ID: " + id.stringValue);
                    }
                }
                foreach (string name in new[] { "LianRescue", "MyoRyeongDuel" })
                {
                    var root = System.Array.Find(scene.GetRootGameObjects(), candidate =>
                        candidate.name == "[STORY] RecruitmentEncounter_" + name);
                    Assert.IsNotNull(root);
                    Assert.AreSame(Load<FlowGraphSO>("FLOW_Scenario" + name), root.GetComponent<FlowGraphRunner>().Graph);
                    int hostiles = 0;
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        var data = new SerializedObject(component);
                        if (component.GetType().Name == "RecruitmentEncounterParticipant"
                            && data.FindProperty("_role").intValue == 1) hostiles++;
                        if (component.GetType().Name != "FlowGraphInteractable" || !component.gameObject.activeSelf) continue;
                        Assert.IsEmpty(data.FindProperty("_requiredQuestId").stringValue);
                        Assert.IsFalse(data.FindProperty("_shouldDeactivateAfterTrigger").boolValue);
                    }
                    Assert.AreEqual(name == "LianRescue" ? 3 : 0, hostiles);
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Folder + name + ".asset");
            Assert.IsNotNull(asset, name);
            return asset;
        }

        private static void Edge(FlowGraphSO graph, string from, string port, string to)
            => Assert.IsTrue(graph.connections.Exists(edge => edge.fromNodeId == from
                && edge.fromPort == port && edge.toNodeId == to), from + "." + port + " → " + to);
    }
}
