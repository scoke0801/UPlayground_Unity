using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Effect
{
    /// <summary>
    /// AbilitySystemComponent가 소유하는 프로젝트 Effect 수명주기.
    /// 정의 SO에는 런타임 값을 기록하지 않는다.
    /// </summary>
    public sealed class GameplayEffectController : IGameplayEffectRuntimeReader
    {
        private readonly Dictionary<ulong, GameplayEffectInstance> _active = new();
        private readonly Dictionary<string, ulong> _stackingKeys = new(StringComparer.Ordinal);
        private GameActor _owner;
        private ulong _nextHandle = 1;
        private AbilitySystemComponent _abilitySystem;

        public event Action StateChanged;

        internal GameplayEffectController(
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
            if (ReferenceEquals(_owner, owner)
                && _abilitySystem != null)
                return;

            _owner = owner;
            _abilitySystem = abilitySystem;
            _abilitySystem?.EnsureInitialized();
        }

        public GameplayEffectHandle ApplyEffect(
            GameplayEffectSO definition,
            GameActor source = null,
            GameplayEffectApplicationOptions options = default)
        {
            return ApplyEffectInternal(
                definition, source, options, true, true, out _);
        }

        public bool TryApplyEffect(
            GameplayEffectSO definition,
            GameActor source,
            out GameplayEffectHandle handle,
            GameplayEffectApplicationOptions options = default)
        {
            handle = ApplyEffectInternal(
                definition, source, options, true, true, out bool succeeded);
            return succeeded;
        }

        private GameplayEffectHandle ApplyEffectInternal(
            GameplayEffectSO definition,
            GameActor source,
            GameplayEffectApplicationOptions options,
            bool executePeriodicOnApplication,
            bool enforceApplicationRules,
            out bool succeeded)
        {
            succeeded = false;
            if (definition == null || string.IsNullOrWhiteSpace(definition.effectId))
                return default;
            if (enforceApplicationRules && !CanApply(definition))
                return default;
            DispelMatchingEffects(definition.dispelTags);

            if (definition.durationType == GameplayEffectDurationType.Instant)
            {
                GameplayEffectApplyOutcome instantOutcome = ApplyGasSpec(
                    definition,
                    source,
                    0f,
                    executePeriodicOnApplication,
                    options.SetByCallerMagnitudes);
                if (!instantOutcome.Succeeded)
                    ReportApplyFailure(definition, instantOutcome);
                succeeded = instantOutcome.Succeeded;
                return default;
            }

            float effectiveDuration = ResolveEffectiveDuration(definition);
            string key = definition.EffectiveStackingKey;
            GameplayEffectInstance replaceAfterSuccessfulApply = null;
            if (_stackingKeys.TryGetValue(key, out ulong existingId)
                && _active.TryGetValue(existingId, out GameplayEffectInstance existing))
            {
                AbilityEffectStackResult stackResult = AbilityEffectStackRuntime.Resolve(
                    ToCoreStackPolicy(definition.stackPolicy),
                    existing.StackCount,
                    definition.maxStackCount);
                switch (stackResult.Action)
                {
                    case AbilityEffectStackAction.KeepExisting:
                        succeeded = true;
                        return existing.Handle;
                    case AbilityEffectStackAction.RefreshExisting:
                        GameplayEffectApplyOutcome refreshed =
                            ApplyGasSpec(definition, source, effectiveDuration,
                                executePeriodicOnApplication,
                                options.SetByCallerMagnitudes);
                        if (!refreshed.Succeeded)
                        {
                            ReportApplyFailure(definition, refreshed);
                            return default;
                        }
                        if (refreshed.ActiveHandle.IsValid)
                            existing.GasHandle = refreshed.ActiveHandle;
                        existing.StackCount = stackResult.StackCount;
                        existing.DurationSeconds = effectiveDuration;
                        existing.RemainingSeconds = effectiveDuration;
                        existing.HudVisibility = options.HudVisibility;
                        StateChanged?.Invoke();
                        succeeded = true;
                        return existing.Handle;
                    case AbilityEffectStackAction.ReplaceExisting:
                        replaceAfterSuccessfulApply = existing;
                        break;
                }
            }

            ulong id = _nextHandle++;
            var instance = new GameplayEffectInstance
            {
                Handle = new GameplayEffectHandle(id),
                Definition = definition,
                Source = source,
                StackCount = 1,
                DurationSeconds = effectiveDuration,
                RemainingSeconds = effectiveDuration,
                NextPeriodSeconds = definition.periodSeconds,
                HudVisibility = options.HudVisibility,
            };
            GameplayEffectApplyOutcome gasOutcome =
                ApplyGasSpec(definition, source, effectiveDuration,
                    executePeriodicOnApplication,
                    options.SetByCallerMagnitudes);
            if (!gasOutcome.Succeeded)
            {
                ReportApplyFailure(definition, gasOutcome);
                return default;
            }
            instance.GasHandle = gasOutcome.ActiveHandle;

            if (replaceAfterSuccessfulApply != null)
                RemoveEffect(replaceAfterSuccessfulApply.Handle);

            _active.Add(id, instance);
            _stackingKeys[key] = id;
            _owner?.Abilities?.GrantTemporaryAbilities(
                definition.grantedAbilities);
            AddGrantedElement(instance);
            StateChanged?.Invoke();
            succeeded = true;
            return instance.Handle;
        }

        public bool RemoveEffect(GameplayEffectHandle handle)
        {
            if (!handle.IsValid || !_active.Remove(handle.Value, out GameplayEffectInstance instance))
                return false;

            string key = instance.Definition.EffectiveStackingKey;
            if (_stackingKeys.TryGetValue(key, out ulong mapped) && mapped == handle.Value)
                _stackingKeys.Remove(key);

            if (instance.GrantsElement)
                _owner?.RemoveElementOverride(instance.Handle.Value);
            if (instance.GasHandle.IsValid)
                _abilitySystem?.Effects?.Remove(instance.GasHandle);
            _owner?.Abilities?.RevokeTemporaryAbilities(
                instance.Definition.grantedAbilities);
            StateChanged?.Invoke();
            return true;
        }

        public void RemoveForSwap()
        {
            var remove = new List<GameplayEffectHandle>();
            foreach (GameplayEffectInstance instance in _active.Values)
                if (instance.Definition.removalPolicy
                    is GameplayEffectRemovalPolicy.RemoveOnSwap
                    or GameplayEffectRemovalPolicy.PersistPerCharacter)
                    remove.Add(instance.Handle);
            for (int i = 0; i < remove.Count; i++)
                RemoveEffect(remove[i]);
        }

        public void RemoveAll()
        {
            var handles = new List<GameplayEffectHandle>();
            foreach (ulong id in _active.Keys)
                handles.Add(new GameplayEffectHandle(id));
            for (int i = 0; i < handles.Count; i++)
                RemoveEffect(handles[i]);
        }

        public void CaptureRuntimeState(
            List<ActiveEffectSaveEntry> destination,
            bool forCharacterSwap)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            var gasEffects = new List<ActiveGameplayEffect>();
            _abilitySystem?.Runtime?.Effects.CopyActive(gasEffects);
            foreach (GameplayEffectInstance instance in _active.Values)
            {
                GameplayEffectSO definition = instance.Definition;
                bool shouldCapture = forCharacterSwap
                    ? definition.removalPolicy
                      == GameplayEffectRemovalPolicy.PersistPerCharacter
                    : definition.savePolicy
                      == GameplayEffectSavePolicy.SaveRemainingDuration;
                if (!shouldCapture) continue;

                var entry = new ActiveEffectSaveEntry
                {
                    effectId = definition.effectId,
                    sourceActorId = instance.Source != null
                        ? instance.Source.ActorId
                        : string.Empty,
                    remainingSeconds = definition.durationType
                        == GameplayEffectDurationType.Infinite
                            ? -1f
                            : Mathf.Max(0f, instance.RemainingSeconds),
                    stackCount = Mathf.Clamp(
                        instance.StackCount, 1, Mathf.Max(1, definition.maxStackCount)),
                    hudVisibility = (int)instance.HudVisibility,
                };
                for (int i = 0; i < gasEffects.Count; i++)
                {
                    ActiveGameplayEffect gasEffect = gasEffects[i];
                    if (!gasEffect.Handle.Equals(instance.GasHandle))
                        continue;
                    foreach (KeyValuePair<AbilityTagId, float> pair
                             in gasEffect.Spec.SetByCaller)
                    {
                        entry.setByCaller.Add(new SetByCallerSaveEntry
                        {
                            key = pair.Key.Value,
                            value = pair.Value,
                        });
                    }
                    break;
                }
                destination.Add(entry);
            }
        }

        public void RestoreRuntimeState(
            List<ActiveEffectSaveEntry> entries,
            Func<string, GameplayEffectSO> definitionResolver)
        {
            if (entries == null || definitionResolver == null) return;
            for (int i = 0; i < entries.Count; i++)
            {
                ActiveEffectSaveEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.effectId))
                    continue;

                GameplayEffectSO definition = definitionResolver(entry.effectId);
                if (definition == null || definition.durationType == GameplayEffectDurationType.Instant)
                    continue;
                if (definition.durationType == GameplayEffectDurationType.Duration
                    && entry.remainingSeconds <= 0f)
                    continue;

                IReadOnlyDictionary<string, float> setByCaller =
                    BuildSetByCallerMap(entry.setByCaller);
                var options = new GameplayEffectApplicationOptions(
                    Enum.IsDefined(
                        typeof(GameplayEffectHudVisibility),
                        entry.hudVisibility)
                            ? (GameplayEffectHudVisibility)entry.hudVisibility
                            : GameplayEffectHudVisibility.UseDefinition,
                    setByCaller);
                GameplayEffectHandle handle = ApplyEffectInternal(
                    definition,
                    null,
                    options,
                    false,
                    false,
                    out _);
                if (!handle.IsValid
                    || !_active.TryGetValue(handle.Value, out GameplayEffectInstance instance))
                    continue;

                instance.StackCount = Mathf.Clamp(
                    entry.stackCount, 1, Mathf.Max(1, definition.maxStackCount));
                for (int stack = 1; stack < instance.StackCount; stack++)
                {
                    GameplayEffectApplyOutcome restoredStack = ApplyGasSpec(
                        definition,
                        null,
                        ResolveEffectiveDuration(definition),
                        false,
                        setByCaller);
                    if (restoredStack.ActiveHandle.IsValid)
                        instance.GasHandle = restoredStack.ActiveHandle;
                }
                instance.RemainingSeconds =
                    definition.durationType == GameplayEffectDurationType.Infinite
                        ? 0f
                        : Mathf.Max(0f, entry.remainingSeconds);
                instance.DurationSeconds = ResolveEffectiveDuration(definition);
                instance.NextPeriodSeconds = definition.periodSeconds;
            }
            StateChanged?.Invoke();
        }

        public void CopyVisibleEffects(List<GameplayEffectViewState> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            foreach (GameplayEffectInstance instance in _active.Values)
            {
                if (IsVisibleInHud(instance))
                    destination.Add(ToViewState(instance));
            }
        }

        public void CopyActiveEffects(List<GameplayEffectViewState> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            foreach (GameplayEffectInstance instance in _active.Values)
                destination.Add(ToViewState(instance));
        }

        public bool RemoveEffectByRuntimeId(ulong runtimeId) =>
            RemoveEffect(new GameplayEffectHandle(runtimeId));

        public int RemoveEffectsByDefinition(GameplayEffectSO definition)
        {
            if (definition == null)
                return 0;
            var handles = new List<GameplayEffectHandle>();
            foreach (GameplayEffectInstance instance in _active.Values)
                if (instance.Definition == definition)
                    handles.Add(instance.Handle);
            for (int i = 0; i < handles.Count; i++)
                RemoveEffect(handles[i]);
            return handles.Count;
        }

        private bool CanApply(GameplayEffectSO definition)
        {
            float chance = Mathf.Clamp01(definition.applicationChance);
            if (chance <= 0f)
                return false;
            if (chance < 1f && UnityEngine.Random.value >= chance)
                return false;
            return HasAllTags(definition.requiredTagIds)
                   && !HasAnyTag(definition.blockedTagIds)
                   && !HasAnyTag(definition.immunityTags);
        }

        private bool HasAllTags(IReadOnlyList<GameplayTag> tags)
        {
            if (tags == null)
                return true;
            for (int i = 0; i < tags.Count; i++)
                if (!string.IsNullOrEmpty(tags[i].TagName)
                    && !(_abilitySystem?.Tags?.Has(
                        new AbilityTagId(tags[i].TagName)) ?? false))
                    return false;
            return true;
        }

        private bool HasAnyTag(IReadOnlyList<GameplayTag> tags)
        {
            if (tags == null)
                return false;
            for (int i = 0; i < tags.Count; i++)
                if (!string.IsNullOrEmpty(tags[i].TagName)
                    && (_abilitySystem?.Tags?.Has(
                        new AbilityTagId(tags[i].TagName)) ?? false))
                    return true;
            return false;
        }

        private void DispelMatchingEffects(
            IReadOnlyList<GameplayTag> dispelTags)
        {
            if (dispelTags == null || dispelTags.Count == 0)
                return;

            var remove = new List<GameplayEffectHandle>();
            foreach (GameplayEffectInstance instance in _active.Values)
            {
                List<GameplayTag> granted = instance.Definition?.grantedTagIds;
                if (granted == null)
                    continue;
                bool matched = false;
                for (int i = 0; i < dispelTags.Count && !matched; i++)
                    for (int j = 0; j < granted.Count; j++)
                        if (!string.IsNullOrEmpty(dispelTags[i].TagName)
                            && string.Equals(
                                dispelTags[i].TagName,
                                granted[j].TagName,
                                StringComparison.Ordinal))
                        {
                            matched = true;
                            break;
                        }
                if (matched)
                    remove.Add(instance.Handle);
            }
            for (int i = 0; i < remove.Count; i++)
                RemoveEffect(remove[i]);
        }

        public bool TryGetVisibleEffect(
            ulong runtimeId,
            out GameplayEffectViewState state)
        {
            if (_active.TryGetValue(runtimeId, out GameplayEffectInstance instance)
                && IsVisibleInHud(instance))
            {
                state = ToViewState(instance);
                return true;
            }

            state = default;
            return false;
        }

        private static bool IsVisibleInHud(GameplayEffectInstance instance)
        {
            if (instance?.Definition == null
                || instance.Definition.durationType == GameplayEffectDurationType.Instant)
            {
                return false;
            }

            return instance.HudVisibility switch
            {
                GameplayEffectHudVisibility.ForceShow => true,
                GameplayEffectHudVisibility.ForceHide => false,
                _ => instance.Definition.presentation?.showInHud ?? true,
            };
        }

        private static GameplayEffectViewState ToViewState(
            GameplayEffectInstance instance)
        {
            GameplayEffectSO definition = instance.Definition;
            GameplayEffectPresentationDefinition presentation = definition.presentation;
            return new GameplayEffectViewState(
                instance.Handle.Value,
                definition.effectId,
                presentation?.displayName ?? definition.effectId,
                presentation?.icon,
                definition.polarity,
                presentation?.hudPriority ?? 0,
                instance.StackCount,
                instance.DurationSeconds,
                instance.RemainingSeconds,
                definition.durationType == GameplayEffectDurationType.Infinite,
                presentation?.showRemainingTime ?? true,
                presentation?.showStackCount ?? true);
        }

        private float ResolveEffectiveDuration(GameplayEffectSO definition)
        {
            float duration = Mathf.Max(0f, definition.durationSeconds);
            if (definition.durationType != GameplayEffectDurationType.Duration
                || definition.ignorePassiveDurationModifiers
                || _owner is not PlayerActor)
            {
                return duration;
            }

            float multiplier = definition.polarity switch
            {
                GameplayEffectPolarity.Beneficial =>
                    Svc.Passives?.GetActiveMultiplier(
                        PassiveModifierType.BeneficialEffectDuration) ?? 1f,
                GameplayEffectPolarity.Harmful =>
                    Svc.Passives?.GetActiveMultiplier(
                        PassiveModifierType.HarmfulEffectDuration) ?? 1f,
                _ => 1f,
            };
            return PassiveModifierCalculator.CalculateEffectDuration(
                duration, definition.polarity, multiplier);
        }

        internal void Tick()
        {
            if (_active.Count == 0) return;

            float delta = _owner != null ? _owner.DeltaTime : Time.deltaTime;
            var expired = new List<GameplayEffectHandle>();
            foreach (GameplayEffectInstance instance in _active.Values)
            {
                GameplayEffectSO definition = instance.Definition;
                if (definition.durationType != GameplayEffectDurationType.Duration)
                    continue;
                instance.RemainingSeconds -= delta;
                if (instance.RemainingSeconds <= 0f)
                    expired.Add(instance.Handle);
            }

            for (int i = 0; i < expired.Count; i++)
                RemoveEffect(expired[i]);
        }

        private void AddGrantedElement(GameplayEffectInstance instance)
        {
            CombatElement element = instance.Definition.grantedElement;
            if (element == CombatElement.None || _owner == null)
                return;

            _owner.AddElementOverride(
                instance.Handle.Value,
                element,
                instance.Definition.elementPriority);
            instance.GrantsElement = true;
        }

        private GameplayEffectApplyOutcome ApplyGasSpec(
            GameplayEffectSO sourceDefinition,
            GameActor source,
            float effectiveDuration,
            bool executePeriodicOnApplication,
            IReadOnlyDictionary<string, float> setByCallerMagnitudes = null)
        {
            if (_abilitySystem?.Runtime == null)
                return new GameplayEffectApplyOutcome(
                    GameplayEffectApplyResult.InvalidTarget,
                    error: "대상 AbilitySystemComponent가 없습니다.");

            GameplayEffectDurationPolicy durationPolicy = sourceDefinition.durationType switch
            {
                GameplayEffectDurationType.Instant => GameplayEffectDurationPolicy.Instant,
                GameplayEffectDurationType.Duration => GameplayEffectDurationPolicy.Duration,
                GameplayEffectDurationType.Infinite => GameplayEffectDurationPolicy.Infinite,
                _ => throw new ArgumentOutOfRangeException(),
            };
            var modifiers = new List<GameplayEffectModifierSpecDefinition>();
            if (durationPolicy != GameplayEffectDurationPolicy.Instant
                && sourceDefinition.modifiers != null)
            {
                for (int i = 0; i < sourceDefinition.modifiers.Count; i++)
                {
                    GameplayEffectModifierDefinition modifier = sourceDefinition.modifiers[i];
                    if (modifier == null)
                        continue;
                    AttributeId attributeId = modifier.AttributeId;
                    if (!attributeId.IsValid)
                    {
                        return new GameplayEffectApplyOutcome(
                            GameplayEffectApplyResult.MissingAttribute,
                            error:
                            $"{sourceDefinition.effectId}: Modifier {i}번 Attribute ID가 없습니다.");
                    }
                    if (!GameplayEffectMagnitudeFactory.TryBuild(
                            modifier,
                            out IGameplayMagnitudeCalculation magnitude,
                            out string magnitudeError))
                    {
                        return new GameplayEffectApplyOutcome(
                            GameplayEffectApplyResult.InvalidDefinition,
                            error:
                            $"{sourceDefinition.effectId}: Modifier {i}번 {magnitudeError}");
                    }
                    modifiers.Add(new GameplayEffectModifierSpecDefinition(
                        attributeId,
                        modifier.modifierType switch
                        {
                            ModifierType.Flat => AttributeModifierOperation.Add,
                            ModifierType.Percent => AttributeModifierOperation.Percent,
                            ModifierType.Multiply => AttributeModifierOperation.Multiply,
                            _ => throw new ArgumentOutOfRangeException(),
                        },
                        magnitude));
                }
            }
            var grantedTags = new List<AbilityTagId>();
            if (sourceDefinition.grantedTagIds != null)
            {
                for (int i = 0; i < sourceDefinition.grantedTagIds.Count; i++)
                {
                    GameplayTag tag = sourceDefinition.grantedTagIds[i];
                    if (string.IsNullOrEmpty(tag.TagName))
                        continue;
                    if (!tag.IsValid())
                    {
                        return new GameplayEffectApplyOutcome(
                            GameplayEffectApplyResult.InvalidDefinition,
                            error:
                            $"{sourceDefinition.effectId}: grantedTagIds[{i}]에 "
                            + $"Registry 미등록 태그가 있습니다: '{tag.TagName}'");
                    }
                    grantedTags.Add(new AbilityTagId(tag.TagName));
                }
            }
            var definition = new GameplayEffectDefinition(
                sourceDefinition.effectId,
                durationPolicy,
                modifiers: modifiers,
                duration: durationPolicy == GameplayEffectDurationPolicy.Duration
                    ? new FixedMagnitudeCalculation(effectiveDuration)
                    : null,
                period: sourceDefinition.periodSeconds > 0f
                    ? new FixedMagnitudeCalculation(sourceDefinition.periodSeconds)
                    : null,
                stackingKey: sourceDefinition.EffectiveStackingKey,
                stackPolicy: ToCoreStackPolicy(sourceDefinition.stackPolicy),
                maxStackCount: sourceDefinition.maxStackCount,
                grantedTags: grantedTags,
                saveActiveEffect: sourceDefinition.savePolicy
                    == GameplayEffectSavePolicy.SaveRemainingDuration,
                executePeriodicOnApplication:
                    sourceDefinition.IsPeriodic && executePeriodicOnApplication);

            AbilitySystemRuntime sourceRuntime = source?.AbilitySystem?.Runtime
                                                ?? _abilitySystem.Runtime;
            var context = new GameplayEffectContext(
                sourceRuntime.Handle,
                sourceRuntime.Handle,
                _abilitySystem.Runtime.Handle,
                sourceObjectId: sourceDefinition.effectId);
            GameplayEffectSpec spec = sourceRuntime.EffectSpecs.Create(
                definition, 1f, context, sourceRuntime);
            if (setByCallerMagnitudes != null)
            {
                foreach (KeyValuePair<string, float> pair in setByCallerMagnitudes)
                {
                    var key = new AbilityTagId(pair.Key);
                    if (!key.IsValid
                        || float.IsNaN(pair.Value)
                        || float.IsInfinity(pair.Value)
                        || !spec.SetMagnitude(key, pair.Value))
                    {
                        return new GameplayEffectApplyOutcome(
                            GameplayEffectApplyResult.InvalidContext,
                            error:
                            $"{sourceDefinition.effectId}: 올바르지 않은 "
                            + $"SetByCaller 값입니다. {pair.Key}");
                    }
                }
            }
            spec.AddTrace("GameplayEffectSO Spec");
            return _abilitySystem.Effects.Apply(spec, sourceRuntime);
        }

        private static IReadOnlyDictionary<string, float> BuildSetByCallerMap(
            List<SetByCallerSaveEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return null;

            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                SetByCallerSaveEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;
                result[entry.key] = entry.value;
            }
            return result.Count > 0 ? result : null;
        }

        private void ReportApplyFailure(
            GameplayEffectSO definition,
            GameplayEffectApplyOutcome outcome)
        {
            Debug.LogError(
                $"[GameplayEffectController] EffectSpec 적용 실패: " +
                $"{definition?.effectId ?? "<null>"}, {outcome.Result}, {outcome.Error}",
                _owner);
        }

        private static AbilityEffectStackPolicy ToCoreStackPolicy(
            GameplayEffectStackPolicy policy) =>
            policy switch
            {
                GameplayEffectStackPolicy.RejectNew =>
                    AbilityEffectStackPolicy.RejectNew,
                GameplayEffectStackPolicy.RefreshDuration =>
                    AbilityEffectStackPolicy.RefreshDuration,
                GameplayEffectStackPolicy.AddStackAndRefresh =>
                    AbilityEffectStackPolicy.AddStackAndRefresh,
                GameplayEffectStackPolicy.ReplaceExisting =>
                    AbilityEffectStackPolicy.ReplaceExisting,
                _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null),
            };

        internal void Dispose() => RemoveAll();
    }
}
