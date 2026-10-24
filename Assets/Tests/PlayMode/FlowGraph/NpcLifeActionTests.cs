using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>생활 행동은 표시 가능한 결과가 있어야 확정되며 저장 사실에서 전후 배치를 복원한다.</summary>
    public sealed class NpcLifeActionTests
    {
        private GameObject _root, _before, _after;
        private Component _presentation;
        private Flags _flags;
        private IGlobalFlagService _previousFlags;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Services.TryGet(out _previousFlags);
            Services.Unregister(_previousFlags);
            _flags = new Flags();
            Services.Register(_flags);
            _root = new GameObject("생활 행동 검사");
            _root.SetActive(false);
            _presentation = _root.AddComponent(Type.GetType("UPlayGround.Gameplay.Interaction.NpcLifePresentation, Assembly-CSharp", true));
            _before = new GameObject("인계 전 꾸러미");
            _after = new GameObject("인계한 꾸러미");
            _before.transform.SetParent(_root.transform);
            _after.transform.SetParent(_root.transform);
            Set("_completedFlag", "test.life.delivered");
            Set("_beforeVisuals", new[] { _before });
            Set("_afterVisuals", new[] { _after });
            _root.SetActive(true);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(_root);
            yield return null;
            Services.Unregister(_flags);
            Services.Register(_previousFlags);
        }

        [UnityTest]
        public IEnumerator 인계는_배치를_먼저_반영하고_한번만_기록한다()
        {
            bool visibleAtCommit = false;
            _flags.OnFlagChanged += (_, value) => visibleAtCommit = value && !_before.activeSelf && _after.activeSelf;
            Assert.That(Complete(), Is.True);
            Assert.That(visibleAtCommit, Is.True);
            Assert.That(Complete(), Is.False);
            Assert.That(_flags.Writes, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator 결과가_없는_빈_상호작용은_사실을_만들지않는다()
        {
            Set("_beforeVisuals", Array.Empty<GameObject>());
            Set("_afterVisuals", Array.Empty<GameObject>());
            Assert.That(Complete(), Is.False);
            Assert.That(_flags.Writes, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 누락된_소품은_부분적용없이_실패한다()
        {
            Set("_afterVisuals", new GameObject[] { null });
            Assert.That(Complete(), Is.False);
            Assert.That(_before.activeSelf, Is.True);
            Assert.That(_flags.Writes, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 완료전후_로드와_재활성화는_저장된_배치를_복원한다()
        {
            Assert.That(Complete(), Is.True);
            _flags.Restore(false);
            yield return null;
            yield return null;
            Assert.That(_before.activeSelf && !_after.activeSelf, Is.True);
            _flags.Restore(true);
            yield return null;
            yield return null;
            Assert.That(!_before.activeSelf && _after.activeSelf, Is.True);
            _root.SetActive(false);
            _flags.Restore(false);
            _root.SetActive(true);
            yield return null;
            yield return null;
            Assert.That(_before.activeSelf && !_after.activeSelf, Is.True);
            Assert.That(_flags.Writes, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator 소품은_착지한_뒤에만_완료되고_로드에서는_낙하를_반복하지않는다()
        {
            Set("_deliveryVisuals", new[] { _after });
            Set("_deliveryDuration", 0.08f);
            Vector3 landing = _after.transform.localPosition;
            var delivery = (IEnumerator)_presentation.GetType().GetMethod("PresentDelivery").Invoke(_presentation, null);
            Assert.That(delivery.MoveNext(), Is.True);
            Assert.That(_after.activeSelf, Is.True);
            Assert.That(_after.transform.localPosition.y, Is.GreaterThan(landing.y));
            Assert.That(_flags.Writes, Is.Zero);
            yield return delivery.Current;
            while (delivery.MoveNext()) yield return delivery.Current;
            Assert.That(_after.transform.localPosition, Is.EqualTo(landing));
            Assert.That(Complete(), Is.True);
            Assert.That(_after.activeSelf, Is.True);
            _flags.Restore(true);
            yield return null;
            yield return null;
            Assert.That(_after.transform.localPosition, Is.EqualTo(landing));
            Assert.That(_flags.Writes, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator 낙하중_취소와_로드는_완료사실없이_소품을_복원한다()
        {
            Set("_deliveryVisuals", new[] { _after });
            Vector3 landing = _after.transform.localPosition;
            var delivery = (IEnumerator)_presentation.GetType().GetMethod("PresentDelivery").Invoke(_presentation, null);
            Assert.That(delivery.MoveNext(), Is.True);
            _presentation.GetType().GetMethod("CancelDelivery").Invoke(_presentation, null);
            Assert.That(delivery.MoveNext(), Is.False);
            Assert.That(_after.transform.localPosition, Is.EqualTo(landing));
            Assert.That(_after.activeSelf, Is.False);
            delivery = (IEnumerator)_presentation.GetType().GetMethod("PresentDelivery").Invoke(_presentation, null);
            Assert.That(delivery.MoveNext(), Is.True);
            _flags.Restore(false);
            yield return null;
            yield return null;
            Assert.That(delivery.MoveNext(), Is.False);
            Assert.That(_after.transform.localPosition, Is.EqualTo(landing));
            Assert.That(_after.activeSelf, Is.False);
            Assert.That(_before.activeSelf, Is.True);
            Assert.That(_flags.Writes, Is.Zero);
        }

        [UnityTest]
        public IEnumerator 취소한_소품_연출의_늦은_재개는_새_인계를_변경하지않는다()
        {
            Set("_deliveryVisuals", new[] { _after });
            var method = _presentation.GetType().GetMethod("PresentDelivery");
            var oldDelivery = (IEnumerator)method.Invoke(_presentation, null);
            Assert.IsTrue(oldDelivery.MoveNext());
            _presentation.GetType().GetMethod("CancelDelivery").Invoke(_presentation, null);
            var newDelivery = (IEnumerator)method.Invoke(_presentation, null);
            Assert.IsTrue(newDelivery.MoveNext());
            Vector3 position = _after.transform.localPosition;
            Assert.IsFalse(oldDelivery.MoveNext());
            Assert.IsTrue(_after.activeSelf);
            Assert.AreEqual(position, _after.transform.localPosition);
            Assert.IsTrue(newDelivery.MoveNext());
            Assert.AreEqual(0, _flags.Writes);
            _presentation.GetType().GetMethod("CancelDelivery").Invoke(_presentation, null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 일과와_중단후_대기_대화는_공중_낙하속도를_보존한다()
        {
            var body = new GameObject("일과 공중 속도 검사");
            body.SetActive(false);
            body.transform.SetParent(_root.transform);
            var motorType = Type.GetType("KinematicCharacterController.KinematicCharacterMotor, KinematicCharacterController", true);
            var motor = body.AddComponent(motorType);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            motorType.GetField("_characterUp", fields).SetValue(motor, Vector3.up);
            var controllerType = Type.GetType("UPlayGround.MovementController.NpcMovementController, UPlayGround.Actor", true);
            var controller = body.AddComponent(controllerType);
            foreach (string name in new[] { "NpcRoutineState", "NpcIdleState", "NpcTalkState" })
            {
                var stateType = Type.GetType("UPlayGround.State." + name + ", UPlayGround.Actor", true);
                object state = Activator.CreateInstance(stateType, name == "NpcRoutineState"
                    ? new object[] { controller, null } : new object[] { controller });
                stateType.BaseType.BaseType.GetField("motor", fields).SetValue(state, motor);
                object[] args = { new Vector3(2f, -6f, 3f), 0.02f };
                stateType.GetMethod("UpdateVelocity").Invoke(state, args);
                Assert.AreEqual(new Vector3(0f, -6f, 0f), args[0], name);
                args[0] = (Vector3)args[0] + Vector3.down * 0.6f;
                stateType.GetMethod("UpdateVelocity").Invoke(state, args);
                Assert.AreEqual(new Vector3(0f, -6.6f, 0f), args[0], name);
            }
            yield return null;
        }

        private bool Complete() => (bool)_presentation.GetType().GetMethod("TryCompleteAction").Invoke(_presentation, new object[] { 0.6f });

        [UnityTest]
        public IEnumerator 작업점_경쟁과_비활성화는_실제_Unity_수명주기에서도_점유를_해제한다()
        {
            var point = CreateWorkPoint("test.lifecycle.point");
            object[] first = { _before, 0UL };
            Assert.IsTrue((bool)point.GetType().GetMethod("TryReserve").Invoke(point, first));
            object[] second = { _after, 0UL };
            Assert.IsFalse((bool)point.GetType().GetMethod("TryReserve").Invoke(point, second));
            point.gameObject.SetActive(false);
            point.gameObject.SetActive(true);
            Assert.IsFalse((bool)point.GetType().GetMethod("IsOwnedBy").Invoke(point, first));
            Assert.IsTrue((bool)point.GetType().GetMethod("TryReserve").Invoke(point, second));
            yield return null;
        }

        [UnityTest]
        public IEnumerator 중복_작업점은_예약을_거부하고_중복이_제거되면_재시도된다()
        {
            var first = CreateWorkPoint("test.lifecycle.duplicate");
            var second = CreateWorkPoint("test.lifecycle.duplicate");
            object[] args = { _before, 0UL };
            Assert.IsFalse((bool)first.GetType().GetMethod("TryReserve").Invoke(first, args));
            second.gameObject.SetActive(false);
            Assert.IsTrue((bool)first.GetType().GetMethod("TryReserve").Invoke(first, args));
            yield return null;
        }

        private Component CreateWorkPoint(string id)
        {
            var go = new GameObject("생활 작업점 검사");
            go.transform.SetParent(_root.transform);
            var point = go.AddComponent(Type.GetType("UPlayGround.Components.LivingWorld.NpcWorkPoint, UPlayGround.Actor", true));
            point.GetType().GetField("_mapId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(point, "test.lifecycle");
            point.GetType().GetField("_pointId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(point, id);
            return point;
        }

        [UnityTest]
        public IEnumerator 일과_비활성화는_예약과_주입한_임시_손소품을_같이_회수한다()
        {
            var go = new GameObject("일과 수명주기 검사");
            go.transform.SetParent(_root.transform);
            var type = Type.GetType("UPlayGround.Components.LivingWorld.NpcRoutine, UPlayGround.Actor", true);
            var routine = (Behaviour)go.AddComponent(type);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_mapId", fields).SetValue(routine, "test.lifecycle");
            var point = CreateWorkPoint("test.lifecycle.cleanup");
            object step = Activator.CreateInstance(type.GetNestedType("Step"));
            step.GetType().GetField("point").SetValue(step, point);
            Assert.IsTrue((bool)type.GetMethod("TryReserve", fields).Invoke(routine, new[] { step }));
            var prop = new GameObject("검사용 임시 손소품");
            prop.transform.SetParent(go.transform);
            type.GetField("_prop", fields).SetValue(routine, prop);
            routine.enabled = false;
            Assert.IsFalse(prop.activeSelf);
            object[] args = { _before, 0UL };
            Assert.IsTrue((bool)point.GetType().GetMethod("TryReserve").Invoke(point, args));
            type.GetMethod("CancelRoutine").Invoke(routine, null);
            yield return null;
            Assert.IsTrue(prop == null);
        }
        [UnityTest]
        public IEnumerator 운반_소품은_정지자세가_있어야_예약되고_작업교체와_취소에서_회수된다()
        {
            var go = new GameObject("운반 소품 검사");
            go.transform.SetParent(_root.transform);
            var type = Type.GetType("UPlayGround.Components.LivingWorld.NpcRoutine, UPlayGround.Actor", true);
            var routine = (Behaviour)go.AddComponent(type);
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_mapId", fields).SetValue(routine, "test.lifecycle");
            type.GetField("_propSocket", fields).SetValue(routine, go.transform);
            var point = CreateWorkPoint("test.lifecycle.carry");
            var step = Activator.CreateInstance(type.GetNestedType("Step"));
            step.GetType().GetField("point").SetValue(step, point);
            step.GetType().GetField("travelPropPrefab").SetValue(step, _before);
            var motionType = step.GetType().GetField("travelMotion").FieldType;
            var motion = ScriptableObject.CreateInstance(motionType);
            try
            {
                step.GetType().GetField("travelMotion").SetValue(step, motion);
                Assert.IsFalse((bool)type.GetMethod("TryReserve", fields).Invoke(routine, new[] { step }));
                Assert.IsFalse((bool)type.GetProperty("HasReservation").GetValue(routine));
                step.GetType().GetField("travelIdleMotion").SetValue(step, motion);
                Assert.IsTrue((bool)type.GetMethod("TryReserve", fields).Invoke(routine, new[] { step }));
                var carried = (GameObject)type.GetField("_prop", fields).GetValue(routine);
                Assert.AreEqual(go.transform, carried.transform.parent);
                Assert.AreEqual(motion, type.GetProperty("TravelIdleMotion").GetValue(routine));
                type.GetMethod("SetHeldProp", fields).Invoke(routine, new object[] { _after });
                var working = (GameObject)type.GetField("_prop", fields).GetValue(routine);
                Assert.IsFalse(carried.activeSelf);
                Assert.AreNotEqual(carried, working);
                type.GetMethod("SetRiskActive").Invoke(routine, new object[] { true });
                Assert.IsFalse(working.activeSelf);
                Assert.IsFalse((bool)type.GetProperty("HasReservation").GetValue(routine));
                yield return null;
                Assert.IsTrue(carried == null && working == null);
                Assert.AreEqual(0, _flags.Writes);
            }
            finally { UnityEngine.Object.Destroy(motion); }
        }

        private void Set(string field, object value) => _presentation.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_presentation, value);

        private sealed class Flags : IGlobalFlagService
        {
            private bool _value;
            public int Writes { get; private set; }
            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded;
            public bool GetFlag(string key) => key == "test.life.delivered" && _value;
            public void SetFlag(string key, bool value) { _value = value; Writes++; OnFlagChanged?.Invoke(key, value); }
            public void Restore(bool value) { _value = value; OnFlagsReloaded?.Invoke(); }
        }
    }
}
