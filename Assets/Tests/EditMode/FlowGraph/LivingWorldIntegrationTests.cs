using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.UI;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>공유 사건의 지역 제한과 두 맵의 직렬화 연결을 검사한다.</summary>
    public sealed class LivingWorldIntegrationTests
    {
        [Test]
        public void 개울_재개는_러너의_씬을_검사하고_기존_진행_키를_유지한다()
        {
            var graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(
                "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/RedTree/R04/FLOW_RedTreeVillageWaterReport.asset");
            var guard = graph.GetNode("livingworld_encounter_scene") as BranchNode;
            Assert.That(guard?.condition, Is.TypeOf<RunnerSceneCondition>());
            Assert.That(((RunnerSceneCondition)guard.condition).sceneName, Is.EqualTo("DroughtVillage"));
            Assert.That(graph.connections.Where(e => e.toNodeId == "encounter_gate")
                .All(e => e.fromNodeId == guard.id && e.fromPort == FlowPort.True), Is.True);
            Assert.That(graph.GetNode("spawn_ent"), Is.Not.Null);
            Assert.That(graph.GetNode("encounter_started"), Is.Not.Null);

            var scene = EditorSceneManager.NewPreviewScene();
            var host = new GameObject("지역 조건 검사");
            host.SetActive(false);
            SceneManager.MoveGameObjectToScene(host, scene);
            try
            {
                var context = new FlowContext(host.AddComponent<FlowGraphRunner>(), null);
                var condition = new RunnerSceneCondition { sceneName = "다른 씬" };
                Assert.That(condition.Evaluate(context), Is.False);
                condition.sceneName = scene.name;
                // PreviewScene 이름이 비어 있으면 지정되지 않은 지역도 거부해야 한다.
                Assert.That(condition.Evaluate(context), Is.EqualTo(!string.IsNullOrWhiteSpace(scene.name)));
                Assert.That(condition.Evaluate(null), Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase("LakeOfLifeScenario", 4)]
        [TestCase("DroughtVillage", 2)]
        public void 맵_등록과_지역_참조가_유효하고_러너와_주민을_중복하지_않는다(string mapId, int routines)
        {
            var db = AssetDatabase.LoadAssetAtPath<MapConfigDatabaseSO>("Assets/10.Datas/Map/MapConfigDatabase.asset");
            Assert.That(db.Entries.Count(e => e.mapId == mapId), Is.EqualTo(1));
            Assert.That(db.GetConfig(mapId), Is.Not.Null);
            Assert.That(db.GetRegionInfo(mapId).flowGraphs, Is.Empty);
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("/" + mapId + ".unity")), Is.True);
            var scene = EditorSceneManager.OpenPreviewScene("Assets/01.Scenes/Scenario/" + mapId + ".unity");
            try
            {
                var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                Assert.That(transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)), Is.Zero);
                var components = transforms.SelectMany(t => t.GetComponents<MonoBehaviour>()).Where(c => c != null).ToArray();
                Assert.That(components.Count(c => c.GetType().Name == "NpcRoutine"), Is.EqualTo(routines));
                Assert.That(components.Count(c => c.GetType().Name == "EnvironmentController"), Is.EqualTo(1));
                var runners = components.OfType<FlowGraphRunner>().ToArray();
                Assert.That(runners.All(r => r.enabled && r.Graph != null), Is.True);
                Assert.That(runners.GroupBy(r => r.Graph.ResolvedGraphId).All(g => g.Count() == 1), Is.True);
                foreach (var volume in components.OfType<FlowGraphTriggerVolume>())
                    Assert.That(new SerializedObject(volume).FindProperty("_runner").objectReferenceValue,
                        Is.EqualTo(volume.GetComponentInParent<FlowGraphRunner>(true)), volume.name);
                var ids = components.Where(c => c.GetType().Name == "SceneEntityId")
                    .Select(c => new SerializedObject(c).FindProperty("_guid").stringValue).ToArray();
                Assert.That(ids.All(id => !string.IsNullOrEmpty(id)), Is.True);
                Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
                var errors = new List<string>();
                foreach (var component in components)
                {
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var value = property.objectReferenceValue;
                        if (value == null && property.objectReferenceInstanceIDValue != 0)
                            errors.Add(component.name + "." + property.propertyPath + ": 누락");
                        var target = value as GameObject ?? (value as Component)?.gameObject;
                        if (target != null && target.scene.IsValid() && target.scene != scene)
                            errors.Add(component.name + "." + property.propertyPath + ": 다른 씬");
                    }
                }
                Assert.That(errors, Is.Empty, string.Join("\n", errors));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
