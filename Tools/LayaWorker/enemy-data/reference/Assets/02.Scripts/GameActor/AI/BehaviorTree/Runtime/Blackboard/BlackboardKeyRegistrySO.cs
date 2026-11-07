using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>직렬화에는 안정 ID와 표시용 캐시 이름만 보관한다.</summary>
    [Serializable]
    public struct BlackboardKeyReference : IEquatable<BlackboardKeyReference>
    {
        [SerializeField] private string _stableId;
        [SerializeField] private string _keyName;

        public string StableId => _stableId ?? string.Empty;
        public string KeyName => _keyName ?? string.Empty;
        public bool HasStableId => !string.IsNullOrWhiteSpace(_stableId);
        public bool HasKey => HasStableId || !string.IsNullOrWhiteSpace(_keyName);

        private BlackboardKeyReference(string stableId, string keyName)
        {
            _stableId = stableId?.Trim() ?? string.Empty;
            _keyName = keyName?.Trim() ?? string.Empty;
        }

        public static BlackboardKeyReference CreateRegistered(string keyName)
        {
            if (BlackboardKeyRegistry.TryResolve(keyName, out BlackboardKeyReference reference))
                return reference;

            throw new ArgumentException(
                $"BlackboardKeyRegistry에 등록되지 않은 Key입니다: '{keyName}'",
                nameof(keyName));
        }

        public static bool TryCreateRegistered(
            string stableIdOrNameOrAlias,
            out BlackboardKeyReference reference) =>
            BlackboardKeyRegistry.TryResolve(stableIdOrNameOrAlias, out reference);

        internal static BlackboardKeyReference CreateResolved(
            string stableId,
            string keyName) =>
            new(stableId, keyName);

        internal static BlackboardKeyReference CreateLegacy(string keyName) =>
            new(string.Empty, keyName);

        public bool TryResolve(
            out BlackboardKeyHandle handle,
            out BlackboardKeyDefinition definition) =>
            BlackboardKeyRegistry.TryResolve(this, out handle, out definition);

        public bool Equals(BlackboardKeyReference other)
        {
            if (HasStableId && other.HasStableId)
                return string.Equals(StableId, other.StableId, StringComparison.Ordinal);

            return string.Equals(KeyName, other.KeyName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) =>
            obj is BlackboardKeyReference other && Equals(other);

        public override int GetHashCode() =>
            HasStableId
                ? StringComparer.Ordinal.GetHashCode(StableId)
                : StringComparer.Ordinal.GetHashCode(KeyName);

        public override string ToString() =>
            string.IsNullOrWhiteSpace(KeyName) ? StableId : KeyName;

        public static bool operator ==(
            BlackboardKeyReference left,
            BlackboardKeyReference right) => left.Equals(right);

        public static bool operator !=(
            BlackboardKeyReference left,
            BlackboardKeyReference right) => !left.Equals(right);
    }

    public enum BlackboardKeyScope
    {
        TreeLocal,
        SubtreeInput,
        SubtreeOutput,
        AgentRuntime,
        SharedGroup,
        DebugOnly
    }

    public enum BlackboardWritePolicy
    {
        ReadWrite,
        RuntimeOnly,
        ReadOnly
    }

    [Serializable]
    public sealed class BlackboardKeyDefinition
    {
        [SerializeField] private string _stableId;
        [SerializeField] private string _keyName;
        [SerializeField] private List<string> _aliases = new();
        [SerializeField] private string _displayName;
        [TextArea, SerializeField] private string _description;
        [SerializeField] private BlackboardValueType _valueType;
        [SerializeField] private BlackboardKeyScope _scope = BlackboardKeyScope.AgentRuntime;
        [SerializeField] private BlackboardWritePolicy _writePolicy = BlackboardWritePolicy.ReadWrite;
        [SerializeField] private bool _required;

        public string StableId => _stableId ?? string.Empty;
        public string KeyName => _keyName ?? string.Empty;
        public IReadOnlyList<string> Aliases => _aliases;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? KeyName : _displayName;
        public string Description => _description ?? string.Empty;
        public BlackboardValueType ValueType => _valueType;
        public BlackboardKeyScope Scope => _scope;
        public BlackboardWritePolicy WritePolicy => _writePolicy;
        public bool Required => _required;

        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(_stableId)
            && !string.IsNullOrWhiteSpace(_keyName);

#if UNITY_EDITOR
        internal void SetEditorData(
            string stableId,
            string keyName,
            IEnumerable<string> aliases,
            string displayName,
            string description,
            BlackboardValueType valueType,
            BlackboardKeyScope scope,
            BlackboardWritePolicy writePolicy,
            bool required)
        {
            _stableId = stableId?.Trim() ?? string.Empty;
            _keyName = keyName?.Trim() ?? string.Empty;
            _aliases = aliases != null ? new List<string>(aliases) : new List<string>();
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;
            _valueType = valueType;
            _scope = scope;
            _writePolicy = writePolicy;
            _required = required;
        }
#endif
    }

    [CreateAssetMenu(
        fileName = "BlackboardKeyRegistry",
        menuName = "UPlayGround/AI/Blackboard Key Registry")]
    public sealed class BlackboardKeyRegistrySO : ScriptableObject
    {
        [SerializeField] private List<BlackboardKeyDefinition> _definitions = new();

        [NonSerialized] private BlackboardKeyInternTable _internTable;

        public IReadOnlyList<BlackboardKeyDefinition> Definitions => _definitions;
        public BlackboardKeyInternTable InternTable =>
            _internTable ??= new BlackboardKeyInternTable(_definitions);

        public void RebuildLookup()
        {
            _internTable = new BlackboardKeyInternTable(_definitions);
        }

        public bool TryResolve(
            string stableIdOrNameOrAlias,
            out BlackboardKeyReference reference)
        {
            if (InternTable.TryResolve(stableIdOrNameOrAlias, out BlackboardKeyHandle handle)
                && InternTable.TryGetDefinition(handle, out BlackboardKeyDefinition definition))
            {
                reference = BlackboardKeyReference.CreateResolved(
                    definition.StableId,
                    definition.KeyName);
                return true;
            }

            reference = default;
            return false;
        }

        public bool TryResolve(
            BlackboardKeyReference reference,
            out BlackboardKeyHandle handle,
            out BlackboardKeyDefinition definition)
        {
            if (reference.HasStableId
                && InternTable.TryResolveStableId(reference.StableId, out handle)
                && InternTable.TryGetDefinition(handle, out definition))
                return true;

            if (InternTable.TryResolve(reference.KeyName, out handle)
                && InternTable.TryGetDefinition(handle, out definition))
                return true;

            handle = default;
            definition = null;
            return false;
        }

#if UNITY_EDITOR
        internal List<BlackboardKeyDefinition> EditorDefinitions => _definitions;

        private void OnValidate()
        {
            RebuildLookup();
            BlackboardKeyRegistry.SetEditorRegistry(this);
        }
#endif
    }

    public readonly struct BlackboardKeyHandle : IEquatable<BlackboardKeyHandle>
    {
        public BlackboardKeyHandle(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }

        public int Index { get; }
        public int Generation { get; }
        public bool IsValid => Index > 0 && Generation > 0;

        public bool Equals(BlackboardKeyHandle other) =>
            Index == other.Index && Generation == other.Generation;
        public override bool Equals(object obj) => obj is BlackboardKeyHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, Generation);
        public static bool operator ==(BlackboardKeyHandle left, BlackboardKeyHandle right) => left.Equals(right);
        public static bool operator !=(BlackboardKeyHandle left, BlackboardKeyHandle right) => !left.Equals(right);
    }

    public sealed class BlackboardKeyInternTable
    {
        private static int s_NextGeneration;
        private readonly List<BlackboardKeyDefinition> _definitions = new();
        private readonly Dictionary<string, int> _stableIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _namesAndAliases = new(StringComparer.Ordinal);
        private readonly int _generation;

        public BlackboardKeyInternTable(IReadOnlyList<BlackboardKeyDefinition> definitions)
        {
            _generation = Interlocked.Increment(ref s_NextGeneration);
            _definitions.Add(null);
            if (definitions == null)
                return;

            var sorted = new List<BlackboardKeyDefinition>();
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i]?.IsValid() == true)
                    sorted.Add(definitions[i]);
            }

            sorted.Sort((left, right) =>
                string.Compare(left.StableId, right.StableId, StringComparison.Ordinal));

            foreach (BlackboardKeyDefinition definition in sorted)
            {
                var index = _definitions.Count;
                _definitions.Add(definition);
                _stableIds.TryAdd(definition.StableId, index);
                _namesAndAliases.TryAdd(definition.KeyName, index);

                foreach (string rawAlias in definition.Aliases)
                {
                    string alias = rawAlias?.Trim();
                    if (!string.IsNullOrEmpty(alias))
                        _namesAndAliases.TryAdd(alias, index);
                }
            }
        }

        public bool TryResolve(string stableIdOrNameOrAlias, out BlackboardKeyHandle handle)
        {
            string normalized = stableIdOrNameOrAlias?.Trim() ?? string.Empty;
            if (_stableIds.TryGetValue(normalized, out int stableIndex)
                || _namesAndAliases.TryGetValue(normalized, out stableIndex))
            {
                handle = new BlackboardKeyHandle(stableIndex, _generation);
                return true;
            }

            handle = default;
            return false;
        }

        public bool TryResolveStableId(string stableId, out BlackboardKeyHandle handle)
        {
            if (_stableIds.TryGetValue(stableId?.Trim() ?? string.Empty, out int index))
            {
                handle = new BlackboardKeyHandle(index, _generation);
                return true;
            }

            handle = default;
            return false;
        }

        public bool TryGetDefinition(
            BlackboardKeyHandle handle,
            out BlackboardKeyDefinition definition)
        {
            if (handle.IsValid
                && handle.Generation == _generation
                && handle.Index < _definitions.Count)
            {
                definition = _definitions[handle.Index];
                return definition != null;
            }

            definition = null;
            return false;
        }
    }

    public static class BlackboardKeyRegistry
    {
        private const string ResourcePath = "BlackboardKeyRegistry";
        private static BlackboardKeyRegistrySO s_Registry;
        private static bool s_LoadAttempted;

        public static bool TryGetRegistry(out BlackboardKeyRegistrySO registry)
        {
            if (!s_LoadAttempted)
            {
                s_LoadAttempted = true;
                s_Registry = Resources.Load<BlackboardKeyRegistrySO>(ResourcePath);
            }

            registry = s_Registry;
            return registry != null;
        }

        public static BlackboardKeyRegistrySO Registry =>
            TryGetRegistry(out BlackboardKeyRegistrySO registry)
                ? registry
                : throw new InvalidOperationException(
                    $"Resources/{ResourcePath}.asset을 찾지 못했습니다.");

        public static bool TryResolve(
            string stableIdOrNameOrAlias,
            out BlackboardKeyReference reference)
        {
            if (TryGetRegistry(out BlackboardKeyRegistrySO registry))
                return registry.TryResolve(stableIdOrNameOrAlias, out reference);

            reference = default;
            return false;
        }

        public static bool TryResolve(
            BlackboardKeyReference reference,
            out BlackboardKeyHandle handle,
            out BlackboardKeyDefinition definition)
        {
            if (TryGetRegistry(out BlackboardKeyRegistrySO registry))
                return registry.TryResolve(reference, out handle, out definition);

            handle = default;
            definition = null;
            return false;
        }

#if UNITY_EDITOR
        internal static void SetEditorRegistry(BlackboardKeyRegistrySO registry)
        {
            s_Registry = registry;
            s_LoadAttempted = registry != null;
        }
#endif
    }
}
