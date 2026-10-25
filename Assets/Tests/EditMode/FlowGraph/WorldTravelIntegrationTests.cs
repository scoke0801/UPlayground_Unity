using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>현장 출구와 지도 도착 거점의 지역 공개·발견·좌표 계약을 검증한다.</summary>
    public sealed class WorldTravelIntegrationTests
    {
        private const string DatabasePath = "Assets/10.Datas/Map/MapConfigDatabase.asset";
        private const string LakeScenePath = "Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity";
        private const string LakeCommonEndingFlag = "lake.red_tree.return.common_ending";

        [Test]
        public void 지도_도착_대상은_표시_이름과_지역_목록을_갖는다()
        {
            var database = AssetDatabase.LoadAssetAtPath<MapConfigDatabaseSO>(DatabasePath);
            Assert.That(database, Is.Not.Null);
            var errors = new List<string>();
            int count = 0;
            foreach (var entry in database.Entries)
            {
                if (entry.regionInfo == null) continue;
                foreach (var portal in entry.regionInfo.portals)
                {
                    string target = string.IsNullOrEmpty(portal.targetSceneName) ? entry.mapId : portal.targetSceneName;
                    CheckListedTarget(database, target, entry.mapId + "/" + portal.label, errors);
                    // 목적지 이름이 있어도 아이콘을 가진 지역이 숨겨져 있으면 선택할 수 없다.
                    CheckListedTarget(database, entry.mapId, "지도 항목 소유 지역", errors);
                    count++;
                }
            }
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void 등록된_플레이_씬의_포털_대상은_지역_목록에서_선택할_수_있다()
        {
            var database = AssetDatabase.LoadAssetAtPath<MapConfigDatabaseSO>(DatabasePath);
            var maps = new HashSet<string>(database.Entries.Select(e => e.mapId));
            var errors = new List<string>();
            int count = 0;
            foreach (var buildScene in EditorBuildSettings.scenes.Where(s => s.enabled && maps.Contains(Path.GetFileNameWithoutExtension(s.path))))
            {
                var scene = EditorSceneManager.OpenPreviewScene(buildScene.path);
                try
                {
                    foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
                    {
                        if (component == null || component.GetType().Name != "PortalActor") continue;
                        var settings = new SerializedObject(component);
                        // 같은 씬 안에서 이동하는 포털의 미사용 목적지 필드는 지역 연결이 아니다.
                        if (settings.FindProperty("_portalType").enumValueIndex != 0) continue;
                        string target = settings.FindProperty("_targetSceneName").stringValue;
                        if (string.IsNullOrWhiteSpace(target)) continue;
                        CheckListedTarget(database, target, buildScene.path + "/" + component.name, errors);
                        count++;
                    }
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            Assert.That(count, Is.GreaterThan(0));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [TestCase("StonegateCitadel", "worldcities.StonegateCitadel.plaza")]
        [TestCase("SaltwindHarbor", "worldcities.SaltwindHarbor.market")]
        public void 도시의_모든_도착이_같은_지도_거점을_해금한다(string mapId, string discoveryId)
        {
            var database = AssetDatabase.LoadAssetAtPath<MapConfigDatabaseSO>(DatabasePath);
            var region = database.GetRegionInfo(mapId);
            Assert.That(region.portals.Count, Is.EqualTo(1));
            var portal = region.portals[0];
            Assert.That(portal.targetSceneName, Is.EqualTo(mapId));
            Assert.That(portal.activationId, Is.EqualTo(discoveryId));
            Assert.That(portal.requiresActivation, Is.True);
            Assert.That(portal.startsActivated, Is.False);
            Assert.That(portal.label, Is.Not.Empty);

            var scene = EditorSceneManager.OpenPreviewScene("Assets/01.Scenes/Scenario/" + mapId + ".unity");
            try
            {
                var arrivals = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(c => c != null && c.GetType().Name == "SceneArrivalPoint").ToArray();
                Assert.That(arrivals.Length, Is.EqualTo(3));
                var ids = new List<string>();
                foreach (var arrival in arrivals)
                {
                    var settings = new SerializedObject(arrival);
                    string id = settings.FindProperty("_id").stringValue;
                    ids.Add(id);
                    Assert.That(settings.FindProperty("_activationId").stringValue, Is.EqualTo(discoveryId), id);
                    if (id == portal.arrivalId)
                        Assert.That(Vector3.Distance(arrival.transform.position, portal.worldPosition), Is.LessThan(.001f));
                }
                Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
                Assert.That(ids, Does.Contain(portal.arrivalId));
                Assert.That(ids, Does.Contain("worldcities." + mapId + ".from.LakeOfLifeScenario"));
                string otherCity = mapId == "StonegateCitadel" ? "SaltwindHarbor" : "StonegateCitadel";
                Assert.That(ids, Does.Contain("worldcities." + mapId + ".from." + otherCity));
                var config = database.GetConfig(mapId);
                Assert.That(config.backgroundSprite, Is.Not.Null);
                Vector2 pixel = config.WorldToMapImagePos(portal.worldPosition, new Vector2(2048, 2048));
                Assert.That(Mathf.Abs(pixel.x), Is.LessThanOrEqualTo(1024));
                Assert.That(Mathf.Abs(pixel.y), Is.LessThanOrEqualTo(1024));
                Assert.That(Vector3.Distance(config.MapImagePosToWorld(pixel, new Vector2(2048, 2048), portal.worldPosition.y), portal.worldPosition), Is.LessThan(.001f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase("StonegateCitadel")]
        [TestCase("SaltwindHarbor")]
        public void 호수의_도시_출구는_본편_결말_뒤에_열린다(string cityMapId)
        {
            var scene = EditorSceneManager.OpenPreviewScene(LakeScenePath);
            try
            {
                var exits = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(c => c != null && c.GetType().Name == "PortalActor")
                    .Select(c => new SerializedObject(c))
                    .Where(s => s.FindProperty("_targetSceneName").stringValue == cityMapId)
                    .ToArray();
                Assert.That(exits.Length, Is.EqualTo(1));

                var exit = exits[0];
                Assert.That(exit.FindProperty("_requiredFlagKey").stringValue, Is.EqualTo(LakeCommonEndingFlag));

                // 결말 전에는 길 표식 자체를 숨겨, 갈 수 없는 길을 안내하지 않는다.
                var visuals = exit.FindProperty("_activeVisuals");
                Assert.That(visuals.arraySize, Is.GreaterThan(0));
                for (int i = 0; i < visuals.arraySize; i++)
                    Assert.That(visuals.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void CheckListedTarget(MapConfigDatabaseSO database, string mapId, string source, List<string> errors)
        {
            var region = database.GetRegionInfo(mapId);
            if (region == null || string.IsNullOrWhiteSpace(region.continentName) || !database.IsRegionListed(mapId))
                errors.Add(source + " -> " + mapId + ": 표시 이름 또는 지역 목록 공개 누락");
            if (database.Entries.All(e => e.mapId != mapId || e.config == null))
                errors.Add(source + " -> " + mapId + ": 지도 설정 누락");
        }
    }
}
