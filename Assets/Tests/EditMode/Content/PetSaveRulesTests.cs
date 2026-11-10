using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Save;
using System.Reflection;

namespace UPlayGround.Content.Tests
{
    public sealed class PetSaveRulesTests
    {
        [Test]
        public void Version34MigrationPreservesLegacyPetAbsenceAndCreatesDropContainer()
        {
            var data = new GameSaveData { saveVersion = "3.4", pet = null, groundDrops = null };
            var type = System.Type.GetType("UPlayGround.Manager.SaveManager, Assembly-CSharp", true);
            type.GetMethod("MigrateToCurrentVersion", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { data });
            Assert.That(data.saveVersion, Is.EqualTo("3.5"));
            Assert.That(data.pet, Is.Null);
            Assert.That(data.groundDrops, Is.Not.Null.And.Empty);
        }
        [Test]
        public void LegacySaveHasNoImplicitPetGrant()
        {
            var result = PetSaveRules.Normalize(null);
            Assert.That(result.owned, Is.Empty);
            Assert.That(result.isAccompanying, Is.False);
        }
        [Test]
        public void DuplicateEntriesMergeUnlocksWithoutDuplicatingBond()
        {
            var result = PetSaveRules.Normalize(new PetSaveData
            {
                selectedPetId = "Unknown.Pet", isAccompanying = true,
                owned = new List<PetOwnedEntry>
                {
                    new() { petId = "Unknown.Pet", bondPoints = -8, unlockedAppearances = new() { "A" } },
                    new() { petId = "Unknown.Pet", bondPoints = 10, unlockedAppearances = new() { "A", "B" } }
                }
            });
            Assert.That(result.owned.Count, Is.EqualTo(1));
            Assert.That(result.owned[0].bondPoints, Is.EqualTo(10));
            Assert.That(result.owned[0].unlockedAppearances, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(result.selectedPetId, Is.EqualTo("Unknown.Pet"));
            Assert.That(result.isAccompanying, Is.True);
        }
        [Test]
        public void UnownedSelectionIsClearedAndSnapshotIsDetached()
        {
            var source = new PetSaveData { selectedPetId = "absent", isAccompanying = true,
                owned = new() { new() { petId = "Pet.Monkey", bondPoints = 5 } } };
            var result = PetSaveRules.Normalize(source);
            Assert.That(result.selectedPetId, Is.Null);
            Assert.That(result.isAccompanying, Is.False);
            result.owned[0].bondPoints = 0;
            Assert.That(source.owned[0].bondPoints, Is.EqualTo(5));
        }
    }
}
