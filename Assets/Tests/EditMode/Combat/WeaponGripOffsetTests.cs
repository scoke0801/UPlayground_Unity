using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Components;

namespace UPlayGround.Combat.Tests
{
    /// <summary>궁극기 파지 보정이 중단·중첩·모델 교체 뒤 일반 공격에 남지 않는지 검사한다.</summary>
    public sealed class WeaponGripOffsetTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerEquipment _equipment;
        private ParentConstraint _main;
        private ParentConstraint _sub;
        private static readonly Vector3 Baseline = new(.1f, .2f, .3f);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("파지 복구 검사");
            _root.SetActive(false);
            _equipment = _root.AddComponent<PlayerEquipment>();
            _main = CreateConstraint("주 무기");
            _sub = CreateConstraint("보조 무기");
            typeof(PlayerEquipment).GetField("_mainWeaponConstraint", PrivateInstance).SetValue(_equipment, _main);
            typeof(PlayerEquipment).GetField("_subWeaponConstraint", PrivateInstance).SetValue(_equipment, _sub);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

        [TestCase(true)]
        [TestCase(false)]
        public void 겹친보정을_어느순서로끝내도_마지막에원래파지를복구한다(bool oldestFirst)
        {
            int first = _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Main, Vector3.right);
            int second = _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Main, Vector3.up);
            _equipment.RestoreWeaponGripOffset(oldestFirst ? first : second);
            Assert.That(_main.GetTranslationOffset(0), Is.EqualTo(oldestFirst ? Vector3.up : Vector3.right));
            _equipment.RestoreWeaponGripOffset(oldestFirst ? second : first);
            Assert.That(_main.GetTranslationOffset(0), Is.EqualTo(Baseline));
        }

        [Test]
        public void 모델정리뒤_이전토큰은_새보정을해제하지않는다()
        {
            int old = _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Main, Vector3.right);
            _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Sub, Vector3.left);
            _equipment.RestoreHiddenWeaponSlots();
            Assert.That(_main.GetTranslationOffset(0), Is.EqualTo(Baseline));
            Assert.That(_sub.GetTranslationOffset(0), Is.EqualTo(Baseline));
            int current = _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Main, Vector3.up);
            _equipment.RestoreWeaponGripOffset(old);
            Assert.That(_main.GetTranslationOffset(0), Is.EqualTo(Vector3.up));
            _equipment.RestoreWeaponGripOffset(current);
            Assert.That(_main.GetTranslationOffset(0), Is.EqualTo(Baseline));
        }

        [Test]
        public void 사망정리는_양쪽파지를복구한다()
        {
            _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Main, Vector3.right);
            _equipment.OverrideWeaponGripOffset(WeaponVisibilitySlot.Sub, Vector3.left);
            typeof(PlayerEquipment).GetMethod("OnSignatureOwnerHealthChanged", PrivateInstance).Invoke(_equipment, new object[] { 0f, 100f });
            Assert.That(_main.GetTranslationOffset(0), Is.EqualTo(Baseline));
            Assert.That(_sub.GetTranslationOffset(0), Is.EqualTo(Baseline));
        }

        private ParentConstraint CreateConstraint(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(_root.transform, false);
            var constraint = child.AddComponent<ParentConstraint>();
            constraint.AddSource(new ConstraintSource { sourceTransform = _root.transform, weight = 1 });
            constraint.SetTranslationOffset(0, Baseline);
            return constraint;
        }
    }
}
