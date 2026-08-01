using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    [Serializable]
    public class Blackboard
    {
        [SerializeField] private List<BlackboardEntry> _entries = new();

        [NonSerialized] private Dictionary<string, BlackboardEntry> _entryLookup;
        [NonSerialized] private Dictionary<string, BlackboardEntry> _stableIdLookup;
        [NonSerialized] private Dictionary<string, float> _runtimeFloatValues;
        [NonSerialized] private int _lookupEntryCount;

        public IReadOnlyList<BlackboardEntry> Entries => _entries;

        public Blackboard Clone()
        {
            var clone = new Blackboard();
            clone._entries.Clear();
            foreach (var entry in _entries)
                clone._entries.Add(entry.Clone());
            return clone;
        }

        public bool Contains(string key) => FindEntry(key) != null;
        public bool Contains(BlackboardKeyReference reference) => FindEntry(reference) != null;

        public void AddEntry(string key, BlackboardValueType valueType)
        {
            if (!BlackboardKeyRegistry.TryResolve(key, out BlackboardKeyReference reference))
            {
                Debug.LogError($"등록되지 않은 Blackboard Key는 추가할 수 없습니다: '{key}'");
                return;
            }

            AddEntry(reference, valueType);
        }

        public void AddEntry(
            BlackboardKeyReference reference,
            BlackboardValueType valueType)
        {
            if (!TryValidateReference(reference, valueType, out BlackboardKeyReference resolved)
                || Contains(resolved))
                return;

            var entry = new BlackboardEntry
            {
                ValueType = valueType
            };
            entry.SetKeyReference(resolved);
            _entries.Add(entry);
            InvalidateLookup();
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return;

            _entries.RemoveAt(index);
            InvalidateLookup();
        }

        public BlackboardEntry FindEntry(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            EnsureLookup();
            return _entryLookup.TryGetValue(key, out var entry) ? entry : null;
        }

        public BlackboardEntry FindEntry(BlackboardKeyReference reference)
        {
            if (!reference.HasKey)
                return null;

            EnsureLookup();
            if (reference.HasStableId
                && _stableIdLookup.TryGetValue(reference.StableId, out BlackboardEntry stableEntry))
                return stableEntry;

            if (_entryLookup.TryGetValue(
                    reference.KeyName,
                    out BlackboardEntry namedEntry))
                return namedEntry;

            if (BlackboardKeyRegistry.TryResolve(
                    reference.KeyName,
                    out BlackboardKeyReference resolved)
                && _stableIdLookup.TryGetValue(
                    resolved.StableId,
                    out BlackboardEntry resolvedEntry))
                return resolvedEntry;

            return null;
        }

        public BlackboardEntry FindEntry(BlackboardKeyHandle handle)
        {
            if (!BlackboardKeyRegistry.TryGetRegistry(out BlackboardKeyRegistrySO registry)
                || !registry.InternTable.TryGetDefinition(handle, out BlackboardKeyDefinition definition))
                return null;

            return FindEntry(BlackboardKeyReference.CreateResolved(
                definition.StableId,
                definition.KeyName));
        }

        public bool TryGetBool(string key, out bool value)
        {
            var entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.Bool)
            {
                value = default;
                return false;
            }

            value = entry.BoolValue;
            return true;
        }

        public bool TryGetInt(string key, out int value)
        {
            var entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.Int)
            {
                value = default;
                return false;
            }

            value = entry.IntValue;
            return true;
        }

        public bool TryGetFloat(string key, out float value)
        {
            var entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.Float)
            {
                value = default;
                return false;
            }

            value = entry.FloatValue;
            return true;
        }

        /// <summary>
        /// Registry에 등록할 수 없는 런타임 조합 Key의 Float 값을 읽는다.
        /// 이 값은 BT 에셋에 직렬화되지 않으며 Clone으로 복사되지 않는다.
        /// </summary>
        public bool TryGetRuntimeFloat(string key, out float value)
        {
            if (_runtimeFloatValues != null
                && !string.IsNullOrWhiteSpace(key)
                && _runtimeFloatValues.TryGetValue(key, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Registry에 등록할 수 없는 런타임 조합 Key의 Float 값을 기록한다.
        /// 정적 Blackboard Key는 SetFloat를 사용해야 한다.
        /// </summary>
        public void SetRuntimeFloat(string key, float value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _runtimeFloatValues ??= new Dictionary<string, float>(StringComparer.Ordinal);
            _runtimeFloatValues[key] = value;
        }

        public bool TryGetString(string key, out string value)
        {
            var entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.String)
            {
                value = default;
                return false;
            }

            value = entry.StringValue;
            return true;
        }

        public bool TryGetVector3(string key, out Vector3 value)
        {
            var entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.Vector3)
            {
                value = default;
                return false;
            }

            value = entry.Vector3Value;
            return true;
        }

        public bool TryGetObject<T>(string key, out T value) where T : UnityEngine.Object
        {
            var entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.Object)
            {
                value = default;
                return false;
            }

            value = entry.ObjectValue as T;
            return value != null;
        }

        public bool TryGetBool(BlackboardKeyReference key, out bool value) =>
            TryGetValue(key, BlackboardValueType.Bool, entry => entry.BoolValue, out value);
        public bool TryGetInt(BlackboardKeyReference key, out int value) =>
            TryGetValue(key, BlackboardValueType.Int, entry => entry.IntValue, out value);
        public bool TryGetFloat(BlackboardKeyReference key, out float value) =>
            TryGetValue(key, BlackboardValueType.Float, entry => entry.FloatValue, out value);
        public bool TryGetString(BlackboardKeyReference key, out string value) =>
            TryGetValue(key, BlackboardValueType.String, entry => entry.StringValue, out value);
        public bool TryGetVector3(BlackboardKeyReference key, out Vector3 value) =>
            TryGetValue(key, BlackboardValueType.Vector3, entry => entry.Vector3Value, out value);

        public bool TryGetObject<T>(BlackboardKeyReference key, out T value)
            where T : UnityEngine.Object
        {
            BlackboardEntry entry = FindEntry(key);
            if (entry == null || entry.ValueType != BlackboardValueType.Object)
            {
                value = default;
                return false;
            }

            value = entry.ObjectValue as T;
            return value != null;
        }

        public bool TryGetBool(BlackboardKeyHandle key, out bool value) =>
            TryGetByHandle(key, BlackboardValueType.Bool, entry => entry.BoolValue, out value);
        public bool TryGetInt(BlackboardKeyHandle key, out int value) =>
            TryGetByHandle(key, BlackboardValueType.Int, entry => entry.IntValue, out value);
        public bool TryGetFloat(BlackboardKeyHandle key, out float value) =>
            TryGetByHandle(key, BlackboardValueType.Float, entry => entry.FloatValue, out value);
        public bool TryGetString(BlackboardKeyHandle key, out string value) =>
            TryGetByHandle(key, BlackboardValueType.String, entry => entry.StringValue, out value);
        public bool TryGetVector3(BlackboardKeyHandle key, out Vector3 value) =>
            TryGetByHandle(key, BlackboardValueType.Vector3, entry => entry.Vector3Value, out value);

        public void SetBool(string key, bool value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Bool);
            if (entry != null)
                entry.BoolValue = value;
        }

        public void SetInt(string key, int value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Int);
            if (entry != null)
                entry.IntValue = value;
        }

        public void SetFloat(string key, float value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Float);
            if (entry != null)
                entry.FloatValue = value;
        }

        public void SetString(string key, string value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.String);
            if (entry != null)
                entry.StringValue = value;
        }

        public void SetVector3(string key, Vector3 value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Vector3);
            if (entry != null)
                entry.Vector3Value = value;
        }

        public void SetObject(string key, UnityEngine.Object value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Object);
            if (entry != null)
                entry.ObjectValue = value;
        }

        public bool TrySetBool(BlackboardKeyReference key, bool value) =>
            TrySetValue(key, BlackboardValueType.Bool, entry => entry.BoolValue = value);
        public bool TrySetInt(BlackboardKeyReference key, int value) =>
            TrySetValue(key, BlackboardValueType.Int, entry => entry.IntValue = value);
        public bool TrySetFloat(BlackboardKeyReference key, float value) =>
            TrySetValue(key, BlackboardValueType.Float, entry => entry.FloatValue = value);
        public bool TrySetString(BlackboardKeyReference key, string value) =>
            TrySetValue(key, BlackboardValueType.String, entry => entry.StringValue = value);
        public bool TrySetVector3(BlackboardKeyReference key, Vector3 value) =>
            TrySetValue(key, BlackboardValueType.Vector3, entry => entry.Vector3Value = value);
        public bool TrySetObject(BlackboardKeyReference key, UnityEngine.Object value) =>
            TrySetValue(key, BlackboardValueType.Object, entry => entry.ObjectValue = value);

        public bool TrySetBool(BlackboardKeyHandle key, bool value) =>
            TrySetByHandle(key, BlackboardValueType.Bool, entry => entry.BoolValue = value);
        public bool TrySetInt(BlackboardKeyHandle key, int value) =>
            TrySetByHandle(key, BlackboardValueType.Int, entry => entry.IntValue = value);
        public bool TrySetFloat(BlackboardKeyHandle key, float value) =>
            TrySetByHandle(key, BlackboardValueType.Float, entry => entry.FloatValue = value);
        public bool TrySetString(BlackboardKeyHandle key, string value) =>
            TrySetByHandle(key, BlackboardValueType.String, entry => entry.StringValue = value);
        public bool TrySetVector3(BlackboardKeyHandle key, Vector3 value) =>
            TrySetByHandle(key, BlackboardValueType.Vector3, entry => entry.Vector3Value = value);
        public bool TrySetObject(BlackboardKeyHandle key, UnityEngine.Object value) =>
            TrySetByHandle(key, BlackboardValueType.Object, entry => entry.ObjectValue = value);

        public bool TryGetBool(BlackboardKeySelector selector, out bool value) => TryGetBool(selector.Reference, out value);
        public bool TryGetInt(BlackboardKeySelector selector, out int value) => TryGetInt(selector.Reference, out value);
        public bool TryGetFloat(BlackboardKeySelector selector, out float value) => TryGetFloat(selector.Reference, out value);
        public bool TryGetString(BlackboardKeySelector selector, out string value) => TryGetString(selector.Reference, out value);
        public bool TryGetVector3(BlackboardKeySelector selector, out Vector3 value) => TryGetVector3(selector.Reference, out value);
        public bool TryGetObject<T>(BlackboardKeySelector selector, out T value) where T : UnityEngine.Object => TryGetObject(selector.Reference, out value);

        public void SetBool(BlackboardKeySelector selector, bool value) => TrySetBool(selector.Reference, value);
        public void SetInt(BlackboardKeySelector selector, int value) => TrySetInt(selector.Reference, value);
        public void SetFloat(BlackboardKeySelector selector, float value) => TrySetFloat(selector.Reference, value);
        public void SetString(BlackboardKeySelector selector, string value) => TrySetString(selector.Reference, value);
        public void SetVector3(BlackboardKeySelector selector, Vector3 value) => TrySetVector3(selector.Reference, value);
        public void SetObject(BlackboardKeySelector selector, UnityEngine.Object value) => TrySetObject(selector.Reference, value);

        private BlackboardEntry GetOrCreate(string key, BlackboardValueType valueType)
        {
            if (!BlackboardKeyRegistry.TryResolve(key, out BlackboardKeyReference reference)
                || !reference.TryResolve(out _, out BlackboardKeyDefinition definition)
                || definition.ValueType != valueType
                || definition.WritePolicy == BlackboardWritePolicy.ReadOnly)
            {
                Debug.LogError(
                    $"등록되지 않았거나 타입/쓰기 정책이 맞지 않는 Blackboard Key입니다: "
                    + $"'{key}' ({valueType})");
                return null;
            }

            var entry = FindEntry(key);
            if (entry != null)
            {
                return entry.ValueType == valueType ? entry : null;
            }

            if (!TryValidateReference(
                    reference,
                    valueType,
                    out BlackboardKeyReference resolved))
            {
                Debug.LogError($"등록되지 않았거나 타입이 다른 Blackboard Key는 자동 생성할 수 없습니다: '{key}' ({valueType})");
                return null;
            }

            entry = new BlackboardEntry
            {
                ValueType = valueType
            };
            entry.SetKeyReference(resolved);
            _entries.Add(entry);
            _entryLookup ??= new Dictionary<string, BlackboardEntry>(_entries.Count);
            _stableIdLookup ??= new Dictionary<string, BlackboardEntry>(_entries.Count);
            _entryLookup[resolved.KeyName] = entry;
            _stableIdLookup[resolved.StableId] = entry;
            _lookupEntryCount = _entries.Count;
            return entry;
        }

        private bool TrySetValue(
            BlackboardKeyReference reference,
            BlackboardValueType valueType,
            Action<BlackboardEntry> setter)
        {
            BlackboardEntry entry = FindEntry(reference);
            if (entry == null || entry.ValueType != valueType)
                return false;

            if (reference.TryResolve(out _, out BlackboardKeyDefinition definition)
                && definition.WritePolicy == BlackboardWritePolicy.ReadOnly)
                return false;

            setter(entry);
            return true;
        }

        private bool TrySetByHandle(
            BlackboardKeyHandle handle,
            BlackboardValueType valueType,
            Action<BlackboardEntry> setter)
        {
            if (!BlackboardKeyRegistry.TryGetRegistry(out BlackboardKeyRegistrySO registry)
                || !registry.InternTable.TryGetDefinition(
                    handle,
                    out BlackboardKeyDefinition definition)
                || definition.WritePolicy == BlackboardWritePolicy.ReadOnly)
                return false;

            BlackboardEntry entry = FindEntry(handle);
            if (entry == null || entry.ValueType != valueType)
                return false;

            setter(entry);
            return true;
        }

        private bool TryGetValue<T>(
            BlackboardKeyReference reference,
            BlackboardValueType valueType,
            Func<BlackboardEntry, T> getter,
            out T value)
        {
            BlackboardEntry entry = FindEntry(reference);
            if (entry == null || entry.ValueType != valueType)
            {
                value = default;
                return false;
            }

            value = getter(entry);
            return true;
        }

        private bool TryGetByHandle<T>(
            BlackboardKeyHandle handle,
            BlackboardValueType valueType,
            Func<BlackboardEntry, T> getter,
            out T value)
        {
            BlackboardEntry entry = FindEntry(handle);
            if (entry == null || entry.ValueType != valueType)
            {
                value = default;
                return false;
            }

            value = getter(entry);
            return true;
        }

        private static bool TryValidateReference(
            BlackboardKeyReference reference,
            BlackboardValueType valueType,
            out BlackboardKeyReference resolved)
        {
            if (reference.TryResolve(out _, out BlackboardKeyDefinition definition)
                && definition.ValueType == valueType)
            {
                resolved = BlackboardKeyReference.CreateResolved(
                    definition.StableId,
                    definition.KeyName);
                return true;
            }

            resolved = default;
            return false;
        }

        private void EnsureLookup()
        {
            if (_entryLookup != null && _lookupEntryCount == _entries.Count)
                return;

            _entryLookup = new Dictionary<string, BlackboardEntry>(_entries.Count);
            _stableIdLookup = new Dictionary<string, BlackboardEntry>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || _entryLookup.ContainsKey(entry.Key))
                    continue;

                _entryLookup.Add(entry.Key, entry);
                if (!string.IsNullOrWhiteSpace(entry.StableId)
                    && !_stableIdLookup.ContainsKey(entry.StableId))
                    _stableIdLookup.Add(entry.StableId, entry);
            }

            _lookupEntryCount = _entries.Count;
        }

        private void InvalidateLookup()
        {
            _entryLookup = null;
            _stableIdLookup = null;
            _lookupEntryCount = 0;
        }
    }
}
