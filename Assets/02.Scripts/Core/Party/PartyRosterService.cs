using System;
using System.Collections.Generic;

namespace UPlayGround.Core.Party
{
    /// <summary>
    /// Unity 객체와 무관한 파티 보유/출전 편성 규칙을 소유한다.
    /// </summary>
    public sealed class PartyRosterService<T>
        where T : struct, Enum
    {
        private readonly List<T> _roster = new();
        private readonly List<T> _battleOrder = new();

        public IReadOnlyList<T> Roster => _roster;
        public IReadOnlyList<T> BattleOrder => _battleOrder;
        public List<T> MutableRoster => _roster;
        public List<T> MutableBattleOrder => _battleOrder;

        public void Clear()
        {
            _roster.Clear();
            _battleOrder.Clear();
        }

        public bool AddToRoster(T type)
        {
            if (_roster.Contains(type))
                return false;

            _roster.Add(type);
            return true;
        }

        public bool AddToBattle(T type, int maxBattleSize)
        {
            if (!_roster.Contains(type) ||
                _battleOrder.Contains(type) ||
                _battleOrder.Count >= Math.Max(1, maxBattleSize))
            {
                return false;
            }

            _battleOrder.Add(type);
            return true;
        }

        public bool RemoveFromBattle(T type, out int removedIndex)
        {
            removedIndex = _battleOrder.IndexOf(type);
            if (removedIndex < 0)
                return false;

            _battleOrder.RemoveAt(removedIndex);
            return true;
        }

        public bool ReplaceBattleSlot(int slotIndex, T type, out int existingIndex)
        {
            existingIndex = _battleOrder.IndexOf(type);
            if (slotIndex < 0 ||
                slotIndex >= _battleOrder.Count ||
                !_roster.Contains(type) ||
                EqualityComparer<T>.Default.Equals(_battleOrder[slotIndex], type))
            {
                return false;
            }

            if (existingIndex >= 0)
            {
                T previous = _battleOrder[slotIndex];
                _battleOrder[existingIndex] = previous;
            }

            _battleOrder[slotIndex] = type;
            return true;
        }

        public bool SetBattleOrder(IReadOnlyList<T> requestedOrder, int maxBattleSize)
        {
            if (requestedOrder == null || requestedOrder.Count == 0)
                return false;

            var validated = new List<T>();
            int limit = Math.Max(1, maxBattleSize);
            for (int i = 0; i < requestedOrder.Count && validated.Count < limit; i++)
            {
                T type = requestedOrder[i];
                if (!_roster.Contains(type) || validated.Contains(type))
                    continue;

                validated.Add(type);
            }

            if (validated.Count == 0)
                return false;

            _battleOrder.Clear();
            _battleOrder.AddRange(validated);
            return true;
        }
    }
}
