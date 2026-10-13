using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Content.Tests
{
    /// <summary>인프라 씬의 지역·카메라 계약과 빌드에서 소실되던 전략 참조를 검사한다.</summary>
    public sealed class StartupContractTests
    {
        [TestCase("Boot", false, false)]
        [TestCase("Title", false, false)]
        [TestCase("Loading", false, false)]
        [TestCase("GamePlay", true, true)]
        [TestCase("Test", true, true)]
        public void SceneRoles_RequireOnlyTheirRuntimeServices(string sceneType, bool region, bool camera)
        {
            Type contextType = FindType("UPlayGround.SceneContext");
            var owner = new GameObject("StartupContract");
            owner.SetActive(false);
            try
            {
                Component context = owner.AddComponent(contextType);
                contextType.GetField("SceneType").SetValue(context, sceneType);
                contextType.GetField("MapID").SetValue(context, "MissingRegion");
                Assert.That(contextType.GetProperty("HasRegionFlow", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(context), Is.EqualTo(region));
                Type adapterType = FindType("UPlayGround.Manager.UPlayGroundCameraRuntimeAdapter");
                object adapter = Activator.CreateInstance(adapterType);
                Assert.That(adapterType.GetMethod("RequiresPlayerCamera").Invoke(adapter, new object[] { sceneType }),
                    Is.EqualTo(camera));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [TestCase("Bow")]
        [TestCase("DoubleAxe")]
        [TestCase("DualBlade")]
        [TestCase("GreatSword")]
        [TestCase("SwordShield")]
        public void CombatStrategies_HavePersistentScriptsAndIntentWeights(string archetype)
        {
            string path = "Assets/10.Datas/Actor/Enemy/BehaviorData/Strategy/CombatStrategy_Humanoid_" + archetype + ".asset";
            var strategy = AssetDatabase.LoadAssetAtPath<EnemyCombatStrategySO>(path);
            Assert.That(strategy, Is.Not.Null, path);
            Assert.That(MonoScript.FromScriptableObject(strategy)?.GetClass(), Is.EqualTo(typeof(EnemyCombatStrategySO)));
            Assert.That(strategy.intentWeights, Is.Not.Null, path);
            Assert.That(EditorUtility.IsPersistent(strategy), Is.True);
        }

        private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
    }
}
