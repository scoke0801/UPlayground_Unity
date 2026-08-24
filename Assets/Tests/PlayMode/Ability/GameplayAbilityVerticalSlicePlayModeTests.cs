using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.PlayModeTests
{
    public sealed class GameplayAbilityVerticalSlicePlayModeTests
    {
        private readonly TestActorObjectService _actorObjects = new();

        [SetUp]
        public void SetUp()
        {
            Services.Register(_actorObjects);
        }

        [TearDown]
        public void TearDown()
        {
            Services.Unregister(_actorObjects);
        }

        [UnityTest]
        public IEnumerator Prepare부터_Payload_Commit_End까지_수직슬라이스가_정리된다()
        {
            var ownerObject = new GameObject("AbilityPlayModeOwner");
            var owner = ownerObject.AddComponent<AbilityPlayModeTestActor>();
            owner.SetActorType(ActorType.Player);

            Assert.That(owner.Abilities, Is.SameAs(owner.AbilitySystem.ProjectAbilities));
            Assert.That(owner.Effects, Is.SameAs(owner.AbilitySystem.ProjectEffects));
            Assert.That(owner.Tags, Is.SameAs(owner.AbilitySystem.ProjectTags));

            var payload = ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
            payload.attackInfo = new AbilityAttackInfo
            {
                motionKey = new MotionKey(
                    "Tests.PlayMode.VerticalSlice.Ground"),
                baseInfo = new AttackInfoBase(),
            };

            GameplayAbilitySO ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Test.PlayMode.VerticalSlice";
            var task = ScriptableObject.CreateInstance<WaitDelayTaskDefinitionSO>();
            task.duration = 999f;
            ability.taskGraph = AbilityTaskGraphSO.CreateTransient(task);
            ability.activation.targetRelation = AbilityTargetRelation.Self;
            ability.activation.executionGrantedTagIds.Add(GameplayTags.State_Combat_Attack);
            ability.cooldown.durationSeconds = 0.1f;
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Ground",
                priority = 1,
                executionPayload = payload,
            });

            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
            {
                slot = PlayerSkillSlot.Ability,
                ability = ability,
            });
            owner.Abilities.SetAbilitySet(set);

            try
            {
                AbilityActivationResult prepare = owner.Abilities.TryPreparePlayerSlot(
                    PlayerSkillSlot.Ability,
                    true,
                    null,
                    out AbilityExecutionHandle handle,
                    out AbilityVariantDefinition variant);

                Assert.That(prepare, Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(
                    UPlayGroundAbilityPayloadResolver.TryResolve(
                        variant,
                        out MotionKey motionKey,
                        out AbilityAttackInfo attackInfo),
                    Is.True);
                Assert.That(
                    motionKey,
                Is.EqualTo(new MotionKey(
                    "Tests.PlayMode.VerticalSlice.Ground")));
                Assert.That(attackInfo, Is.SameAs(payload.attackInfo));

                // 실제 프로젝트에서는 이 사이에서 상태 전환과 MotionSet 시작 승인을 수행한다.
                yield return null;

                Assert.That(
                    owner.Abilities.Commit(handle),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(owner.Abilities.HasActivePlayerAbility, Is.True);
                Assert.That(owner.Tags.HasTag(GameplayTags.State_Combat_Attack), Is.True);

                owner.Abilities.EndActivePlayerAbility(completed: true);

                Assert.That(owner.Abilities.HasActivePlayerAbility, Is.False);
                Assert.That(owner.Tags.HasTag(GameplayTags.State_Combat_Attack), Is.False);
            }
            finally
            {
                Object.Destroy(ownerObject);
                Object.Destroy(payload);
                Object.Destroy(ability);
                Object.Destroy(set);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayEvent_Request는_상태승인후_Commit_End까지_왕복한다()
        {
            var ownerObject = new GameObject("AbilityTriggerPlayModeOwner");
            var owner = ownerObject.AddComponent<AbilityPlayModeTestActor>();
            owner.SetActorType(ActorType.Player);

            GameplayAbilitySO route = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            route.abilityId = "Ability.Test.PlayMode.TriggerRoute";
            route.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                priority = 1,
            });
            route.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Player_Hit_Light,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });

            var payload = ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
            payload.attackInfo = new AbilityAttackInfo
            {
                motionKey = new MotionKey("Tests.PlayMode.TriggerRequest.Ground"),
                baseInfo = new AttackInfoBase(),
            };
            GameplayAbilitySO selected = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            selected.abilityId = "Ability.Test.PlayMode.TriggerSelected";
            var task = ScriptableObject.CreateInstance<WaitDelayTaskDefinitionSO>();
            task.duration = 999f;
            selected.taskGraph = AbilityTaskGraphSO.CreateTransient(task);
            selected.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Ground",
                priority = 1,
                executionPayload = payload,
            });

            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            set.additionalAbilities.Add(route);
            set.additionalAbilities.Add(selected);
            owner.Abilities.SetAbilitySet(set);

            bool stateEntered = false;
            AbilityExecutionHandle acceptedHandle = default;
            owner.Abilities.AbilityTriggerRequested += request =>
            {
                Assert.That(request.Ability, Is.SameAs(route));
                Assert.That(
                    owner.Abilities.TryPrepareAbility(
                        selected,
                        true,
                        null,
                        out AbilityExecutionHandle handle,
                        out _,
                        request.TriggerEvent),
                    Is.EqualTo(AbilityActivationResult.Success));
                stateEntered = owner.TryEnterTriggeredState();
                if (!stateEntered)
                {
                    owner.Abilities.Abort(handle);
                    return;
                }
                Assert.That(
                    owner.Abilities.Commit(handle),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(
                    owner.Abilities.BindActiveExecutionToTrigger(handle, request),
                    Is.True);
            };
            owner.Abilities.AbilityTriggerAccepted += (_, handle) =>
                acceptedHandle = handle;

            owner.Abilities.IssueTriggerEvent(
                GameplayTags.Trigger_Player_Hit_Light,
                owner,
                owner,
                payload: "T5-1");
            yield return null;

            Assert.That(stateEntered, Is.True);
            Assert.That(acceptedHandle.IsValid, Is.True);
            Assert.That(
                owner.Abilities.TryGetActiveExecutionHandle(
                    selected,
                    out AbilityExecutionHandle activeHandle),
                Is.True);
            Assert.That(activeHandle, Is.EqualTo(acceptedHandle));
            Assert.That(
                owner.Abilities.TryGetTriggerEvent(
                    activeHandle,
                    out GameplayEventData triggerEvent),
                Is.True);
            Assert.That(triggerEvent.Payload, Is.EqualTo("T5-1"));

            owner.Abilities.EndAbility(activeHandle, completed: true);
            owner.ExitTriggeredState();
            Assert.That(owner.Abilities.HasActiveAbility, Is.False);
            Assert.That(owner.IsInTriggeredState, Is.False);

            Object.Destroy(ownerObject);
            Object.Destroy(route);
            Object.Destroy(selected);
            Object.Destroy(payload);
            Object.Destroy(set);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DurationEffect는_시간_경과후_Modifier와_Tag를_정리한다()
        {
            var ownerObject = new GameObject("AbilityEffectPlayModeOwner");
            var owner = ownerObject.AddComponent<AbilityPlayModeTestActor>();
            owner.SetActorType(ActorType.Player);

            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.PlayMode.Duration";
            effect.durationType = GameplayEffectDurationType.Duration;
            effect.durationSeconds = 0.05f;
            effect.grantedTagIds.Add(GameplayTags.State_Combat_Charge);
            effect.modifiers.Add(new GameplayEffectModifierDefinition
            {
                attributeId = global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower.Value,
                modifierType = global::UPlayGround.Data.Stat.ModifierType.Flat,
                value = 5f,
            });

            float attackBefore = owner.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower);
            owner.Effects.ApplyEffect(effect, owner);

            Assert.That(owner.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.True);
            Assert.That(owner.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(attackBefore + 5f));

            yield return new WaitForSeconds(0.1f);

            Assert.That(owner.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.False);
            Assert.That(owner.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(attackBefore));

            Object.Destroy(ownerObject);
            Object.Destroy(effect);
            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator Boss_Lian_간격압박_공격트리거는_상태와_Ability를_시작한다()
        {
            const string prefabPath =
                "Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Lian_Whip.prefab";
            const string definitionPath =
                "Assets/10.Datas/Actor/DataBase/Boss/MonsterBossLian.asset";

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var definition = AssetDatabase.LoadAssetAtPath<
                global::UPlayGround.Data.Actor.ActorDefinitionSO>(definitionPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(definition, Is.Not.Null);

            GameObject targetObject = new("LianAttackTarget");
            var target = targetObject.AddComponent<AbilityPlayModeTestActor>();
            target.SetActorType(ActorType.Player);
            GameObject monsterObject = null;
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            try
            {
                monsterObject = Object.Instantiate(prefab);
                monsterObject.name = "Boss_Lian_AttackRegression";
                var monster = monsterObject.GetComponent<MonsterActor>();
                var combat = monsterObject.GetComponent<
                    global::UPlayGround.Components.EnemyCombat>();
                var detection = monsterObject.GetComponent<
                    global::UPlayGround.Components.EnemyDetection>();
                var movement = monsterObject.GetComponent<
                    global::UPlayGround.MovementController.ActorMovementController>();
                var ai = monsterObject.GetComponent<
                    global::UPlayGround.Components.EnemyAIController>();

                Assert.That(monster, Is.Not.Null);
                Assert.That(combat, Is.Not.Null);
                Assert.That(detection, Is.Not.Null);
                Assert.That(movement, Is.Not.Null);

                if (ai != null)
                    ai.enabled = false;
                monster.SetDefinition(definition);
                monsterObject.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                targetObject.transform.position = Vector3.forward * 2f;
                detection.AcquireTarget(targetObject.transform);

                float maxHealth = monster.AbilitySystem.Attributes.GetCurrent(
                    global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth);
                monster.AbilitySystem.Attributes.SetBase(
                    global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                    maxHealth * 0.5f);

                yield return null;

                // 프리팹 초기화 첫 프레임의 KCC 중력/겹침 해소가 테스트 배치 거리를 바꿀 수 있다.
                // Ability 사거리 계약을 검증하기 직전에 배치 위치를 다시 고정한다.
                monsterObject.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                targetObject.transform.position = Vector3.forward * 2f;
                Physics.SyncTransforms();

                Assert.That(
                    combat.HasAvailableSkillAtDistance(
                        2f,
                        AbilityAttackCategory.Heavy,
                        AbilityAIRole.Punish),
                    Is.True,
                    combat.BuildAbilitySelectionDiagnosticSummary());
                Assert.That(
                    global::UPlayGround.AI.BehaviorTree.EnemyAbilityTriggerTags
                        .TryResolveAttackTrigger(
                            combat.AbilitySet,
                            AbilityAttackCategory.Heavy,
                            out _,
                            out GameplayAbilitySO router,
                            out global::UPlayGround.Gameplay.Tag.GameplayTag triggerTag),
                    Is.True);

                AbilityExecutionHandle acceptedHandle = default;
                AbilityActivationResult? rejected = null;
                monster.Abilities.AbilityTriggerAccepted += (_, handle) =>
                    acceptedHandle = handle;
                monster.Abilities.AbilityTriggerRejected += (_, reason) =>
                    rejected = reason;

                combat.ReserveAttackSelection(
                    AbilityAttackCategory.Heavy,
                    AbilityAIRole.Punish);
                monster.Abilities.IssueTriggerEvent(
                    triggerTag,
                    monster,
                    target);

                Assert.That(rejected, Is.Null);
                Assert.That(router, Is.Not.Null);
                Assert.That(acceptedHandle.IsValid, Is.True);
                Assert.That(
                    movement.CurrentState?.StateId,
                    Is.EqualTo(global::UPlayGround.State.ActorStateId.Attack));
                Assert.That(combat.CurrentAbility, Is.Not.Null);
                Assert.That(combat.CurrentSkill, Is.Not.Null);
                Assert.That(combat.CurrentMotionAsset, Is.Not.Null);

                yield return null;

                Assert.That(
                    monster.Abilities.IsExecutionActive(acceptedHandle),
                    Is.True);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
                if (monsterObject != null)
                    Object.Destroy(monsterObject);
                Object.Destroy(targetObject);
            }

            yield return null;
        }
#endif
    }

    public sealed class AbilityPlayModeTestActor : GameActor
    {
        public bool IsInTriggeredState { get; private set; }

        public void SetActorType(ActorType actorType)
        {
            _actorType = actorType;
        }

        public bool TryEnterTriggeredState()
        {
            if (IsInTriggeredState)
                return false;
            IsInTriggeredState = true;
            return true;
        }

        public void ExitTriggeredState()
        {
            IsInTriggeredState = false;
        }
    }

    internal sealed class TestActorObjectService : IActorObjectService
    {
        public PlayerActor Player => null;
        public IActorInteractionService InteractionHandler => null;

        public bool CanInteract() => false;
        public void RegisterActor(GameActor actor) { }
        public void UnregisterActor(GameActor actor) { }
        public void RegisterFXInstance(GameObject instance, float lifeTime) { }

        public GameObject ShowFX(
            FXKeyType key,
            Vector3 position,
            Quaternion rotation = default,
            Transform parent = null,
            float duration = 5f) => null;

        public GameObject ShowFX(
            string key,
            Vector3 position,
            Quaternion rotation = default,
            Transform parent = null,
            float duration = 5f) => null;

        public GameObject CreateWeapon(int itemKey) => null;
        public void GrantAndPresentItems(IReadOnlyList<ItemInstance> items, Vector3 position) { }
    }
}
