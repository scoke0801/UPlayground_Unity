using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Gameplay.Effect
{
    /// <summary>액터별 Effect 수명주기. 정의 SO에는 런타임 값을 기록하지 않는다.</summary>
    public sealed class GameplayEffectController : MonoBehaviour
    {
        private readonly Dictionary<ulong, GameplayEffectInstance> _active = new();
        private readonly Dictionary<string, ulong> _stackingKeys = new(StringComparer.Ordinal);
        private GameActor _owner;
        private ulong _nextHandle = 1;
        private IAbilityResourcePort _resources;
        private IAbilityTagPort _tags;
        private IAbilityStatPort _stats;

        public event Action StateChanged;

        private void Awake()
        {
            Initialize(GetComponent<GameActor>());
        }

        public void Initialize(GameActor owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (ReferenceEquals(_owner, owner)
                && _resources != null
                && _tags != null
                && _stats != null)
                return;

            _owner = owner;
            var ports = new UPlayGroundAbilityOwnerPorts(_owner);
            _resources = ports;
            _tags = ports;
            _stats = ports;
        }

        public GameplayEffectHandle ApplyEffect(GameplayEffectSO definition, GameActor source = null)
        {
            return ApplyEffectInternal(definition, source, applyInitialPeriodic: true);
        }

        private GameplayEffectHandle ApplyEffectInternal(
            GameplayEffectSO definition,
            GameActor source,
            bool applyInitialPeriodic)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.effectId))
                return default;

            if (definition.durationType == GameplayEffectDurationType.Instant)
            {
                ApplyResourceOperations(definition, 1);
                return default;
            }

            string key = definition.EffectiveStackingKey;
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
                        return existing.Handle;
                    case AbilityEffectStackAction.RefreshExisting:
                        bool stackChanged = existing.StackCount != stackResult.StackCount;
                        existing.StackCount = stackResult.StackCount;
                        existing.RemainingSeconds = definition.durationSeconds;
                        if (stackChanged)
                            RebuildModifiers(existing);
                        StateChanged?.Invoke();
                        return existing.Handle;
                    case AbilityEffectStackAction.ReplaceExisting:
                        RemoveEffect(existing.Handle);
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
                RemainingSeconds = definition.durationSeconds,
                NextPeriodSeconds = definition.periodSeconds,
            };

            _active.Add(id, instance);
            _stackingKeys[key] = id;
            AddGrantedTags(instance);
            RebuildModifiers(instance);
            if (definition.IsPeriodic && applyInitialPeriodic)
                ApplyResourceOperations(definition, instance.StackCount);
            StateChanged?.Invoke();
            return instance.Handle;
        }

        public bool RemoveEffect(GameplayEffectHandle handle)
        {
            if (!handle.IsValid || !_active.Remove(handle.Value, out GameplayEffectInstance instance))
                return false;

            string key = instance.Definition.EffectiveStackingKey;
            if (_stackingKeys.TryGetValue(key, out ulong mapped) && mapped == handle.Value)
                _stackingKeys.Remove(key);

            for (int i = 0; i < instance.ModifierHandles.Count; i++)
                _stats.RemoveModifier(instance.ModifierHandles[i]);
            instance.ModifierHandles.Clear();
            for (int i = 0; i < instance.TagHandles.Count; i++)
                _tags.Remove(instance.TagHandles[i]);
            instance.TagHandles.Clear();
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
            List<GameplayEffectSaveEntry> destination,
            bool forCharacterSwap)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            foreach (GameplayEffectInstance instance in _active.Values)
            {
                GameplayEffectSO definition = instance.Definition;
                bool shouldCapture = forCharacterSwap
                    ? definition.removalPolicy
                      == GameplayEffectRemovalPolicy.PersistPerCharacter
                    : definition.savePolicy
                      == GameplayEffectSavePolicy.SaveRemainingDuration;
                if (!shouldCapture) continue;

                destination.Add(new GameplayEffectSaveEntry
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
                });
            }
        }

        public void RestoreRuntimeState(
            List<GameplayEffectSaveEntry> entries,
            Func<string, GameplayEffectSO> definitionResolver)
        {
            if (entries == null || definitionResolver == null) return;
            for (int i = 0; i < entries.Count; i++)
            {
                GameplayEffectSaveEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.effectId))
                    continue;

                GameplayEffectSO definition = definitionResolver(entry.effectId);
                if (definition == null || definition.durationType == GameplayEffectDurationType.Instant)
                    continue;
                if (definition.durationType == GameplayEffectDurationType.Duration
                    && entry.remainingSeconds <= 0f)
                    continue;

                GameplayEffectHandle handle = ApplyEffectInternal(
                    definition, source: null, applyInitialPeriodic: false);
                if (!handle.IsValid
                    || !_active.TryGetValue(handle.Value, out GameplayEffectInstance instance))
                    continue;

                instance.StackCount = Mathf.Clamp(
                    entry.stackCount, 1, Mathf.Max(1, definition.maxStackCount));
                instance.RemainingSeconds =
                    definition.durationType == GameplayEffectDurationType.Infinite
                        ? 0f
                        : Mathf.Max(0f, entry.remainingSeconds);
                instance.NextPeriodSeconds = definition.periodSeconds;
                RebuildModifiers(instance);
            }
            StateChanged?.Invoke();
        }

        private void Update()
        {
            if (_active.Count == 0) return;

            float delta = _owner != null ? _owner.DeltaTime : Time.deltaTime;
            var expired = new List<GameplayEffectHandle>();
            foreach (GameplayEffectInstance instance in _active.Values)
            {
                GameplayEffectSO definition = instance.Definition;
                if (definition.IsPeriodic)
                {
                    instance.NextPeriodSeconds -= delta;
                    while (instance.NextPeriodSeconds <= 0f)
                    {
                        ApplyResourceOperations(definition, instance.StackCount);
                        instance.NextPeriodSeconds += definition.periodSeconds;
                    }
                }

                if (definition.durationType != GameplayEffectDurationType.Duration)
                    continue;
                instance.RemainingSeconds -= delta;
                if (instance.RemainingSeconds <= 0f)
                    expired.Add(instance.Handle);
            }

            for (int i = 0; i < expired.Count; i++)
                RemoveEffect(expired[i]);
        }

        private void AddGrantedTags(GameplayEffectInstance instance)
        {
            if (instance.Definition.grantedTagIds == null) return;
            for (int i = 0; i < instance.Definition.grantedTagIds.Count; i++)
            {
                AbilityTagHandle handle = _tags.Add(
                    instance.Definition.grantedTagIds[i].ToString(),
                    "Effect",
                    instance.Handle.Value);
                if (handle.IsValid) instance.TagHandles.Add(handle);
            }
        }

        private void RebuildModifiers(GameplayEffectInstance instance)
        {
            for (int i = 0; i < instance.ModifierHandles.Count; i++)
                _stats.RemoveModifier(instance.ModifierHandles[i]);
            instance.ModifierHandles.Clear();
            List<GameplayEffectModifierDefinition> modifiers = instance.Definition.modifiers;
            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                GameplayEffectModifierDefinition definition = modifiers[i];
                if (definition == null) continue;
                AbilityModifierHandle handle = _stats.AddModifier(
                    definition.statType.ToString(),
                    ToCoreModifierOperation(definition.modifierType),
                    definition.value * instance.StackCount,
                    "Effect",
                    instance.Handle.Value);
                if (handle.IsValid) instance.ModifierHandles.Add(handle);
            }
        }

        private void ApplyResourceOperations(GameplayEffectSO definition, int stackCount)
        {
            if (definition.resourceOperations == null) return;
            for (int i = 0; i < definition.resourceOperations.Count; i++)
            {
                GameplayResourceOperation operation = definition.resourceOperations[i];
                if (operation == null) continue;
                float magnitude = operation.magnitude * stackCount;
                string resourceId = operation.resourceType.ToString();
                if (!_resources.TryGet(resourceId, out float current, out float maximum))
                    continue;
                float targetValue = operation.operation switch
                {
                    GameplayResourceOperationType.Add => current + magnitude,
                    GameplayResourceOperationType.Set => magnitude,
                    GameplayResourceOperationType.PercentOfMax =>
                        current + maximum * magnitude,
                    _ => current,
                };
                _resources.TrySet(resourceId, targetValue);
            }
        }

        private static AbilityModifierOperation ToCoreModifierOperation(
            ModifierType type) =>
            type switch
            {
                ModifierType.Flat => AbilityModifierOperation.Add,
                ModifierType.Percent => AbilityModifierOperation.Percent,
                ModifierType.Multiply => AbilityModifierOperation.Multiply,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

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

        private void OnDestroy() => RemoveAll();
    }
}
