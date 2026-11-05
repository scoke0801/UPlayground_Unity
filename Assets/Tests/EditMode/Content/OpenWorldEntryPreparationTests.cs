using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UPlayGround.Content.Tests
{
    /// <summary>초기 진입에서 먼 도시를 기다리지 않으며 저장 복원 지역은 유지한다.</summary>
    public sealed class OpenWorldEntryPreparationTests
    {
        private object _region;
        private MethodInfo _isRequired;

        [SetUp]
        public void SetUp()
        {
            var type = Type.GetType("UPlayGround.Manager.World.ContinuousWorldRegion, Assembly-CSharp", true);
            _region = Activator.CreateInstance(type);
            type.GetField("MapId").SetValue(_region, "saved-region");
            type.GetField("Bounds").SetValue(_region, new Rect(1000, 1000, 100, 100));
            type.GetField("PreloadDistance").SetValue(_region, 45f);
            _isRequired = type.GetMethod("IsRequiredForEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Test]
        public void 먼_도시는_새_게임의_준비를_막지_않는다()
        {
            Assert.That(IsRequired(Vector3.zero, null), Is.False);
        }

        [TestCase(955, true)]
        [TestCase(954, false)]
        public void 도착점의_선행_로드_거리를_경계까지_적용한다(float x, bool expected)
        {
            Assert.That(IsRequired(new Vector3(x, 500, 1050), null), Is.EqualTo(expected));
        }

        [Test]
        public void 저장_복원_지역은_기본_도착점에서_멀어도_먼저_준비한다()
        {
            Assert.That(IsRequired(Vector3.zero, "saved-region"), Is.True);
            Assert.That(IsRequired(Vector3.zero, "other-region"), Is.False);
        }

        private bool IsRequired(Vector3 position, string mapId) =>
            (bool)_isRequired.Invoke(_region, new object[] { position, mapId });
    }
}
