using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Quest;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>피해 조사·동의·진실·지원의 경계와 본편 등록을 검증한다.</summary>
    public sealed class RedTreeR04ContentTests
    {
        private const string Root = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R04/";
        private FlowGraphSO Graph => AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Root + "FLOW_RedTreeVillageWaterReport.asset");

        [Test]
        public void 그래프는_유효하며_취소한_대화를_완료로_소비하지_않는다()
        {
            var issues = new List<string>();
            Assert.IsTrue(Graph.Validate(issues), string.Join("\n", issues));
            Assert.IsFalse(Graph.nodes.OfType<EntryNode>().Any(n => n.repeatPolicy == FlowRepeatPolicy.OncePerSave));
            foreach (var node in Graph.nodes.OfType<PlayDialogueNode>())
            {
                Assert.IsNotNull(node.dialogue, node.id);
                Assert.IsFalse(node.continueWhenCancelled, node.id);
            }
        }

        [Test]
        public void 진실은_영입과_독립적으로_저장되고_합류는_재시도할수있다()
        {
            Assert.IsTrue(Graph.connections.Any(e => e.fromNodeId == "mark_truth" && e.toNodeId == "recruit_seola"));
            Assert.IsTrue(Graph.connections.Any(e => e.fromNodeId == "entry_seola" && e.toNodeId == "recruit_seola"));
            foreach (var node in Graph.nodes.OfType<RecruitCharacterByConsentNode>())
            {
                Assert.IsNotNull(node.dialogue);
                Assert.IsNotNull(node.alreadyOwnedDialogue);
                Assert.AreNotEqual(node.requiredFlagKey, node.completedFlagKey);
            }
            Assert.AreEqual(2, Graph.nodes.OfType<RecruitCharacterByConsentNode>().Count());
        }

        [Test]
        public void 인계와_이동은_대화가_대신하지_않고_후반진실을_공개하지_않는다()
        {
            var keys = Graph.nodes.OfType<SetFlagNode>().Select(n => n.flagKey).ToArray();
            CollectionAssert.DoesNotContain(keys, "lake.red_tree.water_handed_over");
            CollectionAssert.DoesNotContain(keys, "lake.red_tree.departure_prepared");
            CollectionAssert.DoesNotContain(keys, "lake.red_tree.supplies_delivered");
            Assert.IsFalse(keys.Any(k => k.Contains("conceal") || k.Contains("truth.mark")));
            Assert.AreEqual("lake.red_tree.creek_cleared", Graph.nodes.OfType<SetFlagNode>().Single(n => n.id == "mark_creek_cleared").flagKey);
            Assert.IsTrue(Graph.connections.Any(e => e.fromNodeId == "wait_ent" && e.fromPort == "Defeated" && e.toNodeId == "mark_creek_cleared"));
        }

        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void 본편_퀘스트는_등록되고_보상은_중복하지_않는다(int number)
        {
            var quest = AssetDatabase.LoadAssetAtPath<QuestSO>($"Assets/10.Datas/Quest/RedTree/quest_main_red_tree_s0{number}.asset");
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>("Assets/10.Datas/Quest/QuestDatabase.asset");
            Assert.IsTrue(quest.isContentEnabled);
            CollectionAssert.Contains(db.QuestList, quest);
            Assert.IsTrue(quest.reward.IsEmpty);
            Assert.IsTrue(quest.autoComplete);
            Assert.IsTrue(quest.objectives.All(o => o.requiredCount == 1 && !string.IsNullOrEmpty(o.markerLocationId)));
        }
    }
}
