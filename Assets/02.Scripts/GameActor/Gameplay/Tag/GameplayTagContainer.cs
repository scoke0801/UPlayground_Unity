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

        public event Action<GameplayTag> OnTagAdded;
        public event Action<GameplayTag> OnTagRemoved;

        // ── 추가 / 제거 ────────────────────────────────────────────────

        public void AddTag(GameplayTag tag)
        {
            if (!tag.IsValid() || !_tags.Add(tag)) return;
            OnTagAdded?.Invoke(tag);
        }

        /// <summary>enum 기반 AddTag 오버로드</summary>
        public void AddTag(GameplayTagId id)
        {
            if (id != GameplayTagId.None) AddTag(id.ToTag());
        }

        public void RemoveTag(GameplayTag tag)
        {
            if (!_tags.Remove(tag)) return;
            OnTagRemoved?.Invoke(tag);
        }

        /// <summary>enum 기반 RemoveTag 오버로드</summary>
        public void RemoveTag(GameplayTagId id)
        {
            if (id != GameplayTagId.None) RemoveTag(id.ToTag());
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
        public bool HasTag(GameplayTag tag) => _tags.Contains(tag);

        /// <summary>enum 기반 태그 보유 여부 (GameplayTagId → GameplayTag 자동 변환)</summary>
        public bool HasTag(GameplayTagId id) => id != GameplayTagId.None && _tags.Contains(id.ToTag());

        /// <summary>parent 계층 아래 임의의 태그를 보유하는지 확인</summary>
        public bool HasTagInHierarchy(GameplayTag parent)
        {
            foreach (var t in _tags)
                if (t.IsChildOf(parent)) return true;
            return false;
        }

        /// <summary>주어진 태그 중 하나라도 보유하면 true</summary>
        public bool HasAnyTag(IEnumerable<GameplayTag> tags)
        {
            foreach (var t in tags)
                if (_tags.Contains(t)) return true;
            return false;
        }

        /// <summary>주어진 태그 전부를 보유해야 true</summary>
        public bool HasAllTags(IEnumerable<GameplayTag> tags)
        {
            foreach (var t in tags)
                if (!_tags.Contains(t)) return false;
            return true;
        }

        public IReadOnlyCollection<GameplayTag> AllTags => _tags;

        public void Clear()
        {
            var copy = new List<GameplayTag>(_tags);
            foreach (var t in copy) RemoveTag(t);
        }

        public override string ToString() => string.Join(", ", _tags);
    }
}
