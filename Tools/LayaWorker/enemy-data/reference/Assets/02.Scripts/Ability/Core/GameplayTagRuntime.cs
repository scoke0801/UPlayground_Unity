using System;
using System.Collections.Generic;

namespace UPlayGround.Ability.Core
{
    public readonly struct AbilityTagId : IEquatable<AbilityTagId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public AbilityTagId(string value) => Value = value?.Trim() ?? string.Empty;
        public bool Equals(AbilityTagId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AbilityTagId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator AbilityTagId(string value) => new(value);

        public bool IsChildOf(AbilityTagId parent) =>
            parent.IsValid && IsValid
            && (Equals(parent) || Value.StartsWith(parent.Value + ".", StringComparison.Ordinal));
    }

    public readonly struct GameplayTagSourceHandle : IEquatable<GameplayTagSourceHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public GameplayTagSourceHandle(ulong value) => Value = value;
        public bool Equals(GameplayTagSourceHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayTagSourceHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public enum GameplayTagQueryMode
    {
        All,
        Any,
        None,
    }

    public sealed class GameplayTagQuery
    {
        public GameplayTagQueryMode Mode { get; }
        public bool MatchHierarchy { get; }
        public IReadOnlyList<AbilityTagId> Tags { get; }

        public GameplayTagQuery(
            GameplayTagQueryMode mode,
            IEnumerable<AbilityTagId> tags,
            bool matchHierarchy = true)
        {
            Mode = mode;
            MatchHierarchy = matchHierarchy;
            Tags = tags == null ? Array.Empty<AbilityTagId>() : new List<AbilityTagId>(tags);
        }
    }

    public sealed class GameplayTagAggregator
    {
        private readonly struct OwnedTag
        {
            public readonly AbilityTagId Tag;
            public readonly string SourceType;
            public readonly ulong SourceId;

            public OwnedTag(AbilityTagId tag, string sourceType, ulong sourceId)
            {
                Tag = tag;
                SourceType = sourceType ?? string.Empty;
                SourceId = sourceId;
            }
        }

        private readonly Dictionary<ulong, OwnedTag> _owned = new();
        private readonly Dictionary<AbilityTagId, int> _counts = new();
        private ulong _nextHandle = 1;

        public event Action<AbilityTagId> TagAdded;
        public event Action<AbilityTagId> TagRemoved;
        public int Count => _counts.Count;

        public GameplayTagSourceHandle Add(AbilityTagId tag, string sourceType, ulong sourceId)
        {
            if (!tag.IsValid) return default;
            ulong value = _nextHandle++;
            if (value == 0) value = _nextHandle++;
            bool existed = _counts.TryGetValue(tag, out int count);
            _counts[tag] = count + 1;
            _owned.Add(value, new OwnedTag(tag, sourceType, sourceId));
            if (!existed) TagAdded?.Invoke(tag);
            return new GameplayTagSourceHandle(value);
        }

        public bool Remove(GameplayTagSourceHandle handle)
        {
            if (!handle.IsValid || !_owned.Remove(handle.Value, out OwnedTag owned))
                return false;
            int count = _counts[owned.Tag] - 1;
            if (count <= 0)
            {
                _counts.Remove(owned.Tag);
                TagRemoved?.Invoke(owned.Tag);
            }
            else
            {
                _counts[owned.Tag] = count;
            }
            return true;
        }

        public int RemoveBySource(string sourceType, ulong sourceId)
        {
            var handles = new List<GameplayTagSourceHandle>();
            foreach (KeyValuePair<ulong, OwnedTag> pair in _owned)
            {
                if (pair.Value.SourceId == sourceId
                    && string.Equals(pair.Value.SourceType, sourceType ?? string.Empty, StringComparison.Ordinal))
                    handles.Add(new GameplayTagSourceHandle(pair.Key));
            }
            for (int i = 0; i < handles.Count; i++) Remove(handles[i]);
            return handles.Count;
        }

        public bool HasExact(AbilityTagId tag) => tag.IsValid && _counts.ContainsKey(tag);

        public bool Has(AbilityTagId tag, bool matchHierarchy = true)
        {
            if (!tag.IsValid) return false;
            if (!matchHierarchy) return HasExact(tag);
            foreach (AbilityTagId owned in _counts.Keys)
                if (owned.IsChildOf(tag)) return true;
            return false;
        }

        public bool Matches(GameplayTagQuery query)
        {
            if (query == null) return true;
            switch (query.Mode)
            {
                case GameplayTagQueryMode.All:
                    for (int i = 0; i < query.Tags.Count; i++)
                        if (!Has(query.Tags[i], query.MatchHierarchy)) return false;
                    return true;
                case GameplayTagQueryMode.Any:
                    for (int i = 0; i < query.Tags.Count; i++)
                        if (Has(query.Tags[i], query.MatchHierarchy)) return true;
                    return false;
                case GameplayTagQueryMode.None:
                    for (int i = 0; i < query.Tags.Count; i++)
                        if (Has(query.Tags[i], query.MatchHierarchy)) return false;
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void CopyTags(ICollection<AbilityTagId> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            foreach (AbilityTagId tag in _counts.Keys) destination.Add(tag);
        }

        public void Clear()
        {
            var handles = new List<GameplayTagSourceHandle>(_owned.Count);
            foreach (ulong handle in _owned.Keys) handles.Add(new GameplayTagSourceHandle(handle));
            for (int i = 0; i < handles.Count; i++) Remove(handles[i]);
        }
    }
}
