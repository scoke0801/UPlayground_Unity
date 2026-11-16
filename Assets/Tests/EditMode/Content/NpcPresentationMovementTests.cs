using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UPlayGround.Content.Tests
{
    /// <summary>대기 중 보행과 완료 후 고정 작업이 저장 사실에 맞춰 왕복 복원되는지 검사한다.</summary>
    public sealed class NpcPresentationMovementTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private Component _brain, _presentation;
        private ScriptableObject _before, _after;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("주민 사건 전후 행동 검사");
            _root.SetActive(false);
            _brain = _root.AddComponent(Type.GetType("UPlayGround.Components.NpcBrain, UPlayGround.Actor", true));
            _presentation = _root.AddComponent(Type.GetType("UPlayGround.Gameplay.Interaction.NpcLifePresentation, Assembly-CSharp", true));
            var motionType = Type.GetType("UPlayGround.Animation.MotionSetAsset, UPlayGround.MotionSet.Core", true);
            _before = ScriptableObject.CreateInstance(motionType);
            _after = ScriptableObject.CreateInstance(motionType);
            Set("_beforeWorkMotion", _before);
            Set("_afterWorkMotion", _after);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_before);
            UnityEngine.Object.DestroyImmediate(_after);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 이동을_저작하지_않은_기존_배치는_작업만_복원한다(bool initialWander)
        {
            _brain.GetType().GetMethod("SetWanderEnabled").Invoke(_brain, new object[] { initialWander });
            Apply(false);
            Assert.That(Work, Is.SameAs(_before));
            Apply(true);
            Assert.That(Work, Is.SameAs(_after));
            Assert.That(CanWander, Is.EqualTo(initialWander));
        }

        [Test]
        public void 식사_전_보행과_배식_후_착석은_완료전후_로드에서_복원된다()
        {
            Set("_configureWander", true);
            Set("_beforeCanWander", true);
            Set("_afterCanWander", false);
            foreach (bool completed in new[] { false, true, true, false, true })
            {
                Apply(completed);
                Assert.That(CanWander, Is.EqualTo(!completed));
                Assert.That(Work, Is.SameAs(completed ? _after : _before));
            }
        }

        private bool CanWander => (bool)_brain.GetType().GetProperty("EnableWander").GetValue(_brain);
        private object Work => _brain.GetType().GetProperty("WorkMotion").GetValue(_brain);
        private void Apply(bool completed) => _presentation.GetType().GetMethod("ApplyWorkBehaviour", Fields)
            .Invoke(_presentation, new object[] { _brain, completed });
        private void Set(string name, object value) => _presentation.GetType().GetField(name, Fields).SetValue(_presentation, value);
    }
}
