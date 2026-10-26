using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI.Tests
{
    /// <summary>날씨 표시 우선순위와 시계 프리팹의 실제 아이콘 연결을 검사한다.</summary>
    public sealed class WorldClockWeatherTests
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/HUD/UI_HUD_WorldClock.prefab";

        [TestCase(0f, 0f, DayPeriod.Day, "ClearDay")]
        [TestCase(0f, 0f, DayPeriod.Night, "ClearNight")]
        [TestCase(0f, 0.5f, DayPeriod.Day, "Cloudy")]
        [TestCase(0f, 0.9f, DayPeriod.Night, "Cloudy")]
        [TestCase(0.1f, 0.9f, DayPeriod.Night, "Rain")]
        [TestCase(0.4f, 0.1f, DayPeriod.Day, "Rain")]
        [TestCase(0.09f, 0.49f, DayPeriod.Dawn, "ClearDay")]
        public void 실제_강수와_구름량_및_밤에_맞는_아이콘을_선택한다(
            float rain, float cloud, DayPeriod period, string expected)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var clock = prefab.GetComponent<UI_HUD_WorldClock>();
            var resolve = typeof(UI_HUD_WorldClock).GetMethod("ResolveWeatherIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            var sprite = (Sprite)resolve.Invoke(clock, new object[] { rain, cloud, period });
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.name, Is.EqualTo(expected));
        }

        [Test]
        public void 날씨_아이콘은_입력을_가로채지_않고_시계와_겹치지_않는다()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var clock = new SerializedObject(prefab.GetComponent<UI_HUD_WorldClock>());
            var icon = clock.FindProperty("_weatherIcon").objectReferenceValue as Image;
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.raycastTarget, Is.False);
            Assert.That(icon.preserveAspect, Is.True);
            var day = (RectTransform)prefab.transform.Find("Panel/DayText");
            float iconLeft = icon.rectTransform.anchoredPosition.x - icon.rectTransform.sizeDelta.x * 0.5f;
            Assert.That(day.offsetMax.x, Is.LessThan(iconLeft));
        }
    }
}
