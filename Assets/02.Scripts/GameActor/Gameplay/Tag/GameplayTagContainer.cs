using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// GameActor에 부착되어 런타임 태그 집합을 관리하는 컴포넌트.
    /// 상태 머신 상태가 OnEnter / OnExit 에서 태그를 추가 / 제거한다.
    /// </summary>
    public class GameplayTagContainer : MonoBehaviour, IGameplayTagReader
    {
        private readonly HashSet<GameplayTag> _tags = new();
        private readonly Dictionary<GameplayTag, int> _ownedTagCounts = new();
        private readonly Dictionary<ulong, OwnedTag> _ownedTags = new();
        private ulong _nextHandleId = 1;

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

        // ── 추가 / 제거 ────────────────────────────────────────────────

        public void AddTag(GameplayTag tag)
        {
            if (!tag.IsValid()) return;
            bool wasPresent = HasTag(tag);
            _tags.Add(tag);
            if (!wasPresent) OnTagAdded?.Invoke(tag);
        }

        /// <summary>enum 기반 AddTag 오버로드</summary>
        public void AddTag(GameplayTagId id)
        {
            if (id != GameplayTagId.None) AddTag(id.ToTag());
        }

        public void RemoveTag(GameplayTag tag)
        {
            if (!_tags.Remove(tag)) return;
            if (!HasTag(tag)) OnTagRemoved?.Invoke(tag);
        }

        /// <summary>enum 기반 RemoveTag 오버로드</summary>
        public void RemoveTag(GameplayTagId id)
        {
            if (id != GameplayTagId.None) RemoveTag(id.ToTag());
        }

        /// <summary>
        /// Ability/Effect가 소유하는 태그를 추가한다. 반환 핸들만 제거할 수 있어
        /// 동일 태그를 부여한 다른 소스의 소유권을 보존한다.
        /// </summary>
        public GameplayTagHandle AddTag(GameplayTagId id, GameplayTagSource source)
        {
            if (id == GameplayTagId.None) return default;
            GameplayTag tag = id.ToTag();
            if (!tag.IsValid()) return default;

            bool wasPresent = HasTag(tag);
            ulong handleId = _nextHandleId++;
            if (handleId == 0) handleId = _nextHandleId++;

            _ownedTags[handleId] = new OwnedTag(tag, source);
            _ownedTagCounts.TryGetValue(tag, out int count);
            _ownedTagCounts[tag] = count + 1;
            if (!wasPresent) OnTagAdded?.Invoke(tag);
            return new GameplayTagHandle(handleId);
        }

        public bool RemoveTag(GameplayTagHandle handle)
        {
            if (!handle.IsValid || !_ownedTags.Remove(handle.Value, out OwnedTag owned))
                return false;

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
            var toRemove = new List<GameplayTag>();
            foreach (var t in _tags)
                if (t.IsChildOf(parent)) toRemove.Add(t);
            foreach (var t in toRemove) RemoveTag(t);
        }

        // ── 쿼리 ───────────────────────────────────────────────────────

        /// <summary>정확히 일치하는 태그 보유 여부</summary>
        public bool HasTag(GameplayTag tag) =>
            _tags.Contains(tag) || _ownedTagCounts.ContainsKey(tag);

        /// <summary>enum 기반 태그 보유 여부 (GameplayTagId → GameplayTag 자동 변환)</summary>
        public bool HasTag(GameplayTagId id) => id != GameplayTagId.None && HasTag(id.ToTag());

        /// <summary>parent 계층 아래 임의의 태그를 보유하는지 확인</summary>
        public bool HasTagInHierarchy(GameplayTag parent)
        {
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
                return result;
            }
        }

        public void Clear()
        {
            var copy = new List<GameplayTag>(_tags);
            foreach (var t in copy) RemoveTag(t);
            var handles = new List<GameplayTagHandle>();
            foreach (ulong id in _ownedTags.Keys)
                handles.Add(new GameplayTagHandle(id));
            foreach (GameplayTagHandle handle in handles)
                RemoveTag(handle);
        }

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
