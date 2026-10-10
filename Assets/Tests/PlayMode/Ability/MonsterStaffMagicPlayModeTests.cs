using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;
using UPlayGround.Manager;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>몬스터 모션의 실제 발사 이벤트가 적 AI의 대상과 오른손 위치를 서비스에 전달하는지 검증한다.</summary>
    public sealed class MonsterStaffMagicPlayModeTests
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator 몬스터용_마력탄과_레이저가_스킬대상을_조준해_발사된다()
        {
            const string root = "Assets/10.Datas/Ability/Actor/P09Staff/";
            var projectiles = new RecordingProjectileService();
            var objects = new TestActorObjectService();
            Services.Register(projectiles);
            Services.Register(objects);
            var owner = new GameObject("몬스터 발사 이벤트 검증");
            var targetObject = new GameObject("스킬 대상");
            try
            {
                var actor = owner.AddComponent<AbilityPlayModeTestActor>();
                actor.SetActorType(ActorType.Monster);
                var combat = owner.AddComponent<EnemyCombat>();
                combat.enabled = false;
                var muzzle = new GameObject("StaffMagicMuzzle").transform;
                muzzle.SetParent(owner.transform, false);
                muzzle.localPosition = new Vector3(.4f, 1.4f, .3f);
                var target = targetObject.AddComponent<StaffMagicPlayModeDamageTarget>();
                targetObject.transform.position = new Vector3(4, 0, 8);
                combat.SkillTargetList.Add(target);
                var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(root + "P09StaffAnimationSet.asset");
                Assert.That(mapping, Is.Not.Null);
                foreach (string key in new[] { "Bolt", "Beam" })
                {
                    var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(root + "GA_P09Staff_" + key + ".asset");
                    var payload = (UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload;
                    typeof(EnemyCombat).GetField("_currentSkill", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(combat, payload.attackInfo);
                    var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
                    var shot = motion.motionSet.motions.SelectMany(m => m.events)
                        .Concat(motion.motionSet.globalEvents).OfType<SpawnProjectileEvent>().Single();
                    int before = projectiles.CountAll;
                    shot.Execute(owner);
                    Assert.That(projectiles.CountAll, Is.EqualTo(before + 1));
                    var request = projectiles.Last;
                    Assert.That(request.owner, Is.SameAs(owner));
                    Assert.That(request.origin, Is.EqualTo(muzzle.position));
                    Assert.That(request.targetTransform, Is.SameAs(target.transform));
                    Assert.That(request.definition, Is.SameAs(payload.attackInfo.baseInfo.hitPhases[0].projectileDefinition));
                    Assert.That(request.hitPhaseIndex, Is.Zero);
                    Vector3 expected = target.transform.position + shot.targetOffset;
                    Assert.That(request.targetPosition, Is.EqualTo(expected));
                    Assert.That(Vector3.Angle(request.direction, expected - muzzle.position), Is.LessThan(.01f));
                    Assert.That(request.hitLayers.value & LayerMask.GetMask("Player"), Is.Not.Zero);
                    yield return null;
                }
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(targetObject);
                Services.Unregister(projectiles);
                Services.Unregister(objects);
            }
        }
#endif
        private sealed class RecordingProjectileService : IProjectileService
        {
            public ProjectileSpawnRequest Last;
            public int CountActive => 0;
            public int CountAll { get; private set; }
            public int CountInactive => 0;
            public void Spawn(ProjectileSpawnRequest request) { Last = request; CountAll++; }
            public bool TryReflect(GameObject projectileObject, GameObject newOwner, Vector3 direction) => false;
        }
    }
}
