using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Animation;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Combat.Tests
{
    public sealed class EnemyDetectionTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects) UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
            Services.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 자기자신과_아군_후보가_있어도_플레이어를_찾는다(bool createPlayerFirst)
        {
            Services.Clear();
            Vector3 center = new(20000, 20000, 20000);
            DetectionTestActor player = createPlayerFirst ? CreateActor(ActorType.Player, center + Vector3.forward * 3) : null;
            DetectionTestActor owner = CreateActor(ActorType.Monster, center);
            CreateActor(ActorType.Monster, center + Vector3.forward);
            if (player == null) player = CreateActor(ActorType.Player, center + Vector3.forward * 3);
            EnemyDetection detection = owner.gameObject.AddComponent<EnemyDetection>();
            typeof(EnemyDetection).GetField("_fieldOfView", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(detection, 360f);
            Physics.SyncTransforms();

            MethodInfo detect = typeof(EnemyDetection).GetMethod("DetectNewTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(detect, Is.Not.Null);
            detect.Invoke(detection, null);

            Assert.That(detection.CurrentTarget, Is.EqualTo(player.transform));
        }

        private DetectionTestActor CreateActor(ActorType type, Vector3 position)
        {
            var item = new GameObject(type.ToString());
            _objects.Add(item);
            item.layer = LayerMask.NameToLayer(type == ActorType.Player ? "Player" : "Enemy");
            item.transform.position = position;
            item.AddComponent<CapsuleCollider>();
            DetectionTestActor actor = item.AddComponent<DetectionTestActor>();
            actor.Configure(type);
            return actor;
        }

        [Test]
        public void 공격_어시스트는_입력방향_범위밖의_가까운_적을_제외한다()
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward);
            var intended = CreateActor(ActorType.Monster, owner.transform.position + Vector3.right * 4);
            Physics.SyncTransforms();
            var targeting = new PlayerTargetingController(owner.transform);
            Assert.That(targeting.FindAttackTarget(4.5f, 50, Vector3.right, LayerMask.GetMask("Enemy"), 0),
                Is.EqualTo(intended.transform));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 정면의_먼적보다_허용각도_안의_가까운_적을_선택한다(bool createNearFirst)
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            Vector3 nearPosition = owner.transform.position + new Vector3(0.6f, 0, 1);
            var near = createNearFirst ? CreateActor(ActorType.Monster, nearPosition) : null;
            CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward * 2.3f);
            if (near == null) near = CreateActor(ActorType.Monster, nearPosition);
            Physics.SyncTransforms();
            Assert.That(new PlayerTargetingController(owner.transform).FindAttackTarget(
                2.5f, 50, Vector3.forward, LayerMask.GetMask("Enemy"), 0), Is.EqualTo(near.transform));
        }

        [Test]
        public void 큰_콜라이더가_탐색구에_걸쳐도_거리밖의_액터는_선정하지_않는다()
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            var far = CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward * 3);
            far.GetComponent<CapsuleCollider>().radius = 1f;
            Physics.SyncTransforms();
            Assert.That(new PlayerTargetingController(owner.transform).FindAttackTarget(
                2.5f, 50, Vector3.forward, LayerMask.GetMask("Enemy"), 0), Is.Null);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 이동_프로필이_없어도_공통_세션의_저작된_회전은_동작한다(int frameRate)
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            var target = CreateActor(ActorType.Monster, owner.transform.position + Vector3.right);
            var warp = owner.gameObject.AddComponent<MotionWarpController>();
            warp.BeginTargetLock();
            warp.SetTarget(target.transform);
            var settings = MotionWarpWindowSettings.Default(0.25f);
            settings.overrideDistance = true;
            settings.maxDistance = 2.5f;
            settings.maxWarpAngle = 30f;
            warp.BeginWarpWindow(settings);
            Assert.That(warp.IsApplicable, Is.False);
            Quaternion rotation = Quaternion.identity;
            for (int frame = 1; frame <= frameRate; frame++)
                warp.TryEvaluateRotation(rotation, owner.transform.position, true,
                    0.25f * (1f - frame / (float)frameRate), 0.25f, 0f, 2.5f, 18f, out rotation);
            Assert.That(Quaternion.Angle(Quaternion.identity, rotation), Is.EqualTo(30f).Within(0.1f));
            warp.CommitTargetTracking();
            target.transform.position = owner.transform.position - Vector3.right;
            Assert.That(warp.TryEvaluateRotation(rotation, owner.transform.position, true,
                0f, 0.25f, 0f, 2.5f, 18f, out _), Is.False);
        }

        [Test]
        public void 빈_대상과_비활성_대상은_같은_공격에서_재선정하지_않는다()
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            var target = CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward);
            var warp = owner.gameObject.AddComponent<MotionWarpController>();
            warp.BeginTargetLock();
            warp.SetTarget((Transform)null);
            warp.SetTarget(target.transform);
            Assert.That(warp.HasTarget, Is.False);
            warp.BeginTargetLock();
            warp.SetTarget(target.transform);
            target.gameObject.SetActive(false);
            warp.SetTarget(owner.transform);
            Assert.That(warp.ActiveTarget.anchor, Is.EqualTo(target.transform));
        }

        [Test]
        public void 높이가_다른_적과_먼_락온은_공격_선정에서_거절된다()
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            var target = CreateActor(ActorType.Monster, owner.transform.position + new Vector3(0, 1.2f, 1));
            var targeting = new PlayerTargetingController(owner.transform);
            Physics.SyncTransforms();
            Assert.That(targeting.FindAttackTarget(2.5f, 50f, Vector3.forward,
                LayerMask.GetMask("Enemy"), 0, 0.6f), Is.Null);
            Assert.That(targeting.CanSelectTarget(target.transform, 2.5f, 0.6f, 0), Is.False);
            target.transform.position = owner.transform.position + Vector3.forward * 7;
            Assert.That(targeting.CanSelectTarget(target.transform, 2.5f, 0.6f, 0), Is.False);
        }

        [TestCase(0.7f)]
        [TestCase(4f)]
        public void 무입력_공격은_정면_적을_유지하고_가까워도_대상을_비우지_않는다(float distance)
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            CreateActor(ActorType.Monster, owner.transform.position - Vector3.forward * 0.5f);
            var intended = CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward * distance);
            Physics.SyncTransforms();
            Assert.That(new PlayerTargetingController(owner.transform).FindAttackTarget(
                5, 80, Vector3.zero, LayerMask.GetMask("Enemy"), 0), Is.EqualTo(intended.transform));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 공격_어시스트는_벽을_제외하되_트리거는_시야를_막지_않는다(bool isTrigger)
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            var target = CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward * 4);
            var wall = new GameObject("시야 차단 벽");
            _objects.Add(wall);
            wall.layer = LayerMask.NameToLayer("Ground");
            wall.transform.position = owner.transform.position + Vector3.forward * 2;
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(3, 4, 0.2f);
            collider.isTrigger = isTrigger;
            Physics.SyncTransforms();
            Assert.That(new PlayerTargetingController(owner.transform).FindAttackTarget(
                5, 80, Vector3.forward, LayerMask.GetMask("Enemy"), LayerMask.GetMask("Ground")),
                Is.EqualTo(isTrigger ? target.transform : null));
        }

        [Test]
        public void 공격_어시스트는_같은_방향이면_가까운_적을_선택하고_아군은_제외한다()
        {
            var owner = CreateActor(ActorType.Player, Vector3.one * 20000);
            CreateActor(ActorType.Player, owner.transform.position + Vector3.forward);
            CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward * 4);
            var near = CreateActor(ActorType.Monster, owner.transform.position + Vector3.forward * 2);
            Physics.SyncTransforms();
            Assert.That(new PlayerTargetingController(owner.transform).FindAttackTarget(
                5, 80, Vector3.forward, ~0, 0), Is.EqualTo(near.transform));
        }
    }

    /// <summary>감지 후보 순서 회귀 검사의 살아 있는 전투 액터.</summary>
    public sealed class DetectionTestActor : GameActor, IDamageable
    {
        public void Configure(ActorType type) => _actorType = type;
        public void SetAnimator(ActorAnimator animator) => _animator = animator;
        public CombatResult ReceiveHit(in HitRequest request) => default;
        public bool IsAlive() => true;
        public bool CanTakeDamage() => true;
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => 1;
        public float GetCurrentHealth() => 100;
        public void ApplyHealingEffect(float healAmount) { }
    }
}
