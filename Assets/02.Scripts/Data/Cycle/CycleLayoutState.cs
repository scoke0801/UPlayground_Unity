using System;
using System.Collections.Generic;
using UPlayGround.Data.Save;

namespace UPlayGround.Data.Cycle
{
    [Serializable]
    public sealed class CycleLayoutState
    {
        public string playerSpawnId;
        public List<CycleBossPlacement> outerBosses = new();
        public CycleBossPlacement centralBoss;
        public List<string> activeRespawnPointIds = new();
        public CycleGeneratedContentLayout generatedContent;

        public CycleLayoutState Clone()
        {
            CycleLayoutState clone = new()
            {
                playerSpawnId = playerSpawnId,
                centralBoss = centralBoss?.Clone(),
                generatedContent = generatedContent?.Clone(),
                activeRespawnPointIds = activeRespawnPointIds != null
                    ? new List<string>(activeRespawnPointIds)
                    : new List<string>(),
            };
            if (outerBosses != null)
            {
                foreach (CycleBossPlacement boss in outerBosses)
                    if (boss != null) clone.outerBosses.Add(boss.Clone());
            }
            return clone;
        }

        public CycleBossPlacement FindBoss(string spawnId)
        {
            if (centralBoss != null && string.Equals(centralBoss.spawnId, spawnId, StringComparison.Ordinal))
                return centralBoss;
            if (outerBosses == null) return null;
            return outerBosses.Find(value => value != null && string.Equals(value.spawnId, spawnId, StringComparison.Ordinal));
        }
    }

    [Serializable]
    public sealed class CycleBossPlacement
    {
        public string spawnId;
        public string actorId;
        public bool isCentral;
        public bool discovered;
        public bool defeated;
        public bool playerTookDamageAfterDiscovery;
        public bool finishedBySpecialBreakAttack;
        public bool defeatedNoHit;

        public CycleBossPlacement Clone() => (CycleBossPlacement)MemberwiseClone();
    }

    [Serializable]
    public sealed class CycleGeneratedContentLayout
    {
        public int placementValidationVersion;
        public string generationId;
        public string questId;
        public List<CycleGeneratedEncounterPlacement> encounters = new();
        public List<CycleGeneratedLootPlacement> loot = new();
        public List<CycleGeneratedInteractionPlacement> interactions = new();

        public CycleGeneratedContentLayout Clone()
        {
            CycleGeneratedContentLayout clone = new()
            {
                placementValidationVersion = placementValidationVersion,
                generationId = generationId,
                questId = questId,
            };
            if (encounters != null)
                foreach (CycleGeneratedEncounterPlacement value in encounters)
                    if (value != null) clone.encounters.Add(value.Clone());
            if (loot != null)
                foreach (CycleGeneratedLootPlacement value in loot)
                    if (value != null) clone.loot.Add(value.Clone());
            if (interactions != null)
                foreach (CycleGeneratedInteractionPlacement value in interactions)
                    if (value != null) clone.interactions.Add(value.Clone());
            return clone;
        }

        public CycleGeneratedEncounterPlacement FindEncounter(string encounterId) =>
            encounters?.Find(value => value != null && string.Equals(value.encounterId, encounterId, StringComparison.Ordinal));

        public CycleGeneratedLootPlacement FindLoot(string lootId) =>
            loot?.Find(value => value != null && string.Equals(value.lootId, lootId, StringComparison.Ordinal));

        public CycleGeneratedInteractionPlacement FindInteraction(string interactionId) =>
            interactions?.Find(value => value != null && string.Equals(value.interactionId, interactionId, StringComparison.Ordinal));
    }

    [Serializable]
    public sealed class CycleGeneratedEncounterPlacement
    {
        public string encounterId;
        public string routeId;
        public float routeProgress;
        public float lateralOffset;
        public int difficultyZone;
        public int threatBudget;
        public SerializableVector3 anchorPosition;
        public bool cleared;
        public List<CycleGeneratedMonsterPlacement> monsters = new();

        public CycleGeneratedEncounterPlacement Clone()
        {
            CycleGeneratedEncounterPlacement clone = (CycleGeneratedEncounterPlacement)MemberwiseClone();
            clone.monsters = new List<CycleGeneratedMonsterPlacement>();
            if (monsters != null)
                foreach (CycleGeneratedMonsterPlacement value in monsters)
                    if (value != null) clone.monsters.Add(value.Clone());
            return clone;
        }
    }

    [Serializable]
    public sealed class CycleGeneratedMonsterPlacement
    {
        public string actorId;
        public int threatCost;
        public SerializableVector3 localOffset;
        public SerializableVector3 position;
        public float yaw;

        public CycleGeneratedMonsterPlacement Clone() => (CycleGeneratedMonsterPlacement)MemberwiseClone();
    }

    [Serializable]
    public sealed class CycleGeneratedLootPlacement
    {
        public string lootId;
        public string routeId;
        public float routeProgress;
        public float lateralOffset;
        public int itemId;
        public int count = 1;
        public SerializableVector3 position;
        public bool collected;

        public CycleGeneratedLootPlacement Clone() => (CycleGeneratedLootPlacement)MemberwiseClone();
    }

    [Serializable]
    public sealed class CycleGeneratedInteractionPlacement
    {
        public string interactionId;
        public string routeId;
        public float routeProgress;
        public float lateralOffset;
        public SerializableVector3 position;
        public bool completed;

        public CycleGeneratedInteractionPlacement Clone() => (CycleGeneratedInteractionPlacement)MemberwiseClone();
    }
}
