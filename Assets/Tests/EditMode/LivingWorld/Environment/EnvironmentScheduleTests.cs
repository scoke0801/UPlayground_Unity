using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace UPlayGround.Core.Tests
{
    /// <summary>공용 asmdef 변경 없이 D의 실제 시간표와 저장 시각 복원 규칙을 검사한다.</summary>
    public sealed class EnvironmentScheduleTests
    {
        private const string Root = "Assets/10.Datas/World/LivingWorld/Environment/";
        private static Type Schedule => Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule, Assembly-CSharp", true);

        private static object Load(string name)
        {
            Type json = Type.GetType("UnityEngine.JsonUtility, UnityEngine.JSONSerializeModule", true);
            return json.GetMethod("FromJson", new[] { typeof(string), typeof(Type) }).Invoke(null,
                new object[] { File.ReadAllText(Root + name + "_Weather_v1.json"), Schedule });
        }

        private static bool Evaluate(object schedule, double minute, out object sample)
        {
            object[] args = { minute, null };
            bool result = (bool)Schedule.GetMethod("TryEvaluate").Invoke(schedule, args);
            sample = args[1];
            return result;
        }
        private static float Value(object sample, string name) => (float)sample.GetType().GetField(name).GetValue(sample);

        private static object CreateRainSchedule()
        {
            Type json = Type.GetType("UnityEngine.JsonUtility, UnityEngine.JSONSerializeModule", true);
            return json.GetMethod("FromJson", new[] { typeof(string), typeof(Type) }).Invoke(null,
                new object[] { "{\"version\":1,\"cycleMinutes\":120,\"keys\":["
                    + "{\"minute\":0,\"presetId\":\"Clear\",\"cloud\":0.1},"
                    + "{\"minute\":60,\"presetId\":\"Clear\",\"cloud\":0.1},"
                    + "{\"minute\":70,\"presetId\":\"Rain\",\"rain\":0.4,\"wetness\":0.6,\"cloud\":0.9},"
                    + "{\"minute\":80,\"presetId\":\"Clear\",\"cloud\":0.1}]}", Schedule });
        }

        [Test]
        public void 강수_전에_흐려져도_빗방울과_젖음은_미리_발생하지_않는다()
        {
            object schedule = CreateRainSchedule();
            Evaluate(schedule, 20, out var clear);
            Evaluate(schedule, 55, out var approaching);
            Assert.AreEqual(0, Value(clear, "RainCloud"));
            Assert.That(Value(approaching, "RainCloud"), Is.GreaterThan(0.5f), "짧은 소나기도 예고해야 한다");
            Assert.That(Value(approaching, "Cloud"), Is.GreaterThan(Value(clear, "Cloud")));
            Assert.AreEqual(0, Value(approaching, "Rain"));
            Assert.AreEqual(0, Value(approaching, "Wetness"));
            Assert.AreEqual(0, Value(approaching, "Storm"));
            Evaluate(schedule, 175, out var replay);
            Assert.AreEqual(Value(approaching, "RainCloud"), Value(replay, "RainCloud"));
        }

        [Test]
        public void 예고_해제와_시간표_순환에서도_강수_원본은_보존된다()
        {
            object schedule = CreateRainSchedule();
            Schedule.GetField("preRainMinutes").SetValue(schedule, 0f);
            Evaluate(schedule, 55, out var before);
            Assert.AreEqual(0, Value(before, "RainCloud"));
            Evaluate(schedule, 70, out var rain);
            Assert.AreEqual(0.4f, Value(rain, "Rain"));
            Assert.AreEqual(1, Value(rain, "RainCloud"));
            Assert.AreEqual(0.98f, Value(rain, "Cloud"), 0.0001f);
            Schedule.GetField("preRainMinutes").SetValue(schedule, 80f);
            Evaluate(schedule, 115, out var midnight);
            Evaluate(CreateRainSchedule(), 115, out var noForecast);
            Assert.That(Value(midnight, "RainCloud"), Is.GreaterThan(Value(noForecast, "RainCloud")));
            Evaluate(schedule, 235, out var nextCycle);
            Assert.AreEqual(Value(midnight, "RainCloud"), Value(nextCycle, "RainCloud"));
        }

        [TestCase("preRainMinutes", -1f)]
        [TestCase("preRainMinutes", float.NaN)]
        [TestCase("rainCloudFullIntensity", 0f)]
        [TestCase("rainCloudCoverage", 1.1f)]
        public void 잘못된_강수_예고_설정을_거부한다(string field, float value)
        {
            object schedule = CreateRainSchedule();
            Schedule.GetField(field).SetValue(schedule, value);
            Assert.IsFalse(Evaluate(schedule, 55, out _));
        }

        [Test]
        public void 가뭄_기본표는_모든_시각에서_비와_젖음이_없다()
        {
            object profile = Load("DroughtVillage");
            for (int minute = 0; minute < 2880; minute++)
            {
                Assert.IsTrue(Evaluate(profile, minute, out var sample));
                Assert.AreEqual(0, Value(sample, "Rain"));
                Assert.AreEqual(0, Value(sample, "Wetness"));
            }
        }

        [Test]
        public void 재입장과_시각_되감기는_같은_날씨를_복원한다()
        {
            object first = Load("LakeOfLife");
            Evaluate(first, 780.5, out var before);
            Evaluate(first, 999999, out _);
            Evaluate(Load("LakeOfLife"), 780.5, out var after);
            foreach (string field in new[] { "Rain", "Wetness", "Cloud", "PhaseMinute" })
                Assert.AreEqual(Value(before, field), Value(after, field), field);
        }

        [Test]
        public void 비가_그친_후_재입장해도_건조_중간값이_남는다()
        {
            Evaluate(Load("LakeOfLife"), 810, out var sample);
            Assert.AreEqual(0, Value(sample, "Rain"));
            Assert.That(Value(sample, "Wetness"), Is.InRange(0.4f, 0.8f));
            Evaluate(Load("LakeOfLife"), 1080, out sample);
            Assert.AreEqual(0, Value(sample, "Wetness"));
        }

        [Test]
        public void 자정과_큰_게임시각에서도_순환이_일치한다()
        {
            object profile = Load("LakeOfLife");
            Evaluate(profile, 600.25, out var a);
            Evaluate(profile, 1440d * 100000 + 600.25, out var b);
            Assert.AreEqual(Value(a, "Rain"), Value(b, "Rain"));
            Evaluate(profile, 1440, out a);
            Evaluate(profile, 0, out b);
            Assert.AreEqual(Value(a, "Wetness"), Value(b, "Wetness"));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1d)]
        public void 잘못된_시각을_반영하지_않는다(double minute)
        {
            Assert.IsFalse(Evaluate(Load("LakeOfLife"), minute, out _));
        }

        [Test]
        public void 중복_키와_잘못된_주기는_거부한다()
        {
            object profile = Load("LakeOfLife");
            Schedule.GetField("cycleMinutes").SetValue(profile, 0f);
            Assert.IsFalse(Evaluate(profile, 0, out _));
            profile = Load("LakeOfLife");
            var keys = (Array)Schedule.GetField("keys").GetValue(profile);
            object second = keys.GetValue(1);
            second.GetType().GetField("minute").SetValue(second, 0f);
            keys.SetValue(second, 1);
            Assert.IsFalse(Evaluate(profile, 0, out _));
        }
    }
}
