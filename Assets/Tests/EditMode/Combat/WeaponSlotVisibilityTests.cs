using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Combat.Tests
{
    /// <summary>무기 투척 도중 중단·중첩·모델 교체 시 외형이 유실되지 않는지 검사한다.</summary>
    public sealed class WeaponSlotVisibilityTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerEquipment _equipment;
        private Renderer _main;
        private Renderer _sub;
        private Renderer _originallyHidden;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("WeaponSlotVisibilityTests");
            _root.SetActive(false);
            _equipment = _root.AddComponent<PlayerEquipment>();
            _main = CreateRenderer(_root.transform, "Main");
            _sub = CreateRenderer(_root.transform, "Sub");
            _originallyHidden = CreateRenderer(_main.transform, "OriginallyHidden");
            _originallyHidden.enabled = false;
            SetField("_currentMainWeaponObj", _main.gameObject);
            SetField("_currentSubWeaponObj", _sub.gameObject);
            Invoke("CacheSignatureWeaponRenderers");
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

        [Test]
        public void OverlappingRequestsRestoreOnlyAfterLastTokenAndPreserveDisabledRenderers()
        {
            int first = _equipment.HideWeaponSlot(WeaponVisibilitySlot.Main);
            int second = _equipment.HideWeaponSlot(WeaponVisibilitySlot.Main);
            Assert.IsFalse(_main.enabled);
            _equipment.RestoreWeaponSlot(first);
            Assert.IsFalse(_main.enabled);
            _equipment.RestoreWeaponSlot(second);
            Assert.IsTrue(_main.enabled);
            Assert.IsFalse(_originallyHidden.enabled);
        }

        [Test]
        public void MainAndSubRequestsAreIndependent()
        {
            int main = _equipment.HideWeaponSlot(WeaponVisibilitySlot.Main);
            int sub = _equipment.HideWeaponSlot(WeaponVisibilitySlot.Sub);
            _equipment.RestoreWeaponSlot(main);
            Assert.IsTrue(_main.enabled);
            Assert.IsFalse(_sub.enabled);
            _equipment.RestoreWeaponSlot(sub);
            Assert.IsTrue(_sub.enabled);
        }

        [Test]
        public void ResetInvalidatesOldTokenWithoutReleasingNewRequest()
        {
            int oldToken = _equipment.HideWeaponSlot(WeaponVisibilitySlot.Main);
            _equipment.RestoreHiddenWeaponSlots();
            int newToken = _equipment.HideWeaponSlot(WeaponVisibilitySlot.Main);
            _equipment.RestoreWeaponSlot(oldToken);
            Assert.IsFalse(_main.enabled);
            _equipment.RestoreWeaponSlot(newToken);
            Assert.IsTrue(_main.enabled);
        }

        [Test]
        public void ModelCleanupRestoresBothSlotsWithoutChangingTheirActiveState()
        {
            _equipment.HideWeaponSlot(WeaponVisibilitySlot.Main);
            _equipment.HideWeaponSlot(WeaponVisibilitySlot.Sub);
            _equipment.ResetInteractionEquipmentImmediate();
            Assert.IsTrue(_main.enabled);
            Assert.IsTrue(_sub.enabled);
            Assert.IsTrue(_main.gameObject.activeSelf);
            Assert.IsFalse(_originallyHidden.enabled);
        }

        [Test]
        public void OwnerDeathRestoresHiddenWeapon()
        {
            _equipment.HideWeaponSlot(WeaponVisibilitySlot.Sub);
            typeof(PlayerEquipment).GetMethod("OnSignatureOwnerHealthChanged", PrivateInstance)
                .Invoke(_equipment, new object[] { 0f, 100f });
            Assert.IsTrue(_sub.enabled);
        }

        private void SetField(string name, object value)
            => typeof(PlayerEquipment).GetField(name, PrivateInstance).SetValue(_equipment, value);

        private void Invoke(string name)
            => typeof(PlayerEquipment).GetMethod(name, PrivateInstance).Invoke(_equipment, null);

        private static Renderer CreateRenderer(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<MeshRenderer>();
        }
    }
}
