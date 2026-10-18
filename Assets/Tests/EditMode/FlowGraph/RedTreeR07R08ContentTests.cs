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
    /// <summary>공동 대응과 최종전의 본편 연결·보상·사실 소유 경계를 검사한다.</summary>
    public sealed class RedTreeR07R08ContentTests
    {
        private const string Root = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R07R08/";
        private FlowGraphSO Graph => AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Root + "FLOW_RedTreeResponseFinale.asset");

        [Test]
        public void 모든대화와_그래프는_유효하고_취소는_완료가아니다()
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
        public void 대피_배급_중단_전투승리는_대화가_생산하지않는다()
        {
            var keys = Graph.nodes.OfType<SetFlagNode>().Select(n => n.flagKey).ToArray();
            foreach (string key in new[] { "response.front_clear", "response.mia_arrived", "response.care_supplied", "response.remaining_supplied", "response.affected_supplied", "marker.west_relay.disconnected", "marker.south_channel.disconnected", "marker.shrine_link.disconnected", "marker.trial_creek.disconnected", "lucia.victory", "absorption.stopped" })
                CollectionAssert.DoesNotContain(keys, "lake.red_tree." + key);
            Assert.IsFalse(keys.Any(k => k.Contains("ending") || k.Contains("lucia.dead") || k.Contains("return_work_started")));
        }

        [Test]
        public void 진입은_정해진12명과_준비를_함께요구한다()
        {
            var c = (PartyCharactersOwnedCondition)Graph.nodes.OfType<BranchNode>().Single(n => n.id == "owned_twelve").condition;
            Assert.AreEqual(12, c.characters.Distinct().Count());
            CollectionAssert.DoesNotContain(c.characters, CharacterActorType.None);
            foreach (string fact in new[] { "r06_complete", "truth.expansion", "response.evacuation_complete", "response.distribution_complete", "response.access_complete", "response_work_started" })
                Assert.IsTrue(Graph.nodes.OfType<CheckFlagNode>().Any(n => n.flagKey == "lake.red_tree." + fact));
        }

        [TestCase("s10")] [TestCase("s11")] [TestCase("s12")] [TestCase("return_next")]
        public void 본편목표는_한번등록하고_보상을_중복지급하지않는다(string suffix)
        {
            var q = AssetDatabase.LoadAssetAtPath<QuestSO>("Assets/10.Datas/Quest/RedTree/quest_main_red_tree_" + suffix + ".asset");
            var db = AssetDatabase.LoadAssetAtPath<QuestDatabase>("Assets/10.Datas/Quest/QuestDatabase.asset");
            Assert.AreEqual(1, db.QuestList.Count(x => x == q));
            Assert.IsTrue(q.isContentEnabled && q.autoComplete && q.reward.IsEmpty);
            Assert.IsTrue(q.objectives.All(o => o.requiredCount == 1 && !string.IsNullOrEmpty(o.markerLocationId)));
            Assert.IsFalse(q.objectives.Any(o => o.targetStringId.EndsWith("trial_creek.disconnected")));
        }

        [Test]
        public void 최초주인공과_반대편주인공의_화자를_구별한다()
        {
            foreach (string file in new[] { "DLG_lucia.asset", "DLG_observe.asset" })
            {
                var d = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + file);
                Assert.IsTrue(d.nodes.Any(n => n.speakerId == "Protagonist"));
                Assert.IsFalse(d.nodes.Any(n => n.speakerId == "Player"));
            }
            Assert.AreEqual("아린", AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_outer.asset").nodes[0].speakerId);
            Assert.AreEqual("라온", AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Root + "DLG_outer_raon.asset").nodes[0].speakerId);
        }

        [Test]
        public void 공격후보와_실행트리거를_함께_보존한다()
        {
            var set = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.AbilitySetSO>(Root + "AbilitySet_Lucia.asset");
            var abilities = set.EnumerateAll().ToArray();
            foreach (string category in new[] { "Basic", "Heavy" })
                Assert.IsTrue(abilities.Any(a => a.triggers.Any(t => t.triggerTag.TagName == "Trigger.Monster.Attack." + category)), category);
            Assert.IsTrue(abilities.Any(a => a.abilityId == "Boss.Lucia.Hand"));
            Assert.IsTrue(abilities.Any(a => a.abilityId == "Boss.Lucia.Stomp"));
        }

        [Test]
        public void 작업동료를_직접조작하면_현재몸을_화자로쓴다()
        {
            var bindings = AssetDatabase.LoadAssetAtPath<SpeakerActorBindingTableSO>("Assets/10.Datas/Dialogue/SpeakerActorBindingTable.asset");
            Assert.IsTrue(bindings.IsCharacterSpeaker("화린", CharacterActorType.Hwarin));
            Assert.IsTrue(bindings.IsCharacterSpeaker("릴리", CharacterActorType.Lili));
            Assert.IsTrue(bindings.IsCharacterSpeaker("설아", CharacterActorType.SeolA));
        }

        [Test]
        public void 기존대기목표의_ID와_인계이벤트를_보존한다()
        {
            Assert.AreEqual("lake.red_tree.response_work_started", ((NotifyQuestStoryEventNode)Graph.GetNode("handoff_notify")).eventId);
            Assert.AreEqual("quest_main_red_tree_response_next", ((CompleteQuestNode)Graph.GetNode("handoff_complete")).questId);
        }
    }
}
