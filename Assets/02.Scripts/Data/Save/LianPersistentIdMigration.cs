using System;
using System.Collections.Generic;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Party;
using UPlayGround.Data.Story;

namespace UPlayGround.Data.Save
{
    /// <summary>구버전 세이브의 LianLian 영속 ID를 현재 Lian ID로 이관한다.</summary>
    public static class LianPersistentIdMigration
    {
        private const string LegacyPascalName = "LianLian";
        private const string CurrentPascalName = "Lian";
        private const string LegacyLowerName = "lianlian";
        private const string CurrentLowerName = "lian";

        /// <summary>세이브 컨테이너의 리안 관련 영속 ID를 손실 없이 현재 규약으로 통일한다.</summary>
        public static void Migrate(GameSaveData data)
        {
            if (data == null)
                return;

            MigrateMonsterCodex(data.monsterCodex);
            MigrateWorld(data.world);
            MigrateParty(data.party);
            MigrateInventory(data.inventory);
            MigrateStory(data.story);
            MigrateFlags(data.flags);
            MigrateRecipe(data.recipe);
            MigrateQuest(data.quest);
            MigrateStringList(data.firstTimeGuide?.shownGuideIds);
            MigrateFlow(data.flow);
        }

        /// <summary>단일 영속 ID의 구 명칭 조각을 현재 명칭으로 치환한다.</summary>
        public static string MigrateId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return id;

            return id.Replace(LegacyPascalName, CurrentPascalName)
                .Replace(LegacyLowerName, CurrentLowerName);
        }

        private static void MigrateMonsterCodex(List<MonsterCodexEntrySave> entries)
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                    entries[i].actorId = MigrateId(entries[i].actorId);
            }
        }

        private static void MigrateWorld(WorldStateSaveData world)
        {
            if (world?.respawnStates == null)
                return;

            for (int i = 0; i < world.respawnStates.Count; i++)
            {
                MonsterRespawnState state = world.respawnStates[i];
                if (state != null)
                    state.actorId = MigrateId(state.actorId);
            }
        }

        private static void MigrateParty(PartySaveData party)
        {
            if (party == null)
                return;

            if (party.members != null)
            {
                for (int i = 0; i < party.members.Count; i++)
                {
                    PartyMemberSaveEntry member = party.members[i];
                    if (member != null)
                        member.type = MigrateId(member.type);
                }
            }

            MigrateStringList(party.roster);
            MigrateStringList(party.battleOrder);
            party.storyProtagonistType = MigrateId(party.storyProtagonistType);
            MigrateSkillProgress(party.skillProgress);
            MigrateAbilitySystems(party.abilitySystems);
        }

        private static void MigrateSkillProgress(List<CharacterSkillProgressState> states)
        {
            if (states == null)
                return;

            for (int i = 0; i < states.Count; i++)
            {
                List<SkillNodeRankEntry> nodes = states[i]?.takenNodes;
                if (nodes == null)
                    continue;

                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j] != null)
                        nodes[j].nodeId = MigrateId(nodes[j].nodeId);
                }
            }
        }

        private static void MigrateAbilitySystems(List<CharacterAbilitySystemSaveEntry> entries)
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                CharacterAbilitySystemSaveEntry entry = entries[i];
                if (entry == null)
                    continue;

                entry.type = MigrateId(entry.type);
                MigrateAbilitySystem(entry.abilitySystem);
            }
        }

        private static void MigrateAbilitySystem(AbilitySystemSaveData abilitySystem)
        {
            if (abilitySystem == null)
                return;

            if (abilitySystem.attributes != null)
            {
                for (int i = 0; i < abilitySystem.attributes.Count; i++)
                {
                    AttributeSaveEntry entry = abilitySystem.attributes[i];
                    if (entry != null)
                        entry.attributeId = MigrateId(entry.attributeId);
                }
            }

            if (abilitySystem.cooldowns != null)
            {
                for (int i = 0; i < abilitySystem.cooldowns.Count; i++)
                {
                    GasCooldownSaveEntry entry = abilitySystem.cooldowns[i];
                    if (entry != null)
                        entry.groupId = MigrateId(entry.groupId);
                }
            }

            if (abilitySystem.activeEffects == null)
                return;

            for (int i = 0; i < abilitySystem.activeEffects.Count; i++)
            {
                ActiveEffectSaveEntry entry = abilitySystem.activeEffects[i];
                if (entry == null)
                    continue;

                entry.effectId = MigrateId(entry.effectId);
                entry.sourceActorId = MigrateId(entry.sourceActorId);
                if (entry.setByCaller == null)
                    continue;

                for (int j = 0; j < entry.setByCaller.Count; j++)
                {
                    SetByCallerSaveEntry value = entry.setByCaller[j];
                    if (value != null)
                        value.key = MigrateId(value.key);
                }
            }
        }

        private static void MigrateInventory(InventorySaveData inventory)
        {
            if (inventory?.equipment == null)
                return;

            for (int i = 0; i < inventory.equipment.Count; i++)
            {
                CharacterEquipmentSaveEntry entry = inventory.equipment[i];
                if (entry != null)
                    entry.type = MigrateId(entry.type);
            }
        }

        private static void MigrateStory(StorySaveData story)
        {
            if (story == null)
                return;

            MigrateStringList(story.completedStories);
            if (story.recruitmentEncounters == null)
                return;

            for (int i = 0; i < story.recruitmentEncounters.Count; i++)
            {
                RecruitmentEncounterSaveEntry entry = story.recruitmentEncounters[i];
                if (entry == null)
                    continue;

                entry.encounterId = MigrateId(entry.encounterId);
                MigrateStringList(entry.defeatedHostileIds);
            }
        }

        private static void MigrateFlags(FlagSaveData flagData)
        {
            if (flagData?.flags == null)
                return;

            var migrated = new Dictionary<string, bool>(flagData.flags.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, bool> pair in flagData.flags)
            {
                string key = MigrateId(pair.Key);
                migrated[key] = migrated.TryGetValue(key, out bool existing)
                    ? existing || pair.Value
                    : pair.Value;
            }
            flagData.flags = migrated;
        }

        private static void MigrateRecipe(RecipeSaveData recipe)
        {
            if (recipe?.monsterKillsByActorId == null)
                return;

            var migrated = new Dictionary<string, int>(
                recipe.monsterKillsByActorId.Count,
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in recipe.monsterKillsByActorId)
            {
                string key = MigrateId(pair.Key);
                long total = pair.Value;
                if (migrated.TryGetValue(key, out int existing))
                    total += existing;
                migrated[key] = (int)Math.Min(int.MaxValue, Math.Max(0L, total));
            }
            recipe.monsterKillsByActorId = migrated;
        }

        private static void MigrateQuest(QuestSaveData quest)
        {
            if (quest == null)
                return;

            MigrateStringList(quest.completedQuestIds);
            MigrateStringList(quest.failedQuestIds);
            quest.trackedQuestId = MigrateId(quest.trackedQuestId);
            if (quest.activeQuests == null)
                return;

            var migrated = new Dictionary<string, ActiveQuestSaveEntry>(StringComparer.Ordinal);
            for (int i = 0; i < quest.activeQuests.Count; i++)
            {
                ActiveQuestSaveEntry entry = quest.activeQuests[i];
                if (entry == null)
                    continue;

                string questId = MigrateId(entry.questId);
                if (!migrated.TryGetValue(questId, out ActiveQuestSaveEntry target))
                {
                    target = new ActiveQuestSaveEntry { questId = questId };
                    migrated.Add(questId, target);
                }
                MergeProgress(target.objectiveProgress, entry.objectiveProgress);
            }
            quest.activeQuests = new List<ActiveQuestSaveEntry>(migrated.Values);
        }

        private static void MergeProgress(
            Dictionary<string, int> target,
            Dictionary<string, int> source)
        {
            if (source == null)
                return;

            foreach (KeyValuePair<string, int> pair in source)
            {
                string key = MigrateId(pair.Key);
                if (!target.TryGetValue(key, out int current) || pair.Value > current)
                    target[key] = pair.Value;
            }
        }

        private static void MigrateFlow(FlowProgressSaveData flow)
        {
            if (flow == null)
                return;

            MigrateStringList(flow.firedKeys);
            if (flow.entries == null)
                return;

            var migrated = new Dictionary<string, FlowEntryProgressSave>(StringComparer.Ordinal);
            for (int i = 0; i < flow.entries.Count; i++)
            {
                FlowEntryProgressSave entry = flow.entries[i];
                if (entry == null)
                    continue;

                string key = MigrateId(entry.key);
                if (!migrated.TryGetValue(key, out FlowEntryProgressSave target))
                {
                    target = new FlowEntryProgressSave { key = key };
                    migrated.Add(key, target);
                }
                target.fireCount = Math.Max(target.fireCount, entry.fireCount);
                target.completeCount = Math.Max(target.completeCount, entry.completeCount);
            }
            flow.entries = new List<FlowEntryProgressSave>(migrated.Values);
        }

        private static void MigrateStringList(List<string> values)
        {
            if (values == null)
                return;

            var unique = new HashSet<string>(StringComparer.Ordinal);
            int writeIndex = 0;
            for (int i = 0; i < values.Count; i++)
            {
                string value = MigrateId(values[i]);
                if (!unique.Add(value))
                    continue;

                values[writeIndex++] = value;
            }

            if (writeIndex < values.Count)
                values.RemoveRange(writeIndex, values.Count - writeIndex);
        }
    }
}
