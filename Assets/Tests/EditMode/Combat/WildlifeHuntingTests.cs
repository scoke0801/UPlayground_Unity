using System;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
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

        [TestCase("APD_Pig")]
        [TestCase("APD_Rabbit")]
        [TestCase("APD_RabbitWhite")]
        public void 작은_동물의_몸_충돌과_피격_영역은_독립적이다(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Environment/Wildlife/" + prefabName + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var hurtbox = prefab.GetComponent<BoxCollider>();
            var body = prefab.transform.Find("BodyCollider")?.GetComponent<BoxCollider>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.isTrigger, Is.False);
            Assert.That(hurtbox.isTrigger, Is.True);
            Assert.That(prefab.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")));
            Assert.That(body.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Default")));
            Assert.That(hurtbox.size.y, Is.GreaterThanOrEqualTo(1.25f));
            Assert.That(hurtbox.size.y, Is.GreaterThan(body.size.y));
            Assert.That(hurtbox.center.y - hurtbox.size.y * 0.5f,
                Is.EqualTo(body.center.y - body.size.y * 0.5f).Within(0.001f));
            var type = _target.GetType();
            var creature = prefab.GetComponent(type);
            Assert.That(type.GetField("_bodyCollider", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(creature), Is.EqualTo(body));
        }

        [Test]
        public void 몸보다_높은_공격은_피격되고_이동은_확대된_피격_영역에_막히지_않는다()
        {
            var hurtbox = _root.GetComponent<BoxCollider>();
            hurtbox.size = new Vector3(0.65f, 1.25f, 0.65f);
            hurtbox.center = Vector3.up * 0.625f;
            var body = AddBody(new Vector3(0.3f, 0.4f, 0.6f));
            Physics.SyncTransforms();
            var shape = new ResolvedCollisionShape
            {
                IsValid = true, ShapeType = CollisionShapeType.Sphere,
                SnapshotPosition = _root.transform.position + Vector3.up,
                SnapshotRotation = Quaternion.identity, LocalRotation = Quaternion.identity, Radius = 0.1f
            };
            var hits = new List<CombatHit>();
            Assert.That(CombatHitDetector.DetectExplicitHits(null, shape, LayerMask.GetMask("Enemy"),
                new Collider[8], null, new HashSet<IDamageable>(), hits), Is.EqualTo(1));
            Assert.That(hits[0].Damageable, Is.SameAs(_target));
            Assert.That(Physics.SphereCast(_root.transform.position + Vector3.up + Vector3.back * 2,
                0.1f, Vector3.forward, out _, 4, ~0, QueryTriggerInteraction.Ignore), Is.False);
            Assert.That(Physics.SphereCast(body.bounds.center + Vector3.back * 2,
                0.1f, Vector3.forward, out var block, 4, ~0, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(block.collider, Is.EqualTo(body));
            Hit(100);
            Assert.That(body.enabled, Is.False, "사망 후 몸 충돌도 해제해야 한다.");
        }

        [Test]
        public void 몸_충돌체가_피격_영역보다_커도_공격_범위를_넓히거나_자기_시야를_가리지_않는다()
        {
            AddBody(Vector3.one * 3);
            _owner = new GameObject("분리된 몸을 바라보는 플레이어");
            _owner.transform.position = _root.transform.position + Vector3.back * 3;
            _owner.AddComponent<DetectionTestActor>().Configure(ActorType.Player);
            Physics.SyncTransforms();
            var targeting = new PlayerTargetingController(_owner.transform);
            Assert.That(targeting.CanSelectTarget(_root.transform, 4, 1, LayerMask.GetMask("Default")), Is.True);
            var shape = new ResolvedCollisionShape
            {
                IsValid = true, ShapeType = CollisionShapeType.Sphere,
                SnapshotPosition = _root.transform.position + Vector3.right,
                SnapshotRotation = Quaternion.identity, LocalRotation = Quaternion.identity, Radius = 0.1f
            };
            Assert.That(CombatHitDetector.DetectExplicitHits(null, shape, LayerMask.GetMask("Enemy"),
                new Collider[8], null, new HashSet<IDamageable>(), new List<CombatHit>()), Is.Zero);
            var wall = new GameObject("시야를 가리는 벽", typeof(BoxCollider));
            try
            {
                wall.transform.position = _root.transform.position + Vector3.back * 2.2f;
                Physics.SyncTransforms();
                Assert.That(targeting.CanSelectTarget(_root.transform, 4, 1, LayerMask.GetMask("Default")), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(wall); }
        }

        private BoxCollider AddBody(Vector3 size)
        {
            var child = new GameObject("BodyCollider");
            child.transform.SetParent(_root.transform, false);
            var collider = child.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = Vector3.up * (size.y * 0.5f);
            return collider;
        }

        private CombatResult Hit(float damage)
        {
            var attack = new AttackData { damage = damage, hitTarget = _root, attackDirection = Vector3.forward };
            return _target.ReceiveHit(HitRequest.FromAttackData(attack));
        }
    }
}
