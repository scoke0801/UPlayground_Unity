using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Dialogue;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>表식 조사와 개별 동의·물자 소유·후속 진행의 본편 데이터 계약을 검사한다.</summary>
    public sealed class RedTreeR06ContentTests
    {
        private const string Root = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R06/";
        private FlowGraphSO Graph => AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Root + "FLOW_RedTreeMarksSupply.asset");

        [Test]
        public void 그래프와_모든대화가_유효하고_취소는_완료가아니다()
        {
            var issues = new List<string>();
            Assert.IsTrue(Graph.Validate(issues), string.Join("\n", issues));
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueGraphSO", new[] { Root.TrimEnd('/') }))
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(d.TryValidatePlayback(out var error), d.name + error);
            }
            Assert.IsTrue(Graph.nodes.OfType<PlayDialogueNode>().All(n => !n.continueWhenCancelled));
        }

        [Test]
        public void 운송원문은_세군데에서_독립열람하고_공통표기로_대조한다()
        {
            var keys = new HashSet<string>();
            foreach (string id in new[] { "list", "cargo", "bundle" })
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_" + id + ".asset");
                var line = d.nodes.First(n => n.nodeType == NodeType.Talk);
                Assert.AreEqual(0, line.typingSpeed);
                StringAssert.Contains("개울가 · 두 줄 매듭", line.dialogueText);
                StringAssert.Contains("마른 우물 옆 집", line.dialogueText);
                Assert.IsTrue(keys.Add(new SerializedObject(line.eventActions.Single()).FindProperty("flagKey").stringValue));
                StringAssert.DoesNotContain("피해를 넓", line.dialogueText);
            }
        }

        [Test]
        public void 분리와_인계는_대화나_흐름플래그로_대체하지않는다()
        {
            var keys = Graph.nodes.OfType<SetFlagNode>().Select(n => n.flagKey).ToArray();
            foreach (string k in new[] { "trial_resident_safe", "marker.trial_creek.disconnected", "offering_handed_over", "external_water_secured", "external_water_handed_over" })
                CollectionAssert.DoesNotContain(keys, "lake.red_tree." + k);
            Assert.AreEqual("lake.red_tree.marker.trial_creek.disconnected", Graph.nodes.OfType<CheckFlagNode>().Single(n => n.id == "require_after").flagKey);
        }

        [Test]
        public void 진실과_책임과_세명의_동의는_별도다()
        {
            var recruits = Graph.nodes.OfType<RecruitCharacterByConsentNode>().ToArray();
            CollectionAssert.AreEquivalent(new[] { CharacterActorType.Myomyo, CharacterActorType.YeonHoa, CharacterActorType.Sera }, recruits.Select(n => n.recruitCharacter));
            foreach (var n in recruits) { Assert.NotNull(n.dialogue); Assert.NotNull(n.alreadyOwnedDialogue); Assert.AreNotEqual(n.requiredFlagKey, n.completedFlagKey); }
            Assert.AreEqual("lake.red_tree.myomyo_responsibility", recruits.Single(n => n.recruitCharacter == CharacterActorType.Myomyo).requiredFlagKey);
            Assert.IsFalse(Graph.connections.Any(e => e.fromNodeId == "mark_truth" && e.toNodeId.StartsWith("recruit_")));
        }

        [Test]
        public void 마지막확인은_지정12명이며_후반공동대응을_완료하지않는다()
        {
            var c = (PartyCharactersOwnedCondition)Graph.nodes.OfType<BranchNode>().Single(n => n.id == "owned_twelve").condition;
            Assert.AreEqual(12, c.characters.Distinct().Count());
            CollectionAssert.DoesNotContain(c.characters, CharacterActorType.H09);
            CollectionAssert.DoesNotContain(c.characters, CharacterActorType.None);
            Assert.IsFalse(Graph.nodes.OfType<SetFlagNode>().Any(n => n.flagKey.Contains("response_work") || n.flagKey.Contains("ending") || n.flagKey.Contains("all_marks")));
        }

        [TestCase(8)] [TestCase(9)]
        public void 본편퀘스트_등록과_순차목표와_무보상(int number)
        {
            var q = AssetDatabase.LoadAssetAtPath<QuestSO>($"Assets/10.Datas/Quest/RedTree/quest_main_red_tree_s0{number}.asset");
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>("Assets/10.Datas/Quest/QuestDatabase.asset");
            Assert.AreEqual(1, db.QuestList.Count(x => x == q));
            Assert.IsTrue(q.isContentEnabled && q.autoComplete && q.reward.IsEmpty);
            Assert.IsTrue(q.objectives.All(o => o.requiredCount == 1 && !string.IsNullOrEmpty(o.markerLocationId)));
        }

        [Test]
        public void 대기목표는_기존이벤트로_끝내고_새퀘스트를_연다()
        {
            Assert.AreEqual("lake.red_tree.marks_investigation_started", Graph.nodes.OfType<NotifyQuestStoryEventNode>().Single(n => n.id == "handoff_notify").eventId);
            Assert.AreEqual("quest_main_red_tree_marks_next", Graph.nodes.OfType<CompleteQuestNode>().Single(n => n.id == "handoff_complete").questId);
            var q = AssetDatabase.LoadAssetAtPath<QuestSO>("Assets/10.Datas/Quest/RedTree/quest_main_red_tree_marks_next.asset");
            Assert.AreEqual("obj_rt_marks_pending", q.objectives[0].objectiveId);
        }
    }
}
