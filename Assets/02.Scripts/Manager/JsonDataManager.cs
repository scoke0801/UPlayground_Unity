using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Game.Data;

public class JsonDataManager : BaseManager<JsonDataManager>, IManager
{
    // 모든 종류의 데이터 딕셔너리를 보관하는 저장소
    // Key: 데이터 타입(Type), Value: 그 타입의 Dictionary<int, T> (object로 저장)
    private Dictionary<Type, object> _repositories = new Dictionary<Type, object>();
    
    #region IManager 구현
    public void Init()
    {
        // 게임 시작 시 필요한 데이터를 등록
        StartCoroutine(InitLoad());
    }

    public void Dispose()
    {
        _repositories.Clear();
    }

    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    #endregion
    
    IEnumerator InitLoad()
    {
        // Addressable 키로 스킬 데이터 로드
        yield return LoadSkillDataFromAddressable("SkillDataTable");
        
        // 다른 데이터 타입 로드 예시
        // yield return LoadJsonData<WeaponData>("Table_Weapon");
        // yield return LoadJsonData<EnemyData>("Table_Enemy");
        
        yield return new WaitForEndOfFrame();
        Debug.Log("[JsonDataManager] All Data Loaded Complete.");
    }
    
    /// <summary>
    /// 스킬 데이터 Addressable 로드 (SkillJsonData 전용)
    /// </summary>
    private IEnumerator LoadSkillDataFromAddressable(string addressableKey)
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            string json = handle.Result.text;
            
            // 스킬 데이터 파싱
            SkillDataWrapper wrapper = JsonUtility.FromJson<SkillDataWrapper>(json);
            
            if (wrapper == null || wrapper.skills == null)
            {
                Debug.LogError($"[JsonDataManager] {addressableKey} 파싱 실패!");
                Addressables.Release(handle);
                yield break;
            }
            
            // Dictionary 생성
            Dictionary<int, SkillJsonData> dic = new Dictionary<int, SkillJsonData>();
            foreach (var data in wrapper.skills)
            {
                if (!dic.ContainsKey(data.GetKey()))
                {
                    dic.Add(data.GetKey(), data);
                }
                else
                {
                    Debug.LogWarning($"[JsonDataManager] 중복된 스킬 ID: {data.GetKey()}");
                }
            }

            // 저장소에 등록
            Type type = typeof(SkillJsonData);
            if (_repositories.ContainsKey(type))
                _repositories[type] = dic;
            else
                _repositories.Add(type, dic);

            Debug.Log($"[JsonDataManager] Loaded [SkillJsonData]: {dic.Count} items");
        }
        else
        {
            Debug.LogError($"[JsonDataManager] Failed to load Addressable: {addressableKey}, Status: {handle.Status}");
        }
        
        Addressables.Release(handle);
    }
    
    /// <summary>
    /// 범용 Json 데이터 로드 (ILoader 구현 타입용)
    /// </summary>
    public IEnumerator LoadJsonData<T>(string addressableKey) where T : class, ILoader<int, T>
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(addressableKey);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            string json = handle.Result.text;
            
            // 파싱
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            
            if (wrapper == null || wrapper.dataList == null)
            {
                Debug.LogError($"[JsonDataManager] {addressableKey} 파싱 실패!");
                Addressables.Release(handle);
                yield break;
            }
            
            // Dictionary 생성
            Dictionary<int, T> dic = new Dictionary<int, T>();
            foreach (var data in wrapper.dataList)
            {
                int key = data.GetKey();
                if (!dic.ContainsKey(key))
                {
                    dic.Add(key, data);
                }
                else
                {
                    Debug.LogWarning($"[JsonDataManager] 중복된 키: {key} in {typeof(T).Name}");
                }
            }

            // 저장소에 등록
            Type type = typeof(T);
            if (_repositories.ContainsKey(type))
                _repositories[type] = dic;
            else
                _repositories.Add(type, dic);

            Debug.Log($"[JsonDataManager] Loaded [{type.Name}]: {dic.Count} items");
        }
        else
        {
            Debug.LogError($"[JsonDataManager] Failed to load Addressable: {addressableKey}, Status: {handle.Status}");
        }
        
        Addressables.Release(handle);
    }

    /// <summary>
    /// 외부에서 데이터를 가져오는 제네릭 함수
    /// </summary>
    public T GetData<T>(int id) where T : class
    {
        Type type = typeof(T);

        if (_repositories.TryGetValue(type, out object repoObj))
        {
            var repo = repoObj as Dictionary<int, T>;
            
            if (repo != null && repo.TryGetValue(id, out T data))
            {
                return data;
            }
        }
        
        Debug.LogWarning($"[JsonDataManager] Data not found: Type {type.Name}, ID {id}");
        return null;
    }
    
    /// <summary>
    /// 전체 리스트가 필요할 때
    /// </summary>
    public List<T> GetAllData<T>() where T : class
    {
        Type type = typeof(T);
        if (_repositories.TryGetValue(type, out object repoObj))
        {
            var repo = repoObj as Dictionary<int, T>;
            if (repo != null)
            {
                return new List<T>(repo.Values);
            }
        }
        return new List<T>();
    }
    
    /// <summary>
    /// 특정 타입의 데이터가 로드되었는지 확인
    /// </summary>
    public bool IsDataLoaded<T>() where T : class
    {
        return _repositories.ContainsKey(typeof(T));
    }
    
    /// <summary>
    /// 특정 타입의 데이터 개수 반환
    /// </summary>
    public int GetDataCount<T>() where T : class
    {
        Type type = typeof(T);
        if (_repositories.TryGetValue(type, out object repoObj))
        {
            var repo = repoObj as Dictionary<int, T>;
            return repo?.Count ?? 0;
        }
        return 0;
    }
}
