using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 프리팹을 Key-Value로 관리하는 데이터베이스
/// </summary>
[CreateAssetMenu(fileName = "UIPrefabDatabase", menuName = "UI/UI Prefab Database")]
public class UIPrefabDatabase : ScriptableObject
{
    [Serializable]
    public class UIPrefabEntry
    {
        [Tooltip("UI를 식별할 고유 키")]
        public string key;

        [Tooltip("UI 프리팹")]
        public GameObject prefab;

        [Tooltip("기본 캔버스 레이어")]
        public CanvasLayer defaultLayer = CanvasLayer.Normal;
    }

    [SerializeField]
    private List<UIPrefabEntry> uiPrefabs = new List<UIPrefabEntry>();

    // 빠른 검색을 위한 캐시
    private Dictionary<string, UIPrefabEntry> _prefabCache;

    /// <summary>
    /// 초기화 (캐시 생성)
    /// </summary>
    public void Initialize()
    {
        _prefabCache = new Dictionary<string, UIPrefabEntry>();

        foreach (var entry in uiPrefabs)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning("[UIPrefabDatabase] 빈 키가 존재합니다.");
                continue;
            }

            if (entry.prefab == null)
            {
                Debug.LogWarning($"[UIPrefabDatabase] '{entry.key}' 키의 프리팹이 null입니다.");
                continue;
            }

            if (_prefabCache.ContainsKey(entry.key))
            {
                Debug.LogWarning($"[UIPrefabDatabase] 중복된 키: {entry.key}");
                continue;
            }

            _prefabCache.Add(entry.key, entry);
        }

        Debug.Log($"[UIPrefabDatabase] {_prefabCache.Count}개의 UI 프리팹 로드 완료");
    }

    /// <summary>
    /// Key로 프리팹 가져오기
    /// </summary>
    public GameObject GetPrefab(string key)
    {
        if (_prefabCache == null)
        {
            Initialize();
        }

        if (_prefabCache.TryGetValue(key, out UIPrefabEntry entry))
        {
            return entry.prefab;
        }

        Debug.LogWarning($"[UIPrefabDatabase] '{key}' 키를 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// Key로 프리팹 엔트리 가져오기
    /// </summary>
    public UIPrefabEntry GetPrefabEntry(string key)
    {
        if (_prefabCache == null)
        {
            Initialize();
        }

        if (_prefabCache.TryGetValue(key, out UIPrefabEntry entry))
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    /// 등록된 모든 UI 키 반환
    /// </summary>
    public List<string> GetAllKeys()
    {
        if (_prefabCache == null)
        {
            Initialize();
        }

        return new List<string>(_prefabCache.Keys);
    }

    /// <summary>
    /// UI가 등록되어 있는지 확인
    /// </summary>
    public bool HasKey(string key)
    {
        if (_prefabCache == null)
        {
            Initialize();
        }

        return _prefabCache.ContainsKey(key);
    }
}