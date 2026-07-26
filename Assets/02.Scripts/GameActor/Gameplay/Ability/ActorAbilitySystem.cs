using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Gameplay.Effect;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// AbilitySystemComponent가 소유하는 프로젝트 Ability 런타임.
    /// 정의(SO)와 실행 상태를 분리하고
    /// Prepare 이후 외부 상태 전환이 성공한 경우에만 비용/쿨다운을 Commit한다.
    /// </summary>
    public sealed class ActorAbilitySystem : IAbilityRuntimeReader
    {
        private readonly Dictionary<ulong, AbilityExecution> _executions = new();
        private GameActor _owner;
        private AbilitySetSO _abilitySet;
        private ulong _nextHandle = 1;
        private ulong _activeExecution;
        private AbilitySystemComponent _abilitySystem;
        private GameplayEffectController _effects;
        private AbilityCooldownRuntime _cooldowns;
        private IAbilityResourcePort _resources;
        private IAbilityTagPort _tags;

        public event Action StateChanged;
        public AbilitySetSO AbilitySet => _abilitySet;
        public bool HasActiveAbility => _activeExecution != 0;
        public bool HasActivePlayerAbility => HasActiveAbility;

        internal ActorAbilitySystem(
            GameActor owner,
            AbilitySystemComponent abilitySystem)
        {
            Initialize(owner, abilitySystem);
        }

        private void Initialize(
            GameActor owner,
            AbilitySystemComponent abilitySystem)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (ReferenceEquals(_owner, owner) && _cooldowns != null)
                return;

            _owner = owner;
            _abilitySystem = abilitySystem
                ?? throw new ArgumentNullException(nameof(abilitySystem));
            _abilitySystem.EnsureInitialized();
            _effects = _abilitySystem.ProjectEffects;
            _cooldowns = _abilitySystem.Runtime.Cooldowns;
            var ports = new UPlayGroundAbilityOwnerPorts(_abilitySystem);
            _resources = ports;
            _tags = ports;
        }

        public void SetAbilitySet(AbilitySetSO abilitySet)
        {
            CancelActiveAbility();
            _abilitySet = abilitySet;
            StateChanged?.Invoke();
        }

        public bool HasPlayerAbility(PlayerSkillSlot slot) =>
            ResolvePlayerAbility(slot) != null;

        public AbilityActivationResult EvaluatePlayerSlot(
            PlayerSkillSlot slot,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant)
        {
            variant = null;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null) return AbilityActivationResult.NotGranted;
            return Evaluate(
                definition,
                isGrounded,
                ResolveTarget(definition, target),
                out variant);
        }

        public AbilityActivationResult EvaluateAbility(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant)
        {
            variant = null;
            if (!IsGrantedAbility(definition))
                return AbilityActivationResult.NotGranted;
            return Evaluate(
                definition,
                isGrounded,
                ResolveTarget(definition, target),
                out variant);
        }

        public AbilityActivationResult TryPreparePlayerSlot(
            PlayerSkillSlot slot,
            bool isGrounded,
            GameActor target,
            out AbilityExecutionHandle handle,
            out AbilityVariantDefinition variant)
        {
            handle = default;
            variant = null;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null) return AbilityActivationResult.NotGranted;

            return TryPrepareAbility(
                definition,
                isGrounded,
                target,
                out handle,
                out variant);
        }

        public AbilityActivationResult TryPrepareAbility(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            out AbilityExecutionHandle handle,
            out AbilityVariantDefinition variant)
        {
            handle = default;
            variant = null;
            if (!IsGrantedAbility(definition))
                return AbilityActivationResult.NotGranted;

            GameActor resolvedTarget = ResolveTarget(definition, target);
            AbilityActivationResult result =
                Evaluate(definition, isGrounded, resolvedTarget, out variant);
            if (result != AbilityActivationResult.Success)
                return result;

            if (_activeExecution != 0)
            {
                if (definition.concurrency == AbilityConcurrencyPolicy.RejectNew)
                    return AbilityActivationResult.ConflictingAbility;
                if (definition.concurrency == AbilityConcurrencyPolicy.CancelExisting)
                    CancelActiveAbility();
            }

            handle = new AbilityExecutionHandle(_nextHandle++);
            _executions.Add(handle.Value, new AbilityExecution(
                handle, definition, variant, _owner, resolvedTarget, Time.frameCount));
            return AbilityActivationResult.Success;
        }

        public AbilityActivationResult Commit(AbilityExecutionHandle handle)
        {
            if (!handle.IsValid || !_executions.TryGetValue(handle.Value, out AbilityExecution execution))
                return AbilityActivationResult.InvalidDefinition;
            if (execution.State == AbilityExecutionState.Active)
                return AbilityActivationResult.AlreadyCommitted;
            if (execution.State != AbilityExecutionState.Prepared)
                return AbilityActivationResult.InvalidDefinition;
            if (Time.frameCount > execution.PreparedFrame + 1)
            {
                Abort(handle);
                return AbilityActivationResult.PreparedExecutionExpired;
            }

            if (!TryConsumeCost(execution.Definition.cost, execution.Handle))
            {
                Abort(handle);
                return AbilityActivationResult.InsufficientResource;
            }

            StartCooldown(execution.Definition);
            AddExecutionTags(execution);
            ApplyEffects(execution.Definition.commitEffects, _owner);
            ApplyEffects(execution.Variant.ownerEffects, _owner);
            ApplyEffects(execution.Variant.targetEffects, execution.Target);

            execution.StartTime = Time.time;
            execution.State = AbilityExecutionState.Active;
            _activeExecution = handle.Value;
            if (execution.Definition.taskGraph?.Root != null)
                _abilitySystem.Runtime.Tasks.Start(handle, execution.Definition.taskGraph.Root);
            StateChanged?.Invoke();
            return AbilityActivationResult.Success;
        }

        public void Abort(AbilityExecutionHandle handle)
        {
            if (!handle.IsValid || !_executions.Remove(handle.Value, out AbilityExecution execution))
                return;
            _abilitySystem.Runtime.Tasks.CancelParent(handle, "AbilityAborted");
            execution.State = AbilityExecutionState.Aborted;
        }

        public void EndActiveAbility(bool completed)
        {
            if (_activeExecution == 0
                || !_executions.Remove(_activeExecution, out AbilityExecution execution))
            {
                _activeExecution = 0;
                return;
            }

            CleanupExecution(execution);
            _abilitySystem.Runtime.Tasks.CancelParent(
                execution.Handle,
                completed ? "AbilityEnded" : "AbilityCancelled");
            execution.State = completed
                ? AbilityExecutionState.Ended
                : AbilityExecutionState.Cancelled;
            if (completed)
                ApplyEffects(execution.Definition.endEffects, _owner);
            _activeExecution = 0;
            StateChanged?.Invoke();
        }

        public void EndActivePlayerAbility(bool completed) => EndActiveAbility(completed);

        public void CancelActiveAbility() => EndActiveAbility(false);

        public void CancelActivePlayerAbility() => CancelActiveAbility();

        public bool TryGetPlayerSlotState(PlayerSkillSlot slot, out AbilitySlotViewState state)
        {
            state = default;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null) return false;

            bool grounded = true;
            if (_owner is PlayerActor player
                && player.PlayerController?.Motor != null)
            {
                grounded = player.PlayerController.Motor.GroundingStatus.IsStableOnGround;
            }

            AbilityActivationResult result =
                Evaluate(
                    definition,
                    grounded,
                    ResolveTarget(definition, null),
                    out AbilityVariantDefinition variant);
            float current = GetResourceCurrent(definition.cost.resourceType);
            float required = GetRequiredCost(definition.cost);
            string group = definition.cooldown.ResolveGroupId(definition.abilityId);
            state = new AbilitySlotViewState(
                definition.abilityId,
                true,
                result != AbilityActivationResult.Locked,
                result == AbilityActivationResult.Success,
                result,
                current,
                required,
                GetCooldownRemaining(group),
                GetEffectiveCooldownDuration(definition, slot),
                variant?.variantId);
            return true;
        }

        public bool TryGetPlayerSlotPresentation(
            PlayerSkillSlot slot,
            out AbilitySlotPresentationState presentation)
        {
            presentation = default;
            GameplayAbilitySO definition = ResolvePlayerAbility(slot);
            if (definition == null)
                return false;

            presentation = new AbilitySlotPresentationState(
                definition.presentation?.displayName,
                definition.presentation?.icon);
            return true;
        }

        public AbilitySystemSaveData CaptureAbilitySystemStateForCharacter(
            bool forCharacterSwap = true)
        {
            AbilitySystemSaveData data = _abilitySystem.Runtime.CaptureSaveData();
            if (!forCharacterSwap)
            {
                data.cooldowns.RemoveAll(
                    entry => entry == null || !ShouldSaveCooldown(entry.groupId));
            }
            data.activeEffects.Clear();
            _effects?.CaptureRuntimeState(data.activeEffects, forCharacterSwap);
            return data;
        }

        public void RestoreAbilitySystemStateForCharacter(AbilitySystemSaveData data)
        {
            _abilitySystem.Runtime.RestoreSaveData(data);
            _effects?.RestoreRuntimeState(data?.activeEffects, ResolveEffectDefinition);
            StateChanged?.Invoke();
        }

        private bool ShouldSaveCooldown(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return false;
            if (_abilitySet != null)
            {
                foreach (GameplayAbilitySO ability in _abilitySet.EnumerateAll())
                {
                    if (ability == null
                        || ability.persistence == null
                        || !ability.persistence.saveCooldown)
                        continue;
                    if (string.Equals(
                            ability.cooldown.ResolveGroupId(ability.abilityId),
                            groupId,
                            StringComparison.Ordinal))
                        return true;
                }
            }

            GameplayAbilitySO elementalImbue =
                ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue);
            if (elementalImbue?.persistence?.saveCooldown == true
                && string.Equals(
                    elementalImbue.cooldown.ResolveGroupId(elementalImbue.abilityId),
                    groupId,
                    StringComparison.Ordinal))
                return true;

            return false;
        }

        public void HandleCharacterSwap()
        {
            CancelActivePlayerAbility();
            _effects?.RemoveForSwap();
        }

        public void HandleOwnerDeath()
        {
            CancelActivePlayerAbility();
            _effects?.RemoveAll();
        }

        private GameplayEffectSO ResolveEffectDefinition(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return null;
            if (_abilitySet != null)
            {
                foreach (GameplayAbilitySO ability in _abilitySet.EnumerateAll())
                {
                    GameplayEffectSO found = FindEffect(ability?.commitEffects, effectId)
                                             ?? FindEffect(ability?.endEffects, effectId);
                    if (found != null) return found;
                    if (ability?.variants == null) continue;
                    for (int i = 0; i < ability.variants.Count; i++)
                    {
                        AbilityVariantDefinition variant = ability.variants[i];
                        found = FindEffect(variant?.ownerEffects, effectId)
                                ?? FindEffect(variant?.targetEffects, effectId);
                        if (found != null) return found;
                    }
                }
            }

            CharacterPassiveSetSO passiveSet =
                Svc.Passives?.GetPassiveSet(_owner.CharacterType);
            if (passiveSet?.passives == null)
                return null;
            for (int i = 0; i < passiveSet.passives.Count; i++)
            {
                GameplayEffectSO found = FindEffect(
                    passiveSet.passives[i]?.triggeredEffects, effectId);
                if (found != null) return found;
            }
            return null;
        }

        private static GameplayEffectSO FindEffect(
            List<GameplayEffectSO> definitions,
            string effectId)
        {
            if (definitions == null) return null;
            for (int i = 0; i < definitions.Count; i++)
                if (definitions[i] != null
                    && string.Equals(
                        definitions[i].effectId, effectId, StringComparison.Ordinal))
                    return definitions[i];
            return null;
        }

        private AbilityActivationResult Evaluate(
            GameplayAbilitySO definition,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant)
        {
            variant = null;
            if (definition == null || string.IsNullOrWhiteSpace(definition.abilityId))
                return AbilityActivationResult.InvalidDefinition;
            if (definition.taskGraph?.Root == null)
                return AbilityActivationResult.MissingExecutionData;
            if (!IsUnlocked(definition))
                return AbilityActivationResult.Locked;

            AbilityActivationRules activation = definition.activation ?? new AbilityActivationRules();
            if (!HasAllTags(activation.requiredTagIds))
                return AbilityActivationResult.MissingRequiredTag;
            if (HasAnyTag(activation.blockedTagIds))
                return AbilityActivationResult.BlockedByTag;
            if (!MatchesGround(activation.groundCondition, isGrounded))
                return AbilityActivationResult.InvalidGroundState;
            if (activation.targetPolicy == AbilityTargetPolicy.Required && target == null)
                return AbilityActivationResult.InvalidTarget;
            if (target != null
                && !MatchesTargetRelation(activation.targetRelation, target))
                return AbilityActivationResult.InvalidTarget;
            if (target != null && !MatchesDistance(activation, target))
                return AbilityActivationResult.OutOfRange;
            if (!CanPayCost(definition.cost))
                return AbilityActivationResult.InsufficientResource;
            if (GetCooldownRemaining(definition.cooldown.ResolveGroupId(definition.abilityId)) > 0f)
                return AbilityActivationResult.CooldownActive;
            if (_activeExecution != 0
                && definition.concurrency == AbilityConcurrencyPolicy.RejectNew)
                return AbilityActivationResult.ConflictingAbility;

            variant = ResolveVariant(definition, isGrounded);
            return variant != null
                ? AbilityActivationResult.Success
                : AbilityActivationResult.MissingExecutionData;
        }

        private AbilityVariantDefinition ResolveVariant(GameplayAbilitySO definition, bool grounded)
        {
            AbilityVariantDefinition best = null;
            int bestPriority = int.MinValue;
            if (definition.variants == null) return null;

            for (int i = 0; i < definition.variants.Count; i++)
            {
                AbilityVariantDefinition candidate = definition.variants[i];
                if (candidate == null
                    || !UPlayGroundAbilityPayloadResolver.IsExecutable(candidate))
                    continue;
                AbilityVariantCondition condition = candidate.condition;
                if (condition != null)
                {
                    if (!MatchesGround(condition.groundCondition, grounded)) continue;
                    if (!HasAllTags(condition.requiredTagIds)) continue;
                    if (HasAnyTag(condition.blockedTagIds)) continue;
                    float current = GetResourceCurrent(definition.cost.resourceType);
                    float max = GetResourceMax(definition.cost.resourceType);
                    if (condition.minResource > 0f && current < condition.minResource) continue;
                    if (condition.requiresFullResource && (max <= 0f || current < max)) continue;
                }
                if (best != null && candidate.priority <= bestPriority) continue;
                best = candidate;
                bestPriority = candidate.priority;
            }
            return best;
        }

        private bool IsUnlocked(GameplayAbilitySO definition)
        {
            if (_owner is not PlayerActor || Svc.Party == null) return true;
            PlayerSkillSlot? slot = FindPlayerSlot(definition);
            if (!slot.HasValue) return true;
            GrowthSkillType type = slot.Value switch
            {
                PlayerSkillSlot.Ability => GrowthSkillType.Ability,
                PlayerSkillSlot.Ultimate => GrowthSkillType.Ultimate,
                PlayerSkillSlot.ElementalImbue => GrowthSkillType.ElementalImbue,
                _ => GrowthSkillType.Ability,
            };
            return Svc.Party.IsSkillUnlocked(Svc.Party.ActiveCharacterType, type);
        }

        private bool CanPayCost(AbilityCostDefinition cost)
        {
            if (cost == null || cost.policy == AbilityCostPolicy.None) return true;
            float current = GetResourceCurrent(cost.resourceType);
            float required = GetRequiredCost(cost);
            if (float.IsInfinity(current)) return false;
            if (cost.policy == AbilityCostPolicy.All && current <= 0f) return false;
            return current >= required;
        }

        private bool TryConsumeCost(
            AbilityCostDefinition cost,
            AbilityExecutionHandle abilityHandle)
        {
            if (!CanPayCost(cost)) return false;
            if (cost == null || cost.policy == AbilityCostPolicy.None) return true;
            string resourceId = cost.resourceType.ToString();
            if (!_resources.TryGet(resourceId, out float current, out _))
                return false;

            float required = GetRequiredCost(cost);
            return _abilitySystem.TryApplyResourceCost(
                cost.resourceType, required, abilityHandle);
        }

        private float GetRequiredCost(AbilityCostDefinition cost)
        {
            if (cost == null || cost.policy == AbilityCostPolicy.None) return 0f;
            float max = GetResourceMax(cost.resourceType);
            return cost.policy switch
            {
                AbilityCostPolicy.Fixed => Mathf.Max(0f, cost.value),
                AbilityCostPolicy.All => Mathf.Max(0f, GetResourceCurrent(cost.resourceType)),
                AbilityCostPolicy.PercentOfMax => Mathf.Max(0f, max * cost.value),
                _ => 0f,
            };
        }

        private float GetResourceCurrent(AbilityResourceType type)
        {
            if (type == AbilityResourceType.None) return 0f;
            if (_resources.TryGet(type.ToString(), out float current, out _))
                return current;
            return float.NegativeInfinity;
        }

        private float GetResourceMax(AbilityResourceType type)
        {
            if (_resources.TryGet(type.ToString(), out _, out float maximum))
                return maximum;
            return 0f;
        }

        private void StartCooldown(GameplayAbilitySO definition)
        {
            float duration = GetEffectiveCooldownDuration(
                definition,
                FindPlayerSlot(definition));
            if (duration <= 0f) return;
            string group = definition.cooldown.ResolveGroupId(definition.abilityId);
            _cooldowns.Start(group, duration);
        }

        private PlayerSkillSlot? FindPlayerSlot(GameplayAbilitySO definition)
        {
            if (definition == null)
                return null;
            if (_abilitySet != null
                && _abilitySet.TryGetPlayerSlot(definition, out PlayerSkillSlot slot))
                return slot;

            if (ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue) == definition)
                return PlayerSkillSlot.ElementalImbue;
            return null;
        }

        private GameplayAbilitySO ResolvePlayerAbility(PlayerSkillSlot slot)
        {
            if (slot == PlayerSkillSlot.ElementalImbue)
            {
                CharacterActorType type = _owner is PlayerActor
                    ? Svc.Party?.ActiveCharacterType ?? CharacterActorType.None
                    : _owner != null ? _owner.CharacterType : CharacterActorType.None;
                return Svc.Party?.GetElementalImbueAbility(type);
            }

            return _abilitySet?.GetPlayerAbility(slot);
        }

        private bool IsGrantedAbility(GameplayAbilitySO definition)
        {
            if (definition == null)
                return false;
            if (_abilitySet != null && _abilitySet.Contains(definition))
                return true;
            return ResolvePlayerAbility(PlayerSkillSlot.ElementalImbue) == definition;
        }

        private float GetEffectiveCooldownDuration(
            GameplayAbilitySO definition,
            PlayerSkillSlot? slot)
        {
            float duration = Mathf.Max(0f, definition?.cooldown?.durationSeconds ?? 0f);
            if (_owner is not PlayerActor || !slot.HasValue)
                return duration;

            float multiplier =
                Svc.Passives?.GetActiveSkillCooldownMultiplier(slot.Value) ?? 1f;
            return duration * Mathf.Max(0f, multiplier);
        }

        private float GetCooldownRemaining(string group)
        {
            return _cooldowns.GetRemaining(group);
        }

        private void AddExecutionTags(AbilityExecution execution)
        {
            List<GameplayTag> tags =
                execution.Definition.activation?.executionGrantedTagIds;
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
            {
                EnsureRegisteredOrEmpty(tags[i], "executionGrantedTagIds", i);
                if (string.IsNullOrEmpty(tags[i].TagName)) continue;
                AbilityTagHandle handle = _tags.Add(
                    tags[i].TagName, "Ability", execution.Handle.Value);
                if (handle.IsValid) execution.GrantedTagHandles.Add(handle);
            }
        }

        private void CleanupExecution(AbilityExecution execution)
        {
            for (int i = 0; i < execution.GrantedTagHandles.Count; i++)
                _tags.Remove(execution.GrantedTagHandles[i]);
            execution.GrantedTagHandles.Clear();
        }

        private void ApplyEffects(List<GameplayEffectSO> effects, GameActor target)
        {
            if (effects == null || target?.Effects == null) return;
            for (int i = 0; i < effects.Count; i++)
                if (effects[i] != null)
                    target.Effects.ApplyEffect(effects[i], _owner);
        }

        private bool HasAllTags(List<GameplayTag> tags)
        {
            if (tags == null) return true;
            for (int i = 0; i < tags.Count; i++)
            {
                EnsureRegisteredOrEmpty(tags[i], "requiredTagIds", i);
                if (!string.IsNullOrEmpty(tags[i].TagName)
                    && !_tags.Has(tags[i].TagName))
                    return false;
            }
            return true;
        }

        private bool HasAnyTag(List<GameplayTag> tags)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Count; i++)
            {
                EnsureRegisteredOrEmpty(tags[i], "blockedTagIds", i);
                if (!string.IsNullOrEmpty(tags[i].TagName)
                    && _tags.Has(tags[i].TagName))
                    return true;
            }
            return false;
        }

        private static void EnsureRegisteredOrEmpty(
            GameplayTag tag,
            string fieldName,
            int index)
        {
            if (string.IsNullOrEmpty(tag.TagName) || tag.IsValid()) return;
            throw new InvalidOperationException(
                $"{fieldName}[{index}]에 Registry 미등록 GameplayTag가 있습니다: "
                + $"'{tag.TagName}'");
        }

        private bool MatchesDistance(AbilityActivationRules activation, GameActor target)
        {
            float distance = Vector3.Distance(_owner.transform.position, target.transform.position);
            if (distance < Mathf.Max(0f, activation.minDistance)) return false;
            return activation.maxDistance <= 0f || distance <= activation.maxDistance;
        }

        private GameActor ResolveTarget(GameplayAbilitySO definition, GameActor target)
        {
            return definition?.activation?.targetRelation == AbilityTargetRelation.Self
                ? _owner
                : target;
        }

        private bool MatchesTargetRelation(
            AbilityTargetRelation relation,
            GameActor target)
        {
            if (target == null) return false;
            if (relation == AbilityTargetRelation.Self)
                return ReferenceEquals(target, _owner);
            if (ReferenceEquals(target, _owner))
                return false;

            bool ownerPlayer = _owner.HasActorType(ActorType.Player);
            bool targetPlayer = target.HasActorType(ActorType.Player);
            bool ownerMonster = _owner.HasActorType(ActorType.Monster);
            bool targetMonster = target.HasActorType(ActorType.Monster);
            bool sameFaction = ownerPlayer && targetPlayer
                               || ownerMonster && targetMonster;
            return relation == AbilityTargetRelation.Ally
                ? sameFaction
                : !sameFaction
                  && (ownerPlayer || ownerMonster)
                  && (targetPlayer || targetMonster);
        }

        private static bool MatchesGround(AbilityGroundCondition condition, bool grounded) =>
            condition == AbilityGroundCondition.Any
            || (condition == AbilityGroundCondition.Grounded && grounded)
            || (condition == AbilityGroundCondition.Airborne && !grounded);

        internal void Tick()
        {
            if (_cooldowns.RemoveExpired())
                StateChanged?.Invoke();
        }

        internal void LateTick()
        {
            var stale = new List<AbilityExecutionHandle>();
            foreach (AbilityExecution execution in _executions.Values)
                if (execution.State == AbilityExecutionState.Prepared
                    && Time.frameCount > execution.PreparedFrame + 1)
                    stale.Add(execution.Handle);
            for (int i = 0; i < stale.Count; i++)
                Abort(stale[i]);
        }

        internal void Dispose()
        {
            CancelActivePlayerAbility();
            _executions.Clear();
        }

    }
}
