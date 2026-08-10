using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Item
{
    public readonly struct ItemDropRollContext
    {
        public ItemDropRollContext(bool isCycleActive)
        {
            IsCycleActive = isCycleActive;
        }

        public bool IsCycleActive { get; }
    }

    public interface IItemDropRandom
    {
        double NextUnit();
        int RangeInclusive(int minimum, int maximum);
    }

    public sealed class SystemItemDropRandom : IItemDropRandom
    {
        private readonly Random _random;

        public SystemItemDropRandom()
            : this(new Random())
        {
        }

        public SystemItemDropRandom(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public double NextUnit() => _random.NextDouble();

        public int RangeInclusive(int minimum, int maximum)
        {
            if (minimum >= maximum)
                return minimum;

            // Random.Next의 상한은 exclusive라 +1이 필요하지만 int.MaxValue는 오버플로된다.
            // 이 경계에서는 long 폭과 NextDouble을 사용해 inclusive 계약을 유지한다.
            if (maximum == int.MaxValue)
            {
                long range = (long)maximum - minimum + 1L;
                return (int)(minimum + (long)(_random.NextDouble() * range));
            }

            return _random.Next(minimum, maximum + 1);
        }
    }

    /// <summary>
    /// 독립 확률 드랍과 상호 배타 가중치 드랍을 같은 규칙으로 계산한다.
    /// Unity 전역 난수에 의존하지 않아 시드 기반 테스트와 재현이 가능하다.
    /// </summary>
    public static class ItemDropResolver
    {
        public static List<ItemInstance> Resolve(
            IReadOnlyList<ItemDropList> independentDrops,
            IReadOnlyList<WeightedItemDropGroup> weightedGroups,
            IItemDropRandom random,
            ItemDropRollContext context)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var results = new List<ItemInstance>();
            ResolveIndependent(independentDrops, random, context, results);
            ResolveWeighted(weightedGroups, random, context, results);
            return results;
        }

        private static void ResolveIndependent(
            IReadOnlyList<ItemDropList> drops,
            IItemDropRandom random,
            ItemDropRollContext context,
            List<ItemInstance> results)
        {
            if (drops == null)
                return;

            for (int i = 0; i < drops.Count; i++)
            {
                ItemDropList drop = drops[i];
                if (drop?.itemData == null || !MatchesScope(drop.scope, context))
                    continue;

                float rate = Math.Max(0.0f, Math.Min(100.0f, drop.rate));
                if (rate <= 0.0f || (rate < 100.0f && random.NextUnit() * 100.0 >= rate))
                    continue;

                results.Add(CreateInstance(
                    drop.itemData,
                    drop.minimumDropCount,
                    drop.maximumDropCount,
                    random));
            }
        }

        private static void ResolveWeighted(
            IReadOnlyList<WeightedItemDropGroup> groups,
            IItemDropRandom random,
            ItemDropRollContext context,
            List<ItemInstance> results)
        {
            if (groups == null)
                return;

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                WeightedItemDropGroup group = groups[groupIndex];
                if (group?.entries == null || group.entries.Count == 0)
                    continue;

                int rolls = Math.Max(1, group.rolls);
                HashSet<ItemSO> selectedItems = group.allowDuplicateItems ? null : new HashSet<ItemSO>();

                for (int roll = 0; roll < rolls; roll++)
                {
                    double totalWeight = Math.Max(0.0f, group.noDropWeight);
                    for (int entryIndex = 0; entryIndex < group.entries.Count; entryIndex++)
                    {
                        WeightedItemDropEntry entry = group.entries[entryIndex];
                        if (IsEligible(entry, context, selectedItems))
                            totalWeight += Math.Max(0.0f, entry.weight);
                    }

                    if (totalWeight <= 0.0)
                        break;

                    double cursor = random.NextUnit() * totalWeight;
                    float noDropWeight = Math.Max(0.0f, group.noDropWeight);
                    if (cursor < noDropWeight)
                        continue;

                    cursor -= noDropWeight;
                    WeightedItemDropEntry selected = null;
                    for (int entryIndex = 0; entryIndex < group.entries.Count; entryIndex++)
                    {
                        WeightedItemDropEntry entry = group.entries[entryIndex];
                        if (!IsEligible(entry, context, selectedItems))
                            continue;

                        float weight = Math.Max(0.0f, entry.weight);
                        if (cursor < weight)
                        {
                            selected = entry;
                            break;
                        }

                        cursor -= weight;
                    }

                    if (selected == null)
                        continue;

                    results.Add(CreateInstance(
                        selected.itemData,
                        selected.minimumDropCount,
                        selected.maximumDropCount,
                        random));
                    selectedItems?.Add(selected.itemData);
                }
            }
        }

        private static bool IsEligible(
            WeightedItemDropEntry entry,
            ItemDropRollContext context,
            HashSet<ItemSO> selectedItems)
        {
            return entry?.itemData != null &&
                   entry.weight > 0.0f &&
                   MatchesScope(entry.scope, context) &&
                   (selectedItems == null || !selectedItems.Contains(entry.itemData));
        }

        private static bool MatchesScope(ItemDropScope scope, ItemDropRollContext context) =>
            scope switch
            {
                ItemDropScope.ActiveCycleOnly => context.IsCycleActive,
                ItemDropScope.OutsideCycleOnly => !context.IsCycleActive,
                _ => true,
            };

        private static ItemInstance CreateInstance(
            ItemSO itemData,
            int minimumDropCount,
            int maximumDropCount,
            IItemDropRandom random)
        {
            int minimum = Math.Max(1, minimumDropCount);
            int maximum = Math.Max(minimum, maximumDropCount);
            return new ItemInstance
            {
                data = itemData,
                count = random.RangeInclusive(minimum, maximum),
            };
        }
    }
}
