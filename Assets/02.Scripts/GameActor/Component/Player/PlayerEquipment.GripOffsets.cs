using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace UPlayGround.Components
{
    public partial class PlayerEquipment
    {
        private struct GripOffsetRequest
        {
            public ParentConstraint constraint;
            public Vector3 offset;
            public Vector3 baseline;
        }

        private readonly Dictionary<int, GripOffsetRequest> _gripOffsetRequests = new(4);
        private int _gripOffsetToken;

        /// <summary>캐시한 장착 슬롯에 모션 전용 파지 위치를 적용하고 복구 토큰을 반환한다.</summary>
        public int OverrideWeaponGripOffset(WeaponVisibilitySlot slot, Vector3 offset)
        {
            if (slot != WeaponVisibilitySlot.Main && slot != WeaponVisibilitySlot.Sub) return 0;
            ParentConstraint constraint = slot == WeaponVisibilitySlot.Main ? _mainWeaponConstraint : _subWeaponConstraint;
            if (constraint == null || constraint.sourceCount == 0) return 0;
            Vector3 baseline = constraint.GetTranslationOffset(0);
            foreach (var entry in _gripOffsetRequests)
                if (entry.Value.constraint == constraint) { baseline = entry.Value.baseline; break; }

            int token = ++_gripOffsetToken;
            _gripOffsetRequests.Add(token, new GripOffsetRequest { constraint = constraint, offset = offset, baseline = baseline });
            constraint.SetTranslationOffset(0, offset);
            return token;
        }

        /// <summary>겹친 요청이 있으면 최신 값을 유지하고 마지막 요청 종료 시 원래 파지를 복구한다.</summary>
        public void RestoreWeaponGripOffset(int token)
        {
            if (!_gripOffsetRequests.TryGetValue(token, out GripOffsetRequest request)) return;
            _gripOffsetRequests.Remove(token);
            if (request.constraint == null || request.constraint.sourceCount == 0) return;
            int latest = 0;
            Vector3 offset = request.baseline;
            foreach (var entry in _gripOffsetRequests)
                if (entry.Value.constraint == request.constraint && entry.Key > latest)
                {
                    latest = entry.Key;
                    offset = entry.Value.offset;
                }
            request.constraint.SetTranslationOffset(0, offset);
        }

        private void RestoreWeaponGripOffsets()
        {
            foreach (var entry in _gripOffsetRequests)
                if (entry.Value.constraint != null && entry.Value.constraint.sourceCount > 0)
                    entry.Value.constraint.SetTranslationOffset(0, entry.Value.baseline);
            _gripOffsetRequests.Clear();
        }
    }
}
