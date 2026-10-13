#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>씬 없이 렌치 Animator만으로 누락 동작 경고의 원인을 대조한다.</summary>
    public sealed class WrenchControllerIsolationTests
    {
        [UnityTest]
        public IEnumerator ControllerWarnings_AreIsolatedFromSceneScripts()
        {
            var lines = new List<string>();
            string phase = "control";
            int sourceWarnings = 0;
            int controlWarnings = 0;
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (!message.Contains("referenced script (Unknown)")) return;
                lines.Add(phase + ": " + message);
                if (phase == "source") sourceWarnings++;
                else controlWarnings++;
            };
            Application.logMessageReceived += capture;
            try
            {
                var control = new GameObject("ControllerlessControl");
                control.AddComponent<Animator>();
                yield return null;
                Object.Destroy(control);
                yield return null;
                phase = "source";
                var probe = new GameObject("ExternalWrenchOnly");
                var animator = probe.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/ExternalAssets/Character/Suiha/prefab/Gimmicks/Modules/Wrench/FX_Wrench.controller");
                yield return null;
                Object.Destroy(probe);
                yield return null;
                phase = "project";
                var repaired = new GameObject("ProjectWrenchOnly");
                repaired.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/10.Datas/Actor/ScenarioResidents/FX_Wrench_Game.controller");
                yield return null;
                Object.Destroy(repaired);
                yield return null;
                lines.Add("sourceWarnings=" + sourceWarnings + "; controlWarnings=" + controlWarnings);
                Assert.That(controlWarnings, Is.Zero);
                Assert.That(sourceWarnings, Is.EqualTo(4));
            }
            finally
            {
                Application.logMessageReceived -= capture;
                Directory.CreateDirectory("output/ScenarioGameplay/P03/HerbRoute");
                File.WriteAllLines("output/ScenarioGameplay/P03/HerbRoute/WrenchIsolation.txt", lines);
            }
        }
    }
}
#endif
