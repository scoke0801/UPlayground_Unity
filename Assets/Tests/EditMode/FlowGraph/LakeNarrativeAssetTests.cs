using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>호수 스토리의 대화 연결과 조사 대상, 영입 이후 진행 경계를 검증한다.</summary>
    public sealed class LakeNarrativeAssetTests
    {
        [TestCase("Test/FLOW_Test_HwarinRescue")]
        [TestCase("Test/FLOW_Test_LianRescue")]
        [TestCase("Test/FLOW_Test_MyoRyeongDuel")]
        [TestCase("FLOW_LakeJunRescue")]
        [TestCase("FLOW_LakeSearchQuestLine")]
        [TestCase("FLOW_LakeShrineChapter1")]
        public void 스토리_그래프의_모든_노드와_포트가_유효하다(string name)
        {
            FlowGraphSO graph = LoadGraph(name);
            var errors = new List<string>();
            Assert.IsTrue(graph.Validate(errors), string.Join("\n", errors));
        }

        [Test]
        public void 호수_대화는_시작점에서_정상적으로_연결된다()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueGraphSO",
                         new[] { "Assets/10.Datas/Dialogue" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("DLG_Lake_") && !path.Contains("DLG_Test_Hwarin")
                    && !path.Contains("DLG_Test_Lian") && !path.Contains("DLG_Test_MyoRyeong")
                    && !path.Contains("DLG_Npc_Joan")) continue;
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
                Assert.IsTrue(graph.TryValidatePlayback(out string error), path + ": " + error);
            }
        }

        [TestCase("Hwarin")]
        [TestCase("Lian")]
        public void 공동전투_영입은_한번의_필수대화_후_확정하고_저장복원도_완료한다(string name)
        {
            FlowGraphSO graph = LoadGraph("Test/FLOW_Test_" + name + "Rescue");
            Assert.IsInstanceOf<PlayDialogueRequiredNode>(graph.GetNode("play_dialogue"));
            AssertEdge(graph, "play_dialogue", "Completed", "commit_recruitment");
            AssertEdge(graph, "commit_recruitment", "Completed", "finalize_recruitment");
            AssertEdge(graph, "resume_encounter", "PostDialogue", "finalize_recruitment");
            Assert.IsFalse(graph.nodes.Exists(node => node is PlayRecruitmentPostDialogueNode));
        }

        [Test]
        public void 묘령_대화_중단은_완료로_소진하지_않고_정상종료_후_메인목표를_추적한다()
        {
            FlowGraphSO graph = LoadGraph("Test/FLOW_Test_MyoRyeongDuel");
            AssertEdge(graph, "play_post_dialogue", "Rejected", "log_dialogue_cancelled");
            AssertEdge(graph, "play_post_dialogue", "Completed", "finalize_recruitment");
            AssertEdge(graph, "notify_quest_progress", "Out", "check_shrine_quest_active");
            AssertEdge(graph, "check_shrine_quest_active", "True", "track_shrine_quest");
            Assert.AreEqual("quest_main_lake_shrine",
                ((TrackQuestNode)graph.GetNode("track_shrine_quest")).questId);
        }

        [Test]
        public void 준_조사점에는_보이는_짐과_올바른_진입점이_있다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Story/Story_LakeSideQuestRoute.prefab");
            Assert.IsNotNull(prefab);
            Transform clue = prefab.transform.Find("JunTrailVolume");
            Assert.IsNotNull(clue);
            Transform sack = clue.Find("JunLostSack");
            Assert.IsNotNull(sack, "이펙트만 있는 빈 조사점이 아니라 실제 짐이 필요합니다.");
            Assert.IsNotEmpty(sack.GetComponentsInChildren<Renderer>(true));
            var volume = new SerializedObject(clue.GetComponent<FlowGraphTriggerVolume>());
            var runner = volume.FindProperty("_runner").objectReferenceValue as FlowGraphRunner;
            Assert.IsNotNull(runner);
            string volumeId = volume.FindProperty("_volumeId").stringValue;
            Assert.IsTrue(runner.Graph.nodes.Exists(node =>
                node is OnTriggerVolumeEntryNode entry && entry.volumeId == volumeId));
            Assert.AreEqual("clue_jun_trail", volumeId);
        }

        private static FlowGraphSO LoadGraph(string name)
        {
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>("Assets/10.Datas/Flow/" + name + ".asset");
            Assert.IsNotNull(graph, name);
            return graph;
        }

        private static void AssertEdge(FlowGraphSO graph, string from, string port, string to)
        {
            Assert.IsTrue(graph.connections.Exists(edge =>
                edge.fromNodeId == from && edge.fromPort == port && edge.toNodeId == to),
                $"{graph.name}: {from}.{port} → {to} 연결 누락");
        }
    }
}
