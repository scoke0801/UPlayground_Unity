using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 프리팹 데이터베이스
/// UIManager가 자동으로 로드
/// </summary>
[CreateAssetMenu(fileName = "UIPrefabDatabase", menuName = "UP/PathDatabase/UI")]
public class UIPrefabDatabase : ScriptableObject
{
    [System.Serializable]
    public class UIPrefabEntry
    {
        [Tooltip("UI 식별 키 (ShowUI 호출 시 사용)")]
        public string key;
        
        [Tooltip("UI 프리팹")]
        public GameObject prefab;
        
        [Tooltip("기본 캔버스 레이어")]
        public CanvasLayer defaultLayer = CanvasLayer.Popup;
        
        [Tooltip("설명 (선택)")]
        public string description;
    }

    [SerializeField]
    private List<UIPrefabEntry> prefabs = new List<UIPrefabEntry>();

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<string, UIPrefabEntry> _prefabDictionary;

    /// <summary>
    /// 초기화 (UIManager가 호출)
    /// </summary>
    public void Initialize()
    {
        _prefabDictionary = new Dictionary<string, UIPrefabEntry>();

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
    public UIPrefabEntry GetPrefabEntry(string key)
    {
        if (_prefabDictionary == null)
        {
            Debug.LogError("[UIPrefabDatabase] 초기화되지 않았습니다!");
            return null;
        }

        if (_prefabDictionary.TryGetValue(key, out UIPrefabEntry entry))
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
    public void AddPrefab(string key, GameObject prefab, CanvasLayer layer, string description = "")
    {
        prefabs.Add(new UIPrefabEntry
        {
            key = key,
            prefab = prefab,
            defaultLayer = layer,
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