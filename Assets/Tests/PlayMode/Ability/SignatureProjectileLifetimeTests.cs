using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Projectile;
using UPlayGround.Data.EnumType;
using UPlayGround.Combat;
using UPlayGround.Manager;

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>시전자 사망 회수와 교대에 쓰는 비활성 모델의 독립 수명을 구분한다.</summary>
    public sealed class SignatureProjectileLifetimeTests
    {
        private IActorObjectService _previousObjects;
        private TestActorObjectService _objects;

        [SetUp]
        public void SetUp()
        {
            Services.TryGet(out _previousObjects);
            if (_previousObjects != null) Services.Unregister(_previousObjects);
            _objects = new TestActorObjectService();
            Services.Register(_objects);
        }

        [TearDown]
        public void TearDown()
        {
            Services.Unregister(_objects);
            if (_previousObjects != null) Services.Register(_previousObjects);
        }

        [Test]
        public void 착탄예고는_목표지점에남고_사망회수와_풀재사용에서_중복되지않는다()
        {
            var owner = new GameObject("예고 시전자");
            var shot = new GameObject("예고 투사체");
            var markerPrefab = new GameObject("착탄 예고 원본");
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            GameObject first = null, second = null;
            try
            {
                var actor = owner.AddComponent<SignatureProjectileTestOwner>();
                var runtime = shot.AddComponent<ProjectileRuntime>();
                definition.landingTelegraphPrefab = markerPrefab;
                definition.returnOnOwnerDeath = true;
                definition.motion = new ArcProjectileMotion { fixedFlightTime = .9f, flightTimeMode = ProjectileArcFlightTimeMode.Fixed };
                var request = new ProjectileSpawnRequest
                {
                    owner = owner, definition = definition, origin = Vector3.up,
                    hasTargetPosition = true, targetPosition = new Vector3(0, 0, 6)
                };
                var markerField = typeof(ProjectileRuntime).GetField("_landingTelegraph", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                runtime.Initialize(definition, request, new AttackData(), (p, _) => p.OnReturnedToPool(), (_, _) => { });
                first = (GameObject)markerField.GetValue(runtime);
                Assert.That(first, Is.Not.Null);
                Assert.That(first.transform.parent, Is.Null);
                Assert.That(first.transform.position, Is.EqualTo(request.targetPosition));
                actor.Alive = false;
                runtime.Tick(.1f);
                Assert.That(markerField.GetValue(runtime), Is.Null);
                Assert.That(first.activeSelf, Is.False);
                actor.Alive = true;
                runtime.Initialize(definition, request, new AttackData(), (p, _) => p.OnReturnedToPool(), (_, _) => { });
                second = (GameObject)markerField.GetValue(runtime);
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(second.activeSelf, Is.True);
                runtime.ForceReturn();
                Assert.That(markerField.GetValue(runtime), Is.Null);
                Assert.That(second.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(markerPrefab);
                if (first != null) UnityEngine.Object.DestroyImmediate(first);
                if (second != null) UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void 지점투척은_비행접촉피해없이_착탄에서만_한번_맞힌다()
        {
            var owner = new GameObject("시전자");
            var target = new GameObject("비행 경로와 착탄 지점의 적") { layer = 29 };
            var shot = new GameObject("투척");
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            try
            {
                Vector3 center = new(10000, 15000, 10000);
                owner.AddComponent<SignatureProjectileTestOwner>().SetActorType(ActorType.Player);
                var victim = target.AddComponent<SignatureProjectileTestOwner>();
                victim.SetActorType(ActorType.Monster);
                target.transform.position = center;
                var collider = target.AddComponent<BoxCollider>();
                collider.center = Vector3.up; collider.size = new Vector3(1, 4, 1);
                Physics.SyncTransforms();
                definition.motion = new ArcProjectileMotion { arcHeight = 0, flightTimeMode = ProjectileArcFlightTimeMode.Fixed, fixedFlightTime = .3f };
                definition.inheritOwnerTimeScale = false; definition.ignoreFlightContacts = true;
                definition.destroyOnHit = false; definition.lifetime = .3f;
                definition.behaviors.Add(new DetonateProjectileBehavior { radius = 3.5f, onHit = false, onExpire = true });
                var runtime = shot.AddComponent<ProjectileRuntime>();
                runtime.Initialize(definition, new ProjectileSpawnRequest { owner = owner, definition = definition,
                        origin = center + Vector3.up * 2, hasTargetPosition = true, targetPosition = center, hitLayers = 1 << 29 },
                    new AttackData(), (p, _) => p.OnReturnedToPool(), (_, _) => { });
                runtime.Tick(.1f); runtime.Tick(.1f);
                Assert.That(victim.HitCount, Is.Zero);
                runtime.Tick(.11f);
                Assert.That(victim.HitCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void 화살비는_수명경계에서도_대상당6회를넘지않고_재대여하면_횟수가초기화된다()
        {
            var owner = new GameObject("시전자");
            var target = new GameObject("피격 대상") { layer = 29 };
            var shot = new GameObject("화살비");
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            try
            {
                Vector3 center = new(10000, 15000, 10000);
                owner.AddComponent<SignatureProjectileTestOwner>().SetActorType(ActorType.Player);
                var victim = target.AddComponent<SignatureProjectileTestOwner>();
                victim.SetActorType(ActorType.Monster);
                target.transform.position = center;
                target.AddComponent<SphereCollider>();
                Physics.SyncTransforms();
                definition.motion = new StationaryProjectileMotion();
                definition.inheritOwnerTimeScale = false;
                definition.lifetime = 3;
                definition.behaviors.Add(new AreaTickProjectileBehavior { radius = 4, interval = .3f,
                    activationDelay = .6f, maxHitsPerTarget = 6, damageFalloff = AnimationCurve.Constant(0, 1, 1) });
                var runtime = shot.AddComponent<ProjectileRuntime>();
                var request = new ProjectileSpawnRequest { owner = owner, definition = definition,
                    origin = center, hitLayers = 1 << 29 };
                for (int reuse = 0; reuse < 2; reuse++)
                {
                    runtime.Initialize(definition, request, new AttackData(), (p, _) => p.OnReturnedToPool(), (_, _) => { });
                    for (int tick = 0; tick < 28; tick++) runtime.Tick(.1f);
                    Assert.That(victim.HitCount, Is.EqualTo((reuse + 1) * 6));
                    runtime.ForceReturn();
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 사망회수정책은_선택한탄에만_적용하고_만료폭발을_만들지않는다(bool returnOnDeath)
        {
            var owner = new GameObject("시전자");
            var shot = new GameObject("장판");
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            try
            {
                var actor = owner.AddComponent<SignatureProjectileTestOwner>();
                definition.motion = new StationaryProjectileMotion();
                definition.returnOnOwnerDeath = returnOnDeath;
                definition.inheritOwnerTimeScale = false;
                definition.lifetime = 2f;
                int returns = 0, children = 0;
                var runtime = shot.AddComponent<ProjectileRuntime>();
                runtime.Initialize(definition, new ProjectileSpawnRequest { owner = owner, definition = definition },
                    new AttackData(), (_, expired) => { Assert.That(expired, Is.False); returns++; }, (_, _) => children++);
                actor.Alive = false;
                runtime.Tick(.1f);
                Assert.That(returns, Is.EqualTo(returnOnDeath ? 1 : 0));
                Assert.That(children, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void 모델비활성은_살아있는시전자탄을_회수하지않는다()
        {
            var owner = new GameObject("교대 전 모델");
            var shot = new GameObject("장판");
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            try
            {
                owner.AddComponent<SignatureProjectileTestOwner>();
                definition.motion = new StationaryProjectileMotion();
                definition.returnOnOwnerDeath = true;
                definition.inheritOwnerTimeScale = false;
                definition.lifetime = 2f;
                int returns = 0;
                var runtime = shot.AddComponent<ProjectileRuntime>();
                runtime.Initialize(definition, new ProjectileSpawnRequest { owner = owner, definition = definition },
                    new AttackData(), (_, _) => returns++, (_, _) => { });
                owner.SetActive(false);
                runtime.Tick(.1f);
                Assert.That(returns, Is.Zero);
                Assert.That(runtime.IsActive, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }

    public sealed class SignatureProjectileTestOwner : GameActor, IDamageable
    {
        public bool Alive = true;
        public int HitCount;
        public void SetActorType(ActorType actorType) => _actorType = actorType;
        public bool IsAlive() => Alive;
        public bool CanTakeDamage() => Alive;
        public CombatResult ReceiveHit(in HitRequest request) { HitCount++; return default; }
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => Alive ? 1 : 0;
        public float GetCurrentHealth() => Alive ? 100 : 0;
        public void ApplyHealingEffect(float healAmount) { }
    }
}
