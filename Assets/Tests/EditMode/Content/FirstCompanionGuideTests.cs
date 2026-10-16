using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Save;
using UPlayGround.Data.UI;

namespace UPlayGround.Content.Tests
{
    /// <summary>첫 안내의 우선순위·저장 복원과 중복 방지를 검사한다.</summary>
    public sealed class FirstCompanionGuideTests
    {
        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void 저장된_대기_안내는_미노출일_때만_복원한다(bool shown, int expectedPending)
        {
            Type type = Type.GetType("UPlayGround.Manager.GameGuideManager, Assembly-CSharp", true);
            var owner = new GameObject("GuidePersistenceTest");
            owner.SetActive(false);
            try
            {
                var manager = owner.AddComponent(type);
                var config = AssetDatabase.LoadAssetAtPath<FirstTimeGuideConfigSO>(
                    "Assets/10.Datas/Guide/FirstTimeGuideConfig.asset");
                type.GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, config);
                var input = new GameSaveData
                {
                    firstTimeGuide = new FirstTimeGuideSaveData
                    {
                        pendingGuideIds = new List<string> { "Guide.Companion", "Guide.Companion" },
                        shownGuideIds = shown ? new List<string> { "Guide.Companion" } : new List<string>()
                    }
                };
                type.GetMethod("ImportSaveData").Invoke(manager, new object[] { input });
                var output = new GameSaveData();
                type.GetMethod("ExportSaveData").Invoke(manager, new object[] { output });
                Assert.That(output.firstTimeGuide.pendingGuideIds.Count, Is.EqualTo(expectedPending));
                Assert.That(output.firstTimeGuide.shownGuideIds.Contains("Guide.Companion"), Is.EqualTo(shown));
                foreach (var entry in config.Entries)
                    if (entry.GuideId == "Guide.Companion")
                        Assert.That(entry.TravelDistanceBeforeShow, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 동료_안내는_먼저_쌓인_장비_안내보다_앞서고_로드해도_순서를_유지한다(bool restore)
        {
            Type type = Type.GetType("UPlayGround.Manager.GameGuideManager, Assembly-CSharp", true);
            var owner = new GameObject("GuidePriorityTest");
            owner.SetActive(false);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var manager = owner.AddComponent(type);
                var config = AssetDatabase.LoadAssetAtPath<FirstTimeGuideConfigSO>(
                    "Assets/10.Datas/Guide/FirstTimeGuideConfig.asset");
                type.GetField("_config", flags).SetValue(manager, config);
                var eventMethod = type.GetMethod("OnEvent");
                eventMethod.Invoke(manager, new object[] { UPlayGround.Data.Event.GameMilestoneEvent.EquipmentAcquired });
                eventMethod.Invoke(manager, new object[] { UPlayGround.Data.Event.GameMilestoneEvent.CombatStarted });
                type.GetField("_hasGuideTravelOrigin", flags).SetValue(manager, true);
                type.GetField("_presentationIdleSeconds", flags).SetValue(manager, 10f);
                eventMethod.Invoke(manager, new object[] { UPlayGround.Data.Event.GameMilestoneEvent.CharacterUnlocked });
                Assert.IsFalse((bool)type.GetField("_hasGuideTravelOrigin", flags).GetValue(manager));
                Assert.That(type.GetField("_presentationIdleSeconds", flags).GetValue(manager), Is.EqualTo(0f));
                if (restore)
                {
                    var legacyOrder = new GameSaveData
                    {
                        firstTimeGuide = new FirstTimeGuideSaveData
                        {
                            pendingGuideIds = new List<string> { "Guide.Equipment", "Guide.Companion", "Guide.Combat", "Guide.Equipment" }
                        }
                    };
                    type.GetMethod("ImportSaveData").Invoke(manager, new object[] { legacyOrder });
                }
                var output = new GameSaveData();
                type.GetMethod("ExportSaveData").Invoke(manager, new object[] { output });
                CollectionAssert.AreEqual(new[] { "Guide.Companion", "Guide.Equipment", "Guide.Combat" }, output.firstTimeGuide.pendingGuideIds,
                    "동일 우선순위는 기존 발생 순서를 유지한다.");
                Assert.IsEmpty(output.firstTimeGuide.shownGuideIds, "우선순위 조정으로 미노출 안내를 소진하지 않는다.");
                config.TryGet(UPlayGround.Data.Event.GameMilestoneEvent.CharacterUnlocked, out var companion);
                config.TryGet(UPlayGround.Data.Event.GameMilestoneEvent.EquipmentAcquired, out var equipment);
                Assert.That(companion.Priority, Is.GreaterThan(equipment.Priority));
                Assert.IsTrue((bool)type.GetMethod("CanShowAfterTravel", flags).Invoke(manager, new object[] { companion }));
                Assert.That(equipment.TravelDistanceBeforeShow, Is.Zero, "장비 안내에는 이동 조건을 추가하지 않는다.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
