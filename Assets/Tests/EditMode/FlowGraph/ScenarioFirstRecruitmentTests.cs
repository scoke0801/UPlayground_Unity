using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Story;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>선행 발견과 영입 정상 종료의 시나리오 전용 연결을 검증한다.</summary>
    public sealed class ScenarioFirstRecruitmentTests
    {
        private const string Folder = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/";

        [Test]
        public void 사용자에게_넘긴_개발_플레이는_검증을_재시작하지_않는다()
        {
            var type = System.Type.GetType("UPlayGround.Tool.Editor.Map.ScenarioMapAuthoring, Assembly-CSharp-Editor", true);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            string key = (string)type.GetField("SmokeHandedOffKey", flags).GetRawConstantValue();
            bool previous = SessionState.GetBool(key, false);
            var report = type.GetField("s_smoke", flags);
            object before = report.GetValue(null);
            try
            {
                SessionState.SetBool(key, true);
                type.GetMethod("BeginSmoke", flags).Invoke(null, null);
                Assert.AreSame(before, report.GetValue(null), "재컴파일 후 사용자 플레이에 새 로드·검증 요청을 만들지 않는다.");
            }
            finally
            {
                SessionState.SetBool(key, previous);
            }
        }

        [TestCase("FLOW_ScenarioFirstQuest")]
        [TestCase("FLOW_ScenarioHwarinRescue")]
        public void 첫_영입_그래프의_노드와_포트가_유효하다(string name)
        {
            var graph = Load(name);
            var errors = new List<string>();
            Assert.IsTrue(graph.Validate(errors), string.Join("\n", errors));
        }

        [Test]
        public void 단서_선행발견을_허용하고_취소는_조사를_완료하지_않는다()
        {
            var graph = Load("FLOW_ScenarioHwarinRescue");
            AssertEdge(graph, "entry_red_cloth", "Out", "check_red_cloth_unexamined");
            Assert.IsFalse(((PlayDialogueNode)graph.GetNode("play_red_cloth_dialogue")).continueWhenCancelled);
            var scenario = AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(Folder + "RecruitmentEncounter_ScenarioHwarin.asset");
            var original = AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(
                "Assets/10.Datas/Story/Test/RecruitmentEncounter_Test_HwarinRescue.asset");
            Assert.IsEmpty(scenario.RequiredFlagKey);
            Assert.AreEqual("lake.story.red_cloth_examined", original.RequiredFlagKey);
            Assert.AreEqual(original.EncounterId, scenario.EncounterId);
        }

        [Test]
        public void 안내_후_단서와_영입_사실을_다시_반영한다()
        {
            var graph = Load("FLOW_ScenarioFirstQuest");
            AssertEdge(graph, "notify_guide", "Out", "sync_red_cloth");
            AssertEdge(graph, "sync_red_cloth", "True", "notify_red_cloth");
            AssertEdge(graph, "notify_red_cloth", "Out", "sync_joined");
            AssertEdge(graph, "sync_joined", "True", "notify_joined");
            Assert.AreEqual("lake.story.honoka_joined", ((NotifyQuestStoryEventNode)graph.GetNode("notify_joined")).eventId);
        }

        [Test]
        public void 정상종료_영입_경계와_원본_저장키를_유지한다()
        {
            var graph = Load("FLOW_ScenarioHwarinRescue");
            Assert.AreEqual("flow_test_honoka_rescue", graph.graphId);
            Assert.IsInstanceOf<PlayDialogueRequiredNode>(graph.GetNode("play_dialogue"));
            AssertEdge(graph, "play_dialogue", "Completed", "commit_recruitment");
            AssertEdge(graph, "play_dialogue", "Rejected", "log_dialogue_cancelled");
            AssertEdge(graph, "resume_encounter", "PostDialogue", "finalize_recruitment");
            AssertEdge(graph, "entry_volume", "Out", "mark_cloth_at_encounter");
        }

        private static FlowGraphSO Load(string name)
        {
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Folder + name + ".asset");
            Assert.IsNotNull(graph, name);
            return graph;
        }

        [Test]
        public void 시나리오_안내_대화의_행동과_종료_경로가_유효하다()
        {
            var graph = AssetDatabase.LoadAssetAtPath<UPlayGround.Dialogue.DialogueGraphSO>(Folder + "DLG_ScenarioGuide.asset");
            Assert.IsNotNull(graph);
            Assert.IsTrue(graph.TryValidatePlayback(out string error), error);
        }

        [Test]
        public void 화린은_이동_없이_동행_제안에서_자기소개와_종료로_이어진다()
        {
            var graph = AssetDatabase.LoadAssetAtPath<UPlayGround.Dialogue.DialogueGraphSO>(Folder + "DLG_ScenarioHwarinRescue.asset");
            Assert.IsTrue(graph.TryValidatePlayback(out string error), error);
            string current = graph.startNodeId;
            foreach (string suffix in new[] { "01", "02", "03", "04", "05", "13", "14" })
            {
                Assert.AreEqual("dlg_test_honoka_rescue_" + suffix, current);
                var node = graph.GetNode(current);
                Assert.AreEqual(UPlayGround.Dialogue.NodeType.Talk, node.nodeType);
                Assert.IsFalse(node.stageBeat?.enabled == true, "자기소개를 위한 강제 이동이나 무언 대기를 넣지 않는다.");
                Assert.IsNotEmpty(node.dialogueText);
                Assert.IsNotEmpty(node.listenerSpeakerId);
                current = node.nextNodeId;
            }
            Assert.AreEqual("dlg_test_honoka_rescue_end", current);
            Assert.AreEqual(UPlayGround.Dialogue.NodeType.End, graph.GetNode(current).nodeType);
            var proposal = graph.GetNode("dlg_test_honoka_rescue_05");
            Assert.AreEqual("Protagonist", proposal.speakerId);
            Assert.AreEqual(UPlayGround.Data.DialogueShotType.TwoShot, proposal.shotType);
        }

        private static void AssertEdge(FlowGraphSO graph, string from, string port, string to) =>
            Assert.IsTrue(graph.connections.Exists(edge => edge.fromNodeId == from && edge.fromPort == port && edge.toNodeId == to),
                from + "." + port + " → " + to);

        [TestCase(UPlayGround.Dialogue.DialogueStageTimeoutPolicy.CancelDialogue, false, 0, 1)]
        [TestCase(UPlayGround.Dialogue.DialogueStageTimeoutPolicy.ContinueDialogue, false, 1, 0)]
        [TestCase(UPlayGround.Dialogue.DialogueStageTimeoutPolicy.ContinueDialogue, true, 0, 0)]
        public void 부수_이동_시간초과만_진행하고_사용자_취소는_정상종료로_바꾸지_않는다(
            UPlayGround.Dialogue.DialogueStageTimeoutPolicy policy, bool cancelled, int completed, int failed)
        {
            var type = System.Type.GetType("UPlayGround.Dialogue.DialogueManager, Assembly-CSharp", true);
            var owner = new UnityEngine.GameObject("StageTimeoutPolicyTest");
            owner.SetActive(false);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            try
            {
                var manager = owner.AddComponent(type);
                int completions = 0, failures = 0;
                type.GetField("_stageBeat", flags).SetValue(manager,
                    new UPlayGround.Dialogue.DialogueStageBeat { timeoutPolicy = policy });
                type.GetField("_stageBeatCompleted", flags).SetValue(manager, (System.Action)(() => completions++));
                type.GetField("_stageBeatFailed", flags).SetValue(manager, (System.Action)(() => failures++));
                if (cancelled) type.GetMethod("CancelStageBeat", flags).Invoke(manager, null);
                if (policy == UPlayGround.Dialogue.DialogueStageTimeoutPolicy.CancelDialogue && !cancelled)
                    UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
                        new System.Text.RegularExpressions.Regex("무언 행동 중단"));
                type.GetMethod("FinishStageMovementTimeout", flags).Invoke(manager, null);
                Assert.AreEqual(completed, completions);
                Assert.AreEqual(failed, failures);
                Assert.IsNull(type.GetField("_stageBeat", flags).GetValue(manager));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
