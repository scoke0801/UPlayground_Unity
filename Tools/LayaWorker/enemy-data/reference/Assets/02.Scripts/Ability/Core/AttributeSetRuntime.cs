using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    /// <summary>
    /// 프로젝트 타입에 의존하지 않는 Attribute 단일 권위 저장소.
    /// 변경은 Transaction 단위로 계산한 뒤 이벤트를 일괄 발행한다.
    /// </summary>
    public sealed class AttributeSetRuntime : IAttributeReader
    {
        private const float Epsilon = 0.00001f;

        private sealed class Entry
        {
            public AttributeId AttributeId;
            public GameplayAttributeDefinition Definition;
            public float Base;
            public float Current;
        }

        private readonly struct AttributeKey : IEquatable<AttributeKey>
        {
            public readonly AttributeHandle Handle;
            public readonly AttributeId FallbackId;
            public bool IsValid => Handle.IsValid || FallbackId.IsValid;

            public AttributeKey(
                AttributeHandle handle,
                AttributeId fallbackId)
            {
                Handle = handle;
                FallbackId = fallbackId;
            }

            public bool Equals(AttributeKey other)
            {
                if (Handle.IsValid || other.Handle.IsValid)
                    return Handle == other.Handle;
                return FallbackId == other.FallbackId;
            }

            public override bool Equals(object obj) =>
                obj is AttributeKey other && Equals(other);
            public override int GetHashCode() =>
                Handle.IsValid
                    ? Handle.GetHashCode()
                    : FallbackId.GetHashCode();
        }

        private readonly struct Modifier
        {
            public readonly AttributeModifierHandle Handle;
            public readonly AttributeKey AttributeKey;
            public readonly AttributeId AttributeId;
            public readonly AttributeModifierOperation Operation;
            public readonly float Magnitude;
            public readonly int Priority;
            public readonly ulong Sequence;
            public readonly string SourceType;
            public readonly ulong SourceId;

            public Modifier(
                AttributeModifierHandle handle,
                AttributeKey attributeKey,
                AttributeId attributeId,
                AttributeModifierOperation operation,
                float magnitude,
                int priority,
                ulong sequence,
                string sourceType,
                ulong sourceId)
            {
                Handle = handle;
                AttributeKey = attributeKey;
                AttributeId = attributeId;
                Operation = operation;
                Magnitude = magnitude;
                Priority = priority;
                Sequence = sequence;
                SourceType = sourceType ?? string.Empty;
                SourceId = sourceId;
            }
        }

        public sealed class Transaction : IDisposable
        {
            private readonly AttributeSetRuntime _owner;
            private readonly Dictionary<AttributeKey, float> _baseChanges =
                new();
            private bool _completed;

            internal Transaction(
                AttributeSetRuntime owner,
                AttributeTransactionHandle handle,
                ulong sourceSpecHandle)
            {
                _owner = owner;
                Handle = handle;
                SourceSpecHandle = sourceSpecHandle;
            }

            public AttributeTransactionHandle Handle { get; }
            public ulong SourceSpecHandle { get; }

            public bool SetBase(AttributeId id, float value)
            {
                if (_completed
                    || !_owner.TryGetKey(id, out AttributeKey key)
                    || !_owner._entries.ContainsKey(key)
                    || float.IsNaN(value)
                    || float.IsInfinity(value))
                    return false;
                _baseChanges[key] = value;
                return true;
            }

            public bool AddToBase(AttributeId id, float delta)
            {
                if (_completed
                    || !_owner.TryGetKey(id, out AttributeKey key)
                    || !_owner.TryGet(
                        key,
                        out GameplayAttributeValue value)
                    || float.IsNaN(delta) || float.IsInfinity(delta))
                    return false;
                float baseValue = _baseChanges.TryGetValue(
                    key,
                    out float pending)
                    ? pending
                    : value.BaseValue;
                _baseChanges[key] = baseValue + delta;
                return true;
            }

            public bool Commit()
            {
                if (_completed) return false;
                _completed = true;
                return _owner.Commit(this, _baseChanges);
            }

            public void Dispose()
            {
                _completed = true;
            }
        }

        private readonly IAttributeResolver _resolver;
        private readonly Dictionary<AttributeKey, Entry> _entries = new();
        private readonly Dictionary<ulong, Modifier> _modifiersByHandle = new();
        private readonly Dictionary<AttributeKey, List<Modifier>>
            _modifiersByAttribute = new();
        private ulong _nextModifierHandle = 1;
        private ulong _nextTransactionHandle = 1;
        private ulong _modifierSequence;
        private bool _isPublishingChanges;
        private readonly Queue<Action> _deferredMutations = new();

        public event Action<AttributeChangedEvent> AttributeChanged;
        public int Count => _entries.Count;
        public int ModifierCount => _modifiersByHandle.Count;

        public AttributeSetRuntime(IAttributeResolver resolver = null)
        {
            _resolver = resolver;
        }

        public bool Register(GameplayAttributeDefinition definition, float? initialBase = null)
        {
            if (definition == null
                || !TryGetKey(
                    definition.AttributeId,
                    out AttributeKey key)
                || _entries.ContainsKey(key))
                return false;

            float value = initialBase ?? definition.DefaultBaseValue;
            var entry = new Entry
            {
                AttributeId = definition.AttributeId,
                Definition = definition,
                Base = value,
                Current = value,
            };
            _entries.Add(key, entry);
            RecalculateAll();
            return true;
        }

        public int Register(AttributeSetDefinitionSO definition)
        {
            if (definition == null) return 0;
            int added = 0;
            IReadOnlyList<GameplayAttributeDefinition> attributes = definition.Attributes;
            for (int i = 0; i < attributes.Count; i++)
                if (Register(attributes[i])) added++;
            return added;
        }

        public bool Contains(AttributeId id) =>
            TryGetKey(id, out AttributeKey key)
            && _entries.ContainsKey(key);

        public bool TryGet(AttributeId id, out GameplayAttributeValue value)
        {
            if (TryGetKey(id, out AttributeKey key)
                && TryGet(key, out value))
                return true;

            value = default;
            return false;
        }

        private bool TryGet(
            AttributeKey key,
            out GameplayAttributeValue value)
        {
            if (_entries.TryGetValue(key, out Entry entry))
            {
                value = new GameplayAttributeValue(entry.Base, entry.Current);
                return true;
            }

            value = default;
            return false;
        }

        public float GetBase(AttributeId id) =>
            TryGetKey(id, out AttributeKey key)
            && _entries.TryGetValue(key, out Entry entry)
                ? entry.Base
                : 0f;

        public float GetCurrent(AttributeId id) =>
            TryGetKey(id, out AttributeKey key)
            && _entries.TryGetValue(key, out Entry entry)
                ? entry.Current
                : 0f;

        public Transaction BeginTransaction(ulong sourceSpecHandle = 0)
        {
            ulong value = _nextTransactionHandle++;
            if (value == 0) value = _nextTransactionHandle++;
            return new Transaction(this, new AttributeTransactionHandle(value), sourceSpecHandle);
        }

        public bool SetBase(AttributeId id, float value, ulong sourceSpecHandle = 0)
        {
            if (_isPublishingChanges)
            {
                _deferredMutations.Enqueue(() => SetBase(id, value, sourceSpecHandle));
                return true;
            }

            using Transaction transaction = BeginTransaction(sourceSpecHandle);
            return transaction.SetBase(id, value) && transaction.Commit();
        }

        public bool AddToBase(AttributeId id, float delta, ulong sourceSpecHandle = 0)
        {
            if (_isPublishingChanges)
            {
                _deferredMutations.Enqueue(() => AddToBase(id, delta, sourceSpecHandle));
                return true;
            }

            using Transaction transaction = BeginTransaction(sourceSpecHandle);
            return transaction.AddToBase(id, delta) && transaction.Commit();
        }

        public AttributeModifierHandle AddModifier(
            AttributeId id,
            AttributeModifierOperation operation,
            float magnitude,
            string sourceType,
            ulong sourceId,
            int priority = 0)
        {
            if (_isPublishingChanges)
                throw new InvalidOperationException("Attribute 변경 콜백에서는 Modifier를 즉시 변경할 수 없습니다.");
            if (!Contains(id) || float.IsNaN(magnitude) || float.IsInfinity(magnitude))
                return default;
            TryGetKey(id, out AttributeKey key);

            Dictionary<AttributeKey, GameplayAttributeValue> before =
                CaptureValues();
            ulong value = _nextModifierHandle++;
            if (value == 0) value = _nextModifierHandle++;
            var handle = new AttributeModifierHandle(value);
            var modifier = new Modifier(
                handle, key, id, operation, magnitude, priority,
                ++_modifierSequence, sourceType, sourceId);
            _modifiersByHandle.Add(value, modifier);
            if (!_modifiersByAttribute.TryGetValue(
                    key,
                    out List<Modifier> list))
            {
                list = new List<Modifier>();
                _modifiersByAttribute.Add(key, list);
            }
            list.Add(modifier);

            RecalculateAll();
            ApplyMaximumModifierPolicies(before);
            RecalculateAll();
            using (Transaction transaction = BeginTransaction(sourceId))
                PublishChanges(before, transaction.Handle, sourceId);
            return handle;
        }

        public bool RemoveModifier(AttributeModifierHandle handle)
        {
            if (_isPublishingChanges)
                throw new InvalidOperationException("Attribute 변경 콜백에서는 Modifier를 즉시 변경할 수 없습니다.");
            if (!handle.IsValid || !_modifiersByHandle.Remove(handle.Value, out Modifier modifier))
                return false;

            Dictionary<AttributeKey, GameplayAttributeValue> before =
                CaptureValues();
            if (_modifiersByAttribute.TryGetValue(
                    modifier.AttributeKey,
                    out List<Modifier> list))
            {
                list.RemoveAll(item => item.Handle.Equals(handle));
                if (list.Count == 0)
                    _modifiersByAttribute.Remove(
                        modifier.AttributeKey);
            }

            RecalculateAll();
            ApplyMaximumModifierPolicies(before);
            RecalculateAll();
            using (Transaction transaction = BeginTransaction(modifier.SourceId))
                PublishChanges(before, transaction.Handle, modifier.SourceId);
            return true;
        }

        public int RemoveModifiersBySource(string sourceType, ulong sourceId)
        {
            var handles = new List<AttributeModifierHandle>();
            foreach (Modifier modifier in _modifiersByHandle.Values)
            {
                if (modifier.SourceId == sourceId
                    && string.Equals(modifier.SourceType, sourceType ?? string.Empty, StringComparison.Ordinal))
                    handles.Add(modifier.Handle);
            }

            for (int i = 0; i < handles.Count; i++) RemoveModifier(handles[i]);
            return handles.Count;
        }

        public void CopyValues(IDictionary<AttributeId, GameplayAttributeValue> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
                destination[pair.Value.AttributeId] =
                    new GameplayAttributeValue(
                        pair.Value.Base,
                        pair.Value.Current);
        }

        public void CopySaveableBases(ICollection<AttributeSaveEntry> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
            {
                if (pair.Value.Definition.SaveBaseValue)
                    destination.Add(new AttributeSaveEntry(
                        pair.Value.AttributeId.Value,
                        pair.Value.Base));
            }
        }

        private bool Commit(
            Transaction transaction,
            Dictionary<AttributeKey, float> changes)
        {
            if (changes.Count == 0) return true;
            if (_isPublishingChanges)
            {
                var copy = new Dictionary<AttributeKey, float>(changes);
                _deferredMutations.Enqueue(() => CommitDeferred(copy, transaction.SourceSpecHandle));
                return true;
            }

            var before = CaptureValues();
            foreach (KeyValuePair<AttributeKey, float> change in changes)
                _entries[change.Key].Base = change.Value;

            ApplyMaximumChangePolicies(before, changes);
            RecalculateAll();
            PublishChanges(before, transaction.Handle, transaction.SourceSpecHandle);
            return true;
        }

        private void CommitDeferred(
            Dictionary<AttributeKey, float> changes,
            ulong sourceSpecHandle)
        {
            using Transaction transaction = BeginTransaction(sourceSpecHandle);
            Commit(transaction, changes);
        }

        private Dictionary<AttributeKey, GameplayAttributeValue> CaptureValues()
        {
            var result =
                new Dictionary<AttributeKey, GameplayAttributeValue>(
                    _entries.Count);
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
                result[pair.Key] = new GameplayAttributeValue(
                    pair.Value.Base,
                    pair.Value.Current);
            return result;
        }

        private void ApplyMaximumChangePolicies(
            Dictionary<AttributeKey, GameplayAttributeValue> before,
            Dictionary<AttributeKey, float> changes)
        {
            foreach (KeyValuePair<AttributeKey, float> changed in changes)
            {
                Entry maximumEntry = _entries[changed.Key];
                AttributeId resourceId = maximumEntry.Definition.DependentResourceId;
                if (!TryGetKey(resourceId, out AttributeKey resourceKey)
                    || !_entries.TryGetValue(
                        resourceKey,
                        out Entry resource))
                    continue;
                if (changes.ContainsKey(resourceKey))
                    continue;

                float oldMaximum = before[changed.Key].CurrentValue;
                float newMaximum = changed.Value;
                float oldResource = before[resourceKey].BaseValue;
                resource.Base = maximumEntry.Definition.MaxChangePolicy switch
                {
                    AttributeMaxChangePolicy.PreserveRatio when oldMaximum > Epsilon =>
                        oldResource / oldMaximum * newMaximum,
                    AttributeMaxChangePolicy.FillOnIncrease =>
                        oldResource + Math.Max(0f, newMaximum - oldMaximum),
                    AttributeMaxChangePolicy.Refill => newMaximum,
                    _ => oldResource,
                };
            }
        }

        private void ApplyMaximumModifierPolicies(
            Dictionary<AttributeKey, GameplayAttributeValue> before)
        {
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
            {
                AttributeId resourceId = pair.Value.Definition.DependentResourceId;
                if (!TryGetKey(resourceId, out AttributeKey resourceKey)
                    || !_entries.TryGetValue(
                        resourceKey,
                        out Entry resource))
                    continue;
                float oldMaximum = before[pair.Key].CurrentValue;
                float newMaximum = pair.Value.Current;
                if (Math.Abs(oldMaximum - newMaximum) < Epsilon) continue;
                float oldResource = before[resourceKey].BaseValue;
                resource.Base = pair.Value.Definition.MaxChangePolicy switch
                {
                    AttributeMaxChangePolicy.PreserveRatio when oldMaximum > Epsilon =>
                        oldResource / oldMaximum * newMaximum,
                    AttributeMaxChangePolicy.FillOnIncrease =>
                        oldResource + Math.Max(0f, newMaximum - oldMaximum),
                    AttributeMaxChangePolicy.Refill => newMaximum,
                    _ => oldResource,
                };
            }
        }

        private void RecalculateAll()
        {
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
                pair.Value.Current = Aggregate(pair.Key, pair.Value.Base);

            // Attribute 기반 Clamp가 다른 Attribute의 재계산 결과를 보도록 별도 패스로 처리한다.
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
            {
                pair.Value.Current = Clamp(pair.Value.Definition, pair.Value.Current);
                if (pair.Value.Definition.IsMetaAttribute)
                    continue;
                // 소모형 Attribute의 Base도 범위를 벗어나지 않게 보정한다.
                if (Math.Abs(pair.Value.Base - pair.Value.Current) < Epsilon
                    || !_modifiersByAttribute.ContainsKey(pair.Key))
                    pair.Value.Base = Clamp(pair.Value.Definition, pair.Value.Base);
            }
        }

        private float Aggregate(AttributeKey id, float baseValue)
        {
            if (!_modifiersByAttribute.TryGetValue(id, out List<Modifier> modifiers))
                return baseValue;

            float add = 0f;
            float percent = 0f;
            float multiply = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;
            int overridePriority = int.MinValue;
            ulong overrideSequence = 0;

            for (int i = 0; i < modifiers.Count; i++)
            {
                Modifier modifier = modifiers[i];
                switch (modifier.Operation)
                {
                    case AttributeModifierOperation.Add:
                        add += modifier.Magnitude;
                        break;
                    case AttributeModifierOperation.Percent:
                        percent += modifier.Magnitude;
                        break;
                    case AttributeModifierOperation.Multiply:
                        multiply *= modifier.Magnitude;
                        break;
                    case AttributeModifierOperation.Override:
                        if (!hasOverride || modifier.Priority > overridePriority
                            || modifier.Priority == overridePriority && modifier.Sequence > overrideSequence)
                        {
                            hasOverride = true;
                            overrideValue = modifier.Magnitude;
                            overridePriority = modifier.Priority;
                            overrideSequence = modifier.Sequence;
                        }
                        break;
                }
            }

            return hasOverride ? overrideValue : (baseValue + add) * (1f + percent) * multiply;
        }

        private float Clamp(GameplayAttributeDefinition definition, float value)
        {
            switch (definition.ClampPolicy)
            {
                case AttributeClampPolicy.None:
                    return value;
                case AttributeClampPolicy.FixedRange:
                    return Math.Min(Math.Max(value, definition.FixedMinimum), definition.FixedMaximum);
                case AttributeClampPolicy.AttributeRange:
                    float minimum = definition.MinimumAttributeId.IsValid
                        ? Math.Max(
                            definition.FixedMinimum,
                            GetCurrent(definition.MinimumAttributeId))
                        : definition.FixedMinimum;
                    float maximum = definition.MaximumAttributeId.IsValid
                        ? Math.Min(
                            definition.FixedMaximum,
                            GetCurrent(definition.MaximumAttributeId))
                        : definition.FixedMaximum;
                    return Math.Min(Math.Max(value, minimum), maximum);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PublishSingleCurrentChange(AttributeId id, float oldCurrent, ulong sourceId)
        {
            if (!TryGetKey(id, out AttributeKey key)
                || !_entries.TryGetValue(key, out Entry entry)
                || Math.Abs(oldCurrent - entry.Current) < Epsilon)
                return;
            using Transaction transaction = BeginTransaction(sourceId);
            Publish(new AttributeChangedEvent(
                id, entry.Base, entry.Base, oldCurrent, entry.Current,
                transaction.Handle, sourceId));
        }

        private void PublishChanges(
            Dictionary<AttributeKey, GameplayAttributeValue> before,
            AttributeTransactionHandle transactionHandle,
            ulong sourceSpecHandle)
        {
            var changes = new List<AttributeChangedEvent>();
            foreach (KeyValuePair<AttributeKey, Entry> pair in _entries)
            {
                GameplayAttributeValue old = before[pair.Key];
                if (Math.Abs(old.BaseValue - pair.Value.Base) < Epsilon
                    && Math.Abs(old.CurrentValue - pair.Value.Current) < Epsilon)
                    continue;
                changes.Add(new AttributeChangedEvent(
                    pair.Value.AttributeId,
                    old.BaseValue,
                    pair.Value.Base,
                    old.CurrentValue, pair.Value.Current,
                    transactionHandle, sourceSpecHandle));
            }

            if (changes.Count == 0) return;
            _isPublishingChanges = true;
            try
            {
                for (int i = 0; i < changes.Count; i++)
                    AttributeChanged?.Invoke(changes[i]);
            }
            finally
            {
                _isPublishingChanges = false;
            }

            DrainDeferredMutations();
        }

        private void Publish(AttributeChangedEvent change)
        {
            _isPublishingChanges = true;
            try
            {
                AttributeChanged?.Invoke(change);
            }
            finally
            {
                _isPublishingChanges = false;
            }

            DrainDeferredMutations();
        }

        private void DrainDeferredMutations()
        {
            while (!_isPublishingChanges && _deferredMutations.Count > 0)
                _deferredMutations.Dequeue().Invoke();
        }

        private bool TryGetKey(
            AttributeId id,
            out AttributeKey key)
        {
            if (!id.IsValid)
            {
                key = default;
                return false;
            }
            if (_resolver == null)
            {
                key = new AttributeKey(default, id);
                return true;
            }
            if (_resolver.TryResolve(id.Value, out AttributeHandle handle))
            {
                key = new AttributeKey(handle, default);
                return true;
            }
            key = default;
            return false;
        }
    }
}
