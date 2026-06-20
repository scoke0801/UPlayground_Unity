using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Sound
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "UPlayGround/오디오/Sound Database")]
    public sealed class SoundDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<SoundEntrySO> entries = new();

        private readonly Dictionary<string, SoundEntrySO> _lookup = new();
        private bool _initialized;

        public IReadOnlyList<SoundEntrySO> Entries => entries;

        public void Initialize()
        {
            _lookup.Clear();

            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                // key가 비면 에셋 이름을 유효 key로 사용한다(SoundEntrySO.OnValidate와 동일 규칙).
                // 에디터에서는 OnValidate가 이미 직렬화 자산에 key를 채워두므로, 공유 SO 인스턴스를
                // 플레이 모드에서 변형하지 않는다(변경이 자산에 잔류하는 것을 방지).
                // 빌드에서는 OnValidate가 없으므로 런타임에 한 번 되써준다(인메모리 한정·디스크 비영속).
                // SoundManager가 entry.key로 쿨다운/동시재생을 추적하기 때문이다.
                if (string.IsNullOrWhiteSpace(entry.key))
                {
#if !UNITY_EDITOR
                    entry.key = entry.name;
#endif
                }

                string key = string.IsNullOrWhiteSpace(entry.key) ? entry.name : entry.key;

                if (string.IsNullOrWhiteSpace(key))
                {
                    Debug.LogWarning($"[{nameof(SoundDatabaseSO)}] 비어 있는 사운드 key가 있습니다: {name}");
                    continue;
                }

                if (_lookup.ContainsKey(key))
                {
                    Debug.LogWarning($"[{nameof(SoundDatabaseSO)}] 중복 사운드 key를 무시합니다: {key}");
                    continue;
                }

                _lookup.Add(key, entry);
            }

            _initialized = true;
        }

        public bool TryGet(string key, out SoundEntrySO entry)
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
