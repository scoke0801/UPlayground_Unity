using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Ability.Core;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Components;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Projectile;
using UPlayGround.Manager;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>생성 프리팹의 BT 공격 요청·레벨 해금·몬스터 대상 조준·취소를 함께 검사한다.</summary>
    public sealed class P09StaffMonsterPlayModeTests
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator 생성된_스태프_몬스터는_등급별_공격을_요청하고_전투대상을_조준한다()
        {
            var objects = new TestActorObjectService();
            var projectiles = new RecordingProjectileService();
            Services.Register(objects);
            Services.Register(projectiles);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "스태프 검사 바닥";
            ground.layer = LayerMask.NameToLayer("Ground");
            var origin = new Vector3(10000, 0, 10000);
            ground.transform.position = origin + Vector3.down * .5f;
            ground.transform.localScale = new Vector3(40, 1, 40);
            var targetObject = new GameObject("스태프 검사 대상") { layer = LayerMask.NameToLayer("Player") };
            var target = targetObject.AddComponent<P09StaffTestTarget>();
            target.ConfigurePlayer();
            GameObject instance = null;
            try
            {
                var definitions = AssetDatabase.FindAssets("t:ActorDefinitionSO", new[] { "Assets/10.Datas/Actor/P09Progression/Generated" })
                    .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("_Staff_"))
                    .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>).ToArray();
                Assert.That(definitions.Length, Is.EqualTo(14));
                foreach (var definition in definitions)
                {
                    instance = Object.Instantiate(definition.prefab, origin, Quaternion.identity);
                    var actor = instance.GetComponent<MonsterActor>();
                    var combat = instance.GetComponent<EnemyCombat>();
                    instance.GetComponent<EnemyAIController>().enabled = false;
                    actor.SetDefinition(definition);
                    targetObject.transform.position = origin + Vector3.forward * 8;
                    instance.GetComponent<EnemyDetection>().AcquireTarget(targetObject.transform);
                    yield return new WaitForSeconds(.3f);
                    Physics.SyncTransforms();

                    Assert.That(combat.HasAvailableSkillAtDistance(8, AbilityAttackCategory.Basic), Is.True,
                        definition.actorId + "\n" + combat.BuildAbilitySelectionDiagnosticSummary());
                    bool elite = definition.EffectiveLevel >= 20;
                    Assert.That(combat.HasAvailableSkillAtDistance(8, AbilityAttackCategory.Skill), Is.EqualTo(elite),
                        definition.actorId + "\n" + combat.BuildAbilitySelectionDiagnosticSummary());
                    var category = elite ? AbilityAttackCategory.Skill : AbilityAttackCategory.Basic;
                    Assert.That(EnemyAbilityTriggerTags.TryResolveAttackTrigger(combat.AbilitySet, category,
                        out _, out _, out var tag), Is.True);
                    AbilityExecutionHandle accepted = default;
                    actor.Abilities.AbilityTriggerAccepted += (_, handle) => accepted = handle;
                    combat.ReserveAttackCategory(category);
                    actor.Abilities.IssueTriggerEvent(tag, actor, target);
                    Assert.That(accepted.IsValid, Is.True, definition.actorId + "\n" + combat.BuildAbilitySelectionDiagnosticSummary());
                    Assert.That(combat.CurrentAbility.abilityId, Is.EqualTo(elite ? "Monster.P09Staff.Beam" : "Monster.P09Staff.Bolt"));
                    Assert.That(actor.Abilities.IsExecutionActive(accepted), Is.True);

                    // 발사 이벤트를 실제 소유 액터에 전달하여 카메라 락온이 아닌 몬스터의 타깃을 검증한다.
                    var spawn = combat.CurrentMotionAsset.motionSet.motions.SelectMany(m => m.events).OfType<SpawnProjectileEvent>().Single();
                    projectiles.HasSpawn = false;
                    spawn.Execute(instance);
                    Assert.That(projectiles.HasSpawn, Is.True);
                    Assert.That(projectiles.Last.targetTransform, Is.SameAs(targetObject.transform));
                    Assert.That(projectiles.Last.definition, Is.SameAs(combat.CurrentSkill.baseInfo.hitPhases[0].projectileDefinition));
                    Assert.That(projectiles.Last.hitPhaseIndex, Is.Zero);
                    Assert.That(projectiles.Last.hitLayers.value & (1 << targetObject.layer), Is.Not.Zero);
                    combat.CancelCurrentAbility();
                    Assert.That(actor.Abilities.IsExecutionActive(accepted), Is.False);
                    Object.Destroy(instance);
                    instance = null;
                    yield return null;
                }
            }
            finally
            {
                if (instance != null) Object.Destroy(instance);
                Object.Destroy(targetObject);
                Object.Destroy(ground);
                Services.Unregister(projectiles);
                Services.Unregister(objects);
            }
            yield return null;
        }

        private sealed class RecordingProjectileService : IProjectileService
        {
            public void CancelOwnedBy(GameObject owner) { }
            public ProjectileSpawnRequest Last;
            public bool HasSpawn;
            public int CountActive => 0;
            public int CountAll => 0;
            public int CountInactive => 0;
            public void Spawn(ProjectileSpawnRequest request) { Last = request; HasSpawn = true; }
            public bool TryReflect(GameObject projectileObject, GameObject newOwner, Vector3 direction) => false;
        }
#endif
    }

    /// <summary>액터 자체가 IDamageable인 현재 전투 대상 계약을 충족하는 테스트 타깃.</summary>
    public sealed class P09StaffTestTarget : GameActor, IDamageable
    {
        /// <summary>테스트 몬스터와 적대하는 플레이어 진영을 지정한다.</summary>
        public void ConfigurePlayer() => _actorType = ActorType.Player;
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
