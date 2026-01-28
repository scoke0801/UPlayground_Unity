using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Path
{
    /// <summary>
    /// Weapon 프리팹 데이터베이스
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponPrefabDatabase", menuName = "UP/PathDatabase/Weapon")]
    public class WeaponPrefabDatabase : ScriptableObject
    {
        [System.Serializable]
        public class WeaponPrefabEntry
        {
            [Tooltip("Weapon 식별 키 ")] public string key;

            [Tooltip("Weapon 프리팹")] public GameObject prefab;

            [Tooltip("설명 (선택)")] public string description;

            public WeaponType weaponType;
            public EquipPosition equipPosition;
        }

        [SerializeField] private List<WeaponPrefabEntry> prefabs = new List<WeaponPrefabEntry>();

        // 빠른 검색을 위한 딕셔너리
        private Dictionary<string, WeaponPrefabEntry> _prefabDictionary;

        /// <summary>   
        /// 초기화 (UIManager가 호출)
        /// </summary>
        public void Initialize()
        {
            _prefabDictionary = new Dictionary<string, WeaponPrefabEntry>();

            foreach (var entry in prefabs)
            {
                if (string.IsNullOrEmpty(entry.key))
                {
                    Debug.LogWarning("[UIPrefabDatabase] 키가 비어있는 항목이 있습니다.");
                    continue;
                }

                if (_prefabDictionary.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[UIPrefabDatabase] 중복된 키가 있습니다: {entry.key}");
                    continue;
                }

                _prefabDictionary.Add(entry.key, entry);
            }

            Debug.Log($"[UIPrefabDatabase] {_prefabDictionary.Count}개의 UI 프리팹 로드 완료");
        }

        /// <summary>
        /// 키로 프리팹 엔트리 가져오기
        /// </summary>
        public WeaponPrefabEntry GetPrefabEntry(string key)
        {
            if (_prefabDictionary == null)
            {
                Debug.LogError("[UIPrefabDatabase] 초기화되지 않았습니다!");
                return null;
            }

            if (_prefabDictionary.TryGetValue(key, out WeaponPrefabEntry entry))
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

    }
}