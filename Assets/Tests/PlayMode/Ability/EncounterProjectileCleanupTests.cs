using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Data;
using UPlayGround.Data.Projectile;
using UPlayGround.Manager;

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>조우 중단이 다른 소유자의 발사체를 제거하거나 후속 발사를 만들지 않는지 검사한다.</summary>
    public sealed class EncounterProjectileCleanupTests
    {
        [UnityTest]
        public IEnumerator 지정소유자의_활성탄과_예약탄만_회수한다()
        {
            var managerObject = new GameObject("투사체 정리 검사");
            var owner = new GameObject("조우 참가자");
            var other = new GameObject("다른 조우 참가자");
            var shot = new GameObject("활성탄");
            var otherShot = new GameObject("다른 활성탄");
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            var objects = new TestActorObjectService();
            Services.Register(objects);
            try
            {
                owner.AddComponent<AbilityPlayModeTestActor>();
                other.AddComponent<AbilityPlayModeTestActor>();
                var type = Type.GetType("UPlayGround.Manager.ProjectileManager, Assembly-CSharp", true);
                var manager = managerObject.AddComponent(type);
                var service = (IProjectileService)manager;
                var active = (IList)type.GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
                var pending = (IList)type.GetField("_pending", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
                int children = 0;
                foreach (var pair in new[] { (owner, shot), (other, otherShot) })
                {
                    var request = new ProjectileSpawnRequest { owner = pair.Item1, definition = definition, delay = 30f };
                    service.Spawn(request);
                    var runtime = pair.Item2.AddComponent<ProjectileRuntime>();
                    runtime.Initialize(definition, request, new AttackData(),
                        (returned, _) => active.Remove(returned), (_, _) => children++);
                    active.Add(runtime);
                }
                service.CancelOwnedBy(owner);
                Assert.AreEqual(1, service.CountActive);
                Assert.AreSame(otherShot.GetComponent<ProjectileRuntime>(), active[0]);
                Assert.AreEqual(1, pending.Count);
                var remaining = (ProjectileSpawnRequest)pending[0].GetType().GetField("request").GetValue(pending[0]);
                Assert.AreSame(other, remaining.owner);
                service.CancelOwnedBy(owner);
                service.CancelOwnedBy(null);
                Assert.AreEqual(1, service.CountActive);
                Assert.AreEqual(0, children);
                service.CancelOwnedBy(other);
                Assert.AreEqual(0, service.CountActive);
                Assert.AreEqual(0, pending.Count);
                yield return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
                UnityEngine.Object.DestroyImmediate(otherShot);
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(other);
                UnityEngine.Object.DestroyImmediate(managerObject);
                UnityEngine.Object.DestroyImmediate(definition);
                Services.Unregister(objects);
            }
        }
    }
}
