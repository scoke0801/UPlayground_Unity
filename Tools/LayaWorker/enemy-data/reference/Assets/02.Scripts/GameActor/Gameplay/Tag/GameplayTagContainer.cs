using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// AbilitySystemComponent가 소유하는 프로젝트 태그 API.
    /// 상태 머신 상태가 OnEnter / OnExit 에서 태그를 추가 / 제거한다.
    /// </summary>
    public sealed class GameplayTagContainer : IGameplayTagReader
    {
        private readonly HashSet<GameplayTag> _tags = new();
        private readonly Dictionary<GameplayTag, int> _explicitTagCounts = new();
        private readonly Dictionary<GameplayTag, int> _ownedTagCounts = new();
        private readonly Dictionary<ulong, OwnedTag> _ownedTags = new();
        private readonly Dictionary<GameplayTag, GameplayTagSourceHandle> _gasExplicit = new();
        private readonly Dictionary<ulong, GameplayTagSourceHandle> _gasOwned = new();
        private ulong _nextHandleId = 1;
        private AbilitySystemComponent _abilitySystem;

        private readonly struct OwnedTag
        {
            public readonly GameplayTag Tag;
            public readonly GameplayTagSource Source;

            public OwnedTag(GameplayTag tag, GameplayTagSource source)
            {
                Tag = tag;
                Source = source;
            }
        }

        public event Action<GameplayTag> OnTagAdded;
        public event Action<GameplayTag> OnTagRemoved;

        internal GameplayTagContainer(AbilitySystemComponent abilitySystem)
        {
            _abilitySystem = abilitySystem
                ?? throw new ArgumentNullException(nameof(abilitySystem));
            _abilitySystem.EnsureInitialized();
        }

        private AbilitySystemComponent EnsureAbilitySystem()
        {
            _abilitySystem?.EnsureInitialized();
            return _abilitySystem;
        }

        // ── 추가 / 제거 ────────────────────────────────────────────────

        public void AddTag(GameplayTag tag)
        {
            EnsureRegisteredOrEmpty(tag, nameof(tag));
            if (!tag.IsValid()) return;
            bool wasPresent = HasTag(tag);
            _explicitTagCounts.TryGetValue(tag, out int count);
            _explicitTagCounts[tag] = count + 1;
            _tags.Add(tag);
            AbilitySystemComponent gas = EnsureAbilitySystem();
            if (!_gasExplicit.ContainsKey(tag) && gas?.Tags != null)
            {
                GameplayTagSourceHandle handle = gas.Tags.Add(
                    new AbilityTagId(tag.TagName), "ExplicitTag", 0);
                if (handle.IsValid) _gasExplicit[tag] = handle;
            }
            if (!wasPresent) OnTagAdded?.Invoke(tag);
        }

        public void RemoveTag(GameplayTag tag)
        {
            EnsureRegisteredOrEmpty(tag, nameof(tag));
            if (!_explicitTagCounts.TryGetValue(tag, out int count)) return;
            if (count > 1)
            {
                _explicitTagCounts[tag] = count - 1;
                return;
            }
            _explicitTagCounts.Remove(tag);
            _tags.Remove(tag);
            if (_gasExplicit.Remove(tag, out GameplayTagSourceHandle gasHandle))
                _abilitySystem?.Tags?.Remove(gasHandle);
            if (!HasTag(tag)) OnTagRemoved?.Invoke(tag);
        }

        /// <summary>
        /// Ability/Effect가 소유하는 태그를 추가한다. 반환 핸들만 제거할 수 있어
        /// 동일 태그를 부여한 다른 소스의 소유권을 보존한다.
        /// </summary>
        public GameplayTagHandle AddTag(GameplayTag tag, GameplayTagSource source)
        {
            EnsureRegisteredOrEmpty(tag, nameof(tag));
            if (!tag.IsValid()) return default;

            bool wasPresent = HasTag(tag);
            ulong handleId = _nextHandleId++;
            if (handleId == 0) handleId = _nextHandleId++;

            _ownedTags[handleId] = new OwnedTag(tag, source);
            AbilitySystemComponent gas = EnsureAbilitySystem();
            if (gas?.Tags != null)
            {
                GameplayTagSourceHandle gasHandle = gas.Tags.Add(
                    new AbilityTagId(tag.TagName), source.Type, source.InstanceId);
                if (gasHandle.IsValid) _gasOwned[handleId] = gasHandle;
            }
            _ownedTagCounts.TryGetValue(tag, out int count);
            _ownedTagCounts[tag] = count + 1;
            if (!wasPresent) OnTagAdded?.Invoke(tag);
            return new GameplayTagHandle(handleId);
        }

        public bool RemoveTag(GameplayTagHandle handle)
        {
            if (!handle.IsValid || !_ownedTags.Remove(handle.Value, out OwnedTag owned))
                return false;

            if (_gasOwned.Remove(handle.Value, out GameplayTagSourceHandle gasHandle))
                _abilitySystem?.Tags?.Remove(gasHandle);

            int count = _ownedTagCounts[owned.Tag] - 1;
            if (count <= 0)
                _ownedTagCounts.Remove(owned.Tag);
            else
                _ownedTagCounts[owned.Tag] = count;

            if (!HasTag(owned.Tag)) OnTagRemoved?.Invoke(owned.Tag);
            return true;
        }

        public int RemoveTagsBySource(GameplayTagSource source)
        {
            var handles = new List<GameplayTagHandle>();
            foreach (KeyValuePair<ulong, OwnedTag> pair in _ownedTags)
                if (pair.Value.Source.Equals(source))
                    handles.Add(new GameplayTagHandle(pair.Key));

            for (int i = 0; i < handles.Count; i++)
                RemoveTag(handles[i]);
            return handles.Count;
        }

        /// <summary>parent의 자식 태그 전부를 한 번에 제거한다.</summary>
        public void RemoveTagsWithParent(GameplayTag parent)
        {
            EnsureRegisteredOrEmpty(parent, nameof(parent));
            if (!parent.IsValid()) return;
            var toRemove = new List<GameplayTag>();
            foreach (var t in _tags)
                if (t.IsChildOf(parent)) toRemove.Add(t);
            foreach (var t in toRemove)
                while (_explicitTagCounts.ContainsKey(t))
                    RemoveTag(t);
        }

        // ── 쿼리 ───────────────────────────────────────────────────────

        /// <summary>정확히 일치하는 태그 보유 여부</summary>
        public bool HasTag(GameplayTag tag)
        {
            EnsureRegisteredOrEmpty(tag, nameof(tag));
            return _tags.Contains(tag)
                   || _ownedTagCounts.ContainsKey(tag)
                   || (tag.IsValid()
                       && EnsureAbilitySystem()?.Tags?.Has(
                           new AbilityTagId(tag.TagName), false) == true);
        }

        public bool HasTag(GameplayTag tag, bool matchHierarchy) =>
            matchHierarchy ? HasTagInHierarchy(tag) : HasTag(tag);

        /// <summary>parent 계층 아래 임의의 태그를 보유하는지 확인</summary>
        public bool HasTagInHierarchy(GameplayTag parent)
        {
            EnsureRegisteredOrEmpty(parent, nameof(parent));
            if (!parent.IsValid()) return false;
            foreach (var t in AllTags)
                if (t.IsChildOf(parent)) return true;
            return false;
        }

        /// <summary>주어진 태그 중 하나라도 보유하면 true</summary>
        public bool HasAnyTag(IEnumerable<GameplayTag> tags)
        {
            foreach (var t in tags)
                if (HasTag(t)) return true;
            return false;
        }

        /// <summary>주어진 태그 전부를 보유해야 true</summary>
        public bool HasAllTags(IEnumerable<GameplayTag> tags)
        {
            foreach (var t in tags)
                if (!HasTag(t)) return false;
            return true;
        }

        public IReadOnlyCollection<GameplayTag> AllTags
        {
            get
            {
                var result = new HashSet<GameplayTag>(_tags);
                foreach (GameplayTag tag in _ownedTagCounts.Keys)
                    result.Add(tag);
                if (EnsureAbilitySystem()?.Tags != null)
                {
                    var gasTags = new List<AbilityTagId>();
                    _abilitySystem.Tags.CopyTags(gasTags);
                    for (int i = 0; i < gasTags.Count; i++)
                        result.Add(GameplayTagRegistry.GetRequired(gasTags[i].Value));
                }
                return result;
            }
        }

        private static void EnsureRegisteredOrEmpty(
            GameplayTag tag,
            string parameterName)
        {
            if (string.IsNullOrEmpty(tag.TagName) || tag.IsValid()) return;
            throw new ArgumentException(
                $"GameplayTagRegistry에 등록되지 않은 태그입니다: '{tag.TagName}'",
                parameterName);
        }

        public void Clear()
        {
            foreach (GameplayTagSourceHandle handle in _gasExplicit.Values)
                _abilitySystem?.Tags?.Remove(handle);
            _gasExplicit.Clear();
            _explicitTagCounts.Clear();
            _tags.Clear();
            var handles = new List<GameplayTagHandle>();
            foreach (ulong id in _ownedTags.Keys)
                handles.Add(new GameplayTagHandle(id));
            foreach (GameplayTagHandle handle in handles)
                RemoveTag(handle);
        }

        internal void Dispose() => Clear();

        public override string ToString() => string.Join(", ", _tags);
    }

    public readonly struct GameplayTagHandle : IEquatable<GameplayTagHandle>
    {
        internal readonly ulong Value;
        public bool IsValid => Value != 0;

        internal GameplayTagHandle(ulong value) => Value = value;
        public bool Equals(GameplayTagHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayTagHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct GameplayTagSource : IEquatable<GameplayTagSource>
    {
        public readonly string Type;
        public readonly ulong InstanceId;

        public GameplayTagSource(string type, ulong instanceId)
        {
            Type = type ?? string.Empty;
            InstanceId = instanceId;
        }

        public bool Equals(GameplayTagSource other) =>
            InstanceId == other.InstanceId && string.Equals(Type, other.Type, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayTagSource other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, InstanceId);
    }
}
