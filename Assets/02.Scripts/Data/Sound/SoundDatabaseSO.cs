using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Sound
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "UPlayGround/Audio/Sound Database")]
    public sealed class SoundDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> entries = new();

        private readonly Dictionary<string, SoundEntry> _lookup = new();
        private bool _initialized;

        public IReadOnlyList<SoundEntry> Entries => entries;

        public void Initialize()
        {
            _lookup.Clear();

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogWarning($"[{nameof(SoundDatabaseSO)}] 비어 있는 사운드 key가 있습니다: {name}");
                    continue;
                }

                if (_lookup.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[{nameof(SoundDatabaseSO)}] 중복 사운드 key를 무시합니다: {entry.key}");
                    continue;
                }

                _lookup.Add(entry.key, entry);
            }

            _initialized = true;
        }

        public bool TryGet(string key, out SoundEntry entry)
        {
            if (!_initialized)
                Initialize();

            if (string.IsNullOrWhiteSpace(key))
            {
                entry = null;
                return false;
            }

            return _lookup.TryGetValue(key, out entry);
        }
    }
}
