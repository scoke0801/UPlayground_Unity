using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    public readonly struct ActiveGameplayEffectHandle : IEquatable<ActiveGameplayEffectHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public ActiveGameplayEffectHandle(ulong value) => Value = value;
        public bool Equals(ActiveGameplayEffectHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ActiveGameplayEffectHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct GameplayEffectApplyOutcome
    {
        public GameplayEffectApplyResult Result { get; }
        public ActiveGameplayEffectHandle ActiveHandle { get; }
        public string Error { get; }
        public bool Succeeded => Result == GameplayEffectApplyResult.Success;

        public GameplayEffectApplyOutcome(
            GameplayEffectApplyResult result,
            ActiveGameplayEffectHandle activeHandle = default,
            string error = null)
        {
            Result = result;
            ActiveHandle = activeHandle;
            Error = error ?? string.Empty;
        }
    }

    public sealed class ActiveGameplayEffect
    {
        internal readonly List<AttributeModifierHandle> ModifierHandles = new();
        internal readonly List<GameplayTagSourceHandle> TagHandles = new();

        internal ActiveGameplayEffect(
            ActiveGameplayEffectHandle handle,
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            float startTime,
            float duration,
            float period)
        {
            Handle = handle;
            Spec = spec;
            Source = source;
            StartTime = startTime;
            RemainingSeconds = duration;
            DurationSeconds = duration;
            PeriodSeconds = period;
            NextPeriodSeconds = period;
            StackCount = 1;
        }

        public ActiveGameplayEffectHandle Handle { get; }
        public GameplayEffectSpec Spec { get; }
        public AbilitySystemRuntime Source { get; }
        public float StartTime { get; }
        public float DurationSeconds { get; internal set; }
        public float RemainingSeconds { get; internal set; }
        public float PeriodSeconds { get; }
        public float NextPeriodSeconds { get; internal set; }
        public int StackCount { get; internal set; }
    }

    public sealed class ActiveGameplayEffectContainer : IDisposable
    {
        private readonly AbilitySystemRuntime _owner;
        private readonly IAbilityClock _clock;
        private readonly Dictionary<ulong, ActiveGameplayEffect> _active = new();
        private readonly Dictionary<string, ActiveGameplayEffectHandle> _stacking =
            new(StringComparer.Ordinal);
        private readonly List<ActiveGameplayEffect> _tickSnapshot = new();
        private readonly List<ActiveGameplayEffectHandle> _expiredHandles = new();
        private ulong _nextHandle = 1;
        private float _lastTickTime;

        public ActiveGameplayEffectContainer(AbilitySystemRuntime owner, IAbilityClock clock)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _lastTickTime = clock.Time;
        }

        public int Count => _active.Count;

        public GameplayEffectApplyOutcome Apply(
            GameplayEffectSpec spec,
            AbilitySystemRuntime source = null)
        {
            if (spec?.Definition == null || string.IsNullOrWhiteSpace(spec.Definition.EffectId))
                return Fail(GameplayEffectApplyResult.InvalidDefinition, spec, "Effect Definition이 유효하지 않습니다.");
            if (!spec.Handle.IsValid || !spec.MarkApplied())
                return Fail(GameplayEffectApplyResult.AlreadyApplied, spec, "동일 Spec은 한 번만 적용할 수 있습니다.");
            if (spec.Context.Target.IsValid && !spec.Context.Target.Equals(_owner.Handle))
                return Fail(GameplayEffectApplyResult.InvalidTarget, spec, "Spec Target과 적용 대상이 다릅니다.");

            GameplayEffectDefinition definition = spec.Definition;
            if (definition.ApplicationRequirement != null
                && !_owner.Tags.Matches(definition.ApplicationRequirement))
                return Fail(GameplayEffectApplyResult.BlockedByTag, spec, "Application Tag 요구를 만족하지 않습니다.");
            if (definition.ImmunityQuery != null && _owner.Tags.Matches(definition.ImmunityQuery))
                return Fail(GameplayEffectApplyResult.Immune, spec, "대상이 Immunity Tag를 보유합니다.");

            spec.CaptureOnApply(source, _owner);
            if (!TryCalculate(definition.Duration, spec, source, out float duration, out string error))
                return Fail(ClassifyCalculationError(error), spec, error);
            if (!TryCalculate(definition.Period, spec, source, out float period, out error))
                return Fail(ClassifyCalculationError(error), spec, error);

            duration = Math.Max(0f, duration);
            period = Math.Max(0f, period);
            if (definition.DurationPolicy == GameplayEffectDurationPolicy.Duration && duration <= 0f)
                return Fail(GameplayEffectApplyResult.CalculationFailed, spec, "Duration Effect의 지속시간은 0보다 커야 합니다.");

            if (definition.DurationPolicy == GameplayEffectDurationPolicy.Instant)
            {
                GameplayEffectApplyResult execution = ExecuteInstant(spec, source, 1, out error);
                if (execution != GameplayEffectApplyResult.Success)
                    return Fail(execution, spec, error);
                Record("AppliedInstant", spec);
                return new GameplayEffectApplyOutcome(GameplayEffectApplyResult.Success);
            }

            if (_stacking.TryGetValue(definition.StackingKey, out ActiveGameplayEffectHandle existingHandle)
                && _active.TryGetValue(existingHandle.Value, out ActiveGameplayEffect existing))
                return ResolveStack(existing, spec, source, duration);

            ulong value = _nextHandle++;
            if (value == 0) value = _nextHandle++;
            var handle = new ActiveGameplayEffectHandle(value);
            var active = new ActiveGameplayEffect(
                handle, spec, source, _clock.Time,
                definition.DurationPolicy == GameplayEffectDurationPolicy.Infinite ? 0f : duration,
                period);
            _active.Add(value, active);
            _stacking[definition.StackingKey] = handle;
            if (!RebuildGrants(active, out error))
            {
                RemoveInternal(active, false);
                return Fail(ClassifyCalculationError(error), spec, error);
            }
            if (period > 0f && definition.ExecutePeriodicOnApplication
                && !ExecuteExecutions(spec, source, active.StackCount, out error))
            {
                RemoveInternal(active, false);
                return Fail(ClassifyCalculationError(error), spec, error);
            }

            Record("Applied", spec, active.Handle.Value);
            return new GameplayEffectApplyOutcome(GameplayEffectApplyResult.Success, handle);
        }

        public bool Remove(ActiveGameplayEffectHandle handle)
        {
            if (!handle.IsValid || !_active.TryGetValue(handle.Value, out ActiveGameplayEffect active))
                return false;
            RemoveInternal(active, true);
            return true;
        }

        public void Tick()
        {
            float now = _clock.Time;
            float delta = Math.Max(0f, now - _lastTickTime);
            _lastTickTime = now;
            if (delta <= 0f || _active.Count == 0) return;

            _tickSnapshot.Clear();
            _expiredHandles.Clear();
            _tickSnapshot.AddRange(_active.Values);
            try
            {
                for (int i = 0; i < _tickSnapshot.Count; i++)
                {
                    ActiveGameplayEffect active = _tickSnapshot[i];
                    if (!_active.ContainsKey(active.Handle.Value)) continue;
                    float periodicDelta = active.Spec.Definition.DurationPolicy
                                          == GameplayEffectDurationPolicy.Duration
                        ? Math.Min(delta, Math.Max(0f, active.RemainingSeconds))
                        : delta;
                    if (active.PeriodSeconds > 0f)
                    {
                        active.NextPeriodSeconds -= periodicDelta;
                        while (active.NextPeriodSeconds <= 0f)
                        {
                            ExecuteExecutions(
                                active.Spec,
                                active.Source,
                                active.StackCount,
                                out _);
                            active.NextPeriodSeconds += active.PeriodSeconds;
                        }
                    }

                    if (active.Spec.Definition.DurationPolicy
                        != GameplayEffectDurationPolicy.Duration)
                    {
                        continue;
                    }
                    active.RemainingSeconds -= delta;
                    if (active.RemainingSeconds <= 0f)
                        _expiredHandles.Add(active.Handle);
                }

                for (int i = 0; i < _expiredHandles.Count; i++)
                    Remove(_expiredHandles[i]);
            }
            finally
            {
                _tickSnapshot.Clear();
                _expiredHandles.Clear();
            }
        }

        public void CopyActive(ICollection<ActiveGameplayEffect> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (ActiveGameplayEffect active in _active.Values) destination.Add(active);
        }

        internal bool RestoreState(
            ActiveGameplayEffectHandle handle,
            float remainingSeconds,
            int stackCount)
        {
            if (!handle.IsValid
                || !_active.TryGetValue(handle.Value, out ActiveGameplayEffect active))
                return false;

            active.StackCount = Math.Min(
                Math.Max(1, stackCount),
                active.Spec.Definition.MaxStackCount);
            if (active.Spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Duration)
            {
                active.RemainingSeconds = Math.Min(
                    active.DurationSeconds,
                    Math.Max(0f, remainingSeconds));
            }
            if (!RebuildGrants(active, out _))
            {
                RemoveInternal(active, false);
                return false;
            }
            return true;
        }

        public void Clear()
        {
            var handles = new List<ActiveGameplayEffectHandle>(_active.Count);
            foreach (ActiveGameplayEffect active in _active.Values) handles.Add(active.Handle);
            for (int i = 0; i < handles.Count; i++) Remove(handles[i]);
        }

        public void Dispose()
        {
            Clear();
        }

        private GameplayEffectApplyOutcome ResolveStack(
            ActiveGameplayEffect existing,
            GameplayEffectSpec incoming,
            AbilitySystemRuntime source,
            float duration)
        {
            AbilityEffectStackResult result = AbilityEffectStackRuntime.Resolve(
                incoming.Definition.StackPolicy,
                existing.StackCount,
                incoming.Definition.MaxStackCount);
            switch (result.Action)
            {
                case AbilityEffectStackAction.KeepExisting:
                    return Fail(GameplayEffectApplyResult.StackRejected, incoming, "Stack 정책이 신규 적용을 거부했습니다.");
                case AbilityEffectStackAction.ReplaceExisting:
                    Remove(existing.Handle);
                    // incoming은 이미 Applied로 표시됐으므로 교체 인스턴스를 직접 생성한다.
                    return AddReplacement(incoming, source, duration);
                case AbilityEffectStackAction.RefreshExisting:
                    existing.StackCount = result.StackCount;
                    if (existing.Spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Duration)
                    {
                        existing.DurationSeconds = duration;
                        existing.RemainingSeconds = duration;
                    }
                    if (!RebuildGrants(existing, out string error))
                        return Fail(ClassifyCalculationError(error), incoming, error);
                    Record("Stacked", incoming, existing.Handle.Value);
                    return new GameplayEffectApplyOutcome(GameplayEffectApplyResult.Success, existing.Handle);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private GameplayEffectApplyOutcome AddReplacement(
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            float duration)
        {
            ulong value = _nextHandle++;
            if (value == 0) value = _nextHandle++;
            var handle = new ActiveGameplayEffectHandle(value);
            TryCalculate(spec.Definition.Period, spec, source, out float period, out _);
            var active = new ActiveGameplayEffect(
                handle, spec, source, _clock.Time,
                spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Infinite ? 0f : duration,
                Math.Max(0f, period));
            _active.Add(value, active);
            _stacking[spec.Definition.StackingKey] = handle;
            if (!RebuildGrants(active, out string error))
            {
                RemoveInternal(active, false);
                return Fail(ClassifyCalculationError(error), spec, error);
            }
            Record("Replaced", spec, handle.Value);
            return new GameplayEffectApplyOutcome(GameplayEffectApplyResult.Success, handle);
        }

        private bool RebuildGrants(ActiveGameplayEffect active, out string error)
        {
            for (int i = 0; i < active.ModifierHandles.Count; i++)
                _owner.Attributes.RemoveModifier(active.ModifierHandles[i]);
            active.ModifierHandles.Clear();

            GameplayEffectDefinition definition = active.Spec.Definition;
            for (int i = 0; i < definition.Modifiers.Count; i++)
            {
                GameplayEffectModifierSpecDefinition modifier = definition.Modifiers[i];
                if (!TryCalculate(modifier.Magnitude, active.Spec, active.Source, out float magnitude, out error))
                    return false;
                AttributeModifierHandle handle = _owner.Attributes.AddModifier(
                    modifier.AttributeId,
                    modifier.Operation,
                    magnitude * active.StackCount,
                    "Effect",
                    active.Handle.Value,
                    modifier.Priority);
                if (!handle.IsValid)
                {
                    error = $"대상 Attribute 누락: {modifier.AttributeId}";
                    return false;
                }
                active.ModifierHandles.Add(handle);
                active.Spec.AddTrace($"{modifier.AttributeId} {modifier.Operation} = {magnitude * active.StackCount}");
            }

            if (active.TagHandles.Count == 0)
            {
                for (int i = 0; i < definition.GrantedTags.Count; i++)
                {
                    GameplayTagSourceHandle handle = _owner.Tags.Add(
                        definition.GrantedTags[i], "Effect", active.Handle.Value);
                    if (handle.IsValid) active.TagHandles.Add(handle);
                }
            }
            error = string.Empty;
            return true;
        }

        private GameplayEffectApplyResult ExecuteInstant(
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            int stackCount,
            out string error)
        {
            using AttributeSetRuntime.Transaction transaction =
                _owner.Attributes.BeginTransaction(spec.Handle.Value);
            for (int i = 0; i < spec.Definition.Modifiers.Count; i++)
            {
                GameplayEffectModifierSpecDefinition modifier = spec.Definition.Modifiers[i];
                if (modifier.Operation != AttributeModifierOperation.Add)
                {
                    error = "Instant Effect Modifier는 Add 연산만 사용할 수 있습니다.";
                    return GameplayEffectApplyResult.CalculationFailed;
                }
                if (!TryCalculate(modifier.Magnitude, spec, source, out float magnitude, out error))
                    return ClassifyCalculationError(error);
                if (!transaction.AddToBase(modifier.AttributeId, magnitude * stackCount))
                {
                    error = $"대상 Attribute 누락: {modifier.AttributeId}";
                    return GameplayEffectApplyResult.MissingAttribute;
                }
                spec.AddTrace($"{modifier.AttributeId} BaseDelta = {magnitude * stackCount}");
            }

            if (!ExecuteExecutions(spec, source, stackCount, out error, transaction))
                return ClassifyCalculationError(error);
            transaction.Commit();
            return GameplayEffectApplyResult.Success;
        }

        private bool ExecuteExecutions(
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            int stackCount,
            out string error,
            AttributeSetRuntime.Transaction sharedTransaction = null)
        {
            bool ownsTransaction = sharedTransaction == null;
            AttributeSetRuntime.Transaction transaction = sharedTransaction
                ?? _owner.Attributes.BeginTransaction(spec.Handle.Value);
            try
            {
                for (int i = 0; i < spec.Definition.Executions.Count; i++)
                {
                    var output = new GameplayEffectExecutionOutput();
                    var input = new GameplayEffectExecutionInput(spec, source, _owner);
                    if (!spec.Definition.Executions[i].Execute(input, output, out error))
                        return false;
                    for (int j = 0; j < output.Deltas.Count; j++)
                    {
                        GameplayEffectExecutionOutput.AttributeDelta delta = output.Deltas[j];
                        if (!transaction.AddToBase(delta.AttributeId, delta.Delta * stackCount))
                        {
                            error = $"Execution 대상 Attribute 누락: {delta.AttributeId}";
                            return false;
                        }
                        spec.AddTrace($"Execution {delta.AttributeId} BaseDelta = {delta.Delta * stackCount}");
                    }
                }
                if (ownsTransaction) transaction.Commit();
                error = string.Empty;
                return true;
            }
            finally
            {
                if (ownsTransaction) transaction.Dispose();
            }
        }

        private bool TryCalculate(
            IGameplayMagnitudeCalculation calculation,
            GameplayEffectSpec spec,
            AbilitySystemRuntime source,
            out float value,
            out string error)
        {
            if (calculation == null)
            {
                value = 0f;
                error = string.Empty;
                return true;
            }
            var context = new GameplayMagnitudeContext(spec, source, _owner);
            bool result = calculation.TryCalculate(context, out value, out error);
            if (result) spec.AddTrace($"Magnitude = {value}");
            return result;
        }

        private void RemoveInternal(ActiveGameplayEffect active, bool record)
        {
            for (int i = 0; i < active.ModifierHandles.Count; i++)
                _owner.Attributes.RemoveModifier(active.ModifierHandles[i]);
            for (int i = 0; i < active.TagHandles.Count; i++)
                _owner.Tags.Remove(active.TagHandles[i]);
            active.ModifierHandles.Clear();
            active.TagHandles.Clear();
            _active.Remove(active.Handle.Value);
            if (_stacking.TryGetValue(active.Spec.Definition.StackingKey, out ActiveGameplayEffectHandle mapped)
                && mapped.Equals(active.Handle))
                _stacking.Remove(active.Spec.Definition.StackingKey);
            if (record) Record("Removed", active.Spec, active.Handle.Value);
        }

        private GameplayEffectApplyOutcome Fail(
            GameplayEffectApplyResult result,
            GameplayEffectSpec spec,
            string error)
        {
            spec?.AddTrace(error);
            Record("Rejected", spec, result: result.ToString(), message: error);
            return new GameplayEffectApplyOutcome(result, error: error);
        }

        private void Record(
            string eventType,
            GameplayEffectSpec spec,
            ulong activeHandle = 0,
            string result = null,
            string message = null) =>
            _owner.Debug.Record(
                AbilityDebugCategory.Effect,
                eventType,
                abilityHandle: spec?.Context.AbilityHandle.Value ?? 0,
                effectHandle: activeHandle != 0 ? activeHandle : spec?.Handle.Value ?? 0,
                result: result,
                source: spec?.Definition?.EffectId,
                message: message);

        private static GameplayEffectApplyResult ClassifyCalculationError(string error)
        {
            if (error?.IndexOf("SetByCaller", StringComparison.Ordinal) >= 0)
                return GameplayEffectApplyResult.MissingSetByCaller;
            if (error?.IndexOf("Attribute", StringComparison.Ordinal) >= 0)
                return GameplayEffectApplyResult.MissingAttribute;
            return GameplayEffectApplyResult.CalculationFailed;
        }
    }
}
