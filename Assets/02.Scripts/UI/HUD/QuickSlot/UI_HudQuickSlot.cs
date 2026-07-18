using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public void Refresh()
        {
            var inventory = UISvc.Inventory;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.Refresh(inventory);
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
