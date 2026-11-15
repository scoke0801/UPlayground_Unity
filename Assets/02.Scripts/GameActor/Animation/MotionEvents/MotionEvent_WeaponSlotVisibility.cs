using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Data.Event
{
    /// <summary>투척한 주·보조 무기만 숨기고 실행 종료 시 자기 요청을 해제한다.</summary>
    [Serializable]
    [MotionEventDescriptor("WeaponSlotVisibility", "Utility", 0, "지정한 장착 슬롯을 구간 동안 숨기고 종료 시 복구합니다.", "weapon", "hide", "무기", "슬롯", "숨김")]
    public sealed class WeaponSlotVisibilityEvent : MotionEventBase
    {
        public WeaponVisibilitySlot slot;

        private struct ActiveRequest
        {
            public PlayerEquipment equipment;
            public int token;
        }

        [NonSerialized] private readonly Dictionary<int, ActiveRequest> _activeRequests = new(4);

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => "무기 슬롯 숨김";
        public override string GetShortLabel() => slot == WeaponVisibilitySlot.Main ? "주 무기 숨김" : "보조 무기 숨김";

        public override void Execute(GameObject target)
        {
            if (target == null || !PlayerEquipment.TryGetForMotionEvent(target, out PlayerEquipment equipment)) return;
            Release(target.GetInstanceID());
            _activeRequests[target.GetInstanceID()] = new ActiveRequest
            {
                equipment = equipment,
                token = equipment.HideWeaponSlot(slot)
            };
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (target != null) Release(target.GetInstanceID());
        }

        private void Release(int targetKey)
        {
            if (!_activeRequests.TryGetValue(targetKey, out ActiveRequest request)) return;
            _activeRequests.Remove(targetKey);
            if (request.equipment != null) request.equipment.RestoreWeaponSlot(request.token);
        }
    }
}
