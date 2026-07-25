using System;
using System.Threading;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
using System.Collections.Generic;
using UPlayGround.Gameplay.Effect;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// 액터의 Ability/Effect/Tag/Attribute 상태를 소유하는 단일 Unity 집합 루트.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilitySystemComponent : MonoBehaviour, IAbilitySystemDebugSource
    {
        private static readonly GameplayEffectDefinition DamageEffect =
            new("GE_Damage", GameplayEffectDurationPolicy.Instant,
                executions: new IGameplayEffectExecution[]
                {
                    new DamageExecution(
                        global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower,
                        global::UPlayGround.Data.Stat.Attributes.Combat.Defense,
                        global::UPlayGround.Data.Stat.Attributes.Vital.Health),
                });
        private static readonly GameplayEffectDefinition HealingEffect =
            new("GE_Healing", GameplayEffectDurationPolicy.Instant,
                executions: new IGameplayEffectExecution[]
                {
                    new HealingExecution(
                        global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                        global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth),
                });
        private static readonly GameplayEffectDefinition PoiseDamageEffect =
            new("GE_PoiseDamage", GameplayEffectDurationPolicy.Instant,
                executions: new IGameplayEffectExecution[]
                {
                    new PoiseDamageExecution(
                        global::UPlayGround.Data.Stat.Attributes.Vital.Poise),
                });
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
        public ActorAbilitySystem ProjectAbilities { get; private set; }
        public GameplayEffectController ProjectEffects { get; private set; }
        public GameplayTagContainer ProjectTags { get; private set; }
        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            Runtime?.Effects.Tick();
            Runtime?.Tasks.Tick();
            ProjectEffects?.Tick();
            if (ProjectAbilities != null)
                ProjectAbilities.Tick();
            else
                Runtime?.Cooldowns.RemoveExpired();
        }

        private void LateUpdate()
        {
            ProjectAbilities?.LateTick();
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
                enableDebug,
                attributeResolver: AttributeRegistry.Resolver);
            RegisterStandardAttributes();
            InitializeProjectRuntime(owner);
            Instances[handle.Value] = new WeakReference<AbilitySystemComponent>(this);
            AbilitySystemDebugRegistry.Register(handle, this);
        }

        private void InitializeProjectRuntime(GameActor owner)
        {
            if (owner == null || ProjectAbilities != null)
                return;

            ProjectTags = new GameplayTagContainer(this);
            ProjectEffects = new GameplayEffectController(owner, this);
            ProjectAbilities = new ActorAbilitySystem(owner, this);
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
            IReadOnlyList<AttributeRegistryEntry> definitions =
                AttributeRegistry.Definitions;
            for (int i = 0; i < definitions.Count; i++)
                Attributes.Register(definitions[i].ToRuntimeDefinition());
        }

        public bool TryGetAttribute(AttributeId attributeId, bool current, out float value)
        {
            value = 0f;
            if (!attributeId.IsValid || !Attributes.Contains(attributeId))
                return false;
            value = current
                ? Attributes.GetCurrent(attributeId)
                : Attributes.GetBase(attributeId);
            return true;
        }

        public void SetAttributeBase(AttributeId attributeId, float value)
        {
            if (!attributeId.IsValid)
                return;
            if (!AttributeRegistry.IsRegistered(attributeId.Value))
                throw new ArgumentException(
                    $"미등록 Attribute ID입니다: {attributeId}",
                    nameof(attributeId));
            if (!Attributes.Contains(attributeId))
                Attributes.Register(
                    AttributeRegistry.CreateRuntimeDefinition(attributeId),
                    value);
            else
                Attributes.SetBase(attributeId, value);
        }

        public bool SetAttributeBases(IReadOnlyDictionary<AttributeId, float> values)
        {
            EnsureInitialized();
            if (values == null) return false;

            foreach (KeyValuePair<AttributeId, float> pair in values)
            {
                if (!pair.Key.IsValid)
                    continue;
                if (!AttributeRegistry.IsRegistered(pair.Key.Value))
                    return false;
                if (!Attributes.Contains(pair.Key))
                    Attributes.Register(
                        AttributeRegistry.CreateRuntimeDefinition(pair.Key),
                        pair.Value);
            }

            using AttributeSetRuntime.Transaction transaction = Attributes.BeginTransaction();
            foreach (KeyValuePair<AttributeId, float> pair in values)
                if (pair.Key.IsValid)
                    transaction.SetBase(pair.Key, pair.Value);
            return transaction.Commit();
        }

        public bool InitializeDefaultAttributes()
        {
            var values = new Dictionary<AttributeId, float>();
            foreach (AttributeId attributeId in UPlayGroundAttributeDefaults.All)
                values[attributeId] =
                    UPlayGroundAttributeDefaults.Get(attributeId);
            return SetAttributeBases(values);
        }

        public bool InitializeAttributes(
            AttributeProfileSO profile,
            out string error)
        {
            EnsureInitialized();
            if (profile == null)
            {
                error = "Attribute Profile이 없습니다.";
                return false;
            }

            var values = new Dictionary<AttributeId, float>();
            if (!profile.TryCopyBaseValues(values, out error))
                return false;

            foreach (KeyValuePair<AttributeId, float> pair in values)
            {
                if (Attributes.Contains(pair.Key))
                    continue;
                error = $"{profile.name}: 등록되지 않은 Attribute ID입니다: {pair.Key}";
                return false;
            }

            using AttributeSetRuntime.Transaction transaction = Attributes.BeginTransaction();
            foreach (KeyValuePair<AttributeId, float> pair in values)
                transaction.SetBase(pair.Key, pair.Value);
            if (transaction.Commit())
            {
                error = string.Empty;
                return true;
            }

            error = $"{profile.name}: Attribute Transaction 커밋에 실패했습니다.";
            return false;
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
                AbilityResourceType.Health => global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                AbilityResourceType.UltimateEnergy => global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy,
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
                AbilityResourceType.Health => global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                AbilityResourceType.UltimateEnergy => global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy,
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

        public ActiveGameplayEffectHandle ApplyAttributeEffect(
            string effectId,
            IReadOnlyList<AttributeModifierValue> modifiers)
        {
            EnsureInitialized();
            var gasModifiers = new List<GameplayEffectModifierSpecDefinition>();
            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    AttributeModifierValue modifier = modifiers[i];
                    if (!modifier.AttributeId.IsValid)
                        throw new ArgumentException(
                            $"Effect '{effectId}' Modifier {i}번의 Attribute ID가 비어 있습니다.",
                            nameof(modifiers));
                    gasModifiers.Add(new GameplayEffectModifierSpecDefinition(
                        modifier.AttributeId,
                        modifier.Operation,
                        new FixedMagnitudeCalculation(modifier.Value)));
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
            ProjectAbilities?.Dispose();
            ProjectEffects?.Dispose();
            ProjectTags?.Dispose();
            ProjectAbilities = null;
            ProjectEffects = null;
            ProjectTags = null;
            AbilitySystemDebugRegistry.Unregister(Runtime.Handle);
            Instances.Remove(Runtime.Handle.Value);
            Runtime.Dispose();
            Runtime = null;
        }
    }
}
