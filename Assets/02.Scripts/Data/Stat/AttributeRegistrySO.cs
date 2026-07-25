using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    [Serializable]
    public sealed class AttributeRegistryEntry
    {
        public string attributeId = string.Empty;
        public string stableId = string.Empty;
        public List<string> aliases = new();
        public string displayName = string.Empty;
        public string category = string.Empty;
        public float defaultBaseValue;
        public AttributeValueFormat format;
        public string unit = string.Empty;
        public AttributeClampPolicy clampPolicy;
        public float fixedMinimum;
        public float fixedMaximum = float.MaxValue;
        public string minimumAttributeId = string.Empty;
        public string maximumAttributeId = string.Empty;
        public string dependentResourceId = string.Empty;
        public AttributeMaxChangePolicy maxChangePolicy =
            AttributeMaxChangePolicy.Clamp;
        public bool saveBaseValue;
        public bool isMetaAttribute;

        public bool IsValid() =>
            !string.IsNullOrWhiteSpace(attributeId)
            && !string.IsNullOrWhiteSpace(stableId);

        public AttributeMetadata ToMetadata() =>
            new(
                attributeId,
                displayName,
                format,
                unit,
                defaultBaseValue,
                clampPolicy,
                fixedMinimum,
                fixedMaximum,
                minimumAttributeId,
                maximumAttributeId,
                dependentResourceId,
                maxChangePolicy,
                saveBaseValue,
                isMetaAttribute);

        public GameplayAttributeDefinition ToRuntimeDefinition() =>
            new(
                new AttributeId(attributeId),
                defaultBaseValue,
                clampPolicy,
                fixedMinimum,
                fixedMaximum,
                new AttributeId(minimumAttributeId),
                new AttributeId(maximumAttributeId),
                new AttributeId(dependentResourceId),
                maxChangePolicy,
                saveBaseValue,
                isMetaAttribute);
    }

    [CreateAssetMenu(
        fileName = "AttributeRegistry",
        menuName = "UPlayGround/Ability/Attribute Registry")]
    public sealed class AttributeRegistrySO : ScriptableObject
    {
        public List<AttributeRegistryEntry> attributes = new();

        [NonSerialized] private AttributeInternTable _internTable;

        public AttributeInternTable InternTable
        {
            get
            {
                if (_internTable == null)
                    _internTable = new AttributeInternTable(attributes);
                return _internTable;
            }
        }

        public bool TryResolve(
            string attributeIdOrAlias,
            out AttributeRegistryEntry entry) =>
            InternTable.TryGetEntry(attributeIdOrAlias, out entry);

        public void RebuildLookup()
        {
            _internTable = new AttributeInternTable(attributes);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
            AttributeRegistry.SetEditorRegistry(this);
        }
#endif
    }

    /// <summary>
    /// 코드 생성 없이 Resources/AttributeRegistry.asset을 조회하는 진입점.
    /// </summary>
    public static class AttributeRegistry
    {
        private const string ResourcePath = "AttributeRegistry";
        private static AttributeRegistrySO s_Registry;

        public static AttributeRegistrySO Registry
        {
            get
            {
                if (s_Registry == null)
                    s_Registry = Resources.Load<AttributeRegistrySO>(
                        ResourcePath);
                return s_Registry != null
                    ? s_Registry
                    : throw new InvalidOperationException(
                        $"Resources/{ResourcePath}.asset을 찾지 못했습니다.");
            }
        }

        public static IReadOnlyList<AttributeRegistryEntry> Definitions =>
            Registry.attributes;

        public static IAttributeResolver Resolver =>
            AttributeRegistryResolver.Instance;

        public static bool IsRegistered(string attributeId) =>
            Registry.TryResolve(attributeId, out _);

        public static bool TryResolve(
            string attributeIdOrAlias,
            out AttributeReference reference)
        {
            if (Registry.TryResolve(
                    attributeIdOrAlias,
                    out AttributeRegistryEntry entry))
            {
                reference = AttributeReference.CreateResolved(
                    entry.attributeId);
                return true;
            }

            reference = default;
            return false;
        }

        public static AttributeReference GetRequired(string attributeId)
        {
            if (TryResolve(attributeId, out AttributeReference reference))
                return reference;
            throw new ArgumentException(
                $"AttributeRegistry에 등록되지 않은 ID입니다: '{attributeId}'",
                nameof(attributeId));
        }

        public static bool TryGetDefinition(
            AttributeId attributeId,
            out AttributeRegistryEntry entry) =>
            Registry.TryResolve(attributeId.Value, out entry);

        public static GameplayAttributeDefinition CreateRuntimeDefinition(
            AttributeId attributeId)
        {
            if (!TryGetDefinition(attributeId, out AttributeRegistryEntry entry))
                throw new ArgumentException(
                    $"AttributeRegistry에 등록되지 않은 ID입니다: '{attributeId}'",
                    nameof(attributeId));
            return entry.ToRuntimeDefinition();
        }

#if UNITY_EDITOR
        internal static void SetEditorRegistry(AttributeRegistrySO registry)
        {
            s_Registry = registry;
        }
#endif
    }

    public sealed class AttributeInternTable
    {
        private readonly List<AttributeRegistryEntry> _entries = new();
        private readonly Dictionary<string, int> _indices =
            new(StringComparer.Ordinal);
        private int[] _parents = Array.Empty<int>();

        public AttributeInternTable(
            IReadOnlyList<AttributeRegistryEntry> definitions)
        {
            _entries.Add(null);
            if (definitions == null) return;

            var sorted = new List<AttributeRegistryEntry>();
            for (int i = 0; i < definitions.Count; i++)
                if (definitions[i]?.IsValid() == true)
                    sorted.Add(definitions[i]);
            sorted.Sort((left, right) => string.Compare(
                left.stableId,
                right.stableId,
                StringComparison.Ordinal));

            for (int i = 0; i < sorted.Count; i++)
            {
                AttributeRegistryEntry entry = sorted[i];
                int index = _entries.Count;
                _entries.Add(entry);
                _indices.TryAdd(entry.attributeId, index);
                if (entry.aliases == null) continue;
                for (int j = 0; j < entry.aliases.Count; j++)
                {
                    string alias = entry.aliases[j]?.Trim();
                    if (!string.IsNullOrEmpty(alias))
                        _indices.TryAdd(alias, index);
                }
            }

            _parents = new int[_entries.Count];
            for (int i = 1; i < _entries.Count; i++)
            {
                string name = _entries[i].attributeId;
                int separator = name.LastIndexOf('.');
                if (separator > 0
                    && _indices.TryGetValue(
                        name.Substring(0, separator),
                        out int parent))
                    _parents[i] = parent;
            }
        }

        public bool TryResolve(
            string attributeIdOrAlias,
            out AttributeHandle handle)
        {
            string normalized = attributeIdOrAlias?.Trim() ?? string.Empty;
            if (_indices.TryGetValue(normalized, out int index))
            {
                handle = new AttributeHandle(index);
                return true;
            }

            handle = default;
            return false;
        }

        public bool TryGetEntry(
            string attributeIdOrAlias,
            out AttributeRegistryEntry entry)
        {
            if (TryResolve(attributeIdOrAlias, out AttributeHandle handle))
                return TryGetEntry(handle, out entry);
            entry = null;
            return false;
        }

        public bool TryGetEntry(
            AttributeHandle handle,
            out AttributeRegistryEntry entry)
        {
            if (handle.IsValid && handle.Index < _entries.Count)
            {
                entry = _entries[handle.Index];
                return entry != null;
            }

            entry = null;
            return false;
        }

        public AttributeHandle GetParent(AttributeHandle handle) =>
            handle.IsValid && handle.Index < _parents.Length
                ? new AttributeHandle(_parents[handle.Index])
                : default;
    }

    internal sealed class AttributeRegistryResolver : IAttributeResolver
    {
        public static readonly AttributeRegistryResolver Instance = new();

        public bool TryResolve(
            string attributeIdOrAlias,
            out AttributeHandle handle) =>
            AttributeRegistry.Registry.InternTable.TryResolve(
                attributeIdOrAlias,
                out handle);

        public bool TryGetMetadata(
            AttributeHandle handle,
            out AttributeMetadata metadata)
        {
            if (AttributeRegistry.Registry.InternTable.TryGetEntry(
                    handle,
                    out AttributeRegistryEntry entry))
            {
                metadata = entry.ToMetadata();
                return true;
            }

            metadata = default;
            return false;
        }
    }
}
