using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Save;
using UPlayGround.Data.Story;

namespace UPlayGround.Content.Tests
{
    public sealed class LianPersistentIdMigrationTests
    {
        [Test]
        public void Migrate_구명칭으로_저장된_진행도를_현재_ID로_통합한다()
        {
            var data = new GameSaveData
            {
                monsterCodex = new List<MonsterCodexEntrySave>
                {
                    new() { actorId = "MonsterLianLian" },
                },
                flags = new FlagSaveData
                {
                    flags = new Dictionary<string, bool>
                    {
                        ["lake.story.lianlian_joined"] = true,
                        ["lake.story.lian_joined"] = false,
                    },
                },
                recipe = new RecipeSaveData
                {
                    monsterKillsByActorId = new Dictionary<string, int>
                    {
                        ["MonsterLianLian"] = 2,
                        ["MonsterLian"] = 3,
                    },
                },
                quest = new QuestSaveData
                {
                    completedQuestIds = new List<string>
                    {
                        "quest_sub_lake_rescue_lianlian",
                        "quest_sub_lake_rescue_lian",
                    },
                    trackedQuestId = "quest_sub_lake_rescue_lianlian",
                    activeQuests = new List<ActiveQuestSaveEntry>
                    {
                        new()
                        {
                            questId = "quest_sub_lake_rescue_lianlian",
                            objectiveProgress = new Dictionary<string, int>
                            {
                                ["obj_help_lianlian"] = 1,
                            },
                        },
                    },
                },
                story = new StorySaveData
                {
                    recruitmentEncounters = new List<RecruitmentEncounterSaveEntry>
                    {
                        new()
                        {
                            encounterId = "test.combat.lianlian_rescue",
                            recruitCharacter = CharacterActorType.Lian,
                            defeatedHostileIds = new List<string> { "lianlian_ally" },
                        },
                    },
                },
                party = new PartySaveData
                {
                    roster = new List<string> { "LianLian", "Lian" },
                    storyProtagonistType = "LianLian",
                    skillProgress = new List<CharacterSkillProgressState>
                    {
                        new()
                        {
                            characterType = CharacterActorType.Lian,
                            takenNodes = new List<SkillNodeRankEntry>
                            {
                                new() { nodeId = "LianLian.Attack.DanceTempo", rank = 1 },
                            },
                        },
                    },
                },
                flow = new FlowProgressSaveData
                {
                    firedKeys = new List<string>
                    {
                        "flow_test_lianlian_rescue:entry_lianlian_marker",
                    },
                },
            };

            LianPersistentIdMigration.Migrate(data);
            LianPersistentIdMigration.Migrate(data);

            Assert.That(data.monsterCodex[0].actorId, Is.EqualTo("MonsterLian"));
            Assert.That(data.flags.flags, Has.Count.EqualTo(1));
            Assert.That(data.flags.flags["lake.story.lian_joined"], Is.True);
            Assert.That(data.recipe.monsterKillsByActorId["MonsterLian"], Is.EqualTo(5));
            Assert.That(data.quest.completedQuestIds, Is.EqualTo(new[] { "quest_sub_lake_rescue_lian" }));
            Assert.That(data.quest.trackedQuestId, Is.EqualTo("quest_sub_lake_rescue_lian"));
            Assert.That(data.quest.activeQuests[0].objectiveProgress, Contains.Key("obj_help_lian"));
            Assert.That(data.story.recruitmentEncounters[0].encounterId,
                Is.EqualTo("test.combat.lian_rescue"));
            Assert.That(data.story.recruitmentEncounters[0].defeatedHostileIds,
                Is.EqualTo(new[] { "lian_ally" }));
            Assert.That(data.party.roster, Is.EqualTo(new[] { "Lian" }));
            Assert.That(data.party.storyProtagonistType, Is.EqualTo("Lian"));
            Assert.That(data.party.skillProgress[0].takenNodes[0].nodeId,
                Is.EqualTo("Lian.Attack.DanceTempo"));
            Assert.That(data.flow.firedKeys[0],
                Is.EqualTo("flow_test_lian_rescue:entry_lian_marker"));
        }
    }
}
