using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Path;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        public GameObject CreateWeapon(int itemKey)
        {
            EquipmentSO equipmentData = ItemManager.Instance.GetItemData(itemKey) as EquipmentSO;
            if (equipmentData == null)
            {
                Debug.LogError("[GameObjectManager] equipmentData 로드되지 않았습니다.");
                return null;
            }

            var prefabEntry = equipmentData.equipmentPrefab;
            if (prefabEntry == null)
            {
                Debug.LogError($"[GameObjectManager] '{itemKey}' WeaponPrefab의 프리팹이 null입니다.");
                return null;
            }

            // 인스턴스 생성
            return Instantiate(prefabEntry);
        }
    }
}