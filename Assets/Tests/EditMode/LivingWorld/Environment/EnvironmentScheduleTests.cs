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
