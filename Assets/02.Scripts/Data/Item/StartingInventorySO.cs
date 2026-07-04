using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Item
{
    [Serializable]
    public class StartingItemEntry
    {
        public ItemSO item;
        [Min(1)] public int count = 1;
    }

    /// <summary>
    /// 새 게임 시작 시에만 지급할 초기 인벤토리 데이터.
    /// 세이브 로드에는 적용하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "StartingInventory", menuName = "UPlayGround/아이템/Starting Inventory")]
    public class StartingInventorySO : ScriptableObject
    {
        [Tooltip("새 게임 시작 시 인벤토리에만 지급할 아이템 목록. 장착 상태에는 영향을 주지 않는다.")]
        public List<StartingItemEntry> items = new();
    }
}
