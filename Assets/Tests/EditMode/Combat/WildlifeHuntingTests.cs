using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.World;

namespace UPlayGround.Combat.Tests
{
    /// <summary>환경 생물의 사망 중복 차단과 액터가 아닌 공격 보정 대상을 검사한다.</summary>
    public sealed class WildlifeHuntingTests
    {
        private GameObject _root, _owner;
        private WildlifeProfileSO _profile;
        private IDamageable _target;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("사냥 대상 검사");
            _root.transform.position = Vector3.one * 22000;
            _root.layer = LayerMask.NameToLayer("Enemy");
            _root.AddComponent<BoxCollider>().isTrigger = true;
            var type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
            var creature = _root.AddComponent(type);
            _profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            _profile.MaxHealth = 10;
            type.GetField("_profile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(creature, _profile);
            _target = (IDamageable)creature;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            if (_owner != null) UnityEngine.Object.DestroyImmediate(_owner);
            UnityEngine.Object.DestroyImmediate(_profile);
        }

        [Test]
        public void 장식_생물은_피격과_회복으로_사냥_대상이_되지_않는다()
        {
            _profile.MaxHealth = 0;
            _target.ApplyHealingEffect(10);
            Assert.That(_target.CanTakeDamage(), Is.False);
            Assert.That(Hit(20).DamageApplied, Is.False);
            Assert.That(_target.GetCurrentHealth(), Is.Zero);
        }

        [Test]
        public void 피해와_회복이_최대체력_안에서_적용된다()
        {
            Assert.That(Hit(4).Resources.HpDelta, Is.EqualTo(-4));
            Assert.That(_target.GetCurrentHealth(), Is.EqualTo(6));
            _target.ApplyHealingEffect(100);
            Assert.That(_target.GetCurrentHealth(), Is.EqualTo(10));
            Assert.That(_target.GetHealthPercent(), Is.EqualTo(1));
        }

        [Test]
        public void 치명타_처치_이후_재피격과_부활을_차단한다()
        {
            var result = Hit(100);
            Assert.That(result.Resources.HpDelta, Is.EqualTo(-10));
            Assert.That(_target.IsAlive(), Is.False);
            Assert.That(_root.GetComponent<Collider>().enabled, Is.False);
            Assert.That(Hit(100).DamageApplied, Is.False);
            _target.ApplyHealingEffect(100);
            Assert.That(_target.GetCurrentHealth(), Is.Zero);
        }

        [Test]
        public void 액터가_아닌_동물도_공격_보정과_명시적_대상_검사를_통과한다()
        {
            _owner = new GameObject("사냥 플레이어");
            _owner.transform.position = _root.transform.position - Vector3.forward * 2;
            _owner.AddComponent<DetectionTestActor>().Configure(ActorType.Player);
            Physics.SyncTransforms();
            var targeting = new PlayerTargetingController(_owner.transform);
            Assert.That(targeting.FindAttackTarget(3, 60, Vector3.forward, LayerMask.GetMask("Enemy"), 0),
                Is.EqualTo(_root.transform));
            Assert.That(targeting.CanSelectTarget(_root.transform, 3, 1, 0), Is.True);
            Hit(100);
            Assert.That(targeting.CanSelectTarget(_root.transform, 3, 1, 0), Is.False);
        }

        private CombatResult Hit(float damage)
        {
            var attack = new AttackData { damage = damage, hitTarget = _root, attackDirection = Vector3.forward };
            return _target.ReceiveHit(HitRequest.FromAttackData(attack));
        }
    }
}
