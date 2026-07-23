using System;
using System.Threading;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
using System.Collections.Generic;

namespace UPlayGround.Gameplay.Ability
{
    public enum AbilitySystemAuthorityMode
    {
        LegacyAuthorityShadow,
        GasAuthorityMirror,
        GasOnly,
    }

    /// <summary>
    /// 액터의 Ability/Effect/Tag/Attribute 상태를 소유하는 단일 Unity 집합 루트.
    /// 현재는 Attribute별 권위 전환을 위해 레거시 Stat Shadow도 지원한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilitySystemComponent : MonoBehaviour, IAbilitySystemDebugSource
    {
        private static readonly GameplayEffectDefinition DamageEffect =
            new("GE_Damage", GameplayEffectDurationPolicy.Instant,
                executions: new IGameplayEffectExecution[] { new DamageExecution() });
        private static readonly GameplayEffectDefinition HealingEffect =
            new("GE_Healing", GameplayEffectDurationPolicy.Instant,
                executions: new IGameplayEffectExecution[] { new HealingExecution() });
        private static readonly GameplayEffectDefinition PoiseDamageEffect =
            new("GE_PoiseDamage", GameplayEffectDurationPolicy.Instant,
                executions: new IGameplayEffectExecution[] { new PoiseDamageExecution() });
        private sealed class UnityAbilityClock : IAbilityClock
        {
            public float Time => UnityEngine.Time.time;
            public int Frame => UnityEngine.Time.frameCount;
        }

        private static long _nextSystemHandle;
        private static readonly Dictionary<ulong, WeakReference<AbilitySystemComponent>> Instances = new();
        public AbilitySystemRuntime Runtime { get; private set; }
        public AttributeSetRuntime Attributes => Runtime?.Attributes;
        public GameplayTagAggregator Tags => Runtime?.Tags;
        public ActiveGameplayEffectContainer Effects => Runtime?.Effects;
        public AbilitySystemAuthorityMode AuthorityMode => AbilitySystemAuthorityMode.GasOnly;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            Runtime?.Effects.Tick();
            Runtime?.Tasks.Tick();
            Runtime?.Cooldowns.RemoveExpired();
        }

        public void EnsureInitialized()
        {
            if (Runtime != null) return;
            ulong handleValue = unchecked((ulong)Interlocked.Increment(ref _nextSystemHandle));
            if (handleValue == 0)
                handleValue = unchecked((ulong)Interlocked.Increment(ref _nextSystemHandle));
            var handle = new AbilitySystemHandle(handleValue);
            var owner = GetComponent<GameActor>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            const bool enableDebug = true;
#else
            const bool enableDebug = false;
#endif
            Runtime = new AbilitySystemRuntime(
                handle,
                owner != null && !string.IsNullOrWhiteSpace(owner.ActorId)
                    ? owner.ActorId
                    : gameObject.name,
                new UnityAbilityClock(),
                enableDebug);
            RegisterStandardAttributes();
            Instances[handle.Value] = new WeakReference<AbilitySystemComponent>(this);
            AbilitySystemDebugRegistry.Register(handle, this);
        }

        public static bool TryResolve(
            AbilitySystemHandle handle,
            out AbilitySystemComponent component)
        {
            component = null;
            if (!handle.IsValid
                || !Instances.TryGetValue(handle.Value, out WeakReference<AbilitySystemComponent> weak)
                || !weak.TryGetTarget(out component)
                || component == null)
            {
                Instances.Remove(handle.Value);
                component = null;
                return false;
            }
            return true;
        }

        private void RegisterStandardAttributes()
        {
            Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.MaxHealth,
                100f,
                dependentResourceId: AttributeIds.Vital.Health,
                maxChangePolicy: AttributeMaxChangePolicy.PreserveRatio));
            Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.Health,
                100f,
                AttributeClampPolicy.AttributeRange,
                fixedMinimum: 0f,
                maximumAttributeId: AttributeIds.Vital.MaxHealth,
                saveBaseValue: true));
            Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.MaxPoise,
                100f,
                dependentResourceId: AttributeIds.Vital.Poise,
                maxChangePolicy: AttributeMaxChangePolicy.PreserveRatio));
            Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Vital.Poise,
                100f,
                AttributeClampPolicy.AttributeRange,
                fixedMinimum: 0f,
                maximumAttributeId: AttributeIds.Vital.MaxPoise,
                saveBaseValue: true));
            Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Resource.MaxUltimateEnergy,
                100f,
                dependentResourceId: AttributeIds.Resource.UltimateEnergy,
                maxChangePolicy: AttributeMaxChangePolicy.Clamp));
            Attributes.Register(new GameplayAttributeDefinition(
                AttributeIds.Resource.UltimateEnergy,
                0f,
                AttributeClampPolicy.AttributeRange,
                fixedMinimum: 0f,
                maximumAttributeId: AttributeIds.Resource.MaxUltimateEnergy,
                saveBaseValue: true));

            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                if (!UPlayGroundAttributeMapping.TryGetAttributeId(
                        statType, out AttributeId attributeId)
                    || Attributes.Contains(attributeId))
                    continue;
                Attributes.Register(new GameplayAttributeDefinition(
                    attributeId,
                    ActorStatSO.GetDefault(statType)));
            }
        }

        public bool TryGetStat(StatType statType, bool current, out float value)
        {
            value = 0f;
            if (!UPlayGroundAttributeMapping.TryGetAttributeId(statType, out AttributeId id)
                || !Attributes.Contains(id))
                return false;
            value = current ? Attributes.GetCurrent(id) : Attributes.GetBase(id);
            return true;
        }

        public void SetStatBase(StatType statType, float value)
        {
            if (!UPlayGroundAttributeMapping.TryGetAttributeId(statType, out AttributeId id))
                return;
            if (!Attributes.Contains(id))
                Attributes.Register(new GameplayAttributeDefinition(id, value), value);
            else
                Attributes.SetBase(id, value);
        }

        public bool SetStatBases(IReadOnlyDictionary<StatType, float> values)
        {
            EnsureInitialized();
            if (values == null) return false;

            foreach (KeyValuePair<StatType, float> pair in values)
            {
                if (!UPlayGroundAttributeMapping.TryGetAttributeId(
                        pair.Key, out AttributeId id))
                    continue;
                if (!Attributes.Contains(id))
                    Attributes.Register(new GameplayAttributeDefinition(id, pair.Value), pair.Value);
            }

            using AttributeSetRuntime.Transaction transaction = Attributes.BeginTransaction();
            foreach (KeyValuePair<StatType, float> pair in values)
            {
                if (UPlayGroundAttributeMapping.TryGetAttributeId(
                        pair.Key, out AttributeId id))
                    transaction.SetBase(id, pair.Value);
            }
            return transaction.Commit();
        }

        public bool InitializeStats(ActorStatSO profile)
        {
            var values = new Dictionary<StatType, float>();
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                values[statType] = profile != null
                    ? profile.GetBase(statType)
                    : ActorStatSO.GetDefault(statType);
            }
            return SetStatBases(values);
        }

        public AttributeModifierHandle AddStatModifier(
            StatModifier modifier,
            string sourceType,
            ulong sourceId = 0)
        {
            if (!UPlayGroundAttributeMapping.TryGetAttributeId(
                    modifier.statType, out AttributeId attributeId))
                return default;
            if (!Attributes.Contains(attributeId))
                Attributes.Register(new GameplayAttributeDefinition(
                    attributeId, ActorStatSO.GetDefault(modifier.statType)));
            AttributeModifierOperation operation = modifier.modifierType switch
            {
                ModifierType.Flat => AttributeModifierOperation.Add,
                ModifierType.Percent => AttributeModifierOperation.Percent,
                ModifierType.Multiply => AttributeModifierOperation.Multiply,
                _ => throw new ArgumentOutOfRangeException(),
            };
            return Attributes.AddModifier(
                attributeId, operation, modifier.value, sourceType, sourceId);
        }

        public GameplayEffectApplyOutcome ApplyResolvedDamage(
            float damage,
            AbilitySystemComponent source = null,
            AbilityExecutionHandle abilityHandle = default)
        {
            EnsureInitialized();
            AbilitySystemRuntime sourceRuntime = source?.Runtime ?? Runtime;
            var context = new GameplayEffectContext(
                sourceRuntime.Handle,
                sourceRuntime.Handle,
                Runtime.Handle,
                abilityHandle);
            GameplayEffectSpec spec = sourceRuntime.EffectSpecs.Create(
                DamageEffect, 1f, context, sourceRuntime);
            spec.SetMagnitude(GameplayDataTags.ResolvedDamage, Mathf.Max(0f, damage));
            return Effects.Apply(spec, sourceRuntime);
        }

        public GameplayEffectApplyOutcome ApplyHealing(
            float amount,
            float percent = 0f,
            AbilitySystemComponent source = null)
        {
            EnsureInitialized();
            AbilitySystemRuntime sourceRuntime = source?.Runtime ?? Runtime;
            var context = new GameplayEffectContext(
                sourceRuntime.Handle,
                sourceRuntime.Handle,
                Runtime.Handle);
            GameplayEffectSpec spec = sourceRuntime.EffectSpecs.Create(
                HealingEffect, 1f, context, sourceRuntime);
            if (amount > 0f) spec.SetMagnitude(GameplayDataTags.HealAmount, amount);
            if (percent > 0f) spec.SetMagnitude(GameplayDataTags.HealPercent, percent);
            return Effects.Apply(spec, sourceRuntime);
        }

        public GameplayEffectApplyOutcome ApplyPoiseDamage(
            float damage,
            AbilitySystemComponent source = null)
        {
            EnsureInitialized();
            AbilitySystemRuntime sourceRuntime = source?.Runtime ?? Runtime;
            var context = new GameplayEffectContext(
                sourceRuntime.Handle,
                sourceRuntime.Handle,
                Runtime.Handle);
            GameplayEffectSpec spec = sourceRuntime.EffectSpecs.Create(
                PoiseDamageEffect, 1f, context, sourceRuntime);
            spec.SetMagnitude(GameplayDataTags.PoiseDamage, Mathf.Max(0f, damage));
            return Effects.Apply(spec, sourceRuntime);
        }

        public bool TryApplyResourceCost(
            AbilityResourceType resourceType,
            float amount,
            AbilityExecutionHandle abilityHandle = default)
        {
            EnsureInitialized();
            if (amount <= 0f) return true;
            AttributeId attributeId = resourceType switch
            {
                AbilityResourceType.Health => AttributeIds.Vital.Health,
                AbilityResourceType.UltimateEnergy => AttributeIds.Resource.UltimateEnergy,
                _ => default,
            };
            if (!attributeId.IsValid || !Attributes.Contains(attributeId)
                || Attributes.GetCurrent(attributeId) < amount)
                return false;

            var definition = new GameplayEffectDefinition(
                $"GE_Cost.{resourceType}",
                GameplayEffectDurationPolicy.Instant,
                modifiers: new[]
                {
                    new GameplayEffectModifierSpecDefinition(
                        attributeId,
                        AttributeModifierOperation.Add,
                        new FixedMagnitudeCalculation(-amount)),
                });
            var context = new GameplayEffectContext(
                Runtime.Handle,
                Runtime.Handle,
                Runtime.Handle,
                abilityHandle);
            GameplayEffectSpec spec = Runtime.EffectSpecs.Create(
                definition, 1f, context, Runtime);
            return Effects.Apply(spec, Runtime).Succeeded;
        }

        public GameplayEffectApplyOutcome ApplyResourceDelta(
            AbilityResourceType resourceType,
            float delta,
            string sourceId,
            AbilitySystemComponent source = null)
        {
            EnsureInitialized();
            AttributeId attributeId = resourceType switch
            {
                AbilityResourceType.Health => AttributeIds.Vital.Health,
                AbilityResourceType.UltimateEnergy => AttributeIds.Resource.UltimateEnergy,
                _ => default,
            };
            if (!attributeId.IsValid || !Attributes.Contains(attributeId))
                return new GameplayEffectApplyOutcome(
                    GameplayEffectApplyResult.MissingAttribute,
                    error: $"지원하지 않는 자원입니다: {resourceType}");

            return ApplyAttributeDelta(attributeId, delta, sourceId, source);
        }

        public GameplayEffectApplyOutcome ApplyAttributeDelta(
            AttributeId attributeId,
            float delta,
            string sourceId,
            AbilitySystemComponent source = null)
        {
            EnsureInitialized();
            if (!attributeId.IsValid || !Attributes.Contains(attributeId))
                return new GameplayEffectApplyOutcome(
                    GameplayEffectApplyResult.MissingAttribute,
                    error: $"대상 Attribute가 없습니다: {attributeId}");

            var definition = new GameplayEffectDefinition(
                string.IsNullOrWhiteSpace(sourceId)
                    ? $"GE_AttributeDelta.{attributeId.Value}"
                    : sourceId,
                GameplayEffectDurationPolicy.Instant,
                modifiers: new[]
                {
                    new GameplayEffectModifierSpecDefinition(
                        attributeId,
                        AttributeModifierOperation.Add,
                        new FixedMagnitudeCalculation(delta)),
                });
            AbilitySystemRuntime sourceRuntime = source?.Runtime ?? Runtime;
            var context = new GameplayEffectContext(
                sourceRuntime.Handle,
                sourceRuntime.Handle,
                Runtime.Handle,
                sourceObjectId: sourceId);
            GameplayEffectSpec spec = sourceRuntime.EffectSpecs.Create(
                definition, 1f, context, sourceRuntime);
            return Effects.Apply(spec, sourceRuntime);
        }

        public ActiveGameplayEffectHandle ApplyLegacyStatEffect(
            string effectId,
            IReadOnlyList<StatModifier> modifiers)
        {
            EnsureInitialized();
            var gasModifiers = new List<GameplayEffectModifierSpecDefinition>();
            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    StatModifier modifier = modifiers[i];
                    if (!UPlayGroundAttributeMapping.TryGetAttributeId(
                            modifier.statType, out AttributeId attributeId))
                        continue;
                    AttributeModifierOperation operation = modifier.modifierType switch
                    {
                        ModifierType.Flat => AttributeModifierOperation.Add,
                        ModifierType.Percent => AttributeModifierOperation.Percent,
                        ModifierType.Multiply => AttributeModifierOperation.Multiply,
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                    gasModifiers.Add(new GameplayEffectModifierSpecDefinition(
                        attributeId,
                        operation,
                        new FixedMagnitudeCalculation(modifier.value)));
                }
            }

            var definition = new GameplayEffectDefinition(
                effectId,
                GameplayEffectDurationPolicy.Infinite,
                modifiers: gasModifiers,
                stackingKey: effectId,
                stackPolicy: AbilityEffectStackPolicy.ReplaceExisting);
            var context = new GameplayEffectContext(
                Runtime.Handle, Runtime.Handle, Runtime.Handle,
                sourceObjectId: effectId);
            GameplayEffectSpec spec = Runtime.EffectSpecs.Create(
                definition, 1f, context, Runtime);
            GameplayEffectApplyOutcome outcome = Effects.Apply(spec, Runtime);
            return outcome.ActiveHandle;
        }

        public bool RemoveEffect(ActiveGameplayEffectHandle handle) => Effects.Remove(handle);

        public AbilitySystemDebugSnapshot CaptureDebugSnapshot(AbilityDebugCaptureOptions options) =>
            Runtime?.CaptureDebugSnapshot(options);

        private void OnDestroy()
        {
            if (Runtime == null) return;
            AbilitySystemDebugRegistry.Unregister(Runtime.Handle);
            Instances.Remove(Runtime.Handle.Value);
            Runtime.Dispose();
            Runtime = null;
        }
    }
}
