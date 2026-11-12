using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Content.Tests
{
    /// <summary>경계 연속성·저장 시각 복원·순환 바람 적분의 회귀를 검사한다.</summary>
    public sealed class RegionalSkyTests
    {
        private static Type Schedule => Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule, Assembly-CSharp", true);

        [TestCase(0d)]
        [TestCase(7.25d)]
        [TestCase(20d)]
        [TestCase(19.9999d)]
        [TestCase(20.0001d)]
        [TestCase(53.75d)]
        public void 누적_바람은_수치적분과_일치하고_재조회해도_같다(double minute)
        {
            object schedule = JsonUtility.FromJson("{\"cycleMinutes\":20,\"version\":1,\"keys\":["
                + "{\"minute\":0,\"presetId\":\"a\",\"wind\":{\"x\":0.8,\"y\":0}},"
                + "{\"minute\":10,\"presetId\":\"b\",\"wind\":{\"x\":0,\"y\":0.6}}]}", Schedule);
            MethodInfo integrate = Schedule.GetMethod("TryIntegrateWind");
            object[] args = { minute, null };
            Assert.IsTrue((bool)integrate.Invoke(schedule, args));
            Vector2 actual = (Vector2)args[1];
            Vector2 expected = Vector2.zero;
            const int steps = 1000;
            double step = minute / steps;
            for (int i = 0; i < steps; i++)
            {
                object[] evaluate = { (i + 0.5) * step, null };
                Assert.IsTrue((bool)Schedule.GetMethod("TryEvaluate").Invoke(schedule, evaluate));
                expected += (Vector2)evaluate[1].GetType().GetField("Wind").GetValue(evaluate[1]) * (float)step;
            }
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.002f));
            object[] replay = { minute, null };
            Assert.IsTrue((bool)integrate.Invoke(schedule, replay));
            Assert.AreEqual(actual, replay[1]);
        }

        [Test]
        public void 지역_가중치는_회전과_스케일을_반영하고_외곽에서_연속적으로_사라진다()
        {
            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var go = new GameObject("SkyRegionTest");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, preview);
            var profile = ScriptableObject.CreateInstance<SkyProfileSO>();
            try
            {
                Type regionType = Type.GetType("UPlayGround.Gameplay.World.SkyRegionBinding, Assembly-CSharp", true);
                var region = go.AddComponent(regionType);
                regionType.GetField("profile").SetValue(region, profile);
                regionType.GetField("size").SetValue(region, new Vector3(10, 10, 10));
                regionType.GetField("blendDistance").SetValue(region, 10f);
                go.transform.localScale = new Vector3(2, 1, 1);
                go.transform.rotation = Quaternion.Euler(0, 45, 0);
                MethodInfo evaluate = regionType.GetMethod("EvaluateWeight");
                Func<float, float> weight = x => (float)evaluate.Invoke(region, new object[] { go.transform.TransformPoint(new Vector3(x, 0, 0)) });
                Assert.AreEqual(1, weight(5));
                Assert.That(weight(7.5f), Is.EqualTo(0.5f).Within(0.0001));
                Assert.AreEqual(0, weight(10));
                Assert.That(Mathf.Abs(weight(5.001f) - weight(4.999f)), Is.LessThan(0.001));
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void 하루_순환과_보간_끝점은_원본_값을_보존한다()
        {
            var profile = ScriptableObject.CreateInstance<SkyProfileSO>();
            try
            {
                Assert.AreEqual(profile.Evaluate(0).Zenith, profile.Evaluate(1).Zenith);
                var a = profile.Evaluate(0.3f);
                profile.cloudCoverage = 0.8f;
                profile.fogEnd = 300;
                var b = profile.Evaluate(0.6f);
                Assert.AreEqual(a.Coverage, SkySample.Lerp(a, b, 0).Coverage);
                Assert.AreEqual(b.FogEnd, SkySample.Lerp(a, b, 1).FogEnd);
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 구름_폴리싱_값은_지역_중간값과_끝점을_보존한다()
        {
            var a = new SkySample { Detail = 0.02f, Rim = 0.1f };
            var b = new SkySample { Detail = 0.08f, Rim = 0.3f };
            Assert.AreEqual(a.Detail, SkySample.Lerp(a, b, 0).Detail);
            Assert.AreEqual(b.Rim, SkySample.Lerp(a, b, 1).Rim);
            Assert.That(SkySample.Lerp(a, b, 0.5f).Detail, Is.EqualTo(0.05f).Within(0.0001f));
        }

        [Test]
        public void 경계_왕복은_현재값에서_이어지고_순간이동과_재입장은_즉시_적용한다()
        {
            WithRendering((rendering, type, go) =>
            {
                var update = type.GetMethod("UpdateSkySample", BindingFlags.NonPublic | BindingFlags.Instance);
                var field = type.GetField("_skySample", BindingFlags.NonPublic | BindingFlags.Instance);
                Action<float, Vector3> apply = (coverage, position) => update.Invoke(rendering,
                    new object[] { new SkySample { Coverage = coverage }, position, 1f });
                apply(0.3f, Vector3.zero);
                apply(0.8f, Vector3.right);
                float outbound = ((SkySample)field.GetValue(rendering)).Coverage;
                apply(0.3f, Vector3.zero);
                float inbound = ((SkySample)field.GetValue(rendering)).Coverage;
                Assert.That(outbound, Is.InRange(0.3f, 0.8f));
                Assert.That(inbound, Is.GreaterThan(0.3f).And.LessThan(outbound));
                apply(0.8f, Vector3.right * 200);
                Assert.AreEqual(0.8f, ((SkySample)field.GetValue(rendering)).Coverage);
                type.GetMethod("RestoreRegionalSky", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rendering, null);
                apply(0.3f, Vector3.right * 200);
                Assert.AreEqual(0.3f, ((SkySample)field.GetValue(rendering)).Coverage);
            });
        }

        [TestCase(0, 0, 0.8f)]
        [TestCase(2, 1, 0.6f)]
        public void 중첩은_우선순위와_동순위_배열_순서를_따르고_비활성_지역은_제외한다(int firstPriority, int lastPriority, float expected)
        {
            WithRendering((rendering, type, go) =>
            {
                Type regionType = Type.GetType("UPlayGround.Gameplay.World.SkyRegionBinding, Assembly-CSharp", true);
                var profiles = new[] { ScriptableObject.CreateInstance<SkyProfileSO>(),
                    ScriptableObject.CreateInstance<SkyProfileSO>(), ScriptableObject.CreateInstance<SkyProfileSO>() };
                try
                {
                    profiles[0].cloudCoverage = 0.3f;
                    profiles[1].cloudCoverage = 0.6f;
                    profiles[2].cloudCoverage = 0.8f;
                    var regions = Array.CreateInstance(regionType, 2);
                    for (int i = 0; i < 2; i++)
                    {
                        var regionObject = new GameObject("Region");
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(regionObject, go.scene);
                        regionObject.transform.SetParent(go.transform, false);
                        var region = regionObject.AddComponent(regionType);
                        regionType.GetField("profile").SetValue(region, profiles[i + 1]);
                        regionType.GetField("priority").SetValue(region, i == 0 ? firstPriority : lastPriority);
                        regions.SetValue(region, i);
                    }
                    var serialized = new UnityEditor.SerializedObject(rendering);
                    serialized.FindProperty("_defaultSkyProfile").objectReferenceValue = profiles[0];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    type.GetField("_skyRegions", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rendering, regions);
                    type.GetMethod("InitializeRegionalSky", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rendering, null);
                    var evaluate = type.GetMethod("EvaluateRegionalSky", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.AreEqual(expected, ((SkySample)evaluate.Invoke(rendering, new object[] { 0.5f, Vector3.zero })).Coverage);
                    ((Behaviour)regions.GetValue(0)).enabled = false;
                    ((Behaviour)regions.GetValue(1)).enabled = false;
                    Assert.AreEqual(0.3f, ((SkySample)evaluate.Invoke(rendering, new object[] { 0.5f, Vector3.zero })).Coverage);
                }
                finally
                {
                    foreach (var profile in profiles) UnityEngine.Object.DestroyImmediate(profile);
                }
            });
        }

        [Test]
        public void 지역_크기가_변해도_기본_프로필의_운송_좌표와_위상은_유지된다()
        {
            WithRendering((rendering, type, go) =>
            {
                var profile = ScriptableObject.CreateInstance<SkyProfileSO>();
                var material = new Material(Shader.Find("UPlayGround/Environment/Day Sky"));
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                try
                {
                    profile.cloudScale = 5000;
                    profile.cloudFlowMultiplier = 3;
                    profile.highCloudSpeed = 0.45f;
                    type.GetField("_defaultSkyProfile", flags).SetValue(rendering, profile);
                    type.GetField("_runtimeSky", flags).SetValue(rendering, material);
                    type.GetMethod("ConfigureCloudMotion", flags).Invoke(rendering, null);
                    material.SetFloat("_CloudClock", 144000);
                    material.SetVector("_CloudWindOffset", new Vector4(12000, 6000, 0, 0));
                    profile.cloudFlowMultiplier = 9;
                    profile.highCloudSpeed = 2;
                    foreach (float scale in new[] { 5000f, 5750f, 6500f, 5000f })
                    {
                        type.GetMethod("ApplySkySample", flags).Invoke(rendering,
                            new object[] { new SkySample { Scale = scale, Height = 1300 } });
                        Assert.AreEqual(scale, material.GetFloat("_CloudScale"));
                        Assert.AreEqual(5000, material.GetFloat("_CloudMotionScale"));
                        Assert.AreEqual(3, material.GetFloat("_CloudFlowMultiplier"));
                        Assert.AreEqual(0.45f, material.GetFloat("_HighCloudSpeed"));
                        Assert.AreEqual(144000, material.GetFloat("_CloudClock"));
                        Assert.AreEqual(new Vector4(12000, 6000, 0, 0), material.GetVector("_CloudWindOffset"));
                    }
                }
                finally
                {
                    type.GetField("_runtimeSky", flags).SetValue(rendering, null);
                    UnityEngine.Object.DestroyImmediate(material);
                    UnityEngine.Object.DestroyImmediate(profile);
                }
            });
        }

        [TestCase(0.1f, 0.3028f)]
        [TestCase(0.63f, 0.411132f)]
        [TestCase(0.9f, 0.5268f)]
        public void 날씨_구름량은_기본과_중복_합산하지_않고_목표로_접근한다(float weather, float expected)
        {
            var sample = new SkySample { Coverage = 0.3f, OvercastCoverage = 0.58f };
            Assert.That(sample.EvaluateCloudCoverage(weather), Is.EqualTo(expected).Within(0.00001f));
        }

        [Test]
        public void 날씨_보간은_단조롭고_지역_목표를_넘거나_기본보다_감소하지_않는다()
        {
            var a = new SkySample { Coverage = 0.3f, OvercastCoverage = 0.58f };
            var b = new SkySample { Coverage = 0.36f, OvercastCoverage = 0.64f };
            foreach (var sample in new[] { a, SkySample.Lerp(a, b, 0.5f), b })
            {
                Assert.AreEqual(sample.Coverage, sample.EvaluateCloudCoverage(-1));
                Assert.AreEqual(sample.OvercastCoverage, sample.EvaluateCloudCoverage(2));
                float previous = sample.Coverage;
                for (int i = 0; i <= 100; i++)
                {
                    float coverage = sample.EvaluateCloudCoverage(i / 100f);
                    Assert.That(coverage, Is.InRange(previous, sample.OvercastCoverage));
                    previous = coverage;
                }
            }
            Assert.That(SkySample.Lerp(a, b, 0.5f).OvercastCoverage, Is.EqualTo(0.61f).Within(0.00001f));
            Assert.AreEqual(0.8f, new SkySample { Coverage = 0.8f, OvercastCoverage = 0.58f }.EvaluateCloudCoverage(1));
        }

        [Test]
        public void 강수_예고는_구름과_조명에만_전달되고_해제하면_지역_하늘로_돌아간다()
        {
            WithRendering((rendering, type, go) =>
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var environmentType = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentController, Assembly-CSharp", true);
                var sampleType = Schedule.GetNestedType("Sample");
                var environmentObject = new GameObject("강수 예고 검사");
                environmentObject.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(environmentObject, go.scene);
                environmentObject.transform.SetParent(go.transform, false);
                var environment = environmentObject.AddComponent(environmentType);
                var material = new Material(Shader.Find("UPlayGround/Environment/Day Sky"));
                try
                {
                    type.GetField("_runtimeSky", flags).SetValue(rendering, material);
                    type.GetField("_environment", flags).SetValue(rendering, environment);
                    environmentType.GetField("<IsReady>k__BackingField", flags).SetValue(environment, true);
                    object weather = Activator.CreateInstance(sampleType);
                    sampleType.GetField("Cloud").SetValue(weather, 0.98f);
                    sampleType.GetField("RainCloud").SetValue(weather, 1f);
                    environmentType.GetField("<Current>k__BackingField", flags).SetValue(environment, weather);
                    var regional = new SkySample { Coverage = 0.3f, OvercastCoverage = 0.8f, Rim = 0.2f };
                    type.GetMethod("ApplySkySample", flags).Invoke(rendering, new object[] { regional });
                    Assert.AreEqual(1, material.GetFloat("_RainCloudIntensity"));
                    Assert.AreEqual(0.98f, material.GetFloat("_CloudCoverage"), 0.0001f);
                    Assert.AreEqual(0.55f, (float)environmentType.GetProperty("LightMultiplier").GetValue(environment), 0.0001f);
                    Assert.AreEqual(-0.25f, (float)environmentType.GetProperty("ExposureOffset").GetValue(environment), 0.0001f);
                    Assert.AreEqual(0, (float)environmentType.GetProperty("RainIntensity").GetValue(environment));
                    environmentType.GetField("<Current>k__BackingField", flags).SetValue(environment, Activator.CreateInstance(sampleType));
                    type.GetMethod("ApplySkySample", flags).Invoke(rendering, new object[] { regional });
                    Assert.AreEqual(0, material.GetFloat("_RainCloudIntensity"));
                    Assert.AreEqual(0.3f, material.GetFloat("_CloudCoverage"), 0.0001f);
                    Assert.AreEqual(0.2f, material.GetFloat("_CloudRimStrength"), 0.0001f);
                    Assert.AreEqual(1, (float)environmentType.GetProperty("LightMultiplier").GetValue(environment));
                }
                finally
                {
                    type.GetField("_runtimeSky", flags).SetValue(rendering, null);
                    UnityEngine.Object.DestroyImmediate(material);
                }
            });
        }

        private static void WithRendering(Action<Component, Type, GameObject> test)
        {
            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var go = new GameObject("SkyTransitionTest");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, preview);
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.SceneEnvironmentRendering, Assembly-CSharp", true);
                test(go.AddComponent(type), type, go);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
