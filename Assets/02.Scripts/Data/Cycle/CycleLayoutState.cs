using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Cycle
{
    [Serializable]
    public sealed class CycleLayoutState
    {
        public string playerSpawnId;
        public List<CycleBossPlacement> outerBosses = new();
        public CycleBossPlacement centralBoss;
        public List<string> activeRespawnPointIds = new();

        public CycleLayoutState Clone()
        {
            CycleLayoutState clone = new()
            {
                playerSpawnId = playerSpawnId,
                centralBoss = centralBoss?.Clone(),
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
}
