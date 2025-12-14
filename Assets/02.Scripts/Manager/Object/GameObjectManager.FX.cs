
using Game.FSM;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
{
    private const string FX_DATABASE_PATH = "UIPrefabDatabase";
    [SerializeField] private FXPrefabDatabase _fxPrefabDatabase;
    private async void LoadFXPrefabDatabase()
    {
        var handle = Addressables.LoadAssetAsync<FXPrefabDatabase>(FX_DATABASE_PATH);
    
        try
        {
            _fxPrefabDatabase = await handle.Task;
        
            if (_fxPrefabDatabase == null)
            {
                Debug.LogError($"[UIManager] UIPrefabDatabase를 '{FX_DATABASE_PATH}' 경로에서 찾을 수 없습니다.");
                return;
            }
        
            _fxPrefabDatabase.Initialize();
            Debug.Log($"[UIManager] FXPrefabDatabase 로드 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] FXPrefabDatabase 로드 실패: {e.Message}");
        }
    }

    public GameObject ShowFX(string key, Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        if (_fxPrefabDatabase == null)
        {
            Debug.LogError("[GameObjectManager] FXPrefabDatabase 로드되지 않았습니다.");
            return null;
        }

        var prefabEntry = _fxPrefabDatabase.GetPrefabEntry(key);
        if (prefabEntry == null)
        {
            Debug.LogError($"[GameObjectManager] '{key}' FX를 찾을 수 없습니다.");
            return null;
        }

        if (prefabEntry.prefab == null)
        {
            Debug.LogError($"[GameObjectManager] '{key}' FX의 프리팹이 null입니다.");
            return null;
        }
        
        // 인스턴스 생성
        return Instantiate(prefabEntry.prefab, position, rotation, parent);
    }
}
