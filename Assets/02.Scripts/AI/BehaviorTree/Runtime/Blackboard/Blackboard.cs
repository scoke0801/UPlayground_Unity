using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    [Serializable]
    public class Blackboard
    {
        [SerializeField] private List<BlackboardEntry> _entries = new();

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

        public void AddEntry(string key, BlackboardValueType valueType)
        {
            if (string.IsNullOrWhiteSpace(key) || Contains(key))
                return;

            _entries.Add(new BlackboardEntry
            {
                Key = key,
                ValueType = valueType
            });
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return;

            _entries.RemoveAt(index);
        }

        public BlackboardEntry FindEntry(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            return _entries.Find(entry => entry.Key == key);
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

        public void SetBool(string key, bool value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Bool);
            entry.BoolValue = value;
        }

        public void SetInt(string key, int value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Int);
            entry.IntValue = value;
        }

        public void SetFloat(string key, float value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Float);
            entry.FloatValue = value;
        }

        public void SetString(string key, string value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.String);
            entry.StringValue = value;
        }

        public void SetVector3(string key, Vector3 value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Vector3);
            entry.Vector3Value = value;
        }

        public void SetObject(string key, UnityEngine.Object value)
        {
            var entry = GetOrCreate(key, BlackboardValueType.Object);
            entry.ObjectValue = value;
        }

        public bool TryGetBool(BlackboardKeySelector selector, out bool value) => TryGetBool(selector.Key, out value);
        public bool TryGetInt(BlackboardKeySelector selector, out int value) => TryGetInt(selector.Key, out value);
        public bool TryGetFloat(BlackboardKeySelector selector, out float value) => TryGetFloat(selector.Key, out value);
        public bool TryGetString(BlackboardKeySelector selector, out string value) => TryGetString(selector.Key, out value);
        public bool TryGetVector3(BlackboardKeySelector selector, out Vector3 value) => TryGetVector3(selector.Key, out value);
        public bool TryGetObject<T>(BlackboardKeySelector selector, out T value) where T : UnityEngine.Object => TryGetObject(selector.Key, out value);

        public void SetBool(BlackboardKeySelector selector, bool value) => SetBool(selector.Key, value);
        public void SetInt(BlackboardKeySelector selector, int value) => SetInt(selector.Key, value);
        public void SetFloat(BlackboardKeySelector selector, float value) => SetFloat(selector.Key, value);
        public void SetString(BlackboardKeySelector selector, string value) => SetString(selector.Key, value);
        public void SetVector3(BlackboardKeySelector selector, Vector3 value) => SetVector3(selector.Key, value);
        public void SetObject(BlackboardKeySelector selector, UnityEngine.Object value) => SetObject(selector.Key, value);

        private BlackboardEntry GetOrCreate(string key, BlackboardValueType valueType)
        {
            var entry = FindEntry(key);
            if (entry != null)
            {
                entry.ValueType = valueType;
                return entry;
            }

            entry = new BlackboardEntry
            {
                Key = key,
                ValueType = valueType
            };
            _entries.Add(entry);
            return entry;
        }
    }
}
