using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Save;
using System.Reflection;

namespace UPlayGround.Content.Tests
{
    public sealed class PetSaveRulesTests
    {
        [TestCase("Pet.Skill.AttackBuff")]
        [TestCase("Future.Support")]
        public void 버전2_선택은_슬롯1로_이전하고_반복_정규화에도_보존한다(string id)
        {
            var source = new PetSaveData { progressionVersion = 2, selectedSupportId = id,
                supportCooldownRemaining = 12f };
            var result = PetSaveRules.Normalize(source);
            Assert.That(result.progressionVersion, Is.EqualTo(3));
            Assert.That(result.supportSlots, Is.EqualTo(new[] { id }));
            Assert.That(result.supportCooldownRemaining, Is.EqualTo(12f));
            result = PetSaveRules.Normalize(result);
            Assert.That(result.SelectedSupportId, Is.EqualTo(id));
            Assert.That(source.supportSlots, Is.Empty);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void 버전3_필드와_알수없는_쿨다운은_독립_사본과_JSON_왕복으로_보존한다(int version)
        {
            var source = new PetSaveData { progressionVersion = version, selectedSupportId = PetSkillCatalogSO.Heal,
                supportSlots = new() { "Future.Support" }, supportMinimumIntervalRemaining = 5f,
                supportCooldowns = new() { new() { groupId = "Future.Group", remainingSeconds = 19f } } };
            var json = UnityEngine.JsonUtility.ToJson(source);
            var result = PetSaveRules.Normalize(UnityEngine.JsonUtility.FromJson<PetSaveData>(json));
            Assert.That(result.SelectedSupportId, Is.EqualTo("Future.Support"));
            Assert.That(result.supportMinimumIntervalRemaining, Is.EqualTo(5f));
            Assert.That(result.supportCooldowns[0].remainingSeconds, Is.EqualTo(19f));
            result.supportCooldowns[0].remainingSeconds = 0f; result.supportSlots[0] = "Changed";
            Assert.That(source.supportSlots[0], Is.EqualTo("Future.Support"));
            Assert.That(source.supportCooldowns[0].remainingSeconds, Is.EqualTo(19f));
        }

        [Test]
        public void 버전1의_선택ID도_잃지_않고_이전한다()
        {
            var source = new PetSaveData { progressionVersion = 1, selectedSupportId = "Future.Support" };
            Assert.That(PetSaveRules.Normalize(source).SelectedSupportId, Is.EqualTo("Future.Support"));
        }
        [Test]
        public void Version34MigrationPreservesLegacyPetAbsenceAndCreatesDropContainer()
        {
            var data = new GameSaveData { saveVersion = "3.4", pet = null, groundDrops = null };
            var type = System.Type.GetType("UPlayGround.Manager.SaveManager, Assembly-CSharp", true);
            type.GetMethod("MigrateToCurrentVersion", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { data });
            Assert.That(data.saveVersion, Is.EqualTo("3.6"));
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
