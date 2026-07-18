using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인벤토리와 HUD 사이에서 공유하는 퀵슬롯 배치 상태.
    /// 신규 런타임은 빈 상태로 시작하며 소비 아이템만 등록할 수 있다.
    /// </summary>
    public static class UIQuickSlotAssignments
    {
        public const int SlotCount = 4;

        private static readonly int[] ItemIds = new int[SlotCount];
        private static bool _initialAssignmentCompleted;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            Array.Clear(ItemIds, 0, ItemIds.Length);
            _initialAssignmentCompleted = false;
            Changed = null;
        }

        public static int GetItemId(int slotIndex) =>
            IsValidSlot(slotIndex) ? ItemIds[slotIndex] : 0;

        public static bool Assign(int slotIndex, ItemSO item)
        {
            if (!IsValidSlot(slotIndex)
                || item == null
                || item.itemType != ItemType.CONSUMABLE)
                return false;

            if (slotIndex == 0)
                _initialAssignmentCompleted = true;
            ItemIds[slotIndex] = item.itemId;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 신규 런타임의 시작 아이템을 빈 퀵슬롯에 한 번만 배치한다.
        /// 이후 사용자가 슬롯을 비우더라도 자동으로 다시 등록하지 않는다.
        /// </summary>
        public static bool AssignInitialIfEmpty(int slotIndex, ItemSO item)
        {
            if (_initialAssignmentCompleted
                || !IsValidSlot(slotIndex)
                || ItemIds[slotIndex] != 0
                || item == null
                || item.itemType != ItemType.CONSUMABLE)
                return false;

            _initialAssignmentCompleted = true;
            ItemIds[slotIndex] = item.itemId;
            Changed?.Invoke();
            return true;
        }

        public static bool Clear(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return false;

            ItemIds[slotIndex] = 0;
            Changed?.Invoke();
            return true;
        }

        private static bool IsValidSlot(int slotIndex) =>
            (uint)slotIndex < SlotCount;
    }
}
