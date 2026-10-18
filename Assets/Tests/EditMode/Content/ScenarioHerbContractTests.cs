using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Content.Tests
{
    /// <summary>시나리오 약초 연결이 원본 식별자와 조사·납품 순서를 보존하는지 검사한다.</summary>
    public sealed class ScenarioHerbContractTests
    {
        private const string Folder = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/";

        [Test]
        public void HerbFlow_PreservesOriginalNodesAndOptionalRecovery()
        {
            var original = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/Flow/FLOW_LakeSideQuestRoute.asset"));
            var scenario = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(Folder + "FLOW_ScenarioHerbRoute.asset"));
            Assert.That(scenario.FindProperty("graphId").stringValue, Is.EqualTo(original.FindProperty("graphId").stringValue));
            var nodes = scenario.FindProperty("nodes");
            // P06 저작기가 원본 조사 노드와 선행 조사·로드 복구를 합친 상태를 보존한다.
            Assert.That(nodes.arraySize, Is.EqualTo(35));
            var ids = new List<string>();
            for (int i = 0; i < nodes.arraySize; i++) ids.Add(nodes.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);
            var originalNodes = original.FindProperty("nodes");
            Assert.That(originalNodes.arraySize, Is.EqualTo(30));
            var expectedIds = new List<string>();
            for (int i = 0; i < originalNodes.arraySize; i++)
                expectedIds.Add(originalNodes.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);
            expectedIds.AddRange(new[] { "optional_unexamined_herb_bed", "optional_unexamined_lantern_1",
                "optional_unexamined_lantern_2", "optional_unexamined_lantern_3", "optional_loaded" });
            Assert.That(ids, Is.EquivalentTo(expectedIds));
            Assert.That(nodes.GetArrayElementAtIndex(2).FindPropertyRelative("continueWhenCancelled").boolValue, Is.False);
            var recovery = nodes.GetArrayElementAtIndex(ids.IndexOf("optional_loaded"));
            Assert.That(recovery.FindPropertyRelative("cancelRunningContexts").boolValue, Is.True);
            var guard = nodes.GetArrayElementAtIndex(ids.IndexOf("optional_unexamined_herb_bed"));
            Assert.That(guard.FindPropertyRelative("flagKey").stringValue, Is.EqualTo("lake.side.herb_bed_examined"));
            Assert.That(guard.FindPropertyRelative("expectedValue").boolValue, Is.False);
        }

        [Test]
        public void ScenarioDialogue_RequiresExaminationBeforeTurnIn()
        {
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(Folder + "DLG_Npc_Hazel_Scenario.asset");
            Assert.That(graph.TryValidatePlayback(out string error), Is.True, error);
            var accepted = graph.nodes.Single(n => n.nodeId == "hazel_cond_accepted");
            Assert.That(accepted.trueNextNodeId, Is.EqualTo("hazel_cond_examined"));
            var examined = graph.nodes.Single(n => n.nodeId == "hazel_cond_examined");
            Assert.That(examined.trueNextNodeId, Is.EqualTo("hazel_cond_has_herb"));
            Assert.That(new SerializedObject(examined.condition).FindProperty("flagKey").stringValue, Is.EqualTo("lake.side.herb_bed_examined"));
            var original = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>("Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Npc_Hazel.asset");
            Assert.That(original.nodes.Single(n => n.nodeId == "hazel_cond_accepted").trueNextNodeId, Is.EqualTo("hazel_cond_has_herb"));
            Assert.That(graph.nodes.Select(n => n.nodeId), Is.SupersetOf(original.nodes.Select(n => n.nodeId)));
        }

        [Test]
        public void ScenarioNpc_PreservesIdentityAndUsesLocalDialogue()
        {
            var original = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/Actor/Npc/NPC_Hazel.asset"));
            var scenario = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(Folder + "NPC_Hazel_Scenario.asset"));
            Assert.That(scenario.FindProperty("questMarkerLocationId").stringValue, Is.EqualTo(original.FindProperty("questMarkerLocationId").stringValue));
            Assert.That(AssetDatabase.GetAssetPath(scenario.FindProperty("dialogueGraph").objectReferenceValue), Is.EqualTo(Folder + "DLG_Npc_Hazel_Scenario.asset"));
            var region = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(Folder + "RegionInfo.asset"));
            Assert.That(region.FindProperty("flowGraphs").arraySize, Is.Zero, "조사 볼륨의 씬 러너와 중복 적용하지 않는다.");
        }
    }
}
