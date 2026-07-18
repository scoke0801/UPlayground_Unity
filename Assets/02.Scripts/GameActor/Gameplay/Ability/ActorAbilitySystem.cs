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
using UPlayGround.Gameplay.Cue;
using UPlayGround.Gameplay.Effect;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// GameActor에 부착되는 Ability 런타임. 정의(SO)와 실행 상태를 분리하고
    /// Prepare 이후 외부 상태 전환이 성공한 경우에만 비용/쿨다운을 Commit한다.
    /// </summary>
    public sealed class ActorAbilitySystem : MonoBehaviour, IAbilityRuntimeReader
    {
        private readonly Dictionary<ulong, AbilityExecution> _executions = new();
        private GameActor _owner;
        private AbilitySetSO _abilitySet;
        private ulong _nextHandle = 1;
        private ulong _activeExecution;
        private GameplayEffectController _effects;
        private AbilityCooldownRuntime _cooldowns;
        private IAbilityResourcePort _resources;
        private IAbilityTagPort _tags;
        private GameplayCueDispatcher _cues;
        private readonly HashSet<string> _trackedCooldownGroups =
            new(StringComparer.Ordinal);

        public event Action StateChanged;
        public AbilitySetSO AbilitySet => _abilitySet;
        public bool HasActiveAbility => _activeExecution != 0;
        public bool HasActivePlayerAbility => HasActiveAbility;

        private void Awake()
        {
            Initialize(GetComponent<GameActor>());
        }

        public void Initialize(GameActor owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (ReferenceEquals(_owner, owner) && _cooldowns != null)
                return;

            _owner = owner;
            _effects = GetComponent<GameplayEffectController>();
            _cooldowns = new AbilityCooldownRuntime(new UnityAbilityClock());
            _cues = gameObject.GetOrAddComponent<GameplayCueDispatcher>();
            var ports = new UPlayGroundAbilityOwnerPorts(_owner);
            _resources = ports;
            _tags = ports;
        }

        public void SetAbilitySet(AbilitySetSO abilitySet)
        {
            CancelActiveAbility();
            _abilitySet = abilitySet;
            _trackedCooldownGroups.Clear();
            StateChanged?.Invoke();
        }

        public bool HasPlayerAbility(PlayerSkillSlot slot) =>
            _abilitySet != null && _abilitySet.GetPlayerAbility(slot) != null;

        public AbilityActivationResult EvaluatePlayerSlot(
            PlayerSkillSlot slot,
            bool isGrounded,
            GameActor target,
            out AbilityVariantDefinition variant)
        {
            variant = null;
            GameplayAbilitySO definition = _abilitySet?.GetPlayerAbility(slot);
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
            if (_abilitySet == null || !_abilitySet.Contains(definition))
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
            GameplayAbilitySO definition = _abilitySet?.GetPlayerAbility(slot);
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
            if (_abilitySet == null || !_abilitySet.Contains(definition))
                return AbilityActivationResult.NotGranted;

            GameActor resolvedTarget = ResolveTarget(definition, target);
            AbilityActivationResult result =
                Evaluate(definition, isGrounded, resolvedTarget, out variant);
            if (result != AbilityActivationResult.Success)
            {
                DispatchCue(
                    definition,
                    variant,
                    AbilityCueEventType.Failed,
                    result);
                return result;
            }

            if (_activeExecution != 0)
            {
                if (definition.concurrency == AbilityConcurrencyPolicy.RejectNew)
                {
                    DispatchCue(
                        definition,
                        variant,
                        AbilityCueEventType.Failed,
                        AbilityActivationResult.ConflictingAbility);
                    return AbilityActivationResult.ConflictingAbility;
                }
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
                DispatchCue(
                    execution.Definition,
                    execution.Variant,
                    AbilityCueEventType.Failed,
                    AbilityActivationResult.PreparedExecutionExpired);
                return AbilityActivationResult.PreparedExecutionExpired;
            }

            if (!TryConsumeCost(execution.Definition.cost))
            {
                Abort(handle);
                DispatchCue(
                    execution.Definition,
                    execution.Variant,
                    AbilityCueEventType.Failed,
                    AbilityActivationResult.InsufficientResource);
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
            DispatchCue(
                execution.Definition,
                execution.Variant,
                AbilityCueEventType.Started,
                AbilityActivationResult.Success);
            StateChanged?.Invoke();
            return AbilityActivationResult.Success;
        }

        public void Abort(AbilityExecutionHandle handle)
        {
            Abort(handle, AbilityActivationResult.Success);
        }

        public void Abort(
            AbilityExecutionHandle handle,
            AbilityActivationResult reason)
        {
            if (!handle.IsValid || !_executions.Remove(handle.Value, out AbilityExecution execution))
                return;
            execution.State = AbilityExecutionState.Aborted;
            if (reason != AbilityActivationResult.Success)
            {
                DispatchCue(
                    execution.Definition,
                    execution.Variant,
                    AbilityCueEventType.Failed,
                    reason);
            }
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
            execution.State = completed
                ? AbilityExecutionState.Ended
                : AbilityExecutionState.Cancelled;
            if (completed)
                ApplyEffects(execution.Definition.endEffects, _owner);
            DispatchCue(
                execution.Definition,
                execution.Variant,
                AbilityCueEventType.Ended,
                AbilityActivationResult.Success);
            _activeExecution = 0;
            StateChanged?.Invoke();
        }

        public void EndActivePlayerAbility(bool completed) => EndActiveAbility(completed);

        public void CancelActiveAbility() => EndActiveAbility(false);

        public void CancelActivePlayerAbility() => CancelActiveAbility();

        public bool TryGetPlayerSlotState(PlayerSkillSlot slot, out AbilitySlotViewState state)
        {
            state = default;
            GameplayAbilitySO definition = _abilitySet?.GetPlayerAbility(slot);
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
                Mathf.Max(0f, definition.cooldown.durationSeconds),
                variant?.variantId);
            return true;
        }

        public AbilityRuntimeSaveData CaptureRuntimeState(bool forCharacterSwap = false)
        {
            var data = new AbilityRuntimeSaveData();
            if (_resources.TryGet(
                    AbilityResourceType.UltimateEnergy.ToString(),
                    out float resourceCurrent,
                    out _))
            {
                data.resources.Add(new AbilityResourceSaveEntry
                {
                    resourceType = AbilityResourceType.UltimateEnergy,
                    currentValue = resourceCurrent,
                });
            }

            var snapshots = new List<AbilityCooldownSnapshot>();
            _cooldowns.Capture(snapshots);
            for (int i = 0; i < snapshots.Count; i++)
            {
                AbilityCooldownSnapshot cooldown = snapshots[i];
                if (!forCharacterSwap && !ShouldSaveCooldown(cooldown.GroupId))
                    continue;
                data.cooldowns.Add(new AbilityCooldownSaveEntry
                {
                    cooldownGroupId = cooldown.GroupId,
                    remainingSeconds = cooldown.RemainingSeconds,
                });
            }
            _effects?.CaptureRuntimeState(data.activeEffects, forCharacterSwap);
            return data;
        }

        public void RestoreRuntimeState(AbilityRuntimeSaveData data)
        {
            _cooldowns.Clear();
            _trackedCooldownGroups.Clear();
            if (data?.resources != null)
            {
                for (int i = 0; i < data.resources.Count; i++)
                {
                    AbilityResourceSaveEntry entry = data.resources[i];
                    if (entry == null || entry.resourceType == AbilityResourceType.None)
                        continue;
                    _resources.TrySet(
                        entry.resourceType.ToString(),
                        Mathf.Max(0f, entry.currentValue));
                }
            }
            if (data?.cooldowns != null)
            {
                for (int i = 0; i < data.cooldowns.Count; i++)
                {
                    AbilityCooldownSaveEntry entry = data.cooldowns[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.cooldownGroupId))
                        continue;
                    float remaining = Mathf.Max(0f, entry.remainingSeconds);
                    if (remaining > 0f)
                    {
                        _cooldowns.Restore(entry.cooldownGroupId, remaining);
                        _trackedCooldownGroups.Add(entry.cooldownGroupId.Trim());
                    }
                }
            }
            _effects?.RestoreRuntimeState(data?.activeEffects, ResolveEffectDefinition);
            StateChanged?.Invoke();
        }

        private bool ShouldSaveCooldown(string groupId)
        {
            if (_abilitySet == null || string.IsNullOrWhiteSpace(groupId))
                return false;
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
            if (_abilitySet == null || string.IsNullOrWhiteSpace(effectId))
                return null;
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
                    || !UPlayGroundAbilityPayloadResolver.TryResolveAnimKey(
                        candidate, out _))
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
            PlayerSkillSlot? slot = null;
            for (int i = 0; i < _abilitySet.playerSlots.Count; i++)
                if (_abilitySet.playerSlots[i]?.ability == definition)
                    slot = _abilitySet.playerSlots[i].slot;
            if (!slot.HasValue) return true;
            GrowthSkillType type = slot.Value == PlayerSkillSlot.Ability
                ? GrowthSkillType.Ability
                : GrowthSkillType.Ultimate;
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

        private bool TryConsumeCost(AbilityCostDefinition cost)
        {
            if (!CanPayCost(cost)) return false;
            if (cost == null || cost.policy == AbilityCostPolicy.None) return true;
            string resourceId = cost.resourceType.ToString();
            if (!_resources.TryGet(resourceId, out float current, out _))
                return false;

            float required = GetRequiredCost(cost);
            return _resources.TrySet(resourceId, Mathf.Max(0f, current - required));
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
            float duration = Mathf.Max(0f, definition.cooldown.durationSeconds);
            if (duration <= 0f) return;
            string group = definition.cooldown.ResolveGroupId(definition.abilityId);
            _cooldowns.Start(group, duration);
            _trackedCooldownGroups.Add(group);
        }

        private float GetCooldownRemaining(string group)
        {
            return _cooldowns.GetRemaining(group);
        }

        private void AddExecutionTags(AbilityExecution execution)
        {
            List<GameplayTagId> tags = execution.Definition.activation?.executionGrantedTagIds;
            if (tags == null) return;
            for (int i = 0; i < tags.Count; i++)
            {
                AbilityTagHandle handle = _tags.Add(
                    tags[i].ToString(), "Ability", execution.Handle.Value);
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

        private bool HasAllTags(List<GameplayTagId> tags)
        {
            if (tags == null) return true;
            for (int i = 0; i < tags.Count; i++)
                if (tags[i] != GameplayTagId.None && !_tags.Has(tags[i].ToString()))
                    return false;
            return true;
        }

        private bool HasAnyTag(List<GameplayTagId> tags)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Count; i++)
                if (tags[i] != GameplayTagId.None && _tags.Has(tags[i].ToString()))
                    return true;
            return false;
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

        private void DispatchCue(
            GameplayAbilitySO definition,
            AbilityVariantDefinition variant,
            AbilityCueEventType eventType,
            AbilityActivationResult result)
        {
            AbilityCueDefinition cues = definition?.cues;
            string cueId = eventType switch
            {
                AbilityCueEventType.Started => cues?.startCueId,
                AbilityCueEventType.Failed => cues?.failureCueId,
                AbilityCueEventType.Ended => cues?.endCueId,
                AbilityCueEventType.CooldownReady => cues?.cooldownReadyCueId,
                _ => null,
            };
            _cues?.Dispatch(new AbilityCueEvent(
                cueId,
                eventType,
                definition?.abilityId,
                variant?.variantId,
                result));
        }

        private void DispatchCooldownReady(string groupId)
        {
            if (_abilitySet == null)
                return;
            foreach (GameplayAbilitySO ability in _abilitySet.EnumerateAll())
            {
                if (ability == null
                    || !string.Equals(
                        ability.cooldown.ResolveGroupId(ability.abilityId),
                        groupId,
                        StringComparison.Ordinal))
                    continue;
                DispatchCue(
                    ability,
                    null,
                    AbilityCueEventType.CooldownReady,
                    AbilityActivationResult.Success);
            }
        }

        private void Update()
        {
            List<string> readyGroups = null;
            foreach (string groupId in _trackedCooldownGroups)
            {
                if (_cooldowns.GetRemaining(groupId) > 0f)
                    continue;
                readyGroups ??= new List<string>();
                readyGroups.Add(groupId);
            }

            if (_cooldowns.RemoveExpired())
                StateChanged?.Invoke();
            if (readyGroups == null)
                return;
            for (int i = 0; i < readyGroups.Count; i++)
            {
                string groupId = readyGroups[i];
                _trackedCooldownGroups.Remove(groupId);
                DispatchCooldownReady(groupId);
            }
        }

        private void LateUpdate()
        {
            var stale = new List<AbilityExecutionHandle>();
            foreach (AbilityExecution execution in _executions.Values)
                if (execution.State == AbilityExecutionState.Prepared
                    && Time.frameCount > execution.PreparedFrame + 1)
                    stale.Add(execution.Handle);
            for (int i = 0; i < stale.Count; i++)
                Abort(stale[i], AbilityActivationResult.PreparedExecutionExpired);
        }

        private void OnDestroy()
        {
            CancelActivePlayerAbility();
            _executions.Clear();
        }

        private sealed class UnityAbilityClock : IAbilityClock
        {
            public float Time => UnityEngine.Time.time;
            public int Frame => UnityEngine.Time.frameCount;
        }
    }
}
