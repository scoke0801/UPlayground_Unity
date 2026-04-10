using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Path
{
    /// <summary>
    /// FX 프리팹 데이터베이스
    /// </summary>
    [CreateAssetMenu(fileName = "FXPrefabDatabase", menuName = "UPlayGround/PathDatabase/FX")]
    public class FXPrefabDatabase : ScriptableObject
    {
        [System.Serializable]
        public class FXPrefabEntry
        {
            [Tooltip("식별 키 (호출 시 사용)")] public string key;

            [Tooltip("프리팹")] public GameObject prefab;
            [Tooltip("설명 (선택)")] public string description;
        }

        [SerializeField] private List<FXPrefabEntry> prefabs = new List<FXPrefabEntry>();

        // 빠른 검색을 위한 딕셔너리
        private Dictionary<string, FXPrefabEntry> _prefabDictionary;

        /// <summary>
        /// 초기화 (UIManager가 호출)
        /// </summary>
        public void Initialize()
        {
            _prefabDictionary = new Dictionary<string, FXPrefabEntry>();

            foreach (var entry in prefabs)
            {
                if (string.IsNullOrEmpty(entry.key))
                {
                    Debug.LogWarning("[FXPrefabDatabase] 키가 비어있는 항목이 있습니다.");
                    continue;
                }

                if (_prefabDictionary.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[FXPrefabDatabase] 중복된 키가 있습니다: {entry.key}");
                    continue;
                }

                _prefabDictionary.Add(entry.key, entry);
            }

            Debug.Log($"[FXPrefabDatabase] {_prefabDictionary.Count}개의 UI 프리팹 로드 완료");
        }

        public FXPrefabEntry GetPrefabEntry(FXKeyType key) => GetPrefabEntry(key.ToKey());
        public GameObject    GetPrefab(FXKeyType key)      => GetPrefab(key.ToKey());

        /// <summary>
        /// 키로 프리팹 엔트리 가져오기
        /// </summary>
        public FXPrefabEntry GetPrefabEntry(string key)
        {
            if (_prefabDictionary == null)
            {
                Debug.LogError("[UIPrefabDatabase] 초기화되지 않았습니다!");
                return null;
            }

            if (_prefabDictionary.TryGetValue(key, out FXPrefabEntry entry))
            {
                return entry;
            }

            return null;
        }

        /// <summary>
        /// 키로 프리팹 직접 가져오기
        /// </summary>
        public GameObject GetPrefab(string key)
        {
            var entry = GetPrefabEntry(key);
            return entry?.prefab;
        }

        /// <summary>
        /// 등록된 모든 키 목록
        /// </summary>
        public List<string> GetAllKeys()
        {
            return new List<string>(_prefabDictionary.Keys);
        }

        /// <summary>
        /// Editor용: 프리팹 추가
        /// </summary>
        public void AddPrefab(string key, GameObject prefab, string description = "")
        {
            prefabs.Add(new FXPrefabEntry
            {
                key = key,
                prefab = prefab,
                description = description
            });

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Editor용: 프리팹 제거
        /// </summary>
        public void RemovePrefab(string key)
        {
            prefabs.RemoveAll(p => p.key == key);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}