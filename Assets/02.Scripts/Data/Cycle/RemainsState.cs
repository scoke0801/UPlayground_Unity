using System;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Save;

namespace UPlayGround.Data.Cycle
{
    [Serializable]
    public sealed class CycleItemStack
    {
        public int itemId;
        public int count;
        public CycleItemStack Clone() => new() { itemId = itemId, count = count };
    }

    [Serializable]
    public sealed class LostExpEntry
    {
        public CharacterActorType characterType;
        public long amount;
        public LostExpEntry Clone() => new() { characterType = characterType, amount = amount };
    }

    [Serializable]
    public sealed class RemainsState
    {
        public string remainsId;
        public string mapId;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public List<LostExpEntry> lostExp = new();
        public List<CycleItemStack> materials = new();
        public bool recovered;

        public RemainsState Clone()
        {
            RemainsState clone = new()
            {
                remainsId = remainsId,
                mapId = mapId,
                position = position,
                rotation = rotation,
                recovered = recovered,
            };
            if (lostExp != null)
                foreach (LostExpEntry entry in lostExp) if (entry != null) clone.lostExp.Add(entry.Clone());
            if (materials != null)
                foreach (CycleItemStack item in materials) if (item != null) clone.materials.Add(item.Clone());
            return clone;
        }
    }

    [Serializable]
    public sealed class CycleLootLedger
    {
        public List<CycleItemStack> unsettledMaterials = new();
        public bool IsDirty { get; private set; }

        public void Add(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0) return;
            CycleItemStack stack = unsettledMaterials.Find(value => value.itemId == itemId);
            if (stack == null) { stack = new CycleItemStack { itemId = itemId }; unsettledMaterials.Add(stack); }
            stack.count += count;
            IsDirty = true;
        }

        public List<CycleItemStack> Snapshot()
        {
            List<CycleItemStack> result = new();
            foreach (CycleItemStack item in unsettledMaterials) if (item != null && item.count > 0) result.Add(item.Clone());
            return result;
        }

        public void Clear() { unsettledMaterials.Clear(); IsDirty = true; }
        public void MarkSaved() => IsDirty = false;
        public void Restore(IEnumerable<CycleItemStack> items)
        {
            unsettledMaterials.Clear();
            if (items != null) foreach (CycleItemStack item in items) if (item != null && item.count > 0) Add(item.itemId, item.count);
            IsDirty = false;
        }
    }
}
