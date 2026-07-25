using System.Collections;
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
using UPlayGround.Gameplay.Cue;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

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
            payload.executionId = "Execution.Test.PlayMode";
            var motionAsset = ScriptableObject.CreateInstance<MotionSetAsset>();
            var motionRef = ScriptableObject.CreateInstance<MotionReferenceSO>();
            motionRef.defaultMotion = motionAsset;
            payload.attackInfo = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase { motionRef = motionRef },
            };

            GameplayAbilitySO ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Test.PlayMode.VerticalSlice";
            var task = ScriptableObject.CreateInstance<WaitDelayTaskDefinitionSO>();
            task.duration = 999f;
            ability.taskGraph = AbilityTaskGraphSO.CreateTransient(task);
            ability.activation.targetRelation = AbilityTargetRelation.Self;
            ability.activation.executionGrantedTagIds.Add(GameplayTags.State_Combat_Attack);
            ability.cooldown.durationSeconds = 0.1f;
            ability.cues.startCueId = "Cue.Test.PlayMode.Start";
            ability.cues.endCueId = "Cue.Test.PlayMode.End";
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

            var cues = new System.Collections.Generic.List<AbilityCueEvent>();
            ownerObject.GetComponent<GameplayCueDispatcher>().CueDispatched += cue => cues.Add(cue);

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
                        variant, WeaponType.NoWeapon, out MotionSetAsset resolvedMotion, out AbilityAttackInfo attackInfo),
                    Is.True);
                Assert.That(resolvedMotion, Is.SameAs(motionAsset));
                Assert.That(attackInfo, Is.SameAs(payload.attackInfo));

                // 실제 프로젝트에서는 이 사이에서 상태 전환과 MotionSet 시작 승인을 수행한다.
                yield return null;

                Assert.That(
                    owner.Abilities.Commit(handle),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(owner.Abilities.HasActivePlayerAbility, Is.True);
                Assert.That(owner.Tags.HasTag(GameplayTags.State_Combat_Attack), Is.True);
                Assert.That(cues, Has.Count.EqualTo(1));
                Assert.That(cues[0].EventType, Is.EqualTo(AbilityCueEventType.Started));

                owner.Abilities.EndActivePlayerAbility(completed: true);

                Assert.That(owner.Abilities.HasActivePlayerAbility, Is.False);
                Assert.That(owner.Tags.HasTag(GameplayTags.State_Combat_Attack), Is.False);
                Assert.That(cues, Has.Count.EqualTo(2));
                Assert.That(cues[1].EventType, Is.EqualTo(AbilityCueEventType.Ended));
            }
            finally
            {
                Object.Destroy(ownerObject);
                Object.Destroy(payload);
                Object.Destroy(motionRef);
                Object.Destroy(motionAsset);
                Object.Destroy(ability);
                Object.Destroy(set);
            }

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
    }

    public sealed class AbilityPlayModeTestActor : GameActor
    {
        public void SetActorType(ActorType actorType)
        {
            _actorType = actorType;
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
        public void SpawnItem(ItemInstance itemInstance, Vector3 position) { }
    }
}
