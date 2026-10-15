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
    /// <summary>이동을 기다리던 동료 안내의 저장 복원과 중복 방지를 검사한다.</summary>
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
                        Assert.That(entry.TravelDistanceBeforeShow, Is.InRange(5f, 10f));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
