using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Quest;
using UPlayGround.Dialogue;
using UnityEngine;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>기록 원문·진실·비전투 영입·본편 등록의 저작 경계를 검사한다.</summary>
    public sealed class RedTreeR05ContentTests
    {
        private const string Root = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R05/";
        private FlowGraphSO Graph => AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Root + "FLOW_RedTreeRecordsPassage.asset");

        [Test]
        public void 기록과_대화의_참조및_분기가_유효하다()
        {
            var issues = new List<string>();
            Assert.IsTrue(Graph.Validate(issues), string.Join("\n", issues));
            foreach (var id in AssetDatabase.FindAssets("t:DialogueGraphSO", new[] { Root.TrimEnd('/') }))
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(AssetDatabase.GUIDToAssetPath(id));
                Assert.IsTrue(d.TryValidatePlayback(out var error), d.name + ": " + error);
            }
            Assert.IsTrue(Graph.nodes.OfType<PlayDialogueNode>().All(n => !n.continueWhenCancelled));
        }

        [Test]
        public void 세_원문은_서로다른_실제열람과_수령선후관계를_갖는다()
        {
            var keys = new HashSet<string>();
            foreach (string id in new[] { "warning", "offering", "inquiry" })
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_" + id + ".asset");
                var text = d.nodes.Single(n => n.nodeType == NodeType.Talk);
                Assert.AreEqual(0, text.typingSpeed, "문서 본문은 즉시 펼친다");
                var fact = text.eventActions.Single(a => a.GetType().Name == "SetFlagActionSO");
                Assert.IsTrue(keys.Add(new SerializedObject(fact).FindProperty("flagKey").stringValue));
                StringAssert.Contains("루시아", text.dialogueText);
                Assert.IsTrue(text.dialogueText.Contains("접수") || text.dialogueText.Contains("수령"));
            }
            Assert.AreEqual(3, keys.Count);
        }

        [Test]
        public void 진실과_관계와_동의는_분리되며_취소후_재접근할수있다()
        {
            Assert.AreEqual("lake.red_tree.truth.concealment", Graph.nodes.OfType<SetFlagNode>().Single(n => n.id == "mark_truth").flagKey);
            Assert.AreEqual("lake.red_tree.myoryeong_stance", Graph.nodes.OfType<SetFlagNode>().Single(n => n.id == "mark_stance").flagKey);
            Assert.IsTrue(Graph.connections.Any(e => e.fromNodeId == "entry_myoryeong" && e.toNodeId == "recruit_myoryeong"));
            Assert.IsTrue(Graph.connections.Any(e => e.fromNodeId == "entry_stance" && e.toNodeId == "require_stance"));
            var recruits = Graph.nodes.OfType<RecruitCharacterByConsentNode>().ToArray();
            Assert.AreEqual(6, recruits.Length, "다섯 사건 중 미선택 주인공만 두 분기");
            foreach (var n in recruits)
            {
                Assert.NotNull(n.dialogue);
                Assert.NotNull(n.alreadyOwnedDialogue);
                Assert.AreNotEqual(n.requiredFlagKey, n.completedFlagKey);
            }
        }

        [Test]
        public void 길열림과_실제구조는_대화가_대신하지않는다()
        {
            var flags = Graph.nodes.OfType<SetFlagNode>().Select(n => n.flagKey).ToArray();
            foreach (string fact in new[] { "records_shared", "passage_open", "yura_rescued", "other_rescued" })
                CollectionAssert.DoesNotContain(flags, "lake.red_tree." + fact);
            Assert.IsFalse(Graph.nodes.OfType<SpawnStoryActorNode>().Any());
            Assert.IsFalse(flags.Any(f => f.Contains("truth.expansion") || f.Contains("install_order")));
        }

        [TestCase(6)]
        [TestCase(7)]
        public void 본편퀘스트가_등록되고_무보상이며_모든목표에_행선지가있다(int number)
        {
            var q = AssetDatabase.LoadAssetAtPath<QuestSO>($"Assets/10.Datas/Quest/RedTree/quest_main_red_tree_s0{number}.asset");
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>("Assets/10.Datas/Quest/QuestDatabase.asset");
            CollectionAssert.Contains(db.QuestList, q);
            Assert.IsTrue(q.isContentEnabled && q.autoComplete && q.reward.IsEmpty);
            Assert.IsTrue(q.objectives.All(o => o.requiredCount == 1 && !string.IsNullOrEmpty(o.markerLocationId)));
        }

        [Test]
        public void 미선택주인공은_저장선택으로만_분기하고_적정의를_사용하지않는다()
        {
            var branch = Graph.nodes.OfType<BranchNode>().Single(n => n.id == "branch_other");
            Assert.AreEqual(CharacterActorType.Raon, ((StoryProtagonistCondition)branch.condition).character);
            Assert.IsTrue(Graph.connections.Any(e => e.fromNodeId == branch.id && e.fromPort == "True" && e.toNodeId == "recruit_arin"));
            foreach (string who in new[] { "Arin", "Raon", "Lian", "MyoRyeong", "Reine", "Yura" })
            {
                var actor = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(Root + "Actor_" + who + ".asset");
                Assert.IsTrue((actor.actorType & ActorType.NPC) != 0);
                Assert.IsTrue((actor.actorType & ActorType.Monster) == 0);
                Assert.AreEqual(CharacterActorType.None, actor.recruitableAs);
                Assert.IsFalse(actor.prefab.GetComponents<MonoBehaviour>().Any(c => c != null && c.GetType().Name == "MonsterActor"));
            }
        }

        [Test]
        public void 고정화자가_현재조작인물일때_데이터로_동일인물을_식별한다()
        {
            var table = AssetDatabase.LoadAssetAtPath<SpeakerActorBindingTableSO>("Assets/10.Datas/Dialogue/SpeakerActorBindingTable.asset");
            Assert.IsTrue(table.IsCharacterSpeaker("리안", CharacterActorType.Lian));
            Assert.IsTrue(table.IsCharacterSpeaker("아린", CharacterActorType.Arin));
            Assert.IsFalse(table.IsCharacterSpeaker("아린", CharacterActorType.Raon));
            Assert.IsFalse(table.IsCharacterSpeaker("없는 화자", CharacterActorType.None));
        }
    }
}
