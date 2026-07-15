using System;
using System.Collections.Generic;

namespace UPlayGround.Cycle
{
    public readonly struct BossDefeatContext
    {
        public readonly string bossActorId;
        public readonly string cycleSpawnId;
        public readonly bool finishedBySpecialBreakAttack;
        public readonly bool noHit;
        public BossDefeatContext(string bossActorId, string cycleSpawnId, bool specialBreak, bool noHit)
        { this.bossActorId = bossActorId; this.cycleSpawnId = cycleSpawnId; finishedBySpecialBreakAttack = specialBreak; this.noHit = noHit; }
    }

    [Serializable]
    public sealed class AssistPityEntry { public string assistId; public int failures; }

    public readonly struct BossRecruitmentResult
    {
        public readonly string assistId;
        public readonly bool rolled;
        public readonly bool success;
        public readonly float finalChance;
        public readonly int pityBefore;
        public readonly int pityAfter;
        public readonly AssistRecruitResult rosterResult;
        public BossRecruitmentResult(string assistId, bool rolled, bool success, float chance, int before, int after, AssistRecruitResult roster)
        { this.assistId = assistId; this.rolled = rolled; this.success = success; finalChance = chance; pityBefore = before; pityAfter = after; rosterResult = roster; }
    }

    public sealed class BossRecruitmentService
    {
        private readonly Dictionary<string, int> _pity = new(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, int> Pity => _pity;

        public BossRecruitmentResult Roll(string assistId, in BossDefeatContext context, Random random, AssistRosterService roster, int maxRosterSize = 4)
        {
            if (string.IsNullOrWhiteSpace(assistId) || random == null) return new BossRecruitmentResult(assistId, false, false, 0f, 0, 0, null);
            _pity.TryGetValue(assistId, out int before);
            float chance = Math.Min(1f, 0.40f + (context.finishedBySpecialBreakAttack ? 0.35f : 0f) + (context.noHit ? 0.15f : 0f) + before * 0.15f);
            bool success = random.NextDouble() < chance;
            int after = success ? 0 : before + 1;
            _pity[assistId] = after;
            AssistRecruitResult rosterResult = success ? roster.TryRecruit(assistId, maxRosterSize) : null;
            return new BossRecruitmentResult(assistId, true, success, chance, before, after, rosterResult);
        }

        public List<AssistPityEntry> Export()
        {
            List<AssistPityEntry> result = new();
            foreach ((string id, int failures) in _pity) result.Add(new AssistPityEntry { assistId = id, failures = failures });
            return result;
        }

        public void Restore(IEnumerable<AssistPityEntry> values)
        {
            _pity.Clear();
            if (values == null) return;
            foreach (AssistPityEntry value in values) if (value != null && !string.IsNullOrWhiteSpace(value.assistId)) _pity[value.assistId] = Math.Max(0, value.failures);
        }
    }
}
