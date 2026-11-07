using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>신규 실물 창 프리팹의 라우터·실제 판정과 중단 정리를 확인한다.</summary>
    public sealed class HumanoidSpearPlayModeTests
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator 여덟_창_공격은_실제_타임라인으로_적중하고_종료시_판정을_반환한다()
        {
            var objects = new TestActorObjectService();
            Services.Register(objects);
            var origin = new Vector3(10000, 0, 10000);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = origin + Vector3.down * .5f;
            ground.transform.localScale = new Vector3(40, 1, 40);
            var targetObject = new GameObject("창 실제 적중 검사 대상") { layer = LayerMask.NameToLayer("Player") };
            var target = targetObject.AddComponent<SpearRecordingTarget>();
            target.ConfigurePlayer();
            var capsule = targetObject.AddComponent<CapsuleCollider>();
            capsule.center = Vector3.up * .8f; capsule.height = 1.6f; capsule.radius = .35f;
            var materialField = typeof(DissolveController).GetField("_dissolveSourceMaterial", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var previousMaterial = materialField.GetValue(null);
            GameObject instance = null;
            var report = new List<string> { "ability,phases,damage,collisionSamples,interrupted" };
            try
            {
                materialField.SetValue(null, AssetDatabase.LoadAssetAtPath<Material>("Assets/11.Material/DissolveMaterial.mat"));
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>("Assets/10.Datas/Actor/HumanoidSpear/Enemy_Field_Spear_001.asset");
                var attacks = definition.EffectiveAbilitySet.EnumerateAll().Where(a => (a.variants.FirstOrDefault()?.executionPayload as UPlayGroundMotionAbilityPayloadSO)?.attackInfo.aiSelectable == true).ToArray();
                Assert.That(attacks.Length, Is.EqualTo(8));
                foreach (var ability in attacks)
                {
                    var info = ((UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload).attackInfo;
                    instance = Object.Instantiate(definition.prefab, origin, Quaternion.identity);
                    // 화면 밖 격리 시험에서도 실제 본·판정 포즈를 갱신한다. 프리팹의 컬링 정책은 유지한다.
                    instance.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    instance.GetComponent<EnemyAIController>().enabled = false;
                    var actor = instance.GetComponent<MonsterActor>();
                    var combat = instance.GetComponent<EnemyCombat>();
                    actor.SetDefinition(definition); actor.ApplyRuntimeLevel(30);
                    float distance = info.aiRoles == AbilityAIRole.GapCloser ? 2.8f : 1.4f;
                    targetObject.transform.position = origin + Vector3.forward * distance;
                    instance.GetComponent<EnemyDetection>().AcquireTarget(targetObject.transform);
                    yield return new WaitForSeconds(.2f);
                    Physics.SyncTransforms(); target.Hits.Clear();
                    combat.CombatTestAbility = ability;
                    if (info.requiredLevel > 1)
                    {
                        actor.ApplyRuntimeLevel(info.requiredLevel - 1);
                        Assert.That(combat.HasAvailableSkillAtDistance(distance, info.attackCategory), Is.False, ability.name + " 해금 직전");
                        actor.ApplyRuntimeLevel(info.requiredLevel);
                    }
                    Assert.That(combat.HasAvailableSkillAtDistance(distance, info.attackCategory), Is.True, ability.name + "\n" + combat.BuildAbilitySelectionDiagnosticSummary());
                    Assert.That(EnemyAbilityTriggerTags.TryResolveAttackTrigger(combat.AbilitySet, info.attackCategory, out _, out _, out var tag), Is.True);
                    AbilityExecutionHandle handle = default;
                    actor.Abilities.AbilityTriggerAccepted += (_, accepted) => handle = accepted;
                    combat.ReserveAttackCategory(info.attackCategory);
                    actor.Abilities.IssueTriggerEvent(tag, actor, target);
                    Assert.That(handle.IsValid, Is.True, ability.name);
                    float duration = combat.CurrentMotionAsset.motionSet.TotalDuration;
                    int collisionSamples = 0;
                    for (float elapsed = 0; elapsed < duration + .2f; elapsed += Time.deltaTime)
                    {
                        if (combat.IsPossibleCollide) collisionSamples++;
                        yield return null;
                    }
                    report.Add($"{ability.abilityId},{string.Join("/", target.Hits.Select(h => h.HitPhaseIndex))},{target.Hits.Sum(h => h.Damage)},{collisionSamples},false");
                    Assert.That(collisionSamples, Is.GreaterThan(0), ability.name + " 판정 이벤트 미실행");
                    Assert.That(target.Hits.Count, Is.GreaterThan(0), ability.name + " 실물 창 미적중");
                    Assert.That(target.Hits.GroupBy(h => h.HitPhaseIndex).All(g => g.Count() == 1), Is.True, ability.name + " 동일 Phase 중복 적중");
                    Assert.That(target.Hits.Select(h => h.HitPhaseIndex), Is.EqualTo(Enumerable.Range(0, info.baseInfo.hitPhases.Count)), ability.name + " Phase 적중 누락");
                    Assert.That(actor.Abilities.IsExecutionActive(handle), Is.False, ability.name + " 종료 누수");
                    Assert.That(combat.IsPossibleCollide, Is.False);
                    Assert.That(combat.IsMotionWarping, Is.False);
                    if (info.aiRoles == AbilityAIRole.Punish && info.requiredLevel == 1)
                    {
                        combat.CombatTestAbility = attacks.Single(a => ((UPlayGroundMotionAbilityPayloadSO)a.variants[0].executionPayload).attackInfo.requiredLevel == 30);
                        Assert.That(combat.HasAvailableSkillAtDistance(distance, AbilityAttackCategory.Heavy), Is.False, "Lv30 응징의 공유 쿨다운 우회");
                        combat.CombatTestAbility = null;
                        Assert.That(combat.HasAvailableSkillAtDistance(distance, AbilityAttackCategory.Basic, AbilityAIRole.Opener), Is.True, "응징 대기 중 기본기 fallback");
                    }
                    if (info.aiRoles == AbilityAIRole.Opener && info.requiredLevel == 1)
                    {
                        combat.CombatTestAbility = null;
                        actor.ApplyRuntimeLevel(50);
                        Assert.That(combat.HasAvailableSkillAtDistance(2f, AbilityAttackCategory.Skill, AbilityAIRole.Signature), Is.False);
                        // 방금 기본기를 실행했으므로 쿨다운 만료 뒤 fallback을 확인한다.
                        yield return new WaitForSeconds(.85f);
                        Assert.That(combat.HasAvailableSkillAtDistance(2f, AbilityAttackCategory.Basic, AbilityAIRole.Opener), Is.True);
                    }
                    Object.Destroy(instance); instance = null; yield return null;
                }
                var strong = attacks.Single(a => ((UPlayGroundMotionAbilityPayloadSO)a.variants[0].executionPayload).attackInfo.requiredLevel == 30);
                foreach (int exit in new[] { 0, 1, 2, 3 })
                {
                    instance = Object.Instantiate(definition.prefab, origin, Quaternion.identity);
                    instance.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    instance.GetComponent<EnemyAIController>().enabled = false;
                    var actor = instance.GetComponent<MonsterActor>();
                    var combat = instance.GetComponent<EnemyCombat>();
                    actor.SetDefinition(definition); actor.ApplyRuntimeLevel(30);
                    targetObject.transform.position = origin + Vector3.forward * 1.4f;
                    instance.GetComponent<EnemyDetection>().AcquireTarget(targetObject.transform);
                    yield return new WaitForSeconds(.2f);
                    combat.CombatTestAbility = strong;
                    Assert.That(EnemyAbilityTriggerTags.TryResolveAttackTrigger(combat.AbilitySet, AbilityAttackCategory.Heavy, out _, out _, out var tag), Is.True);
                    AbilityExecutionHandle handle = default;
                    actor.Abilities.AbilityTriggerAccepted += (_, accepted) => handle = accepted;
                    combat.ReserveAttackCategory(AbilityAttackCategory.Heavy);
                    actor.Abilities.IssueTriggerEvent(tag, actor, target);
                    Assert.That(handle.IsValid, Is.True);
                    yield return new WaitForSeconds(.53f);
                    var movement = instance.GetComponent<global::UPlayGround.MovementController.ActorMovementController>();
                    if (exit == 0) movement.TransitionToState(global::UPlayGround.State.ActorStateId.Idle);
                    else if (exit == 1) movement.TransitionToState(new global::UPlayGround.State.EnemyHitState(movement));
                    else if (exit == 2) movement.TransitionToState(new global::UPlayGround.State.EnemyStunState(movement));
                    else movement.TransitionToState(new global::UPlayGround.State.EnemyDeathState(movement));
                    Assert.That(actor.Abilities.IsExecutionActive(handle), Is.False, "중단 " + exit);
                    Assert.That(combat.IsPossibleCollide, Is.False, "중단 " + exit);
                    Assert.That(combat.IsMotionWarping, Is.False, "중단 " + exit);
                    report.Add($"{strong.abilityId},exit{exit},0,0,true");
                    Object.Destroy(instance); instance = null; yield return null;
                }
            }
            finally
            {
                System.IO.Directory.CreateDirectory("Logs/HumanoidSpear");
                System.IO.File.WriteAllLines("Logs/HumanoidSpear/play-hits.csv", report);
                if (instance != null) Object.Destroy(instance);
                Object.Destroy(targetObject); Object.Destroy(ground);
                materialField.SetValue(null, previousMaterial);
                Services.Unregister(objects);
            }
            yield return null;
        }
#endif
    }

    /// <summary>실제 수신 경계에서 Phase와 스탯 계산 전 입력 피해를 기록한다.</summary>
    public sealed class SpearRecordingTarget : GameActor, IDamageable
    {
        public readonly List<HitRequest> Hits = new();
        public void ConfigurePlayer() => _actorType = ActorType.Player;
        public CombatResult ReceiveHit(in HitRequest request) { Hits.Add(request); return default; }
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
