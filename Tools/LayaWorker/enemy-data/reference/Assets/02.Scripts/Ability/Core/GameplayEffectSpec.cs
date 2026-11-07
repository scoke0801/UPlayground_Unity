using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    public enum GameplayEffectDurationPolicy
    {
        Instant,
        Duration,
        Infinite,
    }

    public enum GameplayEffectCaptureSource
    {
        Source,
        Target,
    }

    public enum GameplayEffectCapturePolicy
    {
        SnapshotOnCreate,
        SnapshotOnApply,
        EvaluateOnExecute,
    }

    public enum GameplayEffectApplyResult
    {
        Success,
        InvalidDefinition,
        InvalidContext,
        MissingSetByCaller,
        MissingAttribute,
        BlockedByTag,
        Immune,
        InvalidTarget,
        CalculationFailed,
        StackRejected,
        AlreadyApplied,
    }

    public readonly struct AbilityVector3
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public AbilityVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public readonly struct HitContextData
    {
        public AbilityVector3 Point { get; }
        public AbilityVector3 Normal { get; }
        public AbilityVector3 Direction { get; }
        public int HitIndex { get; }

        public HitContextData(
            AbilityVector3 point,
            AbilityVector3 normal,
            AbilityVector3 direction,
            int hitIndex)
        {
            Point = point;
            Normal = normal;
            Direction = direction;
            HitIndex = hitIndex;
        }
    }

    public readonly struct GameplayEffectContext
    {
        public AbilitySystemHandle Instigator { get; }
        public AbilitySystemHandle EffectCauser { get; }
        public AbilitySystemHandle Target { get; }
        public AbilityExecutionHandle AbilityHandle { get; }
        public string SourceObjectId { get; }
        public AbilityVector3 Origin { get; }
        public HitContextData Hit { get; }
        public ulong RandomSeed { get; }

        public GameplayEffectContext(
            AbilitySystemHandle instigator,
            AbilitySystemHandle effectCauser,
            AbilitySystemHandle target,
            AbilityExecutionHandle abilityHandle = default,
            string sourceObjectId = null,
            AbilityVector3 origin = default,
            HitContextData hit = default,
            ulong randomSeed = 0)
        {
            Instigator = instigator;
            EffectCauser = effectCauser;
            Target = target;
            AbilityHandle = abilityHandle;
            SourceObjectId = sourceObjectId ?? string.Empty;
            Origin = origin;
            Hit = hit;
            RandomSeed = randomSeed;
        }
    }

    public readonly struct GameplayAttributeCaptureDefinition
    {
        public AttributeId AttributeId { get; }
        public GameplayEffectCaptureSource Source { get; }
        public GameplayEffectCapturePolicy Policy { get; }

        public GameplayAttributeCaptureDefinition(
            AttributeId attributeId,
            GameplayEffectCaptureSource source,
            GameplayEffectCapturePolicy policy)
        {
            AttributeId = attributeId;
            Source = source;
            Policy = policy;
        }
    }

    public readonly struct GameplayMagnitudeContext
    {
        private readonly GameplayEffectSpec _spec;
        private readonly AbilitySystemRuntime _source;
        private readonly AbilitySystemRuntime _target;

        internal GameplayMagnitudeContext(
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            AbilitySystemRuntime target)
        {
            _spec = spec;
            _source = source;
            _target = target;
        }

        public float Level => _spec.Level;

        public bool TryGetSetByCaller(AbilityTagId key, out float value) =>
            _spec.TryGetSetByCaller(key, out value);

        public bool TryGetCaptured(
            GameplayAttributeCaptureDefinition capture,
            out float value) =>
            _spec.TryResolveCapture(capture, _source, _target, out value);
    }

    public interface IGameplayMagnitudeCalculation
    {
        bool TryCalculate(in GameplayMagnitudeContext context, out float magnitude, out string error);
    }

    public sealed class FixedMagnitudeCalculation : IGameplayMagnitudeCalculation
    {
        public float Value { get; }
        public FixedMagnitudeCalculation(float value) => Value = value;

        public bool TryCalculate(
            in GameplayMagnitudeContext context,
            out float magnitude,
            out string error)
        {
            magnitude = Value;
            error = string.Empty;
            return true;
        }
    }

    public sealed class ScalableMagnitudeCalculation : IGameplayMagnitudeCalculation
    {
        public float BaseValue { get; }
        public float PerLevel { get; }

        public ScalableMagnitudeCalculation(float baseValue, float perLevel)
        {
            BaseValue = baseValue;
            PerLevel = perLevel;
        }

        public bool TryCalculate(
            in GameplayMagnitudeContext context,
            out float magnitude,
            out string error)
        {
            magnitude = BaseValue + Math.Max(0f, context.Level - 1f) * PerLevel;
            error = string.Empty;
            return true;
        }
    }

    public sealed class SetByCallerMagnitudeCalculation : IGameplayMagnitudeCalculation
    {
        public AbilityTagId Key { get; }
        public bool AllowDefault { get; }
        public float DefaultValue { get; }

        public SetByCallerMagnitudeCalculation(
            AbilityTagId key,
            bool allowDefault = false,
            float defaultValue = 0f)
        {
            Key = key;
            AllowDefault = allowDefault;
            DefaultValue = defaultValue;
        }

        public bool TryCalculate(
            in GameplayMagnitudeContext context,
            out float magnitude,
            out string error)
        {
            if (context.TryGetSetByCaller(Key, out magnitude))
            {
                error = string.Empty;
                return true;
            }
            magnitude = DefaultValue;
            error = AllowDefault ? string.Empty : $"필수 SetByCaller 누락: {Key}";
            return AllowDefault;
        }
    }

    public sealed class AttributeBasedMagnitudeCalculation : IGameplayMagnitudeCalculation
    {
        public GameplayAttributeCaptureDefinition Capture { get; }
        public float PreAdd { get; }
        public float Coefficient { get; }
        public float PostAdd { get; }

        public AttributeBasedMagnitudeCalculation(
            GameplayAttributeCaptureDefinition capture,
            float coefficient = 1f,
            float preAdd = 0f,
            float postAdd = 0f)
        {
            Capture = capture;
            Coefficient = coefficient;
            PreAdd = preAdd;
            PostAdd = postAdd;
        }

        public bool TryCalculate(
            in GameplayMagnitudeContext context,
            out float magnitude,
            out string error)
        {
            if (!context.TryGetCaptured(Capture, out float captured))
            {
                magnitude = 0f;
                error = $"Attribute Capture 누락: {Capture.AttributeId}";
                return false;
            }
            magnitude = (captured + PreAdd) * Coefficient + PostAdd;
            error = string.Empty;
            return true;
        }
    }

    public sealed class GameplayEffectModifierSpecDefinition
    {
        public AttributeId AttributeId { get; }
        public AttributeModifierOperation Operation { get; }
        public IGameplayMagnitudeCalculation Magnitude { get; }
        public int Priority { get; }

        public GameplayEffectModifierSpecDefinition(
            AttributeId attributeId,
            AttributeModifierOperation operation,
            IGameplayMagnitudeCalculation magnitude,
            int priority = 0)
        {
            AttributeId = attributeId;
            Operation = operation;
            Magnitude = magnitude ?? throw new ArgumentNullException(nameof(magnitude));
            Priority = priority;
        }
    }

    public readonly struct GameplayEffectExecutionInput
    {
        public GameplayEffectSpec Spec { get; }
        public AbilitySystemRuntime Source { get; }
        public AbilitySystemRuntime Target { get; }

        public GameplayEffectExecutionInput(
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            AbilitySystemRuntime target)
        {
            Spec = spec;
            Source = source;
            Target = target;
        }

        public bool TryGetSetByCaller(AbilityTagId key, out float value) =>
            Spec.TryGetSetByCaller(key, out value);
        public float GetSource(AttributeId id) => Source?.Attributes.GetCurrent(id) ?? 0f;
        public float GetTarget(AttributeId id) => Target?.Attributes.GetCurrent(id) ?? 0f;
    }

    public sealed class GameplayEffectExecutionOutput
    {
        private readonly List<AttributeDelta> _deltas = new();
        public IReadOnlyList<AttributeDelta> Deltas => _deltas;

        public void AddBaseDelta(AttributeId attributeId, float delta)
        {
            if (attributeId.IsValid && !float.IsNaN(delta) && !float.IsInfinity(delta))
                _deltas.Add(new AttributeDelta(attributeId, delta));
        }

        public readonly struct AttributeDelta
        {
            public AttributeId AttributeId { get; }
            public float Delta { get; }
            public AttributeDelta(AttributeId attributeId, float delta)
            {
                AttributeId = attributeId;
                Delta = delta;
            }
        }
    }

    public interface IGameplayEffectExecution
    {
        bool Execute(
            in GameplayEffectExecutionInput input,
            GameplayEffectExecutionOutput output,
            out string error);
    }

    public sealed class GameplayEffectDefinition
    {
        public string EffectId { get; }
        public GameplayEffectDurationPolicy DurationPolicy { get; }
        public IGameplayMagnitudeCalculation Duration { get; }
        public IGameplayMagnitudeCalculation Period { get; }
        public string StackingKey { get; }
        public AbilityEffectStackPolicy StackPolicy { get; }
        public int MaxStackCount { get; }
        public GameplayTagQuery ApplicationRequirement { get; }
        public GameplayTagQuery ImmunityQuery { get; }
        public IReadOnlyList<GameplayEffectModifierSpecDefinition> Modifiers { get; }
        public IReadOnlyList<IGameplayEffectExecution> Executions { get; }
        public IReadOnlyList<AbilityTagId> GrantedTags { get; }
        public bool SaveActiveEffect { get; }
        public bool ExecutePeriodicOnApplication { get; }

        public GameplayEffectDefinition(
            string effectId,
            GameplayEffectDurationPolicy durationPolicy,
            IEnumerable<GameplayEffectModifierSpecDefinition> modifiers = null,
            IEnumerable<IGameplayEffectExecution> executions = null,
            IGameplayMagnitudeCalculation duration = null,
            IGameplayMagnitudeCalculation period = null,
            string stackingKey = null,
            AbilityEffectStackPolicy stackPolicy = AbilityEffectStackPolicy.RejectNew,
            int maxStackCount = 1,
            GameplayTagQuery applicationRequirement = null,
            GameplayTagQuery immunityQuery = null,
            IEnumerable<AbilityTagId> grantedTags = null,
            bool saveActiveEffect = false,
            bool executePeriodicOnApplication = false)
        {
            EffectId = effectId?.Trim() ?? string.Empty;
            DurationPolicy = durationPolicy;
            Duration = duration;
            Period = period;
            StackingKey = string.IsNullOrWhiteSpace(stackingKey) ? EffectId : stackingKey.Trim();
            StackPolicy = stackPolicy;
            MaxStackCount = Math.Max(1, maxStackCount);
            ApplicationRequirement = applicationRequirement;
            ImmunityQuery = immunityQuery;
            Modifiers = modifiers == null
                ? Array.Empty<GameplayEffectModifierSpecDefinition>()
                : new List<GameplayEffectModifierSpecDefinition>(modifiers);
            Executions = executions == null
                ? Array.Empty<IGameplayEffectExecution>()
                : new List<IGameplayEffectExecution>(executions);
            GrantedTags = grantedTags == null
                ? Array.Empty<AbilityTagId>()
                : new List<AbilityTagId>(grantedTags);
            SaveActiveEffect = saveActiveEffect;
            ExecutePeriodicOnApplication = executePeriodicOnApplication;
        }
    }

    public sealed class GameplayEffectSpec
    {
        private readonly Dictionary<AbilityTagId, float> _setByCaller = new();
        private readonly Dictionary<CaptureKey, float> _captures = new();
        private readonly List<string> _trace = new();
        private bool _applied;

        private readonly struct CaptureKey : IEquatable<CaptureKey>
        {
            public readonly AttributeId AttributeId;
            public readonly GameplayEffectCaptureSource Source;
            public readonly GameplayEffectCapturePolicy Policy;

            public CaptureKey(GameplayAttributeCaptureDefinition capture)
            {
                AttributeId = capture.AttributeId;
                Source = capture.Source;
                Policy = capture.Policy;
            }
            public bool Equals(CaptureKey other) =>
                AttributeId.Equals(other.AttributeId) && Source == other.Source && Policy == other.Policy;
            public override bool Equals(object obj) => obj is CaptureKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(AttributeId, (int)Source, (int)Policy);
        }

        internal GameplayEffectSpec(
            GameplayEffectSpecHandle handle,
            GameplayEffectDefinition definition,
            float level,
            GameplayEffectContext context,
            AbilitySystemRuntime source)
        {
            Handle = handle;
            Definition = definition;
            Level = Math.Max(1f, level);
            Context = context;
            CaptureDefinitionAttributes(definition, source, GameplayEffectCapturePolicy.SnapshotOnCreate);
        }

        public GameplayEffectSpecHandle Handle { get; }
        public GameplayEffectDefinition Definition { get; }
        public float Level { get; }
        public GameplayEffectContext Context { get; }
        public IReadOnlyDictionary<AbilityTagId, float> SetByCaller => _setByCaller;
        public IReadOnlyList<string> Trace => _trace;

        public bool SetMagnitude(AbilityTagId key, float value)
        {
            if (_applied || !key.IsValid || float.IsNaN(value) || float.IsInfinity(value))
                return false;
            _setByCaller[key] = value;
            return true;
        }

        public bool TryGetSetByCaller(AbilityTagId key, out float value) =>
            _setByCaller.TryGetValue(key, out value);

        internal bool MarkApplied()
        {
            if (_applied) return false;
            _applied = true;
            return true;
        }

        internal void CaptureOnApply(AbilitySystemRuntime source, AbilitySystemRuntime target) =>
            CaptureDefinitionAttributes(Definition, source, GameplayEffectCapturePolicy.SnapshotOnApply, target);

        internal bool TryResolveCapture(
            GameplayAttributeCaptureDefinition capture,
            AbilitySystemRuntime source,
            AbilitySystemRuntime target,
            out float value)
        {
            var key = new CaptureKey(capture);
            if (capture.Policy != GameplayEffectCapturePolicy.EvaluateOnExecute
                && _captures.TryGetValue(key, out value))
                return true;
            AbilitySystemRuntime runtime = capture.Source == GameplayEffectCaptureSource.Source ? source : target;
            return runtime != null && runtime.Attributes.TryGet(capture.AttributeId, out GameplayAttributeValue attribute)
                ? Return(attribute.CurrentValue, out value)
                : Return(0f, out value, false);
        }

        public void AddTrace(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) _trace.Add(message);
        }

        private void CaptureDefinitionAttributes(
            GameplayEffectDefinition definition,
            AbilitySystemRuntime source,
            GameplayEffectCapturePolicy policy,
            AbilitySystemRuntime target = null)
        {
            for (int i = 0; i < definition.Modifiers.Count; i++)
            {
                if (definition.Modifiers[i].Magnitude is AttributeBasedMagnitudeCalculation attribute)
                    TryCapture(attribute.Capture, source, target, policy);
            }
        }

        private void TryCapture(
            GameplayAttributeCaptureDefinition capture,
            AbilitySystemRuntime source,
            AbilitySystemRuntime target,
            GameplayEffectCapturePolicy policy)
        {
            if (capture.Policy != policy) return;
            AbilitySystemRuntime runtime = capture.Source == GameplayEffectCaptureSource.Source ? source : target;
            if (runtime != null && runtime.Attributes.TryGet(capture.AttributeId, out GameplayAttributeValue value))
                _captures[new CaptureKey(capture)] = value.CurrentValue;
        }

        private static bool Return(float input, out float output, bool result = true)
        {
            output = input;
            return result;
        }
    }

    public sealed class GameplayEffectSpecFactory
    {
        private ulong _nextHandle = 1;

        public GameplayEffectSpec Create(
            GameplayEffectDefinition definition,
            float level,
            in GameplayEffectContext context,
            AbilitySystemRuntime source)
        {
            ulong value = _nextHandle++;
            if (value == 0) value = _nextHandle++;
            return new GameplayEffectSpec(
                new GameplayEffectSpecHandle(value), definition, level, context, source);
        }
    }
}
