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
                    && !path.Contains("DLG_Npc_")) continue;
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

        [Test]
        public void 묘령은_화해가_끝난_뒤_합류하고_기존_해금저장은_후속대화로_복구한다()
        {
            FlowGraphSO graph = LoadGraph("Test/FLOW_Test_MyoRyeongDuel");
            Assert.IsInstanceOf<PlayDialogueRequiredNode>(graph.GetNode("play_reconciliation"));
            Assert.IsInstanceOf<CommitRecruitmentEncounterNode>(graph.GetNode("commit_after_victory"));
            AssertEdge(graph, "prepare_result", "Ready", "play_reconciliation");
            AssertEdge(graph, "play_reconciliation", "Completed", "commit_after_victory");
            AssertEdge(graph, "play_reconciliation", "Rejected", "log_dialogue_cancelled");
            AssertEdge(graph, "commit_after_victory", "Completed", "finalize_recruitment");
            AssertEdge(graph, "resume_encounter", "PostDialogue", "play_post_dialogue");
            Assert.IsFalse(graph.nodes.Exists(node => node is CommitRecruitmentAfterVictoryNode));
        }

        [Test]
        public void 묘령_화해는_주인공이_먼저_거리를_내준_후_시작한다()
        {
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(
                "Assets/10.Datas/Dialogue/Test/DLG_Test_MyoRyeongJoined.asset");
            Assert.AreEqual(NodeType.Event, graph.StartNode.nodeType);
            Assert.IsTrue(graph.StartNode.stageBeat.enabled);
            Assert.IsTrue(graph.StartNode.stageBeat.move);
            Assert.AreEqual("Protagonist", graph.StartNode.stageBeat.actorSpeakerId);
            Assert.IsTrue(graph.TryValidatePlayback(out string error), error);
        }

        [Test]
        public void 결말_완료는_귀로_정상종료_뒤이며_저장복원은_격파된_보스를_건너뛴다()
        {
            FlowGraphSO graph = LoadGraph("FLOW_LakeShrineChapter1");
            AssertEdge(graph, "check_alternate_defeated", "True", "check_farewell");
            AssertEdge(graph, "check_farewell", "True", "wait_return");
            AssertEdge(graph, "play_victory", "Out", "mark_farewell");
            AssertEdge(graph, "wait_return", "Out", "play_return");
            AssertEdge(graph, "play_return", "Out", "notify_return");
            AssertEdge(graph, "notify_return", "Out", "mark_chapter_completed");
            AssertEdge(graph, "mark_chapter_completed", "Out", "complete_shrine_quest");
            foreach (string id in new[] { "play_arrival", "play_treasure", "play_treasure_reveal", "play_victory", "play_return" })
            {
                AssertEdge(graph, id, PlayDialogueNode.CancelledPort, "retry_" + id);
                AssertEdge(graph, "retry_" + id, "Out", id);
                Assert.IsFalse(((PlayDialogueNode)graph.GetNode(id)).continueWhenCancelled);
            }
            Assert.AreEqual(FlowRepeatPolicy.WhileIdle, ((GateNode)graph.GetNode("gate_guardian")).policy);
            Assert.AreEqual(FlowRepeatPolicy.WhileIdle, ((GateNode)graph.GetNode("gate_alternate")).policy);
        }

        [Test]
        public void 최종상대는_공개가_끝나야_목표와_전투가_열린다()
        {
            FlowGraphSO graph = LoadGraph("FLOW_LakeShrineChapter1");
            Assert.IsTrue(((SpawnStoryActorNode)graph.GetNode("spawn_alternate")).holdCombatUntilReleased);
            AssertEdge(graph, "spawn_alternate", "Spawned", "play_treasure_reveal");
            AssertEdge(graph, "play_treasure_reveal", "Out", "notify_treasure");
            AssertEdge(graph, "notify_treasure", "Out", "release_alternate");
            AssertEdge(graph, "release_alternate", "Out", "wait_alternate");
        }

        [TestCase("Humanoid_ProtectWound")]
        [TestCase("Humanoid_PackMedicine")]
        public void 부상과_생활_모션은_실재_클립을_참조한다(string name)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/" + name + ".asset");
            Assert.IsNotNull(asset);
            var serialized = new SerializedObject(asset);
            Assert.IsNotNull(serialized.FindProperty("motionSet.motions").GetArrayElementAtIndex(0)
                .FindPropertyRelative("motionClip").objectReferenceValue);
        }

        [Test]
        public void 무언_이동은_대상이나_제한시간이_없으면_재생을_거부한다()
        {
            var graph = ScriptableObject.CreateInstance<DialogueGraphSO>();
            var beat = ScriptableObject.CreateInstance<DialogueNodeSO>();
            var end = ScriptableObject.CreateInstance<DialogueNodeSO>();
            try
            {
                beat.nodeId = "step";
                beat.nodeType = NodeType.Event;
                beat.nextNodeId = "end";
                beat.stageBeat.enabled = true;
                beat.stageBeat.move = true;
                end.nodeId = "end";
                end.nodeType = NodeType.End;
                graph.nodes.Add(beat);
                graph.nodes.Add(end);
                graph.startNodeId = "step";
                Assert.IsFalse(graph.TryValidatePlayback(out _));
                beat.stageBeat.actorSpeakerId = "Protagonist";
                beat.stageBeat.timeoutSeconds = 0f;
                Assert.IsFalse(graph.TryValidatePlayback(out _));
                beat.stageBeat.timeoutSeconds = 5f;
                Assert.IsTrue(graph.TryValidatePlayback(out _));
                beat.channel = DialogueChannel.System;
                Assert.IsFalse(graph.TryValidatePlayback(out _));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(beat);
                UnityEngine.Object.DestroyImmediate(end);
            }
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
