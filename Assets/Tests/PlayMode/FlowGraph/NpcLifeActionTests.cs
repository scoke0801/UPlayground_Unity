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

        private bool Complete() => (bool)_presentation.GetType().GetMethod("TryCompleteAction").Invoke(_presentation, new object[] { 0.6f });
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
