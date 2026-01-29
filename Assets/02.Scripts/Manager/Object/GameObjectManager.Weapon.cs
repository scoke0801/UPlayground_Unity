using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Path;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private const string WEAPON_DATABASE_PATH = "WeaponPrefabDatabase";
        [SerializeField] private WeaponPrefabDatabase _weaponPrefabDatabase;

        public bool IsWeaponDBLoaded { get; set; } = false;

        private async void LoadWeaponPrefabDatabase()
        {
            var handle = Addressables.LoadAssetAsync<WeaponPrefabDatabase>(WEAPON_DATABASE_PATH);

            try
            {
                _weaponPrefabDatabase = await handle.Task;

                if (_weaponPrefabDatabase == null)
                {
                    Debug.LogError(
                        $"[GameObjectManager] WeaponPrefabDatabase를 '{WEAPON_DATABASE_PATH}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                IsWeaponDBLoaded = true;
                _weaponPrefabDatabase.Initialize();
                Debug.Log($"[GameObjectManager] WeaponPrefabDatabase 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameObjectManager] WeaponPrefabDatabase 로드 실패: {e.Message}");
            }
        }

        public GameObject CreateWeapon(string key)
        {
            if (_weaponPrefabDatabase == null)
            {
                Debug.LogError("[GameObjectManager] WeaponPrefabDatabase 로드되지 않았습니다.");
                return null;
            }

            var prefabEntry = _weaponPrefabDatabase.GetPrefabEntry(key);
            if (prefabEntry == null)
            {
                Debug.LogError($"[GameObjectManager] '{key}' WeaponPrefab를 찾을 수 없습니다.");
                return null;
            }

            if (prefabEntry.prefab == null)
            {
                Debug.LogError($"[GameObjectManager] '{key}' WeaponPrefab의 프리팹이 null입니다.");
                return null;
            }

            // 인스턴스 생성
            return Instantiate(prefabEntry.prefab);
        }
    }
}