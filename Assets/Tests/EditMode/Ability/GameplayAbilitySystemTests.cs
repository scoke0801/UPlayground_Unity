using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Cue;
using UPlayGround.Gameplay.Effect;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Ability.Tests
{
    public sealed class GameplayAbilitySystemTests
    {
        private GameObject _gameObject;
        private TestGameActor _actor;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("AbilityTestActor");
            _actor = _gameObject.AddComponent<TestGameActor>();
            _actor.InitializeForEditMode();
            _actor.SetActorType(ActorType.Player);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void GrantedTag_두_소스_중_하나를_제거해도_남은_소유권을_보존한다()
        {
            GameplayTagContainer tags = _actor.Tags;
            GameplayTagHandle first = tags.AddTag(
                GameplayTagId.State_Combat_Attack,
                new GameplayTagSource("Test", 1));
            GameplayTagHandle second = tags.AddTag(
                GameplayTagId.State_Combat_Attack,
                new GameplayTagSource("Test", 2));

            Assert.That(tags.HasTag(GameplayTagId.State_Combat_Attack), Is.True);
            Assert.That(tags.RemoveTag(first), Is.True);
            Assert.That(tags.HasTag(GameplayTagId.State_Combat_Attack), Is.True);
            Assert.That(tags.RemoveTag(second), Is.True);
            Assert.That(tags.HasTag(GameplayTagId.State_Combat_Attack), Is.False);
        }

        [Test]
        public void Ability_Prepare전에는_쿨다운이_시작되지_않고_Commit후에만_시작한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
            {
                slot = PlayerSkillSlot.Ability,
                ability = ability,
            });
            _actor.Abilities.SetAbilitySet(set);

            AbilityActivationResult prepared = _actor.Abilities.TryPreparePlayerSlot(
                PlayerSkillSlot.Ability,
                true,
                null,
                out var handle,
                out _);

            Assert.That(prepared, Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.TryGetPlayerSlotState(
                PlayerSkillSlot.Ability, out var beforeCommit), Is.True);
            Assert.That(beforeCommit.CooldownRemaining, Is.EqualTo(0f));

            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.TryGetPlayerSlotState(
                PlayerSkillSlot.Ability, out var afterCommit), Is.True);
            Assert.That(afterCommit.CooldownRemaining, Is.GreaterThan(0f));

            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void DurationEffect_제거시_Modifier와_GrantedTag를_함께_정리한다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.AttackUp";
            effect.durationType = GameplayEffectDurationType.Duration;
            effect.durationSeconds = 10f;
            effect.grantedTagIds.Add(GameplayTagId.State_Combat_Charge);
            effect.modifiers.Add(new GameplayEffectModifierDefinition
            {
                statType = StatType.AttackPower,
                modifierType = ModifierType.Flat,
                value = 2f,
            });

            float before = _actor.Stats.AttackPower;
            var handle = _actor.Effects.ApplyEffect(effect, _actor);
            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.True);
            Assert.That(_actor.Stats.AttackPower, Is.EqualTo(before + 2f));

            Assert.That(_actor.Effects.RemoveEffect(handle), Is.True);
            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.False);
            Assert.That(_actor.Stats.AttackPower, Is.EqualTo(before));
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Effect_HUD표시는_정의값과_적용옵션을_따른다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.HudVisibility";
            effect.durationType = GameplayEffectDurationType.Duration;
            effect.durationSeconds = 10f;
            effect.presentation.showInHud = true;

            var views = new List<GameplayEffectViewState>();
            GameplayEffectHandle shown = _actor.Effects.ApplyEffect(effect, _actor);
            _actor.Effects.CopyVisibleEffects(views);
            Assert.That(shown.IsValid, Is.True);
            Assert.That(views, Has.Count.EqualTo(1));

            _actor.Effects.RemoveEffect(shown);
            _actor.Effects.ApplyEffect(
                effect,
                _actor,
                new GameplayEffectApplicationOptions(
                    GameplayEffectHudVisibility.ForceHide));
            _actor.Effects.CopyVisibleEffects(views);
            Assert.That(views, Is.Empty);

            _actor.Effects.RemoveAll();
            effect.presentation.showInHud = false;
            _actor.Effects.ApplyEffect(
                effect,
                _actor,
                new GameplayEffectApplicationOptions(
                    GameplayEffectHudVisibility.ForceShow));
            _actor.Effects.CopyVisibleEffects(views);
            Assert.That(views, Has.Count.EqualTo(1));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void 숨김_Effect도_활성목록에서_조회하고_RuntimeId로_제거할_수_있다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.CheatRemoval";
            effect.durationType = GameplayEffectDurationType.Infinite;
            effect.presentation.showInHud = false;

            _actor.Effects.ApplyEffect(effect, _actor);
            var active = new List<GameplayEffectViewState>();
            _actor.Effects.CopyActiveEffects(active);

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(
                _actor.Effects.RemoveEffectByRuntimeId(active[0].RuntimeId),
                Is.True);
            _actor.Effects.CopyActiveEffects(active);
            Assert.That(active, Is.Empty);

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Effect_HUD강제표시정책은_저장후_복원해도_유지된다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.HudVisibilitySave";
            effect.durationType = GameplayEffectDurationType.Duration;
            effect.durationSeconds = 10f;
            effect.savePolicy = GameplayEffectSavePolicy.SaveRemainingDuration;
            effect.presentation.showInHud = false;

            _actor.Effects.ApplyEffect(
                effect,
                _actor,
                new GameplayEffectApplicationOptions(
                    GameplayEffectHudVisibility.ForceShow));
            var entries = new List<GameplayEffectSaveEntry>();
            _actor.Effects.CaptureRuntimeState(entries, forCharacterSwap: false);
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(
                entries[0].hudVisibility,
                Is.EqualTo(GameplayEffectHudVisibility.ForceShow));

            _actor.Effects.RemoveAll();
            _actor.Effects.RestoreRuntimeState(
                entries,
                effectId => effectId == effect.effectId ? effect : null);
            var views = new List<GameplayEffectViewState>();
            _actor.Effects.CopyVisibleEffects(views);
            Assert.That(views, Has.Count.EqualTo(1));

            Object.DestroyImmediate(effect);
        }

        [Test]
        public void 패시브_TriggerEffect는_정의의_HUD노출을_따르는_것이_기본이다()
        {
            PassiveAbilitySO passive =
                ScriptableObject.CreateInstance<PassiveAbilitySO>();

            Assert.That(
                passive.triggeredEffectHudVisibility,
                Is.EqualTo(GameplayEffectHudVisibility.UseDefinition));

            Object.DestroyImmediate(passive);
        }

        [Test]
        public void PersistPerCharacter_Effect는_교체시_제거되고_스택과_함께_복원된다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.PersistentAttackUp";
            effect.durationType = GameplayEffectDurationType.Duration;
            effect.durationSeconds = 20f;
            effect.stackingKey = "Effect.Test.PersistentAttackUp";
            effect.stackPolicy = GameplayEffectStackPolicy.AddStackAndRefresh;
            effect.maxStackCount = 2;
            effect.removalPolicy = GameplayEffectRemovalPolicy.PersistPerCharacter;
            effect.savePolicy = GameplayEffectSavePolicy.SaveRemainingDuration;
            effect.grantedTagIds.Add(GameplayTagId.State_Combat_Charge);
            effect.modifiers.Add(new GameplayEffectModifierDefinition
            {
                statType = StatType.AttackPower,
                modifierType = ModifierType.Flat,
                value = 3f,
            });

            GameplayAbilitySO ability = MakeAbility();
            ability.commitEffects.Add(effect);
            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            set.additionalAbilities.Add(ability);
            _actor.Abilities.SetAbilitySet(set);

            float before = _actor.Stats.AttackPower;
            _actor.Effects.ApplyEffect(effect, _actor);
            _actor.Effects.ApplyEffect(effect, _actor);
            Assert.That(_actor.Stats.AttackPower, Is.EqualTo(before + 6f));

            AbilityRuntimeSaveData snapshot =
                _actor.Abilities.CaptureRuntimeState(forCharacterSwap: true);
            Assert.That(snapshot.activeEffects, Has.Count.EqualTo(1));
            Assert.That(snapshot.activeEffects[0].stackCount, Is.EqualTo(2));

            _actor.Abilities.HandleCharacterSwap();
            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.False);
            Assert.That(_actor.Stats.AttackPower, Is.EqualTo(before));

            _actor.Abilities.RestoreRuntimeState(snapshot);
            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.True);
            Assert.That(_actor.Stats.AttackPower, Is.EqualTo(before + 6f));

            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void OwnerDeath는_활성_Effect와_GrantedTag를_모두_정리한다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.DeathCleanup";
            effect.durationType = GameplayEffectDurationType.Infinite;
            effect.grantedTagIds.Add(GameplayTagId.State_Combat_Charge);

            _actor.Effects.ApplyEffect(effect, _actor);
            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.True);

            _actor.Abilities.HandleOwnerDeath();

            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.False);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Self대상_Ability는_명시적_대상없이_소유자를_대상으로_실행한다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.SelfTarget";
            effect.durationType = GameplayEffectDurationType.Infinite;
            effect.grantedTagIds.Add(GameplayTagId.State_Combat_Charge);

            GameplayAbilitySO ability = MakeAbility();
            ability.activation.targetPolicy = AbilityTargetPolicy.Required;
            ability.activation.targetRelation = AbilityTargetRelation.Self;
            ability.variants[0].targetEffects.Add(effect);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            AbilityActivationResult prepared = _actor.Abilities.TryPreparePlayerSlot(
                PlayerSkillSlot.Ability,
                true,
                null,
                out AbilityExecutionHandle handle,
                out _);

            Assert.That(prepared, Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(handle), Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Tags.HasTag(GameplayTagId.State_Combat_Charge), Is.True);

            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Enemy대상_Ability는_아군을_거부하고_적군을_허용한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.targetPolicy = AbilityTargetPolicy.Required;
            ability.activation.targetRelation = AbilityTargetRelation.Enemy;
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            GameObject allyObject = new GameObject("AbilityTestAlly");
            GameObject enemyObject = new GameObject("AbilityTestEnemy");
            try
            {
                TestGameActor ally = allyObject.AddComponent<TestGameActor>();
                ally.SetActorType(ActorType.Player);
                TestGameActor enemy = enemyObject.AddComponent<TestGameActor>();
                enemy.SetActorType(ActorType.Monster);

                Assert.That(
                    _actor.Abilities.EvaluatePlayerSlot(
                        PlayerSkillSlot.Ability, true, ally, out _),
                    Is.EqualTo(AbilityActivationResult.InvalidTarget));
                Assert.That(
                    _actor.Abilities.EvaluatePlayerSlot(
                        PlayerSkillSlot.Ability, true, enemy, out _),
                    Is.EqualTo(AbilityActivationResult.Success));
            }
            finally
            {
                Object.DestroyImmediate(allyObject);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(ability);
                Object.DestroyImmediate(set);
            }
        }

        [Test]
        public void Cue는_실패_시작_종료를_계산과_분리된_이벤트로_전달한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.requiredTagIds.Add(GameplayTagId.State_Combat_Charge);
            ability.cues.failureCueId = "Cue.Test.Failed";
            ability.cues.startCueId = "Cue.Test.Started";
            ability.cues.endCueId = "Cue.Test.Ended";
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            var received = new System.Collections.Generic.List<AbilityCueEvent>();
            GameplayCueDispatcher dispatcher =
                _gameObject.GetComponent<GameplayCueDispatcher>();
            dispatcher.CueDispatched += cue => received.Add(cue);

            Assert.That(
                _actor.Abilities.TryPreparePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out _, out _),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            _actor.Tags.AddTag(
                GameplayTagId.State_Combat_Charge,
                new GameplayTagSource("Test", 10));
            Assert.That(
                _actor.Abilities.TryPreparePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out var rejected, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            _actor.Abilities.Abort(
                rejected,
                AbilityActivationResult.MissingExecutionData);

            Assert.That(
                _actor.Abilities.TryPreparePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out var handle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(
                _actor.Abilities.Commit(handle),
                Is.EqualTo(AbilityActivationResult.Success));
            _actor.Abilities.EndActivePlayerAbility(completed: true);

            Assert.That(received, Has.Count.EqualTo(4));
            Assert.That(received[0].EventType, Is.EqualTo(AbilityCueEventType.Failed));
            Assert.That(received[0].Result, Is.EqualTo(AbilityActivationResult.MissingRequiredTag));
            Assert.That(received[1].EventType, Is.EqualTo(AbilityCueEventType.Failed));
            Assert.That(received[1].Result, Is.EqualTo(AbilityActivationResult.MissingExecutionData));
            Assert.That(received[2].EventType, Is.EqualTo(AbilityCueEventType.Started));
            Assert.That(received[3].EventType, Is.EqualTo(AbilityCueEventType.Ended));

            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 저장정책이_허용한_쿨다운만_RuntimeSaveData에_포함한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.persistence.saveCooldown = false;
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(
                _actor.Abilities.TryPreparePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out var handle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(
                _actor.Abilities.Commit(handle),
                Is.EqualTo(AbilityActivationResult.Success));

            Assert.That(_actor.Abilities.CaptureRuntimeState().cooldowns, Is.Empty);
            ability.persistence.saveCooldown = true;
            Assert.That(_actor.Abilities.CaptureRuntimeState().cooldowns, Has.Count.EqualTo(1));

            _actor.Abilities.EndActivePlayerAbility(completed: true);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void AbilityRuntimeSaveData는_안정값만_JSON으로_직렬화_복원한다()
        {
            var data = new AbilityRuntimeSaveData();
            data.resources.Add(new AbilityResourceSaveEntry
            {
                resourceType = AbilityResourceType.UltimateEnergy,
                currentValue = 42f,
            });
            data.cooldowns.Add(new AbilityCooldownSaveEntry
            {
                cooldownGroupId = "Cooldown.Test",
                remainingSeconds = 1.5f,
            });
            data.activeEffects.Add(new GameplayEffectSaveEntry
            {
                effectId = "Effect.Test.Save",
                sourceActorId = "Actor.Test",
                remainingSeconds = 3f,
                stackCount = 2,
            });

            string json = JsonUtility.ToJson(data);
            AbilityRuntimeSaveData restored =
                JsonUtility.FromJson<AbilityRuntimeSaveData>(json);

            Assert.That(restored.version, Is.EqualTo(1));
            Assert.That(restored.resources[0].currentValue, Is.EqualTo(42f));
            Assert.That(restored.cooldowns[0].cooldownGroupId, Is.EqualTo("Cooldown.Test"));
            Assert.That(restored.activeEffects[0].stackCount, Is.EqualTo(2));
        }

        private static AbilitySetSO MakeSet(GameplayAbilitySO ability)
        {
            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
            {
                slot = PlayerSkillSlot.Ability,
                ability = ability,
            });
            return set;
        }

        private static GameplayAbilitySO MakeAbility()
        {
            GameplayAbilitySO ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Test.Basic";
            ability.cooldown.durationSeconds = 2f;
            var payload =
                ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
            payload.executionId = ability.abilityId;
            payload.animKey = AnimKey.Attack_1;
            payload.attackInfo = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase { animKey = AnimKey.Attack_1 },
            };
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Ground",
                priority = 1,
                executionPayload = payload,
            });
            return ability;
        }
    }

    public sealed class TestGameActor : GameActor
    {
        public void InitializeForEditMode()
        {
            base.Awake();
            Stats.Init(null);
            Effects.Initialize(this);
            Abilities.Initialize(this);
        }

        public void SetActorType(ActorType actorType)
        {
            _actorType = actorType;
        }
    }
}
