using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Path
{
    /// <summary>
    /// ItemSO 데이터베이스
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "UPlayGround/PathDatabase/Item")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();

        private Dictionary<int, ItemSO> itemDictionary;

        public IReadOnlyList<ItemSO> AllItems => allItems;

        // 초기화 (게임 시작 시 호출)
        public void Initialize()
        {
            itemDictionary = new Dictionary<int, ItemSO>();

            foreach (var item in allItems)
            {
                if (item != null && !itemDictionary.ContainsKey(item.itemId))
                {
                    itemDictionary.Add(item.itemId, item);
                }
            }
        }

        // ID로 아이템 검색
        public ItemSO GetItemById(int itemId)
        {
            if (itemDictionary == null)
                Initialize();

            return itemDictionary.TryGetValue(itemId, out var item) ? item : null;
        }

        // 타입별 아이템 검색
        public List<ItemSO> GetItemsByType(ItemType type)
        {
            return allItems.Where(item => item != null && item.itemType == type).ToList();
        }

        // 장비 슬롯별 검색
        public List<EquipmentSO> GetEquipmentsBySlot(EquipPosition slot)
        {
            return allItems
                .OfType<EquipmentSO>()
                .Where(equip => equip.equipSlot == slot)
                .ToList();
        }

#if UNITY_EDITOR
        // 에디터에서 자동 수집
        [ContextMenu("Refresh Item Database")]
        public void RefreshDatabase(string itemFolderPath)
        {
            allItems.Clear();

            // 특정 폴더 내의 모든 ItemSO 검색
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemSO", new[] { itemFolderPath });
        
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemSO item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            
                if (item != null && !allItems.Contains(item))
                {
                    allItems.Add(item);
                }
            }
        
            allItems = allItems.OrderBy(item => item.itemId).ToList();
        
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"ItemDatabase 갱신 완료: {allItems.Count}개 아이템 로드됨");
        }
#endif
    }
}