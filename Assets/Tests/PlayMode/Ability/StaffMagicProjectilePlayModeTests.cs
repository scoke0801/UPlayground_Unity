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
}
