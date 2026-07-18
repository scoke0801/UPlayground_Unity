using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Effect
{
    /// <summary>액터별 Effect 수명주기. 정의 SO에는 런타임 값을 기록하지 않는다.</summary>
    public sealed class GameplayEffectController : MonoBehaviour
    {
        private readonly Dictionary<ulong, GameplayEffectInstance> _active = new();
        private readonly Dictionary<string, ulong> _stackingKeys = new(StringComparer.Ordinal);
        private GameActor _owner;
        private ulong _nextHandle = 1;

        public event Action StateChanged;

        private void Awake() => _owner = GetComponent<GameActor>();

        public GameplayEffectHandle ApplyEffect(GameplayEffectSO definition, GameActor source = null)
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
                ModifierSource = new object(),
                TagSource = new GameplayTagSource("Effect", id),
            };

            _active.Add(id, instance);
            _stackingKeys[key] = id;
            AddGrantedTags(instance);
            RebuildModifiers(instance);
            if (definition.IsPeriodic)
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

            _owner?.Stats?.RemoveModifiersBySource(instance.ModifierSource);
            if (_owner?.Tags != null)
                for (int i = 0; i < instance.TagHandles.Count; i++)
                    _owner.Tags.RemoveTag(instance.TagHandles[i]);
            instance.TagHandles.Clear();
            StateChanged?.Invoke();
            return true;
        }

        public void RemoveForSwap()
        {
            var remove = new List<GameplayEffectHandle>();
            foreach (GameplayEffectInstance instance in _active.Values)
                if (instance.Definition.removalPolicy == GameplayEffectRemovalPolicy.RemoveOnSwap)
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
            if (_owner?.Tags == null || instance.Definition.grantedTagIds == null) return;
            for (int i = 0; i < instance.Definition.grantedTagIds.Count; i++)
            {
                GameplayTagHandle handle =
                    _owner.Tags.AddTag(instance.Definition.grantedTagIds[i], instance.TagSource);
                if (handle.IsValid) instance.TagHandles.Add(handle);
            }
        }

        private void RebuildModifiers(GameplayEffectInstance instance)
        {
            if (_owner?.Stats == null) return;
            _owner.Stats.RemoveModifiersBySource(instance.ModifierSource);
            List<GameplayEffectModifierDefinition> modifiers = instance.Definition.modifiers;
            if (modifiers == null) return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                GameplayEffectModifierDefinition definition = modifiers[i];
                if (definition == null) continue;
                _owner.Stats.AddModifier(new StatModifier(
                    definition.statType,
                    definition.modifierType,
                    definition.value * instance.StackCount,
                    instance.ModifierSource,
                    -1f));
            }
        }

        private void ApplyResourceOperations(GameplayEffectSO definition, int stackCount)
        {
            if (_owner == null || definition.resourceOperations == null) return;
            for (int i = 0; i < definition.resourceOperations.Count; i++)
            {
                GameplayResourceOperation operation = definition.resourceOperations[i];
                if (operation == null) continue;
                float magnitude = operation.magnitude * stackCount;

                if (operation.resourceType == AbilityResourceType.Health
                    && _owner is IDamageable damageable
                    && operation.operation == GameplayResourceOperationType.Add
                    && magnitude > 0f)
                {
                    damageable.Heal(magnitude);
                }
                else if (operation.resourceType == AbilityResourceType.UltimateEnergy
                         && _owner is PlayerActor player)
                {
                    PlayerSkillGauge gauge = player.SkillGauge;
                    if (gauge == null) continue;
                    switch (operation.operation)
                    {
                        case GameplayResourceOperationType.Add:
                            gauge.AddGauge(magnitude);
                            break;
                        case GameplayResourceOperationType.Set:
                            gauge.SetGauge(magnitude);
                            break;
                        case GameplayResourceOperationType.PercentOfMax:
                            gauge.AddGauge(gauge.MaxGauge * magnitude);
                            break;
                    }
                }
            }
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

        private void OnDestroy() => RemoveAll();
    }
}
