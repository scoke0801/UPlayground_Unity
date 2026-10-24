using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>지역 이름 누락 시 내부 식별자 노출과 비공개 지역의 목록 재등장을 막는다.</summary>
    public sealed class MapDisplayNameTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void 도착_전용_거점은_현재_지역과_다른_지역_지도에_표시된다(bool browsing)
        {
            var host = new GameObject("지도 거점 검사");
            host.SetActive(false);
            var map = host.AddComponent<UI_Scene_Map>();
            var icons = new GameObject("Icons", typeof(RectTransform));
            icons.transform.SetParent(host.transform);
            var region = ScriptableObject.CreateInstance<MapRegionInfoSO>();
            var config = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MinimapIconConfigSO>(
                "Assets/10.Datas/World/WorldCities/SaltwindHarbor/MinimapConfig.asset"));
            try
            {
                region.portals.Add(new MapRegionInfoSO.PortalEntry
                {
                    label = "도착 거점", activationId = "test.arrival", requiresActivation = true, startsActivated = true
                });
                // 발견 키 없는 항목은 공개 설정으로도 잠금을 우회할 수 없다.
                region.portals.Add(new MapRegionInfoSO.PortalEntry { label = "잘못된 거점", startsActivated = true });
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(UI_Scene_Map).GetField("_regionInfo", flags).SetValue(map, region);
                typeof(UI_Scene_Map).GetField("_config", flags).SetValue(map, config);
                typeof(UI_Scene_Map).GetField("_iconContainer", flags).SetValue(map, icons.transform);
                typeof(UI_Scene_Map).GetField("_currentSceneMapId", flags).SetValue(map, "Current");
                typeof(UI_Scene_Map).GetField("_viewRegionMapId", flags).SetValue(map, browsing ? "Other" : "Current");
                typeof(UI_Scene_Map).GetMethod("BuildBrowsePortals", flags).Invoke(map, null);
                Assert.That(icons.transform.childCount, Is.EqualTo(1));
                Assert.That(icons.transform.GetChild(0).name, Is.EqualTo("Marker_도착 거점"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(region);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [TestCase("생명의 호수", true, true)]
        [TestCase("숨은 동굴", false, false)]
        [TestCase("", true, false)]
        [TestCase("   ", true, false)]
        public void 표시_이름과_목록_공개는_별도로_판정한다(string name, bool visible, bool listed)
        {
            var database = ScriptableObject.CreateInstance<MapConfigDatabaseSO>();
            var region = ScriptableObject.CreateInstance<MapRegionInfoSO>();
            try
            {
                region.continentName = name;
                region.showContinentName = visible;
                var settings = new SerializedObject(database);
                var entries = settings.FindProperty("_entries");
                entries.arraySize = 1;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("mapId").stringValue = "InternalMapId";
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("regionInfo").objectReferenceValue = region;
                settings.ApplyModifiedPropertiesWithoutUndo();

                bool hasName = !string.IsNullOrWhiteSpace(name);
                if (!hasName)
                    LogAssert.Expect(LogType.Warning, "[MapConfigDatabase] MapID 'InternalMapId'의 지역 표시 이름이 없습니다.");
                Assert.That(database.GetDisplayName("InternalMapId"), Is.EqualTo(hasName ? name : string.Empty));
                Assert.That(database.IsRegionListed("InternalMapId"), Is.EqualTo(listed));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(database);
                UnityEngine.Object.DestroyImmediate(region);
            }
        }

        [TestCase("RemovedMap")]
        [TestCase(null)]
        public void 미등록_저장_맵도_내부_식별자를_표시하지_않는다(string mapId)
        {
            var database = ScriptableObject.CreateInstance<MapConfigDatabaseSO>();
            try
            {
                LogAssert.Expect(LogType.Warning, $"[MapConfigDatabase] MapID '{mapId}'의 지역 표시 이름이 없습니다.");
                Assert.That(database.GetDisplayName(mapId), Is.Empty);
                Assert.That(database.IsRegionListed(mapId), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(database); }
        }
    }
}
