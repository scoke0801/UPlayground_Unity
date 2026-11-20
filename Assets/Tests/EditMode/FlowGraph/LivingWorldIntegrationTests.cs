using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.UI;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>공유 사건의 지역 제한과 두 맵의 직렬화 연결을 검사한다.</summary>
    public sealed class LivingWorldIntegrationTests
    {
        private const string ResidentScenePath = "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
        private const string VillageScenePath = "Assets/01.Scenes/Scenario/OpenWorld/Regions/DroughtVillageRegion.unity";

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

        [TestCase("LakeOfLifeScenario", 8)]
        [TestCase("DroughtVillage", 2)]
        public void 맵_등록과_지역_참조가_유효하고_러너와_주민을_중복하지_않는다(string mapId, int routines)
        {
            var db = AssetDatabase.LoadAssetAtPath<MapConfigDatabaseSO>("Assets/10.Datas/Map/MapConfigDatabase.asset");
            Assert.That(db.Entries.Count(e => e.mapId == mapId), Is.EqualTo(1));
            Assert.That(db.GetConfig(mapId), Is.Not.Null);
            var regionInfo = db.GetRegionInfo(mapId);
            Assert.That(regionInfo, Is.Not.Null);
            Assert.That(regionInfo.flowGraphs, Is.Empty);

            Assert.That(db.DefaultStartMapId, Is.EqualTo(Path.GetFileNameWithoutExtension(ResidentScenePath)));
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ResidentScenePath), Is.True);
            bool resident = mapId == "LakeOfLifeScenario";
            string scenePath = resident ? ResidentScenePath : VillageScenePath;
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                Assert.That(transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)), Is.Zero);
                var components = transforms.SelectMany(t => t.GetComponents<MonoBehaviour>()).Where(c => c != null).ToArray();
                Assert.That(components.Count(c => c.GetType().Name == "NpcRoutine"), Is.EqualTo(routines));
                Assert.That(components.Count(c => c.GetType().Name == "EnvironmentController"), Is.EqualTo(resident ? 1 : 0));
                if (resident)
                    AssertResidentRegionRegistration(components, mapId);
                else
                    AssertStreamedRegionRoot(components, scene, mapId);
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

        private static void AssertResidentRegionRegistration(MonoBehaviour[] components, string mapId)
        {
            var controller = components.Single(c => c.GetType().Name == "ContinuousWorldController");
            var data = new SerializedObject(controller);
            Assert.That(data.FindProperty("ResidentMapId").stringValue, Is.EqualTo(mapId));
            var regions = data.FindProperty("Regions");
            var village = Enumerable.Range(0, regions.arraySize)
                .Select(regions.GetArrayElementAtIndex)
                .Single(region => region.FindPropertyRelative("MapId").stringValue == "DroughtVillage");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings, Is.Not.Null);
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(VillageScenePath));
            Assert.That(entry, Is.Not.Null);
            Assert.That(village.FindPropertyRelative("SceneKey").stringValue, Is.EqualTo(entry.address));
        }

        private static void AssertStreamedRegionRoot(MonoBehaviour[] components, Scene scene, string mapId)
        {
            var content = components.Single(c => c.GetType().Name == "StreamedRegionContent");
            var data = new SerializedObject(content);
            Assert.That(data.FindProperty("MapId").stringValue, Is.EqualTo(mapId));
            var root = data.FindProperty("ContentRoot").objectReferenceValue as GameObject;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.scene, Is.EqualTo(scene));
        }
    }
}
