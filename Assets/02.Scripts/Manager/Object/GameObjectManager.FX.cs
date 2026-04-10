using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Path;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private const string FX_DATABASE_PATH = "FXPrefabDatabase";
        [SerializeField] private FXPrefabDatabase _fxPrefabDatabase;

        private readonly List<(GameObject obj, float expireTime)> _pendingDestroyFXList = new();

        private async void LoadFXPrefabDatabase()
        {
            var handle = Addressables.LoadAssetAsync<FXPrefabDatabase>(FX_DATABASE_PATH);

            try
            {
                _fxPrefabDatabase = await handle.Task;

                if (_fxPrefabDatabase == null)
                {
                    Debug.LogError($"[GameObjectManager] FXPrefabDatabase를 '{FX_DATABASE_PATH}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                _fxPrefabDatabase.Initialize();
                Debug.Log($"[GameObjectManager] FXPrefabDatabase 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameObjectManager] FXPrefabDatabase 로드 실패: {e.Message}");
            }
        }

        private void ProcessPendingDestroyFX()
        {
            if (_pendingDestroyFXList.Count == 0) return;

            float now = Time.time;

            for (int i = _pendingDestroyFXList.Count - 1; i >= 0; i--)
            {
                var (obj, expireTime) = _pendingDestroyFXList[i];

                if (now < expireTime) continue;

                if (obj != null)
                    Destroy(obj);

                _pendingDestroyFXList.RemoveAt(i);
            }
        }

        public void RegisterFXInstance(GameObject instance, float lifeTime)
        {
            _pendingDestroyFXList.Add((instance, Time.time + lifeTime));
        }
        
        public GameObject ShowFX(FXKeyType key, Vector3 position, Quaternion rotation = default, Transform parent = null, float duration = 5f)
            => ShowFX(key.ToKey(), position, rotation, parent, duration);

        public GameObject ShowFX(string key, Vector3 position, Quaternion rotation = default, Transform parent = null, float duration = 5f)
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
            
            // rotation이 default(zero quaternion)이면 프리팹 자체 회전을 그대로 사용.
            // 외부에서 방향을 지정한 경우에는 "지정 회전 * 프리팹 회전"으로 합성해
            // 프리팹에 설정된 로컬 오프셋(예: -90,0,0)을 보존한다.
            Quaternion baseRot    = (rotation == default) ? Quaternion.identity : rotation;
            Quaternion finalRot   = baseRot * prefabEntry.prefab.transform.rotation;
            var instance = Instantiate(prefabEntry.prefab, position, finalRot, parent);

            if (duration > 0f)
                _pendingDestroyFXList.Add((instance, Time.time + duration));

            return instance;
        }
    }
}