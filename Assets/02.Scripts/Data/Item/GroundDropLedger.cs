using System;
using System.Collections.Generic;
using UPlayGround.Data.Save;

namespace UPlayGround.Data.Item
{
    public enum GroundDropState { PendingSpawn, Available, Committing, Collected }

    /// <summary>월드 드롭의 확정 아이템과 원본 보상 식별자를 보존한다.</summary>
    [Serializable]
    public sealed class GroundDropRecord
    {
        public string dropId;
        public string sourceId;
        public string mapId;
        public SerializableVector3 position;
        public ItemSaveEntry item;
        public bool collected;
    }

    /// <summary>지급 재진입을 막고 예약과 획득 확정을 별도로 관리한다.</summary>
    public sealed class GroundDropLedger
    {
        private sealed class Entry
        {
            public GroundDropRecord Record;
            public GroundDropState State;
            public object ReservationOwner;
            public float ReservationEnd;
        }

        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
        private int _committingCount;
        public bool CanSave => _committingCount == 0;

        /// <summary>월드의 명시적 재생성 시 이전 배치 수명을 종료한다.</summary>
        public bool Remove(string id) => CanSave && id != null && _entries.Remove(id);

        /// <summary>동일 dropId는 상태를 덮어쓰지 않는다.</summary>
        public bool Register(GroundDropRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.dropId)
                || record.item == null || record.item.count <= 0 || _entries.ContainsKey(record.dropId))
                return false;
            _entries.Add(record.dropId, new Entry
            {
                Record = Clone(record),
                State = record.collected ? GroundDropState.Collected : GroundDropState.PendingSpawn
            });
            return true;
        }

        /// <summary>월드 표현이 준비된 드롭만 획득 가능 상태로 바꾼다.</summary>
        public bool MarkAvailable(string id)
        {
            if (!_entries.TryGetValue(id, out var entry) || entry.State == GroundDropState.Collected
                || entry.State == GroundDropState.Committing) return false;
            entry.State = GroundDropState.Available;
            return true;
        }

        /// <summary>펫 예약은 클릭 획득을 막지 않으며 만료되면 다른 예약자가 얻을 수 있다.</summary>
        public bool TryReserve(string id, object owner, float now, float duration)
        {
            if (owner == null || duration <= 0f || !_entries.TryGetValue(id, out var entry)
                || entry.State != GroundDropState.Available) return false;
            if (entry.ReservationOwner != null && !ReferenceEquals(owner, entry.ReservationOwner)
                && entry.ReservationEnd > now) return false;
            entry.ReservationOwner = owner;
            entry.ReservationEnd = now + duration;
            return true;
        }

        /// <summary>예약 소유자만 자신의 예약을 해제한다.</summary>
        public void Release(string id, object owner)
        {
            if (id != null && _entries.TryGetValue(id, out var entry)
                && ReferenceEquals(entry.ReservationOwner, owner)) entry.ReservationOwner = null;
        }

        /// <summary>지급 실패는 Available로 복원하고 중첩 획득은 거절한다.</summary>
        public bool TryCollect(string id, Func<ItemSaveEntry, bool> store, object owner = null, float now = 0f)
        {
            if (id == null || store == null || !_entries.TryGetValue(id, out var entry)
                || entry.State != GroundDropState.Available) return false;
            if (owner != null && (!ReferenceEquals(entry.ReservationOwner, owner)
                || entry.ReservationEnd <= now)) return false;
            entry.State = GroundDropState.Committing;
            _committingCount++;
            try
            {
                if (!store(CloneItem(entry.Record.item))) return false;
                entry.Record.collected = true;
                entry.State = GroundDropState.Collected;
                entry.ReservationOwner = null;
                return true;
            }
            finally
            {
                _committingCount--;
                if (entry.State == GroundDropState.Committing) entry.State = GroundDropState.Available;
            }
        }

        /// <summary>저장에는 임시 예약과 중간 지급 상태를 포함하지 않는다.</summary>
        public List<GroundDropRecord> Export()
        {
            if (!CanSave) throw new InvalidOperationException("아이템 획득 확정 중에는 저장할 수 없습니다.");
            var records = new List<GroundDropRecord>(_entries.Count);
            foreach (var entry in _entries.Values) records.Add(Clone(entry.Record));
            return records;
        }

        /// <summary>로드 뒤에는 월드 생성과 예약을 새로 준비한다.</summary>
        public void Import(IEnumerable<GroundDropRecord> records)
        {
            if (!CanSave) throw new InvalidOperationException("아이템 획득 확정 중에는 로드할 수 없습니다.");
            _entries.Clear();
            if (records != null) foreach (var record in records) Register(record);
        }

        /// <summary>지급·저장 경계에서 장비 옵션 목록까지 독립 사본으로 만든다.</summary>
        public static ItemSaveEntry CloneItem(ItemSaveEntry item) => new()
        {
            itemId = item.itemId, count = item.count, slotKey = item.slotKey,
            enhancementLevel = item.enhancementLevel,
            growthAttributeRolls = item.growthAttributeRolls != null
                ? new List<EquipmentGrowthAttributeRoll>(item.growthAttributeRolls) : new()
        };

        private static GroundDropRecord Clone(GroundDropRecord record) => new()
        {
            dropId = record.dropId, sourceId = record.sourceId, mapId = record.mapId,
            position = record.position, item = CloneItem(record.item), collected = record.collected
        };
    }
}
