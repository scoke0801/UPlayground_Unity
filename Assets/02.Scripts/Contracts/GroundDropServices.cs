using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager
{
    /// <summary>UI와 펫이 공유하는 바닥 아이템의 읽기 전용 표현.</summary>
    public readonly struct GroundDropView
    {
        public readonly string Id;
        public readonly Transform Transform;
        public readonly ItemSO Item;
        public readonly int Count;
        public GroundDropView(string id, Transform transform, ItemSO item, int count)
        { Id = id; Transform = transform; Item = item; Count = count; }
    }

    /// <summary>바닥 보상 생성·예약·획득의 단일 진입점.</summary>
    public interface IGroundDropService : IGameService
    {
        event System.Action<string> Collected;
        GroundDropSettingsSO Settings { get; }
        IReadOnlyList<GroundDropView> VisibleDrops { get; }
        void SpawnItems(IReadOnlyList<ItemInstance> items, Vector3 position, string sourceId = null, Transform source = null);
        bool CanPlayerCollect(string dropId);
        bool TryPlayerCollect(string dropId);
        bool TryReserve(string dropId, object owner, float duration);
        void Release(string dropId, object owner);
        bool TryPetCollect(string dropId, object owner, Vector3 collectorPosition, float arrivalDistance);
        string RegisterPlaced(string sourceId, ItemInstance item, Transform representation);
        void ResetPlaced(string dropId);
    }

    /// <summary>확정된 장비 옵션을 재추첨하지 않는 드롭 전용 인벤토리 계약.</summary>
    public interface IResolvedItemInventoryService : IGameService
    {
        List<ItemSaveEntry> ResolveDropItems(IReadOnlyList<ItemInstance> items);
        bool TryStoreResolvedItem(ItemSaveEntry item);
        void NotifyResolvedItemAcquired(ItemSaveEntry item);
    }
}
