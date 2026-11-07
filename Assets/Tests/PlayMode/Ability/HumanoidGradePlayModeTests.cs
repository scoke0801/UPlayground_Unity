using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>실소유 프리팹에서 레벨 변경·HP 조건·라우터 실행·중단 정리를 검사한다.</summary>
    public sealed class HumanoidGradePlayModeTests
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator 근접2_궁수1_필드조건은_각1슬롯을_양보하고_반환한다()
        {
            var objects = new TestActorObjectService();
            Services.Register(objects);
            var instances = new System.Collections.Generic.List<GameObject>();
            var groupObject = new GameObject("등급 혼성 슬롯 검사");
            var group = groupObject.AddComponent<global::UPlayGround.Group.MonsterGroupController>();
            var settings = new SerializedObject(group);
            settings.FindProperty("_meleeSlotCap").intValue = 1;
            settings.FindProperty("_rangedSlotCap").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var materialField = typeof(DissolveController).GetField("_dissolveSourceMaterial", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var previousMaterial = materialField.GetValue(null);
            try
            {
                materialField.SetValue(null, AssetDatabase.LoadAssetAtPath<Material>("Assets/11.Material/DissolveMaterial.mat"));
                foreach (string path in new[] {
                    "Assets/10.Datas/Actor/DataBase/Enemy_Random_M_GreatSword_001_ActorDef.asset",
                    "Assets/10.Datas/Actor/HumanoidSpear/Enemy_Field_Spear_001.asset",
                    "Assets/10.Datas/Actor/DataBase/Enemy_Random_F_Bow_001_ActorDef.asset" })
                {
                    var d = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                    var instance = Object.Instantiate(d.prefab, new Vector3(10000 + instances.Count * 3, 0, 10000), Quaternion.identity);
                    instances.Add(instance);
                    instance.GetComponent<EnemyAIController>().enabled = false;
                    var actor = instance.GetComponent<MonsterActor>();
                    actor.SetDefinition(d);
                    group.RegisterMember(actor, global::UPlayGround.Group.MemberPriority.Normal);
                }
                var actors = instances.Select(i => i.GetComponent<MonsterActor>()).ToArray();
                Assert.That(group.AliveCount, Is.EqualTo(3));
                Assert.That(group.RequestAttackSlot(actors[0], AttackType.Melee), Is.True);
                Assert.That(group.RequestAttackSlot(actors[1], AttackType.Melee), Is.False, "근접 동시 점유 초과");
                Assert.That(group.RequestAttackSlot(actors[2], AttackType.Ranged), Is.True);
                Assert.That(group.GetDebugSnapshot().MeleeOwners, Is.EqualTo(1));
                Assert.That(group.GetDebugSnapshot().RangedOwners, Is.EqualTo(1));
                group.ReleaseAttackSlot(actors[0]);
                Assert.That(group.RequestAttackSlot(actors[1], AttackType.Melee), Is.True, "양보 뒤 슬롯 누수");
                foreach (var actor in actors) group.UnregisterMember(actor);
                Assert.That(group.GetDebugSnapshot().MeleeOwners + group.GetDebugSnapshot().RangedOwners, Is.Zero);
            }
            finally
            {
                foreach (var instance in instances) Object.Destroy(instance);
                Object.Destroy(groupObject);
                materialField.SetValue(null, previousMaterial);
                Services.Unregister(objects);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator 등급별_실제_프리팹은_해금_조건을_반영하고_공격을_정리한다()
        {
            var objects = new TestActorObjectService();
            Services.Register(objects);
            var origin = new Vector3(10000, 0, 10000);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = origin + Vector3.down * 0.5f;
            ground.transform.localScale = new Vector3(40, 1, 40);
            var targetObject = new GameObject("등급 대검 검사 대상") { layer = LayerMask.NameToLayer("Player") };
            var target = targetObject.AddComponent<P09StaffTestTarget>();
            target.ConfigurePlayer();
            GameObject instance = null;
            var materialField = typeof(DissolveController).GetField("_dissolveSourceMaterial", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var previousMaterial = materialField.GetValue(null);
            try
            {
                materialField.SetValue(null, AssetDatabase.LoadAssetAtPath<Material>("Assets/11.Material/DissolveMaterial.mat"));
                foreach (string path in new[]
                {
                    "Assets/10.Datas/Actor/DataBase/Enemy_Random_M_GreatSword_002_ActorDef.asset",
                    "Assets/10.Datas/Actor/P09Progression/Generated/Enemy_P09_Armor08_GreatSword/Enemy_P09_Armor08_GreatSword_ActorDef.asset",
                    "Assets/10.Datas/Actor/DataBase/Humanoid/MonsterLili.asset",
                    "Assets/10.Datas/Actor/DataBase/Humanoid/MonsterRaon.asset",
                    "Assets/10.Datas/Actor/DataBase/Humanoid/MonsterLian.asset",
                    "Assets/10.Datas/Actor/DataBase/Humanoid/MonsterYura.asset",
                    "Assets/10.Datas/Actor/DataBase/Humanoid/MonsterSera.asset",
                    "Assets/10.Datas/Actor/DataBase/Humanoid/MonsterSeolA.asset"
                })
                foreach (var exitState in new[] { global::UPlayGround.State.ActorStateId.Idle, global::UPlayGround.State.ActorStateId.Hit, global::UPlayGround.State.ActorStateId.Stun, global::UPlayGround.State.ActorStateId.Death })
                {
                    var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                    Assert.That(definition, Is.Not.Null);
                    instance = Object.Instantiate(definition.prefab, origin, Quaternion.identity);
                    instance.GetComponent<EnemyAIController>().enabled = false;
                    var actor = instance.GetComponent<MonsterActor>();
                    var combat = instance.GetComponent<EnemyCombat>();
                    actor.SetDefinition(definition);
                    targetObject.transform.position = origin + Vector3.forward;
                    instance.GetComponent<EnemyDetection>().AcquireTarget(targetObject.transform);
                    yield return new WaitForSeconds(0.2f);

                    float FindAvailable(AbilityAttackCategory category = AbilityAttackCategory.None, AbilityAIRole role = AbilityAIRole.None)
                    {
                        for (float distance = 0.2f; distance < 26f; distance += 0.1f)
                        {
                            targetObject.transform.position = instance.transform.position + Vector3.forward * distance;
                            Physics.SyncTransforms();
                            if (combat.HasAvailableSkillAtDistance(distance, category, role)) return distance;
                        }
                        return -1f;
                    }

                    foreach (var ability in definition.EffectiveAbilitySet.EnumerateAll())
                    {
                        var info = (ability.variants.FirstOrDefault()?.executionPayload as UPlayGroundMotionAbilityPayloadSO)?.attackInfo;
                        if (info == null || info.requiredLevel <= 1) continue;
                        combat.CombatTestAbility = ability;
                        actor.ApplyRuntimeLevel(info.requiredLevel - 1);
                        actor.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, actor.MaxHealth * 0.49f);
                        Assert.That(FindAvailable(), Is.LessThan(0), ability.name + ": 해금 직전");
                        actor.ApplyRuntimeLevel(info.requiredLevel);
                        bool late = info.conditionGroup.conditions.Any(c => c.type == global::UPlayGround.Data.Combat.ConditionType.SelfHealthBased);
                        if (late)
                        {
                            actor.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, actor.MaxHealth * 0.51f);
                            Assert.That(FindAvailable(), Is.LessThan(0), ability.name + ": HP51%");
                        }
                        actor.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, actor.MaxHealth * 0.49f);
                        Assert.That(FindAvailable(), Is.GreaterThan(0), ability.name + "\n" + combat.BuildAbilitySelectionDiagnosticSummary());
                    }
                    combat.CombatTestAbility = null;
                    actor.ApplyRuntimeLevel(30);
                    if (definition.EffectiveGrade == MonsterActorGrade.Normal)
                    {
                        Assert.That(FindAvailable(AbilityAttackCategory.Skill, AbilityAIRole.Signature), Is.LessThan(0));
                        Assert.That(FindAvailable(AbilityAttackCategory.Basic, AbilityAIRole.Opener), Is.GreaterThan(0));
                        Assert.That(combat.CurrentAbility, Is.Null, "선택적 분기 조회는 실행/쿨다운을 소비하지 않아야 합니다.");
                    }
                    var strong = definition.EffectiveAbilitySet.EnumerateAll().First(a => (a.variants.FirstOrDefault()?.executionPayload as UPlayGroundMotionAbilityPayloadSO)?.attackInfo.requiredLevel == 30);
                    combat.CombatTestAbility = strong;
                    actor.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, actor.MaxHealth * 0.49f);
                    Assert.That(FindAvailable(), Is.GreaterThan(0));
                    var category = ((UPlayGroundMotionAbilityPayloadSO)strong.variants[0].executionPayload).attackInfo.attackCategory;
                    Assert.That(EnemyAbilityTriggerTags.TryResolveAttackTrigger(combat.AbilitySet, category, out _, out _, out var tag), Is.True,
                        $"라우터: {definition.actorId}/{combat.AbilitySet?.name}/{category}, 후보 {combat.AbilitySet?.GetRuntimeAbilities().Count}");
                    AbilityExecutionHandle handle = default;
                    actor.Abilities.AbilityTriggerAccepted += (_, accepted) => handle = accepted;
                    combat.ReserveAttackCategory(category);
                    actor.Abilities.IssueTriggerEvent(tag, actor, target);
                    Assert.That(handle.IsValid, Is.True, combat.BuildAbilitySelectionDiagnosticSummary());
                    Assert.That(combat.CurrentAbility, Is.SameAs(strong));
                    yield return new WaitForSeconds(0.1f);
                    // 상태 주입으로 각 OnExit 경로를 검사한다. 실제 피격/브레이크 판정과 입력 체감은 별도 검수한다.
                    var movement = instance.GetComponent<global::UPlayGround.MovementController.ActorMovementController>();
                    if (exitState == global::UPlayGround.State.ActorStateId.Hit)
                        movement.TransitionToState(new global::UPlayGround.State.EnemyHitState(movement));
                    else if (exitState == global::UPlayGround.State.ActorStateId.Stun)
                        movement.TransitionToState(new global::UPlayGround.State.EnemyStunState(movement));
                    else if (exitState == global::UPlayGround.State.ActorStateId.Death)
                        movement.TransitionToState(new global::UPlayGround.State.EnemyDeathState(movement));
                    else
                        movement.TransitionToState(exitState);
                    Assert.That(actor.Abilities.IsExecutionActive(handle), Is.False, definition.actorId + ": 실행 핸들 잔류");
                    Assert.That(combat.IsPossibleCollide, Is.False, definition.actorId + ": Collision 잔류");
                    Assert.That(combat.IsMotionWarping, Is.False, definition.actorId + ": 워프 잔류");
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
                materialField.SetValue(null, previousMaterial);
                Services.Unregister(objects);
            }
            yield return null;
        }
#endif
    }
}
