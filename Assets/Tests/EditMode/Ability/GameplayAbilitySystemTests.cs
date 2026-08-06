using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Editor.Ability;
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
        public void AttributeProfile은_안정_ID_기본값을_원자적으로_적용한다()
        {
            AttributeProfileSO profile = ScriptableObject.CreateInstance<AttributeProfileSO>();
            profile.EditorReplace(
                new[]
                {
                    new AttributeProfileEntry(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, 250f),
                    new AttributeProfileEntry(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 17f),
                });

            try
            {
                Assert.That(
                    _actor.AbilitySystem.InitializeAttributes(profile, out string error),
                    Is.True,
                    error);
                Assert.That(
                    _actor.AbilitySystem.Attributes.GetBase(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth),
                    Is.EqualTo(250f));
                Assert.That(
                    _actor.AbilitySystem.Attributes.GetBase(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower),
                    Is.EqualTo(17f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void AttributeProfile은_중복_ID를_적용하지_않는다()
        {
            float before = _actor.AbilitySystem.Attributes.GetBase(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower);
            AttributeProfileSO profile = ScriptableObject.CreateInstance<AttributeProfileSO>();
            profile.EditorReplace(
                new[]
                {
                    new AttributeProfileEntry(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 10f),
                    new AttributeProfileEntry(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 20f),
                });

            try
            {
                Assert.That(
                    _actor.AbilitySystem.InitializeAttributes(profile, out string error),
                    Is.False);
                Assert.That(error, Does.Contain("중복"));
                Assert.That(
                    _actor.AbilitySystem.Attributes.GetBase(
                        global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower),
                    Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GrantedTag_두_소스_중_하나를_제거해도_남은_소유권을_보존한다()
        {
            GameplayTagContainer tags = _actor.Tags;
            GameplayTagHandle first = tags.AddTag(
                GameplayTags.State_Combat_Attack,
                new GameplayTagSource("Test", 1));
            GameplayTagHandle second = tags.AddTag(
                GameplayTags.State_Combat_Attack,
                new GameplayTagSource("Test", 2));

            Assert.That(tags.HasTag(GameplayTags.State_Combat_Attack), Is.True);
            Assert.That(_actor.AbilitySystem.Tags.Has(
                new AbilityTagId(GameplayTags.State_Combat_Attack.TagName)), Is.True);
            Assert.That(tags.RemoveTag(first), Is.True);
            Assert.That(tags.HasTag(GameplayTags.State_Combat_Attack), Is.True);
            Assert.That(tags.RemoveTag(second), Is.True);
            Assert.That(tags.HasTag(GameplayTags.State_Combat_Attack), Is.False);
            Assert.That(_actor.AbilitySystem.Tags.Has(
                new AbilityTagId(GameplayTags.State_Combat_Attack.TagName)), Is.False);
        }

        [Test]
        public void 명시_태그도_중복_추가_후_한번_제거하면_GAS_소유권을_보존한다()
        {
            _actor.Tags.AddTag(GameplayTags.State_Hit);
            _actor.Tags.AddTag(GameplayTags.State_Hit);

            _actor.Tags.RemoveTag(GameplayTags.State_Hit);

            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Hit), Is.True);
            Assert.That(_actor.AbilitySystem.Tags.Has(
                new AbilityTagId(GameplayTags.State_Hit.TagName)), Is.True);
            _actor.Tags.RemoveTag(GameplayTags.State_Hit);
            Assert.That(_actor.AbilitySystem.Tags.Has(
                new AbilityTagId(GameplayTags.State_Hit.TagName)), Is.False);
        }

        [Test]
        public void ComboRoute의_requireAny는_하나의_태그만_있어도_통과한다()
        {
            var route = new ComboRouteEntry();
            route.tagRequirement.requireAny.Add(GameplayTags.State_Hit);
            route.tagRequirement.requireAny.Add(GameplayTags.State_Dodge);

            Assert.That(route.CheckTagConditions(_actor.Tags), Is.False);
            _actor.Tags.AddTag(GameplayTags.State_Dodge);
            Assert.That(route.CheckTagConditions(_actor.Tags), Is.True);
            _actor.Tags.RemoveTag(GameplayTags.State_Dodge);
        }

        [Test]
        public void ComboRoute는_태그컨테이너가_없으면_신규필수태그를_통과시키지않는다()
        {
            var requiredRoute = new ComboRouteEntry();
            requiredRoute.tagRequirement.requireAny.Add(GameplayTags.State_Dodge);
            var blockedOnlyRoute = new ComboRouteEntry();
            blockedOnlyRoute.tagRequirement.blockAny.Add(GameplayTags.State_Hit);

            Assert.That(requiredRoute.CheckTagConditions(null), Is.False);
            Assert.That(blockedOnlyRoute.CheckTagConditions(null), Is.True);
        }

        [Test]
        public void Owner태그_requireAny는_하나만_보유해도_활성화를_허용한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.ownerTagRequirement.requireAny.Add(GameplayTags.State_Hit);
            ability.activation.ownerTagRequirement.requireAny.Add(GameplayTags.State_Dodge);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(
                _actor.Abilities.EvaluatePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            GameplayTagHandle handle = _actor.Tags.AddTag(
                GameplayTags.State_Dodge,
                new GameplayTagSource("Test", 1));
            Assert.That(
                _actor.Abilities.EvaluatePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.Success));

            _actor.Tags.RemoveTag(handle);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Owner태그_Exact는_하위_계층_태그를_조건_충족으로_보지_않는다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.ownerTagRequirement.requireAll.Add(GameplayTags.State_Combat);
            ability.activation.ownerTagRequirement.matchMode = AbilityTagMatchMode.Exact;
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            GameplayTagHandle handle = _actor.Tags.AddTag(
                GameplayTags.State_Combat_Attack,
                new GameplayTagSource("Test", 1));

            Assert.That(
                _actor.Abilities.EvaluatePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            _actor.Tags.RemoveTag(handle);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 레거시_requiredTagIds는_기존처럼_계층_태그를_허용한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.requiredTagIds.Add(GameplayTags.State_Combat);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(
                _actor.Abilities.EvaluatePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            GameplayTagHandle handle = _actor.Tags.AddTag(
                GameplayTags.State_Combat_Attack,
                new GameplayTagSource("Test", 1));
            Assert.That(
                _actor.Abilities.EvaluatePlayerSlot(
                    PlayerSkillSlot.Ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.Success));

            _actor.Tags.RemoveTag(handle);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void OwnedTagAdded_Immediate는_Background_Ability를_활성화한다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.OwnedTagAdded);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 1));

            Assert.That(_actor.Abilities.HasActiveAbility, Is.True);
            Assert.That(
                _actor.Abilities.TryGetActiveExecutionHandle(ability, out _),
                Is.True);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 태그_트리거도_owner_blockAny_조건을_우회하지_않는다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.OwnedTagAdded);
            ability.activation.ownerTagRequirement.blockAny.Add(GameplayTags.State_Dodge);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            _actor.Tags.AddTag(
                GameplayTags.State_Dodge,
                new GameplayTagSource("Test", 1));

            _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 2));

            Assert.That(_actor.Abilities.HasActiveAbility, Is.False);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Exact_트리거는_하위_계층_태그로_발화하지_않는다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Combat,
                AbilityTriggerSource.OwnedTagAdded);
            ability.triggers[0].matchMode = AbilityTagMatchMode.Exact;
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            _actor.Tags.AddTag(
                GameplayTags.State_Combat_Attack,
                new GameplayTagSource("Test", 1));

            Assert.That(_actor.Abilities.HasActiveAbility, Is.False);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 자기_부여_태그를_트리거로_사용해도_순환_활성화하지_않는다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.OwnedTagAdded);
            ability.activation.executionGrantedTagIds.Add(GameplayTags.State_Hit);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 1));

            Assert.That(_actor.Abilities.HasActiveAbility, Is.False);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void retriggerInterval_안의_재부여는_재활성화하지_않는다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.OwnedTagAdded);
            ability.triggers[0].retriggerIntervalSeconds = 10f;
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            GameplayTagHandle first = _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 1));
            Assert.That(
                _actor.Abilities.TryGetActiveExecutionHandle(
                    ability, out AbilityExecutionHandle execution),
                Is.True);
            _actor.Abilities.EndAbility(execution, completed: true);
            _actor.Tags.RemoveTag(first);

            _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 2));

            Assert.That(_actor.Abilities.HasActiveAbility, Is.False);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void OwnedTagPresent는_태그가_붙으면_활성화하고_사라지면_취소한다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.OwnedTagPresent);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            GameplayTagHandle tagHandle = _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 1));
            Assert.That(_actor.Abilities.HasActiveAbility, Is.True);

            _actor.Tags.RemoveTag(tagHandle);

            Assert.That(_actor.Abilities.HasActiveAbility, Is.False);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void AbilitySet_교체후에는_이전_Set의_트리거가_발화하지_않는다()
        {
            GameplayAbilitySO previousAbility = MakeAbility();
            previousAbility.abilityId = "Ability.Test.PreviousCharacterTrigger";
            previousAbility.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Player_Hit_Light,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            GameplayAbilitySO currentAbility = MakeAbility();
            currentAbility.abilityId = "Ability.Test.CurrentCharacter";
            AbilitySetSO previousSet = MakeSet(previousAbility);
            AbilitySetSO currentSet = MakeSet(currentAbility);

            _actor.Abilities.SetAbilitySet(previousSet);
            _actor.Abilities.SetAbilitySet(currentSet);
            int requestCount = 0;
            _actor.Abilities.AbilityTriggerRequested += _ => requestCount++;

            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Player_Hit_Light);

            Assert.That(requestCount, Is.Zero);
            Object.DestroyImmediate(previousAbility);
            Object.DestroyImmediate(currentAbility);
            Object.DestroyImmediate(previousSet);
            Object.DestroyImmediate(currentSet);
        }

        [Test]
        public void GameplayEffect_GrantedTag도_Immediate_트리거를_발화한다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.OwnedTagAdded);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.TriggerTag";
            effect.durationType = GameplayEffectDurationType.Infinite;
            effect.grantedTagIds.Add(GameplayTags.State_Hit);

            _actor.Effects.ApplyEffect(effect, _actor);

            Assert.That(_actor.Abilities.HasActiveAbility, Is.True);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void GameplayEvent_트리거는_Instigator를_실행에_보존한다()
        {
            GameplayAbilitySO ability = MakeTriggeredBackgroundAbility(
                GameplayTags.State_Hit,
                AbilityTriggerSource.GameplayEvent);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            AbilitySystemHandle instigator = _actor.AbilitySystem.Runtime.Handle;
            var eventData = new GameplayEventData(
                new AbilityTagId(GameplayTags.State_Hit.TagName),
                instigator,
                _actor.AbilitySystem.Runtime.Handle);

            _actor.AbilitySystem.Runtime.Events.Send(eventData);

            Assert.That(
                _actor.Abilities.TryGetActiveExecutionHandle(
                    ability, out AbilityExecutionHandle execution),
                Is.True);
            Assert.That(
                _actor.Abilities.TryGetTriggerEvent(execution, out GameplayEventData stored),
                Is.True);
            Assert.That(stored.Instigator, Is.EqualTo(instigator));
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 같은프레임의_동일_GameplayEvent_2회는_사건을_각각_발급한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.abilityId = "Ability.Test.RepeatedHitEvent";
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Monster_Hit_Light,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            int eventCount = 0;
            _actor.AbilitySystem.Runtime.Events.EventSent += data =>
            {
                if (data.EventTag.Value
                    == GameplayTags.Trigger_Monster_Hit_Light.TagName)
                    eventCount++;
            };

            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Light);
            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Light);

            Assert.That(eventCount, Is.EqualTo(2));
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Request트리거는_선택된_다른Ability_실행핸들에_귀속할수있다()
        {
            GameplayTag triggerTag = GameplayTags.Trigger_Monster_Attack_Basic;
            GameplayAbilitySO routeAbility = MakeAbility();
            routeAbility.abilityId = "Ability.Test.Route.Basic";
            routeAbility.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = triggerTag,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            GameplayAbilitySO selectedAbility = MakeAbility();
            selectedAbility.abilityId = "Ability.Test.Selected.Basic";
            AbilitySetSO set = MakeSet(routeAbility);
            set.additionalAbilities.Add(selectedAbility);
            _actor.Abilities.SetAbilitySet(set);

            AbilityExecutionHandle acceptedHandle = default;
            GameplayAbilitySO acceptedRoute = null;
            _actor.Abilities.AbilityTriggerAccepted += (request, handle) =>
            {
                acceptedRoute = request.Ability;
                acceptedHandle = handle;
            };
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                Assert.That(
                    _actor.Abilities.TryPrepareAbility(
                        selectedAbility,
                        true,
                        null,
                        out AbilityExecutionHandle handle,
                        out _,
                        request.TriggerEvent),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(
                    _actor.Abilities.Commit(handle),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(
                    _actor.Abilities.BindActiveExecutionToTrigger(handle, request),
                    Is.True);
            };

            Assert.That(
                _actor.Abilities.TryGetRequestTriggerAbility(
                    triggerTag,
                    out GameplayAbilitySO found),
                Is.True);
            Assert.That(found, Is.SameAs(routeAbility));
            _actor.Abilities.IssueTriggerEvent(triggerTag);

            Assert.That(acceptedRoute, Is.SameAs(routeAbility));
            Assert.That(acceptedHandle.IsValid, Is.True);
            Assert.That(
                _actor.Abilities.TryGetActiveExecutionHandle(
                    selectedAbility,
                    out AbilityExecutionHandle active),
                Is.True);
            Assert.That(active, Is.EqualTo(acceptedHandle));

            Object.DestroyImmediate(selectedAbility);
            Object.DestroyImmediate(routeAbility);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Request전용Ability는_TaskGraph와_Payload없이_검증을_통과한다()
        {
            GameplayAbilitySO ability =
                ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Test.ExternalRequest";
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                priority = 1,
            });
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Player_Hit_Light,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });

            List<AbilityValidationIssue> issues =
                AbilityDataValidator.Validate(ability);

            Assert.That(issues.Exists(issue =>
                issue.Severity == AbilityValidationSeverity.Error
                && (issue.Message.Contains("Task Graph")
                    || issue.Message.Contains("Payload")
                    || issue.Message.Contains("실행 가능한 Variant"))), Is.False);
            Object.DestroyImmediate(ability);
        }

        [Test]
        public void Request전용Ability는_TaskGraph와_Payload없이_요청을_발급한다()
        {
            GameplayAbilitySO ability =
                ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Test.ExternalRequest.Runtime";
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                priority = 1,
            });
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Player_Hit_Light,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            int requestCount = 0;
            AbilityVariantDefinition requestedVariant = null;
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                requestCount++;
                requestedVariant = request.Variant;
            };

            _actor.Abilities.IssueTriggerEvent(
                GameplayTags.Trigger_Player_Hit_Light);

            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(requestedVariant, Is.SameAs(ability.variants[0]));
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 입력슬롯_Ability에_Request트리거를_함께_지정하면_검증오류다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.abilityId = "Ability.Test.InputAndTrigger";
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Player_Hit_Light,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            AbilitySetSO set = MakeSet(ability);
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
            {
                slot = PlayerSkillSlot.Ability,
                ability = ability,
            });

            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(set);

            Assert.That(issues.Exists(issue =>
                issue.Severity == AbilityValidationSeverity.Error
                && issue.Message.Contains("중복 실행")), Is.True);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Request트리거는_선점허용이_없으면_기존주실행중_거부된다()
        {
            GameplayAbilitySO current = MakeAbility();
            current.abilityId = "Ability.Test.Current";
            GameplayAbilitySO reaction = MakeAbility();
            reaction.abilityId = "Ability.Test.Reaction";
            reaction.concurrency = AbilityConcurrencyPolicy.CancelExisting;
            reaction.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Monster_Hit_Heavy,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            AbilitySetSO set = MakeSet(current);
            set.additionalAbilities.Add(reaction);
            _actor.Abilities.SetAbilitySet(set);
            Assert.That(
                _actor.Abilities.TryPrepareAbility(
                    current, true, null, out AbilityExecutionHandle handle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(
                _actor.Abilities.Commit(handle),
                Is.EqualTo(AbilityActivationResult.Success));

            int requestCount = 0;
            AbilityActivationResult? rejection = null;
            _actor.Abilities.AbilityTriggerRequested += _ => requestCount++;
            _actor.Abilities.AbilityTriggerRejected += (_, result) =>
                rejection = result;
            _actor.Abilities.IssueTriggerEvent(
                GameplayTags.Trigger_Monster_Hit_Heavy);

            Assert.That(requestCount, Is.Zero);
            Assert.That(rejection, Is.EqualTo(AbilityActivationResult.ConflictingAbility));
            Object.DestroyImmediate(reaction);
            Object.DestroyImmediate(current);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 선점허용_Request트리거는_기존주실행중에도_구독자에게_전달된다()
        {
            GameplayAbilitySO current = MakeAbility();
            current.abilityId = "Ability.Test.Current";
            GameplayAbilitySO reaction = MakeAbility();
            reaction.abilityId = "Ability.Test.Reaction";
            reaction.concurrency = AbilityConcurrencyPolicy.CancelExisting;
            reaction.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.Trigger_Monster_Hit_Heavy,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
                allowPreemption = true,
            });
            AbilitySetSO set = MakeSet(current);
            set.additionalAbilities.Add(reaction);
            _actor.Abilities.SetAbilitySet(set);
            Assert.That(
                _actor.Abilities.TryPrepareAbility(
                    current, true, null, out AbilityExecutionHandle handle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(
                _actor.Abilities.Commit(handle),
                Is.EqualTo(AbilityActivationResult.Success));

            int requestCount = 0;
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                if (request.Ability == reaction)
                    requestCount++;
            };
            _actor.Abilities.IssueTriggerEvent(
                GameplayTags.Trigger_Monster_Hit_Heavy);

            Assert.That(requestCount, Is.EqualTo(1));
            Object.DestroyImmediate(reaction);
            Object.DestroyImmediate(current);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Target의_blockAny_태그는_활성화를_차단한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.targetPolicy = AbilityTargetPolicy.Required;
            ability.activation.targetRelation = AbilityTargetRelation.Self;
            ability.activation.targetTagRequirement.blockAny.Add(GameplayTags.State_Hit);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            GameplayTagHandle tagHandle = _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 71));

            Assert.That(_actor.Abilities.EvaluateAbility(
                ability, true, _actor, out _),
                Is.EqualTo(AbilityActivationResult.BlockedByTag));

            _actor.Tags.RemoveTag(tagHandle);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void GameplayEvent_Instigator의_requireAll_미충족은_활성화를_차단한다()
        {
            var sourceObject = new GameObject("AbilitySource");
            var source = sourceObject.AddComponent<TestGameActor>();
            source.InitializeForEditMode();
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.sourceTagRequirement.requireAll.Add(GameplayTags.State_Hit);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            var eventData = new GameplayEventData(
                new AbilityTagId(GameplayTags.State_Combat.TagName),
                source.AbilitySystem.Runtime.Handle,
                _actor.AbilitySystem.Runtime.Handle);

            Assert.That(_actor.Abilities.EvaluateAbility(
                ability, true, null, out _, eventData),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void GameplayEvent_Target의_blockAny는_targetPolicy_None에서도_검사한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.targetTagRequirement.blockAny.Add(GameplayTags.State_Hit);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            GameplayTagHandle tagHandle = _actor.Tags.AddTag(
                GameplayTags.State_Hit,
                new GameplayTagSource("Test", 72));
            var eventData = new GameplayEventData(
                new AbilityTagId(GameplayTags.State_Combat.TagName),
                _actor.AbilitySystem.Runtime.Handle,
                _actor.AbilitySystem.Runtime.Handle);

            Assert.That(_actor.Abilities.EvaluateAbility(
                ability, true, null, out _, eventData),
                Is.EqualTo(AbilityActivationResult.BlockedByTag));

            _actor.Tags.RemoveTag(tagHandle);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Source_태그요구사항은_GameplayEvent가_없으면_실패한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.sourceTagRequirement.requireAll.Add(
                GameplayTags.State_Hit);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(
                _actor.Abilities.EvaluateAbility(ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Target_태그요구사항은_대상컨텍스트가_없으면_실패한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.activation.targetTagRequirement.requireAll.Add(
                GameplayTags.State_Hit);
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(
                _actor.Abilities.EvaluateAbility(ability, true, null, out _),
                Is.EqualTo(AbilityActivationResult.MissingRequiredTag));

            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Validator는_Immediate와_비Background_조합을_Error로_보고한다()
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.State_Hit,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Immediate,
            });

            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(ability);

            Assert.That(issues.Exists(issue =>
                issue.Severity == AbilityValidationSeverity.Error
                && issue.Message.Contains("Background")), Is.True);
            Object.DestroyImmediate(ability);
        }

        [Test]
        public void Commit은_태그가_일치하는_활성_Ability를_취소한다()
        {
            GameplayAbilitySO incoming = MakeAbility();
            incoming.abilityId = "Ability.Test.CancelIncoming";
            incoming.cancelAbilitiesWithTag.Add(GameplayTags.State_Combat);
            GameplayAbilitySO existing = MakeAbility();
            existing.abilityId = "Ability.Test.CancelExisting";
            existing.concurrency = AbilityConcurrencyPolicy.Background;
            existing.persistence.backgroundMaxDurationSeconds = 30f;
            existing.abilityTagIds.Add(GameplayTags.State_Combat_Attack);
            AbilitySetSO set = MakeSet(incoming);
            set.additionalAbilities.Add(existing);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(_actor.Abilities.TryPrepareAbility(
                existing, true, null, out AbilityExecutionHandle existingHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(existingHandle),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.TryPrepareAbility(
                incoming, true, null, out AbilityExecutionHandle incomingHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));

            Assert.That(_actor.Abilities.Commit(incomingHandle),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.IsExecutionActive(existingHandle), Is.False);
            Assert.That(_actor.Abilities.IsExecutionActive(incomingHandle), Is.True);

            Object.DestroyImmediate(existing);
            Object.DestroyImmediate(incoming);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 활성_Ability의_Block태그는_계층_일치하는_새_Ability를_차단한다()
        {
            GameplayAbilitySO candidate = MakeAbility();
            candidate.abilityId = "Ability.Test.BlockCandidate";
            candidate.abilityTagIds.Add(GameplayTags.State_Combat_Attack);
            GameplayAbilitySO blocker = MakeAbility();
            blocker.abilityId = "Ability.Test.BlockOwner";
            blocker.concurrency = AbilityConcurrencyPolicy.Background;
            blocker.persistence.backgroundMaxDurationSeconds = 30f;
            blocker.blockAbilitiesWithTag.Add(GameplayTags.State_Combat);
            AbilitySetSO set = MakeSet(candidate);
            set.additionalAbilities.Add(blocker);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(_actor.Abilities.TryPrepareAbility(
                blocker, true, null, out AbilityExecutionHandle blockerHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(blockerHandle),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.EvaluateAbility(
                candidate, true, null, out _),
                Is.EqualTo(AbilityActivationResult.BlockedByActiveAbility));

            _actor.Abilities.EndAbility(blockerHandle, false);
            Assert.That(_actor.Abilities.EvaluateAbility(
                candidate, true, null, out _),
                Is.EqualTo(AbilityActivationResult.Success));

            Object.DestroyImmediate(blocker);
            Object.DestroyImmediate(candidate);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Prepare후_생긴_Block태그는_Commit에서_재검사한다()
        {
            GameplayAbilitySO candidate = MakeAbility();
            candidate.abilityId = "Ability.Test.CommitRecheck";
            candidate.abilityTagIds.Add(GameplayTags.State_Combat_Attack);
            GameplayAbilitySO blocker = MakeAbility();
            blocker.abilityId = "Ability.Test.CommitBlocker";
            blocker.concurrency = AbilityConcurrencyPolicy.Background;
            blocker.persistence.backgroundMaxDurationSeconds = 30f;
            blocker.blockAbilitiesWithTag.Add(GameplayTags.State_Combat);
            AbilitySetSO set = MakeSet(candidate);
            set.additionalAbilities.Add(blocker);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(_actor.Abilities.TryPrepareAbility(
                candidate, true, null, out AbilityExecutionHandle candidateHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.TryPrepareAbility(
                blocker, true, null, out AbilityExecutionHandle blockerHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(blockerHandle),
                Is.EqualTo(AbilityActivationResult.Success));

            Assert.That(_actor.Abilities.Commit(candidateHandle),
                Is.EqualTo(AbilityActivationResult.BlockedByActiveAbility));
            Assert.That(_actor.Abilities.IsExecutionActive(candidateHandle), Is.False);

            Object.DestroyImmediate(blocker);
            Object.DestroyImmediate(candidate);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 중복_Block태그는_모든_소유_Ability가_끝날_때까지_유지된다()
        {
            GameplayAbilitySO candidate = MakeAbility();
            candidate.abilityId = "Ability.Test.RefCountCandidate";
            candidate.abilityTagIds.Add(GameplayTags.State_Combat_Attack);
            GameplayAbilitySO first = MakeAbility();
            first.abilityId = "Ability.Test.RefCountFirst";
            first.concurrency = AbilityConcurrencyPolicy.Background;
            first.persistence.backgroundMaxDurationSeconds = 30f;
            first.blockAbilitiesWithTag.Add(GameplayTags.State_Combat);
            GameplayAbilitySO second = MakeAbility();
            second.abilityId = "Ability.Test.RefCountSecond";
            second.concurrency = AbilityConcurrencyPolicy.Background;
            second.persistence.backgroundMaxDurationSeconds = 30f;
            second.blockAbilitiesWithTag.Add(GameplayTags.State_Combat);
            AbilitySetSO set = MakeSet(candidate);
            set.additionalAbilities.Add(first);
            set.additionalAbilities.Add(second);
            _actor.Abilities.SetAbilitySet(set);

            Assert.That(_actor.Abilities.TryPrepareAbility(
                first, true, null, out AbilityExecutionHandle firstHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(firstHandle),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.TryPrepareAbility(
                second, true, null, out AbilityExecutionHandle secondHandle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(secondHandle),
                Is.EqualTo(AbilityActivationResult.Success));

            _actor.Abilities.EndAbility(firstHandle, false);
            Assert.That(_actor.Abilities.EvaluateAbility(candidate, true, null, out _),
                Is.EqualTo(AbilityActivationResult.BlockedByActiveAbility));
            _actor.Abilities.EndAbility(secondHandle, false);
            Assert.That(_actor.Abilities.EvaluateAbility(candidate, true, null, out _),
                Is.EqualTo(AbilityActivationResult.Success));

            Object.DestroyImmediate(second);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(candidate);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Abort는_이미_Commit된_실행을_제거하지_않는다()
        {
            GameplayAbilitySO ability = MakeAbility();
            AbilitySetSO set = MakeSet(ability);
            _actor.Abilities.SetAbilitySet(set);
            Assert.That(_actor.Abilities.TryPrepareAbility(
                ability, true, null, out AbilityExecutionHandle handle, out _),
                Is.EqualTo(AbilityActivationResult.Success));
            Assert.That(_actor.Abilities.Commit(handle),
                Is.EqualTo(AbilityActivationResult.Success));

            _actor.Abilities.Abort(handle);

            Assert.That(_actor.Abilities.IsExecutionActive(handle), Is.True);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
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
            string cooldownGroup = ability.cooldown.ResolveGroupId(ability.abilityId);
            Assert.That(
                _actor.AbilitySystem.Runtime.Cooldowns.GetRemaining(cooldownGroup),
                Is.EqualTo(afterCommit.CooldownRemaining).Within(0.01f));

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
            effect.grantedTagIds.Add(GameplayTags.State_Combat_Charge);
            effect.modifiers.Add(new GameplayEffectModifierDefinition
            {
                attributeId = global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower.Value,
                modifierType = ModifierType.Flat,
                value = 2f,
            });

            float before = _actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower);
            var handle = _actor.Effects.ApplyEffect(effect, _actor);
            Assert.That(_actor.AbilitySystem.Effects.Count, Is.EqualTo(1));
            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.True);
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(before + 2f));

            Assert.That(_actor.Effects.RemoveEffect(handle), Is.True);
            Assert.That(_actor.AbilitySystem.Effects.Count, Is.EqualTo(0));
            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.False);
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(before));
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Instant_회복은_EffectSpec_Execution으로_적용된다()
        {
            _actor.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, 50f);
            _actor.AbilitySystem.ApplyHealing(10f);

            Assert.That(
                _actor.AbilitySystem.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Vital.Health),
                Is.EqualTo(60f));
            Assert.That(_actor.AbilitySystem.Effects.Count, Is.EqualTo(0));
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
            var entries = new List<ActiveEffectSaveEntry>();
            _actor.Effects.CaptureRuntimeState(entries, forCharacterSwap: false);
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(
                entries[0].hudVisibility,
                Is.EqualTo((int)GameplayEffectHudVisibility.ForceShow));

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
            effect.grantedTagIds.Add(GameplayTags.State_Combat_Charge);
            effect.modifiers.Add(new GameplayEffectModifierDefinition
            {
                attributeId = global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower.Value,
                modifierType = ModifierType.Flat,
                value = 3f,
            });

            GameplayAbilitySO ability = MakeAbility();
            ability.commitEffects.Add(effect);
            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            set.additionalAbilities.Add(ability);
            _actor.Abilities.SetAbilitySet(set);

            float before = _actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower);
            _actor.Effects.ApplyEffect(effect, _actor);
            _actor.Effects.ApplyEffect(effect, _actor);
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(before + 6f));

            AbilitySystemSaveData snapshot =
                _actor.Abilities.CaptureAbilitySystemStateForCharacter(
                    forCharacterSwap: true);
            Assert.That(snapshot.activeEffects, Has.Count.EqualTo(1));
            Assert.That(snapshot.activeEffects[0].stackCount, Is.EqualTo(2));

            _actor.Abilities.HandleCharacterSwap();
            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.False);
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(before));

            _actor.Abilities.RestoreAbilitySystemStateForCharacter(snapshot);
            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.True);
            Assert.That(_actor.AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower), Is.EqualTo(before + 6f));

            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 세이브복원중_부활한_태그는_OwnedTagPresent를_발화하지_않는다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.RestoreTriggerSuppression";
            effect.durationType = GameplayEffectDurationType.Duration;
            effect.durationSeconds = 20f;
            effect.removalPolicy = GameplayEffectRemovalPolicy.PersistPerCharacter;
            effect.savePolicy = GameplayEffectSavePolicy.SaveRemainingDuration;
            effect.grantedTagIds.Add(GameplayTags.State_Combat_Charge);

            GameplayAbilitySO carrier = MakeAbility();
            carrier.abilityId = "Ability.Test.RestoreEffectCarrier";
            carrier.commitEffects.Add(effect);
            GameplayAbilitySO aura = MakeAbility();
            aura.abilityId = "Ability.Test.RestoreOwnedTagPresent";
            aura.concurrency = AbilityConcurrencyPolicy.Background;
            aura.persistence.backgroundMaxDurationSeconds = 30f;
            aura.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = GameplayTags.State_Combat_Charge,
                source = AbilityTriggerSource.OwnedTagPresent,
                mode = AbilityTriggerActivationMode.Immediate,
                matchMode = AbilityTagMatchMode.Exact,
            });
            AbilitySetSO set = MakeSet(carrier);
            set.additionalAbilities.Add(aura);
            _actor.Abilities.SetAbilitySet(set);

            _actor.Effects.ApplyEffect(effect, _actor);
            AbilitySystemSaveData snapshot =
                _actor.Abilities.CaptureAbilitySystemStateForCharacter(
                    forCharacterSwap: true);
            _actor.Abilities.HandleCharacterSwap();
            int acceptedCount = 0;
            _actor.Abilities.AbilityTriggerAccepted += (_, _) => acceptedCount++;

            _actor.Abilities.RestoreAbilitySystemStateForCharacter(snapshot);

            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.True);
            Assert.That(acceptedCount, Is.Zero);
            Assert.That(
                _actor.Abilities.TryGetActiveExecutionHandle(aura, out _),
                Is.False);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(carrier);
            Object.DestroyImmediate(aura);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void OwnerDeath는_활성_Effect와_GrantedTag를_모두_정리한다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.DeathCleanup";
            effect.durationType = GameplayEffectDurationType.Infinite;
            effect.grantedTagIds.Add(GameplayTags.State_Combat_Charge);

            _actor.Effects.ApplyEffect(effect, _actor);
            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.True);

            _actor.Abilities.HandleOwnerDeath();

            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.False);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void Self대상_Ability는_명시적_대상없이_소유자를_대상으로_실행한다()
        {
            GameplayEffectSO effect = ScriptableObject.CreateInstance<GameplayEffectSO>();
            effect.effectId = "Effect.Test.SelfTarget";
            effect.durationType = GameplayEffectDurationType.Infinite;
            effect.grantedTagIds.Add(GameplayTags.State_Combat_Charge);

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
            Assert.That(_actor.Tags.HasTag(GameplayTags.State_Combat_Charge), Is.True);

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

            Assert.That(
                _actor.Abilities.CaptureAbilitySystemStateForCharacter(
                    forCharacterSwap: false).cooldowns,
                Is.Empty);
            ability.persistence.saveCooldown = true;
            Assert.That(
                _actor.Abilities.CaptureAbilitySystemStateForCharacter(
                    forCharacterSwap: false).cooldowns,
                Has.Count.EqualTo(1));

            _actor.Abilities.EndActivePlayerAbility(completed: true);
            Object.DestroyImmediate(ability);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void AbilitySystemSaveData는_안정값만_JSON으로_직렬화_복원한다()
        {
            var data = new AbilitySystemSaveData();
            data.attributes.Add(new AttributeSaveEntry(
                global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy.Value, 42f));
            data.cooldowns.Add(new GasCooldownSaveEntry
            {
                groupId = "Cooldown.Test",
                remainingSeconds = 1.5f,
            });
            data.activeEffects.Add(new ActiveEffectSaveEntry
            {
                effectId = "Effect.Test.Save",
                sourceActorId = "Actor.Test",
                remainingSeconds = 3f,
                stackCount = 2,
            });

            string json = JsonUtility.ToJson(data);
            AbilitySystemSaveData restored =
                JsonUtility.FromJson<AbilitySystemSaveData>(json);

            Assert.That(
                restored.version, Is.EqualTo(AbilitySystemSaveData.CurrentVersion));
            Assert.That(restored.attributes[0].baseValue, Is.EqualTo(42f));
            Assert.That(restored.cooldowns[0].groupId, Is.EqualTo("Cooldown.Test"));
            Assert.That(restored.activeEffects[0].stackCount, Is.EqualTo(2));
        }

        [Test]
        public void Request전용_Ability는_트리거_경로_밖에서_활성화되지_않는다()
        {
            GameplayAbilitySO router = MakeRequestRouterAbility(
                GameplayTags.Trigger_Monster_Attack_Basic);
            AbilitySetSO set = MakeSet(MakeAbility());
            set.additionalAbilities.Add(router);
            _actor.Abilities.SetAbilitySet(set);

            // 트리거 없이 직접 활성화하면 실행 데이터가 없으므로 거부되어야 한다.
            // 통과시키면 모션 없이 비용·쿨다운만 소모하는 실행이 만들어진다.
            Assert.That(
                _actor.Abilities.EvaluateAbility(router, true, null, out _),
                Is.EqualTo(AbilityActivationResult.MissingExecutionData));
            Assert.That(
                _actor.Abilities.TryPrepareAbility(
                    router, true, null, out AbilityExecutionHandle handle, out _),
                Is.EqualTo(AbilityActivationResult.MissingExecutionData));
            Assert.That(handle.IsValid, Is.False);

            // 반면 트리거 경로로 들어오면 정상적으로 구독자에게 전달된다.
            int requestCount = 0;
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                if (request.Ability == router) requestCount++;
            };
            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Attack_Basic);
            Assert.That(requestCount, Is.EqualTo(1));

            Object.DestroyImmediate(router);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 임시Ability_부여는_대기중인_트리거를_삼키지_않는다()
        {
            GameplayAbilitySO granted = MakeAbility();
            granted.abilityId = "Ability.Test.Granted";
            // 같은 태그에 두 라우터를 걸어 한 신호로 두 건이 대기하게 만든다.
            GameplayAbilitySO first = MakeRequestRouterAbility(
                GameplayTags.Trigger_Monster_Hit_Heavy);
            first.abilityId = "Ability.Test.Router.First";
            GameplayAbilitySO second = MakeRequestRouterAbility(
                GameplayTags.Trigger_Monster_Hit_Heavy);
            second.abilityId = "Ability.Test.Router.Second";
            AbilitySetSO set = MakeSet(MakeAbility());
            set.additionalAbilities.Add(first);
            set.additionalAbilities.Add(second);
            _actor.Abilities.SetAbilitySet(set);

            // 첫 트리거를 처리하는 도중 임시 Ability를 부여해 인덱스 재구축을 유발한다.
            // 재구축이 대기 큐까지 비우면 아직 처리되지 않은 두 번째 트리거가 사라진다.
            int requestCount = 0;
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                if (request.Ability != first && request.Ability != second) return;
                requestCount++;
                if (requestCount == 1)
                    _actor.Abilities.GrantTemporaryAbilities(new[] { granted });
            };

            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Heavy);

            Assert.That(
                requestCount,
                Is.EqualTo(2),
                "인덱스 재구축이 대기 큐를 비우면 두 번째 트리거가 유실된다.");

            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(granted);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 드레인_예산을_넘긴_트리거는_폐기되지_않고_이월된다()
        {
            const int routerCount = 100; // MaxTriggerDrainBudget(64)보다 크게
            var routers = new List<GameplayAbilitySO>(routerCount);
            AbilitySetSO set = MakeSet(MakeAbility());
            for (int i = 0; i < routerCount; i++)
            {
                GameplayAbilitySO router = MakeRequestRouterAbility(
                    GameplayTags.Trigger_Monster_Hit_Light);
                router.abilityId = $"Ability.Test.Router.{i}";
                routers.Add(router);
                set.additionalAbilities.Add(router);
            }
            _actor.Abilities.SetAbilitySet(set);

            int requestCount = 0;
            _actor.Abilities.AbilityTriggerRequested += _ => requestCount++;

            // 한 번의 신호로 예산(64)을 넘는 트리거가 대기열에 쌓인다.
            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Light);
            Assert.That(
                requestCount,
                Is.LessThan(routerCount),
                "이 테스트는 드레인 예산 초과 상황을 전제로 한다.");

            // 초과분은 폐기가 아니라 이월이므로, 다음 드레인 기회에 남김없이 처리된다.
            // (매칭되지 않는 태그를 발행해 드레인만 유발한다.)
            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Grab);

            Assert.That(
                requestCount,
                Is.EqualTo(routerCount),
                "예산 초과분이 폐기되면 발행한 트리거 수보다 적게 처리된다.");

            for (int i = 0; i < routers.Count; i++)
                Object.DestroyImmediate(routers[i]);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 거부된_Request트리거는_retriggerInterval을_소비하지않는다()
        {
            GameplayAbilitySO router = MakeRequestRouterAbility(
                GameplayTags.Trigger_Monster_Hit_Heavy);
            router.triggers[0].retriggerIntervalSeconds = 10f;
            GameplayAbilitySO baseAbility = MakeAbility();
            AbilitySetSO set = MakeSet(baseAbility);
            set.additionalAbilities.Add(router);
            _actor.Abilities.SetAbilitySet(set);

            int requestCount = 0;
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                requestCount++;
                _actor.Abilities.ReportTriggerRejected(
                    request.Ability,
                    AbilityActivationResult.StateTransitionRejected);
            };

            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Heavy);
            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Heavy);

            Assert.That(requestCount, Is.EqualTo(2));
            Object.DestroyImmediate(baseAbility);
            Object.DestroyImmediate(router);
            Object.DestroyImmediate(set);
        }

        [Test]
        public void 승인된_Request트리거만_retriggerInterval을_소비한다()
        {
            GameplayAbilitySO router = MakeRequestRouterAbility(
                GameplayTags.Trigger_Monster_Hit_Heavy);
            router.triggers[0].retriggerIntervalSeconds = 10f;
            GameplayAbilitySO baseAbility = MakeAbility();
            AbilitySetSO set = MakeSet(baseAbility);
            set.additionalAbilities.Add(router);
            _actor.Abilities.SetAbilitySet(set);

            int requestCount = 0;
            AbilityExecutionHandle acceptedHandle = default;
            _actor.Abilities.AbilityTriggerRequested += request =>
            {
                requestCount++;
                Assert.That(
                    _actor.Abilities.TryPrepareAbility(
                        request.Ability,
                        true,
                        null,
                        out acceptedHandle,
                        out _,
                        request.TriggerEvent),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(
                    _actor.Abilities.Commit(acceptedHandle),
                    Is.EqualTo(AbilityActivationResult.Success));
                Assert.That(
                    _actor.Abilities.BindActiveExecutionToTrigger(
                        acceptedHandle,
                        request),
                    Is.True);
            };

            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Heavy);
            Assert.That(acceptedHandle.IsValid, Is.True);
            _actor.Abilities.EndAbility(acceptedHandle, completed: true);
            _actor.Abilities.IssueTriggerEvent(GameplayTags.Trigger_Monster_Hit_Heavy);

            Assert.That(requestCount, Is.EqualTo(1));
            Object.DestroyImmediate(baseAbility);
            Object.DestroyImmediate(router);
            Object.DestroyImmediate(set);
        }

        /// <summary>실행 데이터 없이 Request 트리거만 가진 라우터 Ability.</summary>
        private static GameplayAbilitySO MakeRequestRouterAbility(GameplayTag triggerTag)
        {
            GameplayAbilitySO ability =
                ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Test.Router";
            ability.concurrency = AbilityConcurrencyPolicy.CancelExisting;
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                priority = 1,
            });
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = triggerTag,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
                allowPreemption = true,
            });
            return ability;
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
            var task = ScriptableObject.CreateInstance<WaitDelayTaskDefinitionSO>();
            task.duration = 999f;
            ability.taskGraph = AbilityTaskGraphSO.CreateTransient(task);
            var payload =
                ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
            payload.attackInfo = new AbilityAttackInfo
            {
                motionKey = new MotionKey("Tests.Ground"),
                baseInfo = new AttackInfoBase(),
            };
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Ground",
                priority = 1,
                executionPayload = payload,
            });
            return ability;
        }

        private static GameplayAbilitySO MakeTriggeredBackgroundAbility(
            GameplayTag triggerTag,
            AbilityTriggerSource source)
        {
            GameplayAbilitySO ability = MakeAbility();
            ability.concurrency = AbilityConcurrencyPolicy.Background;
            ability.persistence.backgroundMaxDurationSeconds = 30f;
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = triggerTag,
                source = source,
                mode = AbilityTriggerActivationMode.Immediate,
                matchMode = AbilityTagMatchMode.Exact,
            });
            return ability;
        }
    }

    public sealed class TestGameActor : GameActor
    {
        public void InitializeForEditMode()
        {
            base.Awake();
            AbilitySystem.InitializeDefaultAttributes();
        }

        public void SetActorType(ActorType actorType)
        {
            _actorType = actorType;
        }
    }
}
