using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround.Item.Tests
{
    public class ItemDropResolverTests
    {
        private readonly List<ItemSO> _createdItems = new();

        [TearDown]
        public void TearDown()
        {
            foreach (ItemSO item in _createdItems)
                Object.DestroyImmediate(item);
            _createdItems.Clear();
        }

        [Test]
        public void 독립드랍_0퍼센트는제외하고_100퍼센트는항상포함한다()
        {
            ItemSO zero = CreateItem(1);
            ItemSO guaranteed = CreateItem(2);
            var drops = new List<ItemDropList>
            {
                new() { itemData = zero, rate = 0f },
                new() { itemData = guaranteed, rate = 100f },
            };

            List<ItemInstance> result = ItemDropResolver.Resolve(
                drops,
                null,
                new FakeRandom(),
                new ItemDropRollContext(false));

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].data, Is.SameAs(guaranteed));
        }

        [Test]
        public void 독립드랍_최대수량을포함해서추첨한다()
        {
            ItemSO item = CreateItem(1);
            var drops = new List<ItemDropList>
            {
                new()
                {
                    itemData = item,
                    rate = 100f,
                    minimumDropCount = 2,
                    maximumDropCount = 4,
                },
            };

            List<ItemInstance> result = ItemDropResolver.Resolve(
                drops,
                null,
                new FakeRandom(returnMaximum: true),
                new ItemDropRollContext(false));

            Assert.That(result[0].count, Is.EqualTo(4));
        }

        [Test]
        public void 시스템난수_정수최대값도포함범위로안전하게추첨한다()
        {
            var random = new SystemItemDropRandom(new System.Random(1234));

            int result = random.RangeInclusive(1, int.MaxValue);

            Assert.That(result, Is.InRange(1, int.MaxValue));
        }

        [Test]
        public void 적용범위에따라_사이클전용과외부전용을구분한다()
        {
            ItemSO cycleOnly = CreateItem(1);
            ItemSO outsideOnly = CreateItem(2);
            var drops = new List<ItemDropList>
            {
                new() { itemData = cycleOnly, rate = 100f, scope = ItemDropScope.ActiveCycleOnly },
                new() { itemData = outsideOnly, rate = 100f, scope = ItemDropScope.OutsideCycleOnly },
            };

            List<ItemInstance> activeResult = ItemDropResolver.Resolve(
                drops, null, new FakeRandom(), new ItemDropRollContext(true));
            List<ItemInstance> outsideResult = ItemDropResolver.Resolve(
                drops, null, new FakeRandom(), new ItemDropRollContext(false));

            Assert.That(activeResult, Has.Count.EqualTo(1));
            Assert.That(activeResult[0].data, Is.SameAs(cycleOnly));
            Assert.That(outsideResult, Has.Count.EqualTo(1));
            Assert.That(outsideResult[0].data, Is.SameAs(outsideOnly));
        }

        [Test]
        public void 가중그룹_중복비허용이면각후보를한번만선택한다()
        {
            ItemSO first = CreateItem(1);
            ItemSO second = CreateItem(2);
            var groups = new List<WeightedItemDropGroup>
            {
                new()
                {
                    groupId = "equipment",
                    rolls = 5,
                    allowDuplicateItems = false,
                    entries = new List<WeightedItemDropEntry>
                    {
                        new() { itemData = first, weight = 1f },
                        new() { itemData = second, weight = 1f },
                    },
                },
            };

            List<ItemInstance> result = ItemDropResolver.Resolve(
                null,
                groups,
                new FakeRandom(0.0, 0.0, 0.0),
                new ItemDropRollContext(false));

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].data, Is.Not.SameAs(result[1].data));
        }

        [Test]
        public void 가중그룹_미드랍가중치영역이면아이템을주지않는다()
        {
            ItemSO item = CreateItem(1);
            var groups = new List<WeightedItemDropGroup>
            {
                new()
                {
                    groupId = "equipment",
                    noDropWeight = 9f,
                    entries = new List<WeightedItemDropEntry>
                    {
                        new() { itemData = item, weight = 1f },
                    },
                },
            };

            List<ItemInstance> result = ItemDropResolver.Resolve(
                null,
                groups,
                new FakeRandom(0.5),
                new ItemDropRollContext(false));

            Assert.That(result, Is.Empty);
        }

        private ItemSO CreateItem(int id)
        {
            ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
            item.itemId = id;
            _createdItems.Add(item);
            return item;
        }

        private sealed class FakeRandom : IItemDropRandom
        {
            private readonly Queue<double> _values;
            private readonly bool _returnMaximum;

            public FakeRandom(params double[] values)
                : this(false, values)
            {
            }

            public FakeRandom(bool returnMaximum, params double[] values)
            {
                _returnMaximum = returnMaximum;
                _values = new Queue<double>(values);
            }

            public double NextUnit() => _values.Count > 0 ? _values.Dequeue() : 0.0;

            public int RangeInclusive(int minimum, int maximum) =>
                _returnMaximum ? maximum : minimum;
        }
    }
}
