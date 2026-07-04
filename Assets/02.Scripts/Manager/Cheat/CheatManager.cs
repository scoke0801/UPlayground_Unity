using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 치트 옵션 관리 매니저.
    /// GameManager에 등록되며 개발/테스트용 옵션을 중앙 관리한다.
    /// </summary>
    public class CheatManager : BaseManager<CheatManager>, IManager
    {
        [Header("전투 치트")]
        [Tooltip("활성화 시 어떤 상태에서도 적의 공격을 패리할 수 있다")]
        [SerializeField] private bool _alwaysParry = false;

        /// <summary> 항상 패리 가능 여부 </summary>
        public bool IsAlwaysParryEnabled => _alwaysParry;

        public void SetAlwaysParry(bool value)
        {
            _alwaysParry = value;
            Debug.Log($"[CheatManager] 항상 패리: {(_alwaysParry ? "ON" : "OFF")}");
        }

        public void ToggleAlwaysParry()     => SetAlwaysParry(!_alwaysParry);

        /// <summary>
        /// 지정 아이템을 인벤토리에 지급한다.
        /// 장비 아이템은 InventoryManager 정책에 따라 이미 보유 중이면 추가되지 않는다.
        /// </summary>
        public bool GrantItem(int itemId, int count)
        {
            if (count <= 0)
            {
                Debug.LogWarning($"[CheatManager] 아이템 지급 실패: 수량이 올바르지 않습니다. ({count})");
                return false;
            }

            var itemManager = ItemManager.Instance;
            var inventoryManager = InventoryManager.Instance;
            if (itemManager == null || inventoryManager == null)
            {
                Debug.LogWarning("[CheatManager] 아이템 지급 실패: ItemManager 또는 InventoryManager가 없습니다.");
                return false;
            }

            if (!itemManager.IsItemDBLoaded)
            {
                Debug.LogWarning("[CheatManager] 아이템 지급 실패: ItemDatabase가 아직 로드되지 않았습니다.");
                return false;
            }

            var itemData = itemManager.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[CheatManager] 아이템 지급 실패: ItemID {itemId}를 찾을 수 없습니다.");
                return false;
            }

            int beforeCount = inventoryManager.GetItemCount(itemId);
            inventoryManager.AddItem(itemId, count);
            int afterCount = inventoryManager.GetItemCount(itemId);

            Debug.Log(
                $"[CheatManager] 아이템 지급: {itemData.itemName}({itemId}) x{count} " +
                $"보유 {beforeCount} → {afterCount}");
            return afterCount > beforeCount || beforeCount > 0;
        }

        public bool GrantItem(ItemIdType itemId, int count) => GrantItem((int)itemId, count);

        #region IManager

        public void Init()                          => Debug.Log("[CheatManager] 초기화");
        public void AfterInit()                     { }
        public void Dispose()                       { }
        public void OnUpdate()                      { }
        public void OnFixedUpdate()                 { }
        public void OnLateUpdate()                  { }
        public void OnSceneChanged(string sceneType){ }

        #endregion
    }
}
