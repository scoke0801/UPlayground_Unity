using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Simulation;
using Object = UnityEngine.Object;

namespace UPlayGround.AI.Tests
{
    /// <summary>거리 정지와 활성 임대가 본 제약 및 옷감 설정의 원래 상태를 보존하는지 검사한다.</summary>
    public sealed class ActorSecondaryMotionSimulationTests
    {
        private GameObject _root;
        private ActorSimulationParticipant _participant;
        private Behaviour _binding;
        private Type _bindingType;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("보조 모션 검사");
            _participant = _root.AddComponent<ActorSimulationParticipant>();
            _bindingType = Type.GetType("UPlayGround.Manager.ActorSecondaryMotionSimulation, Assembly-CSharp", true);
            _binding = (Behaviour)_root.AddComponent(_bindingType);
            // EditMode에서는 일반 MonoBehaviour의 런타임 생명주기를 직접 준비한다.
            CallLifecycle("OnEnable");
            _bindingType.GetMethod("Bind").Invoke(_binding, new object[] { _participant });
        }

        [TearDown]
        public void TearDown()
        {
            CallLifecycle("OnDisable");
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void 원래_활성인_제약만_정지하고_반복_전환에도_원래_상태를_복구한다()
        {
            var active = _root.AddComponent<ParentConstraint>();
            var disabled = _root.AddComponent<RotationConstraint>();
            disabled.enabled = false;
            for (int i = 0; i < 2; i++)
            {
                _participant.SetSimulationState(ActorSimulationState.Suspended);
                _participant.SetSimulationState(ActorSimulationState.Suspended);
                Assert.That(active.enabled, Is.False);
                Assert.That(disabled.enabled, Is.False);
                _participant.SetSimulationState(ActorSimulationState.Active);
                Assert.That(active.enabled, Is.True);
                Assert.That(disabled.enabled, Is.False);
            }
        }

        [Test]
        public void 활성_임대는_제약을_즉시_복구하고_임대_중_정지를_막는다()
        {
            var constraint = _root.AddComponent<ParentConstraint>();
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            using (_participant.AcquireActiveLease(this, "대화 검사"))
            {
                Assert.That(constraint.enabled, Is.True);
                _participant.SetSimulationState(ActorSimulationState.Suspended);
                Assert.That(constraint.enabled, Is.True);
            }
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            Assert.That(constraint.enabled, Is.False);
        }

        [Test]
        public void 비활성화와_등록해제는_복구하고_재활성화는_정지상태를_다시_적용한다()
        {
            var constraint = _root.AddComponent<ParentConstraint>();
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            _binding.enabled = false;
            CallLifecycle("OnDisable");
            Assert.That(constraint.enabled, Is.True);
            _binding.enabled = true;
            CallLifecycle("OnEnable");
            Assert.That(constraint.enabled, Is.False);
            _bindingType.GetMethod("Unbind").Invoke(_binding, null);
            Assert.That(constraint.enabled, Is.True);
            _participant.SetSimulationState(ActorSimulationState.Active);
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            Assert.That(constraint.enabled, Is.True);
        }

        [Test]
        public void 파괴된_제약은_건너뛰고_다음_정지에서_교체된_모델을_반영한다()
        {
            var original = _root.AddComponent<ParentConstraint>();
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            Object.DestroyImmediate(original);
            Assert.DoesNotThrow(() => _participant.SetSimulationState(ActorSimulationState.Active));
            var child = new GameObject("교체 모델");
            child.transform.SetParent(_root.transform);
            var replacement = child.AddComponent<ParentConstraint>();
            child.SetActive(false);
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            Assert.That(replacement.enabled, Is.False);
            _participant.SetSimulationState(ActorSimulationState.Active);
            Assert.That(replacement.enabled, Is.True);
        }

        [TestCase(0)]
        [TestCase(30)]
        public void 옷감은_활성을_유지하고_컬링_설정만_임시_변경한다(int originalMode)
        {
            Type clothType = Type.GetType("MagicaCloth2.MagicaCloth, MagicaClothV2", true);
            var cloth = (Behaviour)_root.AddComponent(clothType);
            object data = clothType.GetProperty("SerializeData").GetValue(cloth);
            object settings = data.GetType().GetField("cullingSettings").GetValue(data);
            FieldInfo mode = settings.GetType().GetField("cameraCullingMode");
            mode.SetValue(settings, Enum.ToObject(mode.FieldType, originalMode));
            _participant.SetSimulationState(ActorSimulationState.Suspended);
            Assert.That(cloth.enabled, Is.True);
            Assert.That(Convert.ToInt32(mode.GetValue(settings)), Is.EqualTo(20));
            _participant.SetSimulationState(ActorSimulationState.Active);
            Assert.That(cloth.enabled, Is.True);
            Assert.That(Convert.ToInt32(mode.GetValue(settings)), Is.EqualTo(originalMode));
        }

        private void CallLifecycle(string method) => _bindingType
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_binding, null);
    }
}
