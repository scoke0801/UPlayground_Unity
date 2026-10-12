using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

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
    }

    /// <summary>감지 후보 순서 회귀 검사의 살아 있는 전투 액터.</summary>
    public sealed class DetectionTestActor : GameActor, IDamageable
    {
        public void Configure(ActorType type) => _actorType = type;
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
