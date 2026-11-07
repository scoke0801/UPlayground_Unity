using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    /// <summary>Ability/Effect/Tag/Attribute의 프로젝트 비의존 집합 루트.</summary>
    public sealed class AbilitySystemRuntime : IAbilitySystemDebugSource, IDisposable
    {
        private readonly IAbilityClock _clock;
        private readonly string _ownerId;
        private readonly IAttributeResolver _attributeResolver;

        public AbilitySystemRuntime(
            AbilitySystemHandle handle,
            string ownerId,
            IAbilityClock clock,
            bool enableDebug = false,
            int debugCapacity = 512,
            IAttributeResolver attributeResolver = null)
        {
            if (!handle.IsValid) throw new ArgumentException("유효한 ASC Handle이 필요합니다.", nameof(handle));
            Handle = handle;
            _ownerId = ownerId ?? string.Empty;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _attributeResolver = attributeResolver;
            Attributes = new AttributeSetRuntime(attributeResolver);
            Tags = new GameplayTagAggregator();
            Events = new GameplayEventRouter();
            Cooldowns = new AbilityCooldownRuntime(clock);
            EffectSpecs = new GameplayEffectSpecFactory();
            Effects = new ActiveGameplayEffectContainer(this, clock);
            Tasks = new AbilityTaskContainer(this, clock);
            Debug = new AbilityDebugRecorder(clock, debugCapacity) { Enabled = enableDebug };

            Attributes.AttributeChanged += OnAttributeChanged;
            Tags.TagAdded += OnTagAdded;
            Tags.TagRemoved += OnTagRemoved;
            Events.EventSent += OnEventSent;
        }

        public AbilitySystemHandle Handle { get; }
        public AttributeSetRuntime Attributes { get; }
        public GameplayTagAggregator Tags { get; }
        public GameplayEventRouter Events { get; }
        public AbilityCooldownRuntime Cooldowns { get; }
        public GameplayEffectSpecFactory EffectSpecs { get; }
        public ActiveGameplayEffectContainer Effects { get; }
        public AbilityTaskContainer Tasks { get; }
        public AbilityDebugRecorder Debug { get; }
        public IAbilityInputPort Input { get; private set; }

        public void SetInputPort(IAbilityInputPort input) => Input = input;

        public AbilitySystemDebugSnapshot CaptureDebugSnapshot(AbilityDebugCaptureOptions options)
        {
            var snapshot = new AbilitySystemDebugSnapshot
            {
                AbilitySystemHandle = Handle,
                OwnerId = _ownerId,
                Frame = _clock.Frame,
                Time = _clock.Time,
                Attributes = new Dictionary<AttributeId, GameplayAttributeValue>(),
                Tags = Array.Empty<AbilityTagId>(),
                Events = Array.Empty<AbilityDebugEvent>(),
                Effects = Array.Empty<ActiveGameplayEffectDebugState>(),
                Tasks = Array.Empty<AbilityTaskDebugState>(),
            };

            if ((options & AbilityDebugCaptureOptions.Attributes) != 0)
            {
                var values = new Dictionary<AttributeId, GameplayAttributeValue>();
                Attributes.CopyValues(values);
                snapshot.Attributes = values;
            }
            if ((options & AbilityDebugCaptureOptions.Tags) != 0)
            {
                var tags = new List<AbilityTagId>();
                Tags.CopyTags(tags);
                snapshot.Tags = tags;
            }
            if ((options & AbilityDebugCaptureOptions.Events) != 0)
            {
                var events = new List<AbilityDebugEvent>();
                Debug.CopyTo(events);
                snapshot.Events = events;
            }
            if ((options & AbilityDebugCaptureOptions.Effects) != 0)
            {
                var active = new List<ActiveGameplayEffect>();
                Effects.CopyActive(active);
                var effects = new List<ActiveGameplayEffectDebugState>(active.Count);
                for (int i = 0; i < active.Count; i++)
                    effects.Add(new ActiveGameplayEffectDebugState(active[i]));
                snapshot.Effects = effects;
            }
            if ((options & AbilityDebugCaptureOptions.Tasks) != 0)
            {
                var active = new List<AbilityTaskInstance>();
                Tasks.CopyActive(active);
                var tasks = new List<AbilityTaskDebugState>(active.Count);
                for (int i = 0; i < active.Count; i++)
                    tasks.Add(new AbilityTaskDebugState(active[i]));
                snapshot.Tasks = tasks;
            }
            return snapshot;
        }

        public AbilitySystemSaveData CaptureSaveData()
        {
            var data = new AbilitySystemSaveData();
            Attributes.CopySaveableBases(data.attributes);
            var cooldowns = new List<AbilityCooldownSnapshot>();
            Cooldowns.Capture(cooldowns);
            for (int i = 0; i < cooldowns.Count; i++)
            {
                data.cooldowns.Add(new GasCooldownSaveEntry
                {
                    groupId = cooldowns[i].GroupId,
                    remainingSeconds = cooldowns[i].RemainingSeconds,
                    availableCharges = cooldowns[i].AvailableCharges,
                    maxCharges = cooldowns[i].MaxCharges,
                    rechargeDurationSeconds =
                        cooldowns[i].RechargeDurationSeconds,
                });
            }

            var active = new List<ActiveGameplayEffect>();
            Effects.CopyActive(active);
            for (int i = 0; i < active.Count; i++)
            {
                ActiveGameplayEffect effect = active[i];
                if (!effect.Spec.Definition.SaveActiveEffect) continue;
                var entry = new ActiveEffectSaveEntry
                {
                    effectId = effect.Spec.Definition.EffectId,
                    remainingSeconds = effect.RemainingSeconds,
                    stackCount = effect.StackCount,
                    specLevel = effect.Spec.Level,
                };
                foreach (KeyValuePair<AbilityTagId, float> pair in effect.Spec.SetByCaller)
                    entry.setByCaller.Add(new SetByCallerSaveEntry { key = pair.Key.Value, value = pair.Value });
                data.activeEffects.Add(entry);
            }
            return data;
        }

        public void RestoreSaveData(
            AbilitySystemSaveData data,
            Func<string, GameplayEffectDefinition> effectResolver = null)
        {
            if (data == null) return;
            int sourceVersion = data.version;
            if (effectResolver != null)
                Effects.Clear();
            using (AttributeSetRuntime.Transaction transaction = Attributes.BeginTransaction())
            {
                for (int i = 0; i < data.attributes.Count; i++)
                {
                    AttributeSaveEntry entry = data.attributes[i];
                    if (entry == null) continue;
                    string attributeId = entry.attributeId;
                    if (_attributeResolver != null)
                    {
                        if (!_attributeResolver.TryResolve(
                                attributeId,
                                out AttributeHandle handle)
                            || !_attributeResolver.TryGetMetadata(
                                handle,
                                out AttributeMetadata metadata))
                        {
                            UnityEngine.Debug.LogWarning(
                                $"[AbilitySystem] 세이브의 미등록 Attribute "
                                + $"'{attributeId}'를 기본값으로 유지합니다.");
                            continue;
                        }
                        attributeId = metadata.AttributeId;
                        entry.attributeId = attributeId;
                    }
                    transaction.SetBase(
                        new AttributeId(attributeId),
                        entry.baseValue);
                }
                transaction.Commit();
            }
            data.version = AbilitySystemSaveData.CurrentVersion;
            Cooldowns.Clear();
            for (int i = 0; i < data.cooldowns.Count; i++)
            {
                GasCooldownSaveEntry entry = data.cooldowns[i];
                if (entry == null) continue;
                if (sourceVersion >= 4)
                {
                    Cooldowns.Restore(
                        entry.groupId,
                        entry.remainingSeconds,
                        entry.availableCharges,
                        entry.maxCharges,
                        entry.rechargeDurationSeconds);
                }
                else
                {
                    Cooldowns.Restore(
                        entry.groupId,
                        entry.remainingSeconds);
                }
            }
            if (effectResolver == null) return;
            for (int i = 0; i < data.activeEffects.Count; i++)
            {
                ActiveEffectSaveEntry entry = data.activeEffects[i];
                GameplayEffectDefinition definition = entry == null ? null : effectResolver(entry.effectId);
                if (definition == null) continue;
                if (definition.DurationPolicy == GameplayEffectDurationPolicy.Duration
                    && entry.remainingSeconds <= 0f)
                    continue;
                GameplayEffectDefinition restoredDefinition =
                    CreateRestoredEffectDefinition(definition);
                var context = new GameplayEffectContext(Handle, Handle, Handle);
                float specLevel = sourceVersion >= 5
                                  && !float.IsNaN(entry.specLevel)
                                  && !float.IsInfinity(entry.specLevel)
                                  && entry.specLevel > 0f
                    ? entry.specLevel
                    : 1f;
                GameplayEffectSpec spec = EffectSpecs.Create(
                    restoredDefinition, specLevel, context, this);
                for (int j = 0; j < entry.setByCaller.Count; j++)
                {
                    SetByCallerSaveEntry value = entry.setByCaller[j];
                    if (value != null) spec.SetMagnitude(new AbilityTagId(value.key), value.value);
                }
                GameplayEffectApplyOutcome outcome = Effects.Apply(spec, this);
                if (outcome.Succeeded && outcome.ActiveHandle.IsValid)
                    Effects.RestoreState(
                        outcome.ActiveHandle,
                        entry.remainingSeconds,
                        entry.stackCount);
            }
        }

        private static GameplayEffectDefinition CreateRestoredEffectDefinition(
            GameplayEffectDefinition definition) =>
            new(
                definition.EffectId,
                definition.DurationPolicy,
                modifiers: definition.Modifiers,
                executions: definition.Executions,
                duration: definition.Duration,
                period: definition.Period,
                stackingKey: definition.StackingKey,
                stackPolicy: definition.StackPolicy,
                maxStackCount: definition.MaxStackCount,
                applicationRequirement: definition.ApplicationRequirement,
                immunityQuery: definition.ImmunityQuery,
                grantedTags: definition.GrantedTags,
                saveActiveEffect: definition.SaveActiveEffect,
                executePeriodicOnApplication: false);

        public void Dispose()
        {
            Attributes.AttributeChanged -= OnAttributeChanged;
            Tags.TagAdded -= OnTagAdded;
            Tags.TagRemoved -= OnTagRemoved;
            Events.EventSent -= OnEventSent;
            Tasks.Dispose();
            Effects.Dispose();
            Events.Clear();
            Tags.Clear();
            Cooldowns.Clear();
        }

        private void OnAttributeChanged(AttributeChangedEvent change) => Debug.Record(
            AbilityDebugCategory.Attribute,
            "Changed",
            effectHandle: change.SourceSpecHandle,
            attributeId: change.AttributeId,
            oldValue: change.OldCurrent,
            newValue: change.NewCurrent);

        private void OnTagAdded(AbilityTagId tag) =>
            Debug.Record(AbilityDebugCategory.Tag, "Added", message: tag.Value);

        private void OnTagRemoved(AbilityTagId tag) =>
            Debug.Record(AbilityDebugCategory.Tag, "Removed", message: tag.Value);

        private void OnEventSent(GameplayEventData data) => Debug.Record(
            AbilityDebugCategory.GameplayEvent,
            "Sent",
            abilityHandle: data.AbilityHandle.Value,
            effectHandle: data.EffectSpecHandle.Value,
            message: data.EventTag.Value);
    }
}
