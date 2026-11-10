using System;
using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.Item;
using UPlayGround.Data.Save;

namespace UPlayGround.Item.Tests
{
    public sealed class GroundDropLedgerTests
    {
        private static GroundDropRecord Record() => new()
        {
            dropId = "drop", sourceId = "source", mapId = "map",
            item = new ItemSaveEntry
            {
                itemId = 3, count = 1, enhancementLevel = 5,
                growthAttributeRolls = new List<EquipmentGrowthAttributeRoll>
                { new() { attributeId = "Attack", rank = 4 } }
            }
        };
        private static GroundDropLedger Available()
        { var ledger = new GroundDropLedger(); ledger.Register(Record()); ledger.MarkAvailable("drop"); return ledger; }

        [Test]
        public void SpawnDoesNotGrantAndPendingCannotBeCollected()
        {
            var ledger = new GroundDropLedger(); ledger.Register(Record());
            Assert.That(ledger.TryCollect("drop", _ => throw new Exception()), Is.False);
            Assert.That(ledger.Export()[0].collected, Is.False);
        }
        [Test]
        public void ReentrantAndDuplicateCollectionGrantOnlyOnce()
        {
            var ledger = Available(); int grants = 0;
            Assert.That(ledger.TryCollect("drop", _ =>
            {
                grants++;
                Assert.That(ledger.TryCollect("drop", _ => { grants++; return true; }), Is.False);
                Assert.Throws<InvalidOperationException>(() => ledger.Export());
                return true;
            }), Is.True);
            Assert.That(ledger.TryCollect("drop", _ => { grants++; return true; }), Is.False);
            Assert.That(grants, Is.EqualTo(1));
            Assert.That(ledger.CanSave, Is.True);
        }
        [Test]
        public void InventoryFailureLeavesDropAvailable()
        {
            var ledger = Available();
            Assert.That(ledger.TryCollect("drop", _ => false), Is.False);
            Assert.That(ledger.Export()[0].collected, Is.False);
            Assert.That(ledger.TryCollect("drop", _ => true), Is.True);
        }
        [Test]
        public void ExceptionReleasesCommitAndKeepsDrop()
        {
            var ledger = Available();
            Assert.Throws<InvalidOperationException>(() => ledger.TryCollect("drop", _ => throw new InvalidOperationException()));
            Assert.That(ledger.CanSave, Is.True);
            Assert.That(ledger.TryCollect("drop", _ => true), Is.True);
        }
        [Test]
        public void PlayerCanCollectReservedDropBeforePet()
        {
            var ledger = Available(); var pet = new object();
            Assert.That(ledger.TryReserve("drop", pet, 1f, 3f), Is.True);
            Assert.That(ledger.TryCollect("drop", _ => true), Is.True);
            Assert.That(ledger.TryCollect("drop", _ => true, pet, 2f), Is.False);
        }
        [Test]
        public void ExpiredAndForeignReservationCannotCollect()
        {
            var ledger = Available(); var first = new object(); var second = new object();
            ledger.TryReserve("drop", first, 1f, 3f);
            Assert.That(ledger.TryReserve("drop", second, 2f, 3f), Is.False);
            Assert.That(ledger.TryCollect("drop", _ => true, second, 2f), Is.False);
            Assert.That(ledger.TryCollect("drop", _ => true, first, 4f), Is.False);
            Assert.That(ledger.TryReserve("drop", second, 4f, 3f), Is.True);
            ledger.Release("drop", first);
            Assert.That(ledger.TryCollect("drop", _ => true, second, 5f), Is.True);
        }
        [Test]
        public void SaveRoundTripPreservesOptionsAndDropsReservation()
        {
            var ledger = Available(); var pet = new object(); ledger.TryReserve("drop", pet, 0f, 10f);
            var save = ledger.Export(); var restored = new GroundDropLedger(); restored.Import(save);
            save[0].item.growthAttributeRolls.Clear();
            Assert.That(restored.Export()[0].item.enhancementLevel, Is.EqualTo(5));
            Assert.That(restored.Export()[0].item.growthAttributeRolls[0].rank, Is.EqualTo(4));
            Assert.That(restored.TryCollect("drop", _ => true), Is.False);
            restored.MarkAvailable("drop");
            Assert.That(restored.TryCollect("drop", _ => true, pet, 1f), Is.False);
            Assert.That(restored.TryReserve("drop", new object(), 1f, 2f), Is.True);
        }
        [Test]
        public void CollectedRecordCannotBeResurrectedByRegistrationOrLoad()
        {
            var ledger = Available(); ledger.TryCollect("drop", _ => true);
            Assert.That(ledger.Register(Record()), Is.False);
            var restored = new GroundDropLedger(); restored.Import(ledger.Export());
            Assert.That(restored.MarkAvailable("drop"), Is.False);
            Assert.That(restored.TryCollect("drop", _ => true), Is.False);
        }
    }
}
