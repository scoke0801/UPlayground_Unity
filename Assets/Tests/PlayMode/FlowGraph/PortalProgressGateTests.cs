using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.PlayModeTests
{
    /// <summary>진행 조건 플래그가 있는 출구는 플래그가 켜진 뒤에만 보이고 이용할 수 있다.</summary>
    public sealed class PortalProgressGateTests
    {
        private const string RequiredFlag = "test.portal.ending";

        private GameObject _portalObject;
        private GameObject _sign;
        private Component _portal;
        private Flags _flags;
        private IGlobalFlagService _previousFlags;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Services.TryGet(out _previousFlags);
            Services.Unregister(_previousFlags);
            _flags = new Flags();
            Services.Register(_flags);

            _portalObject = new GameObject("진행 조건 출구");
            _portalObject.SetActive(false);
            _portalObject.AddComponent<SphereCollider>();
            _portal = _portalObject.AddComponent(Type.GetType("UPlayGround.PortalActor, UPlayGround.Actor", true));
            _sign = new GameObject("길 표식");
            _sign.transform.SetParent(_portalObject.transform);

            Set("_travelOnInteract", true);
            Set("_targetSceneName", "TestDestination");
            Set("_requiresActivation", false);
            Set("_startsActivated", true);
            Set("_requiredFlagKey", RequiredFlag);
            Set("_activeVisuals", new[] { _sign });

            _portalObject.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(_portalObject);
            yield return null;
            Services.Unregister(_flags);
            Services.Register(_previousFlags);
        }

        [UnityTest]
        public IEnumerator 조건_플래그가_켜지기_전에는_표식을_숨기고_이용을_막는다()
        {
            Assert.IsFalse(_sign.activeSelf, "결말 전에는 갈 수 없는 길을 안내하지 않는다");
            Assert.IsFalse(CanInteract());
            yield break;
        }

        [UnityTest]
        public IEnumerator 조건_플래그가_켜지면_즉시_표식을_보이고_이용을_허용한다()
        {
            _flags.SetFlag(RequiredFlag, true);
            yield return null;

            Assert.IsTrue(_sign.activeSelf);
            Assert.IsTrue(CanInteract());
        }

        [UnityTest]
        public IEnumerator 결말_전_저장을_불러오면_다시_닫힌다()
        {
            _flags.SetFlag(RequiredFlag, true);
            yield return null;

            _flags.Restore(false);
            yield return null;

            Assert.IsFalse(_sign.activeSelf);
            Assert.IsFalse(CanInteract());
        }

        private bool CanInteract() =>
            (bool)_portal.GetType().GetMethod("CanInteract", BindingFlags.Instance | BindingFlags.Public).Invoke(_portal, null);

        private void Set(string field, object value) =>
            _portal.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_portal, value);

        private sealed class Flags : IGlobalFlagService
        {
            private bool _value;
            public event Action<string, bool> OnFlagChanged;
            public event Action OnFlagsReloaded;
            public bool GetFlag(string key) => key == RequiredFlag && _value;
            public void SetFlag(string key, bool value) { _value = value; OnFlagChanged?.Invoke(key, value); }
            public void Restore(bool value) { _value = value; OnFlagsReloaded?.Invoke(); }
        }
    }
}
