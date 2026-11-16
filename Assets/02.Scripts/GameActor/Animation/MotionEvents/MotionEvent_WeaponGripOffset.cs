using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Data.Event
{
    /// <summary>클립에 잔류하는 Constraint 커브 대신 실행 구간에만 파지 보정을 적용한다.</summary>
    [Serializable]
    [MotionEventDescriptor("WeaponGripOffset", "Utility", 0, "무기 파지 위치를 구간 동안 보정하고 중단·종료 시 복구합니다.", "weapon", "grip", "파지")]
    public sealed class WeaponGripOffsetEvent : MotionEventBase
    {
        public WeaponVisibilitySlot slot;
        public Vector3 translationOffset;

        private struct ActiveRequest
        {
            public PlayerEquipment equipment;
            public int token;
        }

        [NonSerialized] private readonly Dictionary<int, ActiveRequest> _requests = new(4);
        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => MotionEventEnemyExecutionPolicy.Ignored;
        public override string GetDisplayName() => "무기 파지 보정";

        public override void Execute(GameObject target)
        {
            if (target == null || !PlayerEquipment.TryGetForMotionEvent(target, out PlayerEquipment equipment)) return;
            int key = target.GetInstanceID();
            Release(key);
            _requests[key] = new ActiveRequest { equipment = equipment, token = equipment.OverrideWeaponGripOffset(slot, translationOffset) };
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (target != null) Release(target.GetInstanceID());
        }

        private void Release(int key)
        {
            if (!_requests.TryGetValue(key, out ActiveRequest request)) return;
            _requests.Remove(key);
            if (request.equipment != null) request.equipment.RestoreWeaponGripOffset(request.token);
        }
    }
}
