using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Tests
{
    /// <summary>전체 월드 지도가 원본 지역 좌표로 되돌아가지 않는지 검증한다.</summary>
    public sealed class WorldMinimapTests
    {
        [Test]
        public void 지역_이동_목록은_월드_지도에서_숨기고_개별_지도에서_복원한다()
        {
            var root = new GameObject("지역 목록 검증");
            var panel = new GameObject("지역 이동 패널");
            panel.transform.SetParent(root.transform);
            try
            {
                var map = root.AddComponent<UI_Scene_Map>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(UI_Scene_Map);
                type.GetField("_regionSelectorPanel", flags).SetValue(map, panel);
                type.GetField("_usesWorldCoordinates", flags).SetValue(map, true);
                type.GetMethod("PopulateRegionList", flags).Invoke(map, null);
                Assert.That(panel.activeSelf, Is.False);

                type.GetField("_usesWorldCoordinates", flags).SetValue(map, false);
                type.GetMethod("PopulateRegionList", flags).Invoke(map, null);
                Assert.That(panel.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 전체_지도는_포함_지역에서도_월드_좌표와_클릭_위치를_유지한다()
        {
            var root = new GameObject("전체 지도 검증");
            try
            {
                var map = root.AddComponent<UI_Scene_Map>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(UI_Scene_Map);
                type.GetField("_usesWorldCoordinates", flags).SetValue(map, true);
                type.GetField("_currentSceneMapId", flags).SetValue(map, "LakeOfLifeScenario");
                type.GetField("_viewRegionMapId", flags).SetValue(map, "SaltwindHarbor");
                Assert.That(type.GetProperty("IsBrowsing", flags).GetValue(map), Is.False);
                var position = new Vector3(1920, 0, -400);
                Assert.That(type.GetMethod("ToMapPosition", flags).Invoke(map, new object[] { position }),
                    Is.EqualTo(position));

                var config = AssetDatabase.LoadAssetAtPath<MinimapIconConfigSO>(
                    "Assets/10.Datas/World/LakeVillage/MinimapConfig.asset");
                type.GetField("_config", flags).SetValue(map, config);
                type.GetField("_mapDisplaySize", flags).SetValue(map, new Vector2(1200, 1400));
                type.GetField("_currentZoom", flags).SetValue(map, 2f);
                var actual = (Vector3)type.GetMethod("MapLocalPosToWorld", flags)
                    .Invoke(map, new object[] { new Vector2(720, -1100) });
                Assert.That(Vector3.Distance(actual, position), Is.LessThan(0.001f));

                type.GetField("_usesWorldCoordinates", flags).SetValue(map, false);
                Assert.That(type.GetProperty("IsBrowsing", flags).GetValue(map), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 월드_지도는_이동된_지역의_좌표를_그대로_사용한다()
        {
            var root = new GameObject("월드 미니맵 검증");
            try
            {
                var hud = root.AddComponent<UI_HUD_Minimap>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(UI_HUD_Minimap).GetField("_usesWorldCoordinates", flags).SetValue(hud, true);
                typeof(UI_HUD_Minimap).GetField("_mapId", flags).SetValue(hud, "SaltwindHarbor");
                var position = new Vector3(1920, 63, -400);
                var actual = (Vector3)typeof(UI_HUD_Minimap).GetMethod("ToMapPosition", flags)
                    .Invoke(hud, new object[] { position });
                Assert.That(actual, Is.EqualTo(position));

                var config = AssetDatabase.LoadAssetAtPath<MinimapIconConfigSO>(
                    "Assets/10.Datas/World/LakeVillage/MinimapConfig.asset");
                Assert.That(config, Is.Not.Null);
                Assert.That(config.backgroundSprite, Is.Not.Null);
                Assert.That(config.captureCenter, Is.EqualTo(new Vector2(1200, 700)));
                Assert.That(config.captureWorldSizeXY, Is.EqualTo(new Vector2(2400, 2800)));
                Assert.That(config.WorldToMapImagePos(actual, new Vector2(1200, 1400)),
                    Is.EqualTo(new Vector2(360, -550)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
