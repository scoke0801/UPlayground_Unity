using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>원본 스킨과 본 연결을 유지하며 슬롯별 장비 외형을 교체한다.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterEquipmentVisuals : MonoBehaviour
    {
        [Serializable]
        public sealed class Variant
        {
            public string id;
            public Renderer[] renderers = Array.Empty<Renderer>();
        }

        [Serializable]
        public sealed class Slot
        {
            public string id;
            public Variant[] variants = Array.Empty<Variant>();
            public int selectedIndex;
        }

        [SerializeField] private Slot[] _slots = Array.Empty<Slot>();
        public IReadOnlyList<Slot> Slots => _slots;

        /// <summary>저작된 연결을 모두 검사한 뒤 초기 장비를 적용한다.</summary>
        public void Configure(Slot[] slots)
        {
            ValidateSlots(slots);
            _slots = slots;
            foreach (var slot in _slots) Apply(slot, slot.selectedIndex);
        }

        /// <summary>유효한 슬롯과 외형일 때만 장비를 변경한다. 실패 시 현재 외형을 보존한다.</summary>
        public bool TryEquip(string slotId, string variantId)
        {
            foreach (var slot in _slots)
            {
                if (slot == null || slot.id != slotId) continue;
                for (int i = 0; i < slot.variants.Length; i++)
                {
                    if (slot.variants[i].id != variantId) continue;
                    foreach (var variant in slot.variants)
                        foreach (var renderer in variant.renderers)
                            if (renderer == null) return false;
                    Apply(slot, i);
                    return true;
                }
                return false;
            }
            return false;
        }

        private static void Apply(Slot slot, int index)
        {
            // 본이나 자식 장비까지 꺼지는 SetActive 대신 렌더러만 전환한다.
            foreach (var variant in slot.variants)
                foreach (var renderer in variant.renderers) renderer.enabled = false;
            foreach (var renderer in slot.variants[index].renderers) renderer.enabled = true;
            slot.selectedIndex = index;
        }

        /// <summary>중복 ID, 다른 슬롯의 렌더러 공유, 누락 연결을 거부한다.</summary>
        public static void ValidateSlots(Slot[] slots)
        {
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var owners = new Dictionary<Renderer, string>();
            foreach (var slot in slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.id) || !ids.Add(slot.id))
                    throw new InvalidOperationException("장비 슬롯 ID가 비어 있거나 중복됩니다.");
                if (slot.variants == null || slot.variants.Length == 0 ||
                    slot.selectedIndex < 0 || slot.selectedIndex >= slot.variants.Length)
                    throw new InvalidOperationException($"장비 기본 선택이 잘못되었습니다: {slot.id}");
                var variants = new HashSet<string>(StringComparer.Ordinal);
                foreach (var variant in slot.variants)
                {
                    if (variant == null || string.IsNullOrWhiteSpace(variant.id) || !variants.Add(variant.id) || variant.renderers == null)
                        throw new InvalidOperationException($"장비 외형 ID 또는 연결이 잘못되었습니다: {slot.id}");
                    foreach (var renderer in variant.renderers)
                    {
                        if (renderer == null) throw new InvalidOperationException($"장비 렌더러 누락: {slot.id}/{variant.id}");
                        if (owners.TryGetValue(renderer, out var owner) && owner != slot.id)
                            throw new InvalidOperationException($"다른 슬롯이 같은 렌더러를 제어합니다: {owner}, {slot.id}");
                        owners[renderer] = slot.id;
                    }
                }
            }
        }
    }
}
