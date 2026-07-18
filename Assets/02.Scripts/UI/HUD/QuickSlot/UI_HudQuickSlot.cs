using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Item;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 좌하단 소비 아이템 퀵슬롯 HUD.
    /// PlayerAction의 방향별 QuickSlot 액션으로 등록 아이템을 사용한다.
    /// </summary>
    public sealed class UI_HudQuickSlot : UI_Base
    {
        private const int StartingQuickSlotIndex = 0;
        private const int StartingPotionCount = 5;

        [SerializeField] private List<UIHudQuickSlotEntry> _slots = new();

        protected override void RegisterInputEvents()
        {
            var input = Svc.Input;
            if (input == null)
                return;

            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Left,
                null, OnQuickSlotLeft, null, null, null, InputLayer.Level_0);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Right,
                null, OnQuickSlotRight, null, null, null, InputLayer.Level_0);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Up,
                null, OnQuickSlotUp, null, null, null, InputLayer.Level_0);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Down,
                null, OnQuickSlotDown, null, null, null, InputLayer.Level_0);
        }

        protected override void UnRegisterInputEvents()
        {
            var input = Svc.Input;
            if (input == null)
                return;

            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Left,
                null, OnQuickSlotLeft, null);
            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Right,
                null, OnQuickSlotRight, null);
            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Up,
                null, OnQuickSlotUp, null);
            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Down,
                null, OnQuickSlotDown, null);
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (UISvc.Inventory != null)
                UISvc.Inventory.OnInventoryChanged += Refresh;
            UIQuickSlotAssignments.Changed += Refresh;
            Refresh();
        }

        protected override void OnHide()
        {
            if (UISvc.Inventory != null)
                UISvc.Inventory.OnInventoryChanged -= Refresh;
            UIQuickSlotAssignments.Changed -= Refresh;
        }

        protected override void OnDispose()
        {
            if (UISvc.Inventory != null)
                UISvc.Inventory.OnInventoryChanged -= Refresh;
            UIQuickSlotAssignments.Changed -= Refresh;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible)
                return;

            var inventory = UISvc.Inventory;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.RefreshCooldown(inventory);
        }

        public void Refresh()
        {
            var inventory = UISvc.Inventory;
            TryAssignStartingPotion(inventory);

            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.Refresh(inventory);
        }

        private static void TryAssignStartingPotion(IUIInventoryService inventory)
        {
            if (inventory == null
                || UIQuickSlotAssignments.GetItemId(StartingQuickSlotIndex) != 0)
                return;

            int itemId = (int)ItemIdType.저급_회복물약;
            if (inventory.GetItemCount(itemId) < StartingPotionCount)
                return;

            UIQuickSlotAssignments.AssignInitialIfEmpty(
                StartingQuickSlotIndex,
                inventory.GetItem(itemId)?.data);
        }

        public void TryUseSlot(int slotIndex)
        {
            if ((uint)slotIndex >= (uint)_slots.Count)
                return;

            _slots[slotIndex]?.TryUse();
        }

        private void OnQuickSlotLeft(InputAction.CallbackContext context) => TryUseSlot(3);
        private void OnQuickSlotRight(InputAction.CallbackContext context) => TryUseSlot(1);
        private void OnQuickSlotUp(InputAction.CallbackContext context) => TryUseSlot(0);
        private void OnQuickSlotDown(InputAction.CallbackContext context) => TryUseSlot(2);
    }
}
