using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Projectile;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>제작한 실제 VFX와 투사체 정의의 명중·표시·풀 반환을 플레이 모드에서 검증한다.</summary>
    public sealed class StaffMagicProjectilePlayModeTests
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator 실제_반사탄의_표시와_음향설정이_초기화와_재대여에서_유지된다()
        {
            foreach (string path in new[] {
                "Assets/10.Datas/Projectile/Parry/Projectile_EnemyArrow_Parry.asset",
                "Assets/10.Datas/Ability/Actor/P09Staff/Projectile_P09Staff_Bolt_Parry.asset" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
                Assert.That(definition, Is.Not.Null);
                var visual = Object.Instantiate(definition.visualPrefab);
                foreach (var shell in visual.GetComponentsInChildren<BaseProjectile>(true)) shell.enabled = false;
                var runtime = visual.AddComponent<ProjectileRuntime>();
                try
                {
                    int cueId = 0;
                    for (int reuse = 0; reuse < 2; reuse++)
                    {
                        runtime.Initialize(definition, new ProjectileSpawnRequest { origin = new Vector3(10000f, 14000f, 10000f),
                            direction = Vector3.forward }, new AttackData { damage = 9f }, (p, e) => p.OnReturnedToPool(), null);
                        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                        var cue = (GameObject)typeof(ProjectileRuntime).GetField("_flightCue", flags).GetValue(runtime);
                        var returnCue = (GameObject)typeof(ProjectileRuntime).GetField("_returnCue", flags).GetValue(runtime);
                        var sound = (AudioSource)typeof(ProjectileRuntime).GetField("_launchCueSource", flags).GetValue(runtime);
                        Assert.That(cue.activeInHierarchy, Is.True, path);
                        Assert.That(returnCue.activeSelf, Is.False, path);
                        Assert.That(cue.GetComponentsInChildren<TrailRenderer>().Length, Is.EqualTo(2));
                        Assert.That(sound.volume, Is.EqualTo(definition.GetBehavior<ReflectableProjectileBehavior>().launchCueVolume));
                        if (reuse == 0) cueId = cue.GetInstanceID();
                        else Assert.That(cue.GetInstanceID(), Is.EqualTo(cueId), "풀 재대여마다 표시를 생성하면 안 됩니다.");
                        runtime.ForceReturn();
                        Assert.That(runtime.IsReset, Is.True);
                    }
                }
                finally { Object.Destroy(visual); }
                yield return null;
            }
        }

#endif

        [UnityTest]
        public IEnumerator 방어적용은_탄확정_뒤_한번만_실행하고_공간과마스크_실패는_흡수한다()
        {
            for (int failure = 0; failure < 3; failure++)
            {
                var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
                definition.behaviors.Add(new ReflectableProjectileBehavior());
                definition.detachTrailOnReturn = false;
                definition.obstructionLayers = 1 << 29;
                var go = new GameObject("확정 검사 탄");
                var reflectorObject = new GameObject("확정 검사 반사자");
                var targetObject = new GameObject("확정 검사 대상");
                var wall = new GameObject("확정 검사 벽") { layer = 29 };
                Vector3 center = new(10000f, 13000f, 10000f);
                wall.transform.position = failure == 1 ? center : center + Vector3.up * 10f;
                wall.AddComponent<BoxCollider>();
                var runtime = go.AddComponent<ProjectileRuntime>();
                var reflector = reflectorObject.AddComponent<ProjectileReflectionTestActor>();
                if (failure == 2) reflector.ClearCombatType();
                var target = targetObject.AddComponent<ProjectileDefenseCommitTestTarget>();
                target.Defender = reflector;
                try
                {
                    Physics.SyncTransforms();
                    runtime.Initialize(definition, new ProjectileSpawnRequest { origin = center, direction = Vector3.forward },
                        new AttackData(), (p, e) => p.OnReturnedToPool(), null);
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    typeof(ProjectileRuntime).GetField("_contactId", flags).SetValue(runtime, 1u);
                    var contact = new ProjectileContact(runtime,
                        (ulong)typeof(ProjectileRuntime).GetField("_launchId", flags).GetValue(runtime),
                        (uint)typeof(ProjectileRuntime).GetField("_poolGeneration", flags).GetValue(runtime), 1u, Time.time, center);
                    var request = HitRequest.FromAttackData(new AttackData { isProjectile = true,
                        isReflectableProjectile = true, projectileContact = contact });
                    CombatResult result = target.ReceiveHit(request);
                    Assert.That(result.Defense.ProjectileResponse, Is.EqualTo(failure == 0
                        ? ProjectileResponse.Reflected : ProjectileResponse.Absorbed));
                    Assert.That(target.SawCommittedState, Is.True);
                    target.ReceiveHit(request);
                    Assert.That(target.AppliedCount, Is.EqualTo(1));
                    Assert.That(result.DamageApplied, Is.False);
                }
                finally
                {
                    Object.Destroy(go); Object.Destroy(reflectorObject); Object.Destroy(targetObject);
                    Object.Destroy(wall); Object.Destroy(definition);
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 반환탄은_접촉중심에서_한번만_반사하고_새수명을_사용한다()
        {
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.motion = new LinearProjectileMotion { speed = 20f, maxSpeed = 50f };
            definition.behaviors.Add(new ReflectableProjectileBehavior());
            definition.lifetime = 0.1f;
            definition.collisionRadius = 0.1f;
            definition.detachTrailOnReturn = false;
            var go = new GameObject("반환탄");
            var reflectorObject = new GameObject("반사자");
            var runtime = go.AddComponent<ProjectileRuntime>();
            var reflector = reflectorObject.AddComponent<ProjectileReflectionTestActor>();
            Vector3 center = new(10000f, 10000f, 10000f);
            bool returned = false;
            try
            {
                var launchAttack = new AttackData { damage = 9f, criticalMultiplier = 2f,
                    forceReaction = true, forceBreakExpose = true, isCounterAttack = true };
                runtime.Initialize(definition, new ProjectileSpawnRequest { origin = center, direction = Vector3.forward },
                    launchAttack, (p, expired) => { returned = true; p.OnReturnedToPool(); }, null);
                launchAttack.damage = 999f;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(ProjectileRuntime).GetField("_contactId", flags).SetValue(runtime, 1u);
                var contact = new ProjectileContact(runtime,
                    (ulong)typeof(ProjectileRuntime).GetField("_launchId", flags).GetValue(runtime),
                    (uint)typeof(ProjectileRuntime).GetField("_poolGeneration", flags).GetValue(runtime), 1u, Time.time, center);
                Assert.That(runtime.TryCommitDefense(contact, reflector, out ProjectileResponse response), Is.True);
                Assert.That(response, Is.EqualTo(ProjectileResponse.Reflected));
                var reflectedAttack = (AttackData)typeof(ProjectileRuntime).GetField("_attackData", flags).GetValue(runtime);
                Assert.That(reflectedAttack.damage, Is.EqualTo(9f));
                Assert.That(reflectedAttack.forceReaction || reflectedAttack.forceBreakExpose || reflectedAttack.isCounterAttack, Is.False);
                var request = HitRequest.FromAttackData(reflectedAttack);
                var damage = DamageResolver.ResolveMonsterDamage(null, HitContext.Create(request, null), null);
                Assert.That(damage.FinalDamage, Is.EqualTo(18f).Within(0.001f));
                Assert.That(request.ProjectileDamageSource.IsReflected, Is.True);
                int creditId = reflector.CombatantRuntimeId;
                reflector.SetCharacter(global::UPlayGround.Data.EnumType.CharacterActorType.Hwarin);
                var ledger = new CombatContributionLedger(); ledger.Record(request, 18f);
                Assert.That(ledger.CreateKillContext(null).CreditCombatantId, Is.EqualTo(creditId));
                Assert.That(ledger.CreateKillContext(null).CreditCharacter, Is.EqualTo(global::UPlayGround.Data.EnumType.CharacterActorType.Raon));
                Assert.That(ledger.CreateKillContext(null).GrantsPlayerRewards, Is.True);
                Assert.That(runtime.TryCommitDefense(contact, reflector, out _), Is.False);
                Assert.That(runtime.TryReflect(reflector, Vector3.forward), Is.False);
                runtime.Tick(0f);
                Assert.That(runtime.transform.position, Is.EqualTo(center));
                runtime.Tick(0.2f);
                Assert.That(runtime.transform.position.z, Is.EqualTo(center.z - 5f).Within(0.01f));
                Assert.That(returned, Is.False, "원 수명이 아닌 반환 2초 예산을 사용해야 합니다.");
                runtime.Tick(1.79f);
                Assert.That(returned, Is.False);
                runtime.Tick(0.02f);
                Assert.That(returned, Is.True);
                Assert.That(runtime.IsReset, Is.True);
                runtime.Initialize(definition, new ProjectileSpawnRequest { origin = center, direction = Vector3.forward },
                    new AttackData(), null, null);
                Assert.That(runtime.TryCommitDefense(contact, reflector, out _), Is.False, "이전 풀 세대 접촉은 취소해야 합니다.");
            }
            finally
            {
                Object.Destroy(go); Object.Destroy(reflectorObject); Object.Destroy(definition);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 초기겹침_조회는_32초과를_재조회하고_상한포화는_피해없이_소멸한다()
        {
            foreach (int density in new[] { 40, 130, 520 })
            {
                var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
                definition.collisionRadius = 0.2f;
                definition.detachTrailOnReturn = false;
                var go = new GameObject("밀집 투사체");
                var runtime = go.AddComponent<ProjectileRuntime>();
                var objects = new System.Collections.Generic.List<GameObject>();
                Vector3 center = new(10000f, 11000f, 10000f);
                bool returned = false;
                try
                {
                    for (int i = 0; i < density; i++)
                    {
                        var obstacle = new GameObject("겹친 벽") { layer = 29 };
                        obstacle.transform.position = center;
                        obstacle.AddComponent<BoxCollider>();
                        objects.Add(obstacle);
                    }
                    Physics.SyncTransforms();
                    runtime.Initialize(definition, new ProjectileSpawnRequest { origin = center,
                        direction = Vector3.forward, hitLayers = 1 << 29 }, new AttackData(),
                        (p, expired) => { returned = true; p.OnReturnedToPool(); }, null);
                    var collect = typeof(ProjectileRuntime).GetMethod("CollectSweep",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    object[] args = { center - Vector3.forward * 2f, Vector3.forward, 4f, null };
                    int found = (int)collect.Invoke(runtime, args);
                    Assert.That(found, Is.EqualTo(density < 512 ? density : -1), "Sweep 포화도 실제 전체 구간을 재조회해야 합니다.");
                    Assert.That(((RaycastHit[])args[3]).Length, Is.EqualTo(density < 128 ? 128 : 512));
                    runtime.Tick(1f / 30f);
                    Assert.That(returned, Is.True, density.ToString());
                    Assert.That(runtime.IsReset, Is.True);
                }
                finally
                {
                    foreach (var obstacle in objects) Object.Destroy(obstacle);
                    Object.Destroy(go); Object.Destroy(definition);
                }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator 고속_저프레임탄은_벽뒤_대상을_맞히지_않는다()
        {
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.motion = new LinearProjectileMotion { speed = 1000f, maxSpeed = 1000f };
            definition.collisionRadius = 0.1f;
            definition.detachTrailOnReturn = false;
            definition.obstructionLayers = 1 << 29;
            var go = new GameObject("고속 투사체");
            var runtime = go.AddComponent<ProjectileRuntime>();
            var wall = new GameObject("밀착 벽") { layer = 29 };
            var targetObject = new GameObject("벽 뒤 대상") { layer = 29 };
            Vector3 center = new(10000f, 12000f, 10000f);
            wall.transform.position = center + Vector3.forward * 2f;
            targetObject.transform.position = center + Vector3.forward * 4f;
            wall.AddComponent<BoxCollider>(); targetObject.AddComponent<BoxCollider>();
            var target = targetObject.AddComponent<StaffMagicPlayModeDamageTarget>();
            try
            {
                Physics.SyncTransforms();
                runtime.Initialize(definition, new ProjectileSpawnRequest { origin = center,
                    direction = Vector3.forward, hitLayers = 1 << 29 }, new AttackData(), (p, e) => p.OnReturnedToPool(), null);
                runtime.Tick(1f / 30f);
                Assert.That(target.HitCount, Is.Zero);
                Assert.That(runtime.IsReset, Is.True);
            }
            finally { Object.Destroy(go); Object.Destroy(wall); Object.Destroy(targetObject); Object.Destroy(definition); }
            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator 실제_Staff_마력탄과_레이저가_한번_명중하고_반환된다()
        {
            foreach (string name in new[] { "Bolt", "Beam" })
            {
                var source = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(
                    "Assets/10.Datas/Ability/Player/Staff/Projectile_Staff" + name + ".asset");
                Assert.That(source, Is.Not.Null);
                var definition = Object.Instantiate(source);
                // 피격 서비스는 별도 수직 슬라이스에서 검증하고 여기서는 실제 VFX 수명을 확인한다.
                definition.hitEffectKey = string.Empty;
                var origin = new Vector3(10000, 10000, 10000);
                var targetObject = new GameObject("StaffProjectileTarget") { layer = 29 };
                targetObject.transform.position = origin + Vector3.forward * 3;
                targetObject.AddComponent<BoxCollider>();
                var target = targetObject.AddComponent<StaffMagicPlayModeDamageTarget>();
                var visual = Object.Instantiate(definition.visualPrefab);
                var runtime = visual.AddComponent<ProjectileRuntime>();
                bool returned = false;
                try
                {
                    foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled) continue;
                        bool hasMaterial = false;
                        foreach (var material in renderer.sharedMaterials)
                        {
                            // 파티클의 사용하지 않는 Trail 머티리얼 슬롯은 비어 있을 수 있다.
                            if (material == null) continue;
                            hasMaterial = true;
                            Assert.That(material.shader.isSupported, Is.True, material.name);
                        }
                        Assert.That(hasMaterial, Is.True, renderer.name);
                    }
                    int particleCount = visual.GetComponentsInChildren<ParticleSystem>(true).Length;
                    Physics.SyncTransforms();
                    for (int reuse = 0; reuse < 2; reuse++)
                    {
                        returned = false;
                        runtime.Initialize(definition, new ProjectileSpawnRequest
                        {
                            definition = definition, origin = origin, logicalOrigin = origin,
                            direction = Vector3.forward, hitLayers = 1 << 29
                        }, new AttackData { damage = 42 }, (projectile, expired) =>
                        {
                            returned = true;
                            projectile.OnReturnedToPool();
                        }, null);
                        for (int frame = 0; frame < 180 && !returned; frame++)
                        {
                            runtime.Tick(1f / 60f);
                            if (name == "Beam" && frame == 0)
                            {
                                Assert.That(target.HitCount, Is.EqualTo(reuse + 1));
                                Assert.That(runtime.IsActive, Is.True);
                                var line = visual.GetComponentInChildren<LineRenderer>();
                                Assert.That(line.GetPosition(0), Is.EqualTo(origin));
                                Assert.That(line.GetPosition(1).z - origin.z, Is.InRange(2.3f, 3f));
                            }
                            yield return null;
                        }
                        Assert.That(target.HitCount, Is.EqualTo(reuse + 1), name);
                        Assert.That(returned, Is.True, name);
                        Assert.That(runtime.IsReset, Is.True, name);
                        yield return null;
                        Assert.That(visual.GetComponentsInChildren<ParticleSystem>(true).Length,
                            Is.EqualTo(particleCount), "풀 반환 중 외부 파티클이 스스로 삭제되면 재사용 시 VFX가 유실됩니다.");
                    }
                }
                finally
                {
                    Object.Destroy(visual);
                    Object.Destroy(targetObject);
                    Object.Destroy(definition);
                }
                yield return null;
            }
        }
#endif
    }

    public sealed class StaffMagicPlayModeDamageTarget : MonoBehaviour, IDamageable
    {
        public int HitCount { get; private set; }
        public CombatResult ReceiveHit(in HitRequest request) { HitCount++; return default; }
        public bool IsAlive() => true;
        public bool CanTakeDamage() => true;
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => 1;
        public float GetCurrentHealth() => 100;
        public void ApplyHealingEffect(float healAmount) { }
    }

    /// <summary>이동/반사 경계 검사에 필요한 액터 계약만 제공하는 테스트 픽스처.</summary>
    public sealed class ProjectileReflectionTestActor : GameActor
    {
        protected override void Awake()
        {
            _actorType = global::UPlayGround.Data.EnumType.ActorType.Player;
            _characterActorType = global::UPlayGround.Data.EnumType.CharacterActorType.Raon;
        }
        public void SetCharacter(global::UPlayGround.Data.EnumType.CharacterActorType character) => _characterActorType = character;
        public void ClearCombatType() => _actorType = global::UPlayGround.Data.EnumType.ActorType.None;
        protected override void Start() { }
        protected override void OnDestroy() { }
    }

    /// <summary>제품 파이프라인의 확정·적용 순서를 관찰하는 테스트 경계.</summary>
    public sealed class ProjectileDefenseCommitTestTarget : MonoBehaviour, ICombatResolvable
    {
        public GameActor Defender;
        public int AppliedCount;
        public bool SawCommittedState;
        public bool CanResolveHit(in HitRequest request) => true;
        public CombatResult ReceiveHit(in HitRequest request) => CombatResolutionPipeline.Execute(this, request);
        public CombatResult ResolveHit(in HitRequest request) => CombatResult.Build(
            HitContext.Create(request, Defender), new DefenseResult(DefenseOutcome.PerfectGuard, false,
                ProjectileDefenseSource.Guard, 1), default, ReactionDecision.None, ResourceChangeSet.Empty);
        public CombatResult ApplyResolvedHit(in HitRequest request, in CombatResult result)
        {
            AppliedCount++;
            SawCommittedState = result.Defense.ProjectileResponse == ProjectileResponse.Absorbed
                ? !request.ProjectileContact.Runtime.IsActive
                : request.ProjectileContact.Runtime.Owner == Defender;
            return result;
        }
        public bool IsAlive() => true;
        public bool CanTakeDamage() => true;
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => 1f;
        public float GetCurrentHealth() => 100f;
        public void ApplyHealingEffect(float amount) { }
    }
}
