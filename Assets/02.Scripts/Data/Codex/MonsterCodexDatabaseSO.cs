using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Codex
{
    /// <summary>도감 표시 순서와 actorId 기반 정의 조회를 소유한다.</summary>
    [CreateAssetMenu(
        fileName = "MonsterCodexDatabase",
        menuName = "UPlayGround/도감/Monster Codex Database")]
    public sealed class MonsterCodexDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<MonsterCodexEntrySO> _entries = new();

        private Dictionary<string, MonsterCodexEntrySO> _lookup;

        public IReadOnlyList<MonsterCodexEntrySO> Entries => _entries;

        public void Initialize()
        {
            _lookup = new Dictionary<string, MonsterCodexEntrySO>();
            foreach (MonsterCodexEntrySO entry in _entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.actorId))
                {
                    Debug.LogWarning($"[MonsterCodexDatabase] actorId가 비어있는 항목: {entry.name}");
                    continue;
                }

                if (!_lookup.TryAdd(entry.actorId, entry))
                    Debug.LogWarning($"[MonsterCodexDatabase] 중복 actorId: {entry.actorId}");
            }
        }

        public bool TryGetEntry(string actorId, out MonsterCodexEntrySO entry)
        {
            EnsureInitialized();
            entry = null;
            return !string.IsNullOrEmpty(actorId) && _lookup.TryGetValue(actorId, out entry);
        }

        public MonsterCodexEntrySO GetEntry(string actorId) =>
            TryGetEntry(actorId, out MonsterCodexEntrySO entry) ? entry : null;

        private void EnsureInitialized()
        {
            if (_lookup == null)
                Initialize();
        }

        public bool AddEntry(MonsterCodexEntrySO entry)
        {
            if (entry == null || _entries.Contains(entry))
                return false;

            _entries.Add(entry);
            _lookup = null;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            return true;
        }

        public void InvalidateLookup() => _lookup = null;
    }
}
