using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using Object = UnityEngine.Object;

namespace UPlayGround.Content.Tests
{
    /// <summary>기존 날씨 호환성, 강설의 실내 차단, 번개 해제를 검사한다.</summary>
    public sealed class SkyWeatherEffectsTests
    {
        private static Type Schedule => Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule, Assembly-CSharp", true);
        private static Type Effects => Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects, Assembly-CSharp", true);
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void 기존_프로필은_눈과_폭풍이_없고_새_프로필은_재입장해도_같다()
        {
            string root = "Assets/10.Datas/World/LivingWorld/Environment/";
            object old = JsonUtility.FromJson(System.IO.File.ReadAllText(root + "LakeOfLife_Weather_v1.json"), Schedule);
            object cold = JsonUtility.FromJson(System.IO.File.ReadAllText(root + "SnowStorm_Weather_v1.json"), Schedule);
            object[] args = { 720d, null };
            Assert.IsTrue((bool)Schedule.GetMethod("TryEvaluate").Invoke(old, args));
            Assert.AreEqual(0, Read(args[1], "Snow"));
            Assert.AreEqual(0, Read(args[1], "Storm"));
            Assert.IsTrue((bool)Schedule.GetMethod("TryEvaluate").Invoke(cold, args));
            Assert.AreEqual(0.85f, Read(args[1], "Snow"));
            Assert.AreEqual(1, Read(args[1], "Storm"));
            object sample = args[1];
            args[0] = 1440d * 10000 + 720;
            Assert.IsTrue((bool)Schedule.GetMethod("TryEvaluate").Invoke(cold, args));
            Assert.AreEqual(Read(sample, "Snow"), Read(args[1], "Snow"));
        }

        [TestCase("snow", -0.1f)]
        [TestCase("snow", 1.1f)]
        [TestCase("storm", float.NaN)]
        [TestCase("storm", float.PositiveInfinity)]
        public void 잘못된_강설과_폭풍_강도를_거부한다(string field, float value)
        {
            object schedule = Activator.CreateInstance(Schedule);
            Type keyType = Schedule.GetNestedType("Key");
            object key = Activator.CreateInstance(keyType);
            keyType.GetField("presetId").SetValue(key, "검사");
            keyType.GetField(field).SetValue(key, value);
            Array keys = Array.CreateInstance(keyType, 1);
            keys.SetValue(key, 0);
            Schedule.GetField("keys").SetValue(schedule, keys);
            Assert.IsFalse((bool)Schedule.GetMethod("IsValid").Invoke(schedule, null));
        }

        [Test]
        public void 눈은_관측점과_지붕층이_있을_때만_나오고_실내에서는_잔여_입자도_없앤다()
        {
            var root = new GameObject("눈 검사");
            var shelterObject = new GameObject("보호 구역", typeof(BoxCollider));
            try
            {
                var effects = root.AddComponent(Effects);
                var snowObject = new GameObject("눈");
                snowObject.transform.SetParent(root.transform, false);
                var snow = snowObject.AddComponent<ParticleSystem>();
                snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var velocity = snow.velocityOverLifetime;
                velocity.y = -1.5f;
                Effects.GetField("_snow", Private).SetValue(effects, snow);
                object sample = Activator.CreateInstance(Schedule.GetNestedType("Sample"));
                sample.GetType().GetField("Snow").SetValue(sample, 1f);
                Action<bool, bool> apply = (observer, focus) => Effects.GetMethod("Apply").Invoke(effects,
                    new[] { sample, (object)Vector3.zero, observer, focus });
                apply(true, false);
                Assert.AreEqual(0, snow.emission.rateOverTime.constant);
                Effects.GetMethod("BindShelters").Invoke(effects, new object[] { Array.Empty<BoxCollider>(), (LayerMask)1 });
                apply(true, false);
                Assert.AreEqual(80, snow.emission.rateOverTime.constant);
                Assert.AreEqual(-1.5f, snow.velocityOverLifetime.y.constant, 0.001f, "풍향 설정이 낙하속도를 바꾸면 안 된다");
                apply(true, true);
                Assert.AreEqual(20, snow.emission.rateOverTime.constant);
                apply(false, false);
                Assert.AreEqual(0, snow.emission.rateOverTime.constant);
                var shelter = shelterObject.GetComponent<BoxCollider>();
                Effects.GetMethod("BindShelters").Invoke(effects, new object[] { new[] { shelter }, (LayerMask)1 });
                snow.Emit(10);
                apply(true, false);
                Assert.AreEqual(0, snow.particleCount);
                Assert.AreEqual(0, snow.emission.rateOverTime.constant);
                Effects.GetMethod("BindShelters").Invoke(effects, new object[] { Array.Empty<BoxCollider>(), (LayerMask)1 });
                shelterObject.transform.position = Vector3.up * 3;
                Physics.SyncTransforms();
                apply(true, false);
                Assert.AreEqual(0, snow.emission.rateOverTime.constant, "보호 볼륨 없이도 실제 지붕을 감지해야 한다");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(shelterObject); }
        }

        [Test]
        public void 번개는_한번_준비한_버퍼로_생성하고_해제하면_섬광과_천둥을_정리한다()
        {
            var root = new GameObject("번개 검사");
            try
            {
                var effects = root.AddComponent(Effects);
                var lineObject = new GameObject("번개 줄기", typeof(LineRenderer));
                lineObject.transform.SetParent(root.transform, false);
                var line = lineObject.GetComponent<LineRenderer>();
                Effects.GetField("_lightningBolt", Private).SetValue(effects, line);
                Effects.GetMethod("Initialize").Invoke(effects, null);
                Effects.GetMethod("BeginStrike", Private).Invoke(effects, new object[] { Vector3.zero, 1f });
                Assert.AreEqual(25, line.positionCount);
                Assert.That(line.GetPosition(0).y, Is.GreaterThan(line.GetPosition(24).y));
                Assert.That(((Vector3)Effects.GetProperty("LightningDirection").GetValue(effects)).magnitude, Is.EqualTo(1).Within(0.001f));
                Assert.That((float)Effects.GetField("_thunderCountdown", Private).GetValue(effects), Is.GreaterThan(0));
                Effects.GetMethod("StopEffects").Invoke(effects, null);
                Assert.IsFalse(line.enabled);
                Assert.AreEqual(0, Effects.GetProperty("LightningIntensity").GetValue(effects));
                Assert.AreEqual(-1, Effects.GetField("_thunderCountdown", Private).GetValue(effects));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void 밤하늘_지역_보간은_새_표현의_끝점과_중간값을_보존한다()
        {
            var a = new SkySample { Stars = 0, Galaxy = 0.1f, Moon = 1, MoonRadius = 0.5f, MoonPhase = 0 };
            var b = new SkySample { Stars = 2, Galaxy = 0.3f, Moon = 2, MoonRadius = 1, MoonPhase = 1 };
            Assert.AreEqual(a.Stars, SkySample.Lerp(a, b, 0).Stars);
            Assert.AreEqual(b.Moon, SkySample.Lerp(a, b, 1).Moon);
            var half = SkySample.Lerp(a, b, 0.5f);
            Assert.AreEqual(1, half.Stars);
            Assert.AreEqual(0.75f, half.MoonRadius);
            Assert.AreEqual(0.5f, half.MoonPhase);
        }

        [TestCase("EnvironmentEffects")]
        [TestCase("LakeOfLifeEnvironment")]
        [TestCase("DroughtVillageEnvironment")]
        public void 환경_프리팹의_눈과_번개_리소스가_모두_연결된다(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/LivingWorld/Environment/" + name + ".prefab");
            Assert.IsNotNull(prefab);
            var effects = prefab.GetComponentInChildren(Effects, true);
            Assert.IsNotNull(effects);
            var data = new SerializedObject(effects);
            foreach (string field in new[] { "_snow", "_lightningBolt", "_lightningBranch", "_lightningLight", "_thunderAudio" })
                Assert.IsNotNull(data.FindProperty(field).objectReferenceValue, field);
            Assert.That(data.FindProperty("_thunderClips").arraySize, Is.GreaterThan(0));
            Assert.IsNotNull(data.FindProperty("_thunderClips").GetArrayElementAtIndex(0).objectReferenceValue);
            var snow = (ParticleSystem)data.FindProperty("_snow").objectReferenceValue;
            Assert.AreEqual(ParticleSystemSimulationSpace.World, snow.main.simulationSpace);
            Assert.IsFalse(snow.main.playOnAwake);
            Assert.AreEqual(-1.5f, snow.velocityOverLifetime.y.constant);
            Assert.IsNotNull(snow.GetComponent<ParticleSystemRenderer>().sharedMaterial);
        }

        private static float Read(object sample, string field) => (float)sample.GetType().GetField(field).GetValue(sample);
    }
}
