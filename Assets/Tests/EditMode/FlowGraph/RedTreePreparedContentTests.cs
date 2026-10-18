using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Story;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>준비된 도입 데이터가 행동·정상 동의 없이 완료되거나 미구현 후속 퀘스트를 열지 않는지 검사한다.</summary>
    public sealed class RedTreePreparedContentTests
    {
        private const string Root = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/";

        [TestCase("FLOW_RedTreeIntroduction")]
        [TestCase("FLOW_RedTreeHwarinRescue")]
        public void PreparedGraphs_LoadWithValidManagedReferencesAndPorts(string name)
        {
            FlowGraphSO graph = LoadGraph(name);
            var errors = new List<string>();
            Assert.That(graph.Validate(errors), Is.True, string.Join("\n", errors));
            Assert.That(graph.nodes.OfType<EntryNode>().Any(node => node.repeatPolicy == FlowRepeatPolicy.OncePerSave), Is.False);
            foreach (PlayDialogueNode node in graph.nodes.OfType<PlayDialogueNode>())
            {
                Assert.That(node.dialogue, Is.Not.Null, node.id);
                Assert.That(node.continueWhenCancelled, Is.False, node.id);
            }
        }

        [Test]
        public void RefugeeSafety_IsNotProducedByCombatOrDialogue()
        {
            FlowGraphSO graph = LoadGraph("FLOW_RedTreeHwarinRescue");
            Assert.That(graph.nodes.OfType<SetFlagNode>().Any(node => node.flagKey == "lake.red_tree.refugees_safe"), Is.False);
            var definition = AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(Root + "RecruitmentEncounter_RedTreeHwarin.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.RecruitmentRequiredFlagKey, Is.EqualTo("lake.red_tree.refugees_safe"));
            Assert.That(CanReach(graph, "wait_combat", "commit", null), Is.True);
            Assert.That(CanReach(graph, "wait_combat", "commit", "require_refugees_safe"), Is.False);
            Assert.That(CanReach(graph, "resume", "commit", "consent"), Is.False);
            var consent = graph.nodes.OfType<PlayDialogueRequiredNode>().Single();
            Assert.That(consent.alreadyOwnedDialogue, Is.Not.Null);
            Assert.That(consent.alreadyOwnedDialogue, Is.Not.SameAs(consent.dialogue));
            var retry = graph.nodes.OfType<WaitPlayerInVolumeNode>().Single(node => node.id == "consent_retry_arrival");
            Assert.That(retry.requireExitBeforeArrival, Is.True);
            Assert.That(retry.volumeId, Is.EqualTo("lake.red_tree.hwarin_entry"));
            Assert.That(graph.connections.Any(edge => edge.fromNodeId == consent.id
                && edge.fromPort == "Rejected" && edge.toNodeId == retry.id), Is.True);
            Assert.That(CanReach(graph, retry.id, "commit", "consent"), Is.False);
            Assert.That(graph.nodes.OfType<ResumeRecruitmentEncounterNode>().Single(node => node.id == "resume_saved").resumeExistingOnly, Is.True);
        }

        [Test]
        public void Hospitality_DialogueDoesNotInventActionFacts()
        {
            FlowGraphSO graph = LoadGraph("FLOW_RedTreeIntroduction");
            var actions = new HashSet<string>
            {
                "lake.red_tree.delivery", "lake.red_tree.meal_served", "lake.red_tree.care_delivered",
                "lake.red_tree.care_space_ready", "lake.red_tree.shelter_assigned",
            };
            Assert.That(graph.nodes.OfType<SetFlagNode>().Any(node => actions.Contains(node.flagKey)), Is.False);
            Assert.That(CanReach(graph, "Care", "Care_complete", null), Is.True);
            Assert.That(CanReach(graph, "Handoff", "Handoff_complete", null), Is.True);
            Assert.That(CanReach(graph, "Care", "Care_complete", "Care_requires_care_space_ready"), Is.False);
            Assert.That(CanReach(graph, "Care", "Care_complete", "Care_requires_care_delivered"), Is.False);
            Assert.That(CanReach(graph, "Handoff", "Handoff_complete", "Handoff_requires_hwarin_consent"), Is.False);
        }

        [Test]
        public void QuestBoundary_DoesNotGrantDuplicateRewardsOrOpenUnimplementedContent()
        {
            for (int index = 0; index < 3; index++)
            {
                QuestSO quest = AssetDatabase.LoadAssetAtPath<QuestSO>(
                    $"Assets/10.Datas/Quest/RedTree/quest_main_red_tree_s0{index}.asset");
                Assert.That(quest, Is.Not.Null);
                Assert.That(quest.reward.IsEmpty, Is.True);
                Assert.That(quest.autoAcceptOnNewGame, Is.False);
                Assert.That(quest.objectives.All(objective => objective.requiredCount == 1), Is.True);
                if (index == 2)
                    Assert.That(quest.autoAcceptNextQuestIds, Is.Empty);
            }
        }

        private static FlowGraphSO LoadGraph(string name)
        {
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(Root + name + ".asset");
            Assert.That(graph, Is.Not.Null, name);
            return graph;
        }

        private static bool CanReach(FlowGraphSO graph, string start, string target, string blocked)
        {
            var pending = new Stack<string>();
            var visited = new HashSet<string>();
            pending.Push(start);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                if (current == blocked || !visited.Add(current))
                    continue;
                if (current == target)
                    return true;
                foreach (FlowConnection edge in graph.connections)
                {
                    if (edge.fromNodeId == current)
                        pending.Push(edge.toNodeId);
                }
            }
            return false;
        }
    }
}
