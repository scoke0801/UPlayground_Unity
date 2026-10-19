using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Quest;
using UPlayGround.Dialogue;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>반환 본편의 등록과 행동·서사·일회성 완료 경계를 검사한다.</summary>
    public sealed class RedTreeR09ContentTests
    {
        private const string Root = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R09/";
        private FlowGraphSO Graph => AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Root + "FLOW_RedTreeReturn.asset");

        [Test]
        public void 본편그래프와_모든대화가_로드되고_취소는_완료가아니다()
        {
            var issues = new List<string>();
            Assert.IsTrue(Graph.Validate(issues), string.Join("\n", issues));
            var guids = AssetDatabase.FindAssets("t:DialogueGraphSO", new[] { Root.TrimEnd('/') });
            Assert.GreaterOrEqual(guids.Length, 11);
            foreach (string guid in guids)
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(d.TryValidatePlayback(out var error), d.name + ": " + error);
                Assert.IsTrue(d.nodes.All(n => n.eventActions.Count == 0), "대사 시작에서 사실을 생산하지 않는다.");
            }
            Assert.IsTrue(Graph.nodes.OfType<PlayDialogueNode>().All(n => !n.continueWhenCancelled));
        }

        [Test]
        public void 물자와_노동과_출발은_대화로_완료하지않는다()
        {
            string[] keys = Graph.nodes.OfType<SetFlagNode>().Select(n => n.flagKey).ToArray();
            foreach (string fact in new[] { "packed", "cart_supported", "loaded", "handed_over", "channel_supported", "route_supported", "departed" })
                CollectionAssert.DoesNotContain(keys, "lake.red_tree.return." + fact);
            Assert.IsFalse(keys.Any(k => k.StartsWith("lake.red_tree.lucia.") || k.StartsWith("lake.red_tree.absorption.") || k.StartsWith("lake.red_tree.response.")));
        }

        [Test]
        public void 기존인계_ID_GUID_이벤트를_보존하고_새목표는_한번등록한다()
        {
            const string handoffPath = "Assets/10.Datas/Quest/RedTree/quest_main_red_tree_return_next.asset";
            Assert.AreEqual("fbaa0a0832a7d0b4f8a056e86c769f0b", AssetDatabase.AssetPathToGUID(handoffPath));
            var handoff = AssetDatabase.LoadAssetAtPath<QuestSO>(handoffPath);
            Assert.AreEqual("quest_main_red_tree_return_next", handoff.questId);
            Assert.AreEqual("lake.red_tree.return_work_started", handoff.objectives.Single().targetStringId);
            Assert.AreEqual("lake.red_tree.return_work_started", ((NotifyQuestStoryEventNode)Graph.GetNode("handoff_notify")).eventId);
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>("Assets/10.Datas/Quest/QuestDatabase.asset");
            Assert.AreEqual(1, db.QuestList.Count(q => q.questId == "quest_main_red_tree_s13"));
        }

        [Test]
        public void 본편보상과_자동재시작은_없고_작업별_마커가있다()
        {
            var q = AssetDatabase.LoadAssetAtPath<QuestSO>("Assets/10.Datas/Quest/RedTree/quest_main_red_tree_s13.asset");
            Assert.IsTrue(q.isContentEnabled && q.autoComplete && q.reward.IsEmpty);
            Assert.IsFalse(q.isRepeatable || q.autoAcceptOnNewGame);
            Assert.IsEmpty(q.autoAcceptNextQuestIds);
            Assert.AreEqual(13, q.objectives.Count);
            Assert.IsTrue(q.objectives.All(o => o.requiredCount == 1 && o.markerLocationId.StartsWith("lake.red_tree.r09.")));
        }

        [Test]
        public void 최종식사는_출발을_요구하고_기존승리는_다시생산하지않는다()
        {
            Assert.AreEqual("lake.red_tree.return.departed", ((CheckFlagNode)Graph.GetNode("require_meal")).flagKey);
            foreach (string fact in new[] { "lucia.victory", "lucia.attacks_cleared", "absorption.stopped", "absorption.observed" })
                Assert.IsTrue(Graph.nodes.OfType<CheckFlagNode>().Any(n => n.flagKey == "lake.red_tree." + fact));
            Assert.IsFalse(Graph.nodes.OfType<SetFlagNode>().Any(n => !n.value));
        }

        [Test]
        public void 최초주인공과_미선택주인공의_화자를_분리한다()
        {
            Assert.IsInstanceOf<StoryProtagonistCondition>(((BranchNode)Graph.GetNode("first_protagonist")).condition);
            Assert.AreEqual("아린", AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_transport_arin.asset").nodes[0].speakerId);
            Assert.AreEqual("라온", AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_transport_raon.asset").nodes[0].speakerId);
            var meal = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_meal.asset");
            Assert.IsTrue(meal.nodes.Any(n => n.speakerId == "Protagonist"));
            Assert.IsFalse(meal.nodes.Any(n => n.speakerId == "Player" || n.speakerId == "루시아"));
        }

        [Test]
        public void 피해주민은_기존몸을_사용하고_먼안내인은_출발점에서_말한다()
        {
            foreach (string id in new[] { "play_wishes", "play_revisit_field" })
            {
                var node = (PlayDialogueNode)Graph.GetNode(id);
                Assert.AreEqual("RedTree_RemainingResident", node.partnerActorId);
                Assert.AreEqual("남은 주민", node.partnerSpeakerId);
            }
            var meal = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_meal.asset");
            Assert.IsFalse(meal.nodes.Any(n => n.speakerId == "안내인"));
            Assert.IsTrue(meal.nodes.Any(n => n.stageBeat.enabled && n.stagePropPrefab != null));
            Assert.AreEqual("안내인", AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_guide.asset").nodes[0].speakerId);
        }
    }
}
