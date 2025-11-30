using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
    }

    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    #endregion
    
    IEnumerator InitLoad()
    {
        // 로드하고 싶은 타입을 여기서 명시 (Addressable Key와 매칭된다고 가정)
        //yield return LoadJsonData<WeaponData>("Table_Weapon");
        //yield return LoadJsonData<EnemyData>("Table_Enemy");
        // 새로운 데이터 타입이 생겨도 여기에 한 줄만 추가하면 됨
        
        yield return new WaitForEndOfFrame();
        Debug.Log("All Data Loaded Complete.");
    }
    
    // 1. 제네릭 로드 함수 (Addressables 사용)
    private IEnumerator LoadJsonData<T>(string key) where T : class, ILoader<int, T>
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(key);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            string json = handle.Result.text;
            
            // 파싱 (Wrapper는 이전 답변 참고)
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            
            // T 타입 전용 딕셔너리 생성
            Dictionary<int, T> dic = new Dictionary<int, T>();
            foreach (var data in wrapper.dataList)
            {
                dic.Add(data.GetKey(), data);
            }

            // 저장소에 Type을 키로 등록
            if (_repositories.ContainsKey(typeof(T)))
                _repositories[typeof(T)] = dic;
            else
                _repositories.Add(typeof(T), dic);

            Debug.Log($"Loaded [{typeof(T).Name}]: {dic.Count} items");
        }
        
        Addressables.Release(handle);
    }

    // 2. 외부에서 데이터를 가져오는 제네릭 함수 (핵심)
    public T GetData<T>(int id) where T : class
    {
        Type type = typeof(T);

        // 1. 해당 타입의 딕셔너리가 있는지 확인
        if (_repositories.TryGetValue(type, out object repoObj))
        {
            // 2. object를 원래 타입인 Dictionary<int, T>로 캐스팅
            var repo = repoObj as Dictionary<int, T>;
            
            // 3. ID로 데이터 검색
            if (repo != null && repo.TryGetValue(id, out T data))
            {
                return data;
            }
        }
        
        Debug.LogError($"Data not found: Type {type.Name}, ID {id}");
        return null;
    }
    
    // 전체 리스트가 필요할 때
    public List<T> GetAllData<T>() where T : class
    {
        Type type = typeof(T);
        if (_repositories.TryGetValue(type, out object repoObj))
        {
            var repo = repoObj as Dictionary<int, T>;
            return new List<T>(repo.Values);
        }
        return new List<T>();
    }
}