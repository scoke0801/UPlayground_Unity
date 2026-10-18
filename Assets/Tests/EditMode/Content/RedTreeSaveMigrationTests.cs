using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.Save;

namespace UPlayGround.Content.Tests
{
    /// <summary>구 사건과 새 사건의 분리 및 재이관의 멱등성을 검증한다.</summary>
    public sealed class RedTreeSaveMigrationTests
    {
        [TestCase("Raon")]
        [TestCase("Arin")]
        [TestCase("Bokusei")]
        public void CopyMigration_PreservesPartyAndHistoryWithoutCompletingNewEvents(string protagonist)
        {
            var copy = CreateLegacySave(protagonist);
            PartySaveData party = copy.party;
            InventorySaveData inventory = copy.inventory;
            FlowProgressSaveData flow = copy.flow;
            Assert.That(RedTreeSaveMigration.TryApplyToCopy(copy, "test_map", "test_scene", out _), Is.True);
            Assert.That(copy.party, Is.SameAs(party));
            Assert.That(copy.inventory, Is.SameAs(inventory));
            Assert.That(copy.party.roster, Is.EqualTo(new[] { protagonist, "Honoka" }));
            Assert.That(copy.party.activeIndex, Is.EqualTo(1));
            Assert.That(copy.party.storyProtagonistType, Is.EqualTo(protagonist));
            Assert.That(copy.inventory.gold, Is.EqualTo(731));
            Assert.That(copy.flow, Is.SameAs(flow));
            Assert.That(copy.flow.firedKeys, Does.Contain("flow_lake_shrine_chapter1:entry"));
            Assert.That(copy.quest.completedQuestIds, Does.Contain("quest_main_lake_shrine"));
            Assert.That(copy.flags.flags["lake.story.alternate_self_defeated"], Is.True);
            Assert.That(copy.flags.flags.ContainsKey("lake.red_tree.hwarin_consent"), Is.False);
            Assert.That(copy.flags.flags.ContainsKey("lake.red_tree.lucia_defeated"), Is.False);
            Assert.That(copy.quest.activeQuests.Count, Is.EqualTo(1));
            Assert.That(copy.quest.activeQuests[0].questId, Is.EqualTo("quest_other_region"));
            Assert.That(copy.quest.trackedQuestId, Is.EqualTo(RedTreeSaveMigration.ArrivalQuestId));
            Assert.That(copy.party.hasLocation, Is.False);
        }

        [Test]
        public void RepeatedMigration_DoesNotResetNewProgressOrLocation()
        {
            var copy = CreateLegacySave("Arin");
            RedTreeSaveMigration.TryApplyToCopy(copy, "test_map", "test_scene", out _);
            copy.party.hasLocation = true;
            copy.party.mapId = "visited_map";
            copy.flags.flags["lake.red_tree.refugees_safe"] = true;
            copy.quest.trackedQuestId = "quest_main_red_tree_s02";
            Assert.That(RedTreeSaveMigration.TryApplyToCopy(copy, "test_map", "test_scene", out _), Is.True);
            Assert.That(copy.party.hasLocation, Is.True);
            Assert.That(copy.party.mapId, Is.EqualTo("visited_map"));
            Assert.That(copy.flags.flags["lake.red_tree.refugees_safe"], Is.True);
            Assert.That(copy.quest.trackedQuestId, Is.EqualTo("quest_main_red_tree_s02"));
        }

        [TestCase(null)]
        [TestCase("Hwarin")]
        [TestCase("999")]
        public void UnknownProtagonist_RejectsWithoutGuessingFromActiveMember(string protagonist)
        {
            var copy = CreateLegacySave(protagonist);
            Assert.That(RedTreeSaveMigration.TryApplyToCopy(copy, "test_map", "test_scene", out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(copy.quest.activeQuests.Count, Is.EqualTo(2));
            Assert.That(copy.party.hasLocation, Is.True);
            Assert.That(copy.flags.flags.ContainsKey(RedTreeSaveMigration.ContentVersionFlag), Is.False);
        }

        [Test]
        public void UnrelatedTrackingAndSuppression_ArePreserved()
        {
            var copy = CreateLegacySave("Raon");
            copy.quest.trackedQuestId = "quest_other_region";
            copy.quest.questTrackingSuppressed = true;
            Assert.That(RedTreeSaveMigration.TryApplyToCopy(copy, "test_map", "test_scene", out _), Is.True);
            Assert.That(copy.quest.trackedQuestId, Is.EqualTo("quest_other_region"));
            Assert.That(copy.quest.questTrackingSuppressed, Is.True);
        }

        private static GameSaveData CreateLegacySave(string protagonist)
        {
            var save = new GameSaveData();
            save.party.storyProtagonistType = protagonist;
            save.party.roster = new List<string> { protagonist, "Honoka" };
            save.party.battleOrder = new List<string>(save.party.roster);
            save.party.activeIndex = 1;
            save.party.hasLocation = true;
            save.inventory.gold = 731;
            save.quest.activeQuests.Add(new ActiveQuestSaveEntry { questId = "quest_sub_lake_rescue_honoka" });
            save.quest.activeQuests.Add(new ActiveQuestSaveEntry { questId = "quest_other_region" });
            save.quest.completedQuestIds.Add("quest_main_lake_shrine");
            save.quest.trackedQuestId = "quest_main_lake_shrine";
            save.flags.flags["lake.story.alternate_self_defeated"] = true;
            save.flow.firedKeys.Add("flow_lake_shrine_chapter1:entry");
            return save;
        }
    }
}
